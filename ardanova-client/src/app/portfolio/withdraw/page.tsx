"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { ArrowLeft, Loader2, AlertCircle, TrendingUp } from "lucide-react";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { ConversionChain } from "~/components/equity/conversion-chain";
import { PayoutStatusTracker } from "~/components/equity/payout-status-tracker";
import { cn } from "~/lib/utils";

type HolderClass = "CONTRIBUTOR" | "INVESTOR" | "FOUNDER";
type PayoutStatus =
  | "PENDING"
  | "PROCESSING"
  | "COMPLETED"
  | "FAILED"
  | "CANCELLED";

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

interface ConversionPreview {
  projectTokens: number;
  ardaAmount: number;
  usdAmount: number;
  tokenRate: number;
  ardaRate: number;
}

interface CreatedPayout {
  id: string;
  status: PayoutStatus;
  usdAmount: number;
}

const QUICK_PCT = [25, 50, 75, 100] as const;
const TOKEN_TICKER = "TOKENS";

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

function holderClassColor(hc: HolderClass): string {
  if (hc === "CONTRIBUTOR") return "text-neon-green";
  if (hc === "INVESTOR") return "text-neon-cyan";
  return "text-neon-pink";
}

export default function WithdrawPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const searchParams = useSearchParams();

  const configId = searchParams.get("configId") ?? "";
  const holderClassParam = (searchParams.get("holderClass") ??
    "CONTRIBUTOR") as HolderClass;

  const userId = session?.user?.id ?? "";

  const [rawAmount, setRawAmount] = useState("");
  const [debouncedAmount, setDebouncedAmount] = useState(0);
  const [inputError, setInputError] = useState<string | null>(null);
  const [createdPayout, setCreatedPayout] = useState<CreatedPayout | null>(
    null,
  );

  // Debounce 400 ms
  useEffect(() => {
    const parsed = parseFloat(rawAmount);
    if (!rawAmount || isNaN(parsed) || parsed <= 0) {
      setDebouncedAmount(0);
      return;
    }
    const timer = setTimeout(() => setDebouncedAmount(parsed), 400);
    return () => clearTimeout(timer);
  }, [rawAmount]);

  const { data: portfolioRaw, isLoading: portfolioLoading } =
    api.tokenBalances.getPortfolio.useQuery(
      { userId },
      { enabled: !!userId },
    );

  const portfolio = portfolioRaw as unknown as PortfolioData | undefined;
  const holding = portfolio?.holdings.find(
    (h) => h.projectTokenConfigId === configId,
  );

  const {
    data: previewRaw,
    isLoading: previewLoading,
    error: previewError,
  } = api.exchange.getConversionPreview.useQuery(
    { projectTokenConfigId: configId, tokenAmount: debouncedAmount },
    { enabled: !!configId && debouncedAmount > 0 },
  );

  const preview = previewRaw as unknown as ConversionPreview | undefined;

  const requestPayout = api.payouts.requestPayout.useMutation({
    onSuccess: (data) => {
      const result = data as { id: string };
      setCreatedPayout({
        id: result.id ?? "unknown",
        status: "PENDING",
        usdAmount: preview?.usdAmount ?? 0,
      });
    },
  });

  const cancelPayout = api.payouts.cancelPayout.useMutation({
    onSuccess: () => {
      if (createdPayout) {
        setCreatedPayout({ ...createdPayout, status: "CANCELLED" });
      }
    },
  });

  function applyQuickPct(pct: number) {
    if (!holding) return;
    const amount = Math.floor((holding.tokenAmount * pct) / 100);
    setRawAmount(String(amount));
    setInputError(null);
  }

  function validate(): boolean {
    const parsed = parseFloat(rawAmount);
    if (!rawAmount || isNaN(parsed) || parsed <= 0) {
      setInputError("Enter a valid token amount");
      return false;
    }
    if (holding && parsed > holding.tokenAmount) {
      setInputError(`Maximum is ${formatInteger(holding.tokenAmount)} tokens`);
      return false;
    }
    setInputError(null);
    return true;
  }

  function handleSubmit() {
    if (!validate()) return;
    const parsed = parseFloat(rawAmount);
    requestPayout.mutate({
      userId,
      sourceProjectTokenConfigId: configId,
      sourceTokenAmount: Math.floor(parsed),
      holderClass: holderClassParam,
    });
  }

  if (sessionStatus === "loading" || portfolioLoading) {
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

  if (!configId) {
    return (
      <div className="flex min-h-screen items-center justify-center px-4">
        <div className="rounded-none border-2 border-destructive/50 bg-destructive/10 p-8 max-w-sm text-center space-y-3">
          <AlertCircle className="mx-auto h-8 w-8 text-destructive" />
          <p className="font-mono text-sm text-destructive">
            Missing token configuration. Return to portfolio and try again.
          </p>
          <Button variant="outline" asChild className="w-full">
            <Link href="/portfolio">Back to Portfolio</Link>
          </Button>
        </div>
      </div>
    );
  }

  if (!holding) {
    return (
      <div className="flex min-h-screen items-center justify-center px-4">
        <div className="rounded-none border-2 border-border bg-muted/20 p-8 max-w-sm text-center space-y-3">
          <AlertCircle className="mx-auto h-8 w-8 text-muted-foreground" />
          <p className="font-mono text-sm text-muted-foreground">
            Holding not found or no longer available.
          </p>
          <Button variant="outline" asChild className="w-full">
            <Link href="/portfolio">Back to Portfolio</Link>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="mx-auto max-w-xl px-4 py-8 space-y-6">
        {/* Header */}
        <div>
          <Button variant="ghost" asChild className="-ml-2 mb-4">
            <Link href="/portfolio">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Portfolio
            </Link>
          </Button>

          <h1 className="font-mono text-2xl font-bold uppercase tracking-tight text-foreground flex items-center gap-3">
            <div className="w-9 h-9 border-2 border-neon-green/60 bg-neon-green/10 flex items-center justify-center">
              <TrendingUp className="h-4 w-4 text-neon-green" />
            </div>
            Withdraw
          </h1>
          <p className="font-mono text-sm text-muted-foreground mt-1">
            {holding.projectName}
            {" · "}
            <span
              className={cn("font-semibold", holderClassColor(holderClassParam))}
            >
              {holderClassParam}
            </span>
          </p>
        </div>

        {createdPayout ? (
          /* Post-submit: status tracker */
          <div className="space-y-4">
            <PayoutStatusTracker
              status={createdPayout.status}
              payoutId={createdPayout.id}
              usdAmount={createdPayout.usdAmount}
              onCancel={
                createdPayout.status === "PENDING"
                  ? () =>
                      cancelPayout.mutate({
                        payoutRequestId: createdPayout.id,
                      })
                  : undefined
              }
              isCancelling={cancelPayout.isPending}
            />
            {(createdPayout.status === "COMPLETED" ||
              createdPayout.status === "CANCELLED" ||
              createdPayout.status === "FAILED") && (
              <Button
                variant="outline"
                asChild
                className="w-full rounded-none font-mono text-xs uppercase tracking-widest"
              >
                <Link href="/portfolio">Back to Portfolio</Link>
              </Button>
            )}
          </div>
        ) : (
          /* Pre-submit: form */
          <>
            {/* Available balance */}
            <Card className="rounded-none border-2 border-neon-green/30 bg-neon-green/5">
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
                    Available to Withdraw
                  </p>
                  <div className="text-right">
                    <p className="font-mono text-lg font-bold text-neon-green">
                      {formatInteger(holding.tokenAmount)} {TOKEN_TICKER}
                    </p>
                    <p className="font-mono text-xs text-muted-foreground">
                      {formatUsd(holding.usdValue)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Amount input */}
            <Card className="rounded-none" padding="none">
              <CardHeader className="p-4 pb-2">
                <CardTitle className="font-mono text-sm uppercase tracking-widest">
                  Enter Amount
                </CardTitle>
              </CardHeader>
              <CardContent className="p-4 pt-0 space-y-3">
                {/* Quick % buttons */}
                <div className="grid grid-cols-4 gap-2">
                  {QUICK_PCT.map((pct) => (
                    <button
                      key={pct}
                      type="button"
                      onClick={() => applyQuickPct(pct)}
                      className="rounded-none border-2 border-border bg-muted/20 py-1.5 font-mono text-xs uppercase tracking-wide text-foreground hover:border-neon-cyan/60 hover:bg-neon-cyan/5 hover:text-neon-cyan transition-colors"
                    >
                      {pct}%
                    </button>
                  ))}
                </div>

                {/* Text input */}
                <div className="space-y-1">
                  <div className="relative">
                    <Input
                      type="number"
                      min={1}
                      max={holding.tokenAmount}
                      step={1}
                      value={rawAmount}
                      onChange={(e) => {
                        setRawAmount(e.target.value);
                        setInputError(null);
                      }}
                      placeholder={`0 — max ${formatInteger(holding.tokenAmount)}`}
                      className={cn(
                        "rounded-none border-2 font-mono pr-20",
                        inputError ? "border-destructive" : "border-border",
                      )}
                    />
                    <span className="absolute right-3 top-1/2 -translate-y-1/2 font-mono text-xs text-muted-foreground pointer-events-none">
                      {TOKEN_TICKER}
                    </span>
                  </div>
                  {inputError && (
                    <p className="font-mono text-xs text-destructive">
                      {inputError}
                    </p>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Conversion preview */}
            {debouncedAmount > 0 && (
              <div className="relative">
                {previewLoading && (
                  <div className="absolute inset-0 z-10 flex items-center justify-center bg-background/60 rounded-none">
                    <Loader2 className="h-5 w-5 animate-spin text-neon-cyan" />
                  </div>
                )}
                {previewError ? (
                  <div className="rounded-none border-2 border-destructive/40 bg-destructive/10 p-4">
                    <p className="font-mono text-xs text-destructive flex items-center gap-2">
                      <AlertCircle className="h-3.5 w-3.5" />
                      Could not load conversion preview
                    </p>
                  </div>
                ) : preview ? (
                  <ConversionChain
                    projectTokens={preview.projectTokens}
                    tokenTicker={TOKEN_TICKER}
                    ardaAmount={preview.ardaAmount}
                    usdAmount={preview.usdAmount}
                    tokenRate={preview.tokenRate}
                    ardaRate={preview.ardaRate}
                  />
                ) : null}
              </div>
            )}

            {/* Mutation error */}
            {requestPayout.error && (
              <div className="rounded-none border-2 border-destructive/40 bg-destructive/10 p-4">
                <p className="font-mono text-xs text-destructive flex items-center gap-2">
                  <AlertCircle className="h-3.5 w-3.5" />
                  {requestPayout.error.message}
                </p>
              </div>
            )}

            {/* Submit */}
            <div className="flex gap-3">
              <Button
                onClick={handleSubmit}
                disabled={requestPayout.isPending || !rawAmount}
                className="flex-1 rounded-none bg-neon-green text-black font-mono text-xs uppercase tracking-widest font-bold hover:bg-neon-green/90 py-5 disabled:opacity-50"
              >
                {requestPayout.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Submitting...
                  </>
                ) : (
                  <>
                    <TrendingUp className="h-4 w-4 mr-2" />
                    Request Payout
                  </>
                )}
              </Button>
              <Button
                variant="outline"
                asChild
                className="rounded-none font-mono text-xs uppercase tracking-widest py-5"
              >
                <Link href="/portfolio">Cancel</Link>
              </Button>
            </div>

            {/* Disclaimer */}
            <p className="font-mono text-[10px] text-muted-foreground/60 text-center leading-relaxed">
              Payout requests are processed within 1–3 business days. Rates
              shown are indicative and may vary at execution time.
            </p>
          </>
        )}
      </div>
    </div>
  );
}
