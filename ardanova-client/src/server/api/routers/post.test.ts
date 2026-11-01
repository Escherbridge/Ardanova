import { beforeEach, describe, expect, it, vi } from "vitest";
import { TRPCError } from "@trpc/server";

vi.mock("server-only", () => ({}));

const { currentSession, posts } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  posts: { create: vi.fn(), delete: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { posts } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { getActorAssertion } from "~/server/actor-assertion";
import { postRouter } from "./post";
import { HelpWantedConflictCause } from "~/server/api/help-wanted-conflict-error";

async function callerAs(userId: string) {
  currentSession.mockResolvedValue({
    expires: "2099-01-01T00:00:00Z",
    user: { id: userId, role: "INDIVIDUAL" },
  });
  return createCallerFactory(postRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("post.create (HELP_WANTED, plan §2)", () => {
  beforeEach(() => vi.clearAllMocks());

  it("signs the write as the signed-in session user, never a caller-supplied value", async () => {
    process.env.ACTOR_ASSERTION_HMAC_KEY = "post-test-signing-key-at-least-32-bytes";
    let signedSubject: string | undefined;
    posts.create.mockImplementation(async () => {
      const assertion = getActorAssertion({ method: "POST", url: "/api/posts" });
      const payload = assertion?.split(".")[0];
      signedSubject = payload
        ? (JSON.parse(Buffer.from(payload, "base64url").toString()) as { subject: string }).subject
        : undefined;
      return { status: 201, data: { id: "post-1" } };
    });

    const caller = await callerAs("user-ada");
    await caller.create({ content: "Who can help with the framing?" });

    expect(signedSubject).toBe("user-ada");
  });

  it("a 409 on a HELP_WANTED create surfaces CONFLICT carrying the existing post's id", async () => {
    posts.create.mockResolvedValue({
      status: 409,
      error: "An ask for this task already exists",
      code: "HELP_WANTED_EXISTS",
      conflictId: "post-existing-1",
    });

    const caller = await callerAs("user-ada");
    const create = caller.create({
      content: "Who can help with the framing?",
      type: "HELP_WANTED",
      projectId: "project-1",
      taskId: "task-1",
    });

    await expect(create).rejects.toBeInstanceOf(TRPCError);
    const failure = (await create.catch((error: unknown) => error)) as TRPCError;
    expect(failure.code).toBe("CONFLICT");
    expect(failure.cause).toBeInstanceOf(HelpWantedConflictCause);
    expect((failure.cause as HelpWantedConflictCause).existingPostId).toBe(
      "post-existing-1",
    );
  });

  it("a 409 with no conflictId still surfaces CONFLICT, just without a cause to follow", async () => {
    posts.create.mockResolvedValue({ status: 409, error: "Conflict" });

    const caller = await callerAs("user-ada");
    const create = caller.create({ content: "x", type: "HELP_WANTED", taskId: "task-1" });

    await expect(create).rejects.toMatchObject({ code: "CONFLICT" });
    const failure = (await create.catch((error: unknown) => error)) as TRPCError;
    expect(failure.cause).toBeUndefined();
  });

  it.each([
    [400, "BAD_REQUEST"],
    [403, "FORBIDDEN"],
    [500, "INTERNAL_SERVER_ERROR"],
  ])("maps a non-conflict %i to %s", async (status, code) => {
    posts.create.mockResolvedValue({ status, error: "refused" });
    const caller = await callerAs("user-ada");

    await expect(caller.create({ content: "x" })).rejects.toMatchObject({ code });
  });
});

describe("post.delete (manager removes a HELP_WANTED, plan §2)", () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    [403, "FORBIDDEN"],
    [404, "NOT_FOUND"],
    [500, "INTERNAL_SERVER_ERROR"],
  ])("maps the API's %i to %s; the API alone decides who may remove", async (status, code) => {
    posts.delete.mockResolvedValue({ status, error: "refused" });
    const caller = await callerAs("user-manager");

    await expect(caller.delete({ id: "post-1" })).rejects.toMatchObject({ code });
  });
});
