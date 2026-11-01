import type { KnownFeedPostType } from "./types";

/** How a card renders: the attachment it carries and the action family it offers. */
export type FeedCardKind =
  | "post"
  | "help"
  | "work"
  | "funding"
  | "opportunity"
  | "event"
  | "launch"
  | "discussion";

/** PostType -> renderer kind. `satisfies` makes a PostType added to the contract a compile error here. */
export const FEED_CARD_REGISTRY = {
  POST: "post",
  PROJECT_UPDATE: "post",
  GUILD_ACTIVITY: "post",
  GUILD_UPDATE: "post",
  GUILD_FOUNDED: "post",
  MEMBER_JOINED: "post",
  PROPOSAL: "post",
  SHOP_ITEM: "post",
  HELP_WANTED: "help",
  TASK_COMPLETED: "work",
  MILESTONE: "work",
  FUNDING_PROGRESS: "funding",
  OPPORTUNITY_OPENED: "opportunity",
  OPPORTUNITY_UPDATE: "opportunity",
  EVENT_SCHEDULED: "event",
  PROJECT_LAUNCHED: "launch",
  DISCUSSION: "discussion",
} as const satisfies Record<KnownFeedPostType, FeedCardKind>;

export const KNOWN_FEED_POST_TYPES = Object.keys(
  FEED_CARD_REGISTRY,
) as KnownFeedPostType[];

export function isKnownFeedPostType(code: string): code is KnownFeedPostType {
  return Object.hasOwn(FEED_CARD_REGISTRY, code);
}

/** Tolerant reader: a PostType this client does not know renders as a plain post. */
export function feedCardKindOf(postType: string): FeedCardKind {
  return isKnownFeedPostType(postType) ? FEED_CARD_REGISTRY[postType] : "post";
}
