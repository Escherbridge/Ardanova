"use client";

import { cn } from "~/lib/utils";

interface TreasuryBucketsProps {
  indexFundUsd: number;
  liquidReserveUsd: number;
  operationsUsd: number;
  totalUsd: number;
  ardaSupply: number;
  ardaValueUsd: number;
}

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatInteger(value: number): string {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 0,
  }).format(Math.round(value));
}

function formatPercent(value: number): string {
  return `${value.toFixed(1)}%`;
}

interface BucketColumnProps {
  label: string;
  amountUsd: number;
  totalUsd: number;
  targetPct: number;
  colorClass: string;
  barColorClass: string;
}

function BucketColumn({
  label,
  amountUsd,
  totalUsd,
  targetPct,
  colorClass,
  barColorClass,
}: BucketColumnProps) {
  const actualPct = totalUsd > 0 ? (amountUsd / totalUsd) * 100 : 0;
  const barWidth = Math.min(actualPct, 100);

  return (
    <div className="flex flex-1 flex-col gap-4 border-2 border-white/20 p-5">
      <div className="space-y-1">
        <p
          className={cn(
            "font-mono text-xs font-bold tracking-widest uppercase",
            colorClass,
          )}
        >
          {label}
        </p>
        <p className="text-muted-foreground text-xs">
          Target: {formatPercent(targetPct)}
        </p>
      </div>

      <div className="space-y-2">
        <p className={cn("font-mono text-2xl font-bold", colorClass)}>
          {formatUsd(amountUsd)}
        </p>
        <p className="text-muted-foreground font-mono text-sm">
          {formatPercent(actualPct)} of total
        </p>
      </div>

      {/* Progress bar */}
      <div className="h-3 w-full border border-white/20 bg-black">
        <div
          className={cn("h-full transition-all duration-500", barColorClass)}
          style={{ width: `${barWidth}%` }}
        />
      </div>

      {/* Target marker */}
      <div className="relative h-1">
        <div
          className="absolute top-0 h-3 w-px -translate-y-1 bg-white/40"
          style={{ left: `${targetPct}%` }}
          title={`Target: ${formatPercent(targetPct)}`}
        />
      </div>
    </div>
  );
}

export function TreasuryBuckets({
  indexFundUsd,
  liquidReserveUsd,
  operationsUsd,
  totalUsd,
  ardaSupply,
  ardaValueUsd,
}: TreasuryBucketsProps) {
  return (
    <div className="space-y-6">
      {/* Three buckets */}
      <div className="flex gap-4">
        <BucketColumn
          label="Index Fund"
          amountUsd={indexFundUsd}
          totalUsd={totalUsd}
          targetPct={55}
          colorClass="text-system"
          barColorClass="bg-system"
        />
        <BucketColumn
          label="Liquid Reserve"
          amountUsd={liquidReserveUsd}
          totalUsd={totalUsd}
          targetPct={30}
          colorClass="text-success"
          barColorClass="bg-success"
        />
        <BucketColumn
          label="Operations"
          amountUsd={operationsUsd}
          totalUsd={totalUsd}
          targetPct={15}
          colorClass="text-destructive"
          barColorClass="bg-destructive"
        />
      </div>

      {/* Summary row */}
      <div className="flex gap-4 border-2 border-white/10 bg-white/5 p-4">
        <div className="flex-1 space-y-1">
          <p className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Total Treasury
          </p>
          <p className="font-mono text-xl font-bold">{formatUsd(totalUsd)}</p>
        </div>
        <div className="w-px bg-white/10" />
        <div className="flex-1 space-y-1">
          <p className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            ARDA Value
          </p>
          <p className="text-system font-mono text-xl font-bold">
            {formatUsd(ardaValueUsd)}
          </p>
        </div>
        <div className="w-px bg-white/10" />
        <div className="flex-1 space-y-1">
          <p className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Total Supply
          </p>
          <p className="font-mono text-xl font-bold">
            {formatInteger(ardaSupply)}{" "}
            <span className="text-muted-foreground text-sm">ARDA</span>
          </p>
        </div>
      </div>
    </div>
  );
}
