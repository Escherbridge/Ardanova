import { describe, expect, it } from "vitest";

import {
  FEED_REASON_KINDS,
  type FeedReason,
  type FeedScope,
} from "~/lib/contracts/feed-contract";
import { CHAIN_WORDS } from "~/lib/vocabulary/chain-words";

import { feedCardFixture } from "./card-fixture";
import { helpStateOf, primaryActionFor } from "./primary-action";
import { reasonFragment } from "./reason";
import { KNOWN_FEED_POST_TYPES } from "./registry";

const reason = (
  kind: string,
  about: string | null,
  aboutId: string | null,
  text = "server sentence",
): FeedReason => ({ kind, about, aboutId, text });

const ROWS: {
  row: string;
  reasons: FeedReason[];
  scope?: FeedScope;
  expected: string | null;
}[] = [
  {
    row: "renders from kind + aboutId, never the server's text",
    reasons: [
      reason(
        "FollowedProject",
        "PROJECT",
        "project-p",
        "Because you follow Something Else",
      ),
    ],
    expected: "Following Barn raising",
  },
  {
    row: "team",
    reasons: [reason("ProjectTeam", "PROJECT", "project-p")],
    expected: "You're on Barn raising",
  },
  {
    row: "tag interest",
    reasons: [reason("TagInterest", "TAG", "carpentry")],
    expected: "#carpentry",
  },
  {
    row: "sector code becomes its label",
    reasons: [reason("NewInSector", "SECTOR", "FOOD_BEVERAGE")],
    expected: "New in Food and drink",
  },
  {
    row: "your own activity says nothing; the next reason speaks",
    reasons: [
      reason("YourActivity", null, null),
      reason("TagInterest", "TAG", "carpentry"),
    ],
    expected: "#carpentry",
  },
  {
    row: "only your own activity: no fragment",
    reasons: [reason("YourActivity", null, null)],
    expected: null,
  },
  {
    row: "on the project's own page, a reason naming that project is dropped",
    reasons: [reason("InScope", "PROJECT", "project-p")],
    scope: "project:project-p",
    expected: null,
  },
  {
    row: "on a tag page, the sector match explains why an untagged card is there",
    reasons: [reason("InScope", "SECTOR", "CONSTRUCTION")],
    scope: "tag:construction",
    expected: "In Construction",
  },
  {
    row: "a tag reason restating the tag page is dropped, whatever its case",
    reasons: [reason("InScope", "TAG", "carpentry")],
    scope: "tag:Carpentry",
    expected: null,
  },
  {
    row: "a kind this client does not know falls back to the server's text",
    reasons: [reason("SharedByNeighbour", null, null, "From your street")],
    expected: "From your street",
  },
  {
    row: "a known kind wins over an unknown one listed first",
    reasons: [
      reason("SharedByNeighbour", null, null, "From your street"),
      reason("FollowedProject", "PROJECT", "project-p"),
    ],
    expected: "Following Barn raising",
  },
];

describe("reasonFragment", () => {
  it.each(ROWS)("$row", ({ reasons, scope, expected }) => {
    expect(reasonFragment(feedCardFixture({ reasons }), scope)).toBe(expected);
  });
});

describe("the words a member is shown on a feed card", () => {
  const ENGAGEMENT_WORDS = /\b(trending|viral|popular|views?|streaks?|top)\b/i;

  it("carry no chain machinery and no engagement bait, over every action, state and reason", () => {
    const shown = new Set<string>();
    for (const postType of KNOWN_FEED_POST_TYPES) {
      for (const state of ["OPEN", "PICKED_UP", "DONE"]) {
        const card = feedCardFixture({
          postType,
          viewer: { isMember: true, isFollowing: false, canClaim: true },
          task: { id: "task-t", state, assigneeName: "Ada" },
          opportunity: { id: "opp-1", title: "Lead carpenter" },
        });
        shown.add(primaryActionFor(card).label);
        const chip = helpStateOf(card);
        if (chip) shown.add(chip.label);
      }
    }
    for (const kind of FEED_REASON_KINDS) {
      for (const about of [
        "PROJECT",
        "GUILD",
        "PERSON",
        "SECTOR",
        "TAG",
        null,
      ]) {
        const fragment = reasonFragment(
          feedCardFixture({ reasons: [reason(kind, about, "CONSTRUCTION")] }),
        );
        if (fragment) shown.add(fragment);
      }
    }

    const offending = [...shown].filter(
      (copy) =>
        ENGAGEMENT_WORDS.test(copy) ||
        CHAIN_WORDS.some((word) => word.pattern.test(copy)),
    );
    expect(offending).toEqual([]);
    expect(shown.size).toBeGreaterThan(10);
  });
});
