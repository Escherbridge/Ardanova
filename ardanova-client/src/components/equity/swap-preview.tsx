"use client";

import { ArrowRight, TrendingUp } from "lucide-react";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(value);
}

function formatRate(rate: number): string {
  return `$${rate.toFixed(4)} / token`;
}

function formatArdaAmount(amount: number): string {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 2,
  }).format(amount);
}

export interface SwapPreviewProps {
  sourceAmount: number;
  sourceUnit: string;
  sourceUsd: number;
  ardaAmount: number;
  targetAmount: number;
  targetUnit: string;
  targetUsd: number;
  sourceRate: number;
  targetRate: number;
  ardaRate: number;
}

export function SwapPreview({
  sourceAmount,
  sourceUnit,
  sourceUsd,
  ardaAmount,
  targetAmount,
  targetUnit,
  targetUsd,
  sourceRate,
  targetRate,
  ardaRate,
}: SwapPreviewProps) {
  return (
    <div className="space-y-3">
      {/* Main conversion card */}
      <div className="grid grid-cols-[1fr_auto_1fr] gap-2 items-stretch">
        {/* Source side */}
        <Card variant="neon-pink" padding="sm" className="rounded-none">
          <CardContent className="p-3 space-y-2">
            <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
              You Send
            </p>
            <p className="font-mono text-2xl font-bold text-neon-pink">
              {sourceAmount.toLocaleString()}
            </p>
            <Badge variant="neon-pink" size="sm">
              {sourceUnit}
            </Badge>
            <p className="font-mono text-xs text-muted-foreground">
              {formatUsd(sourceUsd)}
            </p>
            <p className="font-mono text-[10px] text-muted-foreground/60">
              {formatRate(sourceRate)}
            </p>
          </CardContent>
        </Card>

        {/* ARDA intermediary */}
        <div className="flex flex-col items-center justify-center gap-2 px-1">
          <ArrowRight className="h-4 w-4 text-muted-foreground/40" />
          <div className="border border-neon-cyan/30 bg-neon-cyan/5 px-2 py-3 flex flex-col items-center gap-1">
            <p className="font-mono text-[9px] uppercase tracking-widest text-neon-cyan/70">
              via ARDA
            </p>
            <p className="font-mono text-sm font-bold text-neon-cyan">
              {formatArdaAmount(ardaAmount)}
            </p>
            <p className="font-mono text-[9px] text-muted-foreground/50">
              {formatUsd(ardaRate)}/ARDA
            </p>
          </div>
          <ArrowRight className="h-4 w-4 text-muted-foreground/40" />
        </div>

        {/* Target side */}
        <Card variant="neon-green" padding="sm" className="rounded-none">
          <CardContent className="p-3 space-y-2">
            <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
              You Receive
            </p>
            <p className="font-mono text-2xl font-bold text-neon-green">
              {targetAmount.toLocaleString()}
            </p>
            <Badge variant="neon-green" size="sm">
              {targetUnit}
            </Badge>
            <p className="font-mono text-xs text-muted-foreground">
              {formatUsd(targetUsd)}
            </p>
            <p className="font-mono text-[10px] text-muted-foreground/60">
              {formatRate(targetRate)}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Rate summary row */}
      <div className="border border-border bg-muted/10 px-4 py-2 flex items-center gap-3">
        <TrendingUp className="h-3 w-3 text-muted-foreground/60 shrink-0" />
        <p className="font-mono text-[10px] text-muted-foreground/70">
          <span className="text-neon-pink">{sourceUnit}</span>
          {" → ARDA → "}
          <span className="text-neon-green">{targetUnit}</span>
          {" | Rate: "}
          <span className="text-foreground">{(targetAmount / (sourceAmount || 1)).toFixed(4)}</span>
          {` ${targetUnit}/${sourceUnit}`}
        </p>
      </div>
    </div>
  );
}
