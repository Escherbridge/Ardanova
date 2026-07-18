"use client";

import { cn } from "~/lib/utils";

interface SupplyBreakdownBarProps {
  totalSupply: number;
  contributorSupply: number;
  investorSupply: number;
  founderSupply: number;
  burnedSupply: number;
  unitName: string;
}

interface Segment {
  key: string;
  label: string;
  color: string;
  bg: string;
  value: number;
}

function formatToken(n: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(Math.round(n))} ${unit}`;
}

function formatPct(n: number) {
  return `${n.toFixed(1)}%`;
}

export default function SupplyBreakdownBar({
  totalSupply,
  contributorSupply,
  investorSupply,
  founderSupply,
  burnedSupply,
  unitName,
}: SupplyBreakdownBarProps) {
  const allocated =
    contributorSupply + investorSupply + founderSupply + burnedSupply;
  const available = Math.max(0, totalSupply - allocated);

  const segments: Segment[] = [
    {
      key: "contributor",
      label: "Contributors",
      color: "text-success",
      bg: "bg-success",
      value: contributorSupply,
    },
    {
      key: "investor",
      label: "Investors",
      color: "text-system",
      bg: "bg-system",
      value: investorSupply,
    },
    {
      key: "founder",
      label: "Founders",
      color: "text-warning",
      bg: "bg-warning",
      value: founderSupply,
    },
    {
      key: "burned",
      label: "Burned",
      color: "text-destructive",
      bg: "bg-destructive",
      value: burnedSupply,
    },
    {
      key: "available",
      label: "Available",
      color: "text-muted-foreground",
      bg: "bg-muted-foreground",
      value: available,
    },
  ].filter((s) => s.value > 0);

  return (
    <div className="space-y-3">
      <span className="text-muted-foreground font-mono text-xs font-bold tracking-widest">
        TOKEN SUPPLY BREAKDOWN
      </span>

      {/* Stacked bar */}
      <div
        className="border-border flex h-8 overflow-hidden border-2"
        role="img"
        aria-label={`Supply allocation: ${segments
          .map((segment) => {
            const percentage =
              totalSupply > 0 ? (segment.value / totalSupply) * 100 : 0;
            return `${segment.label} ${formatPct(percentage)}`;
          })
          .join(", ")}`}
      >
        {segments.map((seg) => {
          const pct = totalSupply > 0 ? (seg.value / totalSupply) * 100 : 0;
          return (
            <div
              key={seg.key}
              className={cn(
                "border-card h-full border-r-2 transition-all last:border-r-0",
                seg.bg,
              )}
              style={{ width: `${pct}%` }}
              title={`${seg.label}: ${formatToken(seg.value, unitName)} (${formatPct(pct)})`}
              aria-hidden="true"
            />
          );
        })}
      </div>

      {/* Legend */}
      <div className="grid grid-cols-2 gap-2">
        {segments.map((seg) => {
          const pct = totalSupply > 0 ? (seg.value / totalSupply) * 100 : 0;
          return (
            <div key={seg.key} className="flex items-center gap-2">
              <div
                className={cn("border-foreground size-3 border", seg.bg)}
                aria-hidden="true"
              />
              <div className="min-w-0 flex-1">
                <span className="text-muted-foreground text-xs">
                  {seg.label}
                </span>
              </div>
              <div className="text-right">
                <span className={cn("font-mono text-xs font-bold", seg.color)}>
                  {formatPct(pct)}
                </span>
              </div>
            </div>
          );
        })}
      </div>

      {/* Totals row */}
      <div className="border-border flex items-center justify-between border-t-2 pt-3">
        <span className="text-muted-foreground text-xs">Total Supply</span>
        <span className="text-foreground font-mono text-sm font-bold">
          {formatToken(totalSupply, unitName)}
        </span>
      </div>
    </div>
  );
}
