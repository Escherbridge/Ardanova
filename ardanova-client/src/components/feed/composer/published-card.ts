import type { Post } from "~/lib/api/ardanova/endpoints/posts";
import type { FeedCard } from "~/lib/contracts/feed-contract";

/** The just-published post as a feed card, for `FeedStream leadingCards`. See ./AGENTS.md §Published card. */

export interface ComposerAuthor {
  id: string;
  name?: string | null;
  image?: string | null;
}

export type CardProject = NonNullable<FeedCard["project"]>;
export type CardGuild = NonNullable<FeedCard["guild"]>;

export interface PublishedCardContext {
  author?: ComposerAuthor;
  project?: CardProject | null;
  guild?: CardGuild | null;
  /** equityReward > 0 on the asked task; omitted when unknown. */
  taskPaid?: boolean;
}

/** The PostDto fields the card reads; `hashtags` is the API's own extraction from the words. */
export type PublishedPost = Pick<
  Post,
  | "id"
  | "authorId"
  | "type"
  | "content"
  | "createdAt"
  | "likesCount"
  | "commentsCount"
  | "sharesCount"
> &
  Partial<Pick<Post, "projectId" | "guildId" | "title" | "taskId">> & {
    hashtags?: unknown;
  };

/**
 * A card built only from facts the write returned. `canClaim` is always false: the claim rule lives once, server-side
 * (TaskClaimRule), and the next feed read replaces this card with the server's.
 */
export function feedCardFromPost(
  post: PublishedPost,
  context: PublishedCardContext = {},
): FeedCard {
  const projectId = post.projectId ?? null;
  const guildId = post.guildId ?? null;
  const project =
    context.project && context.project.id === projectId
      ? context.project
      : null;
  const guild =
    context.guild && context.guild.id === guildId ? context.guild : null;

  return {
    id: post.id,
    postType: post.type,
    sourceType: null,
    occurredAt: post.createdAt,
    author: {
      id: post.authorId,
      name:
        context.author?.id === post.authorId
          ? (context.author.name ?? null)
          : null,
      image:
        context.author?.id === post.authorId
          ? (context.author.image ?? null)
          : null,
    },
    title: post.title ?? null,
    body: post.content,
    bodyIsUserWritten: true,
    hashtags: Array.isArray(post.hashtags)
      ? post.hashtags.filter((tag): tag is string => typeof tag === "string")
      : [],
    project,
    guild,
    opportunity: null,
    event: null,
    funding: null,
    thread: null,
    counters: {
      likes: post.likesCount,
      replies: post.commentsCount,
      shares: post.sharesCount,
    },
    // The API lets only members write into a project or guild, so an attached post's author is on its team.
    viewer: {
      isMember: projectId !== null || guildId !== null,
      isFollowing: false,
      canClaim: false,
    },
    isDiscovery: false,
    reasons: [],
    task:
      post.type === "HELP_WANTED" && post.taskId
        ? {
            id: post.taskId,
            state: "OPEN",
            assigneeName: null,
            ...(context.taskPaid === undefined
              ? {}
              : { paid: context.taskPaid }),
          }
        : null,
  };
}

/** A ProjectDto-shaped read as the card's project; sectors are the project's SECTOR categories. */
export function cardProjectOf(project: {
  id: string;
  title: string;
  slug: string;
  summary?: string | null;
  categories?: readonly string[];
}): CardProject {
  return {
    id: project.id,
    title: project.title,
    slug: project.slug,
    summary: project.summary ?? null,
    sectors: [...(project.categories ?? [])],
  };
}

/** A Guild read as the card's guild; null without a slug, because the guild link resolves by slug. */
export function cardGuildOf(guild: {
  id: string;
  name: string;
  slug?: string | null;
}): CardGuild | null {
  return guild.slug
    ? { id: guild.id, name: guild.name, slug: guild.slug }
    : null;
}
