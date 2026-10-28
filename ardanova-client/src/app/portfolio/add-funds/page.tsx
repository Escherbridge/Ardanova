"use client";

import { Suspense, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Clock,
  Info,
  Loader2,
  ShieldCheck,
} from "lucide-react";

import { buildSignInHref } from "~/lib/auth-navigation";
import {
  ADD_FUNDS_REQUEST_FLOW,
  canSetAsideSavedPayment,
  endsSavedPayment,
  type AddFundsWire,
} from "~/lib/commerce/add-funds-request";
import { moneyCodeOf } from "~/lib/commerce/money-refusal";
import {
  ADD_FUNDS_MAXIMUM_DOLLARS,
  ADD_FUNDS_MINIMUM_DOLLARS,
  describeAddFundsAmountProblem,
  getAddFundsReadiness,
  parseDollarsToMinorUnits,
  getOnRampConfirmingPresentation,
  getOnRampPresentation,
  isOnRampSettled,
  type OnRampPresentation,
  type OnRampReport,
} from "~/lib/commerce/on-ramp-status";
import {
  claimRequestKey,
  clearSavedRequest,
  readSavedRequest,
  recordAdmittedId,
  settleSavedRequestAfterFailure,
  type SavedRequest,
} from "~/lib/commerce/persisted-request-key";
import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";

/** The amounts most people pick, so most people never type. */
const PRESET_DOLLAR_AMOUNTS = [10, 25, 50, 100, 250] as const;

const MINIMUM_DOLLARS = ADD_FUNDS_MINIMUM_DOLLARS;
const MAXIMUM_DOLLARS = ADD_FUNDS_MAXIMUM_DOLLARS;

const AMOUNT_ERROR_ID = "custom-amount-error";
const READINESS_DETAIL_ID = "add-funds-readiness-detail";

const SAVED_PAYMENT_UNCONFIRMED_MESSAGE =
  "We couldn't confirm what happened to that earlier payment, so it stays saved. Nothing new was started - try again in a moment.";

function formatDollars(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: value % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatUnits(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    value,
  );
}

export default function AddFundsPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-[60vh] items-center justify-center">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      }
    >
      <AddFundsFlow />
    </Suspense>
  );
}

