"use client";

import { useCallback, useRef, useState } from "react";
import { useQueryClient, type InfiniteData } from "@tanstack/react-query";
import { getQueryKey } from "@trpc/react-query";
import { Check, Plus } from "lucide-react";
import { toast } from "sonner";

import {
  FeedStream,
  FieldNoteComposer,
  type ComposerAuthor,
} from "~/components/feed";
import { Button } from "~/components/ui/button";
import { describeMutationError } from "~/components/ui/query-error-copy";
import type { FeedCard } from "~/lib/contracts/feed-contract";
import { feedSubjectKey } from "~/lib/feed/stream";
import type { FeedSubject } from "~/lib/feed/types";
import { useSubjectFollow } from "~/lib/feed/use-subject-follow";
import { cn } from "~/lib/utils";
import { api, type RouterOutputs } from "~/trpc/react";

type FeedPage = RouterOutputs["feed"]["getPage"];

/** One project's follow state, shared by the header toggle and every card chip; see ./AGENTS.md §Follow. */
export interface ProjectFollow {
  /** Undefined until known, and for a reader who cannot follow (signed out, or on the team). */
  following: boolean | undefined;
  /** The latest change is not confirmed yet. */
  pending: boolean;
  /** The header's toggle. Rejects when the API refuses; the state is already restored. */
  toggleFromHeader: (follow: boolean) => Promise<void>;
  /** `FeedStream onToggleFollow`: this project goes through the shared state; any other subject straight to the API. */
  toggleFromCard: (subject: FeedSubject, follow: boolean) => Promise<unknown>;
}

/** Sets `viewer.isFollowing` on this project's cards in every cached feed page (Home included). */
function withProjectFollowing(
  data: InfiniteData<FeedPage> | undefined,
  projectId: string,
  isFollowing: boolean,
): InfiniteData<FeedPage> | undefined {
  if (!data) return data;
  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.map((card) =>
        card.project?.id === projectId
          ? { ...card, viewer: { ...card.viewer, isFollowing } }
          : card,
      ),
    })),
  };
}

/** The reader's follow state on `projectId`; `enabled` only for a signed-in reader known not to be on the team. */
export function useProjectFollow(
  projectId: string,
  enabled: boolean,
): ProjectFollow {
  const utilities = api.useUtils();
  const queryClient = useQueryClient();
  const confirmed = api.project.getMyFollowState.useQuery(
    { projectId },
    { enabled },
  );
  const followOtherSubject = useSubjectFollow();
  const [optimistic, setOptimistic] = useState<boolean | null>(null);
  const latestClick = useRef(0);
  const inFlight = useRef<Promise<unknown>>(Promise.resolve());

  // Serialized optimistic toggle: requests run in click order and only the latest click may restore state.
  const setFollowing = useCallback(
    (follow: boolean): Promise<void> => {
      const input = { projectId };
      const showOnCards = (isFollowing: boolean) =>
        queryClient.setQueriesData<InfiniteData<FeedPage>>(
          { queryKey: getQueryKey(api.feed.getPage, undefined, "infinite") },
          (data) => withProjectFollowing(data, projectId, isFollowing),
        );
      const click = ++latestClick.current;
      setOptimistic(follow);
      showOnCards(follow);

      const request = inFlight.current.then(() =>
        follow
          ? utilities.client.project.followProject.mutate(input)
          : utilities.client.project.unfollowProject.mutate(input),
      );
      inFlight.current = request.catch(() => undefined);

      return request.then(
        (answer) => {
          utilities.project.getMyFollowState.setData(input, answer);
          if (click === latestClick.current) setOptimistic(null);
        },
        (error: unknown) => {
          if (click === latestClick.current) {
            setOptimistic(null);
            showOnCards(
              utilities.project.getMyFollowState.getData(input)?.isFollowing ??
                !follow,
            );
          }
          throw error;
        },
      );
    },
    [projectId, queryClient, utilities],
  );

  const toggleFromCard = useCallback(
    (subject: FeedSubject, follow: boolean): Promise<unknown> =>
      subject.type === "project" && subject.id === projectId
        ? setFollowing(follow)
        : followOtherSubject(subject, follow),
    [projectId, setFollowing, followOtherSubject],
  );

  return {
    following: enabled
      ? (optimistic ?? confirmed.data?.isFollowing)
      : undefined,
    pending: optimistic !== null,
    toggleFromHeader: setFollowing,
    toggleFromCard,
  };
}

/** The header's Follow toggle, styled and labelled like the card chip's. Renders nothing until the state is known. */
export function ProjectFollowButton({
  follow,
  projectTitle,
}: {
  follow: ProjectFollow;
  projectTitle: string;
}) {
  const following = follow.following;
  if (following === undefined) return null;
  return (
    <Button
      type="button"
      size="sm"
      variant="outline"
      disabled={follow.pending}
      className={cn(
        "font-mono text-xs tracking-[0.06em] uppercase",
        following &&
          "border-foreground bg-foreground text-background hover:bg-foreground/85 hover:text-background",
      )}
      onClick={() =>
        void follow
          .toggleFromHeader(!following)
          .catch((error: unknown) =>
            toast.error(
              describeMutationError(
                error,
                `${following ? "unfollow" : "follow"} ${projectTitle}`,
              ),
            ),
          )
      }
    >
      {following ? <Check aria-hidden /> : <Plus aria-hidden />}
      {following ? "Following" : "Follow"}
    </Button>
  );
}

export interface ProjectFeedTabProps {
  projectId: string;
  /** The signed-in reader; the composer is offered only to the team (`canPost`), and the API refuses anyone else. */
  author: ComposerAuthor | null;
  canPost: boolean;
  /** Posts published on this page, newest first; the stream skips its own copy of each. */
  leadingCards: readonly FeedCard[];
  onPublished: (card: FeedCard) => void;
  follow: ProjectFollow;
}

/** The default tab: the composer for the team, then this project's stream, newest first. */
export function ProjectFeedTab({
  projectId,
  author,
  canPost,
  leadingCards,
  onPublished,
  follow,
}: ProjectFeedTabProps) {
  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6">
      {canPost && author ? (
        <FieldNoteComposer
          projectId={projectId}
          author={author}
          onPublished={onPublished}
        />
      ) : null}
      <FeedStream
        scope={`project:${projectId}`}
        label="Project feed"
        viewerId={author?.id ?? null}
        leadingCards={leadingCards}
        onToggleFollow={author ? follow.toggleFromCard : undefined}
        // The header and this project's chips read one state, so a header change after a chip tap shows on the chips.
        following={{ [feedSubjectKey("project", projectId)]: follow.following }}
      />
    </div>
  );
}
