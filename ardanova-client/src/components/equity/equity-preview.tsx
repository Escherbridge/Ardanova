"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, ArrowRight, Info, Loader2, Lock } from "lucide-react";

import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import {
  fundingProjectTokenConfigSchema,
  parseFundingAmount,
} from "~/lib/commerce/investment-preview-contract";
import { cn } from "~/lib/utils";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface ProjectTokenPreviewProps {
  configId: string;
  projectSlug: string;
  gateStatus: GateStatus;
  unitName: string;
}

const QUICK_AMOUNTS = [100, 500, 1000, 5000];

function formatUSD(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}

function formatToken(value: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(value)} ${unit}`;
}

function formatPct(value: number) {
  return `${value.toFixed(4)}%`;
}

export default function ProjectTokenPreview({
  configId,
  projectSlug,
  gateStatus,
  unitName,
}: ProjectTokenPreviewProps) {
  const router = useRouter();
  const [usdInput, setUsdInput] = useState("100");
  const [debouncedInput, setDebouncedInput] = useState("100");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedInput(usdInput), 400);
    return () => window.clearTimeout(timer);
  }, [usdInput]);

  const currentAmount = parseFundingAmount(usdInput);
  const previewAmount = parseFundingAmount(debouncedInput);
  const previewMatchesInput =
    currentAmount !== null &&
    previewAmount !== null &&
    currentAmount.apiAmount === previewAmount.apiAmount;
  const isDisabled = gateStatus !== "FUNDING";

  const tokenValueQuery = api.exchange.getProjectTokenValue.useQuery(
    { configId },
    { enabled: !isDisabled },
  );
  const tokenAmount =
    previewAmount && tokenValueQuery.data && tokenValueQuery.data > 0
      ? Math.max(1, Math.round(previewAmount.value / tokenValueQuery.data))
      : null;
  const previewQuery = api.exchange.getConversionPreview.useQuery(
    { projectTokenConfigId: configId, tokenAmount: tokenAmount ?? 1 },
    { enabled: !isDisabled && Boolean(tokenAmount && tokenAmount > 0) },
  );
  const supplyQuery = api.projectTokens.getSupply.useQuery(
    { id: configId },
    { enabled: !isDisabled },
  );
  const supplyResult = fundingProjectTokenConfigSchema.safeParse(
    supplyQuery.data,
  );
  const estimateError = tokenValueQuery.error ?? previewQuery.error;
  const preview =
    previewMatchesInput && !estimateError ? previewQuery.data : undefined;
  const supplyShare =
    preview && supplyResult.success && supplyResult.data.totalSupply > 0
      ? (preview.sourceTokenAmount / supplyResult.data.totalSupply) * 100
      : null;

  const handleQuickAmount = useCallback((amount: number) => {
    const next = String(amount);
    setUsdInput(next);
    setDebouncedInput(next);
  }, []);

  const handleFund = () => {
    if (!currentAmount || !preview || !previewMatchesInput) return;
    router.push(
      `/projects/${projectSlug}/invest?amount=${encodeURIComponent(currentAmount.apiAmount)}`,
    );
  };

  return (
    <section
      className="border-foreground space-y-4 border p-4 sm:p-5"
      aria-labelledby="funding-preview-heading"
    >
      <div className="border-border flex items-start justify-between gap-4 border-b pb-4">
        <div>
          <p className="text-primary font-mono text-xs font-bold tracking-widest uppercase">
            Funding preview
          </p>
          <h3 id="funding-preview-heading" className="mt-2 text-xl font-bold">
            Estimate a project-token position
          </h3>
        </div>
        {isDisabled ? (
          <span className="border-border text-muted-foreground inline-flex min-h-8 items-center gap-1 border px-2 font-mono text-xs">
            <Lock className="size-3" aria-hidden="true" />
            {gateStatus}
          </span>
        ) : (
          <Info className="text-primary size-5 shrink-0" aria-hidden="true" />
        )}
      </div>

      <div className="flex flex-wrap gap-2" aria-label="Quick funding amounts">
        {QUICK_AMOUNTS.map((amount) => (
          <button
            key={amount}
            type="button"
            onClick={() => handleQuickAmount(amount)}
            disabled={isDisabled}
            aria-pressed={usdInput === String(amount)}
            className={cn(
              "min-h-11 border px-3 font-mono text-xs font-bold transition-colors",
              usdInput === String(amount)
                ? "border-primary bg-primary text-primary-foreground"
                : "border-border bg-background text-foreground hover:border-foreground",
              isDisabled && "cursor-not-allowed opacity-45",
            )}
          >
            ${amount.toLocaleString()}
          </button>
        ))}
      </div>

      <div>
        <label
          htmlFor={`funding-preview-${configId}`}
          className="mb-2 block text-sm font-semibold"
        >
          Amount in USD
        </label>
        <div className="relative">
          <span
            className="text-muted-foreground absolute top-1/2 left-3 -translate-y-1/2 font-mono text-sm"
            aria-hidden="true"
          >
            $
          </span>
          <Input
            id={`funding-preview-${configId}`}
            type="number"
            inputMode="decimal"
            value={usdInput}
            onChange={(event) => setUsdInput(event.target.value)}
            disabled={isDisabled}
            className="border-foreground h-11 pl-7 font-mono"
            placeholder="0.00"
            min="0.01"
            step="0.01"
            aria-invalid={!isDisabled && currentAmount === null}
          />
        </div>
      </div>

      <div className="border-border min-h-24 border-y py-2" aria-live="polite">
        {isDisabled ? (
          <p className="text-muted-foreground py-3 text-sm">
            New funding intents are unavailable in the current gate state.
          </p>
        ) : !currentAmount ? (
          <p className="text-destructive py-3 text-sm">
            Enter a positive amount with no more than two decimal places.
          </p>
        ) : !previewMatchesInput ||
          tokenValueQuery.isLoading ||
          previewQuery.isLoading ? (
          <div
            className="text-muted-foreground flex items-center gap-2 py-3 text-sm"
            role="status"
          >
            <Loader2 className="size-4 animate-spin" />
            Updating estimate…
          </div>
        ) : estimateError ? (
          <div
            className="text-destructive flex items-start gap-2 py-3 text-sm"
            role="alert"
          >
            <AlertCircle
              className="mt-0.5 size-4 shrink-0"
              aria-hidden="true"
            />
            The conversion service did not return a valid preview.
          </div>
        ) : preview ? (
          <dl className="divide-border divide-y">
            <PreviewRow
              label="Estimated project-token units"
              value={formatToken(preview.sourceTokenAmount, unitName)}
              emphasis
            />
            <PreviewRow
              label="Configured supply share"
              value={
                supplyShare === null ? "Not available" : formatPct(supplyShare)
              }
            />
            <PreviewRow
              label="Reference value"
              value={formatUSD(preview.usdValue)}
            />
            <PreviewRow
              label="Informational ARDA equivalent"
              value={`${new Intl.NumberFormat("en-US").format(preview.ardaAmount)} ARDA`}
            />
          </dl>
        ) : (
          <p className="text-muted-foreground py-3 text-sm">
            No estimate is available for this amount.
          </p>
        )}
      </div>

      <div className="border-primary text-muted-foreground border-l-4 pl-3 text-xs leading-relaxed">
        <p>
          This preview does not execute a conversion or guarantee token
          allocation, liquidity, redemption, or future value.
        </p>
        <p className="text-foreground mt-2 font-semibold">
          Supply share is not an equity or governance percentage.
        </p>
      </div>

      <Button
        type="button"
        className="w-full font-mono font-bold tracking-wide uppercase"
        disabled={isDisabled || !preview || !previewMatchesInput}
        onClick={handleFund}
      >
        Review funding terms
        <ArrowRight className="size-4" />
      </Button>
    </section>
  );
}

function PreviewRow({
  label,
  value,
  emphasis = false,
}: {
  label: string;
  value: string;
  emphasis?: boolean;
}) {
  return (
    <div className="grid grid-cols-[minmax(0,1fr)_auto] items-baseline gap-3 py-2">
      <dt className="text-muted-foreground text-xs">{label}</dt>
      <dd
        className={cn(
          "text-right font-mono text-xs font-bold",
          emphasis && "text-primary",
        )}
      >
        {value}
      </dd>
    </div>
  );
}