function AddFundsFlow() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returningIntentId = searchParams.get("intent");

  const [selectedDollars, setSelectedDollars] = useState<number | null>(25);
  const [customAmount, setCustomAmount] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  // Load-bearing: persisted to storage BEFORE a request leaves, so a reload resumes the SAME
  // admission instead of the node opening a second payment page for one intent. Rules:
  // ~/lib/commerce/AGENTS.md §Persisted request keys.
  const [savedRequest, setSavedRequest] =
    useState<SavedRequest<AddFundsWire> | null>(null);
  // What became of a saved payment this page resolved without a return (shown once it is cleared).
  const [resolvedSavedPayment, setResolvedSavedPayment] =
    useState<OnRampPresentation | null>(null);
  const [settingAside, setSettingAside] = useState(false);
  // The request in flight, for the callbacks; `savedRequest` only reflects what is on screen.
  const activeRequestRef = useRef<SavedRequest<AddFundsWire> | null>(null);

  const subjectId = session?.user?.id ?? null;
  const queryEnabled = Boolean(session?.user?.id);
  const utils = api.useUtils();
  const accountStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: queryEnabled,
    retry: false,
  });
  const ardaBalance = api.tokenBalances.getArdaBalance.useQuery(undefined, {
    enabled: queryEnabled,
    retry: false,
  });
  // The server is the only thing allowed to say what happened to a payment. A return polls the
  // payment it came back for; with no return, the saved key's own payment is read once on mount,
  // so an abandoned or paid-but-never-returned checkout resolves instead of looping on a 409.
  const trackedPaymentId = returningIntentId ?? savedRequest?.admittedId ?? null;
  const trackedPayment = api.addFunds.getPayment.useQuery(
    { paymentId: trackedPaymentId ?? "" },
    {
      enabled: queryEnabled && Boolean(trackedPaymentId),
      retry: false,
      refetchInterval: (query) =>
        !returningIntentId ||
        (query.state.data && isOnRampSettled(query.state.data as OnRampReport))
          ? false
          : 5000,
    },
  );
  const begin = api.addFunds.begin.useMutation({
    onSuccess: (payment, variables) => {
      // Saved BEFORE leaving: the payment id is how a later visit asks the server what became of
      // this key, whether or not the person ever comes back from the payment page.
      const recorded =
        subjectId &&
        recordAdmittedId(
          window.localStorage,
          ADD_FUNDS_REQUEST_FLOW,
          subjectId,
          variables.requestKey,
          payment.paymentId,
        );
      if (payment.paymentUrl) {
        window.location.assign(payment.paymentUrl);
        return;
      }
      // A resumed payment with no page left is already paid or closed: read its real state.
      if (recorded) setSavedRequest(recorded);
      router.replace(
        `/portfolio/add-funds?intent=${encodeURIComponent(payment.paymentId)}`,
      );
    },
    onError: (error, variables) => {
      setSubmitError(error.message);
      if (!subjectId) return;
      // Released ONLY on the server's `definite`; a missing code, a 4xx with no code, a network
      // drop all keep the key - the call may still have reached the node.
      const settled = settleSavedRequestAfterFailure(
        window.localStorage,
        ADD_FUNDS_REQUEST_FLOW,
        subjectId,
        variables.requestKey,
        moneyCodeOf(error),
      );
      setSavedRequest(
        settled === "released"
          ? null
          : (readSavedRequest(window.localStorage, ADD_FUNDS_REQUEST_FLOW, subjectId) ??
              activeRequestRef.current),
      );
    },
  });
  // A second instance so replaying the saved key to learn its fate never redirects anywhere.
  const replaySaved = api.addFunds.begin.useMutation();

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.replace(buildSignInHref("/portfolio/add-funds"));
    }
  }, [router, sessionStatus]);

  // Surfaces an unfinished attempt from BEFORE a reload (or a closed tab), so Continue below
  // resumes it instead of quietly starting a second payment for the same money. Pre-selects the
  // saved amount so a plain Continue is the resuming path, not a fresh one.
  useEffect(() => {
    if (!subjectId) return;
    const saved = readSavedRequest(
      window.localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      subjectId,
    );
    setSavedRequest(saved);
    if (!saved) return;

    const savedDollars = saved.wire.amountMinorUnits / 100;
    if ((PRESET_DOLLAR_AMOUNTS as readonly number[]).includes(savedDollars)) {
      setSelectedDollars(savedDollars);
    } else {
      setSelectedDollars(null);
      setCustomAmount(String(savedDollars));
    }
  }, [subjectId]);

  // Ends the saved key only when the server reports THAT key's own payment finished either way.
  // An unconfirmed read, an unfinished page, or a return for some other payment keeps it.
  useEffect(() => {
    if (!subjectId || !savedRequest || !trackedPayment.data) return;
    if (!endsSavedPayment(savedRequest, trackedPayment.data)) return;

    clearSavedRequest(
      window.localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      subjectId,
      savedRequest.requestKey,
    );
    setSavedRequest(null);
    if (!returningIntentId) {
      setResolvedSavedPayment(getOnRampPresentation(trackedPayment.data));
    }
  }, [trackedPayment.data, savedRequest, subjectId, returningIntentId]);

  const amountMinorUnits = useMemo(() => {
    if (selectedDollars !== null) return selectedDollars * 100;
    return parseDollarsToMinorUnits(customAmount);
  }, [customAmount, selectedDollars]);

  // Presets are always within bounds; only a typed amount can be wrong.
  const amountProblem = useMemo(
    () =>
      selectedDollars !== null
        ? null
        : describeAddFundsAmountProblem(customAmount),
    [customAmount, selectedDollars],
  );
  const showAmountError =
    amountProblem !== null && customAmount.trim().length > 0;

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  const readiness = getAddFundsReadiness(accountStatus.data, {
    pending: accountStatus.isLoading,
    loadFailed: Boolean(accountStatus.error),
    loadErrorCode: accountStatus.error?.data?.code ?? null,
  });

  // Never inferred from the redirect: until the record itself is read back, the return reads as
  // "we're confirming this" rather than either outcome.
  const returnPresentation: OnRampPresentation | null = !returningIntentId
    ? resolvedSavedPayment
    : trackedPayment.data
      ? getOnRampPresentation(trackedPayment.data as OnRampReport)
      : getOnRampConfirmingPresentation();

  function startAddFunds() {
    if (amountProblem) {
      setSubmitError(amountProblem);
      return;
    }
    if (amountMinorUnits === null || !subjectId) return;

    const claim = claimRequestKey(
      window.localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      subjectId,
      { amountMinorUnits },
      amountMinorUnits,
    );
    if (claim.kind === "pending") {
      // Never silently replaced: an unresolved key for a DIFFERENT amount must be resumed or
      // explicitly set aside first, or one lost payment page becomes two.
      setSavedRequest(claim.pending);
      setSubmitError(
        `You have an unfinished payment for ${formatDollars(claim.pending.displayMinorUnits / 100)}. Finish that one, or start a new payment instead below, before starting a different amount.`,
      );
      return;
    }

    // Not re-shown here: the "resuming" banner is for what a RELOAD (or a kept-key failure)
    // finds saved; a brand-new first attempt redirects away on success.
    activeRequestRef.current = claim.request;
    setSubmitError(null);
    begin.mutate({
      amountMinorUnits: claim.request.wire.amountMinorUnits,
      requestKey: claim.request.requestKey,
    });
  }

  /**
   * "Start a new payment instead" asks the server first: it reads the saved key's payment (or,
   * when the answer that named it was lost, replays the saved key without redirecting) and lets
   * the key go only on an outcome the node actually stated.
   */
  async function setAsideSavedRequest() {
    const saved = savedRequest;
    if (!saved || !subjectId || settingAside) return;
    setSettingAside(true);
    setSubmitError(null);

    try {
      const payment = saved.admittedId
        ? await utils.addFunds.getPayment.fetch(
            { paymentId: saved.admittedId },
            { staleTime: 0 },
          )
        : await replaySaved.mutateAsync({
            amountMinorUnits: saved.wire.amountMinorUnits,
            requestKey: saved.requestKey,
          });

      if (!canSetAsideSavedPayment(payment)) {
        setSavedRequest(
          recordAdmittedId(
            window.localStorage,
            ADD_FUNDS_REQUEST_FLOW,
            subjectId,
            saved.requestKey,
            payment.paymentId,
          ) ?? saved,
        );
        setSubmitError(SAVED_PAYMENT_UNCONFIRMED_MESSAGE);
        return;
      }

      clearSavedRequest(
        window.localStorage,
        ADD_FUNDS_REQUEST_FLOW,
        subjectId,
        saved.requestKey,
      );
      setSavedRequest(null);
      setResolvedSavedPayment(getOnRampPresentation(payment));
    } catch (error) {
      const settled = settleSavedRequestAfterFailure(
        window.localStorage,
        ADD_FUNDS_REQUEST_FLOW,
        subjectId,
        saved.requestKey,
        moneyCodeOf(error),
      );
      if (settled === "released") {
        setSavedRequest(null);
      } else {
        setSubmitError(SAVED_PAYMENT_UNCONFIRMED_MESSAGE);
      }
    } finally {
      setSettingAside(false);
    }
  }

  const notice = submitError;
  // Rendered whenever a key is saved - including on a `?intent=` return, where Stripe's success
  // AND cancel both land - so the way out is always on screen.
  const showResumeNotice = savedRequest !== null;

  // A disabled Continue must say why: point it at whichever reason is on screen.
  const continueDescribedBy =
    [
      readiness.heading !== null && readiness.detail
        ? READINESS_DETAIL_ID
        : null,
      showAmountError ? AMOUNT_ERROR_ID : null,
    ]
      .filter(Boolean)
      .join(" ") || undefined;

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl space-y-8 px-4 py-8">
        <header className="border-foreground space-y-4 border-b-2 pb-6">
          <Button asChild variant="ghost" size="sm" className="-ml-2">
            <Link href="/portfolio">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to your portfolio
            </Link>
          </Button>
          <div>
            <p className="text-primary font-mono text-xs tracking-[0.22em] uppercase">
              Your account
            </p>
            <h1 className="text-foreground mt-2 text-4xl font-black tracking-tight uppercase">
              Add funds
            </h1>
            <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed">
              Add money to your ArdaNova balance so you can back the projects
              you believe in. Your balance is held in ARDA, the unit ArdaNova
              uses across the site — {formatDollars(1)} adds 100 ARDA, backed
              one-to-one.
            </p>
          </div>
        </header>

        {returnPresentation && (
          <ReturnState presentation={returnPresentation} />
        )}

        {showResumeNotice && savedRequest && (
          <section
            role="status"
            className="border-foreground bg-secondary flex flex-col gap-3 border-2 p-5 sm:flex-row sm:items-center sm:justify-between"
          >
            <div className="flex items-start gap-3">
              <Clock className="mt-0.5 h-5 w-5 shrink-0" />
              <div>
                <h2 className="font-mono text-sm font-black uppercase">
                  Resuming an unfinished payment
                </h2>
                <p className="text-muted-foreground mt-1 text-sm">
                  You started adding{" "}
                  {formatDollars(savedRequest.displayMinorUnits / 100)} and
                  never saw it finish. Continue below to pick up that same
                  payment — nothing is charged twice. Starting a new one first
                  checks what became of this one.
                </p>
              </div>
            </div>
            <Button
              type="button"
              variant="outline"
              className="shrink-0"
              disabled={settingAside || begin.isPending}
              onClick={() => void setAsideSavedRequest()}
            >
              {settingAside ? "Checking…" : "Start a new payment instead"}
            </Button>
          </section>
        )}

        <section
          aria-labelledby="balance-heading"
          className="border-border bg-muted/20 border-2 p-5"
        >
          <h2
            id="balance-heading"
            className="text-muted-foreground font-mono text-[10px] tracking-[0.2em] uppercase"
          >
            Your balance now
          </h2>
          <p className="text-foreground mt-2 font-mono text-2xl font-bold">
            {ardaBalance.isLoading
              ? "—"
              : ardaBalance.error
                ? "Not available"
                : ardaBalance.data
                  ? `${formatUnits(ardaBalance.data.availableBalance)} ARDA ready to use`
                  : "Not available"}
          </p>
          {ardaBalance.error && (
            <p className="text-muted-foreground mt-1 text-xs">
              We couldn&apos;t read your balance just now. This is not a zero
              balance — nothing has changed.
            </p>
          )}
        </section>

        {readiness.heading !== null && (
          <section
            role="status"
            className="border-foreground bg-secondary flex flex-col gap-3 border-2 p-5 sm:flex-row sm:items-center sm:justify-between"
          >
            <div className="flex items-start gap-3">
              <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0" />
              <div>
                <h2 className="font-mono text-sm font-black uppercase">
                  {readiness.heading}
                </h2>
                {readiness.detail && (
                  <p
                    id={READINESS_DETAIL_ID}
                    className="text-muted-foreground mt-1 text-sm"
                  >
                    {readiness.detail}
                  </p>
                )}
              </div>
            </div>
            {/* Only offered when there is genuinely setup left to do. Somebody held for review
                has nothing to finish, so no call to action is rendered for them at all. */}
            {readiness.action && (
              <Button asChild className="shrink-0">
                <Link href={readiness.action.href}>{readiness.action.label}</Link>
              </Button>
            )}
            {readiness.canRetry && (
              <Button
                type="button"
                variant="outline"
                className="shrink-0"
                disabled={accountStatus.isFetching}
                onClick={() => void accountStatus.refetch()}
              >
                {accountStatus.isFetching ? "Checking…" : "Try again"}
              </Button>
            )}
          </section>
        )}

        <section aria-labelledby="amount-heading" className="space-y-4">
          <h2
            id="amount-heading"
            className="border-border border-b pb-2 font-mono text-sm tracking-widest uppercase"
          >
            How much would you like to add?
          </h2>

          <div className="flex flex-wrap gap-2">
            {PRESET_DOLLAR_AMOUNTS.map((dollars) => (
              <Button
                key={dollars}
                type="button"
                variant={selectedDollars === dollars ? "default" : "outline"}
                aria-pressed={selectedDollars === dollars}
                className="min-h-11 min-w-24 rounded-none"
                onClick={() => {
                  setSelectedDollars(dollars);
                  setCustomAmount("");
                  setSubmitError(null);
                }}
              >
                {formatDollars(dollars)}
              </Button>
            ))}
          </div>

          <div className="space-y-2">
            <Label htmlFor="custom-amount">Or type an amount</Label>
            <Input
              id="custom-amount"
              inputMode="decimal"
              autoComplete="off"
              placeholder="40.00"
              value={customAmount}
              aria-invalid={showAmountError || undefined}
              aria-describedby={
                showAmountError
                  ? `custom-amount-help ${AMOUNT_ERROR_ID}`
                  : "custom-amount-help"
              }
              className={
                showAmountError
                  ? "border-destructive focus-visible:ring-destructive"
                  : undefined
              }
              onChange={(event) => {
                setCustomAmount(event.target.value);
                setSelectedDollars(null);
                setSubmitError(null);
              }}
            />
            <p
              id="custom-amount-help"
              className="text-muted-foreground text-xs"
            >
              Between {formatDollars(MINIMUM_DOLLARS)} and{" "}
              {formatDollars(MAXIMUM_DOLLARS)}. You&apos;ll see the exact total
              before anything is charged.
            </p>
          </div>

          {showAmountError && (
            <p
              id={AMOUNT_ERROR_ID}
              className="text-destructive text-sm"
              role="alert"
            >
              {amountProblem}
            </p>
          )}
        </section>

        <section
          aria-labelledby="before-you-pay-heading"
          className="border-border space-y-3 border-2 p-5"
        >
          <h2
            id="before-you-pay-heading"
            className="flex items-center gap-2 font-mono text-sm tracking-widest uppercase"
          >
            <Info className="h-4 w-4" />
            Before you add funds
          </h2>
          <ul className="text-muted-foreground space-y-2 text-sm">
            <li>
              You can spend your balance on any project on ArdaNova, and you can
              hold it as long as you like.
            </li>
            <li>
              <strong className="text-foreground">
                You cannot cash out yet.
              </strong>{" "}
              Turning your balance back into money in your bank isn&apos;t
              available on ArdaNova yet. Until it is, the only way back is a
              refund of a recent payment. Please only add what you mean to put
              to work here.
            </li>
            <li>
              Backing a project is not a share, a loan, or a promise of a
              return.
            </li>
          </ul>
        </section>

        <section className="space-y-3">
          {notice && (
            <div
              role="status"
              className="border-border bg-muted/30 flex items-start gap-3 border-2 p-4"
            >
              <AlertCircle className="mt-0.5 h-5 w-5 shrink-0" />
              <p className="text-sm">{notice}</p>
            </div>
          )}

          <Button
            type="button"
            size="lg"
            className="min-h-12 w-full rounded-none"
            aria-describedby={continueDescribedBy}
            disabled={
              !readiness.canContinue ||
              amountProblem !== null ||
              begin.isPending ||
              settingAside
            }
            onClick={startAddFunds}
          >
            {amountMinorUnits !== null && amountProblem === null
              ? `Continue — ${formatDollars(amountMinorUnits / 100)}`
              : "Continue"}
          </Button>
          <p className="text-muted-foreground text-center text-xs">
            You&apos;ll be taken to a secure payment page and brought straight
            back here.
          </p>
        </section>
      </div>
    </div>
  );
}

