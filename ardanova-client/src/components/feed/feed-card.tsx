"use client";

import { useId, useState, type CSSProperties } from "react";
import Link from "next/link";
import { CornerDownRight, Heart, MessageSquare } from "lucide-react";

import { formatNoteAge } from "~/components/feed/field-note-audience";
import { HashtagList, HashtagText, hashtagsIn } from "~/components/hashtags";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button";
import type { FeedScope } from "~/lib/contracts/feed-contract";
import {
  messageActionFor,
  primaryActionFor,
  type FeedPrimaryAction,
} from "~/lib/feed/primary-action";
import { reasonFragment } from "~/lib/feed/reason";
import { feedCardKindOf } from "~/lib/feed/registry";
import { feedRoutes } from "~/lib/feed/routes";
import { feedSubjectOf } from "~/lib/feed/stream";
import type { FeedKitCard } from "~/lib/feed/types";
import { cn } from "~/lib/utils";

import { CardAttachment } from "./card-attachment";
import { InlineReply } from "./inline-reply";
import { SubjectChip, type FollowToggleHandler } from "./subject-chip";

export interface FeedCardProps {
  card: FeedKitCard;
  /** The signed-in reader; enables "Message" on help asks and labels their own replies. */
  viewerId?: string | null;
  /** The stream's scope, so a reason that restates the page ("From Barn raising" on Barn raising) is dropped. */
  scope?: FeedScope;
  /** Overrides `card.viewer.isFollowing` (FeedStream passes its optimistic state). */
  following?: boolean;
  /** Renders the Follow toggle on the subject chip; omitted for team members regardless. */
  onToggleFollow?: FollowToggleHandler;
  followPending?: boolean;
  /** 1-based position and total for the ARIA feed pattern; total -1 while more may load. */
  position?: number;
  setSize?: number;
  style?: CSSProperties;
  className?: string;
}

/** One polymorphic feed card: the person's words first, near-silent chrome, exactly one primary action. */
export function FeedCard({
  card,
  viewerId,
  scope,
  following,
  onToggleFollow,
  followPending,
  position,
  setSize,
  style,
  className,
}: FeedCardProps) {
  const baseId = useId();
  const [replyOpen, setReplyOpen] = useState(false);
  const [repliesSent, setRepliesSent] = useState(0);

  const kind = feedCardKindOf(card.postType);
  const action = primaryActionFor(card);
  const message = messageActionFor(card, viewerId);
  const reason = reasonFragment(card, scope);
  const subject = feedSubjectOf(card);
  const isFollowing = following ?? card.viewer.isFollowing;

  const authorName = card.author.name?.trim() || "Someone";
  const words = card.bodyIsUserWritten;
  // Chips only for tags the visible copy does not already link inline.
  const linkedInline = words
    ? hashtagsIn(`${card.title ?? ""}\n${card.body}`)
    : new Set<string>();
  const extraTags = card.hashtags.filter((tag) => !linkedInline.has(tag));
  const replies = card.counters.replies + repliesSent;

  const ids = {
    author: `${baseId}-author`,
    time: `${baseId}-time`,
    body: `${baseId}-body`,
    replies: `${baseId}-replies`,
  };

  return (
    <article
      aria-labelledby={`${ids.author} ${ids.time}`}
      aria-describedby={ids.body}
      aria-posinset={position}
      aria-setsize={setSize}
      tabIndex={-1}
      data-post-type={card.postType}
      style={style}
      className={cn(
        "bg-card border-border focus-visible:outline-ring relative min-w-0 border px-4 py-4 focus-visible:outline-2 focus-visible:-outline-offset-2 sm:px-5",
        kind === "help" && "border-l-primary border-l-[3px]",
        className,
      )}
    >
      <header className="flex flex-wrap items-start justify-between gap-x-3 gap-y-2">
        <div className="flex min-w-0 items-center gap-2.5">
          <Avatar className="border-border size-8 border">
            {card.author.image ? (
              <AvatarImage src={card.author.image} alt="" />
            ) : null}
            <AvatarFallback className="font-mono text-[11px] font-semibold">
              {initialsOf(authorName)}
            </AvatarFallback>
          </Avatar>
          <div className="min-w-0 leading-tight">
            <Link
              id={ids.author}
              href={feedRoutes.person(card.author.id)}
              className="block truncate text-sm font-semibold hover:underline"
            >
              {authorName}
            </Link>
            <time
              id={ids.time}
              dateTime={card.occurredAt}
              suppressHydrationWarning
              className="text-muted-foreground font-mono text-[11px]"
            >
              {formatNoteAge(card.occurredAt)}
            </time>
          </div>
        </div>
        {subject ? (
          <SubjectChip
            subject={subject}
            following={isFollowing}
            onToggleFollow={card.viewer.isMember ? undefined : onToggleFollow}
            pending={followPending}
          />
        ) : null}
      </header>

      {words ? (
        <>
          {card.title ? (
            <h3 className="mt-3 text-base leading-snug font-semibold">
              <HashtagText text={card.title} />
            </h3>
          ) : null}
          <div
            id={ids.body}
            className={cn(
              "text-[15px] leading-relaxed break-words whitespace-pre-wrap",
              card.title ? "mt-1.5" : "mt-3",
            )}
          >
            <HashtagText text={card.body} />
          </div>
        </>
      ) : (
        <div id={ids.body} className="mt-3">
          <p className="text-muted-foreground text-sm">{card.body}</p>
          {card.title ? (
            <p className="mt-0.5 text-base leading-snug font-semibold">
              {card.title}
            </p>
          ) : null}
        </div>
      )}

      <CardAttachment kind={kind} card={card} />

      <HashtagList tags={extraTags} className="mt-3" />

      {reason ? (
        <p className="text-muted-foreground mt-3 flex items-center gap-1.5 font-mono text-[11px]">
          <CornerDownRight aria-hidden className="size-3 shrink-0" />
          <span className="truncate">{reason}</span>
        </p>
      ) : null}

      <footer className="mt-3 flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
        <Counters likes={card.counters.likes} replies={replies} />
        <div className="flex items-center gap-2">
          {message ? (
            <Button asChild variant="ghost" size="sm">
              {/* The name starts with the visible label (WCAG 2.5.3). */}
              <Link
                href={message.href}
                aria-label={`${message.label} ${authorName}`}
              >
                {message.label}
              </Link>
            </Button>
          ) : null}
          <PrimaryAction
            action={action}
            replyOpen={replyOpen}
            repliesId={ids.replies}
            onToggleReply={() => setReplyOpen((open) => !open)}
          />
        </div>
      </footer>

      {replyOpen && action.kind === "reply" ? (
        <InlineReply
          postId={card.id}
          panelId={ids.replies}
          placeholder={
            kind === "help" ? "How can you help?" : "What do you think?"
          }
          viewerId={viewerId}
          askerId={card.author.id}
          askerName={card.author.name}
          onSent={() => setRepliesSent((count) => count + 1)}
        />
      ) : null}
    </article>
  );
}

