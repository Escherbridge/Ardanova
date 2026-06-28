"use client";

import { api } from "~/trpc/react";
import { Loader2, AlertCircle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";

import GateStatusBanner from "~/components/equity/gate-status-banner";
import GateTimeline from "~/components/equity/gate-timeline";
import EquityPreview from "~/components/equity/equity-preview";
import InvestorTable from "~/components/equity/investor-table";
import FounderAllocation from "~/components/equity/founder-allocation";

interface FundingTabProps {
  projectId: string;
  projectSlug: string;
}

export default function FundingTab({ projectId, projectSlug }: FundingTabProps) {
  const { data: configRaw, isLoading: configLoading, error: configError } =
    api.projectTokens.getConfigByProject.useQuery({ projectId });
  const config = configRaw as unknown as TokenConfig | undefined;

  if (configLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-6 animate-spin text-primary" />
      </div>
    );
  }

  if (configError ?? !config) {
    return (
      <div className="border-2 border-destructive/40 bg-destructive/5 p-6 flex items-start gap-3">
        <AlertCircle className="size-5 text-destructive shrink-0 mt-0.5" />
        <div>
          <p className="font-mono text-sm font-bold text-destructive">FUNDING NOT CONFIGURED</p>
          <p className="text-xs text-muted-foreground mt-1">
            This project does not have a token configuration yet.
          </p>
        </div>
      </div>
    );
  }

  return <FundingTabContent config={config} projectSlug={projectSlug} />;
}

type TokenConfig = {
  id: string;
  projectId: string;
  totalSupply: number;
  fundingGoal: number;
  fundingRaised: number;
  unitName: string;
  assetName: string;
  contributorSupply: number;
  investorSupply: number;
  founderSupply: number;
  burnedSupply: number;
  status: string;
  gateStatus: string;
};

function FundingTabContent({
  config,
  projectSlug,
}: {
  config: TokenConfig;
  projectSlug: string;
}) {
  const gateStatus = config.gateStatus as "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

  const { data: gateDataRaw } = api.projectTokens.getGateStatus.useQuery({ configId: config.id });
  const gateData = gateDataRaw as unknown as { gateStatus: string; gate1ClearedAt?: string; gate2ClearedAt?: string; fundingRaised: number; fundingGoal: number } | undefined;

  const { data: investorsRaw, isLoading: investorsLoading } = api.projectTokens.getInvestors.useQuery(
    { configId: config.id },
  );
  const investors = investorsRaw as unknown as { userId: string; displayName: string; avatarUrl?: string; usdAmount: number; tokenAmount: number; equityPct: number; holderClass: string }[] | undefined;

  const founderInvestors = investors?.filter((i) => i.holderClass === "FOUNDER") ?? [];
  const regularInvestors = investors?.filter((i) => i.holderClass !== "FOUNDER") ?? [];

  const founderEntry = founderInvestors[0];

  return (
    <div className="space-y-6">
      {/* Gate Status Banner */}
      <GateStatusBanner
        gateStatus={gateStatus}
        fundingRaised={gateData?.fundingRaised ?? config.fundingRaised}
        fundingGoal={gateData?.fundingGoal ?? config.fundingGoal}
        gate1ClearedAt={gateData?.gate1ClearedAt}
        gate2ClearedAt={gateData?.gate2ClearedAt}
      />

      {/* Timeline */}
      <Card className="bg-card border-2 border-border">
        <CardContent className="p-4">
          <GateTimeline gateStatus={gateStatus} />
        </CardContent>
      </Card>

      {/* Equity Preview / CTA */}
      <Card className="bg-card border-2 border-border">
        <CardHeader className="pb-2">
          <CardTitle className="text-base font-mono tracking-wide">
            INVEST IN THIS PROJECT
          </CardTitle>
        </CardHeader>
        <CardContent>
          <EquityPreview
            configId={config.id}
            projectSlug={projectSlug}
            gateStatus={gateStatus}
            unitName={config.unitName}
          />
        </CardContent>
      </Card>

      {/* Founder Allocation */}
      {founderEntry && (
        <FounderAllocation
          founder={{
            displayName: founderEntry.displayName,
            avatarUrl: founderEntry.avatarUrl,
            equityPct: founderEntry.equityPct,
          }}
          isLocked={gateStatus === "FUNDING" || gateStatus === "ACTIVE"}
        />
      )}

      {/* Investor Table */}
      <Card className="bg-card border-2 border-border">
        <CardContent className="p-4">
          {investorsLoading ? (
            <div className="flex items-center gap-2 py-6 justify-center">
              <Loader2 className="size-4 animate-spin text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Loading investors...</span>
            </div>
          ) : (
            <InvestorTable
              investors={regularInvestors.map((i) => ({
                displayName: i.displayName,
                avatarUrl: i.avatarUrl,
                usdAmount: i.usdAmount,
                equityPct: i.equityPct,
                holderClass: i.holderClass,
              }))}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
