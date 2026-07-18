"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Check,
  Info,
  Loader2,
  Lock,
  ShieldCheck,
} from "lucide-react";

import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Checkbox } from "~/components/ui/checkbox";
import { Input } from "~/components/ui/input";
import {
  fundingProjectTokenConfigSchema,
  parseFundingAmount,
} from "~/lib/commerce/investment-preview-contract";
import { cn } from "~/lib/utils";

const QUICK_AMOUNTS = [100, 500, 1000, 5000];
const STEP_LABELS = ["Amount", "Review", "Checkout"];

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

  const [step, setStep] = useState(1);
  const [usdInput, setUsdInput] = useState(initialAmount);
  const [debouncedInput, setDebouncedInput] = useState(initialAmount);
  const [checkLocked, setCheckLocked] = useState(false);
  const [checkTrust, setCheckTrust] = useState(false);
  const [handoffError, setHandoffError] = useState<string | null>(null);
  const checkoutIdempotencyKey = useRef<string | undefined>(undefined);

  const checkout = api.fundingIntent.createCheckout.useMutation({
    onSuccess: ({ intentId, checkoutUrl }) => {
      try {
        const destination = new URL(checkoutUrl);
        if (destination.protocol !== "https:") {
          throw new Error("Checkout destination must use HTTPS");
        }

        window.sessionStorage.setItem(
          `ardanova:funding-intent:${slug}`,
          intentId,
        );
        window.location.assign(destination.href);
      } catch {
        setHandoffError(
          "The server returned an invalid checkout destination. No payment or settlement is confirmed.",
        );
      }
    },
  });

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedInput(usdInput), 400);
    return () => window.clearTimeout(timer);
  }, [usdInput]);

  const projectQuery = api.project.getById.useQuery({ id: slug });
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

  const currentAmount = parseFundingAmount(usdInput);
  const previewAmount = parseFundingAmount(debouncedInput);
  const previewMatchesInput =
    currentAmount !== null &&
    previewAmount !== null &&
    currentAmount.apiAmount === previewAmount.apiAmount;

  const tokenValueQuery = api.exchange.getProjectTokenValue.useQuery(
    { configId: config?.id ?? "" },
    { enabled: Boolean(config?.id) },
  );
  const tokenAmount =
    previewAmount && tokenValueQuery.data && tokenValueQuery.data > 0
      ? Math.max(1, Math.round(previewAmount.value / tokenValueQuery.data))
      : null;
  const previewQuery = api.exchange.getConversionPreview.useQuery(
    {
      projectTokenConfigId: config?.id ?? "",
      tokenAmount: tokenAmount ?? 1,
    },
    {
      enabled: Boolean(config?.id && tokenAmount && tokenAmount > 0),
    },
  );
  const estimateError = tokenValueQuery.error ?? previewQuery.error;
  const preview =
    previewMatchesInput && !estimateError ? previewQuery.data : undefined;
  const supplyShare =
    preview && config && config.totalSupply > 0
      ? (preview.sourceTokenAmount / config.totalSupply) * 100
      : null;

  const handleQuickAmount = useCallback((amount: number) => {
    const next = String(amount);
    setUsdInput(next);
    setDebouncedInput(next);
  }, []);

  const beginCheckout = () => {
    if (!config || !currentAmount || !preview || !previewMatchesInput) return;

    checkoutIdempotencyKey.current ??= window.crypto.randomUUID();
    setHandoffError(null);
    setStep(3);
    checkout.mutate({
      projectTokenConfigId: config.id,
      amount: currentAmount.apiAmount,
      disclosureVersion: "funding-disclosure-v1",
      idempotencyKey: checkoutIdempotencyKey.current,
    });
  };

  const handleNext = () => {
    if (step === 1 && preview && previewMatchesInput) setStep(2);
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
            We could not verify this project&apos;s token configuration against
            the funding contract. Checkout is disabled until the data is
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
            This project is not accepting new funding intents in its current
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
          Review an estimate, acknowledge the current terms, then create a
          durable funding intent. Provider checkout is a handoff—not proof of
          payment, token allocation, or reconciled settlement.
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
                  Funding amount
                </h2>
              </div>

              <div className="flex flex-wrap gap-2" aria-label="Quick amounts">
                {QUICK_AMOUNTS.map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => handleQuickAmount(amount)}
                    aria-pressed={usdInput === String(amount)}
                    className={cn(
                      "min-h-11 border px-4 font-mono text-sm font-bold transition-colors",
                      usdInput === String(amount)
                        ? "border-primary bg-primary text-primary-foreground"
                        : "border-border bg-background text-foreground hover:border-foreground",
                    )}
                  >
                    ${amount.toLocaleString()}
                  </button>
                ))}
              </div>

              <div>
                <label
                  htmlFor="funding-amount"
                  className="mb-2 block text-sm font-semibold"
                >
                  Custom amount in USD
                </label>
                <div className="relative">
                  <span
                    className="text-muted-foreground absolute top-1/2 left-3 -translate-y-1/2 font-mono"
                    aria-hidden="true"
                  >
                    $
                  </span>
                  <Input
                    id="funding-amount"
                    type="number"
                    inputMode="decimal"
                    value={usdInput}
                    onChange={(event) => setUsdInput(event.target.value)}
                    className="border-foreground h-12 pl-8 font-mono text-lg"
                    placeholder="0.00"
                    min="0.01"
                    step="0.01"
                    aria-invalid={currentAmount === null}
                    aria-describedby="funding-amount-help"
                  />
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
                    : "Enter a positive USD amount with no more than two decimal places."}
                </p>
              </div>
            </CardContent>
          </Card>

          <Card className="border-primary bg-primary/5">
            <CardContent className="space-y-4 p-5 sm:p-6">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-primary font-mono text-xs font-bold tracking-widest uppercase">
                    Project-token position estimate
                  </p>
                  <h2 className="mt-2 text-xl font-bold">
                    What the API can preview
                  </h2>
                </div>
                <Info
                  className="text-primary size-5 shrink-0"
                  aria-hidden="true"
                />
              </div>

              {!currentAmount ? (
                <p className="text-muted-foreground text-sm">
                  Enter a valid amount to request an estimate.
                </p>
              ) : !previewMatchesInput ||
                tokenValueQuery.isLoading ||
                previewQuery.isLoading ? (
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
                    The conversion service did not return a valid preview.
                    Checkout remains disabled.
                  </p>
                </div>
              ) : preview ? (
                <dl className="divide-border border-border divide-y border-y">
                  <DataRow
                    label="Funding amount"
                    value={formatUSD(currentAmount.value)}
                  />
                  <DataRow
                    label="Estimated project-token units"
                    value={formatToken(
                      preview.sourceTokenAmount,
                      config.unitName,
                    )}
                    emphasis
                  />
                  <DataRow
                    label="Share of configured token supply"
                    value={
                      supplyShare === null
                        ? "Not available"
                        : formatPct(supplyShare)
                    }
                  />
                  <DataRow
                    label="Reference value per project token"
                    value={formatUSD(preview.projectTokenValueUsd)}
                  />
                  <DataRow
                    label="Reference value of estimated units"
                    value={formatUSD(preview.usdValue)}
                  />
                  <DataRow
                    label="Informational ARDA equivalent"
                    value={`${new Intl.NumberFormat("en-US").format(preview.ardaAmount)} ARDA`}
                  />
                  <DataRow
                    label="Reference value per ARDA"
                    value={formatUSD(preview.ardaValueUsd)}
                  />
                </dl>
              ) : (
                <p className="text-muted-foreground text-sm">
                  No preview is available for this amount. Checkout remains
                  disabled.
                </p>
              )}

              <div className="border-primary text-muted-foreground border-l-4 pl-4 text-xs leading-relaxed">
                <p>
                  This estimate does not execute a project-token-to-ARDA
                  conversion. Rates can change before settlement.
                </p>
                <p className="text-foreground mt-2 font-semibold">
                  A project-token supply percentage is not equity, governance
                  power, a redemption right, or a guaranteed USD value.
                </p>
              </div>
            </CardContent>
          </Card>

          <Button
            type="button"
            className="h-12 w-full font-mono font-bold tracking-wide uppercase"
            disabled={!preview || !previewMatchesInput}
            onClick={handleNext}
          >
            Review the funding intent
            <ArrowRight className="size-4" />
          </Button>
        </section>
      )}

      {step === 2 && preview && currentAmount && (
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
              <dl className="divide-border border-border divide-y border-y">
                <DataRow label="Project" value={projectTitle} />
                <DataRow
                  label="Funding amount"
                  value={formatUSD(currentAmount.value)}
                  emphasis
                />
                <DataRow
                  label="Estimated project-token units"
                  value={formatToken(
                    preview.sourceTokenAmount,
                    config.unitName,
                  )}
                />
                <DataRow
                  label="Configured supply share"
                  value={
                    supplyShare === null
                      ? "Not available"
                      : formatPct(supplyShare)
                  }
                />
              </dl>
              <p className="text-muted-foreground text-xs leading-relaxed">
                The server will create an actor-owned, idempotent funding intent
                before opening Stripe. A provider return does not confirm
                payment, allocate project tokens, or prove reconciled
                settlement.
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
                    Project-token availability
                  </span>
                  <span className="text-muted-foreground mt-1 block text-xs leading-relaxed">
                    I understand any resulting project-token position follows
                    the recorded gate and settlement state. This preview does
                    not make the position liquid or transferable.
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
            </CardContent>
          </Card>

          <Button
            type="button"
            className="h-12 w-full font-mono font-bold tracking-wide uppercase"
            disabled={!checkLocked || !checkTrust || checkout.isPending}
            onClick={handleNext}
          >
            Create secure checkout
            <ArrowRight className="size-4" />
          </Button>
        </section>
      )}

      {step === 3 && (
        <section aria-labelledby="checkout-heading">
          <Card
            className={cn(
              "border-foreground",
              (checkout.error || handoffError) && "border-destructive",
            )}
          >
            <CardContent className="flex flex-col items-start gap-5 p-6 sm:p-8">
              {checkout.isPending ? (
                <Loader2
                  className="text-primary size-10 animate-spin"
                  aria-hidden="true"
                />
              ) : checkout.error || handoffError ? (
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
                  03 · Checkout handoff
                </p>
                <h2 id="checkout-heading" className="mt-2 text-2xl font-bold">
                  {checkout.isPending
                    ? "Creating a durable funding intent"
                    : checkout.error || handoffError
                      ? "Checkout handoff unavailable"
                      : "Opening secure checkout"}
                </h2>
                <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
                  {checkout.isPending
                    ? "The server is recording your intent before requesting a provider checkout. No payment is confirmed yet."
                    : (handoffError ??
                      checkout.error?.message ??
                      "Your provider checkout is opening. Funding remains unconfirmed until server verification and settlement processing complete.")}
                </p>
              </div>

              <div className="border-border w-full border-y py-3 text-sm">
                <div className="flex justify-between gap-4">
                  <span className="text-muted-foreground">Funding amount</span>
                  <span className="font-mono font-bold">
                    {currentAmount
                      ? formatUSD(currentAmount.value)
                      : "Unavailable"}
                  </span>
                </div>
              </div>

              {(checkout.error || handoffError) && (
                <div className="w-full space-y-3">
                  <p className="text-muted-foreground text-xs leading-relaxed">
                    No payment or settlement is confirmed. Retry uses the same
                    idempotency key so the server can safely replay the intent.
                  </p>
                  <Button
                    type="button"
                    onClick={beginCheckout}
                    className="w-full"
                  >
                    Retry checkout handoff
                  </Button>
                </div>
              )}

              <div className="border-border text-muted-foreground flex items-start gap-2 border p-3 text-xs">
                <ShieldCheck
                  className="text-primary mt-0.5 size-4 shrink-0"
                  aria-hidden="true"
                />
                Checkout destinations must use HTTPS. Final status comes from
                the actor-scoped funding record, not browser redirect
                parameters.
              </div>
            </CardContent>
          </Card>
        </section>
      )}
    </div>
  );
}

function DataRow({
  label,
  value,
  emphasis = false,
}: {
  label: string;
  value: string;
  emphasis?: boolean;
}) {
  return (
    <div className="grid grid-cols-[minmax(0,1fr)_auto] items-baseline gap-4 py-3">
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
