"use client";

import { Suspense, useEffect, useMemo, useRef, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import Link from "next/link";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { AlertCircle, ArrowLeft, CheckCircle2, Clock3, Lock, Wallet } from "lucide-react";
import {
  describeFundingError,
  getFundingIntentPresentation,
} from "~/lib/commerce/funding-intent-status";

/** How long this page keeps asking the server before it stops polling and just says "we'll email you". */
const POLL_TIMEOUT_MS = 3 * 60 * 1000;
const POLL_INTERVAL_MS = 5000;

/** What the invest page persisted at checkout time — an estimate, never the confirmed figure. */
interface QuotedIntent {
  intentId: string;
  ardaAmount: number;
  quantity: number;
  unitName: string;
  supplySharePercent: number | null;
}

function readQuotedIntent(slug: string): QuotedIntent | null {
  try {
    const raw = window.sessionStorage.getItem(`ardanova:funding-intent:${slug}`);
    if (!raw) return null;
    const parsed: unknown = JSON.parse(raw);
    if (
      parsed &&
      typeof parsed === "object" &&
      "intentId" in parsed &&
      typeof (parsed as { intentId: unknown }).intentId === "string"
    ) {
      return parsed as QuotedIntent;
    }
    return null;
  } catch {
    return null;
  }
}

/**
 * The server's own date and sentence, read off the status payload.
 *
 * `FundingIntentStatusDto` returns both (`ReleaseNotBefore`, `HoldingReasonDetail`, camel-cased
 * by the API's serializer) and the `FundingIntentStatus` interface in
 * `~/lib/api/ardanova/endpoints/funding-intents.ts` now declares them, so this helper is a
 * narrowing step rather than a workaround: it turns an empty string into null so the copy
 * below never renders a blank date or a blank reason as if the server had said something.
 */
function readServerFacts(intent: unknown): {
  releaseNotBefore: string | null;
  holdingReasonDetail: string | null;
} {
  if (!intent || typeof intent !== "object") {
    return { releaseNotBefore: null, holdingReasonDetail: null };
  }
  const record = intent as Record<string, unknown>;
  const release = record.releaseNotBefore;
  const reason = record.holdingReasonDetail;
  return {
    releaseNotBefore: typeof release === "string" && release ? release : null,
    holdingReasonDetail: typeof reason === "string" && reason ? reason : null,
  };
}

/** The release date as a person reads it — "14 March 2026", never a timestamp. */
function formatReleaseDate(iso: string): string | null {
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) return null;
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(parsed);
}

function formatPct(value: number) {
  return `${value.toFixed(4)}%`;
}

function formatArda(value: number) {
  return `${new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value)} ARDA`;
}

export default function InvestSuccessPage() {
  return (
    <Suspense
      fallback={
        <div className="bg-background flex min-h-screen items-center justify-center p-4">
          <Clock3 className="text-muted-foreground size-8 animate-pulse" />
        </div>
      }
    >
      <InvestSuccessFlow />
    </Suspense>
  );
}

