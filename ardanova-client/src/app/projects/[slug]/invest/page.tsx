"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Check,
  Loader2,
  Lock,
  Plus,
  ShieldCheck,
} from "lucide-react";

import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Checkbox } from "~/components/ui/checkbox";
import { Input } from "~/components/ui/input";
import { ConversionChain } from "~/components/equity/conversion-chain";
import { describeFundingError } from "~/lib/commerce/funding-intent-status";
import {
  fundingProjectTokenConfigSchema,
  parseFundingAmount,
} from "~/lib/commerce/investment-preview-contract";
import { reviewContactHref } from "~/components/azoa/custodial-account-card";
import { evaluateValueActionAccount } from "~/lib/contracts/project-create-contract";
import { cn } from "~/lib/utils";

const QUICK_AMOUNTS = [100, 500, 1000, 5000];
const STEP_LABELS = ["Amount", "Review", "Support"];

/** sessionStorage key both this page and /invest/success read the quoted intent under. */
function intentStorageKey(slug: string) {
  return `ardanova:funding-intent:${slug}`;
}

/** What this page persists for the success page to show as "as quoted", never as confirmed. */
interface QuotedIntent {
  intentId: string;
  ardaAmount: number;
  quantity: number;
  unitName: string;
  supplySharePercent: number | null;
}

function formatUsd(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(value);
}

function formatArda(value: number) {
  return `${new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value)} ARDA`;
}

