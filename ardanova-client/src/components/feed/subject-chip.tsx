import Link from "next/link";
import { Check, Plus } from "lucide-react";

import type { FeedSubject } from "~/lib/feed/types";
import { cn } from "~/lib/utils";

export type FollowToggleHandler = (
  subject: FeedSubject,
  follow: boolean,
) => void;

/** The card's project or guild, with an optional Follow toggle (wired by the mounting lane). */
export function SubjectChip({
  subject,
  following,
  onToggleFollow,
  pending = false,
}: {
  subject: FeedSubject;
  following: boolean;
  /** Omit to render the chip without a toggle (signed out, or the reader is on the team). */
  onToggleFollow?: FollowToggleHandler;
  pending?: boolean;
}) {
  const label = following ? "Following" : "Follow";
  return (
    <span className="border-border inline-flex max-w-full min-w-0 items-stretch border text-xs">
      <Link
        href={subject.href}
        className="hover:bg-secondary flex min-h-8 min-w-0 items-center px-2 font-medium transition-colors"
      >
        <span className="truncate">{subject.name}</span>
      </Link>
      {onToggleFollow ? (
        <button
          type="button"
          disabled={pending}
          // The name starts with the visible label (WCAG 2.5.3) and says which subject.
          aria-label={`${label} ${subject.name}`}
          onClick={() => onToggleFollow(subject, !following)}
          className={cn(
            "border-border flex min-h-8 shrink-0 items-center gap-1 border-l px-2 font-mono text-[11px] font-semibold tracking-[0.06em] uppercase transition-colors disabled:opacity-60",
            following
              ? "bg-foreground text-background hover:bg-foreground/85"
              : "hover:bg-secondary",
          )}
        >
          {following ? (
            <Check aria-hidden className="size-3" />
          ) : (
            <Plus aria-hidden className="size-3" />
          )}
          {label}
        </button>
      ) : null}
    </span>
  );
}
