"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  Check,
  Clock,
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
import { moneyCodeOf } from "~/lib/commerce/money-refusal";
import {
  claimRequestKey,
  clearSavedRequest,
  readSavedRequest,
  settleSavedRequestAfterFailure,
  type SavedRequest,
} from "~/lib/commerce/persisted-request-key";
import { fundingProjectTokenConfigSchema } from "~/lib/commerce/investment-preview-contract";
import {
  SPONSOR_AMOUNT_HELP,
  dollarsToPlatformUnits,
  formatSponsorDollars,
  parseSponsorDollars,
  platformUnitsToDollars,
} from "~/lib/commerce/sponsor-amount-contract";
import {
  draftSponsorshipRequest,
  sponsorshipRequestFlow,
  type SponsorshipWire,
} from "~/lib/commerce/sponsorship-request";
import { reviewContactHref } from "~/components/azoa/custodial-account-card";
import { evaluateValueActionAccount } from "~/lib/contracts/project-create-contract";
import { cn } from "~/lib/utils";

/** Dollars, because that is what a sponsor is choosing between. */
const QUICK_AMOUNTS = [25, 100, 250, 1000];
const STEP_LABELS = ["Amount", "Review", "Support"];

/** sessionStorage key both this page and /invest/success read the quoted intent under. */
function intentStorageKey(slug: string) {
  return `ardanova:funding-intent:${slug}`;
}

/** The review-step quote a send was made from; null when the bytes sent are a resumed request. */
type SentQuote = {
  quantity: number;
  unitName: string;
  supplySharePercent: number | null;
  dollarAmount: number | null;
} | null;

/** One send in flight: the exact saved request, and the quote it matches (if any). */
type ActiveSend = { request: SavedRequest<SponsorshipWire>; quote: SentQuote };

/** What this page persists for the success page to show as "as quoted", never as confirmed. */
interface QuotedIntent {
  intentId: string;
  /** What the person actually named, in dollars. The figure the receipt leads with. */
  dollarAmount: number | null;
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
  const { data: session, status: sessionStatus } = useSession();
  const subjectId = session?.user?.id ?? null;
  const slug = params.slug as string;
  // Dollars. The project page hands this over already in dollars (equity-preview.tsx).
  const initialAmount = searchParams.get("amount") ?? "100";
  const accountSetupHref = `/settings/verification?returnTo=${encodeURIComponent(
    `/projects/${slug}/invest`,
  )}`;

  const [step, setStep] = useState(1);
  const [dollarInput, setDollarInput] = useState(initialAmount);
  const [debouncedInput, setDebouncedInput] = useState(initialAmount);
  const [checkLocked, setCheckLocked] = useState(false);
  const [checkTrust, setCheckTrust] = useState(false);
  const [showName, setShowName] = useState(false);
  // Load-bearing: the key AND the exact bytes it covers, persisted BEFORE a request leaves, so a
  // reload or Retry resends the SAME request and `FundingIntentService` replays the intent it
  // already admitted instead of debiting twice. Rules: ~/lib/commerce/AGENTS.md §Persisted request keys.
  const [savedRequest, setSavedRequest] =
    useState<SavedRequest<SponsorshipWire> | null>(null);
  // The send in flight: state for the step-3 render, a ref for the callbacks that outlive it.
  const [activeSend, setActiveSend] = useState<ActiveSend | null>(null);
  const activeSendRef = useRef<ActiveSend | null>(null);

