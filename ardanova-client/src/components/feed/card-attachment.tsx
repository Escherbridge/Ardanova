import type { ReactNode } from "react";
import { CalendarDays } from "lucide-react";

import { helpStateOf } from "~/lib/feed/primary-action";
import type { FeedCardKind } from "~/lib/feed/registry";
import type { FeedKitCard } from "~/lib/feed/types";
import { cn } from "~/lib/utils";

type AttachmentRenderer = (card: FeedKitCard) => ReactNode;

/** Renderer side of lib/feed/registry: the one extra block each card kind carries under the body. */
const ATTACHMENTS: Record<FeedCardKind, AttachmentRenderer> = {
  post: () => null,
  work: () => null,
  opportunity: () => null,
  discussion: () => null,
  help: (card) => <HelpState card={card} />,
  funding: (card) =>
    card.funding ? <FundingMark percent={card.funding.percent} /> : null,
  event: (card) =>
    card.event ? <EventWhen startsAt={card.event.startsAt} /> : null,
  launch: (card) =>
    card.project?.summary ? (
      <p className="text-muted-foreground mt-2 line-clamp-2 text-sm leading-relaxed">
        {card.project.summary}
      </p>
    ) : null,
};

export function CardAttachment({
  kind,
  card,
}: {
  kind: FeedCardKind;
  card: FeedKitCard;
}) {
  return <>{ATTACHMENTS[kind](card)}</>;
}

const HELP_TONES = {
  open: "border-system text-system",
  "picked-up": "border-border text-muted-foreground",
  done: "border-success text-success",
} as const;

function HelpState({ card }: { card: FeedKitCard }) {
  const state = helpStateOf(card);
  if (!state) return null;
  return (
    <p className="mt-3">
      <span
        className={cn(
          "inline-flex items-center border px-2 py-0.5 font-mono text-[11px] font-semibold tracking-[0.06em] uppercase",
          HELP_TONES[state.tone],
        )}
      >
        {state.label}
      </span>
    </p>
  );
}

/** Aggregate progress only: the API never sends an amount or a backer, and neither does this. */
function FundingMark({ percent }: { percent: number }) {
  const shown = Math.max(0, Math.min(100, Math.round(percent)));
  return (
    <div className="mt-3 flex items-center gap-3">
      <div
        role="progressbar"
        aria-label="Funded"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={shown}
        className="bg-secondary border-border h-1.5 flex-1 border"
      >
        <div className="bg-success h-full" style={{ width: `${shown}%` }} />
      </div>
      <span className="font-mono text-xs font-semibold">{shown}%</span>
    </div>
  );
}

function EventWhen({ startsAt }: { startsAt: string }) {
  const when = new Date(startsAt);
  if (Number.isNaN(when.getTime())) return null;
  return (
    <p className="mt-2 flex items-center gap-1.5 font-mono text-xs">
      <CalendarDays aria-hidden className="size-3.5" />
      <time dateTime={startsAt} suppressHydrationWarning>
        {when.toLocaleString(undefined, {
          weekday: "short",
          day: "numeric",
          month: "short",
          hour: "numeric",
          minute: "2-digit",
        })}
      </time>
    </p>
  );
}
