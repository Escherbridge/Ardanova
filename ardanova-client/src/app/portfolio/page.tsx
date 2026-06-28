"use client";

import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { Loader2, Wallet, TrendingUp, Lock, Zap } from "lucide-react";
import { api } from "~/trpc/react";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { PortfolioHoldingCard } from "~/components/equity/portfolio-holding-card";

type HolderClass = "CONTRIBUTOR" | "INVESTOR" | "FOUNDER";

interface Holding {
  projectName: string;
  tokenAmount: number;
  equityPct: number;
  usdValue: number;
  isLiquid: boolean;
  holderClass: string;
  projectTokenConfigId: string;
}

interface PortfolioData {
  holdings: Holding[];
}

interface ArdaBalanceData {
  balance: number;
  usdValue: number;
}

interface PayoutRecord {
  id: string;
  status: string;
  sourceTokenAmount: number;
  usdAmount: number;
  createdAt: string;
  projectName: string;
}

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
    Math.round(value),
  );
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

const statusBadgeVariant: Record<
  string,
  "neon" | "neon-green" | "neon-pink" | "warning" | "outline" | "destructive"
> = {
  PENDING: "warning",
  PROCESSING: "neon",
  COMPLETED: "neon-green",
  FAILED: "destructive",
  CANCELLED: "outline",
};

