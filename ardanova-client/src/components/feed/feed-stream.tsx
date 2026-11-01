"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

import { describeMutationError } from "~/components/ui/query-error-copy";
import { QueryErrorState } from "~/components/ui/query-error-state";
import { Button } from "~/components/ui/button";
import type { FeedScope } from "~/lib/contracts/feed-contract";
import { feedSubjectOf, mergeFeedPages, sectorsSeen } from "~/lib/feed/stream";
import type { FeedKitCard, FeedSubject } from "~/lib/feed/types";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

import { ColdStartRow } from "./cold-start-row";
import { FeedCard } from "./feed-card";
import { useInView } from "./use-in-view";

export interface FeedEndState {
  scope: FeedScope;
  cards: readonly FeedKitCard[];
  isEmpty: boolean;
}

export interface FeedStreamProps {
  scope?: FeedScope;
  pageSize?: number;
  /** The signed-in reader's id; null or omitted when signed out. */
  viewerId?: string | null;
  /** Accessible name of the feed region. */
  label?: string;
  /** Follow/unfollow a card's subject. Shown optimistically; a rejected promise reverts it. */
  onToggleFollow?: (
    subject: FeedSubject,
    follow: boolean,
  ) => Promise<unknown> | void;
  /** Follow state the page owns, by subject key (`feedSubjectKey`); wins over the stream's own optimistic state. */
  following?: Readonly<Record<string, boolean | undefined>>;
  /** Cards inserted locally above the stream (a just-published post); the stream's copy is skipped. */
  leadingCards?: readonly FeedKitCard[];
  /** What follows the last card once `nextCursor` is null. Defaults to `defaultFeedEnd`. */
  renderEnd?: (state: FeedEndState) => ReactNode;
  className?: string;
}

const NO_CARDS: readonly FeedKitCard[] = [];
const STAGGER_STEP_MS = 40;
const STAGGER_MAX_STEPS = 8;

/** Home ends in the cold-start row; a scoped stream ends silently, or with one line when empty. */
export function defaultFeedEnd({ scope, cards, isEmpty }: FeedEndState) {
  if (scope === "home") {
    return <ColdStartRow sectors={sectorsSeen(cards)} isEmpty={isEmpty} />;
  }
  return isEmpty ? (
    <p className="text-muted-foreground py-8 text-center text-sm">
      Nothing posted yet.
    </p>
  ) : null;
}

/**
 * The infinite ranked stream for one scope. Keeps fetching while `nextCursor` is non-null, even after a
 * short page; only `nextCursor: null` ends it. See components/feed/AGENTS.md §FeedStream.
 */
