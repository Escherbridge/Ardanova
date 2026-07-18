"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "~/components/ui/dialog";
import { TreasuryBuckets } from "~/components/equity/treasury-buckets";
import { TreasuryTransactionLog } from "~/components/equity/treasury-transaction-log";
import { Loader2, RefreshCw, TrendingUp, Scale } from "lucide-react";

// ---------------------------------------------------------------------------
// Local type helpers — mirrors what the .NET API returns but with concrete
// types so we don't propagate `unknown` through the component tree.
// ---------------------------------------------------------------------------
interface TreasuryStatus {
  indexFundUsd: number;
  liquidReserveUsd: number;
  operationsUsd: number;
  totalUsd: number;
  ardaSupply: number;
  ardaValueUsd: number;
}

const treasuryTransactionTypes = [
  "FUNDING_INFLOW",
  "ALLOCATION_INDEX",
  "ALLOCATION_LIQUID",
  "ALLOCATION_OPS",
  "PAYOUT_DEBIT",
  "INDEX_RETURN",
  "PROFIT_SHARE",
  "REBALANCE",
  "TRUST_PROTECTION",
  "FOUNDER_BURN",
] as const;

type TreasuryTransactionType = (typeof treasuryTransactionTypes)[number];

interface TreasuryTransaction {
  id: string;
  type: TreasuryTransactionType;
  amountUsd: number;
  description: string;
  createdAt: Date | string;
  projectId?: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function isTreasuryTransactionType(
  value: unknown,
): value is TreasuryTransactionType {
  return (
    typeof value === "string" &&
    treasuryTransactionTypes.some((candidate) => candidate === value)
  );
}

function readDate(value: unknown): Date | string {
  return value instanceof Date || typeof value === "string"
    ? value
    : new Date();
}

function asStatus(raw: unknown): TreasuryStatus {
  const r = isRecord(raw) ? raw : {};
  return {
    indexFundUsd: Number(r.indexFundUsd ?? 0),
    liquidReserveUsd: Number(r.liquidReserveUsd ?? 0),
    operationsUsd: Number(r.operationsUsd ?? 0),
    totalUsd: Number(r.totalUsd ?? 0),
    ardaSupply: Number(r.ardaSupply ?? 0),
    ardaValueUsd: Number(r.ardaValueUsd ?? 0),
  };
}

function asTransactions(raw: unknown): TreasuryTransaction[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item: unknown) => {
    if (!isRecord(item) || !isTreasuryTransactionType(item.type)) return [];

    const r = item;
    return {
      id: readString(r.id),
      type: item.type,
      amountUsd: Number(r.amountUsd ?? 0),
      description: readString(r.description),
      createdAt: readDate(r.createdAt),
      projectId: typeof r.projectId === "string" ? r.projectId : undefined,
    } satisfies TreasuryTransaction;
  });
}

