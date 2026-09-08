"use client";

import {
  Circle,
  Coins,
  ArrowDownCircle,
  RotateCcw,
  AlertTriangle,
  Clock,
} from "lucide-react";
import { cn } from "~/lib/utils";

import type { TaskEscrowStatus } from "~/lib/contracts/task-escrow-contract";

interface EscrowTimelineData {
  createdAt: Date | string;
  fundedAt?: Date | string | null;
  releasedAt?: Date | string | null;
  refundedAt?: Date | string | null;
  disputedAt?: Date | string | null;
  /** The escrow's current status. Used only to phrase "what's next" — every dated row above stays grounded in a real timestamp. */
  status?: TaskEscrowStatus;
}

interface TimelineEvent {
  key: string;
  label: string;
  description: string;
  date: Date | string | null | undefined;
  color: string;
  dotColor: string;
  lineColor: string;
}

function formatDate(date: Date | string | null | undefined): string {
  if (!date) return "";
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

interface EscrowTimelineProps {
  escrow: EscrowTimelineData;
  className?: string;
}

export function EscrowTimeline({ escrow, className }: EscrowTimelineProps) {
  const allEvents: TimelineEvent[] = [
    {
      key: "created",
      label: "Escrow record created",
      description:
        "The record exists; funding is not yet confirmed by this event.",
      date: escrow.createdAt,
      color: "text-muted-foreground",
      dotColor: "bg-muted-foreground",
      lineColor: "bg-white/10",
    },
    {
      key: "funded",
      label: "Funding recorded",
      description: "A funding event was recorded for this escrow.",
      date: escrow.fundedAt,
      color: "text-neon-cyan",
      dotColor: "bg-neon-cyan",
      lineColor: "bg-neon-cyan/30",
    },
    {
      key: "disputed",
      label: "Dispute Raised",
      description: "Units held while the dispute is reviewed.",
      date: escrow.disputedAt,
      color: "text-neon-pink",
      dotColor: "bg-neon-pink",
      lineColor: "bg-neon-pink/30",
    },
    {
      key: "released",
      label: "Released",
      description: "The held units were released to the contributor.",
      date: escrow.releasedAt,
      color: "text-neon-green",
      dotColor: "bg-neon-green",
      lineColor: "bg-neon-green/30",
    },
    {
      key: "refunded",
      label: escrow.disputedAt ? "Dispute resolved — refunded" : "Refunded",
      description: "The held units were returned to the reserve they were drawn from.",
      date: escrow.refundedAt,
      color: "text-neon-yellow",
      dotColor: "bg-neon-yellow",
      lineColor: "bg-neon-yellow/30",
    },
  ];

  // Filter to only events that occurred (have a date), always include created
  const occurredEvents = allEvents.filter(
    (e) => e.key === "created" || e.date != null,
  );

  // Sort by date
  const sortedEvents = [...occurredEvents].sort((a, b) => {
    if (!a.date) return -1;
    if (!b.date) return 1;
    return new Date(a.date).getTime() - new Date(b.date).getTime();
  });

  const displayEvents = [...sortedEvents];
  const nextPending = describeNextStep(escrow);

  return (
    <div className={cn("space-y-0", className)}>
      <p className="text-muted-foreground mb-4 text-xs font-medium tracking-widest uppercase">
        Timeline
      </p>

      <div className="relative">
        {displayEvents.map((event) => {
          return (
            <div key={event.key} className="flex gap-3">
              {/* Dot + line column */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "border-background flex h-7 w-7 shrink-0 items-center justify-center rounded-none border-2",
                    event.dotColor,
                  )}
                >
                  <TimelineIcon eventKey={event.key} />
                </div>
                <div
                  className={cn("min-h-[1.5rem] w-0.5 flex-1", event.lineColor)}
                />
              </div>

              {/* Content */}
              <div className="pb-5">
                <p
                  className={cn("text-sm leading-7 font-semibold", event.color)}
                >
                  {event.label}
                </p>
                <p className="text-muted-foreground text-xs">
                  {event.description}
                </p>
                {event.date && (
                  <p className="text-muted-foreground/70 mt-0.5 font-mono text-xs">
                    {formatDate(event.date)}
                  </p>
                )}
              </div>
            </div>
          );
        })}

        {/* Pending next step — omitted once the escrow reaches a terminal state */}
        {nextPending && (
          <div className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className="border-border flex h-7 w-7 shrink-0 items-center justify-center rounded-none border-2 border-dashed bg-transparent">
                <Clock className="text-muted-foreground h-3.5 w-3.5" />
              </div>
            </div>
            <div>
              <p className="text-muted-foreground text-sm font-medium">
                {nextPending.label}
              </p>
              <p className="text-muted-foreground text-xs">
                {nextPending.description}
              </p>
              <p className="text-muted-foreground mt-0.5 text-xs">Pending</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

/**
 * What's left, phrased from the escrow's current status — never from a fabricated timestamp.
 * Null once the escrow has reached a terminal state (RELEASED or REFUNDED): nothing further is
 * pending, so no dashed row is drawn.
 */
function describeNextStep(
  escrow: EscrowTimelineData,
): { label: string; description: string } | null {
  switch (escrow.status) {
    case "RELEASED":
    case "REFUNDED":
      return null;
    case "DISPUTED":
      return {
        label: "Resolution pending",
        description: "Units remain held while the dispute is reviewed.",
      };
    case "FUNDED":
      return escrow.disputedAt
        ? {
            label: "Dispute rejected — release decision pending",
            description:
              "The dispute was dismissed. Units remain reserved until an authorized release or a new dispute.",
          }
        : {
            label: "Release decision pending",
            description:
              "Units remain reserved until an authorized release, dispute, or refund.",
          };
    case "NONE":
      return {
        label: "Funding confirmation pending",
        description:
          "Record and verify the funding event before work is treated as funded.",
      };
    default:
      // Status not supplied by the caller: fall back to what the dates alone can tell us.
      return escrow.disputedAt
        ? {
            label: "Resolution pending",
            description: "Units remain held while the dispute is reviewed.",
          }
        : escrow.fundedAt
          ? {
              label: "Release decision pending",
              description:
                "Units remain reserved until an authorized release, dispute, or refund.",
            }
          : {
              label: "Funding confirmation pending",
              description:
                "Record and verify the funding event before work is treated as funded.",
            };
  }
}

function TimelineIcon({ eventKey }: { eventKey: string }) {
  const className = "text-background h-3.5 w-3.5";
  switch (eventKey) {
    case "funded":
      return <Coins className={className} aria-hidden="true" />;
    case "disputed":
      return <AlertTriangle className={className} aria-hidden="true" />;
    case "refunded":
      return <RotateCcw className={className} aria-hidden="true" />;
    case "released":
      return <ArrowDownCircle className={className} aria-hidden="true" />;
    case "created":
    default:
      return <Circle className={className} aria-hidden="true" />;
  }
}
