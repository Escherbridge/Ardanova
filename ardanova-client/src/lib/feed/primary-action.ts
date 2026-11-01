import { feedCardKindOf } from "./registry";
import { feedRoutes } from "./routes";
import type { FeedKitCard } from "./types";

/** The only labels a card's primary action may carry (SR-03 Phase 3 plan §5 seeds). */
export type FeedActionLabel =
  | "Reply"
  | "Pick up task"
  | "Apply"
  | "Back this"
  | "Open board"
  | "See events";

/** Exactly one per card: an inline reply on the card, or a link elsewhere. */
export type FeedPrimaryAction =
  | { kind: "reply"; label: "Reply"; emphasis: "quiet" }
  | {
      kind: "link";
      label: FeedActionLabel;
      href: string;
      emphasis: "quiet" | "strong";
    };

export interface FeedSecondaryAction {
  label: "Message";
  href: string;
}

const INLINE_REPLY: FeedPrimaryAction = {
  kind: "reply",
  label: "Reply",
  emphasis: "quiet",
};

const link = (
  label: FeedActionLabel,
  href: string,
  emphasis: "quiet" | "strong" = "quiet",
): FeedPrimaryAction => ({ kind: "link", label, href, emphasis });

/**
 * The card's one primary action, from server-computed facts only (postType, viewer, task, links).
 * Never reads reason text. Missing facts fall back to Reply. See components/feed/AGENTS.md §One action.
 */
export function primaryActionFor(card: FeedKitCard): FeedPrimaryAction {
  const kind = feedCardKindOf(card.postType);
  switch (kind) {
    case "post":
    case "launch":
      return INLINE_REPLY;
    case "help":
      return helpWantedAction(card);
    case "work":
      return card.project
        ? link("Open board", feedRoutes.projectBoard(card.project.slug))
        : INLINE_REPLY;
    case "funding":
      return card.project
        ? link("Back this", feedRoutes.projectInvest(card.project.slug))
        : INLINE_REPLY;
    case "opportunity":
      return card.opportunity
        ? link("Apply", feedRoutes.opportunity(card.opportunity))
        : INLINE_REPLY;
    case "event":
      return link("See events", feedRoutes.events);
    case "discussion":
      return discussionAction(card);
    default:
      return unreachable(kind);
  }
}

/** HELP_WANTED routing (plan §2, Revision 2): first matching row wins. There is no Apply row. */
function helpWantedAction(card: FeedKitCard): FeedPrimaryAction {
  const task = card.task;
  if (!task) return INLINE_REPLY;
  if (task.state === "DONE") {
    return card.project
      ? link("Open board", feedRoutes.projectBoard(card.project.slug))
      : INLINE_REPLY;
  }
  if (task.state === "PICKED_UP") return INLINE_REPLY;
  // canClaim already encodes member + unassigned + not completed + no agreement + escrow NONE (K1's predicate).
  if (card.viewer.canClaim) {
    return link("Pick up task", feedRoutes.task(task.id), "strong");
  }
  return INLINE_REPLY;
}

/** A DISCUSSION card's replies live in its native thread; the API refuses a reply on the card itself. */
function discussionAction(card: FeedKitCard): FeedPrimaryAction {
  const thread = card.thread;
  if (thread?.source === "OPPORTUNITY_COMMENT") {
    const opportunity =
      card.opportunity ??
      (thread.opportunityId ? { id: thread.opportunityId } : null);
    if (opportunity) return link("Reply", feedRoutes.opportunity(opportunity));
  }
  if (card.project) {
    return link("Reply", feedRoutes.projectComments(card.project.slug));
  }
  return INLINE_REPLY;
}

/** "Message" beside a help ask: a draft DM to the asker. Never on your own ask or a finished one. */
export function messageActionFor(
  card: FeedKitCard,
  viewerId: string | null | undefined,
): FeedSecondaryAction | null {
  if (feedCardKindOf(card.postType) !== "help") return null;
  if (!viewerId || viewerId === card.author.id) return null;
  if (card.task?.state === "DONE") return null;
  return { label: "Message", href: feedRoutes.chatWith(card.author.id) };
}

/** The state chip on a help ask: Open / Picked up by X / Done; null without task facts. */
export function helpStateOf(card: FeedKitCard): {
  tone: "open" | "picked-up" | "done";
  label: string;
} | null {
  const task = card.task;
  if (!task) return null;
  switch (task.state) {
    case "OPEN":
      return { tone: "open", label: "Open" };
    case "PICKED_UP":
      return {
        tone: "picked-up",
        label: task.assigneeName
          ? `Picked up by ${task.assigneeName}`
          : "Picked up",
      };
    case "DONE":
      return { tone: "done", label: "Done" };
    default:
      return null;
  }
}

function unreachable(value: never): never {
  throw new Error(`Unhandled feed card kind: ${String(value)}`);
}
