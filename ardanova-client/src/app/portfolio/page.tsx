"use client";

import { useEffect, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  ArrowUpRight,
  CircleDollarSign,
  Layers3,
  Loader2,
  Lock,
  PauseCircle,
  Wallet,
} from "lucide-react";
import type { PayoutStatus } from "~/lib/api/ardanova/endpoints/payouts";
import { buildSignInHref } from "~/lib/auth-navigation";
import {
  payoutStageLabel,
  summarizePayoutRecords,
} from "~/lib/commerce/portfolio-contract";
import { api } from "~/trpc/react";
import { PortfolioHoldingCard } from "~/components/equity/portfolio-holding-card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";

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

function shortenReference(value: string | null): string {
  if (!value) return "Platform token";
  return value.length > 18 ? `${value.slice(0, 8)}…${value.slice(-6)}` : value;
}

const statusBadgeVariant = {
  PENDING: "warning",
  PROCESSING: "neon",
  COMPLETED: "outline",
  FAILED: "destructive",
  CANCELLED: "outline",
} as const satisfies Record<
  PayoutStatus,
  "neon" | "warning" | "outline" | "destructive"
>;

export default function PortfolioPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const queryEnabled = Boolean(session?.user?.id);

  const {
    data: portfolio,
    isLoading: portfolioLoading,
    error: portfolioError,
  } = api.tokenBalances.getPortfolio.useQuery(undefined, {
    enabled: queryEnabled,
  });
  const {
    data: ardaBalance,
    isLoading: ardaLoading,
    error: ardaError,
    refetch: refetchArdaBalance,
  } = api.tokenBalances.getArdaBalance.useQuery(undefined, {
    enabled: queryEnabled,
    retry: false,
  });
  const {
    data: payouts,
    isLoading: payoutsLoading,
    error: payoutsError,
  } = api.payouts.getPayoutsByUser.useQuery(undefined, {
    enabled: queryEnabled,
  });

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.replace(buildSignInHref("/portfolio"));
    }
  }, [router, sessionStatus]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  const holdings = portfolio?.holdings ?? [];
  const payoutSummary = summarizePayoutRecords(payouts ?? []);

  function viewPayoutStatus(projectTokenConfigId: string) {
    router.push(
      `/portfolio/withdraw?configId=${encodeURIComponent(projectTokenConfigId)}`,
    );
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-5xl space-y-8 px-4 py-8">
        <header className="border-foreground grid gap-5 border-b-2 pb-6 md:grid-cols-[1fr_auto] md:items-end">
          <div>
            <p className="text-primary font-mono text-xs tracking-[0.22em] uppercase">
              Recorded positions
            </p>
            <h1 className="text-foreground mt-2 flex items-center gap-3 text-4xl font-black tracking-tight uppercase">
              <Wallet className="h-8 w-8" />
              Portfolio
            </h1>
            <p className="text-muted-foreground mt-3 max-w-2xl text-sm">
              Inspect balances and payout records returned by ArdaNova’s
              tokenomics API. A token balance is not proof of equity, ownership,
              or governance rights.
            </p>
          </div>
          <Button asChild variant="outline" className="min-h-11 rounded-none">
            <Link href="/portfolio/withdraw">
              <PauseCircle className="mr-2 h-4 w-4" />
              Payout status
              <ArrowUpRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </header>

        <section
          aria-labelledby="portfolio-summary-heading"
          className="space-y-3"
        >
          <div className="flex items-end justify-between gap-3">
            <div>
              <p className="text-muted-foreground font-mono text-[10px] tracking-[0.2em] uppercase">
                Backend record
              </p>
              <h2
                id="portfolio-summary-heading"
                className="text-xl font-black uppercase"
              >
                Portfolio summary
              </h2>
            </div>
            {portfolioLoading && <Loader2 className="h-5 w-5 animate-spin" />}
          </div>

          {portfolioError ? (
            <div
              role="alert"
              className="border-destructive bg-destructive/10 border-2 p-5"
            >
              <p className="text-destructive text-sm">
                Portfolio records could not be loaded. No balance or valuation
                claims are shown.
              </p>
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <SummaryCard
                label="Balance records"
                value={portfolioLoading ? "—" : formatInteger(holdings.length)}
                icon={<Layers3 className="h-4 w-4" />}
                detail="Project and platform token records returned by the API."
              />
              <SummaryCard
                label="Recorded total USD"
                value={
                  portfolioLoading
                    ? "—"
                    : formatUsd(portfolio?.totalPortfolioValueUsd ?? 0)
                }
                icon={<CircleDollarSign className="h-4 w-4" />}
                detail="Portfolio-level API field; not calculated from individual cards."
              />
              <SummaryCard
                label="Recorded liquid USD"
                value={
                  portfolioLoading
                    ? "—"
                    : formatUsd(portfolio?.totalLiquidValueUsd ?? 0)
                }
                icon={<Wallet className="h-4 w-4" />}
                detail="Value currently marked liquid by the portfolio endpoint."
              />
              <SummaryCard
                label="Recorded locked USD"
                value={
                  portfolioLoading
                    ? "—"
                    : formatUsd(portfolio?.totalLockedValueUsd ?? 0)
                }
                icon={<Lock className="h-4 w-4" />}
                detail="Value currently marked locked by the portfolio endpoint."
              />
            </div>
          )}

          <div
            className={`border p-3 text-xs ${
              ardaError
                ? "border-destructive bg-destructive/10 text-destructive"
                : "border-border bg-muted/20 text-muted-foreground"
            }`}
            role={ardaError ? "alert" : undefined}
          >
            <span className="text-foreground font-mono font-bold uppercase">
              ARDA utility balance:
            </span>{" "}
            {ardaLoading
              ? "Loading…"
              : ardaError
                ? "The ARDA balance request failed. This is not a zero balance, and no amount is shown."
                : ardaBalance
                  ? `${formatInteger(ardaBalance.balance)} total · ${formatInteger(ardaBalance.availableBalance)} available`
                  : "No separate ARDA balance record was returned."}
            {ardaError && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="mt-3 flex"
                onClick={() => void refetchArdaBalance()}
              >
                Retry ARDA balance
              </Button>
            )}
          </div>
        </section>

        <section aria-labelledby="holdings-heading" className="space-y-4">
          <div className="border-border flex items-center justify-between border-b pb-2">
            <h2
              id="holdings-heading"
              className="font-mono text-sm tracking-widest uppercase"
            >
              Token balance records
            </h2>
            {!portfolioLoading && !portfolioError && (
              <Badge variant="outline" size="sm">
                {holdings.length} record{holdings.length === 1 ? "" : "s"}
              </Badge>
            )}
          </div>

          {portfolioLoading ? (
            <div className="flex justify-center py-12">
              <Loader2 className="text-primary h-6 w-6 animate-spin" />
            </div>
          ) : portfolioError ? null : holdings.length === 0 ? (
            <div className="border-border border-2 border-dashed p-8 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                No token balance records were returned.
              </p>
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              {holdings.map((holding) => {
                const projectTokenConfigId = holding.projectTokenConfigId;

                return (
                  <PortfolioHoldingCard
                    key={holding.id}
                    holding={holding}
                    onViewPayoutStatus={
                      projectTokenConfigId
                        ? () => viewPayoutStatus(projectTokenConfigId)
                        : undefined
                    }
                  />
                );
              })}
            </div>
          )}
        </section>

        <section aria-labelledby="payout-records-heading" className="space-y-4">
          <div className="border-border border-b pb-3">
            <p className="text-primary font-mono text-[10px] tracking-[0.2em] uppercase">
              Submitted ≠ confirmed ≠ reconciled
            </p>
            <h2
              id="payout-records-heading"
              className="mt-1 text-xl font-black uppercase"
            >
              Payout records
            </h2>
          </div>

          {!payoutsLoading && !payoutsError && (
            <div className="grid gap-3 sm:grid-cols-2">
              <SummaryCard
                label="Submitted / processing USD"
                value={formatUsd(payoutSummary.submittedUsd)}
                icon={<PauseCircle className="h-4 w-4" />}
                detail={`${payoutSummary.pendingCount} pending record${payoutSummary.pendingCount === 1 ? "" : "s"}; this is not confirmed settlement.`}
              />
              <SummaryCard
                label="API completed-record USD"
                value={formatUsd(payoutSummary.completedRecordUsd)}
                icon={<CircleDollarSign className="h-4 w-4" />}
                detail="The current API has no durable reconciliation field, so this is not labeled reconciled."
              />
            </div>
          )}

          {payoutsLoading ? (
            <div className="flex justify-center py-8">
              <Loader2 className="text-primary h-6 w-6 animate-spin" />
            </div>
          ) : payoutsError ? (
            <div
              role="alert"
              className="border-destructive bg-destructive/10 border-2 p-5"
            >
              <p className="text-destructive text-sm">
                Payout records could not be loaded. No settlement totals are
                shown.
              </p>
            </div>
          ) : !payouts?.length ? (
            <div className="border-border border-2 border-dashed p-8 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                No payout records were returned.
              </p>
            </div>
          ) : (
            <div className="border-border overflow-x-auto border-2">
              <table className="w-full min-w-[46rem] border-collapse">
                <caption className="sr-only">
                  Payout requests returned by the API and their recorded status
                </caption>
                <thead className="border-border bg-muted/30 border-b">
                  <tr>
                    {[
                      "Requested",
                      "Source configuration",
                      "Tokens",
                      "Recorded USD",
                      "API status",
                    ].map((column) => (
                      <th
                        key={column}
                        scope="col"
                        className="text-muted-foreground px-4 py-3 text-left font-mono text-[10px] font-normal tracking-widest uppercase"
                      >
                        {column}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {payouts.map((payout) => (
                    <tr
                      key={payout.id}
                      className="border-border border-b last:border-b-0"
                    >
                      <td className="text-muted-foreground px-4 py-3 font-mono text-xs">
                        {formatDate(payout.requestedAt)}
                      </td>
                      <td
                        className="max-w-56 truncate px-4 py-3 font-mono text-xs"
                        title={payout.sourceProjectTokenConfigId ?? undefined}
                      >
                        {shortenReference(payout.sourceProjectTokenConfigId)}
                      </td>
                      <td className="px-4 py-3 font-mono text-xs">
                        {formatInteger(payout.sourceTokenAmount)}
                      </td>
                      <td className="px-4 py-3 font-mono text-xs">
                        {payout.usdAmount === null
                          ? "Not recorded"
                          : formatUsd(payout.usdAmount)}
                      </td>
                      <td className="px-4 py-3">
                        <Badge
                          variant={statusBadgeVariant[payout.status]}
                          size="sm"
                        >
                          {payoutStageLabel[payout.status]}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

function SummaryCard({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: string;
  detail: string;
  icon: ReactNode;
}) {
  return (
    <Card className="border-border rounded-none border-2" padding="sm">
      <CardContent className="space-y-2 p-0">
        <p className="text-muted-foreground flex items-center gap-2 font-mono text-[10px] tracking-widest uppercase">
          {icon}
          {label}
        </p>
        <p className="text-foreground font-mono text-xl font-bold">{value}</p>
        <p className="text-muted-foreground text-xs">{detail}</p>
      </CardContent>
    </Card>
  );
}
