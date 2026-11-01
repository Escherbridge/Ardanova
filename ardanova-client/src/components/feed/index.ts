export { FieldNoteComposer } from "./field-note-composer";
export {
  audienceDescription,
  audienceLabel,
  formatNoteAge,
  projectAudienceOptions,
  type FieldNoteAudience,
} from "./field-note-audience";

// SR-03 Phase 3 feed kit; see ./AGENTS.md.
export {
  FeedStream,
  defaultFeedEnd,
  type FeedEndState,
  type FeedStreamProps,
} from "./feed-stream";
export { FeedCard, type FeedCardProps } from "./feed-card";
export { ColdStartRow } from "./cold-start-row";
export { SubjectChip, type FollowToggleHandler } from "./subject-chip";

// SR-03 Phase 3 composer (Lane C); see ./composer/AGENTS.md.
export type { FieldNoteComposerProps } from "./field-note-composer";
export {
  feedCardFromPost,
  type ComposerAuthor,
  type PublishedPost,
} from "./composer/published-card";
export { useAttachableSubjects } from "./composer/use-composer-subjects";
export { audienceOptionsFor, type AudienceContext } from "./field-note-audience";
