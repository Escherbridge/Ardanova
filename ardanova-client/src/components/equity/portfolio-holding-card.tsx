"use client";

import { Lock, ShieldCheck, TrendingUp } from "lucide-react";
import { cn } from "~/lib/utils";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";

type HolderClass = "CONTRIBUTOR" | "INVESTOR" | "FOUNDER";

interface PortfolioHoldingCardProps {
  projectName: string;
  tokenAmount: number;
  equityPct: number;
  usdValue: number;
  isLiquid: boolean;
  holderClass: HolderClass;
  configId: string;
  /** e.g. "Gate 2" */
  gateStatus?: string;
  /** Whether this project has failed / trust protection applies */
  projectFailed?: boolean;
  onWithdraw?: (configId: string, holderClass: HolderClass) => void;
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
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    Math.round(value),
  );
}

function formatEquity(value: number): string {
  return value.toFixed(2) + "%";
}

const holderClassMeta: Record<
  HolderClass,
  { label: string; badgeVariant: "neon" | "neon-green" | "neon-pink" }
> = {
  CONTRIBUTOR: { label: "Contributor", badgeVariant: "neon-green" },
  INVESTOR: { label: "Investor", badgeVariant: "neon" },
  FOUNDER: { label: "Founder", badgeVariant: "neon-pink" },
};

export function PortfolioHoldingCard({
  projectName,
  tokenAmount,
  equityPct,
  usdValue,
  isLiquid,
  holderClass,
  configId,
  gateStatus,
  projectFailed,
  onWithdraw,
  className,
}: PortfolioHoldingCardProps) {
  const meta = holderClassMeta[holderClass];
  const canWithdraw = isLiquid && !projectFailed;

  const borderClass = projectFailed
    ? "border-border"
    : isLiquid && holderClass === "CONTRIBUTOR"
      ? "border-neon-green/60 shadow-[0_0_8px_rgba(0,255,136,0.08)]"
      : holderClass === "INVESTOR"
        ? "border-primary/40"
        : holderClass === "FOUNDER"
          ? "border-neon-pink/40"
          : "border-border";

  return (
    <Card
      className={cn("rounded-none", borderClass, className)}
      padding="none"
    >
      <CardContent className="p-4 space-y-3">
        {/* Top row: project name + badges */}
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="font-mono text-sm font-bold truncate text-foreground">
              {projectName}
            </p>
          </div>
          <div className="flex items-center gap-1.5 shrink-0">
            <Badge variant={meta.badgeVariant} size="sm">
              {meta.label}
            </Badge>
            {isLiquid && !projectFailed ? (
              <Badge variant="neon-green" size="sm">
                LIQUID
              </Badge>
            ) : projectFailed ? (
              <Badge variant="outline" size="sm">
                FAILED
              </Badge>
            ) : (
              <Badge variant="outline" size="sm">
                LOCKED
              </Badge>
            )}
          </div>
        </div>

        {/* Stats row */}
        <div className="grid grid-cols-3 gap-2">
          <div className="space-y-0.5">
            <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
              Tokens
            </p>
            <p className="font-mono text-base font-bold text-foreground">
              {formatInteger(tokenAmount)}
            </p>
          </div>
          <div className="space-y-0.5">
            <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
              Equity
            </p>
            <p className="font-mono text-base font-bold text-foreground">
              {formatEquity(equityPct)}
            </p>
          </div>
          <div className="space-y-0.5">
            <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
              USD Value
            </p>
            <p
              className={cn(
                "font-mono text-base font-bold",
                isLiquid && !projectFailed
                  ? "text-neon-green"
                  : "text-foreground",
              )}
            >
              {formatUsd(usdValue)}
            </p>
          </div>
        </div>

        {/* Lock info / trust protection */}
        {!isLiquid && !projectFailed && gateStatus && (
          <div className="flex items-center gap-2 rounded-none border border-border bg-muted/20 px-3 py-2">
            <Lock className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            <p className="font-mono text-xs text-muted-foreground">
              Locked until {gateStatus}
            </p>
          </div>
        )}

        {projectFailed && holderClass === "INVESTOR" && (
          <div className="flex items-center gap-2 rounded-none border border-neon-cyan/30 bg-neon-cyan/5 px-3 py-2">
            <ShieldCheck className="h-3.5 w-3.5 text-neon-cyan shrink-0" />
            <p className="font-mono text-xs text-neon-cyan">
              Trust protection active — recovery in progress
            </p>
          </div>
        )}

        {/* Action */}
        {canWithdraw && onWithdraw && (
          <Button
            size="sm"
            onClick={() => onWithdraw(configId, holderClass)}
            className="w-full rounded-none bg-neon-green text-black font-mono text-xs uppercase tracking-widest font-bold hover:bg-neon-green/90"
          >
            <TrendingUp className="h-3.5 w-3.5 mr-1.5" />
            Withdraw
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