export function FeedStream({
  scope = "home",
  pageSize = 20,
  viewerId,
  label = "Feed",
  onToggleFollow,
  following,
  leadingCards = NO_CARDS,
  renderEnd = defaultFeedEnd,
  className,
}: FeedStreamProps) {
  const feed = api.feed.getPage.useInfiniteQuery(
    { limit: pageSize, scope },
    { getNextPageParam: (page) => page.nextCursor ?? undefined },
  );
  const {
    data,
    error,
    hasNextPage,
    isFetchingNextPage,
    isFetchNextPageError,
    fetchNextPage,
  } = feed;

  const cards = useMemo(
    () => mergeFeedPages<FeedKitCard>(leadingCards, data?.pages),
    [leadingCards, data?.pages],
  );

  // The sentinel stays mounted; a short page leaves it in view, so this re-fires when the page lands.
  const [sentinel, setSentinel] = useState<HTMLDivElement | null>(null);
  const sentinelInView = useInView(sentinel);
  // Not while any fetch runs: fetchNextPage cancels an in-flight refetch by default.
  const shouldLoadMore =
    sentinelInView && hasNextPage && !feed.isFetching && !isFetchNextPageError;
  const pagesLoaded = data?.pages.length ?? 0;

  useEffect(() => {
    if (shouldLoadMore) void fetchNextPage();
    // pagesLoaded: a fast page can land before a render ever commits isFetching=true, leaving
    // shouldLoadMore true throughout; the page count is what still changes.
  }, [shouldLoadMore, pagesLoaded, fetchNextPage]);

  const { followOverrides, followPending, toggleFollow } =
    useOptimisticFollow(onToggleFollow);

  const setSize = hasNextPage ? -1 : cards.length;
  const ended = data !== undefined && !hasNextPage;

  return (
    <div className={cn("flex min-w-0 flex-col", className)}>
      {feed.isPending ? <FeedSkeleton /> : null}

      {feed.isError && !data ? (
        <QueryErrorState
          title="Feed didn't load."
          error={error}
          onRetry={() => void feed.refetch()}
          retryLabel="Retry"
        />
      ) : null}

      {cards.length > 0 ? (
        <div
          role="feed"
          aria-label={label}
          aria-busy={feed.isFetching}
          onKeyDown={moveBetweenArticles}
          className="flex flex-col"
        >
          {cards.map((card, index) => {
            const subjectKey = feedSubjectOf(card)?.key;
            return (
              <FeedCard
                key={card.id}
                card={card}
                viewerId={viewerId}
                scope={scope}
                following={
                  subjectKey
                    ? (following?.[subjectKey] ?? followOverrides[subjectKey])
                    : undefined
                }
                followPending={
                  subjectKey ? followPending.has(subjectKey) : false
                }
                onToggleFollow={onToggleFollow ? toggleFollow : undefined}
                position={index + 1}
                setSize={setSize}
                className="motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-1 motion-safe:fill-mode-both -mt-px first:mt-0 motion-safe:duration-300"
                style={{
                  animationDelay: `${Math.min(index % pageSize, STAGGER_MAX_STEPS) * STAGGER_STEP_MS}ms`,
                }}
              />
            );
          })}
        </div>
      ) : null}

      <div ref={setSentinel} aria-hidden className="h-px" />

      {hasNextPage && !isFetchNextPageError ? (
        <div className="mt-4 flex justify-center">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            disabled={isFetchingNextPage}
            onClick={() => void fetchNextPage()}
          >
            {isFetchingNextPage ? (
              <>
                <Loader2 aria-hidden className="animate-spin" />
                Loading
              </>
            ) : (
              "Load more"
            )}
          </Button>
        </div>
      ) : null}

      {isFetchNextPageError ? (
        <QueryErrorState
          className="mt-4"
          title="Feed didn't load."
          error={error}
          onRetry={() => void fetchNextPage()}
          retryLabel="Retry"
        />
      ) : null}

      {ended ? (
        <div className="mt-6">
          {renderEnd({ scope, cards, isEmpty: cards.length === 0 })}
        </div>
      ) : null}
    </div>
  );
}

/** Per-subject optimistic Follow state, so every card of one project flips together and a failure reverts. */
function useOptimisticFollow(
  onToggleFollow: FeedStreamProps["onToggleFollow"],
) {
  const [followOverrides, setFollowOverrides] = useState<
    Record<string, boolean>
  >({});
  const [followPending, setFollowPending] = useState<ReadonlySet<string>>(
    () => new Set(),
  );

  const toggleFollow = useCallback(
    (subject: FeedSubject, follow: boolean) => {
      if (!onToggleFollow) return;
      const settle = (pending: boolean) =>
        setFollowPending((current) => {
          const next = new Set(current);
          if (pending) next.add(subject.key);
          else next.delete(subject.key);
          return next;
        });

      setFollowOverrides((current) => ({ ...current, [subject.key]: follow }));
      settle(true);
      void Promise.resolve()
        .then(() => onToggleFollow(subject, follow))
        .catch((error: unknown) => {
          setFollowOverrides((current) => ({
            ...current,
            [subject.key]: !follow,
          }));
          toast.error(
            describeMutationError(
              error,
              `${follow ? "follow" : "unfollow"} ${subject.name}`,
            ),
          );
        })
        .finally(() => settle(false));
    },
    [onToggleFollow],
  );

  return { followOverrides, followPending, toggleFollow };
}

/** ARIA feed pattern: Page Down / Page Up move focus between articles. */
function moveBetweenArticles(event: KeyboardEvent<HTMLDivElement>) {
  if (event.key !== "PageDown" && event.key !== "PageUp") return;
  const articles = Array.from(
    event.currentTarget.querySelectorAll<HTMLElement>(":scope > article"),
  );
  const current = articles.findIndex((article) =>
    article.contains(document.activeElement),
  );
  if (current < 0) return;
  const next = articles[current + (event.key === "PageDown" ? 1 : -1)];
  if (!next) return;
  event.preventDefault();
  next.focus();
}

function FeedSkeleton() {
  return (
    <div role="status" className="flex flex-col motion-safe:animate-pulse">
      <span className="sr-only">Loading</span>
      {[0, 1, 2].map((row) => (
        <div
          key={row}
          aria-hidden
          className="bg-card border-border -mt-px border px-5 py-4 first:mt-0"
        >
          <div className="flex items-center gap-2.5">
            <div className="bg-muted size-8" />
            <div className="bg-muted h-3 w-28" />
          </div>
          <div className="bg-muted mt-4 h-3 w-full" />
          <div className="bg-muted mt-2 h-3 w-4/5" />
        </div>
      ))}
    </div>
  );
}