export default function TreasuryDashboard() {
  const { data: session } = useSession();
  const [txFilter, setTxFilter] = useState("ALL");
  const [requiredLiquid, setRequiredLiquid] = useState("");

  // Dialog open states
  const [indexReturnOpen, setIndexReturnOpen] = useState(false);
  const [rebalanceOpen, setRebalanceOpen] = useState(false);
  const [reconcileOpen, setReconcileOpen] = useState(false);

  const utils = api.useUtils();

  const {
    data: status,
    isLoading: statusLoading,
    error: statusError,
  } = api.treasury.getStatus.useQuery();

  const { data: transactions, isLoading: txLoading } =
    api.treasury.getTransactions.useQuery({ limit: 100 });

  const applyIndexReturn = api.treasury.applyIndexReturn.useMutation({
    onSuccess: () => {
      void utils.treasury.getStatus.invalidate();
      void utils.treasury.getTransactions.invalidate();
      setIndexReturnOpen(false);
    },
  });

  const rebalance = api.treasury.rebalance.useMutation({
    onSuccess: () => {
      void utils.treasury.getStatus.invalidate();
      void utils.treasury.getTransactions.invalidate();
      setRebalanceOpen(false);
      setRequiredLiquid("");
    },
  });

  const reconcile = api.treasury.reconcile.useMutation({
    onSuccess: () => {
      void utils.treasury.getStatus.invalidate();
      void utils.treasury.getTransactions.invalidate();
      setReconcileOpen(false);
    },
  });

  // Admin guard
  if (!session) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-system h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (session.user?.role !== "ADMIN") {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <div className="border-destructive border-2 px-8 py-6 text-center">
          <p className="text-destructive font-mono text-xl font-bold">
            UNAUTHORIZED
          </p>
          <p className="text-muted-foreground mt-2 text-sm">
            Admin access required to view treasury data.
          </p>
        </div>
      </div>
    );
  }

  if (statusLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-system h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (statusError || !status) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="border-destructive border-2 px-8 py-6 text-center">
          <p className="text-destructive font-mono">
            Failed to load treasury data
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {statusError?.message ?? "Unknown error"}
          </p>
        </div>
      </div>
    );
  }

  const rebalanceLiquidAmount = parseFloat(requiredLiquid);
  const isRebalanceValid =
    !isNaN(rebalanceLiquidAmount) && rebalanceLiquidAmount > 0;

  // Coerce loosely-typed API responses to concrete shapes
  const typedStatus = asStatus(status);
  const typedTransactions = asTransactions(transactions);

  return (
    <div className="container mx-auto max-w-7xl space-y-10 py-8">
      {/* Header */}
      <div className="space-y-2 border-b-2 border-white/10 pb-6">
        <h1 className="font-mono text-3xl font-bold tracking-tight uppercase">
          Treasury Dashboard
        </h1>
        <p className="text-muted-foreground text-sm">
          Real-time overview of ARDA treasury allocation buckets and transaction
          history.
        </p>
      </div>

      {/* Bucket visualization */}
      <section className="space-y-3">
        <h2 className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
          Allocation Buckets
        </h2>
        <TreasuryBuckets
          indexFundUsd={typedStatus.indexFundUsd}
          liquidReserveUsd={typedStatus.liquidReserveUsd}
          operationsUsd={typedStatus.operationsUsd}
          totalUsd={typedStatus.totalUsd}
          ardaSupply={typedStatus.ardaSupply}
          ardaValueUsd={typedStatus.ardaValueUsd}
        />
      </section>

      {/* Admin actions */}
      <section className="space-y-3">
        <h2 className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
          Admin Actions
        </h2>
        <div className="flex flex-wrap gap-3">
          {/* Apply Index Return */}
          <Dialog open={indexReturnOpen} onOpenChange={setIndexReturnOpen}>
            <DialogTrigger asChild>
              <Button
                variant="outline"
                className="border-success text-success hover:bg-success/10 border-2 font-mono"
              >
                <TrendingUp className="mr-2 h-4 w-4" />
                Apply Index Return
              </Button>
            </DialogTrigger>
            <DialogContent className="border-2 border-white/20">
              <DialogHeader>
                <DialogTitle className="font-mono">
                  Apply Index Return
                </DialogTitle>
                <DialogDescription>
                  Distribute index fund returns proportionally to all ARDA
                  holders. This action cannot be undone.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setIndexReturnOpen(false)}
                  disabled={applyIndexReturn.isPending}
                >
                  Cancel
                </Button>
                <Button
                  className="border-success text-success hover:bg-success/20 border-2 bg-transparent font-mono"
                  onClick={() => applyIndexReturn.mutate()}
                  disabled={applyIndexReturn.isPending}
                >
                  {applyIndexReturn.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <TrendingUp className="mr-2 h-4 w-4" />
                  )}
                  Confirm
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          {/* Rebalance */}
          <Dialog open={rebalanceOpen} onOpenChange={setRebalanceOpen}>
            <DialogTrigger asChild>
              <Button
                variant="outline"
                className="border-system text-system hover:bg-system/10 border-2 font-mono"
              >
                <Scale className="mr-2 h-4 w-4" />
                Rebalance
              </Button>
            </DialogTrigger>
            <DialogContent className="border-2 border-white/20">
              <DialogHeader>
                <DialogTitle className="font-mono">
                  Rebalance Treasury
                </DialogTitle>
                <DialogDescription>
                  Rebalance bucket allocations. Specify the required liquid
                  reserve amount in USD.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-2 py-2">
                <label className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  Required Liquid (USD)
                </label>
                <Input
                  type="number"
                  placeholder="e.g. 25000"
                  value={requiredLiquid}
                  onChange={(e) => setRequiredLiquid(e.target.value)}
                  className="border-2 border-white/20 font-mono"
                  min="0"
                  step="0.01"
                />
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => {
                    setRebalanceOpen(false);
                    setRequiredLiquid("");
                  }}
                  disabled={rebalance.isPending}
                >
                  Cancel
                </Button>
                <Button
                  className="border-system text-system hover:bg-system/20 border-2 bg-transparent font-mono"
                  onClick={() => {
                    if (isRebalanceValid) {
                      rebalance.mutate({
                        requiredLiquid: rebalanceLiquidAmount,
                      });
                    }
                  }}
                  disabled={!isRebalanceValid || rebalance.isPending}
                >
                  {rebalance.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Scale className="mr-2 h-4 w-4" />
                  )}
                  Confirm Rebalance
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

          {/* Reconcile */}
          <Dialog open={reconcileOpen} onOpenChange={setReconcileOpen}>
            <DialogTrigger asChild>
              <Button
                variant="outline"
                className="border-2 border-white/40 font-mono text-white hover:bg-white/10"
              >
                <RefreshCw className="mr-2 h-4 w-4" />
                Reconcile
              </Button>
            </DialogTrigger>
            <DialogContent className="border-2 border-white/20">
              <DialogHeader>
                <DialogTitle className="font-mono">
                  Reconcile Treasury
                </DialogTitle>
                <DialogDescription>
                  Run a full reconciliation pass to sync on-chain and off-chain
                  balances. This may take a few seconds.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setReconcileOpen(false)}
                  disabled={reconcile.isPending}
                >
                  Cancel
                </Button>
                <Button
                  className="border-2 border-white/40 bg-transparent font-mono hover:bg-white/10"
                  onClick={() => reconcile.mutate()}
                  disabled={reconcile.isPending}
                >
                  {reconcile.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <RefreshCw className="mr-2 h-4 w-4" />
                  )}
                  Confirm Reconcile
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>

        {/* Mutation error display */}
        {(applyIndexReturn.isError ||
          rebalance.isError ||
          reconcile.isError) && (
          <p className="text-destructive font-mono text-xs">
            Error:{" "}
            {(applyIndexReturn.error ?? rebalance.error ?? reconcile.error)
              ?.message ?? "Unknown error"}
          </p>
        )}
      </section>

      {/* Transaction log */}
      <section className="space-y-3">
        <h2 className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
          Transaction Audit Log
        </h2>
        {txLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="text-system h-6 w-6 animate-spin" />
          </div>
        ) : (
          <TreasuryTransactionLog
            transactions={typedTransactions}
            filterType={txFilter}
            onFilterChange={setTxFilter}
          />
        )}
      </section>
    </div>
  );
}
