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

const followUrl = `${dotNetBaseUrl}/api/projects/project-barn/follow`;

/**
 * Each verb answers a different .NET shape (POST the follow row, GET and DELETE `{ following }`) and the router maps
 * all three to one `ProjectFollowState`. Following twice, and unfollowing twice at once, are idempotent in .NET itself
 * (ProjectFollowFlowTests on real Postgres); this side only signs and maps.
 */
describe("project follow (plan §2, §8-6)", () => {
  it("follow POSTs as the session user with no follower id in the body, and reads the follow row as following", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, {
        id: "follow-1",
        projectId: "project-barn",
        userId: "member-ada",
        createdAt: "2026-11-01T09:00:00Z",
      }),
    );

    const state = await browser.project.followProject.mutate({
      projectId: "project-barn",
    });

    expect(state).toEqual({ projectId: "project-barn", isFollowing: true });
    expect(requests).toEqual([
      {
        method: "POST",
        url: followUrl,
        body: {},
        signedActor: "member-ada",
      },
    ]);
  });

  it("unfollow DELETEs as the session user and reads .NET's { following: false }", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, { following: false }),
    );

    const state = await browser.project.unfollowProject.mutate({
      projectId: "project-barn",
    });

    expect(state).toEqual({ projectId: "project-barn", isFollowing: false });
    expect(requests).toEqual([
      expect.objectContaining({
        method: "DELETE",
        url: followUrl,
        signedActor: "member-ada",
      }),
    ]);
  });

  it.each([true, false])(
    "the viewer's own state reads .NET's { following: %s }",
    async (following) => {
      fakeDotNetApi(() => dotNetAnswer(200, { following }));

      const state = await browser.project.getMyFollowState.query({
        projectId: "project-barn",
      });

      expect(state).toEqual({
        projectId: "project-barn",
        isFollowing: following,
      });
    },
  );

  it("a hidden or missing project's 404 reaches the browser as NOT_FOUND", async () => {
    fakeDotNetApi(() =>
      dotNetAnswer(404, {
        error: "Project with id project-draft not found",
        statusCode: 404,
        traceId: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
      }),
    );

    const failure = await refusalOf(
      browser.project.followProject.mutate({ projectId: "project-draft" }),
    );

    expect(failure.data?.code).toBe("NOT_FOUND");
  });
});
