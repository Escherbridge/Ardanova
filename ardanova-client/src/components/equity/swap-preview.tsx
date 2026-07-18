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
      <div className="grid grid-cols-[1fr_auto_1fr] items-stretch gap-2">
        {/* Source side */}
        <Card variant="neon-pink" padding="sm" className="rounded-none">
          <CardContent className="space-y-2 p-3">
            <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              You Send
            </p>
            <p className="text-destructive font-mono text-2xl font-bold">
              {sourceAmount.toLocaleString()}
            </p>
            <Badge variant="neon-pink" size="sm">
              {sourceUnit}
            </Badge>
            <p className="text-muted-foreground font-mono text-xs">
              {formatUsd(sourceUsd)}
            </p>
            <p className="text-muted-foreground/60 font-mono text-[10px]">
              {formatRate(sourceRate)}
            </p>
          </CardContent>
        </Card>

        {/* ARDA intermediary */}
        <div className="flex flex-col items-center justify-center gap-2 px-1">
          <ArrowRight className="text-muted-foreground/40 h-4 w-4" />
          <div className="border-system/30 bg-system/5 flex flex-col items-center gap-1 border px-2 py-3">
            <p className="text-system/70 font-mono text-[9px] tracking-widest uppercase">
              via ARDA
            </p>
            <p className="text-system font-mono text-sm font-bold">
              {formatArdaAmount(ardaAmount)}
            </p>
            <p className="text-muted-foreground/50 font-mono text-[9px]">
              {formatUsd(ardaRate)}/ARDA
            </p>
          </div>
          <ArrowRight className="text-muted-foreground/40 h-4 w-4" />
        </div>

        {/* Target side */}
        <Card variant="neon-green" padding="sm" className="rounded-none">
          <CardContent className="space-y-2 p-3">
            <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              You Receive
            </p>
            <p className="text-success font-mono text-2xl font-bold">
              {targetAmount.toLocaleString()}
            </p>
            <Badge variant="neon-green" size="sm">
              {targetUnit}
            </Badge>
            <p className="text-muted-foreground font-mono text-xs">
              {formatUsd(targetUsd)}
            </p>
            <p className="text-muted-foreground/60 font-mono text-[10px]">
              {formatRate(targetRate)}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Rate summary row */}
      <div className="border-border bg-muted/10 flex items-center gap-3 border px-4 py-2">
        <TrendingUp className="text-muted-foreground/60 h-3 w-3 shrink-0" />
        <p className="text-muted-foreground/70 font-mono text-[10px]">
          <span className="text-destructive">{sourceUnit}</span>
          {" → ARDA → "}
          <span className="text-success">{targetUnit}</span>
          {" | Rate: "}
          <span className="text-foreground">
            {(targetAmount / (sourceAmount || 1)).toFixed(4)}
          </span>
          {` ${targetUnit}/${sourceUnit}`}
        </p>
      </div>
    </div>
  );
}
