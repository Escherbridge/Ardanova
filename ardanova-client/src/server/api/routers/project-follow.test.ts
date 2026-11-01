import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, projects } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  projects: {
    followProject: vi.fn(),
    unfollowProject: vi.fn(),
    getMyFollowState: vi.fn(),
  },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { projects } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { getActorAssertion } from "~/server/actor-assertion";
import { projectRouter } from "./project";

async function callerAs(userId: string) {
  currentSession.mockResolvedValue({
    expires: "2099-01-01T00:00:00Z",
    user: { id: userId, role: "INDIVIDUAL" },
  });
  return createCallerFactory(projectRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("project follow (plan §2, §8-6)", () => {
  beforeEach(() => vi.clearAllMocks());

  it("signs the follow as the signed-in session user, never a caller-supplied follower id", async () => {
    process.env.ACTOR_ASSERTION_HMAC_KEY = "follow-test-signing-key-at-least-32-bytes";
    let signedSubject: string | undefined;
    projects.followProject.mockImplementation(async () => {
      const assertion = getActorAssertion({ method: "POST", url: "/api/projects/project-1/follow" });
      const payload = assertion?.split(".")[0];
      signedSubject = payload
        ? (JSON.parse(Buffer.from(payload, "base64url").toString()) as { subject: string }).subject
        : undefined;
      return { status: 200, data: { projectId: "project-1", isFollowing: true } };
    });

    const caller = await callerAs("user-ada");
    await caller.followProject({ projectId: "project-1" });

    expect(signedSubject).toBe("user-ada");
  });

  it("following twice both succeed (idempotent, not a conflict)", async () => {
    projects.followProject.mockResolvedValue({
      status: 200,
      data: { projectId: "project-1", isFollowing: true },
    });

    const caller = await callerAs("user-ada");
    const first = await caller.followProject({ projectId: "project-1" });
    const second = await caller.followProject({ projectId: "project-1" });

    expect(first).toEqual({ projectId: "project-1", isFollowing: true });
    expect(second).toEqual({ projectId: "project-1", isFollowing: true });
  });

  it("following a hidden project surfaces NOT_FOUND", async () => {
    projects.followProject.mockResolvedValue({ status: 404, error: "Project not found" });

    const caller = await callerAs("user-ada");
    await expect(caller.followProject({ projectId: "project-hidden" })).rejects.toMatchObject({
      code: "NOT_FOUND",
    });
  });

  it("unfollowing a project never followed still succeeds", async () => {
    projects.unfollowProject.mockResolvedValue({
      status: 200,
      data: { projectId: "project-1", isFollowing: false },
    });

    const caller = await callerAs("user-ada");
    await expect(caller.unfollowProject({ projectId: "project-1" })).resolves.toEqual({
      projectId: "project-1",
      isFollowing: false,
    });
  });

  it("reads back the viewer's own follow state, no follower list involved", async () => {
    projects.getMyFollowState.mockResolvedValue({
      status: 200,
      data: { projectId: "project-1", isFollowing: true },
    });

    const caller = await callerAs("user-ada");
    await expect(caller.getMyFollowState({ projectId: "project-1" })).resolves.toEqual({
      projectId: "project-1",
      isFollowing: true,
    });
  });
});
