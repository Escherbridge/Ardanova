"use client";

import { api } from "~/trpc/react";
import { cn } from "~/lib/utils";
import { Loader2, TrendingUp } from "lucide-react";

interface TaskEquityBadgeProps {
  taskId: string;
  configId: string;
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

function formatTokens(n: number) {
  return new Intl.NumberFormat("en-US").format(Math.round(n));
}

export default function TaskEquityBadge({ taskId, configId }: TaskEquityBadgeProps) {
  const { data: allocationsRaw, isLoading } = api.projectTokens.getAllocationsByTask.useQuery(
    { taskId },
    { staleTime: 60_000 }
  );
  const allocations = allocationsRaw as unknown as { tokenAmount: number; equityPercentage: number }[] | undefined;

  if (isLoading) {
    return (
      <span className="inline-flex items-center gap-1 text-[10px] font-mono border border-border/40 px-1.5 py-0.5 text-muted-foreground">
        <Loader2 className="size-3 animate-spin" />
        equity
      </span>
    );
  }

  const allocation = allocations?.find((a) => a.tokenAmount > 0);

  if (!allocation) {
    return null;
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 text-[10px] font-mono border px-1.5 py-0.5",
        "border-[#00ff88]/40 bg-[#00ff88]/5 text-[#00ff88]",
      )}
      title={`${formatTokens(allocation.tokenAmount)} tokens (${formatPct(allocation.equityPercentage)} equity)`}
    >
      <TrendingUp className="size-3" />
      {formatPct(allocation.equityPercentage)}
    </span>
  );
}
