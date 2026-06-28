"use client";

import { ArrowRight } from "lucide-react";
import { Badge } from "~/components/ui/badge";

export type SwapStatus = "PENDING" | "PROCESSING" | "COMPLETED" | "FAILED" | "CANCELLED";

export interface SwapHistoryItem {
  id: string;
  sourceUnitName: string;
  sourceTokenAmount: number;
  targetUnitName: string;
  targetTokenAmount: number;
  sourceUsdValue?: number;
  targetUsdValue?: number;
  createdAt: string;
  status: SwapStatus;
}

export interface SwapHistoryProps {
  swaps: SwapHistoryItem[];
}

const statusVariant: Record<
  SwapStatus,
  "neon" | "neon-green" | "neon-pink" | "warning" | "outline" | "destructive"
> = {
  PENDING: "warning",
  PROCESSING: "neon",
  COMPLETED: "neon-green",
  FAILED: "destructive",
  CANCELLED: "outline",
};

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function SwapHistory({ swaps }: SwapHistoryProps) {
  if (swaps.length === 0) {
    return (
      <div className="rounded-none border-2 border-dashed border-border p-8 text-center">
        <ArrowRight className="mx-auto h-8 w-8 text-muted-foreground/30 mb-3" />
        <p className="font-mono text-sm text-muted-foreground">No swaps yet</p>
        <p className="font-mono text-xs text-muted-foreground/60 mt-1">
          Execute a swap above to exchange project tokens
        </p>
      </div>
    );
  }

  return (
    <div className="rounded-none border-2 border-border overflow-hidden">
      {/* Table header */}
      <div className="grid grid-cols-[1fr_auto_1fr_1fr_auto] gap-3 bg-muted/30 px-4 py-2 border-b border-border">
        <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          From
        </p>
        <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground" />
        <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          To
        </p>
        <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
          Date
        </p>
        <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground text-right">
          Status
        </p>
      </div>

      {/* Rows */}
      {swaps.map((swap, idx) => (
        <div
          key={swap.id}
          className={`grid grid-cols-[1fr_auto_1fr_1fr_auto] gap-3 px-4 py-3 items-center ${
            idx < swaps.length - 1 ? "border-b border-border" : ""
          }`}
        >
          {/* Source */}
          <div className="space-y-0.5 min-w-0">
            <p className="font-mono text-xs font-semibold text-neon-pink truncate">
              {swap.sourceTokenAmount.toLocaleString()} {swap.sourceUnitName}
            </p>
            {swap.sourceUsdValue !== undefined && (
              <p className="font-mono text-[10px] text-muted-foreground">
                {formatUsd(swap.sourceUsdValue)}
              </p>
            )}
          </div>

          {/* Arrow */}
          <ArrowRight className="h-3 w-3 text-muted-foreground/40 shrink-0" />

          {/* Target */}
          <div className="space-y-0.5 min-w-0">
            <p className="font-mono text-xs font-semibold text-neon-green truncate">
              {swap.targetTokenAmount.toLocaleString()} {swap.targetUnitName}
            </p>
            {swap.targetUsdValue !== undefined && (
              <p className="font-mono text-[10px] text-muted-foreground">
                {formatUsd(swap.targetUsdValue)}
              </p>
            )}
          </div>

          {/* Date */}
          <p className="font-mono text-xs text-muted-foreground">
            {formatDate(swap.createdAt)}
          </p>

          {/* Status */}
          <div className="flex justify-end">
            <Badge variant={statusVariant[swap.status]} size="sm">
              {swap.status}
            </Badge>
          </div>
        </div>
      ))}
    </div>
  );
}
