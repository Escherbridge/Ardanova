"use client";

import {
  Circle,
  Coins,
  ArrowDownCircle,
  AlertTriangle,
  CheckCircle2,
  Clock,
  GitMerge,
} from "lucide-react";
import { cn } from "~/lib/utils";

interface EscrowTimelineData {
  createdAt: Date | string;
  fundedAt?: Date | string | null;
  releasedAt?: Date | string | null;
  disputedAt?: Date | string | null;
  resolvedAt?: Date | string | null;
}

interface TimelineEvent {
  key: string;
  label: string;
  description: string;
  date: Date | string | null | undefined;
  icon: React.ComponentType<{ className?: string }>;
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
      label: "Escrow Created",
      description: "Escrow record initialised, awaiting funding.",
      date: escrow.createdAt,
      icon: Circle,
      color: "text-muted-foreground",
      dotColor: "bg-muted-foreground",
      lineColor: "bg-white/10",
    },
    {
      key: "funded",
      label: "Funded",
      description: "Funds deposited into escrow.",
      date: escrow.fundedAt,
      icon: Coins,
      color: "text-neon-cyan",
      dotColor: "bg-neon-cyan",
      lineColor: "bg-neon-cyan/30",
    },
    {
      key: "disputed",
      label: "Dispute Raised",
      description: "Funds frozen pending admin resolution.",
      date: escrow.disputedAt,
      icon: AlertTriangle,
      color: "text-neon-pink",
      dotColor: "bg-neon-pink",
      lineColor: "bg-neon-pink/30",
    },
    {
      key: "resolved",
      label: "Dispute Resolved",
      description: "Admin reviewed and resolved the dispute.",
      date: escrow.resolvedAt,
      icon: CheckCircle2,
      color: "text-neon-green",
      dotColor: "bg-neon-green",
      lineColor: "bg-neon-green/30",
    },
    {
      key: "released",
      label: "Funds Released",
      description: "Funds transferred to the contributor.",
      date: escrow.releasedAt,
      icon: ArrowDownCircle,
      color: "text-neon-green",
      dotColor: "bg-neon-green",
      lineColor: "bg-neon-green/30",
    },
  ];

  // Filter to only events that occurred (have a date), always include created
  const occurredEvents = allEvents.filter(
    (e) => e.key === "created" || (e.date != null)
  );

  // Sort by date
  const sortedEvents = [...occurredEvents].sort((a, b) => {
    if (!a.date) return -1;
    if (!b.date) return 1;
    return new Date(a.date).getTime() - new Date(b.date).getTime();
  });

  // Pending steps: events that haven't occurred yet, shown as upcoming
  const pendingSteps: TimelineEvent[] = allEvents.filter(
    (e) =>
      e.key !== "created" &&
      !e.date &&
      // Only show "funded" as pending if not disputed/released
      (e.key !== "funded" || (!escrow.fundedAt && !escrow.releasedAt && !escrow.disputedAt))
  );

  const displayEvents = [...sortedEvents];
  // Add a single "pending next step" indicator
  const nextPending = pendingSteps[0];

  return (
    <div className={cn("space-y-0", className)}>
      <p className="mb-4 text-xs font-medium uppercase tracking-widest text-muted-foreground">
        Timeline
      </p>

      <div className="relative">
        {displayEvents.map((event, idx) => {
          const Icon = event.icon;
          const isLast = idx === displayEvents.length - 1 && !nextPending;

          return (
            <div key={event.key} className="flex gap-3">
              {/* Dot + line column */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "flex h-7 w-7 shrink-0 items-center justify-center rounded-full border-2 border-background",
                    event.dotColor
                  )}
                >
                  <Icon className="h-3.5 w-3.5 text-background" />
                </div>
                {!isLast && (
                  <div
                    className={cn(
                      "w-0.5 flex-1 min-h-[1.5rem]",
                      event.lineColor
                    )}
                  />
                )}
              </div>

              {/* Content */}
              <div className={cn("pb-5", isLast && "pb-0")}>
                <p className={cn("text-sm font-semibold leading-7", event.color)}>
                  {event.label}
                </p>
                <p className="text-xs text-muted-foreground">
                  {event.description}
                </p>
                {event.date && (
                  <p className="mt-0.5 font-mono text-xs text-muted-foreground/70">
                    {formatDate(event.date)}
                  </p>
                )}
              </div>
            </div>
          );
        })}

        {/* Pending next step */}
        {nextPending && (
          <div className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full border-2 border-dashed border-white/20 bg-transparent">
                <Clock className="h-3.5 w-3.5 text-white/30" />
              </div>
            </div>
            <div>
              <p className="text-sm font-medium text-white/30">
                {nextPending.label}
              </p>
              <p className="text-xs text-muted-foreground/50">
                {nextPending.description}
              </p>
              <p className="mt-0.5 text-xs text-muted-foreground/30">Pending</p>
            </div>
          </div>
        )}

        {/* If everything is done */}
        {!nextPending && (
          <div className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-neon-green">
                <GitMerge className="h-3.5 w-3.5 text-background" />
              </div>
            </div>
            <div>
              <p className="text-sm font-semibold text-neon-green">Complete</p>
              <p className="text-xs text-muted-foreground">
                Escrow lifecycle finished.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
