"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowUpRight,
  CheckCircle2,
  Clock,
  Coins,
  Loader2,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { buildSignInHref } from "~/lib/auth-navigation";
import { api } from "~/trpc/react";
import type { SellableUnits } from "~/server/api/routers/payouts";

function formatUnits(value: number): string {
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
  return new Intl.DateTimeFormat("en-US", { dateStyle: "long" }).format(
    new Date(value),
  );
}

/** One holding, chosen by the project it belongs to and the way it was earned. */
function holdingKey(holding: SellableUnits): string {
  return `${holding.projectTokenConfigId}:${holding.holderClass ?? "unclassified"}`;
}

export default function SellUnitsPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const utils = api.useUtils();

  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [amountText, setAmountText] = useState("");
  const [soldMessage, setSoldMessage] = useState<string | null>(null);

  const signedIn = Boolean(session?.user?.id);

  const holdings = api.payouts.getSellableUnits.useQuery(undefined, {
    enabled: signedIn,
    retry: false,
  });

  const selected = useMemo(
    () =>
      (holdings.data ?? []).find(
        (holding) => holdingKey(holding) === selectedKey,
      ) ?? null,
    [holdings.data, selectedKey],
  );

  // A whole number of units, or nothing. A partial unit is never a unit, and a
  // quote for one would be a promise the sale cannot keep.
  const requestedUnits = useMemo(() => {
    const parsedAmount = Number.parseInt(amountText, 10);
    return Number.isSafeInteger(parsedAmount) && parsedAmount > 0
      ? parsedAmount
      : null;
  }, [amountText]);

  const withinHolding =
    selected !== null &&
    requestedUnits !== null &&
    requestedUnits <= selected.sellableUnitCount;

  const quote = api.payouts.quoteSale.useQuery(
    {
      projectTokenConfigId: selected?.projectTokenConfigId ?? "",
      holderClass: selected?.holderClass ?? "CONTRIBUTOR",
      units: requestedUnits ?? 1,
    },
    { enabled: signedIn && withinHolding, retry: false },
  );

  const sell = api.payouts.sellUnits.useMutation({
    onSuccess: async () => {
      setSoldMessage(
        selected
          ? `Sold ${formatUnits(requestedUnits ?? 0)} ${selected.unitName}. The funds are in your balance.`
          : "Your units were sold. The funds are in your balance.",
      );
      setAmountText("");
      await Promise.all([
        utils.payouts.getSellableUnits.invalidate(),
        utils.payouts.getPayoutsByUser.invalidate(),
        utils.tokenBalances.getPortfolio.invalidate(),
        utils.tokenBalances.getArdaBalance.invalidate(),
      ]);
    },
  });

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.push(buildSignInHref("/swap"));
    }
  }, [router, sessionStatus]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary size-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  const available = holdings.data ?? [];
  const sellable = available.filter((holding) => holding.canSell);
  const waiting = available.filter((holding) => !holding.canSell);

  function choose(holding: SellableUnits) {
    setSelectedKey(holdingKey(holding));
    setAmountText("");
    setSoldMessage(null);
    sell.reset();
  }

  function confirmSale() {
    if (!selected || requestedUnits === null || !withinHolding) return;
    // A holding with no recorded holder class is never sellable; the server refuses it too.
    if (selected.holderClass === null) return;
    setSoldMessage(null);
    sell.mutate({
      sourceProjectTokenConfigId: selected.projectTokenConfigId,
      sourceTokenAmount: requestedUnits,
      holderClass: selected.holderClass,
    });
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl space-y-8 px-4 py-8">
        <header className="border-foreground border-b-2 pb-6">
          <div className="flex items-center gap-3">
            <span className="border-foreground bg-system/10 text-system flex size-10 items-center justify-center border-2">
              <Coins className="size-5" aria-hidden="true" />
            </span>
            <div>
              <p className="text-primary font-mono text-xs font-bold tracking-[0.16em] uppercase">
                Step one of two
              </p>
              <h1 className="font-mono text-3xl font-black tracking-tight uppercase">
                Sell your units
              </h1>
            </div>
          </div>
          <p className="text-muted-foreground mt-4 max-w-2xl text-sm leading-relaxed">
            Sell the units you earned or backed a project with, and the funds go
            straight into your balance. When you want that money in your bank,
            withdraw it from your portfolio.
          </p>
        </header>

        {soldMessage && (
          <div
            role="status"
            className="border-primary bg-primary/10 flex gap-3 border-2 p-4"
          >
            <CheckCircle2 className="text-primary mt-0.5 size-5 shrink-0" />
            <div>
              <p className="font-mono text-sm font-bold uppercase">Sold</p>
              <p className="text-muted-foreground mt-1 text-sm">{soldMessage}</p>
              <Button asChild variant="outline" size="sm" className="mt-3">
                <Link href="/portfolio/withdraw">
                  Send it to your bank
                  <ArrowUpRight className="ml-2 size-4" />
                </Link>
              </Button>
            </div>
          </div>
        )}

        {sell.error && (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 flex gap-3 border-2 p-4"
          >
            <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
            <div>
              <p className="text-destructive font-mono text-sm font-bold uppercase">
                Nothing was sold
              </p>
              <p className="text-destructive mt-1 text-sm">
                {describeMutationError(sell.error, "complete the sale")}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                Your units are exactly where they were.
              </p>
            </div>
          </div>
        )}

        <section className="space-y-4" aria-labelledby="your-units-title">
          <div className="border-border flex items-center justify-between border-b pb-2">
            <h2
              id="your-units-title"
              className="font-mono text-sm font-bold tracking-widest uppercase"
            >
              What you hold
            </h2>
            {!holdings.isLoading && !holdings.error && (
              <Badge variant="outline" size="sm">
                {available.length} project{available.length === 1 ? "" : "s"}
              </Badge>
            )}
          </div>

          {holdings.isLoading ? (
            <div className="flex items-center justify-center py-8" role="status">
              <Loader2 className="text-primary size-6 animate-spin" />
            </div>
          ) : holdings.error ? (
            <Card className="border-destructive">
              <CardContent className="space-y-3 p-4">
                <p className="text-destructive text-sm" role="alert">
                  {holdings.error.message}
                </p>
                <p className="text-muted-foreground text-xs">
                  This does not mean you hold nothing.
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void holdings.refetch()}
                >
                  Try again
                </Button>
              </CardContent>
            </Card>
          ) : available.length === 0 ? (
            <div className="border-border border-2 border-dashed p-7 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                You do not hold units in any project yet.
              </p>
              <Button asChild variant="outline" size="sm" className="mt-4">
                <Link href="/projects">Find a project</Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-3">
              {sellable.map((holding) => {
                const chosen = holdingKey(holding) === selectedKey;
                return (
                  <Card
                    key={holdingKey(holding)}
                    className={
                      chosen
                        ? "border-primary rounded-none border-2"
                        : "border-border rounded-none border-2"
                    }
                    padding="none"
                  >
                    <CardContent className="space-y-4 p-4">
                      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <p className="font-mono text-sm font-bold uppercase">
                            {holding.unitName}
                          </p>
                          <p className="text-muted-foreground mt-1 text-xs">
                            {holding.statusMessage}
                          </p>
                        </div>
                        <Badge variant="outline" size="sm">
                          {formatUnits(holding.sellableUnitCount)} ready
                        </Badge>
                      </div>

                      {holding.restrictedUnits > 0 && (
                        <p className="text-muted-foreground flex items-start gap-2 text-xs">
                          <Clock className="mt-0.5 size-3.5 shrink-0" />
                          <span>
                            {formatUnits(holding.restrictedUnits)} of your{" "}
                            {holding.unitName} are yours already and are held
                            {holding.restrictedUntil
                              ? ` until ${formatDate(holding.restrictedUntil)}`
                              : " until their date is confirmed"}
                            . You can sell them after that.
                          </span>
                        </p>
                      )}

                      {chosen ? (
                        <div className="space-y-3">
                          <div className="space-y-2">
                            <Label htmlFor="units-to-sell">
                              How many do you want to sell?
                            </Label>
                            <Input
                              id="units-to-sell"
                              inputMode="numeric"
                              value={amountText}
                              onChange={(event) =>
                                setAmountText(event.target.value)
                              }
                              placeholder={formatUnits(
                                holding.sellableUnitCount,
                              )}
                              aria-describedby="units-to-sell-help"
                            />
                            <p
                              id="units-to-sell-help"
                              className="text-muted-foreground text-xs"
                            >
                              You can sell up to{" "}
                              {formatUnits(holding.sellableUnitCount)} right now.
                            </p>
                          </div>

                          <div
                            className="border-border bg-muted/20 border p-3"
                            aria-live="polite"
                          >
                            {requestedUnits === null ? (
                              <p className="text-muted-foreground text-xs">
                                Enter a whole number of units to see what you
                                would get.
                              </p>
                            ) : !withinHolding ? (
                              <p className="text-muted-foreground text-xs">
                                That is more than you can sell right now.
                              </p>
                            ) : quote.isLoading ? (
                              <p className="text-muted-foreground flex items-center gap-2 text-xs">
                                <Loader2 className="size-3.5 animate-spin" />
                                Working out what you would get...
                              </p>
                            ) : quote.error ? (
                              <p className="text-destructive text-xs">
                                {quote.error.message}
                              </p>
                            ) : quote.data && !quote.data.canSell ? (
                              <p className="text-muted-foreground text-xs">
                                {quote.data.statusMessage}
                              </p>
                            ) : quote.data ? (
                              <>
                                <p className="text-foreground font-mono text-2xl font-bold">
                                  {formatMoney(
                                    quote.data.usdReferenceMinorUnits,
                                  )}
                                </p>
                                <p className="text-muted-foreground mt-1 text-xs">
                                  goes into your balance for{" "}
                                  {formatUnits(quote.data.sourceUnitAmount)}{" "}
                                  {holding.unitName}. The amount can move a
                                  little between now and the sale.
                                </p>
                              </>
                            ) : null}
                          </div>

                          <Button
                            type="button"
                            onClick={confirmSale}
                            disabled={
                              sell.isPending ||
                              !withinHolding ||
                              quote.isLoading ||
                              !(quote.data?.canSell ?? false)
                            }
                            className="min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
                          >
                            {sell.isPending ? (
                              <>
                                <Loader2 className="mr-2 size-4 animate-spin" />
                                Selling...
                              </>
                            ) : (
                              "Sell these units"
                            )}
                          </Button>
                        </div>
                      ) : (
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() => choose(holding)}
                          className="min-h-11 w-full rounded-none font-mono text-xs tracking-widest uppercase"
                        >
                          Sell some of these
                        </Button>
                      )}
                    </CardContent>
                  </Card>
                );
              })}

              {waiting.map((holding) => (
                <Card
                  key={holdingKey(holding)}
                  className="border-border rounded-none border-2 border-dashed"
                  padding="none"
                >
                  <CardContent className="space-y-2 p-4">
                    <div className="flex items-start justify-between gap-3">
                      <p className="font-mono text-sm font-bold uppercase">
                        {holding.unitName}
                      </p>
                      <Badge variant="outline" size="sm">
                        {formatUnits(holding.heldUnits)} held
                      </Badge>
                    </div>
                    <p className="text-muted-foreground text-xs">
                      {holding.statusMessage}
                    </p>
                    {holding.restrictedUntil && (
                      <p className="text-muted-foreground flex items-center gap-2 text-xs">
                        <Clock className="size-3.5" />
                        Yours from {formatDate(holding.restrictedUntil)}.
                      </p>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </section>

        <section className="border-border space-y-3 border-t pt-5">
          <h2 className="font-mono text-sm font-bold tracking-widest uppercase">
            Step two
          </h2>
          <p className="text-muted-foreground text-sm">
            Money you have in your balance can be sent to your bank whenever you
            like. You only need to set up where it goes once.
          </p>
          <Button asChild variant="outline" className="rounded-none">
            <Link href="/portfolio/withdraw">
              Withdraw money
              <ArrowUpRight className="ml-2 size-4" />
            </Link>
          </Button>
        </section>
      </div>
    </div>
  );
}
