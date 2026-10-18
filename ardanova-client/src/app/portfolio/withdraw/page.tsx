"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import { Suspense, useEffect, useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowLeft,
  Banknote,
  CheckCircle2,
  Clock,
  Loader2,
  XCircle,
} from "lucide-react";

import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";
import { buildSignInHref } from "~/lib/auth-navigation";
import { getCancellablePayouts } from "~/lib/commerce/portfolio-contract";
import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "~/components/ui/dialog";

/** Plain words for a sale's stage. The API's own status names are not shown to anyone. */
const saleStageLabel: Record<PayoutRequestDto["status"], string> = {
  PENDING: "Waiting to go through",
  PROCESSING: "Going through now",
  COMPLETED: "Done — the funds are in your balance",
  FAILED: "Did not go through — your units were returned",
  CANCELLED: "Called off — your units were returned",
};

function formatInteger(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    value,
  );
}

function formatMoney(minorUnits: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(minorUnits / 100);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatDay(value: string): string {
  return new Intl.DateTimeFormat("en-US", { dateStyle: "long" }).format(
    new Date(value),
  );
}

function shortenReference(value: string): string {
  return value.length > 22
    ? `${value.slice(0, 10)}...${value.slice(-8)}`
    : value;
}

/**
 * Dollars typed by a person, as exact cents.
 *
 * Parsed off the digits rather than through a float: `Math.round(12.29 * 100)`
 * is one of the classic ways a cent goes missing, and money that is off by a
 * cent is money somebody has to explain.
 */
function toMinorUnits(text: string): number | null {
  const trimmed = text.trim().replace(/^\$/, "").replace(/,/g, "");
  if (!/^\d+(\.\d{0,2})?$/.test(trimmed)) return null;

  const [whole, fraction = ""] = trimmed.split(".");
  const cents = Number.parseInt(`${whole}${fraction.padEnd(2, "0")}`, 10);
  return Number.isSafeInteger(cents) && cents > 0 ? cents : null;
}

/** A key that is stable for one composed withdrawal, so a second tap resumes rather than repeats. */
function newRequestKey(): string {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `withdraw-${Date.now()}-${Math.floor(Math.random() * 1e9)}`;
}

function WithdrawPageInner() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const searchParams = useSearchParams();
  const utils = api.useUtils();

  const [amountText, setAmountText] = useState("");
  const [requestKey, setRequestKey] = useState(newRequestKey);
  const [sentMessage, setSentMessage] = useState<string | null>(null);
  const [pendingCancellation, setPendingCancellation] =
    useState<PayoutRequestDto | null>(null);
  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const selectedConfigId = searchParams.get("configId") ?? undefined;
  const signedIn = Boolean(session?.user?.id);

  const availability = api.payouts.getWithdrawAvailability.useQuery(undefined, {
    enabled: signedIn,
    retry: false,
  });
  const destination = api.payouts.getWithdrawDestination.useQuery(undefined, {
    enabled: signedIn,
    retry: false,
  });
  const withdrawals = api.payouts.getWithdrawals.useQuery(undefined, {
    enabled: signedIn,
    retry: false,
  });
  const sales = api.payouts.getPayoutsByUser.useQuery(undefined, {
    enabled: signedIn,
  });

  const beginSetup = api.payouts.beginWithdrawDestinationSetup.useMutation({
    onSuccess: async (result) => {
      await utils.payouts.getWithdrawDestination.invalidate();
      if (result.setupUrl) window.location.assign(result.setupUrl);
    },
  });

  const withdraw = api.payouts.withdraw.useMutation({
    onSuccess: async (record) => {
      setSentMessage(record.statusMessage);
      setAmountText("");
      setRequestKey(newRequestKey());
      await Promise.all([
        utils.payouts.getWithdrawAvailability.invalidate(),
        utils.payouts.getWithdrawals.invalidate(),
        utils.tokenBalances.getArdaBalance.invalidate(),
      ]);
    },
  });

  const cancelSale = api.payouts.cancelPayout.useMutation({
    onMutate: ({ payoutRequestId }) => setCancellingId(payoutRequestId),
    onSuccess: async () => {
      setPendingCancellation(null);
      await Promise.all([
        utils.payouts.getPayoutsByUser.invalidate(),
        utils.payouts.getSellableUnits.invalidate(),
        utils.tokenBalances.getPortfolio.invalidate(),
      ]);
    },
    onSettled: () => setCancellingId(null),
  });

  const amountMinorUnits = useMemo(
    () => toMinorUnits(amountText),
    [amountText],
  );

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      const callbackUrl = selectedConfigId
        ? `/portfolio/withdraw?configId=${encodeURIComponent(selectedConfigId)}`
        : "/portfolio/withdraw";
      router.replace(buildSignInHref(callbackUrl));
    }
  }, [router, selectedConfigId, sessionStatus]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  const quote = availability.data ?? null;
  const bank = destination.data ?? null;
  const bankReady = bank?.readyToReceive ?? false;

  const belowMinimum =
    quote !== null &&
    amountMinorUnits !== null &&
    amountMinorUnits < quote.minimumAmountMinorUnits;
  const aboveMaximum =
    quote !== null &&
    amountMinorUnits !== null &&
    amountMinorUnits > quote.maximumAmountMinorUnits;
  const moreThanAvailable =
    quote !== null &&
    amountMinorUnits !== null &&
    amountMinorUnits > quote.cashableMinorUnits;

  const amountProblem = belowMinimum
    ? `The smallest amount you can send is ${formatMoney(quote.minimumAmountMinorUnits)}.`
    : aboveMaximum
      ? `The most you can send at once is ${formatMoney(quote.maximumAmountMinorUnits)}.`
      : moreThanAvailable
        ? `You have ${formatMoney(quote.cashableMinorUnits)} ready to send right now.`
        : null;

  const canSend =
    bankReady &&
    (quote?.available ?? false) &&
    amountMinorUnits !== null &&
    amountProblem === null &&
    !withdraw.isPending;

  const cancellableSales = getCancellablePayouts(
    sales.data ?? [],
    selectedConfigId,
  );

  function send() {
    if (!canSend || amountMinorUnits === null) return;
    setSentMessage(null);
    withdraw.mutate({ requestKey, amountMinorUnits });
  }

  function confirmCancellation() {
    if (
      !pendingCancellation ||
      getCancellablePayouts([pendingCancellation]).length !== 1
    ) {
      return;
    }
    cancelSale.mutate({ payoutRequestId: pendingCancellation.id });
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl space-y-6 px-4 py-8">
        <header className="border-foreground border-b-2 pb-6">
          <Button asChild variant="ghost" className="mb-5 -ml-3 rounded-none">
            <Link href="/portfolio">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Portfolio
            </Link>
          </Button>
          <p className="text-primary font-mono text-xs tracking-[0.22em] uppercase">
            Step two of two
          </p>
          <h1 className="mt-2 flex items-center gap-3 text-4xl font-black tracking-tight uppercase">
            <Banknote className="h-8 w-8" />
            Withdraw money
          </h1>
          <p className="text-muted-foreground mt-3 max-w-2xl text-sm">
            Send money from your balance to your own bank account. You only set
            up where it goes once, the first time you withdraw.
          </p>
        </header>

        {sentMessage && (
          <div
            role="status"
            className="border-primary bg-primary/10 flex gap-3 border-2 p-4"
          >
            <CheckCircle2 className="text-primary mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-mono text-sm font-bold uppercase">
                On its way
              </p>
              <p className="text-muted-foreground mt-1 text-sm">
                {sentMessage}
              </p>
            </div>
          </div>
        )}

        {withdraw.error && (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 flex gap-3 border-2 p-4"
          >
            <AlertCircle className="text-destructive mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="text-destructive font-mono text-sm font-bold uppercase">
                Nothing was sent
              </p>
              <p className="text-destructive mt-1 text-sm">
                {describeMutationError(withdraw.error, "send the withdrawal")}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                Your balance has not changed.
              </p>
            </div>
          </div>
        )}

        <section
          aria-labelledby="ready-to-send-heading"
          className="border-foreground bg-foreground grid gap-px border-2 md:grid-cols-2"
        >
          <h2 id="ready-to-send-heading" className="sr-only">
            What you can send
          </h2>
          <Figure
            label="Ready to send"
            value={
              availability.isLoading
                ? "—"
                : quote
                  ? formatMoney(quote.cashableMinorUnits)
                  : "Not available"
            }
            detail={
              availability.error
                ? availability.error.message
                : (quote?.statusMessage ??
                  "We could not read this just now. This is not a zero balance.")
            }
          />
          <Figure
            label="Still held"
            value={
              availability.isLoading
                ? "—"
                : quote
                  ? formatMoney(quote.onHoldMinorUnits)
                  : "Not available"
            }
            detail={
              quote?.availableFrom
                ? `Yours to send from ${formatDay(quote.availableFrom)}.`
                : "Money you added very recently is held for a short while before it can leave."
            }
            icon={<Clock className="h-5 w-5" />}
          />
        </section>

        <section
          aria-labelledby="destination-heading"
          className="border-border space-y-4 border-2 p-5"
        >
          <div>
            <p className="text-primary font-mono text-[10px] tracking-[0.2em] uppercase">
              Where your money goes
            </p>
            <h2
              id="destination-heading"
              className="mt-1 text-xl font-black uppercase"
            >
              Your bank details
            </h2>
          </div>

          {destination.isLoading ? (
            <div className="flex py-3" role="status">
              <Loader2 className="text-primary h-5 w-5 animate-spin" />
            </div>
          ) : destination.error ? (
            <p className="text-destructive text-sm" role="alert">
              {destination.error.message}
            </p>
          ) : (
            <>
              <p className="text-muted-foreground text-sm">
                {bank?.statusMessage ??
                  "We could not read your details just now."}
              </p>
              {bank && !bankReady && (
                <>
                  {bank.outstandingRequirementCount > 0 && (
                    <p className="text-muted-foreground text-xs">
                      There{" "}
                      {bank.outstandingRequirementCount === 1 ? "is" : "are"}{" "}
                      {bank.outstandingRequirementCount} thing
                      {bank.outstandingRequirementCount === 1 ? "" : "s"} left
                      to confirm before money can arrive.
                    </p>
                  )}
                  <Button
                    type="button"
                    onClick={() => beginSetup.mutate()}
                    disabled={beginSetup.isPending || !bank.available}
                    className="min-h-11 rounded-none font-mono text-xs tracking-widest uppercase"
                  >
                    {beginSetup.isPending ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Opening...
                      </>
                    ) : bank.opened ? (
                      "Finish setting this up"
                    ) : (
                      "Tell us where to send your money"
                    )}
                  </Button>
                  {beginSetup.error && (
                    <p className="text-destructive text-xs" role="alert">
                      {describeMutationError(
                        beginSetup.error,
                        "start payout setup",
                      )}
                    </p>
                  )}
                </>
              )}
            </>
          )}
        </section>

        <section
          aria-labelledby="amount-heading"
          className="border-border space-y-4 border-2 p-5"
        >
          <div>
            <p className="text-primary font-mono text-[10px] tracking-[0.2em] uppercase">
              How much
            </p>
            <h2
              id="amount-heading"
              className="mt-1 text-xl font-black uppercase"
            >
              Send money
            </h2>
          </div>

          <div className="space-y-2">
            <Label htmlFor="withdraw-amount">Amount in dollars</Label>
            <Input
              id="withdraw-amount"
              inputMode="decimal"
              value={amountText}
              onChange={(event) => setAmountText(event.target.value)}
              placeholder="0.00"
              disabled={!bankReady}
              aria-describedby="withdraw-amount-help"
              aria-invalid={amountProblem !== null}
            />
            <p
              id="withdraw-amount-help"
              className="text-muted-foreground text-xs"
            >
              {amountProblem ??
                (bankReady
                  ? "Enter an amount up to what is ready to send."
                  : "Set up where your money goes first.")}
            </p>
          </div>

          <Button
            type="button"
            onClick={send}
            disabled={!canSend}
            className="min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
          >
            {withdraw.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Sending...
              </>
            ) : (
              "Send to my bank"
            )}
          </Button>
        </section>

        <section aria-labelledby="withdrawals-heading" className="space-y-4">
          <div className="border-border flex items-center justify-between border-b pb-2">
            <h2
              id="withdrawals-heading"
              className="font-mono text-sm font-bold tracking-widest uppercase"
            >
              Money you have sent
            </h2>
            {!withdrawals.isLoading && !withdrawals.error && (
              <Badge variant="outline" size="sm">
                {(withdrawals.data ?? []).length}
              </Badge>
            )}
          </div>

          {withdrawals.isLoading ? (
            <div className="flex justify-center py-8" role="status">
              <Loader2 className="text-primary h-6 w-6 animate-spin" />
            </div>
          ) : withdrawals.error ? (
            <p className="text-destructive text-sm" role="alert">
              {withdrawals.error.message}
            </p>
          ) : (withdrawals.data ?? []).length === 0 ? (
            <div className="border-border border-2 border-dashed p-7 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                You have not sent any money to your bank yet.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {(withdrawals.data ?? []).map((record) => (
                <Card
                  key={record.id}
                  className="border-border rounded-none border-2"
                  padding="none"
                >
                  <CardContent className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <p className="font-mono text-lg font-bold">
                        {formatMoney(record.amountMinorUnits)}
                      </p>
                      <p className="text-muted-foreground mt-1 text-xs">
                        {record.statusMessage}
                      </p>
                    </div>
                    <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
                      {formatDate(record.paidAt ?? record.requestedAt)}
                    </p>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </section>

        <section aria-labelledby="sales-heading" className="space-y-4">
          <div className="border-border border-b pb-3">
            <p className="text-primary font-mono text-[10px] tracking-[0.2em] uppercase">
              Step one
            </p>
            <h2
              id="sales-heading"
              className="mt-1 text-xl font-black uppercase"
            >
              Sales still going through
            </h2>
            <p className="text-muted-foreground mt-2 text-xs">
              A sale that has not gone through yet can be called off, and your
              units come straight back. Once it has gone through it cannot.
            </p>
          </div>

          {cancelSale.error && (
            <p
              className="border-destructive bg-destructive/10 text-destructive border-2 p-3 text-sm"
              role="alert"
            >
              {describeMutationError(cancelSale.error, "cancel the sale")}
            </p>
          )}

          {sales.isLoading ? (
            <div className="flex justify-center py-8" role="status">
              <Loader2 className="text-primary h-6 w-6 animate-spin" />
            </div>
          ) : sales.error ? (
            <p className="text-destructive text-sm" role="alert">
              {sales.error.message}
            </p>
          ) : cancellableSales.length === 0 ? (
            <div className="border-border border-2 border-dashed p-7 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                Nothing is waiting.
              </p>
              <Button asChild variant="outline" size="sm" className="mt-4">
                <Link href="/swap">Sell some units</Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-3">
              {cancellableSales.map((sale) => (
                <Card
                  key={sale.id}
                  className="border-border rounded-none border-2"
                  padding="none"
                >
                  <CardContent className="space-y-4 p-4">
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
                          Started {formatDate(sale.requestedAt)}
                        </p>
                        <p className="mt-1 font-mono text-sm font-bold">
                          {formatInteger(sale.sourceTokenAmount)} units
                        </p>
                      </div>
                      <Badge variant="warning" size="sm">
                        {saleStageLabel[sale.status]}
                      </Badge>
                    </div>

                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        cancelSale.reset();
                        setPendingCancellation(sale);
                      }}
                      disabled={cancellingId !== null}
                      className="border-destructive text-destructive hover:bg-destructive/10 min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
                    >
                      {cancellingId === sale.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Calling it off...
                        </>
                      ) : (
                        <>
                          <XCircle className="mr-2 h-4 w-4" />
                          Call off this sale
                        </>
                      )}
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </section>

        <Dialog
          open={pendingCancellation !== null}
          onOpenChange={(nextOpen) => {
            if (!nextOpen && !cancelSale.isPending) {
              setPendingCancellation(null);
              cancelSale.reset();
            }
          }}
        >
          <DialogContent className="rounded-none border-2 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Call off this sale?</DialogTitle>
              <DialogDescription>
                Your units come back to you and nothing is sold. Nothing changes
                until we hear that it worked.
              </DialogDescription>
            </DialogHeader>

            {pendingCancellation && (
              <dl className="border-border grid gap-3 border-y py-4 text-sm">
                <Datum
                  label="Units"
                  value={formatInteger(pendingCancellation.sourceTokenAmount)}
                />
                <Datum
                  label="Reference"
                  value={shortenReference(pendingCancellation.id)}
                />
              </dl>
            )}

            {cancelSale.error && (
              <p
                className="border-destructive bg-destructive/10 text-destructive border-2 p-3 text-sm"
                role="alert"
              >
                {describeMutationError(cancelSale.error, "cancel the sale")}
              </p>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setPendingCancellation(null)}
                disabled={cancelSale.isPending}
              >
                Keep the sale
              </Button>
              <Button
                type="button"
                variant="destructive"
                onClick={confirmCancellation}
                disabled={cancelSale.isPending || !pendingCancellation}
              >
                {cancelSale.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Calling it off...
                  </>
                ) : (
                  "Call it off"
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}

function Figure({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: string;
  detail: string;
  icon?: ReactNode;
}) {
  return (
    <div className="bg-background p-4">
      <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
        {label}
      </p>
      <div className="mt-3 flex items-center gap-2">
        {icon}
        <p className="font-mono text-2xl font-bold">{value}</p>
      </div>
      <p className="text-muted-foreground mt-2 text-xs">{detail}</p>
    </div>
  );
}

function Datum({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
        {label}
      </dt>
      <dd className="text-foreground mt-1 font-mono text-xs">{value}</dd>
    </div>
  );
}

export default function WithdrawPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-screen items-center justify-center">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      }
    >
      <WithdrawPageInner />
    </Suspense>
  );
}