/** The single primary action; `data-primary-action` marks it so tests can prove there is exactly one. */
function PrimaryAction({
  action,
  replyOpen,
  repliesId,
  onToggleReply,
}: {
  action: FeedPrimaryAction;
  replyOpen: boolean;
  repliesId: string;
  onToggleReply: () => void;
}) {
  if (action.kind === "reply") {
    return (
      <Button
        type="button"
        size="sm"
        variant="outline"
        data-primary-action=""
        aria-expanded={replyOpen}
        aria-controls={replyOpen ? repliesId : undefined}
        onClick={onToggleReply}
      >
        {action.label}
      </Button>
    );
  }
  return (
    <Button
      asChild
      size="sm"
      variant={action.emphasis === "strong" ? "default" : "outline"}
    >
      <Link href={action.href} data-primary-action="">
        {action.label}
      </Link>
    </Button>
  );
}

/** Raw counters, shown small and only when non-zero; never ranked on or styled as a quality signal. */
function Counters({ likes, replies }: { likes: number; replies: number }) {
  return (
    <p className="text-muted-foreground flex items-center gap-3 font-mono text-[11px]">
      {likes > 0 ? (
        <span className="inline-flex items-center gap-1">
          <Heart aria-hidden className="size-3" />
          {likes}{" "}
          <span className="sr-only">{likes === 1 ? "like" : "likes"}</span>
        </span>
      ) : null}
      {replies > 0 ? (
        <span className="inline-flex items-center gap-1">
          <MessageSquare aria-hidden className="size-3" />
          {replies}{" "}
          <span className="sr-only">{replies === 1 ? "reply" : "replies"}</span>
        </span>
      ) : null}
    </p>
  );
}

function initialsOf(name: string): string {
  const parts = name.split(/\s+/).filter(Boolean);
  const letters =
    parts.length > 1 ? [parts[0], parts[parts.length - 1]] : parts;
  return (
    letters.map((part) => part?.charAt(0).toUpperCase() ?? "").join("") || "?"
  );
}
