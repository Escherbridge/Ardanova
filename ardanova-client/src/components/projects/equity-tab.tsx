"use client";

import { AlertCircle, Loader2, ShieldCheck } from "lucide-react";

import AllocationTable from "~/components/equity/allocation-table";
import SupplyBreakdownBar from "~/components/equity/supply-breakdown-bar";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import {
  projectAllocationSummaryDtoSchema,
  projectGateStatusDtoSchema,
  projectTokenConfigDtoSchema,
  tokenAllocationDtoSchema,
  type ProjectAllocationSummaryDto,
  type ProjectTokenConfigDto,
} from "~/lib/contracts/tokenomics-contract";
import { api } from "~/trpc/react";

interface TokenPositionTabProps {
  projectId: string;
}

const gateLabel = {
  FUNDING: "Funding",
  ACTIVE: "Active",
  SUCCEEDED: "Succeeded",
  FAILED: "Failed",
} as const;

export default function EquityTab({ projectId }: TokenPositionTabProps) {
  const configQuery = api.projectTokens.getConfigByProject.useQuery({
    projectId,
  });

  if (configQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-20" role="status">
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading project stake</span>
      </div>
    );
  }

  if (configQuery.error || !configQuery.data) {
    return (
      <div
        className="border-destructive bg-destructive/5 flex items-start gap-3 border p-6"
        role="status"
      >
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div>
          <p className="text-destructive font-mono text-sm font-bold uppercase">
            Project stake configuration unavailable
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            No validated stake configuration is available for this project.
          </p>
        </div>
      </div>
    );
  }

  return <TokenPositionContent config={configQuery.data} />;
}

function TokenPositionContent({ config }: { config: ProjectTokenConfigDto }) {
  const supplyQuery = api.projectTokens.getSupply.useQuery({ id: config.id });
  const allocationsQuery = api.projectTokens.getAllocations.useQuery({
    configId: config.id,
  });
  const gateQuery = api.projectTokens.getGateStatus.useQuery({
    configId: config.id,
  });
  const supplyResult = projectTokenConfigDtoSchema.safeParse(supplyQuery.data);
  // The route branches its payload on project membership: a member is answered with the
  // itemised allocations, everyone else with an aggregate object. Discriminating on the
  // shape is required — parsing only the array shape would turn a withheld answer into
  // "no allocations recorded", which is a different and untrue statement.
  const allocationResult = tokenAllocationDtoSchema
    .array()
    .safeParse(allocationsQuery.data);
  const allocationSummaryResult = projectAllocationSummaryDtoSchema.safeParse(
    allocationsQuery.data,
  );
  const gateResult = projectGateStatusDtoSchema.safeParse(gateQuery.data);
  const gate = gateResult.success ? gateResult.data : undefined;

  return (
    <div className="space-y-6">
      <Card className="border-foreground">
        <CardHeader className="pb-2">
          <CardTitle className="font-mono text-base tracking-wide uppercase">
            Project unit supply
          </CardTitle>
        </CardHeader>
        <CardContent>
          {supplyQuery.isLoading ? (
            <div
              className="flex items-center justify-center gap-2 py-6"
              role="status"
            >
              <Loader2 className="text-muted-foreground size-4 animate-spin" />
              <span className="text-muted-foreground text-xs">
                Loading validated supply…
              </span>
            </div>
          ) : supplyResult.success ? (
            <SupplyBreakdownBar
              totalSupply={supplyResult.data.totalSupply}
              contributorSupply={supplyResult.data.contributorSupply}
              investorSupply={supplyResult.data.investorSupply}
              founderSupply={supplyResult.data.founderSupply}
              burnedSupply={supplyResult.data.burnedSupply}
              unitName={config.unitName}
            />
          ) : (
            <p className="text-muted-foreground text-xs" role="status">
              Supply data is unavailable. No totals are inferred.
            </p>
          )}
        </CardContent>
      </Card>

      <Card className="border-foreground">
        <CardHeader className="pb-2">
          <CardTitle className="flex items-center gap-2 font-mono text-base tracking-wide uppercase">
            <ShieldCheck className="size-4" aria-hidden="true" />
            Gate status
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {gateQuery.isLoading ? (
            <p className="text-muted-foreground text-xs" role="status">
              Loading gate evidence…
            </p>
          ) : gate ? (
            <>
              <div className="flex flex-wrap items-center gap-3">
                <Badge
                  variant={
                    gate.gateStatus === "FAILED" ? "destructive" : "outline"
                  }
                >
                  {gateLabel[gate.gateStatus]}
                </Badge>
                <span className="text-muted-foreground font-mono text-xs">
                  {new Intl.NumberFormat("en-US", {
                    style: "currency",
                    currency: "USD",
                  }).format(gate.fundingRaised)}{" "}
                  of{" "}
                  {new Intl.NumberFormat("en-US", {
                    style: "currency",
                    currency: "USD",
                  }).format(gate.fundingGoal)}{" "}
                  recorded
                </span>
              </div>
              <p className="text-muted-foreground text-xs leading-relaxed">
                This is a read-only projection of the backend gate record. Gate
                transitions, unit write-offs, and protection processing are
                privileged administrative workflows and are not available from
                this project view.
              </p>
            </>
          ) : (
            <p className="text-muted-foreground text-xs" role="status">
              Gate evidence is unavailable. No transition or payout state is
              inferred.
            </p>
          )}
        </CardContent>
      </Card>

      <Card className="border-foreground">
        <CardHeader className="pb-2">
          <CardTitle className="font-mono text-base tracking-wide uppercase">
            Recorded allocations
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {allocationsQuery.isLoading ? (
            <div
              className="flex items-center justify-center gap-2 py-10"
              role="status"
            >
              <Loader2 className="text-muted-foreground size-4 animate-spin" />
              <span className="text-muted-foreground text-sm">
                Loading allocations…
              </span>
            </div>
          ) : allocationResult.success ? (
            <AllocationTable allocations={allocationResult.data} />
          ) : allocationSummaryResult.success ? (
            <AllocationSummary summary={allocationSummaryResult.data} />
          ) : (
            <p className="text-muted-foreground p-5 text-xs" role="status">
              Allocation records are unavailable. No allocation is inferred.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

/**
 * What a person who is not on this project sees: proof that work has been set aside, with
 * no per-worker award size, no recipient and no settlement reference. Shown INSTEAD of the
 * table, never as an empty table — "no allocations recorded" would be a different claim
 * from "these are not yours to read".
 */
function AllocationSummary({
  summary,
}: {
  summary: ProjectAllocationSummaryDto;
}) {
  return (
    <div className="space-y-2 p-5">
      <p className="font-mono text-sm font-bold">
        {summary.allocationCount === 1
          ? "1 allocation recorded"
          : `${summary.allocationCount} allocations recorded`}
        {summary.allocationCount > 0
          ? ` · ${summary.distributedCount} paid out`
          : ""}
      </p>
      <p className="text-muted-foreground text-xs">
        {summary.totalUnitsAllocated.toLocaleString("en-US")} units set aside in
        total. Who each allocation is for, and how large it is, is shown to
        project members only.
      </p>
    </div>
  );
}
