"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import { useParams, useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Checkbox } from "~/components/ui/checkbox";
import { Card, CardContent } from "~/components/ui/card";
import { Progress } from "~/components/ui/progress";
import { cn } from "~/lib/utils";
import {
  ArrowLeft,
  ArrowRight,
  Check,
  Loader2,
  Lock,
  TrendingUp,
  ExternalLink,
  AlertCircle,
  DollarSign,
  Shield,
} from "lucide-react";

const QUICK_AMOUNTS = [100, 500, 1000, 5000];

function formatUSD(n: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(n);
}

function formatToken(n: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(Math.round(n))} ${unit}`;
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

// Step indicator
function StepIndicator({ current, total }: { current: number; total: number }) {
  return (
    <div className="flex items-center gap-2">
      {Array.from({ length: total }, (_, i) => (
        <div key={i} className="flex items-center gap-2">
          <div
            className={cn(
              "size-7 rounded-full border-2 flex items-center justify-center font-mono text-xs font-bold transition-colors",
              i + 1 < current
                ? "border-[#00ff88] bg-[#00ff88] text-background"
                : i + 1 === current
                ? "border-[#00d4ff] bg-[#00d4ff]/10 text-[#00d4ff]"
                : "border-border text-muted-foreground",
            )}
          >
            {i + 1 < current ? <Check className="size-3" /> : i + 1}
          </div>
          {i < total - 1 && (
            <div
              className={cn(
                "h-0.5 w-8",
                i + 1 < current ? "bg-[#00ff88]" : "bg-border",
              )}
            />
          )}
        </div>
      ))}
    </div>
  );
}

export default function InvestPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const router = useRouter();
  const slug = params.slug as string;

  const initialAmount = searchParams.get("amount") ?? "100";
  const [step, setStep] = useState(1);

  // Step 1 state
  const [usdInput, setUsdInput] = useState(initialAmount);
  const [debouncedUsd, setDebouncedUsd] = useState(parseFloat(initialAmount) || 100);

  // Step 2 state
  const [checkLocked, setCheckLocked] = useState(false);
  const [checkTrust, setCheckTrust] = useState(false);
  const checkoutIdempotencyKey = useRef<string | undefined>(undefined);
  const checkout = api.fundingIntent.createCheckout.useMutation({
    onSuccess: ({ intentId, checkoutUrl }) => {
      window.sessionStorage.setItem(`ardanova:funding-intent:${slug}`, intentId);
      window.location.assign(checkoutUrl);
    },
  });

  // Debounce
  useEffect(() => {
    const t = setTimeout(() => {
      const v = parseFloat(usdInput);
      if (!isNaN(v) && v > 0) setDebouncedUsd(v);
    }, 400);
    return () => clearTimeout(t);
  }, [usdInput]);

  // Fetch project by slug to get projectId
  const { data: project, isLoading: projectLoading } = api.project.getById.useQuery({ id: slug });

  const { data: configRaw, isLoading: configLoading } =
    api.projectTokens.getConfigByProject.useQuery(
      { projectId: project?.id ?? "" },
      { enabled: !!project?.id },
    );
  const config = configRaw as unknown as { id: string; unitName: string; assetName: string; gateStatus: string } | undefined;

  const { data: tokenValueRaw } = api.exchange.getProjectTokenValue.useQuery(
    { configId: config?.id ?? "" },
    { enabled: !!config?.id },
  );
  const tokenValue = tokenValueRaw as unknown as number | undefined;

  const tokenAmount = tokenValue && tokenValue > 0 ? Math.max(1, Math.round(debouncedUsd / tokenValue)) : null;

  const { data: previewRaw, isLoading: previewLoading } = api.exchange.getConversionPreview.useQuery(
    { projectTokenConfigId: config?.id ?? "", tokenAmount: tokenAmount ?? 1 },
    { enabled: !!config?.id && !!tokenAmount && tokenAmount >= 1 },
  );
  const preview = previewRaw as unknown as { projectTokens: number; ardaAmount: number; usdAmount: number; tokenRate: number; ardaRate: number } | undefined;

  const { data: supplyRaw } = api.projectTokens.getSupply.useQuery(
    { id: config?.id ?? "" },
    { enabled: !!config?.id },
  );
  const supply = supplyRaw as unknown as { totalSupply: number; contributorSupply: number; investorSupply: number; founderSupply: number; burnedSupply: number } | undefined;

  const equityPct =
    preview && supply && supply.totalSupply > 0
      ? (preview.projectTokens / supply.totalSupply) * 100
      : null;

  const gateStatus = config?.gateStatus as "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED" | undefined;
  const isOpen = gateStatus === "FUNDING";

  const handleQuickAmount = useCallback((amt: number) => setUsdInput(String(amt)), []);

  const beginCheckout = () => {
    if (!config) return;

    checkoutIdempotencyKey.current ??= window.crypto.randomUUID();
    setStep(3);
    checkout.mutate({
      projectTokenConfigId: config.id,
      amount: usdInput.trim(),
      disclosureVersion: "funding-disclosure-v1",
      idempotencyKey: checkoutIdempotencyKey.current,
    });
  };

  const handleNext = () => {
    if (step === 1 && preview) setStep(2);
    else if (step === 2 && checkLocked && checkTrust) beginCheckout();
  };

  const handleBack = () => {
    if (step > 1) setStep((s) => s - 1);
    else router.push(`/projects/${slug}`);
  };

  if (projectLoading || configLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!project || !config) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center gap-4 p-4">
        <AlertCircle className="size-10 text-destructive" />
        <p className="font-mono font-bold text-destructive">CONFIGURATION NOT FOUND</p>
        <Button asChild variant="outline">
          <Link href={`/projects/${slug}`}>
            <ArrowLeft className="size-4 mr-2" />
            Back to Project
          </Link>
        </Button>
      </div>
    );
  }

  if (!isOpen) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center gap-4 p-4">
        <Lock className="size-10 text-muted-foreground" />
        <p className="font-mono font-bold text-muted-foreground">
          FUNDING IS NOT OPEN
        </p>
        <p className="text-sm text-muted-foreground text-center max-w-sm">
          This project is currently in <span className="font-mono font-bold text-[#00d4ff]">{gateStatus}</span> status and is not accepting new investments.
        </p>
        <Button asChild variant="outline">
          <Link href={`/projects/${slug}`}>
            <ArrowLeft className="size-4 mr-2" />
            Back to Project
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-lg mx-auto px-4 py-8">
        {/* Back nav */}
        <Button variant="ghost" onClick={handleBack} className="-ml-2 mb-6">
          <ArrowLeft className="size-4 mr-2" />
          {step === 1 ? "Back to Project" : "Back"}
        </Button>

        {/* Header */}
        <div className="mb-6">
          <h1 className="text-2xl font-bold font-mono tracking-wide">
            INVEST IN {project.title?.toUpperCase()}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            {config.unitName} · {config.assetName}
          </p>
        </div>

        {/* Step indicator */}
        <div className="flex items-center justify-between mb-8">
          <StepIndicator current={step} total={3} />
          <span className="font-mono text-xs text-muted-foreground">
            STEP {step} OF 3
          </span>
        </div>

        {/* Step 1: Amount */}
        {step === 1 && (
          <div className="space-y-5">
            <Card className="border-2 border-border">
              <CardContent className="p-5 space-y-4">
                <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground block">
                  INVESTMENT AMOUNT
                </span>

                {/* Quick amounts */}
                <div className="flex gap-2 flex-wrap">
                  {QUICK_AMOUNTS.map((amt) => (
                    <button
                      key={amt}
                      onClick={() => handleQuickAmount(amt)}
                      className={cn(
                        "font-mono text-xs border-2 px-3 py-1.5 transition-colors",
                        usdInput === String(amt)
                          ? "border-[#00d4ff] bg-[#00d4ff]/10 text-[#00d4ff]"
                          : "border-border text-muted-foreground hover:border-foreground hover:text-foreground",
                      )}
                    >
                      ${amt.toLocaleString()}
                    </button>
                  ))}
                </div>

                {/* Custom input */}
                <div className="relative">
                  <DollarSign className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                  <Input
                    type="number"
                    value={usdInput}
                    onChange={(e) => setUsdInput(e.target.value)}
                    className="pl-9 font-mono border-2 text-lg h-12"
                    placeholder="0.00"
                    min="1"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Conversion preview */}
            <Card className="border-2 border-[#00d4ff]/30 bg-[#00d4ff]/5">
              <CardContent className="p-5 space-y-3">
                <span className="font-mono text-xs font-bold tracking-widest text-[#00d4ff] block">
                  EQUITY BREAKDOWN
                </span>

                {previewLoading ? (
                  <div className="flex items-center gap-2 text-muted-foreground text-sm py-2">
                    <Loader2 className="size-4 animate-spin" />
                    Calculating...
                  </div>
                ) : preview ? (
                  <div className="space-y-2">
                    <Row label="You pay" value={formatUSD(debouncedUsd)} accent="#ffffff" />
                    <Row
                      label={`${config.unitName} tokens`}
                      value={formatToken(preview.projectTokens, config.unitName)}
                      accent="#00d4ff"
                    />
                    <Row
                      label="Equity stake"
                      value={equityPct !== null ? formatPct(equityPct) : "—"}
                      accent="#00ff88"
                    />
                    <Row
                      label="ARDA equivalent"
                      value={preview.ardaAmount ? `${new Intl.NumberFormat("en-US").format(Math.round(preview.ardaAmount))} ARDA` : "—"}
                      accent="#a855f7"
                    />
                    {preview.tokenRate && (
                      <Row
                        label="Rate"
                        value={`${formatUSD(preview.tokenRate)} / token`}
                        accent="#fbbf24"
                      />
                    )}
                  </div>
                ) : (
                  <p className="text-xs text-muted-foreground">Enter an amount to see your equity.</p>
                )}
              </CardContent>
            </Card>

            <Button
              className="w-full font-mono font-bold h-12"
              variant="neon"
              disabled={!preview}
              onClick={handleNext}
            >
              CONTINUE TO REVIEW
              <ArrowRight className="size-4 ml-2" />
            </Button>
          </div>
        )}

        {/* Step 2: Review & Confirm */}
        {step === 2 && preview && (
          <div className="space-y-5">
            <Card className="border-2 border-border">
              <CardContent className="p-5 space-y-3">
                <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground block">
                  ORDER SUMMARY
                </span>
                <div className="space-y-2">
                  <Row label="Investment" value={formatUSD(debouncedUsd)} accent="#ffffff" />
                  <Row label={`${config.unitName} tokens`} value={formatToken(preview.projectTokens, config.unitName)} accent="#00d4ff" />
                  <Row label="Equity" value={equityPct !== null ? formatPct(equityPct) : "—"} accent="#00ff88" />
                  <Row
                    label="Project"
                    value={project.title ?? slug}
                    accent="#ffffff"
                  />
                </div>
                <div className="border-t-2 border-border pt-3 flex justify-between">
                  <span className="font-mono text-sm font-bold">TOTAL</span>
                  <span className="font-mono text-lg font-bold text-[#00d4ff]">
                    {formatUSD(debouncedUsd)}
                  </span>
                </div>
              </CardContent>
            </Card>

            {/* Checkboxes */}
            <Card className="border-2 border-border">
              <CardContent className="p-5 space-y-4">
                <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground block">
                  AGREEMENTS
                </span>

                <label className="flex items-start gap-3 cursor-pointer group">
                  <Checkbox
                    checked={checkLocked}
                    onCheckedChange={(v) => setCheckLocked(!!v)}
                    className="mt-0.5 border-2"
                  />
                  <div>
                    <p className="text-sm font-medium">Token Lock Period</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      I understand that my tokens are locked and cannot be transferred until Gate 2 is cleared by the project founder.
                    </p>
                  </div>
                </label>

                <label className="flex items-start gap-3 cursor-pointer group">
                  <Checkbox
                    checked={checkTrust}
                    onCheckedChange={(v) => setCheckTrust(!!v)}
                    className="mt-0.5 border-2"
                  />
                  <div>
                    <p className="text-sm font-medium">Trust Protection</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      I understand this investment is protected by ArdaNova trust protocol. In case of project failure, I will be refunded my investment amount.
                    </p>
                  </div>
                </label>
              </CardContent>
            </Card>

            <Button
              className="w-full font-mono font-bold h-12"
              variant="neon"
              disabled={!checkLocked || !checkTrust}
              onClick={handleNext}
            >
              PROCEED TO PAYMENT
              <ArrowRight className="size-4 ml-2" />
            </Button>
          </div>
        )}

        {/* Step 3: Stripe redirect placeholder */}
        {step === 3 && (
          <div className="space-y-5">
            <Card className="border-2 border-[#00ff88]/40 bg-[#00ff88]/5">
              <CardContent className="p-8 flex flex-col items-center gap-4 text-center">
                <div className="size-16 rounded-full border-2 border-[#00ff88] bg-[#00ff88]/10 flex items-center justify-center">
                  <Loader2 className="size-8 animate-spin text-[#00ff88]" />
                </div>
                <div>
                  <p className="font-mono text-lg font-bold text-[#00ff88]">REDIRECTING TO STRIPE</p>
                  <p className="text-sm text-muted-foreground mt-2">
                    Preparing your secure checkout session for {formatUSD(debouncedUsd)}...
                  </p>
                </div>

                <div className="flex items-center gap-2 text-xs text-muted-foreground border border-border/40 px-3 py-2">
                  <Shield className="size-3 text-[#00d4ff]" />
                  Secured by Stripe · 256-bit TLS
                </div>

                <div className="space-y-1 text-xs text-muted-foreground w-full text-left border-t border-border/40 pt-3">
                  <p className="font-mono font-bold text-muted-foreground mb-2">ORDER DETAILS</p>
                  <div className="flex justify-between">
                    <span>Amount</span>
                    <span className="font-mono font-bold text-foreground">{formatUSD(debouncedUsd)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Tokens</span>
                    <span className="font-mono font-bold text-[#00d4ff]">
                      {preview ? formatToken(preview.projectTokens, config.unitName) : "—"}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span>Equity</span>
                    <span className="font-mono font-bold text-[#00ff88]">
                      {equityPct !== null ? formatPct(equityPct) : "—"}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>

            <p className="text-xs text-center text-muted-foreground">
              You&apos;ll be redirected automatically. If nothing happens,{" "}
              <button
                className="text-[#00d4ff] underline"
                  onClick={beginCheckout}
              >
                click here
              </button>
              .
            </p>

            {checkout.error && (
              <p className="text-xs text-center text-destructive">
                {checkout.error.message}
              </p>
            )}

            <Button
              variant="outline"
              className="w-full font-mono"
              onClick={() => setStep(2)}
            >
              <ArrowLeft className="size-4 mr-2" />
              GO BACK
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

function Row({ label, value, accent }: { label: string; value: string; accent: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="font-mono text-xs font-bold" style={{ color: accent }}>
        {value}
      </span>
    </div>
  );
}
