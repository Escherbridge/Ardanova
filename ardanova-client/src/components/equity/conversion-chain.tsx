"use client";

import { ArrowRight } from "lucide-react";
import { cn } from "~/lib/utils";

interface ConversionChainProps {
  projectTokens: number;
  tokenTicker: string;
  ardaAmount: number;
  usdAmount: number;
  tokenRate: number;
  ardaRate: number;
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

function formatInteger(value: number): string {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 0,
  }).format(Math.round(value));
}

export function ConversionChain({
  projectTokens,
  tokenTicker,
  ardaAmount,
  usdAmount,
  tokenRate,
  ardaRate,
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
        Conversion Preview
      </p>

      {/* Chain row */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1">
        {/* Project tokens */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="text-success font-mono text-lg font-bold">
            {formatInteger(projectTokens)}
          </span>
          <span className="text-muted-foreground font-mono text-xs">
            {tokenTicker}
          </span>
        </div>

        {/* Arrow 1 */}
        <div className="flex shrink-0 flex-col items-center gap-1">
          <ArrowRight className="text-system h-5 w-5" />
          <span className="text-muted-foreground font-mono text-[10px] whitespace-nowrap">
            1 {tokenTicker} = {formatInteger(tokenRate)} ARDA
          </span>
        </div>

        {/* ARDA */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="text-system font-mono text-lg font-bold">
            {formatInteger(ardaAmount)}
          </span>
          <span className="text-muted-foreground font-mono text-xs">ARDA</span>
        </div>

        {/* Arrow 2 */}
        <div className="flex shrink-0 flex-col items-center gap-1">
          <ArrowRight className="text-system h-5 w-5" />
          <span className="text-muted-foreground font-mono text-[10px] whitespace-nowrap">
            1 ARDA = {formatUsd(ardaRate)}
          </span>
        </div>

        {/* USD */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="text-foreground font-mono text-lg font-bold">
            {formatUsd(usdAmount)}
          </span>
          <span className="text-muted-foreground font-mono text-xs">USD</span>
        </div>
      </div>

      {/* Summary line */}
      <div className="border-border border-t pt-2">
        <p className="text-muted-foreground font-mono text-xs">
          {formatInteger(projectTokens)} {tokenTicker} &rarr;{" "}
          {formatInteger(ardaAmount)} ARDA &rarr;{" "}
          <span className="text-foreground font-semibold">
            {formatUsd(usdAmount)}
          </span>
        </p>
      </div>
    </div>
  );
}
