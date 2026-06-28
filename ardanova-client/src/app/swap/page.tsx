"use client";

import { useState, useEffect, useCallback } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { Loader2, ArrowLeftRight, AlertCircle, CheckCircle2 } from "lucide-react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { SwapPreview } from "~/components/equity/swap-preview";
import { SwapHistory } from "~/components/equity/swap-history";

// Debounce hook
function useDebounce<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState<T>(value);
  useEffect(() => {
    const t = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(t);
  }, [value, delayMs]);
  return debounced;
}

export default function SwapPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const userId = session?.user?.id ?? "";

  // Form state
  const [sourceConfigId, setSourceConfigId] = useState<string>("");
  const [targetConfigId, setTargetConfigId] = useState<string>("");
  const [sourceAmountInput, setSourceAmountInput] = useState<string>("");
  const [swapSuccess, setSwapSuccess] = useState(false);
  const [swapError, setSwapError] = useState<string | null>(null);

  const sourceAmount = parseInt(sourceAmountInput, 10);
  const validAmount = !isNaN(sourceAmount) && sourceAmount > 0;

  // Debounced inputs for preview
  const debouncedSourceAmount = useDebounce(sourceAmount, 500);
  const debouncedSourceConfig = useDebounce(sourceConfigId, 300);
  const debouncedTargetConfig = useDebounce(targetConfigId, 300);

  const previewEnabled =
    !!userId &&
    !!debouncedSourceConfig &&
    !!debouncedTargetConfig &&
    !isNaN(debouncedSourceAmount) &&
    debouncedSourceAmount > 0 &&
    debouncedSourceConfig !== debouncedTargetConfig;

  // Data queries
  const { data: portfolio, isLoading: portfolioLoading } =
    api.tokenBalances.getPortfolio.useQuery(
      { userId },
      { enabled: !!userId },
    );

  const {
    data: preview,
    isLoading: previewLoading,
    error: previewError,
    refetch: refetchPreview,
  } = api.swap.getPreview.useQuery(
    {
      userId,
      sourceConfigId: debouncedSourceConfig,
      targetConfigId: debouncedTargetConfig,
      sourceTokenAmount: debouncedSourceAmount || 1,
    },
    { enabled: previewEnabled, retry: false },
  );

  const { data: history, isLoading: historyLoading, refetch: refetchHistory } =
    api.swap.getHistory.useQuery(
      { userId },
      { enabled: !!userId },
    );

  // Execute swap mutation
  const executeSwap = api.swap.executeSwap.useMutation({
    onSuccess: () => {
      setSwapSuccess(true);
      setSwapError(null);
      setSourceAmountInput("");
      void refetchHistory();
    },
    onError: (err) => {
      setSwapError(err.message);
      setSwapSuccess(false);
    },
  });

  const handleExecute = useCallback(() => {
    if (!previewEnabled || !validAmount) return;
    setSwapSuccess(false);
    setSwapError(null);
    executeSwap.mutate({
      userId,
      sourceConfigId,
      targetConfigId,
      sourceTokenAmount: sourceAmount,
    });
  }, [previewEnabled, validAmount, userId, sourceConfigId, targetConfigId, sourceAmount, executeSwap]);

  // Redirect if unauthenticated
  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.push("/auth/signin");
    }
  }, [sessionStatus, router]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-neon-cyan" />
      </div>
    );
  }

  if (!session) return null;

  // Build liquid contributor holdings for source dropdown
  type Holding = {
    projectTokenConfigId: string;
    tokenAmount: number;
    projectName?: string;
    unitName?: string;
    isLiquid?: boolean;
    holderClass?: string;
  };

  const liquidHoldings: Holding[] = ((portfolio as { holdings?: Holding[] } | undefined)?.holdings ?? []).filter(
    (h) => h.isLiquid && h.holderClass === "CONTRIBUTOR",
  );

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-3xl px-4 py-8 space-y-8">
        {/* Header */}
        <div>
          <h1 className="font-mono text-3xl font-bold uppercase tracking-tight text-foreground flex items-center gap-3">
            <div className="w-10 h-10 border-2 border-neon-cyan/60 bg-neon-cyan/10 flex items-center justify-center">
              <ArrowLeftRight className="h-5 w-5 text-neon-cyan" />
            </div>
            Token Swap
          </h1>
          <p className="font-mono text-sm text-muted-foreground mt-2">
            Exchange project tokens via ARDA intermediary
          </p>
        </div>

        {/* Swap form */}
        <Card variant="elevated" className="rounded-none">
          <CardHeader className="pb-4">
            <CardTitle className="font-mono text-sm uppercase tracking-widest text-foreground">
              Configure Swap
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {/* Source token */}
            <div className="space-y-2">
              <label className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Source Token (your liquid holdings)
              </label>
              {portfolioLoading ? (
                <div className="flex items-center gap-2">
                  <Loader2 className="h-4 w-4 animate-spin text-neon-cyan" />
                  <span className="font-mono text-xs text-muted-foreground">
                    Loading holdings...
                  </span>
                </div>
              ) : liquidHoldings.length === 0 ? (
                <div className="border border-dashed border-border p-3">
                  <p className="font-mono text-xs text-muted-foreground">
                    No liquid contributor holdings available for swap
                  </p>
                </div>
              ) : (
                <Select
                  value={sourceConfigId}
                  onValueChange={(v) => {
                    setSourceConfigId(v);
                    setSwapSuccess(false);
                    setSwapError(null);
                  }}
                >
                  <SelectTrigger className="font-mono rounded-none">
                    <SelectValue placeholder="Select source token..." />
                  </SelectTrigger>
                  <SelectContent>
                    {liquidHoldings.map((h) => (
                      <SelectItem
                        key={h.projectTokenConfigId}
                        value={h.projectTokenConfigId}
                        className="font-mono"
                      >
                        {h.unitName ?? h.projectName ?? h.projectTokenConfigId} —{" "}
                        {h.tokenAmount.toLocaleString()} liquid
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>

            {/* Amount input */}
            <div className="space-y-2">
              <label className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Amount to Swap
              </label>
              <Input
                type="number"
                min={1}
                step={1}
                placeholder="0"
                value={sourceAmountInput}
                onChange={(e) => {
                  setSourceAmountInput(e.target.value);
                  setSwapSuccess(false);
                  setSwapError(null);
                }}
                className="font-mono rounded-none"
              />
            </div>

            {/* Target token */}
            <div className="space-y-2">
              <label className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Target Token Config ID
              </label>
              <Input
                type="text"
                placeholder="Enter target project token config ID..."
                value={targetConfigId}
                onChange={(e) => {
                  setTargetConfigId(e.target.value.trim());
                  setSwapSuccess(false);
                  setSwapError(null);
                }}
                className="font-mono rounded-none text-xs"
              />
              <p className="font-mono text-[10px] text-muted-foreground/60">
                The config ID of the project token you want to receive
              </p>
            </div>

            {/* Validation notice */}
            {sourceConfigId && targetConfigId && sourceConfigId === targetConfigId && (
              <div className="flex items-center gap-2 border border-destructive/40 bg-destructive/10 px-3 py-2">
                <AlertCircle className="h-3.5 w-3.5 text-destructive shrink-0" />
                <p className="font-mono text-xs text-destructive">
                  Source and target tokens must be different
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Live preview */}
        {previewEnabled && (
          <section className="space-y-3">
            <div className="flex items-center justify-between border-b border-border pb-2">
              <h2 className="font-mono text-sm uppercase tracking-widest text-foreground">
                Conversion Preview
              </h2>
              {previewLoading && (
                <Loader2 className="h-4 w-4 animate-spin text-neon-cyan" />
              )}
            </div>

            {previewLoading && !preview && (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-neon-cyan" />
              </div>
            )}

            {previewError && !previewLoading && (
              <div className="flex items-start gap-2 border border-destructive/40 bg-destructive/10 px-4 py-3">
                <AlertCircle className="h-4 w-4 text-destructive shrink-0 mt-0.5" />
                <p className="font-mono text-xs text-destructive">
                  {previewError.message}
                </p>
              </div>
            )}

            {preview && !previewError && (
              <SwapPreview
                sourceAmount={preview.sourceTokenAmount}
                sourceUnit={preview.sourceUnitName}
                sourceUsd={preview.sourceUsdValue}
                ardaAmount={preview.ardaAmount}
                targetAmount={preview.targetTokenAmount}
                targetUnit={preview.targetUnitName}
                targetUsd={preview.targetUsdValue}
                sourceRate={preview.sourceTokenRate}
                targetRate={preview.targetTokenRate}
                ardaRate={preview.ardaRate}
              />
            )}
          </section>
        )}

        {/* Execute button + feedback */}
        <div className="space-y-3">
          {swapSuccess && (
            <div className="flex items-center gap-2 border border-neon-green/40 bg-neon-green/10 px-4 py-3">
              <CheckCircle2 className="h-4 w-4 text-neon-green shrink-0" />
              <p className="font-mono text-xs text-neon-green">
                Swap executed successfully
              </p>
            </div>
          )}

          {swapError && (
            <div className="flex items-start gap-2 border border-destructive/40 bg-destructive/10 px-4 py-3">
              <AlertCircle className="h-4 w-4 text-destructive shrink-0 mt-0.5" />
              <p className="font-mono text-xs text-destructive">{swapError}</p>
            </div>
          )}

          <Button
            onClick={handleExecute}
            disabled={
              !previewEnabled ||
              !preview ||
              !validAmount ||
              executeSwap.isPending ||
              preview.targetTokenAmount === 0
            }
            className="w-full font-mono uppercase tracking-widest rounded-none"
            variant="default"
          >
            {executeSwap.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Executing Swap...
              </>
            ) : (
              "Execute Swap"
            )}
          </Button>
        </div>

        {/* Swap history */}
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-border pb-2">
            <h2 className="font-mono text-sm uppercase tracking-widest text-foreground">
              Swap History
            </h2>
            {!historyLoading && (
              <Badge variant="outline" size="sm">
                {(history ?? []).length} swap{(history ?? []).length !== 1 ? "s" : ""}
              </Badge>
            )}
          </div>

          {historyLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-neon-cyan" />
            </div>
          ) : (
            <SwapHistory
              swaps={(history ?? []).map((s) => ({
                id: s.id,
                sourceUnitName: s.sourceUnitName,
                sourceTokenAmount: s.sourceTokenAmount,
                targetUnitName: s.targetUnitName,
                targetTokenAmount: s.targetTokenAmount,
                sourceUsdValue: s.sourceUsdValue,
                targetUsdValue: s.targetUsdValue,
                createdAt: s.createdAt,
                status: s.status as
                  | "PENDING"
                  | "PROCESSING"
                  | "COMPLETED"
                  | "FAILED"
                  | "CANCELLED",
              }))}
            />
          )}
        </section>
      </div>
    </div>
  );
}
