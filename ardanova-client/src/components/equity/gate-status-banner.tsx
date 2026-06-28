"use client";

import { Progress } from "~/components/ui/progress";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface GateStatusBannerProps {
  gateStatus: GateStatus;
  fundingRaised: number;
  fundingGoal: number;
  gate1ClearedAt?: string | Date | null;
  gate2ClearedAt?: string | Date | null;
}

const statusConfig: Record<GateStatus, { color: string; bg: string; border: string; label: string; description: string }> = {
  FUNDING: {
    color: "text-[#00d4ff]",
    bg: "bg-[#00d4ff]/10",
    border: "border-[#00d4ff]/40",
    label: "FUNDING",
    description: "Accepting investor contributions",
  },
  ACTIVE: {
    color: "text-[#00ff88]",
    bg: "bg-[#00ff88]/10",
    border: "border-[#00ff88]/40",
    label: "ACTIVE",
    description: "Project is live and funded",
  },
  SUCCEEDED: {
    color: "text-[#00ff88]",
    bg: "bg-[#00ff88]/10",
    border: "border-[#00ff88]/40",
    label: "SUCCEEDED",
    description: "Project reached its funding goal",
  },
  FAILED: {
    color: "text-[#ff0080]",
    bg: "bg-[#ff0080]/10",
    border: "border-[#ff0080]/40",
    label: "FAILED",
    description: "Funding goal was not reached",
  },
};

function formatUSD(amount: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(amount);
}

export default function GateStatusBanner({
  gateStatus,
  fundingRaised,
  fundingGoal,
  gate1ClearedAt,
  gate2ClearedAt,
}: GateStatusBannerProps) {
  const config = statusConfig[gateStatus];
  const pct = fundingGoal > 0 ? Math.min(100, (fundingRaised / fundingGoal) * 100) : 0;

  const progressVariant =
    gateStatus === "FUNDING" ? "neon" :
    gateStatus === "FAILED" ? "default" :
    "success";

  return (
    <div className={cn("border-2 p-4 space-y-3", config.bg, config.border)}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className={cn("font-mono text-xs font-bold tracking-widest", config.color)}>
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
        <span className="font-mono text-xs text-muted-foreground">
          {pct.toFixed(1)}% funded
        </span>
      </div>

      <Progress
        value={pct}
        variant={progressVariant}
        className="h-4"
      />

      <div className="flex items-center justify-between">
        <span className={cn("font-mono text-sm font-bold", config.color)}>
          {formatUSD(fundingRaised)}
        </span>
        <span className="font-mono text-sm text-muted-foreground">
          goal: {formatUSD(fundingGoal)}
        </span>
      </div>

      <p className="text-xs text-muted-foreground">{config.description}</p>

      {(gate1ClearedAt ?? gate2ClearedAt) && (
        <div className="flex gap-4 pt-1">
          {gate1ClearedAt && (
            <div className="text-xs text-[#00ff88]">
              <span className="font-mono font-bold">Gate 1 cleared: </span>
              {new Date(gate1ClearedAt).toLocaleDateString()}
            </div>
          )}
          {gate2ClearedAt && (
            <div className="text-xs text-[#00ff88]">
              <span className="font-mono font-bold">Gate 2 cleared: </span>
              {new Date(gate2ClearedAt).toLocaleDateString()}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