export default function PortfolioPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const userId = session?.user?.id ?? "";

  const {
    data: portfolioRaw,
    isLoading: portfolioLoading,
    error: portfolioError,
  } = api.tokenBalances.getPortfolio.useQuery(
    { userId },
    { enabled: !!userId },
  );

  const { data: ardaRaw, isLoading: ardaLoading } =
    api.tokenBalances.getArdaBalance.useQuery(
      { userId },
      { enabled: !!userId },
    );

  const { data: payoutsRaw, isLoading: payoutsLoading } =
    api.payouts.getPayoutsByUser.useQuery(
      { userId },
      { enabled: !!userId },
    );

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-neon-cyan" />
      </div>
    );
  }

  if (!session) {
    router.push("/sign-in");
    return null;
  }

  const portfolio = portfolioRaw as unknown as PortfolioData | undefined;
  const ardaBalance = ardaRaw as unknown as ArdaBalanceData | undefined;
  const payouts = payoutsRaw as unknown as PayoutRecord[] | undefined;

  const holdings = portfolio?.holdings ?? [];

  const totalUsd = holdings.reduce((sum, h) => sum + h.usdValue, 0);
  const liquidUsd = holdings
    .filter((h) => h.isLiquid)
    .reduce((sum, h) => sum + h.usdValue, 0);
  const lockedUsd = totalUsd - liquidUsd;

  function handleWithdraw(configId: string, holderClass: HolderClass) {
    router.push(
      `/portfolio/withdraw?configId=${encodeURIComponent(configId)}&holderClass=${holderClass}`,
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-4xl px-4 py-8 space-y-8">
        {/* Page header */}
        <div>
          <h1 className="font-mono text-3xl font-bold uppercase tracking-tight text-foreground flex items-center gap-3">
            <div className="w-10 h-10 border-2 border-neon-cyan/60 bg-neon-cyan/10 flex items-center justify-center">
              <Wallet className="h-5 w-5 text-neon-cyan" />
            </div>
            Portfolio
          </h1>
          <p className="font-mono text-sm text-muted-foreground mt-2">
            Your equity holdings and payout history
          </p>
        </div>

        {/* Summary cards */}
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          {/* Total Value */}
          <Card variant="elevated" padding="sm" className="rounded-none">
            <CardContent className="p-0 space-y-1">
              <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground flex items-center gap-1">
                <TrendingUp className="h-3 w-3" />
                Total Value
              </p>
              {portfolioLoading ? (
                <Loader2 className="h-5 w-5 animate-spin text-neon-cyan" />
              ) : (
                <p className="font-mono text-xl font-bold text-foreground">
                  {formatUsd(totalUsd)}
                </p>
              )}
            </CardContent>
          </Card>

          {/* Liquid */}
          <Card variant="neon-green" padding="sm" className="rounded-none">
            <CardContent className="p-0 space-y-1">
              <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground flex items-center gap-1">
                <Zap className="h-3 w-3" />
                Liquid
              </p>
              {portfolioLoading ? (
                <Loader2 className="h-5 w-5 animate-spin text-neon-green" />
              ) : (
                <p className="font-mono text-xl font-bold text-neon-green">
                  {formatUsd(liquidUsd)}
                </p>
              )}
            </CardContent>
          </Card>

          {/* Locked */}
          <Card padding="sm" className="rounded-none border-2 border-border">
            <CardContent className="p-0 space-y-1">
              <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground flex items-center gap-1">
                <Lock className="h-3 w-3" />
                Locked
              </p>
              {portfolioLoading ? (
                <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
              ) : (
                <p className="font-mono text-xl font-bold text-foreground">
                  {formatUsd(lockedUsd)}
                </p>
              )}
            </CardContent>
          </Card>

          {/* ARDA Balance */}
          <Card variant="neon" padding="sm" className="rounded-none">
            <CardContent className="p-0 space-y-1">
              <p className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                ARDA Balance
              </p>
              {ardaLoading ? (
                <Loader2 className="h-5 w-5 animate-spin text-neon-cyan" />
              ) : (
                <>
                  <p className="font-mono text-xl font-bold text-neon-cyan">
                    {formatInteger(ardaBalance?.balance ?? 0)}
                  </p>
                  <p className="font-mono text-[10px] text-muted-foreground">
                    {formatUsd(ardaBalance?.usdValue ?? 0)}
                  </p>
                </>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Holdings */}
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-border pb-2">
            <h2 className="font-mono text-sm uppercase tracking-widest text-foreground">
              Holdings
            </h2>
            {!portfolioLoading && (
              <Badge variant="outline" size="sm">
                {holdings.length} project{holdings.length !== 1 ? "s" : ""}
              </Badge>
            )}
          </div>

          {portfolioLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-neon-cyan" />
            </div>
          ) : portfolioError ? (
            <div className="rounded-none border-2 border-destructive/40 bg-destructive/10 p-6 text-center">
              <p className="font-mono text-sm text-destructive">
                Failed to load portfolio. Please refresh and try again.
              </p>
            </div>
          ) : holdings.length === 0 ? (
            <div className="rounded-none border-2 border-dashed border-border p-12 text-center">
              <Wallet className="mx-auto h-10 w-10 text-muted-foreground/30 mb-3" />
              <p className="font-mono text-sm text-muted-foreground">
                No holdings yet
              </p>
              <p className="font-mono text-xs text-muted-foreground/60 mt-1">
                Contribute to a project to earn equity tokens
              </p>
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2">
              {holdings.map((holding) => (
                <PortfolioHoldingCard
                  key={holding.projectTokenConfigId}
                  projectName={holding.projectName}
                  tokenAmount={holding.tokenAmount}
                  equityPct={holding.equityPct}
                  usdValue={holding.usdValue}
                  isLiquid={holding.isLiquid}
                  holderClass={holding.holderClass as HolderClass}
                  configId={holding.projectTokenConfigId}
                  onWithdraw={handleWithdraw}
                />
              ))}
            </div>
          )}
        </section>

        {/* Payout history */}
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-border pb-2">
            <h2 className="font-mono text-sm uppercase tracking-widest text-foreground">
              Payout History
            </h2>
            {!payoutsLoading && (
              <Badge variant="outline" size="sm">
                {(payouts ?? []).length} request
                {(payouts ?? []).length !== 1 ? "s" : ""}
              </Badge>
            )}
          </div>

          {payoutsLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-neon-cyan" />
            </div>
          ) : !payouts || payouts.length === 0 ? (
            <div className="rounded-none border-2 border-dashed border-border p-8 text-center">
              <p className="font-mono text-sm text-muted-foreground">
                No payout requests yet
              </p>
            </div>
          ) : (
            <div className="rounded-none border-2 border-border overflow-hidden">
              {/* Table header */}
              <div className="grid grid-cols-4 gap-3 bg-muted/30 px-4 py-2 border-b border-border">
                {["Date", "Project", "Amount", "Status"].map((col, i) => (
                  <p
                    key={col}
                    className={`font-mono text-[10px] uppercase tracking-widest text-muted-foreground ${i >= 2 ? "text-right" : ""}`}
                  >
                    {col}
                  </p>
                ))}
              </div>

              {/* Rows */}
              {payouts.map((payout, idx) => (
                <div
                  key={payout.id}
                  className={`grid grid-cols-4 gap-3 px-4 py-3 items-center ${
                    idx < payouts.length - 1 ? "border-b border-border" : ""
                  }`}
                >
                  <p className="font-mono text-xs text-muted-foreground">
                    {formatDate(payout.createdAt)}
                  </p>
                  <p className="font-mono text-xs text-foreground truncate">
                    {payout.projectName}
                  </p>
                  <p className="font-mono text-xs font-semibold text-foreground text-right">
                    {formatUsd(payout.usdAmount)}
                  </p>
                  <div className="flex justify-end">
                    <Badge
                      variant={statusBadgeVariant[payout.status] ?? "outline"}
                      size="sm"
                    >
                      {payout.status}
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