function ReturnState({ presentation }: { presentation: OnRampPresentation }) {
  const tone =
    presentation.outcome === "added"
      ? {
          icon: <CheckCircle2 className="h-5 w-5" />,
          className: "border-foreground bg-primary text-primary-foreground",
          badge: "Done",
        }
      : presentation.outcome === "on-its-way" ||
          presentation.outcome === "unfinished" ||
          presentation.outcome === "confirming"
        ? {
            icon: <Clock className="h-5 w-5" />,
            className: "border-border bg-muted/30",
            badge: "In progress",
          }
        : {
            icon: <AlertCircle className="h-5 w-5" />,
            className: "border-destructive bg-destructive/10",
            badge: "Not added",
          };

  return (
    <Card
      className={`rounded-none border-2 ${tone.className}`}
      padding="sm"
      role="status"
      aria-live="polite"
    >
      <CardContent className="flex items-start gap-4 p-0">
        <span className="mt-0.5 shrink-0">{tone.icon}</span>
        <div className="space-y-1">
          <Badge variant="outline" size="sm">
            {tone.badge}
          </Badge>
          <h2 className="font-mono text-lg font-black uppercase">
            {presentation.heading}
          </h2>
          <p className="text-sm leading-relaxed opacity-90">
            {presentation.detail}
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