  const checkout = api.fundingIntent.createCheckout.useMutation({
    onSuccess: ({ intentId, checkoutUrl }, variables) => {
      // This create-or-replay now has a definite, successful answer, so the key that covered it
      // is done; a later sponsorship of this same project is a NEW attempt.
      if (subjectId) {
        clearSavedRequest(
          window.localStorage,
          sponsorshipRequestFlow(variables.projectTokenConfigId),
          subjectId,
          variables.idempotencyKey,
        );
      }
      setSavedRequest(null);

      // The success page shows "as quoted" only for a send that matched the review-step quote;
      // a resumed request's bytes predate this screen, so it carries no quote of its own.
      const sent = activeSendRef.current;
      try {
        window.sessionStorage.setItem(
          intentStorageKey(slug),
          JSON.stringify({
            intentId,
            dollarAmount:
              sent?.quote?.dollarAmount ??
              (sent ? sent.request.displayMinorUnits / 100 : null),
            ardaAmount: Number(variables.amount),
            quantity: sent?.quote?.quantity ?? 0,
            unitName: sent?.quote?.unitName ?? "",
            supplySharePercent: sent?.quote?.supplySharePercent ?? null,
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
    onError: (error, variables) => {
      if (!subjectId) return;
      // Released ONLY on the BFF's `definite`, which it emits solely for refusals raised before
      // the intent was persisted (or for a final state). A post-commit 400 from the BFF's
      // exception middleware carries no code, so the key - and Retry's replay - survive it.
      const flow = sponsorshipRequestFlow(variables.projectTokenConfigId);
      const settled = settleSavedRequestAfterFailure(
        window.localStorage,
        flow,
        subjectId,
        variables.idempotencyKey,
        moneyCodeOf(error),
      );
      setSavedRequest(
        settled === "released"
          ? null
          : (readSavedRequest(window.localStorage, flow, subjectId) ??
              activeSendRef.current?.request ??
              null),
      );
    },
  });

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedInput(dollarInput), 400);
    return () => window.clearTimeout(timer);
  }, [dollarInput]);

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

  // Surfaces an unfinished sponsorship from BEFORE a reload (or a closed tab), so the "Retry
  // uses the same idempotency key" promise at step 3 holds even after one, and a changed amount
  // is never silently admitted as a second sponsorship of the same project.
  useEffect(() => {
    if (!config?.id || !subjectId) return;
    setSavedRequest(
      readSavedRequest(window.localStorage, sponsorshipRequestFlow(config.id), subjectId),
    );
  }, [config?.id, subjectId]);

  // What the person typed, in DOLLARS. The platform-unit figure is derived from it below
  // and never typed - the founder's ask was inverted this way first, and a sponsor
  // spending in a unit they never chose was the other half of the same story.
  const currentDollars = parseSponsorDollars(dollarInput);
  const previewDollars = parseSponsorDollars(debouncedInput);
  const previewMatchesInput =
    currentDollars !== null &&
    previewDollars !== null &&
    currentDollars.normalized === previewDollars.normalized;

  const ardaBalanceQuery = api.tokenBalances.getArdaBalance.useQuery(
    undefined,
    { enabled: valueAccountReady, retry: false },
  );
  const availableArda = ardaBalanceQuery.data?.availableBalance ?? null;

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

  // Dollars -> platform units -> project units. The middle leg is the wire contract, not
  // a thing a person is asked to hold in their head; the route still settles in ARDA.
  const currentArda =
    currentDollars && dollarsToPlatformUnits(currentDollars.value, usdPerArda);
  const previewArda =
    previewDollars && dollarsToPlatformUnits(previewDollars.value, usdPerArda);

  const insufficientArda =
    availableArda !== null &&
    currentArda !== null &&
    currentArda !== undefined &&
    currentArda > availableArda;
  const availableDollars = platformUnitsToDollars(availableArda ?? 0, usdPerArda);

  const quantity =
    previewMatchesInput && previewArda && ardaPerUnit && ardaPerUnit > 0
      ? Math.floor(previewArda / ardaPerUnit)
      : null;
  const chargedArda =
    quantity && quantity > 0 && ardaPerUnit
      ? Math.ceil(quantity * ardaPerUnit * 100) / 100
      : null;
  const chargedDollars = 
    chargedArda !== null ? platformUnitsToDollars(chargedArda, usdPerArda) : null;
  const remainderDollars =
    previewDollars && chargedDollars !== null
      ? Math.max(0, Math.round((previewDollars.value - chargedDollars) * 100) / 100)
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
    dollarAmount: number | null;
  } | null>(null);
  useEffect(() => {
    if (hasQuote && quantity && config) {
      lastQuoteRef.current = {
        quantity,
        unitName: config.unitName,
        supplySharePercent: supplyShare,
        dollarAmount: chargedDollars,
      };
    }
  }, [hasQuote, quantity, config, supplyShare, chargedDollars]);

  const handleQuickAmount = useCallback((amount: number) => {
    const next = String(amount);
    setDollarInput(next);
    setDebouncedInput(next);
  }, []);

  // What a FRESH sponsorship of the typed dollars would send at today's rate. Only ever used to
  // mint a key or to recognise the saved request; a saved request is never rebuilt from it.
  const currentDraft =
    config && currentDollars
      ? draftSponsorshipRequest({
          projectTokenConfigId: config.id,
          dollars: currentDollars.value,
          usdPerArda,
          displayNameConsent: showName,
        })
      : null;
  const sessionReady = sessionStatus !== "loading" && subjectId !== null;

  /** Sends EXACTLY the saved bytes under their key. */
  const sendSponsorship = (request: SavedRequest<SponsorshipWire>) => {
    const send: ActiveSend = {
      request,
      quote:
        currentDraft?.amount === request.wire.amount ? lastQuoteRef.current : null,
    };
    activeSendRef.current = send;
    setActiveSend(send);
    setStep(3);
    checkout.mutate({ ...request.wire, idempotencyKey: request.requestKey });
  };

  const beginCheckout = () => {
    if (
      !config ||
      !currentDraft ||
      !hasQuote ||
      !previewMatchesInput ||
      !currentDollars ||
      !sessionReady ||
      !subjectId
    ) {
      return;
    }

    const claim = claimRequestKey(
      window.localStorage,
      sponsorshipRequestFlow(config.id),
      subjectId,
      currentDraft,
      Math.round(currentDollars.value * 100),
    );
    if (claim.kind === "pending") {
      // Never silently replaced: an unresolved key for a DIFFERENT request must be resumed
      // first, or one lost sponsorship becomes two debits.
      setSavedRequest(claim.pending);
      setStep(1);
      return;
    }

    sendSponsorship(claim.request);
  };

  /**
   * Retry, and the banner's way out, both REPLAY the saved request - its own amount string,
   * never one recomputed at today's rate (which the server would refuse as a different
   * sponsorship). The key is dropped only on a final answer.
   */
  const resumeSavedSponsorship = () => {
    if (!config || !sessionReady || !subjectId) return;
    const saved =
      readSavedRequest(window.localStorage, sponsorshipRequestFlow(config.id), subjectId) ??
      savedRequest;
    if (saved) sendSponsorship(saved);
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
          ? "We could not confirm account setup. To protect your funds, support remains unavailable until the status can be confirmed."
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
          this project with funds from your balance. This is a durable
          sponsorship, not a hosted checkout — a browser redirect is never
          proof of a settled stake.
        </p>
      </header>

      <div className="my-6">
        <StepIndicator current={step} />
      </div>

      {savedRequest && step < 3 && (
        <section
          role="status"
          className="border-foreground bg-secondary mb-5 flex flex-col gap-3 border-2 p-5 sm:flex-row sm:items-center sm:justify-between"
        >
          <div className="flex items-start gap-3">
            <Clock className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <h2 className="font-mono text-sm font-black uppercase">
                Resuming an unfinished sponsorship
              </h2>
              <p className="text-muted-foreground mt-1 text-sm">
                {currentDraft?.amount === savedRequest.wire.amount ? (
                  <>
                    You started sponsoring this project with{" "}
                    {formatSponsorDollars(savedRequest.displayMinorUnits / 100)}{" "}
                    and never saw it finish. Supporting below picks up that
                    same sponsorship — nothing is debited twice.
                  </>
                ) : (
                  <>
                    You have an unfinished sponsorship of this project for{" "}
                    {formatSponsorDollars(savedRequest.displayMinorUnits / 100)}
                    , which does not match the amount entered now. It may
                    already be on its way, so it can&apos;t simply be dropped:
                    resuming sends exactly the same request again. If it
                    already went through, nothing is debited twice; if it was
                    refused or closed, it is cleared and you can continue with
                    this amount.
                  </>
                )}
              </p>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            className="shrink-0"
            disabled={!sessionReady || checkout.isPending}
            onClick={resumeSavedSponsorship}
          >
            Resume it
          </Button>
        </section>
      )}

      {step === 1 && (
        <section className="space-y-5" aria-labelledby="amount-heading">
          <Card className="border-foreground">
            <CardContent className="space-y-5 p-5 sm:p-6">
              <div>
                <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                  01 · Define an amount
                </p>
                <h2 id="amount-heading" className="mt-2 text-2xl font-bold">
                  How much would you like to put in?
                </h2>
              </div>

              <div className="flex flex-wrap gap-2" aria-label="Quick amounts">
                {QUICK_AMOUNTS.map((amount) => (
                  <button
                    key={amount}
                    type="button"
                    onClick={() => handleQuickAmount(amount)}
                    aria-pressed={dollarInput === String(amount)}
                    className={cn(
                      "min-h-11 border px-4 font-mono text-sm font-bold transition-colors",
                      dollarInput === String(amount)
                        ? "border-primary bg-primary text-primary-foreground"
                        : "border-border bg-background text-foreground hover:border-foreground",
                    )}
                  >
                    {formatSponsorDollars(amount)}
                  </button>
                ))}
              </div>

              <div>
                <label
                  htmlFor="funding-amount"
                  className="mb-2 block text-sm font-semibold"
                >
                  Another amount
                </label>
                <div className="relative">
                  {/* The affix is a dollar sign, on the LEFT, where money is written. The
                      ticker chip that used to sit on the right is the same shape the
                      founder's ask lost when it became a dollar field. */}
                  <span
                    className="text-muted-foreground absolute top-1/2 left-4 -translate-y-1/2 font-mono text-lg"
                    aria-hidden="true"
                  >
                    $
                  </span>
                  <Input
                    id="funding-amount"
                    type="number"
                    inputMode="decimal"
                    value={dollarInput}
                    onChange={(event) => setDollarInput(event.target.value)}
                    className="border-foreground h-12 pl-9 font-mono text-lg"
                    placeholder="0.00"
                    min="1"
                    step="0.01"
                    aria-invalid={currentDollars === null}
                    aria-describedby="funding-amount-help"
                  />
                </div>
                <p
                  id="funding-amount-help"
                  className={cn(
                    "mt-2 text-xs",
                    currentDollars
                      ? "text-muted-foreground"
                      : "text-destructive",
                  )}
                >
                  {SPONSOR_AMOUNT_HELP}
                </p>
              </div>

              {availableDollars !== null && (
                <p className="text-muted-foreground text-xs">
                  You have {formatSponsorDollars(availableDollars)} available.
                </p>
              )}

              {insufficientArda && currentDollars && availableDollars !== null && (
                <div
                  className="border-destructive bg-destructive/5 flex items-start justify-between gap-4 border p-3"
                  role="alert"
                >
                  <p className="text-destructive text-sm">
                    You have {formatSponsorDollars(availableDollars)} available,
                    but this needs {formatSponsorDollars(currentDollars.value)}.
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
                  What this could get you
                </h2>
              </div>

              {!currentDollars ? (
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
                    dollarAmount={chargedDollars ?? 0}
                    ardaAmount={chargedArda}
                    projectUnits={quantity}
                    unitName={config.unitName}
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

                  {remainderDollars > 0 && (
                    <p className="text-muted-foreground text-xs">
                      Whole units only — {formatSponsorDollars(remainderDollars)}
                      {" "}above the cost of {formatUnits(quantity, config.unitName)}
                      {" "}stays in your balance.
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
                  This estimate does not spend anything or create project units.
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

      {step === 2 && hasQuote && currentDollars && quantity && chargedArda !== null && (
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
                dollarAmount={chargedDollars ?? 0}
                ardaAmount={chargedArda}
                projectUnits={quantity}
                unitName={config.unitName}
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
                sponsorship and starts moving exactly the amounts above.
                Nothing here confirms settlement — the next screen reads your
                actual sponsorship record.
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
            // Until the session resolves there is no subject to save a key under, and a press
            // that silently does nothing reads as a broken button.
            disabled={
              !checkLocked || !checkTrust || checkout.isPending || !sessionReady
            }
            onClick={handleNext}
          >
            {sessionReady ? "Support this project" : "Checking your session…"}
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
                    ? "Moving your funds now"
                    : checkout.error
                      ? "Sponsorship unavailable"
                      : "Redirecting…"}
                </h2>
                <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
                  {checkout.isPending
                    ? "Your terms are locked in and the exchange is being completed now. No stake is confirmed yet."
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
                    {activeSend?.quote?.dollarAmount != null
                      ? formatSponsorDollars(activeSend.quote.dollarAmount)
                      : activeSend
                        ? formatSponsorDollars(activeSend.request.displayMinorUnits / 100)
                        : "Unavailable"}
                  </span>
                </div>
              </div>

              {checkout.error && savedRequest && (
                <div className="w-full space-y-3">
                  <p className="text-muted-foreground text-xs leading-relaxed">
                    Retry sends exactly the same request under the same
                    idempotency key, so the server can safely replay this
                    sponsorship rather than create a second one.
                  </p>
                  <Button
                    type="button"
                    onClick={resumeSavedSponsorship}
                    disabled={!sessionReady}
                    className="w-full"
                  >
                    Retry sponsorship
                  </Button>
                </div>
              )}

              {checkout.error && !savedRequest && (
                <div className="w-full space-y-3">
                  <p className="text-muted-foreground text-xs leading-relaxed">
                    The server gave a final answer for that sponsorship, so it
                    is closed. You can start a new one.
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setStep(1)}
                    className="w-full"
                  >
                    Change the amount
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
