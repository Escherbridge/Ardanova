import { describe, expect, it } from "vitest";

import type { FeedPostType } from "~/lib/contracts/feed-contract";

import { feedCardFixture } from "./card-fixture";
import {
  helpStateOf,
  messageActionFor,
  primaryActionFor,
  type FeedActionLabel,
} from "./primary-action";
import type { FeedKitCard } from "./types";

type Expected = {
  label: FeedActionLabel;
  /** null: an inline reply on the card. */
  href: string | null;
};

const guild = { id: "guild-g", name: "Timber framers", slug: "timber-framers" };
const opportunity = { id: "opp-1", title: "Lead carpenter" };
const task = { id: "task-t", state: "OPEN", assigneeName: null, paid: false };
const member = { isMember: true, isFollowing: false, canClaim: true };
const follower = { isMember: false, isFollowing: true, canClaim: false };

/** Keyed by the contract's FeedPostType: a type added there without a row here fails `tsc`. */
const EVERY_CONTRACT_TYPE: Record<
  FeedPostType,
  { facts?: Partial<FeedKitCard>; expected: Expected }
> = {
  POST: { expected: { label: "Reply", href: null } },
  PROJECT_UPDATE: { expected: { label: "Reply", href: null } },
  GUILD_ACTIVITY: {
    facts: { project: null, guild },
    expected: { label: "Reply", href: null },
  },
  TASK_COMPLETED: {
    facts: { bodyIsUserWritten: false, body: "Finished a piece of work." },
    expected: { label: "Open board", href: "/projects/barn-raising/board" },
  },
  MILESTONE: {
    facts: { bodyIsUserWritten: false, body: "Reached a milestone." },
    expected: { label: "Open board", href: "/projects/barn-raising/board" },
  },
  PROPOSAL: { expected: { label: "Reply", href: null } },
  SHOP_ITEM: { expected: { label: "Reply", href: null } },
  PROJECT_LAUNCHED: {
    facts: { bodyIsUserWritten: false, body: "Opened to the public." },
    expected: { label: "Reply", href: null },
  },
  FUNDING_PROGRESS: {
    facts: { bodyIsUserWritten: false, funding: { percent: 50 } },
    expected: { label: "Back this", href: "/projects/barn-raising/invest" },
  },
  OPPORTUNITY_OPENED: {
    facts: { opportunity: { ...opportunity, slug: "lead-carpenter" } },
    expected: { label: "Apply", href: "/opportunities/lead-carpenter" },
  },
  OPPORTUNITY_UPDATE: {
    // Today's DTO has no slug: the id is the fallback (the page resolves slugs; see AGENTS.md §Asks).
    facts: { opportunity },
    expected: { label: "Apply", href: "/opportunities/opp-1" },
  },
  GUILD_FOUNDED: {
    facts: { project: null, guild, bodyIsUserWritten: false },
    expected: { label: "Reply", href: null },
  },
  GUILD_UPDATE: {
    facts: { project: null, guild },
    expected: { label: "Reply", href: null },
  },
  EVENT_SCHEDULED: {
    facts: {
      event: {
        id: "evt-1",
        title: "Raising day",
        startsAt: "2026-11-07T15:00:00Z",
      },
    },
    expected: { label: "See events", href: "/events" },
  },
  DISCUSSION: {
    facts: {
      thread: {
        source: "PROJECT_COMMENT",
        commentId: "c-1",
        targetType: "PROJECT",
        targetId: "project-p",
        opportunityId: null,
      },
    },
    expected: { label: "Reply", href: "/projects/barn-raising?tab=comments" },
  },
  MEMBER_JOINED: {
    facts: { bodyIsUserWritten: false, body: "Joined the project team." },
    expected: { label: "Reply", href: null },
  },
  // The common case; every HELP_WANTED row is in HELP_WANTED_ROWS below.
  HELP_WANTED: {
    facts: { viewer: member, task },
    expected: { label: "Pick up task", href: "/tasks/task-t" },
  },
};

