"use client";

import { AlertCircle, Loader2 } from "lucide-react";

import EquityPreview from "~/components/equity/equity-preview";
import GateStatusBanner from "~/components/equity/gate-status-banner";
import GateTimeline from "~/components/equity/gate-timeline";
import InvestorTable from "~/components/equity/investor-table";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import {
  projectGateStatusDtoSchema,
  projectInvestmentDtoSchema,
  type ProjectTokenConfigDto,
} from "~/lib/contracts/tokenomics-contract";
import { api } from "~/trpc/react";

interface FundingTabProps {
  projectId: string;
  projectSlug: string;
}

export default function FundingTab({
  projectId,
  projectSlug,
}: FundingTabProps) {
  const configQuery = api.projectTokens.getConfigByProject.useQuery({
    projectId,
  });

  if (configQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-20" role="status">
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading funding configuration</span>
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
            Funding is not configured
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            This project does not have a validated project-token configuration.
          </p>
        </div>
      </div>
    );
  }

  return (
    <FundingTabContent config={configQuery.data} projectSlug={projectSlug} />
  );
}

function FundingTabContent({
  config,
  projectSlug,
}: {
  config: ProjectTokenConfigDto;
  projectSlug: string;
}) {
  const gateQuery = api.projectTokens.getGateStatus.useQuery({
    configId: config.id,
  });
  const investorsQuery = api.projectTokens.getInvestors.useQuery({
    configId: config.id,
  });
  const gateResult = projectGateStatusDtoSchema.safeParse(gateQuery.data);
  const investmentsResult = projectInvestmentDtoSchema
    .array()
    .safeParse(investorsQuery.data);
  const gate = gateResult.success ? gateResult.data : undefined;

  return (
    <div className="space-y-6">
      <GateStatusBanner
        gateStatus={gate?.gateStatus ?? config.gateStatus}
        fundingRaised={gate?.fundingRaised ?? config.fundingRaised}
        fundingGoal={gate?.fundingGoal ?? config.fundingGoal}
        gate1ClearedAt={gate?.gate1ClearedAt ?? undefined}
        gate2ClearedAt={gate?.gate2ClearedAt ?? undefined}
      />

      <Card className="border-foreground">
        <CardContent className="space-y-3 p-4">
          <GateTimeline gateStatus={gate?.gateStatus ?? config.gateStatus} />
          <p className="text-muted-foreground text-xs leading-relaxed">
            Gate status is read-only here. A status change does not itself prove
            a token allocation, protection payment, or settled payout.
          </p>
        </CardContent>
      </Card>

      <Card className="border-foreground">
        <CardHeader className="pb-2">
          <CardTitle className="font-mono text-base tracking-wide uppercase">
            Fund this project
          </CardTitle>
        </CardHeader>
        <CardContent>
          <EquityPreview
            configId={config.id}
            projectSlug={projectSlug}
            gateStatus={gate?.gateStatus ?? config.gateStatus}
            unitName={config.unitName}
          />
        </CardContent>
      </Card>

      <Card className="border-foreground">
        <CardContent className="p-4">
          {investorsQuery.isLoading ? (
            <div
              className="flex items-center justify-center gap-2 py-6"
              role="status"
            >
              <Loader2 className="text-muted-foreground size-4 animate-spin" />
              <span className="text-muted-foreground text-sm">
                Loading recorded funding…
              </span>
            </div>
          ) : investorsQuery.error || !investmentsResult.success ? (
            <div className="border-destructive/60 border p-4" role="status">
              <p className="text-destructive text-sm font-semibold">
                Funding records unavailable
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                No contributor identity, amount, or protection state is inferred
                while the service is unavailable.
              </p>
            </div>
          ) : (
            <InvestorTable investments={investmentsResult.data} />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
