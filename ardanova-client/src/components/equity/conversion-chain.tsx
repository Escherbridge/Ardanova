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
        "flex flex-col gap-3 rounded-none border-2 border-border bg-muted/20 p-4",
        className,
      )}
    >
      <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
        Conversion Preview
      </p>

      {/* Chain row */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1">
        {/* Project tokens */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="font-mono text-lg font-bold text-neon-green">
            {formatInteger(projectTokens)}
          </span>
          <span className="font-mono text-xs text-muted-foreground">
            {tokenTicker}
          </span>
        </div>

        {/* Arrow 1 */}
        <div className="flex flex-col items-center gap-1 shrink-0">
          <ArrowRight className="h-5 w-5 text-neon-cyan" />
          <span className="font-mono text-[10px] text-muted-foreground whitespace-nowrap">
            1 {tokenTicker} = {formatInteger(tokenRate)} ARDA
          </span>
        </div>

        {/* ARDA */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="font-mono text-lg font-bold text-neon-cyan">
            {formatInteger(ardaAmount)}
          </span>
          <span className="font-mono text-xs text-muted-foreground">ARDA</span>
        </div>

        {/* Arrow 2 */}
        <div className="flex flex-col items-center gap-1 shrink-0">
          <ArrowRight className="h-5 w-5 text-neon-cyan" />
          <span className="font-mono text-[10px] text-muted-foreground whitespace-nowrap">
            1 ARDA = {formatUsd(ardaRate)}
          </span>
        </div>

        {/* USD */}
        <div className="flex min-w-0 flex-col items-center gap-1">
          <span className="font-mono text-lg font-bold text-foreground">
            {formatUsd(usdAmount)}
          </span>
          <span className="font-mono text-xs text-muted-foreground">USD</span>
        </div>
      </div>

      {/* Summary line */}
      <div className="border-t border-border pt-2">
        <p className="font-mono text-xs text-muted-foreground">
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
