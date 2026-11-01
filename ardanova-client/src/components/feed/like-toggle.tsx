"use client";

import { useState } from "react";
import { Heart } from "lucide-react";
import { toast } from "sonner";

import { describeMutationError } from "~/components/ui/query-error-copy";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

/** The like count as the card shows it: small, mono, only when non-zero, never styled as a score. */
export function LikeCount({ likes }: { likes: number }) {
  if (likes <= 0) return null;
  return (
    <span className="inline-flex items-center gap-1">
      <Heart aria-hidden className="size-3" />
      {likes}{" "}
      <span className="sr-only">{likes === 1 ? "like" : "likes"}</span>
    </span>
  );
}

/**
 * A card's quiet approval toggle (SR-03-D3): optimistic, rolled back on failure, and the server's count wins on
 * success. Not a primary action. See components/feed/AGENTS.md §Like toggle.
 */
export function LikeToggle({
  postId,
  likes,
  subjectName,
}: {
  postId: string;
  likes: number;
  /** Who or what is being liked, appended to the accessible name after the visible label. */
  subjectName: string;
}) {
  // null until the reader taps: the card has no "you liked this" fact, so it starts unpressed at the card's count.
  const [settled, setSettled] = useState<{
    liked: boolean;
    likes: number;
  } | null>(null);
  const [pending, setPending] = useState(false);
  const like = api.post.like.useMutation();
  const unlike = api.post.unlike.useMutation();

  const liked = settled?.liked ?? false;
  const shown = settled?.likes ?? likes;

  const toggle = () => {
    // One request at a time: a like and an unlike racing could land in either order.
    if (pending) return;
    const before = { liked, likes: shown };
    const next = !liked;
    setSettled({ liked: next, likes: Math.max(0, shown + (next ? 1 : -1)) });
    setPending(true);
    (next ? like : unlike)
      .mutateAsync({ id: postId })
      .then((post) => setSettled({ liked: next, likes: post.likesCount }))
      .catch((error: unknown) => {
        setSettled(before);
        toast.error(
          describeMutationError(
            error,
            next ? "like this" : "remove your like",
          ),
        );
      })
      .finally(() => setPending(false));
  };

  return (
    <span className="inline-flex items-center gap-2">
      <button
        type="button"
        aria-pressed={liked}
        aria-busy={pending || undefined}
        // The name starts with the visible label (WCAG 2.5.3); the label stays "Like" and aria-pressed carries state.
        aria-label={`Like ${subjectName}`}
        onClick={toggle}
        className={cn(
          "text-muted-foreground hover:text-foreground inline-flex min-h-8 items-center gap-1 px-1 font-mono text-[11px] font-semibold tracking-[0.06em] uppercase transition-colors",
          liked && "text-foreground",
        )}
      >
        <Heart
          aria-hidden
          className={cn("size-3", liked && "fill-current")}
        />
        Like
      </button>
      <LikeCount likes={shown} />
    </span>
  );
}
