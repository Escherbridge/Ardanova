"use client";

import { Suspense, useEffect, useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowLeft,
  Ban,
  CheckCircle2,
  Loader2,
  LockKeyhole,
  PauseCircle,
  XCircle,
} from "lucide-react";
import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";
import { buildSignInHref } from "~/lib/auth-navigation";
import {
  getCancellablePayouts,
  payoutStageLabel,
} from "~/lib/commerce/portfolio-contract";
import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "~/components/ui/dialog";

function formatInteger(value: number): string {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 }).format(
    value,
  );
}

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function shortenReference(value: string): string {
  return value.length > 22
    ? `${value.slice(0, 10)}...${value.slice(-8)}`
    : value;
}

function WithdrawPageInner() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const searchParams = useSearchParams();
  const utils = api.useUtils();
  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [cancelledId, setCancelledId] = useState<string | null>(null);
  const [pendingCancellation, setPendingCancellation] =
    useState<PayoutRequestDto | null>(null);

  const selectedConfigId = searchParams.get("configId") ?? undefined;
  const queryEnabled = Boolean(session?.user?.id);
  const {
    data: payouts,
    isLoading,
    error: payoutsError,
  } = api.payouts.getPayoutsByUser.useQuery(undefined, {
    enabled: queryEnabled,
  });

  const cancelPayout = api.payouts.cancelPayout.useMutation({
    onMutate: ({ payoutRequestId }) => {
      setCancelledId(null);
      setCancellingId(payoutRequestId);
    },
    onSuccess: async (cancelled) => {
      setCancelledId(cancelled.id);
      setPendingCancellation(null);
      await Promise.all([
        utils.payouts.getPayoutsByUser.invalidate(),
        utils.tokenBalances.getPortfolio.invalidate(),
        utils.tokenBalances.getArdaBalance.invalidate(),
      ]);
    },
    onSettled: () => setCancellingId(null),
  });

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

  const cancellablePayouts = getCancellablePayouts(
    payouts ?? [],
    selectedConfigId,
  );
  const relevantRecords = selectedConfigId
    ? (payouts ?? []).filter(
        (payout) => payout.sourceProjectTokenConfigId === selectedConfigId,
      )
    : (payouts ?? []);

  function cancel(record: PayoutRequestDto) {
    if (getCancellablePayouts([record]).length !== 1) return;
    cancelPayout.reset();
    setPendingCancellation(record);
  }

  function confirmCancellation() {
    if (
      !pendingCancellation ||
      getCancellablePayouts([pendingCancellation]).length !== 1
    ) {
      return;
    }
    cancelPayout.mutate({ payoutRequestId: pendingCancellation.id });
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
            Safety hold
          </p>
          <h1 className="mt-2 flex items-center gap-3 text-4xl font-black tracking-tight uppercase">
            <PauseCircle className="h-8 w-8" />
            Payouts paused
          </h1>
          <p className="text-muted-foreground mt-3 max-w-2xl text-sm">
            New payout requests are unavailable until ArdaNova has a verified
            provider transfer and durable settlement reconciliation. Visiting
            this page does not submit a request or move or lock any tokens.
          </p>
        </header>

        <section
          aria-labelledby="processing-contract-heading"
          className="border-foreground bg-foreground grid gap-px border-2 md:grid-cols-3"
        >
          <ContractStep
            label="01 / Submit"
            value="Paused"
            detail="No new request action is exposed."
            icon={<Ban className="h-5 w-5" />}
          />
          <ContractStep
            label="02 / Confirm transfer"
            value="Unavailable"
            detail="The processing endpoint intentionally returns HTTP 503."
            icon={<XCircle className="h-5 w-5" />}
          />
          <ContractStep
            label="03 / Reconcile"
            value="Unavailable"
            detail="The current DTO has no durable reconciliation field."
            icon={<LockKeyhole className="h-5 w-5" />}
          />
          <h2 id="processing-contract-heading" className="sr-only">
            Payout processing contract
          </h2>
        </section>

        {selectedConfigId && (
          <div className="border-border bg-muted/20 text-muted-foreground border p-3 text-xs">
            Showing records for token configuration{" "}
            <span
              className="text-foreground font-mono"
              title={selectedConfigId}
            >
              {shortenReference(selectedConfigId)}
            </span>
            .
          </div>
        )}

        {cancelledId && (
          <div
            role="status"
            className="border-primary bg-primary/10 flex gap-3 border-2 p-4"
          >
            <CheckCircle2 className="text-primary mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-mono text-sm font-bold uppercase">
                Cancellation confirmed by the API
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                The cancellation service unlocks the recorded tokens before it
                changes a pending request to cancelled. Reference{" "}
                <span className="font-mono">
                  {shortenReference(cancelledId)}
                </span>
                .
              </p>
            </div>
          </div>
        )}

        {cancelPayout.error && !pendingCancellation && (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 flex gap-3 border-2 p-4"
          >
            <AlertCircle className="text-destructive mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="text-destructive font-mono text-sm font-bold uppercase">
                Cancellation not confirmed
              </p>
              <p className="text-destructive mt-1 text-xs">
                {cancelPayout.error.message}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                No token-unlock claim is made unless the API returns success.
              </p>
            </div>
          </div>
        )}

        <section
          aria-labelledby="pending-cancellations-heading"
          className="space-y-4"
        >
          <div className="border-border border-b pb-3">
            <p className="text-primary font-mono text-[10px] tracking-[0.2em] uppercase">
              Recovery action
            </p>
            <h2
              id="pending-cancellations-heading"
              className="mt-1 text-xl font-black uppercase"
            >
              Existing pending requests
            </h2>
            <p className="text-muted-foreground mt-2 text-xs">
              Only an existing token-conversion record with status PENDING, a
              source configuration, and a positive source-token amount can be
              cancelled safely. Cancellation is the only mutation available on
              this screen.
            </p>
          </div>

          {isLoading ? (
            <div className="flex justify-center py-10">
              <Loader2 className="text-primary h-6 w-6 animate-spin" />
            </div>
          ) : payoutsError ? (
            <div
              role="alert"
              className="border-destructive bg-destructive/10 border-2 p-5"
            >
              <p className="text-destructive text-sm">
                Existing payout records could not be loaded. No cancellation
                controls are shown.
              </p>
            </div>
          ) : cancellablePayouts.length === 0 ? (
            <div className="border-border border-2 border-dashed p-7 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                {selectedConfigId
                  ? "No cancellable pending request was returned for this token configuration."
                  : "No cancellable pending payout request was returned."}
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {cancellablePayouts.map((payout) => (
                <Card
                  key={payout.id}
                  className="border-border rounded-none border-2"
                  padding="none"
                >
                  <CardContent className="space-y-4 p-4">
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
                          Submitted {formatDate(payout.requestedAt)}
                        </p>
                        <p className="mt-1 font-mono text-sm font-bold">
                          {shortenReference(payout.id)}
                        </p>
                      </div>
                      <Badge variant="warning" size="sm">
                        {payoutStageLabel[payout.status]}
                      </Badge>
                    </div>

                    <dl className="border-border grid grid-cols-2 gap-3 border-y py-3 sm:grid-cols-4">
                      <RecordDatum
                        label="Tokens"
                        value={formatInteger(payout.sourceTokenAmount)}
                      />
                      <RecordDatum
                        label="Recorded USD"
                        value={
                          payout.usdAmount === null
                            ? "Not recorded"
                            : formatUsd(payout.usdAmount)
                        }
                      />
                      <RecordDatum
                        label="Holder class"
                        value={payout.holderClass}
                      />
                      <RecordDatum
                        label="Gate at request"
                        value={payout.gateStatusAtRequest}
                      />
                    </dl>

                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => cancel(payout)}
                      disabled={cancellingId !== null}
                      className="border-destructive text-destructive hover:bg-destructive/10 min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
                    >
                      {cancellingId === payout.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Confirming cancellation...
                        </>
                      ) : (
                        <>
                          <XCircle className="mr-2 h-4 w-4" />
                          Cancel pending request
                        </>
                      )}
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </section>

        {!isLoading && !payoutsError && relevantRecords.length > 0 && (
          <section className="border-border text-muted-foreground border-t pt-4 text-xs">
            <p>
              {relevantRecords.length} matching API record
              {relevantRecords.length === 1 ? " is" : "s are"} visible in the
              portfolio history. Status labels preserve the difference between
              submitted, confirmed, and reconciled; the latter two are not
              inferred when the contract does not provide them.
            </p>
          </section>
        )}

        <Dialog
          open={pendingCancellation !== null}
          onOpenChange={(nextOpen) => {
            if (!nextOpen && !cancelPayout.isPending) {
              setPendingCancellation(null);
              cancelPayout.reset();
            }
          }}
        >
          <DialogContent className="rounded-none border-2 sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Cancel this pending payout request?</DialogTitle>
              <DialogDescription>
                This asks the backend to unlock the recorded source tokens and
                change the pending request to cancelled. No unlock or status
                change is assumed until the API confirms success.
              </DialogDescription>
            </DialogHeader>

            {pendingCancellation && (
              <dl className="border-border grid gap-3 border-y py-4 text-sm">
                <RecordDatum
                  label="Source tokens"
                  value={formatInteger(pendingCancellation.sourceTokenAmount)}
                />
                <RecordDatum
                  label="Token configuration"
                  value={shortenReference(
                    pendingCancellation.sourceProjectTokenConfigId ??
                      "Not recorded",
                  )}
                />
              </dl>
            )}

            {cancelPayout.error && (
              <p
                className="border-destructive bg-destructive/10 text-destructive border-2 p-3 text-sm"
                role="alert"
              >
                Cancellation was not confirmed: {cancelPayout.error.message}
              </p>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setPendingCancellation(null)}
                disabled={cancelPayout.isPending}
              >
                Keep request
              </Button>
              <Button
                type="button"
                variant="destructive"
                onClick={confirmCancellation}
                disabled={cancelPayout.isPending || !pendingCancellation}
              >
                {cancelPayout.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Confirming...
                  </>
                ) : (
                  "Confirm cancellation"
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}

function ContractStep({
  label,
  value,
  detail,
  icon,
}: {
  label: string;
  value: string;
  detail: string;
  icon: ReactNode;
}) {
  return (
    <div className="bg-background p-4">
      <p className="text-muted-foreground font-mono text-[10px] tracking-widest uppercase">
        {label}
      </p>
      <div className="mt-3 flex items-center gap-2">
        {icon}
        <p className="font-mono text-sm font-bold uppercase">{value}</p>
      </div>
      <p className="text-muted-foreground mt-2 text-xs">{detail}</p>
    </div>
  );
}

function RecordDatum({ label, value }: { label: string; value: string }) {
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
