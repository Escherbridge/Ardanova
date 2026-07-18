"use client";

import { Users } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import type { ProjectInvestmentDto } from "~/lib/contracts/tokenomics-contract";

interface InvestorTableProps {
  investments: ProjectInvestmentDto[];
}

function formatUsd(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
  }).format(value);
}

function shortReference(value: string) {
  return value.length > 16 ? `${value.slice(0, 12)}…` : value;
}

export default function InvestorTable({ investments }: InvestorTableProps) {
  if (investments.length === 0) {
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

  const sorted = [...investments].sort(
    (left, right) => right.usdAmount - left.usdAmount,
  );

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
              <p
                className="truncate font-mono text-sm font-semibold"
                title={investment.userId}
              >
                User {shortReference(investment.userId)}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                Recorded {new Date(investment.investedAt).toLocaleDateString()}
              </p>
            </div>

            <div>
              <p className="text-primary font-mono text-sm font-bold">
                {formatUsd(investment.usdAmount)}
              </p>
              <p className="text-muted-foreground font-mono text-xs">
                {new Intl.NumberFormat("en-US").format(investment.tokenAmount)}{" "}
                token units
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
