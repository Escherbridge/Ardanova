import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  browserOverTheBff,
  dotNetAnswer,
  dotNetBaseUrl,
  fakeDotNetApi,
  refusalOf,
} from "~/server/api/dotnet-test-edge";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-ada", role: "INDIVIDUAL" },
  }),
}));

const browser = await browserOverTheBff();

afterEach(() => {
  vi.unstubAllGlobals();
});

afterAll(() => {
  vi.unstubAllEnvs();
});

/** .NET `PostDto` for a freshly created ask, as PostsController answers 201. */
const createdAsk = {
  id: "post-ask-1",
  authorId: "member-ada",
  projectId: "project-barn",
  guildId: null,
  type: "HELP_WANTED",
  visibility: "PUBLIC",
  title: null,
  content: "Framing the east wall Saturday, need two hands",
  metadata: null,
  hashtags: [],
  taskId: "task-frame",
  likesCount: 0,
  commentsCount: 0,
  sharesCount: 0,
  viewsCount: 0,
  isPinned: false,
  createdAt: "2026-11-01T09:00:00Z",
  updatedAt: "2026-11-01T09:00:00Z",
};

/** The error envelope .NET writes for a refused result: `{ error }` plus `statusCode` and `traceId`. */
function refused(status: number, error: string) {
  return dotNetAnswer(status, {
    error,
    statusCode: status,
    traceId: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  });
}

describe("post.create (HELP_WANTED, plan §2)", () => {
  it("sends the ask signed as the session user, with no author field a caller could forge", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(201, createdAsk));

    await browser.post.create.mutate({
      type: "HELP_WANTED",
      taskId: "task-frame",
      content: "Framing the east wall Saturday, need two hands",
    });

    expect(requests).toEqual([
      expect.objectContaining({
        method: "POST",
        url: `${dotNetBaseUrl}/api/posts`,
        signedActor: "member-ada",
      }),
    ]);
    expect(requests[0]?.body).toMatchObject({
      type: "HELP_WANTED",
      taskId: "task-frame",
    });
    expect(requests[0]?.body).not.toHaveProperty("authorId");
  });

  it.each([
    [400, "An ask for help needs a task.", "BAD_REQUEST"],
    [
      403,
      "Only the project's members can ask for help on its tasks.",
      "FORBIDDEN",
    ],
    // A missing task, or one on a draft the author cannot see.
    [404, "Task task-frame not found", "NOT_FOUND"],
    // ExceptionHandlingMiddleware's own words: a fault never reads as the caller's mistake.
    [
      500,
      "An error occurred processing your request.",
      "INTERNAL_SERVER_ERROR",
    ],
  ] as const)(
    "a .NET %i reaches the browser as %s with the API's own words",
    async (status, error, code) => {
      fakeDotNetApi(() => refused(status, error));

      const failure = await refusalOf(
        browser.post.create.mutate({
          type: "HELP_WANTED",
          taskId: "task-frame",
          content: "Need a hand",
        }),
      );

      expect(failure.data?.code).toBe(code);
      expect(failure.message).toBe(error);
    },
  );
});

describe("post.delete (an author, or a manager removing an ask, plan §2)", () => {
  it("a .NET 204 removes it; the API alone decided who may", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(204));

    await browser.post.delete.mutate({ id: "post-ask-1" });

    expect(requests).toEqual([
      expect.objectContaining({
        method: "DELETE",
        url: `${dotNetBaseUrl}/api/posts/post-ask-1`,
        signedActor: "member-ada",
      }),
    ]);
  });

  it.each([
    [403, "Only the author can delete this post", "FORBIDDEN"],
    [404, "Post post-ask-1 not found", "NOT_FOUND"],
  ] as const)(
    "a .NET %i reaches the browser as %s",
    async (status, error, code) => {
      fakeDotNetApi(() => refused(status, error));

      const failure = await refusalOf(
        browser.post.delete.mutate({ id: "post-ask-1" }),
      );

      expect(failure.data?.code).toBe(code);
      expect(failure.message).toBe(error);
    },
  );
});
