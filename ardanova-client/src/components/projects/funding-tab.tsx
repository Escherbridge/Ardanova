"use client";

import { useState, type FormEvent } from "react";
import { AlertCircle, Info, Loader2, ShieldCheck } from "lucide-react";
import { toast } from "sonner";

import EquityPreview from "~/components/equity/equity-preview";
import GateStatusBanner from "~/components/equity/gate-status-banner";
import GateTimeline from "~/components/equity/gate-timeline";
import InvestorTable from "~/components/equity/investor-table";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import {
  projectGateStatusDtoSchema,
  projectInvestorsResponseSchema,
  type ProjectTokenConfigDto,
} from "~/lib/contracts/tokenomics-contract";
import { api, type RouterOutputs } from "~/trpc/react";

/** The funding setup shape the server returns; no address, asset id or chain name is in it. */
type ProjectTokenFunding = RouterOutputs["projectTokens"]["getFunding"];

interface FundingTabProps {
  projectId: string;
  projectSlug: string;
}

/**
 * The funding tab reads ONE answer — `isFundable` — which the server derives from what was
 * actually recorded for the project (its stake, its treasury account, its worker and sponsor
 * allowances). Everything else on this tab is presentation of that answer.
 */
export default function FundingTab({
  projectId,
  projectSlug,
}: FundingTabProps) {
  const fundingQuery = api.projectTokens.getFunding.useQuery({ projectId });

  if (fundingQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-20" role="status">
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading funding</span>
      </div>
    );
  }

  if (fundingQuery.error || !fundingQuery.data) {
    return (
      <div
        className="border-destructive bg-destructive/5 flex items-start gap-3 border p-6"
        role="status"
      >
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div>
          <p className="text-destructive font-mono text-sm font-bold uppercase">
            Funding is unavailable right now
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {fundingQuery.error?.message ??
              "Nothing about this project's funding is inferred while the service is unreachable."}
          </p>
        </div>
      </div>
    );
  }

  const funding = fundingQuery.data;

  if (!funding.isFundable) {
    return (
      <FundingSetupPanel
        funding={funding}
        onCompleted={() => void fundingQuery.refetch()}
      />
    );
  }

  return <FundingReadyPanel funding={funding} projectSlug={projectSlug} />;
}

/**
 * Everything short of ready. A visitor is told plainly that the project is not taking funds
 * yet; the owner is additionally given the declaration that starts (or resumes) setup.
 */
function FundingSetupPanel({
  funding,
  onCompleted,
}: {
  funding: ProjectTokenFunding;
  onCompleted: () => void;
}) {
  const suspended = funding.state === "Suspended";

  return (
    <div className="space-y-6">
      <div
        className={`flex items-start gap-3 border p-6 ${
          suspended
            ? "border-destructive bg-destructive/5"
            : "border-foreground bg-muted/30"
        }`}
        role="status"
      >
        <Info
          className={`mt-0.5 size-5 shrink-0 ${suspended ? "text-destructive" : "text-muted-foreground"}`}
        />
        <div>
          <p className="font-mono text-sm font-bold uppercase">
            {suspended
              ? "Funding is suspended"
              : "This project is not taking funds yet"}
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {funding.setupMessage ??
              "Funding setup for this project is not finished yet."}
          </p>
        </div>
      </div>

      {funding.ownerCanCompleteSetup ? (
        <FundingDeclarationForm funding={funding} onCompleted={onCompleted} />
      ) : null}
    </div>
  );
}

/**
 * The owner's declaration: what a stake in this project is, how much of it exists, and how
 * it is split between the people who fund the work and the people who do it. Submitting runs
 * the server-side setup job; it is safe to submit again after a failure because the server
 * resumes rather than repeating what already succeeded.
 */
