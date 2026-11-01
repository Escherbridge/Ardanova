import type {
  FeedCard,
  FeedPostType,
  FeedTask,
} from "~/lib/contracts/feed-contract";

export type { FeedTaskStateCode } from "~/lib/contracts/feed-contract";

/** PostType values the feed kit maps: exactly the contract's `FEED_POST_TYPES` (HELP_WANTED included). */
export type KnownFeedPostType = FeedPostType;

/** A HELP_WANTED card's task facts: the contract's `feedTaskSchema`. Optional on every other card. */
export type FeedTaskFacts = FeedTask;

/**
 * A feed card as the kit reads it: exactly a parsed `feed.getPage` card (task facts and opportunity slug included).
 * See components/feed/AGENTS.md §Inputs.
 */
export type FeedKitCard = FeedCard;

/** The project or guild a card belongs to: what the subject chip names and Follow targets. */
export interface FeedSubject {
  type: "project" | "guild";
  id: string;
  name: string;
  href: string;
  /** Stable key for per-subject state, e.g. "project:abc". */
  key: string;
}
