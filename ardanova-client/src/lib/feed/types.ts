import type { FeedCard, FeedPostType } from "~/lib/contracts/feed-contract";

/** PostType values the feed kit renders: the contract's list plus HELP_WANTED, which S3 adds. */
export type KnownFeedPostType = FeedPostType | "HELP_WANTED";

/** Server-computed state of a HELP_WANTED card's task; unknown codes read as neither done nor picked up. */
export type FeedTaskStateCode = "OPEN" | "PICKED_UP" | "DONE" | (string & {});

/** Task facts K2 adds to HELP_WANTED cards. Absent today, so every reader treats them as optional. */
export interface FeedTaskFacts {
  id: string;
  state: FeedTaskStateCode;
  assigneeName?: string | null;
  /** equityReward > 0: the task page makes the one readiness read; the card never does. */
  paid?: boolean;
}

/**
 * A feed card as the kit reads it: the live contract plus the optional facts later lanes add.
 * Every `FeedCard` from `feed.getPage` is assignable as-is. See components/feed/AGENTS.md §Inputs.
 */
export type FeedKitCard = Omit<FeedCard, "opportunity"> & {
  opportunity: { id: string; title: string; slug?: string | null } | null;
  task?: FeedTaskFacts | null;
};

/** The project or guild a card belongs to: what the subject chip names and Follow targets. */
export interface FeedSubject {
  type: "project" | "guild";
  id: string;
  name: string;
  href: string;
  /** Stable key for per-subject state, e.g. "project:abc". */
  key: string;
}