function FundingDeclarationForm({
  funding,
  onCompleted,
}: {
  funding: ProjectTokenFunding;
  onCompleted: () => void;
}) {
  const alreadyDeclared = funding.state !== "NotDeclared";
  // The declaration is only fixed once the stake itself exists; before that a retry may
  // still change it, and the server adopts the new one (ProjectTokenService.ApplyDeclaration).
  const declarationLocked =
    funding.state !== "NotDeclared" && funding.state !== "AwaitingUnits";
  const [assetName, setAssetName] = useState(funding.assetName ?? "");
  const [unitName, setUnitName] = useState(funding.unitName ?? "");
  const [totalSupply, setTotalSupply] = useState(
    funding.totalSupply > 0 ? String(funding.totalSupply) : "1000",
  );
  const [investorSupply, setInvestorSupply] = useState(
    funding.investorPartitionSupply > 0
      ? String(funding.investorPartitionSupply)
      : "600",
  );
  const [contributorSupply, setContributorSupply] = useState(
    funding.contributorPartitionSupply > 0
      ? String(funding.contributorPartitionSupply)
      : "300",
  );
  const [fundingGoal, setFundingGoal] = useState(
    funding.fundingGoal > 0 ? String(funding.fundingGoal) : "",
  );
  const [disclosure, setDisclosure] = useState(funding.disclosure ?? "");
  const [error, setError] = useState<string | null>(null);

  const provision = api.projectTokens.provisionFunding.useMutation({
    onSuccess: (result) => {
      setError(null);
      if (result.isFundable) {
        toast.success("This project can now accept funding.");
      } else {
        toast.warning(
          result.setupMessage ?? "Funding setup did not finish. Try again.",
        );
      }
      onCompleted();
    },
    onError: (mutationError) => setError(mutationError.message),
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const total = Number(totalSupply);
    const forSponsors = Number(investorSupply);
    const forWorkers = Number(contributorSupply);
    const goal = Number(fundingGoal);

    if (!Number.isInteger(total) || total < 1) {
      setError("Set how many units of stake exist, as a whole number.");
      return;
    }
    if (
      !Number.isInteger(forSponsors) ||
      !Number.isInteger(forWorkers) ||
      forSponsors < 0 ||
      forWorkers < 0
    ) {
      setError("Sponsor and worker shares must be whole numbers.");
      return;
    }
    if (forSponsors + forWorkers > total) {
      setError(
        `Sponsor and worker shares add up to ${forSponsors + forWorkers} units, more than the ${total} that exist.`,
      );
      return;
    }
    if (!(goal > 0)) {
      setError("Set a funding goal above zero.");
      return;
    }

    setError(null);
    provision.mutate({
      projectId: funding.projectId,
      assetName: assetName.trim(),
      unitName: unitName.trim().toUpperCase(),
      totalSupply: total,
      investorSupply: forSponsors,
      contributorSupply: forWorkers,
      fundingGoal: goal,
      disclosure: disclosure.trim(),
      reservedPercentage: 0,
    });
  };

  return (
    <Card className="border-foreground">
      <CardHeader className="pb-2">
        <CardTitle className="font-mono text-base tracking-wide uppercase">
          {alreadyDeclared ? "Finish funding setup" : "Set up funding"}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-muted-foreground mb-4 text-xs leading-relaxed">
          A stake is a share of this project. Sponsors receive stake when they put
          funds in; workers receive stake for the work they do. You decide how much
          exists and how it is divided.
        </p>

        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="funding-stake-name">Name of the stake</Label>
              <Input
                id="funding-stake-name"
                value={assetName}
                onChange={(event) => setAssetName(event.target.value)}
                placeholder="Riverside Allotment Stake"
                maxLength={32}
                disabled={declarationLocked || provision.isPending}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="funding-stake-label">Short label</Label>
              <Input
                id="funding-stake-label"
                value={unitName}
                onChange={(event) => setUnitName(event.target.value)}
                placeholder="PLOT"
                maxLength={8}
                disabled={declarationLocked || provision.isPending}
                required
              />
              <p className="text-muted-foreground text-xs">
                Shown next to a quantity, e.g. &ldquo;12 PLOT&rdquo;.
              </p>
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-1.5">
              <Label htmlFor="funding-total-supply">Units in total</Label>
              <Input
                id="funding-total-supply"
                inputMode="numeric"
                value={totalSupply}
                onChange={(event) => setTotalSupply(event.target.value)}
                disabled={declarationLocked || provision.isPending}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="funding-sponsor-share">For sponsors</Label>
              <Input
                id="funding-sponsor-share"
                inputMode="numeric"
                value={investorSupply}
                onChange={(event) => setInvestorSupply(event.target.value)}
                disabled={declarationLocked || provision.isPending}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="funding-worker-share">For workers</Label>
              <Input
                id="funding-worker-share"
                inputMode="numeric"
                value={contributorSupply}
                onChange={(event) => setContributorSupply(event.target.value)}
                disabled={declarationLocked || provision.isPending}
                required
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="funding-goal">Funding goal</Label>
            <Input
              id="funding-goal"
              inputMode="decimal"
              value={fundingGoal}
              onChange={(event) => setFundingGoal(event.target.value)}
              disabled={declarationLocked || provision.isPending}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="funding-disclosure">
              What a stake in this project means
            </Label>
            <Textarea
              id="funding-disclosure"
              value={disclosure}
              onChange={(event) => setDisclosure(event.target.value)}
              rows={4}
              minLength={20}
              maxLength={2000}
              placeholder="Say what a supporter is getting, when their stake becomes fully theirs, and what happens if the project does not finish."
              disabled={declarationLocked || provision.isPending}
              required
            />
            <p className="text-muted-foreground text-xs">
              Everyone sees this before they commit funds.
            </p>
          </div>

          {error ? (
            <p className="text-destructive text-sm" role="alert">
              {error}
            </p>
          ) : null}

          <Button type="submit" disabled={provision.isPending}>
            {provision.isPending ? (
              <>
                <Loader2 className="mr-2 size-4 animate-spin" />
                Setting up…
              </>
            ) : alreadyDeclared ? (
              "Retry setup"
            ) : (
              "Set up funding"
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

/** The fundable project: what it has raised, what a stake means, and who has supported it. */
function FundingReadyPanel({
  funding,
  projectSlug,
}: {
  funding: ProjectTokenFunding;
  projectSlug: string;
}) {
  const configQuery = api.projectTokens.getConfigByProject.useQuery({
    projectId: funding.projectId,
  });

  if (configQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-20" role="status">
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading funding detail</span>
      </div>
    );
  }

  if (configQuery.error || !configQuery.data) {
    return (
      <div className="border-destructive/60 border p-6" role="status">
        <p className="text-destructive text-sm font-semibold">
          Funding detail is unavailable
        </p>
        <p className="text-muted-foreground mt-1 text-xs">
          This project can accept funding, but its current figures could not be
          read. Nothing is inferred while the service is unreachable.
        </p>
      </div>
    );
  }

  return (
    <FundingTabContent
      config={configQuery.data}
      funding={funding}
      projectSlug={projectSlug}
    />
  );
}

function FundingTabContent({
  config,
  funding,
  projectSlug,
}: {
  config: ProjectTokenConfigDto;
  funding: ProjectTokenFunding;
  projectSlug: string;
}) {
  const gateQuery = api.projectTokens.getGateStatus.useQuery({
    configId: config.id,
  });
  const investorsQuery = api.projectTokens.getInvestors.useQuery({
    configId: config.id,
  });
  const gateResult = projectGateStatusDtoSchema.safeParse(gateQuery.data);
  // Members get itemised pledges; everyone else gets an aggregate summary. Parse the union so a
  // summary reaches InvestorTable as a summary rather than failing and reading "unavailable".
  const investmentsResult = projectInvestorsResponseSchema.safeParse(
    investorsQuery.data,
  );
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
            a stake allocation, protection payment, or settled payout.
          </p>
        </CardContent>
      </Card>

      {funding.disclosure ? (
        <Card className="border-foreground">
          <CardHeader className="pb-2">
            <CardTitle className="flex items-center gap-2 font-mono text-base tracking-wide uppercase">
              <ShieldCheck className="size-4" />
              What a stake here means
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <p className="text-sm leading-relaxed whitespace-pre-line">
              {funding.disclosure}
            </p>
            <p className="text-muted-foreground text-xs">
              {funding.investorUnitsRemaining} of{" "}
              {funding.investorPartitionSupply} sponsor units and{" "}
              {funding.contributorUnitsRemaining} of{" "}
              {funding.contributorPartitionSupply} worker units are still
              available.
            </p>
          </CardContent>
        </Card>
      ) : null}

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