function InvestSuccessFlow() {
  const params = useParams();
  const searchParams = useSearchParams();
  const slug = params.slug as string;

  const [quotedIntent, setQuotedIntent] = useState<QuotedIntent | null>(null);
  const [timedOut, setTimedOut] = useState(false);
  const pollStartedAtRef = useRef(Date.now());

  useEffect(() => {
    setQuotedIntent(readQuotedIntent(slug));
  }, [slug]);

  const intentId = searchParams.get("intent") ?? quotedIntent?.intentId ?? "";

  useEffect(() => {
    pollStartedAtRef.current = Date.now();
    setTimedOut(false);
  }, [intentId]);

  const { data: intent, error } = api.fundingIntent.getStatus.useQuery(
    { intentId },
    {
      enabled: Boolean(intentId),
      refetchInterval: (query) => {
        const presentation = getFundingIntentPresentation({
          status: query.state.data?.status,
        });
        if (!presentation.poll) return false;
        if (Date.now() - pollStartedAtRef.current >= POLL_TIMEOUT_MS) {
          setTimedOut(true);
          return false;
        }
        return POLL_INTERVAL_MS;
      },
    },
  );

  const serverFacts = useMemo(() => readServerFacts(intent), [intent]);

  const presentation = useMemo(() => {
    if (error) return getFundingIntentPresentation();
    // The server's own release time and its own refusal sentence. The fixed 30-day window in
    // `funding-intent-status.ts` is now only reachable when the server sends no date at all.
    return getFundingIntentPresentation({
      status: intent?.status,
      reasonDetail: serverFacts.holdingReasonDetail,
      releaseNotBefore: serverFacts.releaseNotBefore,
    });
  }, [error, intent?.status, serverFacts]);

  const releaseDate =
    presentation.outcome === "confirmed" && serverFacts.releaseNotBefore
      ? formatReleaseDate(serverFacts.releaseNotBefore)
      : null;

  return (
    <div className="bg-background flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <StatusHero
          presentation={presentation}
          errorMessage={error ? describeFundingError(error) : null}
        />

        {releaseDate && (
          <p className="text-muted-foreground text-center text-xs">
            Your stake is yours to move from{" "}
            <strong className="text-foreground">{releaseDate}</strong>. That is
            the date recorded against this sponsorship, not an estimate.
          </p>
        )}

        {timedOut && presentation.outcome === "waiting" && (
          <p className="text-muted-foreground text-center text-xs">
            This is taking longer than usual. We&apos;ll still email you the
            result — check back on your portfolio later.
          </p>
        )}

        {quotedIntent &&
          quotedIntent.intentId === intentId &&
          quotedIntent.quantity > 0 && (
          <Card className="border-border bg-muted/20 border-2">
            <CardContent className="space-y-2 p-5">
              <p className="text-muted-foreground font-mono text-[10px] font-bold tracking-widest uppercase">
                As quoted when you confirmed
              </p>
              <p className="text-sm">
                {formatArda(quotedIntent.ardaAmount)} for{" "}
                <strong>
                  {quotedIntent.quantity.toLocaleString()} {quotedIntent.unitName}
                </strong>
              </p>
              {quotedIntent.supplySharePercent !== null && (
                <details className="text-muted-foreground text-xs">
                  <summary className="text-foreground cursor-pointer font-semibold">
                    Show my share of the project
                  </summary>
                  <p className="mt-1 leading-relaxed">
                    {formatPct(quotedIntent.supplySharePercent)} of this
                    project&apos;s configured supply, as quoted — not equity,
                    governance power, or a redemption right.
                  </p>
                </details>
              )}
              <p className="text-muted-foreground text-xs">
                This is what you saw at checkout, not a confirmed figure. Your
                portfolio reflects what the node actually recorded.
              </p>
            </CardContent>
          </Card>
        )}

        <Card className="border-system bg-system/5 border-2">
          <CardContent className="space-y-3 p-5">
            <span className="text-muted-foreground block font-mono text-xs font-bold tracking-widest">
              WHAT HAPPENS NEXT
            </span>
            <ol className="text-muted-foreground list-none space-y-2 text-sm">
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  01
                </span>
                The server asks the node to move your ARDA and records the
                result on your sponsorship.
              </li>
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  02
                </span>
                {releaseDate
                  ? `A freshly issued stake is held until ${releaseDate}, then it's yours to move — never confirmed from this redirect or a URL parameter.`
                  : "A freshly issued stake sits in a short hold before it's yours to move — never confirmed from this redirect or a URL parameter."}
              </li>
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  03
                </span>
                Your portfolio is the source for what you actually hold,
                pending or available.
              </li>
            </ol>

            <div className="text-muted-foreground border-border/40 mt-1 flex items-start gap-2 border p-2 text-xs">
              <Lock className="text-muted-foreground mt-0.5 size-3 shrink-0" />
              <span>
                A confirmed sponsorship, its hold window, and your recorded
                balance determine any portfolio availability.
              </span>
            </div>
          </CardContent>
        </Card>

        <div className="flex flex-col gap-3">
          <Button
            asChild
            variant="default"
            className="h-12 w-full font-mono font-bold"
          >
            <Link href="/portfolio">
              <Wallet className="mr-2 size-4" />
              VIEW MY PORTFOLIO
            </Link>
          </Button>

          <Button asChild variant="outline" className="h-12 w-full font-mono">
            <Link href={`/projects/${slug}`}>
              <ArrowLeft className="mr-2 size-4" />
              BACK TO PROJECT
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}

function StatusHero({
  presentation,
  errorMessage,
}: {
  presentation: ReturnType<typeof getFundingIntentPresentation>;
  errorMessage: string | null;
}) {
  const icon =
    presentation.outcome === "confirmed" ? (
      <CheckCircle2 className="text-primary size-10" />
    ) : presentation.outcome === "went-wrong" ? (
      <AlertCircle className="text-destructive size-10" />
    ) : (
      <Clock3 className="text-system size-10" />
    );

  const iconWrapClass =
    presentation.outcome === "went-wrong"
      ? "border-destructive bg-destructive/10"
      : "border-system bg-system/10";

  return (
    <div className="space-y-3 text-center">
      <div
        className={`mx-auto flex size-20 items-center justify-center border-2 ${iconWrapClass}`}
      >
        {icon}
      </div>
      <h1 className="text-foreground font-mono text-2xl font-bold tracking-wide">
        {presentation.heading}
      </h1>
      <p className="text-muted-foreground text-sm">
        {errorMessage ?? presentation.detail}
      </p>
    </div>
  );
}
