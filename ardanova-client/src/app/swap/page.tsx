"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { useSession } from "next-auth/react";
import { buildSignInHref } from "~/lib/auth-navigation";
import { useRouter } from "next/navigation";
import {
  Loader2,
  ArrowLeftRight,
  AlertCircle,
  CheckCircle2,
} from "lucide-react";
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
import type { TokenBalanceDto } from "~/lib/api/ardanova/endpoints/token-balances";

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
  const [swapSubmitted, setSwapSubmitted] = useState(false);
  const [swapError, setSwapError] = useState<string | null>(null);

  const sourceAmount = Number(sourceAmountInput);

  // Debounced inputs for preview
  const debouncedSourceAmount = useDebounce(sourceAmount, 500);
  const debouncedSourceConfig = useDebounce(sourceConfigId, 300);
  const debouncedTargetConfig = useDebounce(targetConfigId, 300);

  // Data queries
  const {
    data: portfolio,
    isLoading: portfolioLoading,
    error: portfolioError,
    refetch: refetchPortfolio,
  } = api.tokenBalances.getPortfolio.useQuery(undefined, {
    enabled: !!userId,
    retry: false,
  });

  const {
    data: history,
    isLoading: historyLoading,
    error: historyError,
    refetch: refetchHistory,
  } = api.swap.getHistory.useQuery(undefined, {
    enabled: !!userId,
    retry: false,
  });

  const liquidHoldings = useMemo(
    () =>
      (portfolio?.holdings ?? []).filter(
        (
          holding,
        ): holding is TokenBalanceDto & { projectTokenConfigId: string } =>
          holding.isLiquid &&
          holding.holderClass === "CONTRIBUTOR" &&
          typeof holding.projectTokenConfigId === "string" &&
          holding.projectTokenConfigId.length > 0 &&
          Number.isFinite(holding.availableBalance) &&
          holding.availableBalance > 0,
      ),
    [portfolio?.holdings],
  );
  const sourceConfigIds = useMemo(
    () => [
      ...new Set(liquidHoldings.map((holding) => holding.projectTokenConfigId)),
    ],
    [liquidHoldings],
  );
  const { data: sourceMetadata, error: sourceMetadataError } =
    api.projectTokens.getMetadata.useQuery(
      { ids: sourceConfigIds.length > 0 ? sourceConfigIds : ["unavailable"] },
      { enabled: !!userId && sourceConfigIds.length > 0, retry: false },
    );
  const sourceConfigLabels = useMemo(
    () =>
      new Map(
        (sourceMetadata?.items ?? []).map((config) => [
          config.id,
          config.unitName?.trim() || config.assetName?.trim() || config.id,
        ]),
      ),
    [sourceMetadata?.items],
  );
  const selectedHolding = liquidHoldings.find(
    (holding) => holding.projectTokenConfigId === sourceConfigId,
  );
  const validAmount =
    Number.isSafeInteger(sourceAmount) &&
    sourceAmount > 0 &&
    (!selectedHolding || sourceAmount <= selectedHolding.availableBalance);
  const previewEnabled =
    !!userId &&
    !portfolioError &&
    !!selectedHolding &&
    !!debouncedSourceConfig &&
    !!debouncedTargetConfig &&
    debouncedSourceConfig === selectedHolding.projectTokenConfigId &&
    debouncedSourceAmount === sourceAmount &&
    debouncedTargetConfig === targetConfigId &&
    Number.isSafeInteger(debouncedSourceAmount) &&
    debouncedSourceAmount > 0 &&
    debouncedSourceAmount <= selectedHolding.availableBalance &&
    debouncedSourceConfig !== debouncedTargetConfig;

  const {
    data: preview,
    isLoading: previewLoading,
    error: previewError,
  } = api.swap.getPreview.useQuery(
    {
      sourceConfigId: debouncedSourceConfig,
      targetConfigId: debouncedTargetConfig,
      sourceTokenAmount: debouncedSourceAmount || 1,
    },
    { enabled: previewEnabled, retry: false },
  );

  // Execute swap mutation
  const executeSwap = api.swap.executeSwap.useMutation({
    onSuccess: () => {
      setSwapSubmitted(true);
      setSwapError(null);
      setSourceAmountInput("");
      void Promise.all([refetchHistory(), refetchPortfolio()]);
    },
    onError: (err) => {
      setSwapError(err.message);
      setSwapSubmitted(false);
    },
  });

  const handleExecute = useCallback(() => {
    if (!previewEnabled || !validAmount) return;
    setSwapSubmitted(false);
    setSwapError(null);
    executeSwap.mutate({
      sourceConfigId,
      targetConfigId,
      sourceTokenAmount: sourceAmount,
    });
  }, [
    previewEnabled,
    validAmount,
    sourceConfigId,
    targetConfigId,
    sourceAmount,
    executeSwap,
  ]);

  // Redirect if unauthenticated
  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.push(buildSignInHref("/swap"));
    }
  }, [sessionStatus, router]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-neon-cyan h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl space-y-8 px-4 py-8">
        {/* Header */}
        <div>
          <h1 className="text-foreground flex items-center gap-3 font-mono text-3xl font-bold tracking-tight uppercase">
            <div className="border-neon-cyan/60 bg-neon-cyan/10 flex h-10 w-10 items-center justify-center border-2">
              <ArrowLeftRight className="text-neon-cyan h-5 w-5" />
            </div>
            Token Swap
          </h1>
          <p className="text-muted-foreground mt-2 font-mono text-sm">
            Submit a project-token conversion request through the ARDA utility
            intermediary
          </p>
          <p className="border-primary text-muted-foreground mt-3 border-l-2 pl-3 text-sm">
            A submitted request is not a completed swap. Confirm the processing
            status below and use reconciled portfolio balances as the source of
            truth.
          </p>
        </div>

        {/* Swap form */}
        <Card variant="elevated" className="rounded-none">
          <CardHeader className="pb-4">
            <CardTitle className="text-foreground font-mono text-sm tracking-widest uppercase">
              Configure Swap
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {/* Source token */}
            <div className="space-y-2">
              <label
                htmlFor="swap-source-token"
                className="text-muted-foreground font-mono text-xs tracking-widest uppercase"
              >
                Source Token (your liquid holdings)
              </label>
              {portfolioLoading ? (
                <div className="flex items-center gap-2">
                  <Loader2 className="text-neon-cyan h-4 w-4 animate-spin" />
                  <span className="text-muted-foreground font-mono text-xs">
                    Loading holdings...
                  </span>
                </div>
              ) : portfolioError ? (
                <div
                  role="alert"
                  className="border-destructive bg-destructive/10 space-y-3 border-2 p-3"
                >
                  <p className="text-destructive text-sm">
                    Holdings could not be loaded. No available-balance claim is
                    shown and swap submission stays disabled.
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => void refetchPortfolio()}
                  >
                    Retry holdings
                  </Button>
                </div>
              ) : liquidHoldings.length === 0 ? (
                <div className="border-border border border-dashed p-3">
                  <p className="text-muted-foreground font-mono text-xs">
                    No liquid contributor holdings available for swap
                  </p>
                </div>
              ) : (
                <Select
                  value={sourceConfigId}
                  onValueChange={(v) => {
                    setSourceConfigId(v);
                    setSwapSubmitted(false);
                    setSwapError(null);
                  }}
                >
                  <SelectTrigger
                    id="swap-source-token"
                    className="min-h-11 rounded-none font-mono"
                  >
                    <SelectValue placeholder="Select source token..." />
                  </SelectTrigger>
                  <SelectContent>
                    {liquidHoldings.map((holding) => (
                      <SourceHoldingOption
                        key={holding.id}
                        holding={holding}
                        label={
                          sourceConfigLabels.get(
                            holding.projectTokenConfigId,
                          ) ?? holding.projectTokenConfigId
                        }
                      />
                    ))}
                  </SelectContent>
                </Select>
              )}
              {sourceMetadataError && liquidHoldings.length > 0 && (
                <p className="text-muted-foreground text-xs" role="status">
                  Token names are temporarily unavailable; verified config IDs
                  are shown instead.
                </p>
              )}
              {!sourceMetadataError &&
                (sourceMetadata?.missingIds.length ?? 0) > 0 && (
                  <p className="text-muted-foreground text-xs" role="status">
                    Some token names are not available from the current metadata
                    contract; their verified config IDs are shown instead.
                  </p>
                )}
            </div>

            {/* Amount input */}
            <div className="space-y-2">
              <label
                htmlFor="swap-source-amount"
                className="text-muted-foreground font-mono text-xs tracking-widest uppercase"
              >
                Amount to Swap
              </label>
              <Input
                id="swap-source-amount"
                type="number"
                min={1}
                max={selectedHolding?.availableBalance}
                step={1}
                placeholder="0"
                value={sourceAmountInput}
                onChange={(e) => {
                  setSourceAmountInput(e.target.value);
                  setSwapSubmitted(false);
                  setSwapError(null);
                }}
                className="min-h-11 rounded-none font-mono"
                aria-invalid={sourceAmountInput.length > 0 && !validAmount}
                aria-describedby="swap-source-amount-help"
              />
              <p
                id="swap-source-amount-help"
                className="text-muted-foreground text-xs"
              >
                {selectedHolding
                  ? `${selectedHolding.balance.toLocaleString()} total · ${selectedHolding.availableBalance.toLocaleString()} available. Enter a whole number no greater than the available balance.`
                  : "Choose a source holding, then enter a positive whole number."}
              </p>
              {sourceAmountInput.length > 0 && !validAmount && (
                <p className="text-destructive text-xs" role="alert">
                  Enter a positive whole-number amount within the recorded
                  available balance.
                </p>
              )}
            </div>

            {/* Target token */}
            <div className="space-y-2">
              <label
                htmlFor="swap-target-token"
                className="text-muted-foreground font-mono text-xs tracking-widest uppercase"
              >
                Target Token Config ID
              </label>
              <Input
                id="swap-target-token"
                type="text"
                placeholder="Enter target project token config ID..."
                value={targetConfigId}
                onChange={(e) => {
                  setTargetConfigId(e.target.value.trim());
                  setSwapSubmitted(false);
                  setSwapError(null);
                }}
                aria-describedby="swap-target-token-help"
                className="min-h-11 rounded-none font-mono text-xs"
              />
              <p
                id="swap-target-token-help"
                className="text-muted-foreground font-mono text-xs"
              >
                The config ID of the project token you want to receive
              </p>
            </div>

            {/* Validation notice */}
            {sourceConfigId &&
              targetConfigId &&
              sourceConfigId === targetConfigId && (
                <div
                  role="alert"
                  className="border-destructive/40 bg-destructive/10 flex items-center gap-2 border px-3 py-2"
                >
                  <AlertCircle className="text-destructive h-3.5 w-3.5 shrink-0" />
                  <p className="text-destructive font-mono text-xs">
                    Source and target tokens must be different
                  </p>
                </div>
              )}
          </CardContent>
        </Card>

        {/* Live preview */}
        {previewEnabled && (
          <section className="space-y-3">
            <div className="border-border flex items-center justify-between border-b pb-2">
              <h2 className="text-foreground font-mono text-sm tracking-widest uppercase">
                Conversion Preview
              </h2>
              {previewLoading && (
                <Loader2 className="text-neon-cyan h-4 w-4 animate-spin" />
              )}
            </div>

            {previewLoading && !preview && (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="text-neon-cyan h-6 w-6 animate-spin" />
              </div>
            )}

            {previewError && !previewLoading && (
              <div className="border-destructive/40 bg-destructive/10 flex items-start gap-2 border px-4 py-3">
                <AlertCircle className="text-destructive mt-0.5 h-4 w-4 shrink-0" />
                <p className="text-destructive font-mono text-xs">
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
          {swapSubmitted && (
            <div
              role="status"
              className="border-neon-green/40 bg-neon-green/10 flex items-start gap-2 border px-4 py-3"
            >
              <CheckCircle2 className="text-neon-green h-4 w-4 shrink-0" />
              <p className="text-neon-green font-mono text-xs">
                Swap request accepted. Track processing and completion below;
                balances are not final until reconciliation is recorded.
              </p>
            </div>
          )}

          {swapError && (
            <div
              role="alert"
              className="border-destructive/40 bg-destructive/10 flex items-start gap-2 border px-4 py-3"
            >
              <AlertCircle className="text-destructive mt-0.5 h-4 w-4 shrink-0" />
              <p className="text-destructive font-mono text-xs">{swapError}</p>
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
            className="min-h-11 w-full rounded-none font-mono tracking-widest uppercase"
            variant="default"
          >
            {executeSwap.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Submitting Request...
              </>
            ) : (
              "Submit Swap Request"
            )}
          </Button>
        </div>

        {/* Swap history */}
        <section className="space-y-4">
          <div className="border-border flex items-center justify-between border-b pb-2">
            <h2 className="text-foreground font-mono text-sm tracking-widest uppercase">
              Swap History
            </h2>
            {!historyLoading && !historyError && (
              <Badge variant="outline" size="sm">
                {(history ?? []).length} swap
                {(history ?? []).length !== 1 ? "s" : ""}
              </Badge>
            )}
          </div>

          {historyLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="text-neon-cyan h-6 w-6 animate-spin" />
            </div>
          ) : historyError ? (
            <div
              role="alert"
              className="border-destructive bg-destructive/10 space-y-3 border-2 p-4"
            >
              <p className="text-destructive text-sm">
                Swap history could not be loaded. This is not an empty history,
                and no prior-request claim is shown.
              </p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => void refetchHistory()}
              >
                Retry history
              </Button>
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
                status: s.status,
              }))}
            />
          )}
        </section>
      </div>
    </div>
  );
}

function SourceHoldingOption({
  holding,
  label,
}: {
  holding: TokenBalanceDto & { projectTokenConfigId: string };
  label: string;
}) {
  return (
    <SelectItem
      value={holding.projectTokenConfigId}
      className="min-h-11 font-mono"
    >
      {label} — {holding.balance.toLocaleString()} total ·{" "}
      {holding.availableBalance.toLocaleString()} available
    </SelectItem>
  );
}
