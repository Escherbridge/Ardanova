"use client";

import { Lock, PauseCircle, ArrowUpRight } from "lucide-react";
import type { TokenBalanceDto } from "~/lib/api/ardanova/endpoints/token-balances";
import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { cn } from "~/lib/utils";

/**
 * Project-unit holdings for the portfolio page (W6.4). Deliberately never renders a chain asset
 * id, address, or transaction hash — `ProjectTokenConfigDto.assetId`/`mintTxHash` exist on the
 * lookup this reads but are not surfaced here. `TokenBalanceDto` itself carries none of those
 * fields either; the label comes from a project-token lookup keyed by `projectTokenConfigId`.
 */
interface HoldingsProps {
  holdings: readonly TokenBalanceDto[];
  onViewPayoutStatus?: (projectTokenConfigId: string) => void;
  className?: string;
}

function formatUnits(value: number): string {
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

export function Holdings({
  holdings,
  onViewPayoutStatus,
  className,
}: HoldingsProps) {
  const projectHoldings = holdings.filter(
    (holding) => !holding.isPlatformToken && holding.projectTokenConfigId,
  );

  return (
    <div className={cn("grid gap-4 md:grid-cols-2", className)}>
      {projectHoldings.map((holding) => (
        <HoldingCard
          key={holding.id}
          holding={holding}
          onViewPayoutStatus={
            holding.projectTokenConfigId && onViewPayoutStatus
              ? () => onViewPayoutStatus(holding.projectTokenConfigId!)
              : undefined
          }
        />
      ))}
    </div>
  );
}

function HoldingCard({
  holding,
  onViewPayoutStatus,
}: {
  holding: TokenBalanceDto;
  onViewPayoutStatus?: () => void;
}) {
  const configId = holding.projectTokenConfigId;
  const configQuery = api.projectTokens.getSupply.useQuery(
    { id: configId ?? "" },
    { enabled: Boolean(configId), retry: false },
  );

  // A resolvable human label, never the raw configuration id — that id is a database key, not
  // something to show as if it were a chain reference.
  const projectLabel = configQuery.data?.assetName ?? "Project units";
  const unitName = configQuery.data?.unitName ?? "units";
  const hasPayoutContext = holding.holderClass !== null;

  return (
    <Card className="border-border rounded-none border-2" padding="none">
      <CardContent className="space-y-4 p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0">
            <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Project units
            </p>
            <p className="text-foreground mt-1 truncate font-mono text-sm font-bold">
              {configQuery.isLoading ? "Loading…" : projectLabel}
            </p>
          </div>
          {holding.holderClass && (
            <Badge variant="outline" size="sm">
              {holding.holderClass.toLowerCase()}
            </Badge>
          )}
        </div>

        <dl className="border-border grid grid-cols-2 gap-3 border-y py-3">
          <div>
            <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Available
            </dt>
            <dd className="text-foreground mt-1 font-mono text-base font-bold">
              {formatUnits(holding.availableBalance)} {unitName}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
              Pending
            </dt>
            <dd className="text-foreground mt-1 font-mono text-base font-bold">
              {formatUnits(holding.lockedBalance)} {unitName}
            </dd>
          </div>
        </dl>

        {holding.lockedBalance > 0 && (
          <div className="border-border bg-muted/20 flex items-start gap-2 border p-3">
            <Lock className="text-muted-foreground mt-0.5 h-4 w-4 shrink-0" />
            <p className="text-muted-foreground text-xs">
              {formatUnits(holding.lockedBalance)} {unitName} are still in
              their hold window. This screen does not infer a release date.
            </p>
          </div>
        )}

        <div className="text-muted-foreground space-y-2 text-xs">
          <p>
            Holding project units is not proof of equity, ownership, or
            governance rights. Those rights require a separate approved
            agreement.
          </p>
          <p className="font-mono text-[10px] tracking-wide uppercase">
            Updated {formatDate(holding.updatedAt)}
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
