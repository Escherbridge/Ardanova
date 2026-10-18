"use client";

import { type FormEvent, useState } from "react";
import { useSession } from "next-auth/react";
import Link from "next/link";
import { Info, Loader2, Send, Undo2 } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import { formatEnumLabel } from "~/hooks/use-enum";
import { useOpportunityMutations } from "~/hooks/use-opportunity-mutations";
import type { OpportunityBid } from "~/lib/api/ardanova/endpoints/opportunity-bids";
import { api } from "~/trpc/react";
import { ApplyForm } from "~/components/opportunities/apply-form";

interface BidFormProps {
  opportunityId: string;
  opportunityStatus: string;
  projectId?: string | null;
}

const OPEN_BID_STATUSES = new Set(["SUBMITTED", "UNDER_REVIEW"]);
const REPROPOSABLE_BID_STATUSES = new Set(["WITHDRAWN", "REJECTED"]);
const MAX_PROPOSAL_LENGTH = 2000;

function formatAmount(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(
    value,
  );
}

/** Positive finite number, or undefined for blank/invalid input — mirrors the server's `.positive()`. */
function parsePositiveAmount(raw: string): number | undefined {
  const trimmed = raw.trim();
  if (!trimmed) return undefined;
  const value = Number(trimmed);
  return Number.isFinite(value) && value > 0 ? value : undefined;
}

/** Positive whole number, or undefined — mirrors the server's `.int().positive()`. */
function parsePositiveInteger(raw: string): number | undefined {
  const trimmed = raw.trim();
  if (!trimmed) return undefined;
  const value = Number(trimmed);
  return Number.isInteger(value) && value > 0 ? value : undefined;
}

/**
 * Worker-side "propose terms" flow. Submits an opportunityBid.create proposal — the paid
 * counterpart to the free-text apply flow — and, once one exists, shows its status with a
 * withdraw action so the same bid the owner's OpportunityBidsPanel accepts is visible here
 * too. The award is denominated in the project's own units, with a reference ARDA figure
 * shown alongside so the number is never read as a token price.
 */
export function BidForm(props: BidFormProps) {
  const { data: session, status: sessionStatus } = useSession();
  const isAuthenticated = sessionStatus === "authenticated" && !!session?.user;

  // Joining the team is the primary path; paid terms are a second, optional offer.
  return (
    <div className="space-y-6">
      {isAuthenticated ? (
        <ApplyForm
          opportunityId={props.opportunityId}
          opportunityStatus={props.opportunityStatus}
        />
      ) : null}
      <PaidProposalForm {...props} isAuthenticated={isAuthenticated} />
    </div>
  );
}

