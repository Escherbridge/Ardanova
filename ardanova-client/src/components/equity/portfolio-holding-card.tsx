"use client";

import { ArrowUpRight, Lock, PauseCircle } from "lucide-react";
import type { TokenBalanceDto } from "~/lib/api/ardanova/endpoints/token-balances";
import { cn } from "~/lib/utils";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";

interface PortfolioHoldingCardProps {
  holding: TokenBalanceDto;
  onViewPayoutStatus?: () => void;
  className?: string;
}

function formatInteger(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    value,
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(value));
}

function shortenReference(value: string): string {
  return value.length > 18 ? `${value.slice(0, 8)}…${value.slice(-6)}` : value;
}

export function PortfolioHoldingCard({
  holding,
  onViewPayoutStatus,
  className,
}: PortfolioHoldingCardProps) {
  const reference = holding.projectTokenConfigId ?? holding.id;
  const hasPayoutContext =
    holding.projectTokenConfigId !== null && holding.holderClass !== null;

  return (
    <Card
      className={cn("border-border rounded-none border-2", className)}
      padding="none"
    >
      <CardContent className="space-y-4 p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0">
            <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              {holding.isPlatformToken
                ? "ARDA utility token record"
                : "Project token configuration"}
            </p>
            <p
              className="text-foreground mt-1 truncate font-mono text-sm font-bold"
              title={reference}
            >
              {shortenReference(reference)}
            </p>
          </div>
          <div className="flex flex-wrap gap-1.5">
            {holding.holderClass && (
              <Badge variant="outline" size="sm">
                {holding.holderClass.toLowerCase()}
              </Badge>
            )}
            <Badge
              variant={holding.isLiquid ? "neon-green" : "outline"}
              size="sm"
            >
              {holding.isLiquid ? "API: liquid" : "API: time-locked"}
            </Badge>
          </div>
        </div>

        <dl className="border-border grid grid-cols-3 gap-3 border-y py-3">
          <div>
            <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Balance
            </dt>
            <dd className="text-foreground mt-1 font-mono text-base font-bold">
              {formatInteger(holding.balance)}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Available
            </dt>
            <dd className="text-foreground mt-1 font-mono text-base font-bold">
              {formatInteger(holding.availableBalance)}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Locked
            </dt>
            <dd className="text-foreground mt-1 font-mono text-base font-bold">
              {formatInteger(holding.lockedBalance)}
            </dd>
          </div>
        </dl>

        {holding.lockedBalance > 0 && (
          <div className="border-border bg-muted/20 flex items-start gap-2 border p-3">
            <Lock className="text-muted-foreground mt-0.5 h-4 w-4 shrink-0" />
            <p className="text-muted-foreground text-xs">
              The API records {formatInteger(holding.lockedBalance)} tokens as
              locked. This screen does not infer an unlock date.
            </p>
          </div>
        )}

        <div className="text-muted-foreground space-y-2 text-xs">
          <p>
            A token balance is not proof of equity, ownership, or governance
            rights. Those rights require a separate approved agreement.
          </p>
          <p className="font-mono text-[10px] tracking-wide uppercase">
            API updated {formatDate(holding.updatedAt)}
          </p>
        </div>

        {hasPayoutContext && onViewPayoutStatus && (
          <Button
            type="button"
            variant="outline"
            onClick={onViewPayoutStatus}
            className="min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
          >
            <PauseCircle className="mr-2 h-4 w-4" />
            Payout status
            <ArrowUpRight className="ml-auto h-4 w-4" />
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
