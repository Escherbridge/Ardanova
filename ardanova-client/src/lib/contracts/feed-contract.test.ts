import { describe, expect, it } from "vitest";

import { feedPageSchema } from "./feed-contract";

/** A .NET FeedCardDto as GET /api/feed serialises it (camelCase), before the fields each case changes. */
const apiCard = {
  id: "post-1",
  postType: "POST",
  sourceType: null,
  occurredAt: "2026-11-01T12:00:00Z",
  author: { id: "user-b", name: "Bea", image: null },
  title: null,
  body: "Framing the east wall Saturday, need two hands #carpentry",
  bodyIsUserWritten: true,
  hashtags: ["carpentry"],
  project: {
    id: "project-p",
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
  counters: { likes: 0, replies: 0, shares: 0 },
  viewer: { isMember: true, isFollowing: false, canClaim: false },
  isDiscovery: false,
  reasons: [],
  task: null,
};

const firstCard = (card: Record<string, unknown>) =>
  feedPageSchema.parse({ items: [card], nextCursor: null }).items[0];

describe("feed API contract", () => {
  // feedCardSchema is a plain z.object, which strips keys it does not list: these fail if a field is dropped.
  it("keeps a HELP_WANTED card's task facts and the viewer's claim through the parse", () => {
    const ask = {
      ...apiCard,
      postType: "HELP_WANTED",
      viewer: { isMember: true, isFollowing: false, canClaim: true },
      task: {
        id: "task-t",
        state: "PICKED_UP",
        assigneeName: "Ada",
        paid: true,
      },
    };

    const card = firstCard(ask);

    expect(card?.task).toEqual({
      id: "task-t",
      state: "PICKED_UP",
      assigneeName: "Ada",
      paid: true,
    });
    expect(card?.viewer.canClaim).toBe(true);
  });

  it("keeps the opportunity slug the Apply link resolves by", () => {
    const opening = {
      ...apiCard,
      postType: "OPPORTUNITY_OPENED",
      opportunity: {
        id: "opp-1",
        title: "Lead carpenter",
        slug: "lead-carpenter",
      },
    };

    expect(firstCard(opening)?.opportunity).toEqual(opening.opportunity);
  });

  it("still reads a card from an API without the phase 3 fields, and a task state it does not know yet", () => {
    const older: Record<string, unknown> = {
      ...apiCard,
      opportunity: { id: "opp-1", title: "Lead carpenter" },
    };
    Reflect.deleteProperty(older, "task");
    const newerState = {
      ...apiCard,
      task: {
        id: "task-t",
        state: "UNDER_REVIEW",
        assigneeName: null,
        paid: false,
      },
    };

    expect(firstCard(older)?.opportunity).toEqual({
      id: "opp-1",
      title: "Lead carpenter",
    });
    expect(firstCard(older)?.task).toBeUndefined();
    expect(firstCard(newerState)?.task?.state).toBe("UNDER_REVIEW");
  });
});
