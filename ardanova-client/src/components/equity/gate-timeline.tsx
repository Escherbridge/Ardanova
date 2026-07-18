"use client";

import { cn } from "~/lib/utils";
import { Check, X, Clock, Zap, Trophy } from "lucide-react";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface GateTimelineProps {
  gateStatus: GateStatus;
}

interface Step {
  id: GateStatus | "base";
  label: string;
  description: string;
  icon: React.ReactNode;
}

const steps: Step[] = [
  {
    id: "FUNDING",
    label: "FUNDING",
    description: "Funding gate",
    icon: <Clock className="size-4" aria-hidden="true" />,
  },
  {
    id: "ACTIVE",
    label: "ACTIVE",
    description: "Active project gate",
    icon: <Zap className="size-4" aria-hidden="true" />,
  },
  {
    id: "SUCCEEDED",
    label: "SUCCEEDED",
    description: "Success gate cleared",
    icon: <Trophy className="size-4" aria-hidden="true" />,
  },
];

type StepState = "completed" | "current" | "upcoming" | "failed";

function getStepState(stepId: string, gateStatus: GateStatus): StepState {
  if (gateStatus === "FAILED") {
    return "upcoming";
  }
  const order: GateStatus[] = ["FUNDING", "ACTIVE", "SUCCEEDED"];
  const currentIdx = order.indexOf(gateStatus);
  const stepIdx = order.indexOf(stepId as GateStatus);
  if (stepIdx < currentIdx) return "completed";
  if (stepIdx === currentIdx) return "current";
  return "upcoming";
}

const stateStyles: Record<
  StepState,
  { dot: string; text: string; connector: string }
> = {
  completed: {
    dot: "bg-success border-success text-success-foreground",
    text: "text-success",
    connector: "bg-success",
  },
  current: {
    dot: "bg-system border-system text-system-foreground",
    text: "text-system",
    connector: "bg-border",
  },
  upcoming: {
    dot: "bg-background border-border text-muted-foreground",
    text: "text-muted-foreground",
    connector: "bg-border",
  },
  failed: {
    dot: "bg-destructive border-destructive text-destructive-foreground",
    text: "text-destructive",
    connector: "bg-destructive",
  },
};

export default function GateTimeline({ gateStatus }: GateTimelineProps) {
  return (
    <div className="space-y-2">
      <span className="text-muted-foreground font-mono text-xs font-bold tracking-widest">
        PROJECT TIMELINE
      </span>

      <ol className="flex items-start gap-0">
        {steps.map((step, idx) => {
          const state = getStepState(step.id, gateStatus);
          const styles = stateStyles[state];
          const isLast = idx === steps.length - 1;

          return (
            <li
              key={step.id}
              className="flex flex-1 items-start"
              aria-current={state === "current" ? "step" : undefined}
            >
              {/* Step dot + label */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "flex size-8 shrink-0 items-center justify-center rounded-full border-2",
                    styles.dot,
                  )}
                >
                  {state === "completed" ? (
                    <Check className="size-4" aria-hidden="true" />
                  ) : state === "failed" ? (
                    <X className="size-4" aria-hidden="true" />
                  ) : (
                    step.icon
                  )}
                </div>
                <div className="mt-2 max-w-[80px] text-center">
                  <p
                    className={cn(
                      "font-mono text-[10px] font-bold tracking-wide",
                      styles.text,
                    )}
                  >
                    {step.label}
                  </p>
                  <p className="text-muted-foreground mt-0.5 text-[9px] leading-tight">
                    {step.description}
                  </p>
                </div>
              </div>

              {/* Connector line */}
              {!isLast && (
                <div className="mt-4 flex flex-1 items-center">
                  <div
                    className={cn(
                      "h-0.5 w-full transition-colors",
                      state === "completed" ? "bg-success" : "bg-border",
                    )}
                  />
                </div>
              )}
            </li>
          );
        })}
      </ol>

      {/* Failed branch */}
      {gateStatus === "FAILED" && (
        <div className="border-destructive bg-destructive/10 mt-4 border-2 p-3">
          <div className="flex items-center gap-2">
            <X className="text-destructive size-4" aria-hidden="true" />
            <span className="text-destructive font-mono text-xs font-bold tracking-wide">
              PROJECT GATE FAILED
            </span>
          </div>
          <p className="text-muted-foreground mt-1 text-xs">
            Failure is recorded. Token burns, protection processing, and any
            payout remain separate backend records; this status does not prove
            that any funds were returned.
          </p>
        </div>
      )}
    </div>
  );
}
