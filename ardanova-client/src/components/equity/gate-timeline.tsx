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
    description: "Collecting investor contributions",
    icon: <Clock className="size-4" />,
  },
  {
    id: "ACTIVE",
    label: "ACTIVE",
    description: "Funded & project in progress",
    icon: <Zap className="size-4" />,
  },
  {
    id: "SUCCEEDED",
    label: "SUCCEEDED",
    description: "Project completed successfully",
    icon: <Trophy className="size-4" />,
  },
];

type StepState = "completed" | "current" | "upcoming" | "failed";

function getStepState(stepId: string, gateStatus: GateStatus): StepState {
  if (gateStatus === "FAILED") {
    if (stepId === "FUNDING") return "failed";
    return "upcoming";
  }
  const order: GateStatus[] = ["FUNDING", "ACTIVE", "SUCCEEDED"];
  const currentIdx = order.indexOf(gateStatus);
  const stepIdx = order.indexOf(stepId as GateStatus);
  if (stepIdx < currentIdx) return "completed";
  if (stepIdx === currentIdx) return "current";
  return "upcoming";
}

const stateStyles: Record<StepState, { dot: string; text: string; connector: string }> = {
  completed: {
    dot: "bg-[#00ff88] border-[#00ff88] text-background",
    text: "text-[#00ff88]",
    connector: "bg-[#00ff88]",
  },
  current: {
    dot: "bg-[#00d4ff] border-[#00d4ff] text-background shadow-[0_0_12px_rgba(0,212,255,0.6)]",
    text: "text-[#00d4ff]",
    connector: "bg-border",
  },
  upcoming: {
    dot: "bg-background border-border text-muted-foreground",
    text: "text-muted-foreground",
    connector: "bg-border",
  },
  failed: {
    dot: "bg-[#ff0080] border-[#ff0080] text-background shadow-[0_0_12px_rgba(255,0,128,0.6)]",
    text: "text-[#ff0080]",
    connector: "bg-[#ff0080]",
  },
};

export default function GateTimeline({ gateStatus }: GateTimelineProps) {
  return (
    <div className="space-y-2">
      <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground">
        PROJECT TIMELINE
      </span>

      <div className="flex items-start gap-0">
        {steps.map((step, idx) => {
          const state = getStepState(step.id, gateStatus);
          const styles = stateStyles[state];
          const isLast = idx === steps.length - 1;

          return (
            <div key={step.id} className="flex items-start flex-1">
              {/* Step dot + label */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "size-8 rounded-full border-2 flex items-center justify-center shrink-0",
                    styles.dot,
                  )}
                >
                  {state === "completed" ? (
                    <Check className="size-4" />
                  ) : state === "failed" ? (
                    <X className="size-4" />
                  ) : (
                    step.icon
                  )}
                </div>
                <div className="mt-2 text-center max-w-[80px]">
                  <p className={cn("font-mono text-[10px] font-bold tracking-wide", styles.text)}>
                    {step.label}
                  </p>
                  <p className="text-[9px] text-muted-foreground leading-tight mt-0.5">
                    {step.description}
                  </p>
                </div>
              </div>

              {/* Connector line */}
              {!isLast && (
                <div className="flex-1 flex items-center mt-4">
                  <div
                    className={cn(
                      "h-0.5 w-full transition-colors",
                      state === "completed" ? "bg-[#00ff88]" : "bg-border",
                    )}
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Failed branch */}
      {gateStatus === "FAILED" && (
        <div className="mt-4 border-2 border-[#ff0080]/40 bg-[#ff0080]/5 p-3">
          <div className="flex items-center gap-2">
            <X className="size-4 text-[#ff0080]" />
            <span className="font-mono text-xs font-bold text-[#ff0080] tracking-wide">
              FUNDING FAILED
            </span>
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            The project did not reach its funding goal. Investors will be refunded.
          </p>
        </div>
      )}
    </div>
  );
}
