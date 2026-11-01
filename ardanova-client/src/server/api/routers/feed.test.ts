import { beforeEach, describe, expect, it, vi } from "vitest";
import { TRPCError } from "@trpc/server";

vi.mock("server-only", () => ({}));

const { currentSession, feed } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  feed: { getPage: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { feed } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { feedRouter } from "./feed";

/** A .NET FeedCardDto as the API serializes it. */
function card(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: "post-1",
    postType: "PROJECT_UPDATE",
    sourceType: "PROJECT_UPDATE",
    occurredAt: "2026-10-31T09:00:00Z",
    author: { id: "user-b", name: "Bo Builder", image: null },
    title: "South wall framed",
    body: "Framed the south wall today #carpentry",
    bodyIsUserWritten: true,
    hashtags: ["carpentry"],
    project: {
      id: "project-1",
      title: "Barn raising",
      slug: "barn-raising",
      summary: null,
      sectors: ["CONSTRUCTION"],
    },
    guild: null,
    opportunity: null,
    event: null,
    funding: null,
    thread: null,
    counters: { likes: 2, replies: 1, shares: 0 },
    viewer: { isMember: false, isFollowing: true, canClaim: false },
    isDiscovery: false,
    reasons: [
      {
        kind: "FollowedProject",
        text: "Because you follow Barn raising",
        about: "PROJECT",
        aboutId: "project-1",
      },
    ],
    ...overrides,
  };
}

async function signedOutCaller() {
  currentSession.mockResolvedValue(null);
  return createCallerFactory(feedRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("feed reads", () => {
  beforeEach(() => vi.clearAllMocks());

  /**
   * The API adds envelope kinds (PostType) and reason kinds before this client redeploys. A page carrying
   * one must still render the rest of the scroll, not fail the whole read.
   */
  it("tolerates an unknown post type and reason kind without failing the page", async () => {
    feed.getPage.mockResolvedValue({
      status: 200,
      data: {
        items: [
          card({
            id: "post-2",
            postType: "HARVEST_LOGGED",
            reasons: [
              { kind: "SeasonalPick", text: "In season", about: null, aboutId: null },
            ],
          }),
          card(),
        ],
        nextCursor: "opaque",
      },
    });

    const page = await (await signedOutCaller()).getPage({ limit: 20 });

    expect(page.items.map((item) => item.postType)).toEqual([
      "HARVEST_LOGGED",
      "PROJECT_UPDATE",
    ]);
    expect(page.nextCursor).toBe("opaque");
  });

  it.each([
    [404, "NOT_FOUND"],
    [400, "BAD_REQUEST"],
    [500, "INTERNAL_SERVER_ERROR"],
  ])("maps an API %i to %s", async (status, code) => {
    feed.getPage.mockResolvedValue({ status, error: "refused" });

    const read = (await signedOutCaller()).getPage({
      limit: 20,
      scope: "project:project-draft",
    });

    await expect(read).rejects.toBeInstanceOf(TRPCError);
    await expect(read).rejects.toMatchObject({ code });
  });
});
