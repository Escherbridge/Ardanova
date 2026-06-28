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
  const allocated = contributorSupply + investorSupply + founderSupply + burnedSupply;
  const available = Math.max(0, totalSupply - allocated);

  const segments: Segment[] = [
    { key: "contributor", label: "Contributors", color: "text-[#00ff88]", bg: "bg-[#00ff88]", value: contributorSupply },
    { key: "investor", label: "Investors", color: "text-[#00d4ff]", bg: "bg-[#00d4ff]", value: investorSupply },
    { key: "founder", label: "Founders", color: "text-[#ff0080]", bg: "bg-[#ff0080]", value: founderSupply },
    { key: "burned", label: "Burned", color: "text-muted-foreground", bg: "bg-muted-foreground/40", value: burnedSupply },
    { key: "available", label: "Available", color: "text-border", bg: "bg-border/40", value: available },
  ].filter((s) => s.value > 0);

  return (
    <div className="space-y-3">
      <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground">
        TOKEN SUPPLY BREAKDOWN
      </span>

      {/* Stacked bar */}
      <div className="flex h-8 border-2 border-border overflow-hidden rounded-sm">
        {segments.map((seg) => {
          const pct = totalSupply > 0 ? (seg.value / totalSupply) * 100 : 0;
          return (
            <div
              key={seg.key}
              className={cn("h-full transition-all", seg.bg)}
              style={{ width: `${pct}%` }}
              title={`${seg.label}: ${formatToken(seg.value, unitName)} (${formatPct(pct)})`}
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
              <div className={cn("size-3 border border-border/60", seg.bg)} />
              <div className="flex-1 min-w-0">
                <span className="text-xs text-muted-foreground">{seg.label}</span>
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
      <div className="border-t-2 border-border pt-3 flex justify-between items-center">
        <span className="text-xs text-muted-foreground">Total Supply</span>
        <span className="font-mono text-sm font-bold text-foreground">
          {formatToken(totalSupply, unitName)}
        </span>
      </div>
    </div>
  );
}