/** Plan §2 HELP_WANTED rows, first match wins; plus the row a card without task facts (an older API) takes. */
const HELP_WANTED_ROWS: {
  row: string;
  facts: Partial<FeedKitCard>;
  expected: Expected;
  chip: string | null;
}[] = [
  {
    row: "no task facts (an older API), even with canClaim",
    facts: { viewer: member },
    expected: { label: "Reply", href: null },
    chip: null,
  },
  {
    row: "task done",
    facts: { viewer: member, task: { ...task, state: "DONE" } },
    expected: { label: "Open board", href: "/projects/barn-raising/board" },
    chip: "Done",
  },
  {
    row: "task assigned",
    facts: {
      viewer: { ...member, canClaim: false },
      task: { ...task, state: "PICKED_UP", assigneeName: "Ada" },
    },
    expected: { label: "Reply", href: null },
    chip: "Picked up by Ada",
  },
  {
    row: "canClaim: member, unassigned, unpaid",
    facts: { viewer: member, task },
    expected: { label: "Pick up task", href: "/tasks/task-t" },
    chip: "Open",
  },
  {
    row: "canClaim on a paid task: same card action; the task page makes the readiness read",
    facts: { viewer: member, task: { ...task, paid: true } },
    expected: { label: "Pick up task", href: "/tasks/task-t" },
    chip: "Open",
  },
  {
    row: "otherwise (a follower): Reply as a public offer",
    facts: { viewer: follower, task },
    expected: { label: "Reply", href: null },
    chip: "Open",
  },
];

const hrefOf = (card: FeedKitCard) => {
  const action = primaryActionFor(card);
  return action.kind === "link" ? action.href : null;
};

describe("primaryActionFor", () => {
  it.each(Object.entries(EVERY_CONTRACT_TYPE))(
    "%s -> one action with the expected label and target",
    (postType, { facts, expected }) => {
      const card = feedCardFixture({ postType, ...facts });
      expect(primaryActionFor(card).label).toBe(expected.label);
      expect(hrefOf(card)).toBe(expected.href);
    },
  );

  it.each(HELP_WANTED_ROWS)(
    "HELP_WANTED, $row",
    ({ facts, expected, chip }) => {
      const card = feedCardFixture({ postType: "HELP_WANTED", ...facts });
      expect(primaryActionFor(card).label).toBe(expected.label);
      expect(hrefOf(card)).toBe(expected.href);
      expect(helpStateOf(card)?.label ?? null).toBe(chip);
    },
  );

  it("reads a PostType it does not know as a plain post", () => {
    const unknown = feedCardFixture({ postType: "BARN_DANCE_ANNOUNCED" });
    expect(primaryActionFor(unknown)).toEqual(
      primaryActionFor(feedCardFixture({ postType: "POST" })),
    );
  });

  it("sends an opportunity thread's Reply to the opportunity when only the thread names it", () => {
    const card = feedCardFixture({
      postType: "DISCUSSION",
      project: null,
      thread: {
        source: "OPPORTUNITY_COMMENT",
        commentId: "c-2",
        targetType: null,
        targetId: null,
        opportunityId: "opp-9",
      },
    });
    expect(hrefOf(card)).toBe("/opportunities/opp-9");
  });
});

describe("messageActionFor", () => {
  const ask = feedCardFixture({ postType: "HELP_WANTED", task });

  it.each([
    {
      row: "another reader on a help ask",
      card: ask,
      viewerId: "user-a",
      href: "/chats?with=user-b",
    },
    {
      row: "the asker on their own ask",
      card: ask,
      viewerId: "user-b",
      href: null,
    },
    { row: "a signed-out reader", card: ask, viewerId: null, href: null },
    {
      row: "a finished ask",
      card: feedCardFixture({
        postType: "HELP_WANTED",
        task: { ...task, state: "DONE" },
      }),
      viewerId: "user-a",
      href: null,
    },
    {
      row: "any card that is not a help ask",
      card: feedCardFixture(),
      viewerId: "user-a",
      href: null,
    },
  ])("$row -> $href", ({ card, viewerId, href }) => {
    expect(messageActionFor(card, viewerId)?.href ?? null).toBe(href);
  });
});
