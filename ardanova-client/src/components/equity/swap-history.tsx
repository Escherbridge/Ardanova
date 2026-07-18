"use client";

import { ArrowRight } from "lucide-react";
import { Badge } from "~/components/ui/badge";

export type SwapStatus =
  | "PENDING"
  | "PROCESSING"
  | "COMPLETED"
  | "FAILED"
  | "CANCELLED";

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

const statusLabel: Record<SwapStatus, string> = {
  PENDING: "Request received",
  PROCESSING: "Processing",
  COMPLETED: "Reconciled",
  FAILED: "Failed",
  CANCELLED: "Cancelled",
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
      <div className="border-border rounded-none border-2 border-dashed p-8 text-center">
        <ArrowRight className="text-muted-foreground/30 mx-auto mb-3 h-8 w-8" />
        <p className="text-muted-foreground font-mono text-sm">No swaps yet</p>
        <p className="text-muted-foreground/60 mt-1 font-mono text-xs">
          Submit a request above; processing and reconciliation will appear
          here.
        </p>
      </div>
    );
  }

  return (
    <div className="border-border overflow-x-auto rounded-none border-2">
      <table className="w-full min-w-[42rem] border-collapse">
        <caption className="sr-only">
          Submitted swap requests and their reconciliation status
        </caption>
        <thead className="border-border bg-muted/30 border-b">
          <tr>
            <th
              scope="col"
              className="text-muted-foreground px-4 py-2 text-left font-mono text-[10px] font-normal tracking-widest uppercase"
            >
              From
            </th>
            <th scope="col" className="w-8 px-1 py-2">
              <span className="sr-only">Converts to</span>
            </th>
            <th
              scope="col"
              className="text-muted-foreground px-4 py-2 text-left font-mono text-[10px] font-normal tracking-widest uppercase"
            >
              To
            </th>
            <th
              scope="col"
              className="text-muted-foreground px-4 py-2 text-left font-mono text-[10px] font-normal tracking-widest uppercase"
            >
              Submitted
            </th>
            <th
              scope="col"
              className="text-muted-foreground px-4 py-2 text-right font-mono text-[10px] font-normal tracking-widest uppercase"
            >
              Status
            </th>
          </tr>
        </thead>
        <tbody>
          {swaps.map((swap, index) => (
            <tr
              key={swap.id}
              className={
                index < swaps.length - 1 ? "border-border border-b" : undefined
              }
            >
              <td className="px-4 py-3">
                <p className="text-destructive truncate font-mono text-xs font-semibold">
                  {swap.sourceTokenAmount.toLocaleString()}{" "}
                  {swap.sourceUnitName}
                </p>
                {swap.sourceUsdValue !== undefined && (
                  <p className="text-muted-foreground font-mono text-[10px]">
                    {formatUsd(swap.sourceUsdValue)}
                  </p>
                )}
              </td>
              <td className="px-1 py-3 text-center">
                <ArrowRight
                  aria-hidden="true"
                  className="text-muted-foreground/40 mx-auto h-3 w-3 shrink-0"
                />
              </td>
              <td className="px-4 py-3">
                <p className="text-success truncate font-mono text-xs font-semibold">
                  {swap.targetTokenAmount.toLocaleString()}{" "}
                  {swap.targetUnitName}
                </p>
                {swap.targetUsdValue !== undefined && (
                  <p className="text-muted-foreground font-mono text-[10px]">
                    {formatUsd(swap.targetUsdValue)}
                  </p>
                )}
              </td>
              <td className="text-muted-foreground px-4 py-3 font-mono text-xs">
                {formatDate(swap.createdAt)}
              </td>
              <td className="px-4 py-3 text-right">
                <Badge variant={statusVariant[swap.status]} size="sm">
                  {statusLabel[swap.status]}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
