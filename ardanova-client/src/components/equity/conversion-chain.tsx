"use client";

import { ArrowRight } from "lucide-react";
import { cn } from "~/lib/utils";

/**
 * One hop: ARDA in, project units out. Replaces the old three-node
 * project-token -> ARDA -> USD chain (W6.4) — a sponsor spends ARDA and holds units, nothing
 * else appears in between. The USD figure is a reference on the arrow, never a second leg.
 */
interface ConversionChainProps {
  /** Whole ARDA the sponsor is spending (the charged amount, not the raw input). */
  ardaAmount: number;
  /** Whole project units that ARDA buys. */
  projectUnits: number;
  /** The project's unit label, e.g. "PLOT". */
  unitName: string;
  /** Reference value of one ARDA, e.g. 0.0134 for "1 ARDA = $0.0134". Informational only. */
  usdPerArda: number;
  className?: string;
}

function formatUsdRate(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(value);
}

function formatArda(value: number): string {
  return new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatUnits(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    value,
  );
}

export function ConversionChain({
  ardaAmount,
  projectUnits,
  unitName,
  usdPerArda,
  className,
}: ConversionChainProps) {
  return (
    <div
      className={cn(
        "border-border bg-muted/20 flex flex-col gap-3 rounded-none border-2 p-4",
        className,
      )}
    >
      <p className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
        One-hop preview
      </p>

      <div className="flex items-center gap-3">
        <div className="flex min-w-0 flex-1 flex-col items-center gap-1 text-center">
          <span className="text-system font-mono text-2xl font-bold">
            {formatArda(ardaAmount)}
          </span>
          <span className="text-muted-foreground font-mono text-xs tracking-wide uppercase">
            ARDA
          </span>
        </div>

        <div className="flex shrink-0 flex-col items-center gap-1">
          <ArrowRight className="text-primary h-5 w-5" aria-hidden="true" />
          <span className="text-muted-foreground font-mono text-[10px] whitespace-nowrap">
            1 ARDA = {formatUsdRate(usdPerArda)}
          </span>
        </div>

        <div className="flex min-w-0 flex-1 flex-col items-center gap-1 text-center">
          <span className="text-foreground font-mono text-2xl font-bold">
            {formatUnits(projectUnits)}
          </span>
          <span className="text-muted-foreground font-mono text-xs tracking-wide uppercase">
            {unitName}
          </span>
        </div>
      </div>

      <div className="border-border border-t pt-2">
        <p className="text-muted-foreground font-mono text-xs">
          {formatArda(ardaAmount)} ARDA &rarr;{" "}
          <span className="text-foreground font-semibold">
            {formatUnits(projectUnits)} {unitName}
          </span>
        </p>
      </div>
    </div>
  );
}
