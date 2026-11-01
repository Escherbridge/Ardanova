import { z } from "zod";

/** PostType values this client knows; the API adds envelope kinds over time (see `feedPostTypeCodeSchema`). */
export const FEED_POST_TYPES = [
  "POST",
  "PROJECT_UPDATE",
  "GUILD_ACTIVITY",
  "TASK_COMPLETED",
  "MILESTONE",
  "PROPOSAL",
  "SHOP_ITEM",
  "PROJECT_LAUNCHED",
  "FUNDING_PROGRESS",
  "OPPORTUNITY_OPENED",
  "OPPORTUNITY_UPDATE",
  "GUILD_FOUNDED",
  "GUILD_UPDATE",
  "EVENT_SCHEDULED",
  "DISCUSSION",
  "MEMBER_JOINED",
] as const;

/** FeedReasonKind values this client knows; render unknown ones by their `text`. */
export const FEED_REASON_KINDS = [
  "FollowedProject",
  "ProjectTeam",
  "FollowedGuild",
  "GuildMember",
  "FollowedPerson",
  "YourActivity",
  "TagInterest",
  "YourSector",
  "NewInSector",
  "NewProject",
  "Discovery",
  "InScope",
] as const;

export type FeedPostType = (typeof FEED_POST_TYPES)[number];
/** A card's PostType as read from the API: a known value, or one a newer API added since. */
export type FeedPostTypeCode = FeedPostType | (string & {});
/** Read-side PostType: any non-empty code. See src/lib/AGENTS.md §Tolerant enum reads. */
export const feedPostTypeCodeSchema: z.ZodType<FeedPostTypeCode> = z
  .string()
  .min(1);

export type FeedReasonKind = (typeof FEED_REASON_KINDS)[number];
export type FeedReasonKindCode = FeedReasonKind | (string & {});
export const feedReasonKindCodeSchema: z.ZodType<FeedReasonKindCode> = z
  .string()
  .min(1);

/** The feed scopes the API accepts; the tag is normalised server-side ("#Carpentry" -> "carpentry"). */
export type FeedScope =
  | "home"
  | `project:${string}`
  | `guild:${string}`
  | `tag:${string}`;

export const feedScopeSchema: z.ZodType<FeedScope> = z
  .string()
  .regex(/^(home|(project|guild|tag):.+)$/) as z.ZodType<FeedScope>;

const dotNetDateTimeSchema = z.string().datetime({ offset: true, local: true });

export const feedReasonSchema = z.object({
  kind: feedReasonKindCodeSchema,
  text: z.string(),
  about: z.string().nullable(),
  aboutId: z.string().nullable(),
});

/** A .NET FeedCardDto. Unknown `postType`/`sourceType`/reason kinds and extra fields parse rather than fail. */
export const feedCardSchema = z.object({
  id: z.string().min(1),
  postType: feedPostTypeCodeSchema,
  sourceType: z.string().nullable(),
  occurredAt: dotNetDateTimeSchema,
  author: z.object({
    id: z.string().min(1),
    name: z.string().nullable(),
    image: z.string().nullable(),
  }),
  title: z.string().nullable(),
  body: z.string(),
  bodyIsUserWritten: z.boolean(),
  hashtags: z.array(z.string()),
  project: z
    .object({
      id: z.string(),
      title: z.string(),
      slug: z.string(),
      summary: z.string().nullable(),
      sectors: z.array(z.string()),
    })
    .nullable(),
  guild: z
    .object({ id: z.string(), name: z.string(), slug: z.string() })
    .nullable(),
  opportunity: z.object({ id: z.string(), title: z.string() }).nullable(),
  event: z
    .object({ id: z.string(), title: z.string(), startsAt: dotNetDateTimeSchema })
    .nullable(),
  /** Aggregate progress only: the API never sends an amount or a backer. */
  funding: z.object({ percent: z.number().int() }).nullable(),
  thread: z
    .object({
      source: z.string(),
      commentId: z.string(),
      targetType: z.string().nullable(),
      targetId: z.string().nullable(),
      opportunityId: z.string().nullable(),
    })
    .nullable(),
  counters: z.object({
    likes: z.number().int(),
    replies: z.number().int(),
    shares: z.number().int(),
  }),
  /** The reader's standing, computed server-side: pick the primary action from this, never from reason text. */
  viewer: z.object({
    isMember: z.boolean(),
    isFollowing: z.boolean(),
    canClaim: z.boolean(),
  }),
  isDiscovery: z.boolean(),
  reasons: z.array(feedReasonSchema),
});

export const feedPageSchema = z.object({
  items: z.array(feedCardSchema),
  nextCursor: z.string().nullable(),
});

export type FeedReason = z.infer<typeof feedReasonSchema>;
export type FeedCard = z.infer<typeof feedCardSchema>;
export type FeedPage = z.infer<typeof feedPageSchema>;

export function isFeedPostType(value: string): value is FeedPostType {
  return (FEED_POST_TYPES as readonly string[]).includes(value);
}
