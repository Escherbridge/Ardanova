"use client";

import { Progress } from "~/components/ui/progress";
import { cn } from "~/lib/utils";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface GateStatusBannerProps {
  gateStatus: GateStatus;
  fundingRaised: number;
  fundingGoal: number;
  gate1ClearedAt?: string | Date | null;
  gate2ClearedAt?: string | Date | null;
}

const statusConfig: Record<
  GateStatus,
  {
    color: string;
    bg: string;
    border: string;
    label: string;
    description: string;
  }
> = {
  FUNDING: {
    color: "text-system",
    bg: "bg-system/10",
    border: "border-system",
    label: "FUNDING",
    description: "The backend reports that the project is in its funding gate.",
  },
  ACTIVE: {
    color: "text-success",
    bg: "bg-success/10",
    border: "border-success",
    label: "ACTIVE",
    description: "The funding gate is cleared and the project gate is active.",
  },
  SUCCEEDED: {
    color: "text-success",
    bg: "bg-success/10",
    border: "border-success",
    label: "SUCCEEDED",
    description: "The backend reports that the success gate is cleared.",
  },
  FAILED: {
    color: "text-destructive",
    bg: "bg-destructive/10",
    border: "border-destructive",
    label: "FAILED",
    description:
      "The backend reports a failed gate. Downstream processing is separate.",
  },
};

function formatUSD(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

export default function GateStatusBanner({
  gateStatus,
  fundingRaised,
  fundingGoal,
  gate1ClearedAt,
  gate2ClearedAt,
}: GateStatusBannerProps) {
  const config = statusConfig[gateStatus];
  const pct =
    fundingGoal > 0 ? Math.min(100, (fundingRaised / fundingGoal) * 100) : 0;

  const progressVariant =
    gateStatus === "FUNDING"
      ? "neon"
      : gateStatus === "FAILED"
        ? "default"
        : "success";

  return (
    <div className={cn("space-y-3 border-2 p-4", config.bg, config.border)}>
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div className="flex flex-wrap items-center gap-3">
          <span
            className={cn(
              "font-mono text-xs font-bold tracking-widest",
              config.color,
            )}
          >
            GATE STATUS
          </span>
          <span
            className={cn(
              "inline-flex items-center border px-2.5 py-0.5 text-xs font-medium",
              config.bg,
              config.border,
              config.color,
            )}
          >
            {config.label}
          </span>
        </div>
        <span className="text-muted-foreground font-mono text-xs">
          {pct.toFixed(1)}% funded
        </span>
      </div>

      <Progress
        value={pct}
        variant={progressVariant}
        className="h-4"
        aria-label="Funding progress"
        aria-valuetext={`${pct.toFixed(1)}% funded: ${formatUSD(fundingRaised)} of ${formatUSD(fundingGoal)}`}
      />

      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className={cn("font-mono text-sm font-bold", config.color)}>
          {formatUSD(fundingRaised)}
        </span>
        <span className="text-muted-foreground font-mono text-sm">
          goal: {formatUSD(fundingGoal)}
        </span>
      </div>

      <p className="text-muted-foreground text-xs">{config.description}</p>

      {(gate1ClearedAt ?? gate2ClearedAt) && (
        <div className="flex flex-col gap-2 pt-1 sm:flex-row sm:gap-4">
          {gate1ClearedAt && (
            <div className="text-success text-xs">
              <span className="font-mono font-bold">Gate 1 cleared: </span>
              {new Date(gate1ClearedAt).toLocaleDateString()}
            </div>
          )}
          {gate2ClearedAt && (
            <div className="text-success text-xs">
              <span className="font-mono font-bold">Gate 2 cleared: </span>
              {new Date(gate2ClearedAt).toLocaleDateString()}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