/** Paid-work proposal; an award is required because task commerce cannot accept an unpaid bid. */
function PaidProposalForm({
  opportunityId,
  opportunityStatus,
  projectId,
  isAuthenticated,
}: BidFormProps & { isAuthenticated: boolean }) {

  const { createBid, withdrawBid, isCreatingBid, isWithdrawingBid } =
    useOpportunityMutations();

  const myBidsQuery = api.opportunityBid.getByBidderId.useQuery(undefined, {
    enabled: isAuthenticated,
  });
  const myBids: OpportunityBid[] = myBidsQuery.data ?? [];
  const myBid = myBids.find((bid) => bid.opportunityId === opportunityId);

  // The award is quoted in the project's own units; the ARDA figure alongside it is a
  // reference only (same one-hop rate the funding preview quotes), never a second input.
  const configQuery = api.projectTokens.getConfigByProject.useQuery(
    { projectId: projectId ?? "" },
    { enabled: isAuthenticated && !!projectId, retry: false },
  );
  const config = configQuery.data;
  const unitName = config?.unitName;

  const tokenValueQuery = api.exchange.getProjectTokenValue.useQuery(
    { configId: config?.id ?? "" },
    { enabled: isAuthenticated && !!config?.id, retry: false },
  );
  const ardaValueQuery = api.exchange.getArdaValue.useQuery(undefined, {
    enabled: isAuthenticated && !!config?.id,
    retry: false,
  });
  const ardaPerUnit =
    tokenValueQuery.data !== undefined &&
    ardaValueQuery.data !== undefined &&
    ardaValueQuery.data > 0
      ? tokenValueQuery.data / ardaValueQuery.data
      : null;

  const [proposal, setProposal] = useState("");
  const [proposedAmount, setProposedAmount] = useState("");
  const [estimatedHours, setEstimatedHours] = useState("");
  const [timeline, setTimeline] = useState("");
  const [deliverables, setDeliverables] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const amountValue = parsePositiveAmount(proposedAmount);
  const ardaReference =
    amountValue !== undefined && ardaPerUnit !== null
      ? amountValue * ardaPerUnit
      : null;

  const isOpportunityOpen = opportunityStatus === "OPEN";
  const hasOpenBid = !!myBid && OPEN_BID_STATUSES.has(myBid.status);
  const canProposeNew = !myBid || REPROPOSABLE_BID_STATUSES.has(myBid.status);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setValidationError(null);

    const trimmedProposal = proposal.trim();
    if (trimmedProposal.length === 0) {
      setValidationError("Describe the terms you're proposing.");
      return;
    }
    if (trimmedProposal.length > MAX_PROPOSAL_LENGTH) {
      setValidationError(
        `Keep your proposal under ${MAX_PROPOSAL_LENGTH} characters.`,
      );
      return;
    }
    if (amountValue === undefined) {
      setValidationError(
        "Enter a positive award. To help without pay, apply for the role instead.",
      );
      return;
    }
    if (
      estimatedHours.trim() &&
      parsePositiveInteger(estimatedHours) === undefined
    ) {
      setValidationError("Estimated hours must be a positive whole number.");
      return;
    }

    createBid.mutate(
      {
        opportunityId,
        proposal: trimmedProposal,
        proposedAmount: amountValue,
        estimatedHours: parsePositiveInteger(estimatedHours),
        timeline: timeline.trim() || undefined,
        deliverables: deliverables.trim() || undefined,
      },
      {
        onSuccess: () => {
          setProposal("");
          setProposedAmount("");
          setEstimatedHours("");
          setTimeline("");
          setDeliverables("");
        },
      },
    );
  }

  if (!isAuthenticated) {
    return (
      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Propose paid terms</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground text-sm">
            <Link href="/auth/signin" className="text-primary underline">
              Sign in
            </Link>{" "}
            to propose terms and get paid for this work.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (myBid && !canProposeNew) {
    return (
      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between gap-3">
            <CardTitle className="text-lg">Your proposal</CardTitle>
            <Badge variant="outline">{formatEnumLabel(myBid.status)}</Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm whitespace-pre-wrap">{myBid.proposal}</p>
          {myBid.proposedAmount != null && (
            <p className="text-muted-foreground text-sm">
              Proposed award: {formatAmount(myBid.proposedAmount)}{" "}
              {unitName ?? "units"}
              {ardaPerUnit !== null &&
                ` (≈ ${formatAmount(myBid.proposedAmount * ardaPerUnit)} ARDA)`}
            </p>
          )}
          {hasOpenBid && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => withdrawBid.mutate({ id: myBid.id })}
              disabled={isWithdrawingBid}
            >
              {isWithdrawingBid ? (
                <Loader2 className="animate-spin" />
              ) : (
                <Undo2 />
              )}
              Withdraw proposal
            </Button>
          )}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="bg-card border-border">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Propose paid terms</CardTitle>
      </CardHeader>
      <CardContent>
        {myBid && (
          <p className="text-muted-foreground mb-4 text-sm">
            Your last proposal was {formatEnumLabel(myBid.status).toLowerCase()}
            . You can propose new terms below.
          </p>
        )}
        {!isOpportunityOpen ? (
          <p className="text-muted-foreground text-sm">
            This opportunity is no longer accepting proposals.
          </p>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="bid-proposal">Proposal</Label>
              <Textarea
                id="bid-proposal"
                value={proposal}
                onChange={(event) => setProposal(event.target.value)}
                placeholder="How you'd approach this work, and why you're a good fit."
                maxLength={MAX_PROPOSAL_LENGTH}
                rows={4}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="bid-amount">
                  Proposed award{unitName ? ` (${unitName})` : ""}
                </Label>
                <Input
                  id="bid-amount"
                  type="number"
                  inputMode="decimal"
                  min="0"
                  step="any"
                  value={proposedAmount}
                  onChange={(event) => setProposedAmount(event.target.value)}
                  required
                />
                {ardaReference !== null && (
                  <p className="text-muted-foreground text-xs">
                    &asymp; {formatAmount(ardaReference)} ARDA at today&apos;s
                    rate
                  </p>
                )}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="bid-hours">Estimated hours</Label>
                <Input
                  id="bid-hours"
                  type="number"
                  inputMode="numeric"
                  min="1"
                  step="1"
                  value={estimatedHours}
                  onChange={(event) => setEstimatedHours(event.target.value)}
                  placeholder="Optional"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="bid-timeline">Timeline</Label>
              <Input
                id="bid-timeline"
                value={timeline}
                onChange={(event) => setTimeline(event.target.value)}
                placeholder="e.g. 2 weeks from acceptance"
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="bid-deliverables">Deliverables</Label>
              <Textarea
                id="bid-deliverables"
                value={deliverables}
                onChange={(event) => setDeliverables(event.target.value)}
                placeholder="What you'll hand over when the work is done."
                rows={3}
              />
            </div>

            {validationError && (
              <p className="text-destructive text-sm">{validationError}</p>
            )}

            <div className="text-muted-foreground flex items-start gap-2 text-xs">
              <Info className="mt-0.5 size-3.5 shrink-0" />
              <p>
                The opportunity owner reviews proposals and accepts one to
                start paid work; submitting here does not move funds.
              </p>
            </div>

            <Button type="submit" disabled={isCreatingBid}>
              {isCreatingBid ? (
                <Loader2 className="animate-spin" />
              ) : (
                <Send />
              )}
              Submit proposal
            </Button>
          </form>
        )}
      </CardContent>
    </Card>
  );
}