function formatUnits(value: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(value)} ${unit}`;
}

function formatPct(value: number) {
  return `${value.toFixed(4)}%`;
}

/** Only ever navigates within this app. A same-origin path is required, never trusted as-is. */
function internalPath(candidate: string, fallback: string): string {
  return candidate.startsWith("/") && !candidate.startsWith("//")
    ? candidate
    : fallback;
}

function StepIndicator({ current }: { current: number }) {
  return (
    <ol
      className="border-border grid grid-cols-3 border"
      aria-label="Funding progress"
    >
      {STEP_LABELS.map((label, index) => {
        const number = index + 1;
        const complete = number < current;
        const active = number === current;

        return (
          <li
            key={label}
            className={cn(
              "border-border flex min-h-12 items-center gap-2 border-r px-3 last:border-r-0",
              active && "bg-primary text-primary-foreground",
              complete && "bg-secondary text-foreground",
            )}
            aria-current={active ? "step" : undefined}
          >
            <span
              className={cn(
                "flex size-6 shrink-0 items-center justify-center border font-mono text-xs font-bold",
                active ? "border-primary-foreground" : "border-current",
              )}
              aria-hidden="true"
            >
              {complete ? <Check className="size-3.5" /> : number}
            </span>
            <span className="hidden text-xs font-bold tracking-wide uppercase sm:inline">
              {label}
            </span>
          </li>
        );
      })}
    </ol>
  );
}

export default function InvestPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const router = useRouter();
  const slug = params.slug as string;
  const initialAmount = searchParams.get("amount") ?? "100";
  const accountSetupHref = `/settings/verification?returnTo=${encodeURIComponent(
    `/projects/${slug}/invest`,
  )}`;

  const [step, setStep] = useState(1);
  const [ardaInput, setArdaInput] = useState(initialAmount);
  const [debouncedInput, setDebouncedInput] = useState(initialAmount);
  const [checkLocked, setCheckLocked] = useState(false);
  const [checkTrust, setCheckTrust] = useState(false);
  const [showName, setShowName] = useState(false);
  const checkoutIdempotencyKey = useRef<string | undefined>(undefined);

  const checkout = api.fundingIntent.createCheckout.useMutation({
    onSuccess: ({ intentId, checkoutUrl }, variables) => {
      // amount was sent in ARDA; recompute the display quote from the same review-step numbers
      // so the success page can show "as quoted" without a new field on the status DTO.
      try {
        window.sessionStorage.setItem(
          intentStorageKey(slug),
          JSON.stringify({
            intentId,
            ardaAmount: Number(variables.amount),
            quantity: lastQuoteRef.current?.quantity ?? 0,
            unitName: lastQuoteRef.current?.unitName ?? "",
            supplySharePercent: lastQuoteRef.current?.supplySharePercent ?? null,
          } satisfies QuotedIntent),
        );
      } catch {
        // Best-effort convenience only; the success page still works from the intent id alone.
      }

      router.push(
        internalPath(
          checkoutUrl,
          `/projects/${slug}/invest/success?intent=${encodeURIComponent(intentId)}`,
        ),
      );
    },
  });

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedInput(ardaInput), 400);
    return () => window.clearTimeout(timer);
  }, [ardaInput]);

  const projectQuery = api.project.getById.useQuery({ id: slug });
  const accountStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    retry: false,
  });
  // Three-way, never a boolean: an account held for a human decision is not an account with
  // setup left to finish, and an unread account is neither. Which refusal it is decides the
  // words; only "ready" decides the permission.
  const accountEvaluation = evaluateValueActionAccount(accountStatus.data, {
    loadFailed: Boolean(accountStatus.error),
  });
  const valueAccountReady = accountEvaluation.state === "ready";
  const configQuery = api.projectTokens.getConfigByProject.useQuery(
    { projectId: projectQuery.data?.id ?? "" },
    { enabled: Boolean(projectQuery.data?.id) },
  );
  const configResult = fundingProjectTokenConfigSchema.safeParse(
    configQuery.data,
  );
  const config = configResult.success ? configResult.data : undefined;
  const configContractInvalid =
    configQuery.data !== undefined && !configResult.success;

  const currentAmount = parseFundingAmount(ardaInput);
  const previewAmount = parseFundingAmount(debouncedInput);
  const previewMatchesInput =
    currentAmount !== null &&
    previewAmount !== null &&
    currentAmount.apiAmount === previewAmount.apiAmount;

  const ardaBalanceQuery = api.tokenBalances.getArdaBalance.useQuery(
    undefined,
    { enabled: valueAccountReady, retry: false },
  );
  const availableArda = ardaBalanceQuery.data?.availableBalance ?? null;
  const insufficientArda =
    availableArda !== null &&
    currentAmount !== null &&
    currentAmount.value > availableArda;

  // One hop: ARDA -> project units. ardaPerUnit mirrors ExchangeService.GetDeclaredRateAsync
  // (unitValue / ardaValue); quantity floors and chargedArda ceils, mirroring
  // FundingIntentService.TryQuote so the preview reads the same way the server will price it.
  const tokenValueQuery = api.exchange.getProjectTokenValue.useQuery(
    { configId: config?.id ?? "" },
    { enabled: Boolean(config?.id) },
  );
  const ardaValueQuery = api.exchange.getArdaValue.useQuery(undefined, {
    enabled: Boolean(config?.id),
  });
  const estimateError = tokenValueQuery.error ?? ardaValueQuery.error;
  const estimateLoading = tokenValueQuery.isLoading || ardaValueQuery.isLoading;

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
  const remainderArda =
    previewAmount && chargedArda !== null
      ? Math.max(0, Math.round((previewAmount.value - chargedArda) * 100) / 100)
      : 0;
  const supplyShare =
    quantity && quantity > 0 && config && config.totalSupply > 0
      ? (quantity / config.totalSupply) * 100
      : null;
  const hasQuote =
    !estimateLoading &&
    !estimateError &&
    !priceUnavailable &&
    quantity !== null &&
    quantity > 0 &&
    chargedArda !== null;

  // The review step re-reads these at submit time, so the intent stored for the success page
  // reflects what the person actually saw on the review screen.
  const lastQuoteRef = useRef<{
    quantity: number;
    unitName: string;
    supplySharePercent: number | null;
  } | null>(null);
  useEffect(() => {
    if (hasQuote && quantity && config) {
      lastQuoteRef.current = {
        quantity,
        unitName: config.unitName,
        supplySharePercent: supplyShare,
      };
    }
  }, [hasQuote, quantity, config, supplyShare]);

  const handleQuickAmount = useCallback((amount: number) => {
    const next = String(amount);
    setArdaInput(next);
    setDebouncedInput(next);
  }, []);

  const beginCheckout = () => {
    if (!config || !currentAmount || !hasQuote || !previewMatchesInput) return;

    checkoutIdempotencyKey.current ??= window.crypto.randomUUID();
    setStep(3);
    checkout.mutate({
      projectTokenConfigId: config.id,
      amount: currentAmount.apiAmount,
      disclosureVersion: "funding-disclosure-v1",
      displayNameConsent: showName,
      idempotencyKey: checkoutIdempotencyKey.current,
    });
  };

  const handleNext = () => {
    if (step === 1 && hasQuote && previewMatchesInput && !insufficientArda)
      setStep(2);
    if (step === 2 && checkLocked && checkTrust) beginCheckout();
  };

  const handleBack = () => {
    if (step > 1) {
      setStep((current) => current - 1);
      return;
    }
    router.push(`/projects/${slug}`);
  };

  if (projectQuery.isLoading || configQuery.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center p-6">
        <div className="flex items-center gap-3" role="status">
          <Loader2 className="text-primary size-5 animate-spin" />
          <span className="font-mono text-sm">Loading funding terms…</span>
        </div>
      </div>
    );
  }

  if (
    projectQuery.error ||
    configQuery.error ||
    !projectQuery.data ||
    !config ||
    configContractInvalid
  ) {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <AlertCircle className="text-destructive size-10" aria-hidden="true" />
        <div>
          <h1 className="font-mono text-2xl font-bold">
            Funding terms unavailable
          </h1>
          <p className="text-muted-foreground mt-2 text-sm">
            We could not verify this project&apos;s funding configuration against
            the funding contract. Support is disabled until the data is
            available and valid.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href={`/projects/${slug}`}>
            <ArrowLeft className="size-4" />
            Back to project
          </Link>
        </Button>
      </div>
    );
  }

  if (config.gateStatus !== "FUNDING") {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <Lock className="text-muted-foreground size-10" aria-hidden="true" />
        <div>
          <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
            Gate status · {config.gateStatus}
          </p>
          <h1 className="mt-2 font-mono text-2xl font-bold">
            Funding is not open
          </h1>
          <p className="text-muted-foreground mt-2 text-sm">
            This project is not accepting new sponsorships in its current
            gate state.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href={`/projects/${slug}`}>
            <ArrowLeft className="size-4" />
            Back to project
          </Link>
        </Button>
      </div>
    );
  }

  if (!valueAccountReady) {
    const accountUnderReview =
      !accountStatus.isPending && accountEvaluation.state === "under-review";
    const reviewContact = accountUnderReview ? reviewContactHref() : null;
    const accountSetupEyebrow = accountUnderReview
      ? "Account review"
      : "Account setup";
    const accountSetupHeading = accountStatus.isPending
      ? "Checking your account setup"
      : accountUnderReview
        ? "Your account is being reviewed"
        : accountEvaluation.state === "service-unavailable"
          ? "We could not confirm your account setup"
          : "Complete account setup before funding";
    // The held-review sentence is the shared contract's, so it matches the server's refusal
    // word for word; the other two keep this page's own funding wording. The node's
    // `unavailableReason` is a provider diagnostic and is never printed at a sponsor.
    const accountSetupMessage = accountStatus.isPending
      ? "We are checking the account setup required for funding. Support remains unavailable until that check is complete."
      : accountUnderReview
        ? // The shared contract's own words, matching the server's refusal text.
          accountEvaluation.message
        : accountEvaluation.state === "service-unavailable"
          ? "We could not confirm account setup. To protect your ARDA, support remains unavailable until the status can be confirmed."
          : "Complete account setup before funding a project. You can still review project details and estimates while setup is pending.";

    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <ShieldCheck className="text-primary size-10" aria-hidden="true" />
        <div>
          <p className="text-primary font-mono text-xs font-bold tracking-widest uppercase">
            {accountSetupEyebrow}
          </p>
          <h1 className="mt-2 font-mono text-2xl font-bold">
            {accountSetupHeading}
          </h1>
          <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
            {accountSetupMessage}
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          {accountUnderReview ? (
            reviewContact && (
              <Button asChild>
                <a href={reviewContact}>Contact the review team</a>
              </Button>
            )
          ) : (
            <Button asChild>
              <Link href={accountSetupHref}>Review account setup</Link>
            </Button>
          )}
          <Button asChild variant="outline">
            <Link href={`/projects/${slug}`}>
              <ArrowLeft className="size-4" />
              Back to project
            </Link>
          </Button>
        </div>
      </div>
    );
  }

  const projectTitle = projectQuery.data.title ?? "this project";

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 sm:px-6 sm:py-10">
      <Button
        type="button"
        variant="ghost"
        onClick={handleBack}
        className="mb-6 -ml-3"
        disabled={step === 3 && checkout.isPending}
      >
        <ArrowLeft className="size-4" />
        {step === 1 ? "Back to project" : "Back"}
      </Button>

      <header className="border-foreground border-y py-6 sm:py-8">
        <p className="text-primary font-mono text-xs font-bold tracking-[0.2em] uppercase">
          Project funding
        </p>
        <h1 className="mt-3 text-3xl font-black tracking-tight sm:text-5xl">
          Fund {projectTitle}
        </h1>
        <p className="text-muted-foreground mt-3 max-w-2xl text-sm sm:text-base">
          Review an estimate, acknowledge the current terms, then support
          this project with ARDA from your balance. This is a durable
          sponsorship, not a hosted checkout — a browser redirect is never
          proof of a settled stake.
        </p>
      </header>

      <div className="my-6">
        <StepIndicator current={step} />
      </div>

      {step === 1 && (
        <section className="space-y-5" aria-labelledby="amount-heading">
          <Card className="border-foreground">
            <CardContent className="space-y-5 p-5 sm:p-6">
              <div>
                <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                  01 · Define an amount
                </p>
                <h2 id="amount-heading" className="mt-2 text-2xl font-bold">
                  Amount to spend, in ARDA
                </h2>
              </div>

              <div className="flex flex-wrap gap-2" aria-label="Quick amounts">
                {QUICK_AMOUNTS.map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => handleQuickAmount(amount)}
                    aria-pressed={ardaInput === String(amount)}
                    className={cn(
                      "min-h-11 border px-4 font-mono text-sm font-bold transition-colors",
                      ardaInput === String(amount)
                        ? "border-primary bg-primary text-primary-foreground"
                        : "border-border bg-background text-foreground hover:border-foreground",
                    )}
                  >
                    {amount.toLocaleString()} ARDA
                  </button>
                ))}
              </div>

              <div>
                <label
                  htmlFor="funding-amount"
                  className="mb-2 block text-sm font-semibold"
                >
                  Custom amount in ARDA
                </label>
                <div className="relative">
                  <Input
                    id="funding-amount"
                    type="number"
                    inputMode="decimal"
                    value={ardaInput}
                    onChange={(event) => setArdaInput(event.target.value)}
                    className="border-foreground h-12 pr-16 font-mono text-lg"
                    placeholder="0.00"
                    min="0.01"
                    step="0.01"
                    aria-invalid={currentAmount === null}
                    aria-describedby="funding-amount-help"
                  />
                  <span
                    className="text-muted-foreground absolute top-1/2 right-4 -translate-y-1/2 font-mono text-sm"
                    aria-hidden="true"
                  >
                    ARDA
                  </span>
                </div>
                <p
                  id="funding-amount-help"
                  className={cn(
                    "mt-2 text-xs",
                    currentAmount
                      ? "text-muted-foreground"
                      : "text-destructive",
                  )}
                >
                  {currentAmount
                    ? "Use a positive amount with no more than two decimal places."
                    : "Enter a positive ARDA amount with no more than two decimal places."}
                </p>
              </div>

              {availableArda !== null && (
                <p className="text-muted-foreground text-xs">
                  You have {formatArda(availableArda)} available.
                </p>
              )}

              {insufficientArda && currentAmount && availableArda !== null && (
                <div
                  className="border-destructive bg-destructive/5 flex items-start justify-between gap-4 border p-3"
                  role="alert"
                >
                  <p className="text-destructive text-sm">
                    You have {formatArda(availableArda)} available, but this
                    needs {formatArda(currentAmount.value)}.
                  </p>
                  <Button asChild size="sm" variant="outline" className="shrink-0">
                    <Link href="/portfolio/add-funds">
                      <Plus className="size-4" />
                      Add funds
                    </Link>
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="border-primary bg-primary/5">
            <CardContent className="space-y-4 p-5 sm:p-6">
              <div>
                <p className="text-primary font-mono text-xs font-bold tracking-widest uppercase">
                  Your share of this project
                </p>
                <h2 className="mt-2 text-xl font-bold">
                  What the API can preview
                </h2>
              </div>

              {!currentAmount ? (
                <p className="text-muted-foreground text-sm">
                  Enter a valid amount to request an estimate.
                </p>
              ) : !previewMatchesInput || estimateLoading ? (
                <div
                  className="text-muted-foreground flex items-center gap-2 text-sm"
                  role="status"
                >
                  <Loader2 className="size-4 animate-spin" />
                  Updating the estimate…
                </div>
              ) : estimateError ? (
                <div className="border-destructive border p-3" role="alert">
                  <p className="text-destructive text-sm font-semibold">
                    Estimate unavailable
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    The pricing service did not return a valid preview.
                    Support remains disabled.
                  </p>
                </div>
              ) : priceUnavailable ? (
                <p className="text-muted-foreground text-sm">
                  This project has no price yet, so nothing was taken.
                </p>
              ) : hasQuote && quantity && chargedArda !== null ? (
                <div className="space-y-4">
                  <ConversionChain
                    ardaAmount={chargedArda}
                    projectUnits={quantity}
                    unitName={config.unitName}
                    usdPerArda={usdPerArda ?? 0}
                  />

                  <p className="text-sm">
                    You&apos;ll hold{" "}
                    <strong>{formatUnits(quantity, config.unitName)}</strong>
                  </p>

                  <details className="border-border border p-3 text-sm">
                    <summary className="cursor-pointer font-semibold">
                      Show my share of the project
                    </summary>
                    <p className="text-muted-foreground mt-2 text-xs leading-relaxed">
                      {supplyShare === null
                        ? "Share percentage is not available."
                        : `${formatPct(supplyShare)} of this project's configured supply.`}{" "}
                      This is not equity, governance power, a redemption
                      right, or a guaranteed USD value.
                    </p>
                  </details>

                  {remainderArda > 0 && (
                    <p className="text-muted-foreground text-xs">
                      Whole units only — {formatArda(remainderArda)} above the
                      cost of {formatUnits(quantity, config.unitName)} stays
                      in your balance.
                    </p>
                  )}
                  {usdPerUnit !== undefined && (
                    <p className="text-muted-foreground text-xs">
                      Reference value: {formatUsd(quantity * usdPerUnit)}
                    </p>
                  )}
                </div>
              ) : (
                <p className="text-muted-foreground text-sm">
                  That amount does not buy a whole unit of this project yet.
                  Support remains disabled.
                </p>
              )}

              <div className="border-primary text-muted-foreground border-l-4 pl-4 text-xs leading-relaxed">
                <p>
                  This estimate does not spend ARDA or create project units.
                  Rates can change before settlement.
                </p>
              </div>
            </CardContent>
          </Card>

          <Button
            type="button"
            className="h-12 w-full font-mono font-bold tracking-wide uppercase"
            disabled={!hasQuote || !previewMatchesInput || insufficientArda}
            onClick={handleNext}
          >
            Review the funding intent
            <ArrowRight className="size-4" />
          </Button>
        </section>
      )}

      {step === 2 && hasQuote && currentAmount && quantity && chargedArda !== null && (
        <section className="space-y-5" aria-labelledby="review-heading">
          <Card className="border-foreground">
            <CardContent className="space-y-4 p-5 sm:p-6">
              <div>
                <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                  02 · Review the intent
                </p>
                <h2 id="review-heading" className="mt-2 text-2xl font-bold">
                  Funding intent summary
                </h2>
              </div>

              <ConversionChain
                ardaAmount={chargedArda}
                projectUnits={quantity}
                unitName={config.unitName}
                usdPerArda={usdPerArda ?? 0}
              />

              <details className="border-border border p-3 text-sm">
                <summary className="cursor-pointer font-semibold">
                  Show my share of the project
                </summary>
                <p className="text-muted-foreground mt-2 text-xs leading-relaxed">
                  {supplyShare === null
                    ? "Share percentage is not available."
                    : `${formatPct(supplyShare)} of this project's configured supply.`}{" "}
                  This is not equity, governance power, or a redemption
                  right.
                </p>
              </details>

              <p className="text-muted-foreground text-xs leading-relaxed">
                Pressing Support creates an actor-owned, idempotent
                sponsorship and asks the node to move exactly the amounts
                above. Nothing here confirms settlement — the next screen
                reads your actual sponsorship record.
              </p>
            </CardContent>
          </Card>

          <Card className="border-foreground">
            <CardContent className="space-y-5 p-5 sm:p-6">
              <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                Required acknowledgements
              </p>

              <label className="border-border flex cursor-pointer items-start gap-3 border p-4">
                <Checkbox
                  checked={checkLocked}
                  onCheckedChange={(value) => setCheckLocked(Boolean(value))}
                  className="mt-0.5"
                />
                <span>
                  <span className="block text-sm font-semibold">
                    Project-unit availability
                  </span>
                  <span className="text-muted-foreground mt-1 block text-xs leading-relaxed">
                    I understand any resulting project-unit position follows
                    the recorded gate and settlement state, and sits in a
                    hold before it&apos;s mine to move. This preview does not
                    make the position liquid or transferable.
                  </span>
                </span>
              </label>

              <label className="border-border flex cursor-pointer items-start gap-3 border p-4">
                <Checkbox
                  checked={checkTrust}
                  onCheckedChange={(value) => setCheckTrust(Boolean(value))}
                  className="mt-0.5"
                />
                <span>
                  <span className="block text-sm font-semibold">
                    Protection is conditional
                  </span>
                  <span className="text-muted-foreground mt-1 block text-xs leading-relaxed">
                    I understand protection eligibility and amount depend on the
                    recorded terms, gate state, configured treasury rate, and
                    payout processing. It is not a guaranteed full refund. A
                    protection record is not proof that funds were paid out.
                  </span>
                </span>
              </label>

              <label className="border-border flex cursor-pointer items-start gap-3 border p-4">
                <Checkbox
                  checked={showName}
                  onCheckedChange={(value) => setShowName(Boolean(value))}
                  className="mt-0.5"
                />
                <span>
                  <span className="block text-sm font-semibold">
                    Show my name to other backers
                  </span>
                  <span className="text-muted-foreground mt-1 block text-xs leading-relaxed">
                    Optional, and off by default — your support is counted
                    either way. Leave it off and your pledge is only ever
                    counted in the total.
                  </span>
                </span>
              </label>
            </CardContent>
          </Card>

          <Button
            type="button"
            className="h-12 w-full font-mono font-bold tracking-wide uppercase"
            disabled={!checkLocked || !checkTrust || checkout.isPending}
            onClick={handleNext}
          >
            Support this project
            <ArrowRight className="size-4" />
          </Button>
        </section>
      )}

      {step === 3 && (
        <section aria-labelledby="checkout-heading">
          <Card
            className={cn(
              "border-foreground",
              checkout.error && "border-destructive",
            )}
          >
            <CardContent className="flex flex-col items-start gap-5 p-6 sm:p-8">
              {checkout.isPending ? (
                <Loader2
                  className="text-primary size-10 animate-spin"
                  aria-hidden="true"
                />
              ) : checkout.error ? (
                <AlertCircle
                  className="text-destructive size-10"
                  aria-hidden="true"
                />
              ) : (
                <ShieldCheck
                  className="text-primary size-10"
                  aria-hidden="true"
                />
              )}

              <div>
                <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                  03 · Confirming your sponsorship
                </p>
                <h2 id="checkout-heading" className="mt-2 text-2xl font-bold">
                  {checkout.isPending
                    ? "Asking the node to move your ARDA"
                    : checkout.error
                      ? "Sponsorship unavailable"
                      : "Redirecting…"}
                </h2>
                <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
                  {checkout.isPending
                    ? "The server locked your terms and is asking the node to complete the exchange. No stake is confirmed yet."
                    : checkout.error
                      ? describeFundingError(checkout.error)
                      : "Your sponsorship was accepted. Taking you to its status now."}
                </p>
              </div>

              <div className="border-border w-full border-y py-3 text-sm">
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">
                    Amount spent
                  </span>
                  <span className="font-mono font-bold">
                    {currentAmount
                      ? formatArda(currentAmount.value)
                      : "Unavailable"}
                  </span>
                </div>
              </div>

              {checkout.error && (
                <div className="w-full space-y-3">
                  <p className="text-muted-foreground text-xs leading-relaxed">
                    Retry uses the same idempotency key, so the server can
                    safely replay this sponsorship rather than create a
                    second one.
                  </p>
                  <Button
                    type="button"
                    onClick={beginCheckout}
                    className="w-full"
                  >
                    Retry sponsorship
                  </Button>
                </div>
              )}

              <div className="border-border text-muted-foreground flex items-start gap-2 border p-3 text-xs">
                <ShieldCheck
                  className="text-primary mt-0.5 size-4 shrink-0"
                  aria-hidden="true"
                />
                Final status comes from your actor-scoped sponsorship
                record, never from this page&apos;s redirect.
              </div>
            </CardContent>
          </Card>
        </section>
      )}
    </div>
  );
}
