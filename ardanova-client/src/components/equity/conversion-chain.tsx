"use client";

import { ArrowRight } from "lucide-react";
import { cn } from "~/lib/utils";

/**
 * One hop: DOLLARS in, project units out.
 *
 * It used to read "ARDA -> project units" with a USD rate on the arrow, which put a platform
 * unit at the front of the primary sponsor money surface. D52 settles the direction — a person
 * thinks in dollars and the platform unit is at most a derived caption — and the founder's ask
 * was inverted the same way first. The ARDA figure survives only as the small line underneath,
 * because it is what the exchange leg actually settles in and a receipt that never mentions it
 * would not reconcile.
 */
interface ConversionChainProps {
  /** Dollars the sponsor is spending — the charged amount, not the raw input. */
  dollarAmount: number;
  /** The platform units that dollar figure buys, shown only as the settlement caption. */
  ardaAmount: number;
  /** Whole project units those dollars buy. */
  projectUnits: number;
  /** The project's unit label, e.g. "PLOT". */
  unitName: string;
  className?: string;
}

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatPlatformUnits(value: number): string {
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
  dollarAmount,
  ardaAmount,
  projectUnits,
  unitName,
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
            {formatUsd(dollarAmount)}
          </span>
          <span className="text-muted-foreground font-mono text-xs tracking-wide uppercase">
            You put in
          </span>
        </div>

        <div className="flex shrink-0 flex-col items-center gap-1">
          <ArrowRight className="text-primary h-5 w-5" aria-hidden="true" />
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
          {formatUsd(dollarAmount)} &rarr;{" "}
          <span className="text-foreground font-semibold">
            {formatUnits(projectUnits)} {unitName}
          </span>
        </p>
        <p className="text-muted-foreground mt-1 font-mono text-[10px]">
          Settles as {formatPlatformUnits(ardaAmount)} ARDA from your balance.
        </p>
      </div>
    </div>
  );
}
