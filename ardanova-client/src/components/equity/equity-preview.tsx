"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, ArrowRight, Info, Loader2, Lock } from "lucide-react";

import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { ConversionChain } from "~/components/equity/conversion-chain";
import { parseFundingAmount } from "~/lib/commerce/investment-preview-contract";
import { cn } from "~/lib/utils";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface ProjectTokenPreviewProps {
  configId: string;
  projectSlug: string;
  gateStatus: GateStatus;
  unitName: string;
}

const QUICK_AMOUNTS = [100, 500, 1000, 5000];

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}

function formatPct(value: number) {
  return `${value.toFixed(4)}%`;
}

/**
 * The funding preview embedded on a project page. Ownership of this widget is split from the
 * full invest flow at `/invest`: this quotes ARDA -> project units one hop, the full flow adds
 * the ARDA-balance check, acknowledgements, and checkout (W6.4). Prop contract is unchanged so
 * `funding-tab.tsx` (owned elsewhere) keeps working without edits.
 */
export default function ProjectTokenPreview({
  configId,
  projectSlug,
  gateStatus,
  unitName,
}: ProjectTokenPreviewProps) {
  const router = useRouter();
  const [ardaInput, setArdaInput] = useState("100");
  const [debouncedInput, setDebouncedInput] = useState("100");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedInput(ardaInput), 400);
    return () => window.clearTimeout(timer);
  }, [ardaInput]);

  const currentAmount = parseFundingAmount(ardaInput);
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
  const ardaValueQuery = api.exchange.getArdaValue.useQuery(undefined, {
    enabled: !isDisabled,
  });
  const supplyQuery = api.projectTokens.getSupply.useQuery(
    { id: configId },
    { enabled: !isDisabled },
  );
  const estimateError =
    tokenValueQuery.error ?? ardaValueQuery.error ?? supplyQuery.error;
  const estimateLoading =
    tokenValueQuery.isLoading || ardaValueQuery.isLoading;

  // One hop: ARDA per whole project unit, mirroring ExchangeService.GetDeclaredRateAsync
  // (unitValue / ardaValue). A non-positive rate means the project has no price yet, so no
  // quantity can be quoted — the server refuses the same way at checkout.
  const usdPerUnit = tokenValueQuery.data;
  const usdPerArda = ardaValueQuery.data;
  const ardaPerUnit =
    usdPerUnit !== undefined && usdPerArda !== undefined && usdPerArda > 0
      ? usdPerUnit / usdPerArda
      : null;
  const priceUnavailable = ardaPerUnit !== null && ardaPerUnit <= 0;

  const quantity =
    previewMatchesInput && previewAmount && ardaPerUnit && ardaPerUnit > 0
      ? Math.floor(previewAmount.value / ardaPerUnit)
      : null;
  const chargedArda =
    quantity && quantity > 0 && ardaPerUnit
      ? Math.ceil(quantity * ardaPerUnit * 100) / 100
      : null;
  const supplyShare =
    quantity && quantity > 0 && supplyQuery.data && supplyQuery.data.totalSupply > 0
      ? (quantity / supplyQuery.data.totalSupply) * 100
      : null;

  const handleQuickAmount = useCallback((amount: number) => {
    const next = String(amount);
    setArdaInput(next);
    setDebouncedInput(next);
  }, []);

  const handleFund = () => {
    if (!currentAmount || !previewMatchesInput || !quantity || quantity <= 0)
      return;
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
          <p className="text-primary font-mono text-xs tracking-widest uppercase">
            Funding preview
          </p>
          <h3 id="funding-preview-heading" className="mt-2 text-xl font-bold">
            Estimate a project-unit position
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

      <div className="flex flex-wrap gap-2" aria-label="Quick ARDA amounts">
        {QUICK_AMOUNTS.map((amount) => (
          <button
            key={amount}
            type="button"
            onClick={() => handleQuickAmount(amount)}
            disabled={isDisabled}
            aria-pressed={ardaInput === String(amount)}
            className={cn(
              "min-h-11 border px-3 font-mono text-xs font-bold transition-colors",
              ardaInput === String(amount)
                ? "border-primary bg-primary text-primary-foreground"
                : "border-border bg-background text-foreground hover:border-foreground",
              isDisabled && "cursor-not-allowed opacity-45",
            )}
          >
            {amount.toLocaleString()} ARDA
          </button>
        ))}
      </div>

      <div>
        <label
          htmlFor={`funding-preview-${configId}`}
          className="mb-2 block text-sm font-semibold"
        >
          Amount in ARDA
        </label>
        <div className="relative">
          <Input
            id={`funding-preview-${configId}`}
            type="number"
            inputMode="decimal"
            value={ardaInput}
            onChange={(event) => setArdaInput(event.target.value)}
            disabled={isDisabled}
            className="border-foreground h-11 font-mono"
            placeholder="0.00"
            min="0.01"
            step="0.01"
            aria-invalid={!isDisabled && currentAmount === null}
          />
          <span
            className="text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2 font-mono text-xs"
            aria-hidden="true"
          >
            ARDA
          </span>
        </div>
      </div>

      <div className="min-h-24" aria-live="polite">
        {isDisabled ? (
          <p className="text-muted-foreground py-3 text-sm">
            New funding intents are unavailable in the current gate state.
          </p>
        ) : !currentAmount ? (
          <p className="text-destructive py-3 text-sm">
            Enter a positive amount with no more than two decimal places.
          </p>
        ) : !previewMatchesInput || estimateLoading ? (
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
            The pricing service did not return a valid preview.
          </div>
        ) : priceUnavailable ? (
          <p className="text-muted-foreground py-3 text-sm">
            This project has no price yet, so nothing can be quoted.
          </p>
        ) : quantity && quantity > 0 && chargedArda !== null ? (
          <div className="space-y-3">
            <ConversionChain
              ardaAmount={chargedArda}
              projectUnits={quantity}
              unitName={unitName}
              usdPerArda={usdPerArda ?? 0}
            />
            <p className="text-sm">
              You&apos;ll hold <strong>{quantity.toLocaleString()} {unitName}</strong>
            </p>
            <details className="text-muted-foreground text-xs">
              <summary className="text-foreground cursor-pointer font-semibold">
                Show my share of the project
              </summary>
              <p className="mt-1 leading-relaxed">
                {supplyShare === null
                  ? "Share percentage is not available."
                  : `${formatPct(supplyShare)} of this project's configured supply.`}{" "}
                This is not equity, governance power, or a redemption right.
              </p>
            </details>
            {usdPerUnit !== undefined && (
              <p className="text-muted-foreground text-xs">
                Reference value: {formatUsd(quantity * usdPerUnit)}
              </p>
            )}
          </div>
        ) : (
          <p className="text-muted-foreground py-3 text-sm">
            That amount does not buy a whole unit of this project yet.
          </p>
        )}
      </div>

      <div className="border-primary text-muted-foreground border-l-4 pl-3 text-xs leading-relaxed">
        <p>
          This preview does not spend ARDA or guarantee unit allocation,
          liquidity, redemption, or future value. Rates can change before
          settlement.
        </p>
      </div>

      <Button
        type="button"
        className="w-full font-mono font-bold tracking-wide uppercase"
        disabled={
          isDisabled ||
          !previewMatchesInput ||
          !quantity ||
          quantity <= 0 ||
          priceUnavailable
        }
        onClick={handleFund}
      >
        Review funding terms
        <ArrowRight className="size-4" />
      </Button>
    </section>
  );
}
