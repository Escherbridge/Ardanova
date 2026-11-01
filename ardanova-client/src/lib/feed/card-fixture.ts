import type { FeedKitCard } from "./types";

/** Test builder: a valid card from the live contract shape, with only what a case changes overridden. */
export function feedCardFixture(
  overrides: Partial<FeedKitCard> = {},
): FeedKitCard {
  return {
    id: "post-1",
    postType: "POST",
    sourceType: null,
    occurredAt: "2026-10-31T12:00:00Z",
    author: { id: "user-b", name: "Bea Builder", image: null },
    title: null,
    body: "Framing the east wall Saturday",
    bodyIsUserWritten: true,
    hashtags: [],
    project: {
      id: "project-p",
      title: "Barn raising",
      slug: "barn-raising",
      summary: "A barn for the commons.",
      sectors: ["CONSTRUCTION"],
    },
    guild: null,
    opportunity: null,
    event: null,
    funding: null,
    thread: null,
    counters: { likes: 0, replies: 0, shares: 0 },
    viewer: { isMember: false, isFollowing: false, canClaim: false },
    isDiscovery: false,
    reasons: [],
    ...overrides,
  };
}
