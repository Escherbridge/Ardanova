"use client";

import { Users } from "lucide-react";

import { Badge } from "~/components/ui/badge";

/**
 * One pledge, as a project member is allowed to see it. Mirrors
 * `ProjectInvestorLineDto` (ArdaNova.API.Controllers.ProjectTokensController) — the backer's
 * internal user id never reaches this component; `displayName` reads "Anonymous backer" unless
 * the backer opted in via their per-pledge "show my name" consent.
 */
export interface ProjectInvestorLine {
  id: string;
  displayName: string;
  amount: number;
  unitsPledged: number;
  investedAt: string;
  protectionEligible: boolean;
  protectionPaidOut: boolean;
  protectionAmount: number | null;
}

/**
 * What a non-member sees: proof the project has backers, with no per-pledge amount and no name
 * a stranger could match to a real person. Mirrors `ProjectInvestorSummaryDto`.
 */
export interface ProjectInvestorSummary {
  backerCount: number;
  totalAmount: number;
}

/**
 * The GetInvestors response shape: an itemised list for project members, or an aggregate-only
 * summary for everyone else. Distinguish with `Array.isArray`.
 */
export type ProjectInvestorsResponse = ProjectInvestorLine[] | ProjectInvestorSummary;

interface InvestorTableProps {
  investments: ProjectInvestorsResponse;
}

function formatUsd(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
  }).format(value);
}

function EmptyState() {
  return (
    <div className="border-border flex flex-col items-center justify-center gap-3 border p-8 text-center">
      <div className="border-border flex size-12 items-center justify-center border">
        <Users className="text-muted-foreground size-5" />
      </div>
      <div>
        <p className="text-muted-foreground font-mono text-sm font-bold uppercase">
          No funding records yet
        </p>
        <p className="text-muted-foreground mt-1 text-xs">
          Reconciled funding records will appear here when the backend reports
          them.
        </p>
      </div>
    </div>
  );
}

export default function InvestorTable({ investments }: InvestorTableProps) {
  if (!Array.isArray(investments)) {
    // Non-member view: a project's backers are visible to anyone signed in, but only as a
    // headcount and a total — no name, no per-pledge amount a stranger could match to someone.
    if (investments.backerCount === 0) return <EmptyState />;

    return (
      <div className="border-border flex flex-col items-center justify-center gap-3 border p-8 text-center">
        <div className="border-border flex size-12 items-center justify-center border">
          <Users className="text-muted-foreground size-5" />
        </div>
        <div>
          <p className="font-mono text-2xl font-bold">
            {new Intl.NumberFormat("en-US").format(investments.backerCount)}{" "}
            {investments.backerCount === 1 ? "backer" : "backers"}
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {formatUsd(investments.totalAmount)} raised so far. Join this
            project to see individual pledges.
          </p>
        </div>
      </div>
    );
  }

  if (investments.length === 0) return <EmptyState />;

  const sorted = [...investments].sort((left, right) => right.amount - left.amount);

  return (
    <div className="border-border border">
      <div className="border-border bg-muted/30 border-b px-4 py-3">
        <span className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
          Recorded funding ({investments.length})
        </span>
      </div>

      <div className="divide-border divide-y">
        {sorted.map((investment) => (
          <article
            key={investment.id}
            className="grid grid-cols-2 gap-3 px-4 py-4 sm:grid-cols-[minmax(0,1fr)_auto_auto] sm:items-center"
          >
            <div className="col-span-2 min-w-0 sm:col-span-1">
              <p className="truncate font-mono text-sm font-semibold">
                {investment.displayName}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                Recorded {new Date(investment.investedAt).toLocaleDateString()}
              </p>
            </div>

            <div>
              <p className="text-primary font-mono text-sm font-bold">
                {formatUsd(investment.amount)}
              </p>
              <p className="text-muted-foreground font-mono text-xs">
                {new Intl.NumberFormat("en-US").format(
                  investment.unitsPledged,
                )}{" "}
                units
              </p>
            </div>

            <div className="text-right">
              {investment.protectionPaidOut ? (
                <>
                  <Badge variant="outline">Protection recorded paid</Badge>
                  <p className="text-muted-foreground mt-1 text-[10px]">
                    {investment.protectionAmount === null
                      ? "Amount unavailable"
                      : formatUsd(investment.protectionAmount)}
                  </p>
                </>
              ) : investment.protectionEligible ? (
                <Badge variant="secondary">Protection eligible</Badge>
              ) : (
                <Badge variant="outline">No protection eligibility</Badge>
              )}
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}
