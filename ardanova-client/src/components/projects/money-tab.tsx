"use client";

import { useMemo, useState } from "react";
import {
  AlertCircle,
  ArrowDownLeft,
  ArrowUpRight,
  Clock,
  Landmark,
  Loader2,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { api, type RouterOutputs } from "~/trpc/react";

/** A project's money as the server returns it; mirrors ProjectMoneyView on the .NET side. */
type ProjectMoneyView = RouterOutputs["projectTokens"]["getMoney"];

/** One movement, as the server described it. */
type ProjectMoneyEntry = ProjectMoneyView["entries"][number];

/** Which entries are on screen. Two denominations are never mixed into one running total. */
const ENTRY_FILTERS = [
  { key: "all", label: "Everything" },
  { key: "Funds", label: "Funds" },
  { key: "Stake", label: "Stake" },
] as const;

type EntryFilter = (typeof ENTRY_FILTERS)[number]["key"];

interface MoneyTabProps {
  projectId: string;
}

/**
 * A project's money in one place: what it holds, and every movement that explains it.
 *
 * The balance and the entries are two different kinds of fact. The balance is the project's own
 * funds account as the server read it; the entries are only the story of how it got there, and
 * this component never adds them up — a total assembled here would be a second, disagreeing
 * answer to a question the funds account already answers.
 */
export default function MoneyTab({ projectId }: MoneyTabProps) {
  const moneyQuery = api.projectTokens.getMoney.useQuery({ projectId });

  if (moneyQuery.isLoading) {
    return (
      <div className="flex items-center justify-center py-20" role="status">
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading this project&rsquo;s money</span>
      </div>
    );
  }

  if (moneyQuery.error || !moneyQuery.data) {
    return (
      <div
        className="border-destructive bg-destructive/5 flex items-start gap-3 border p-6"
        role="status"
      >
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div>
          <p className="text-destructive font-mono text-sm font-bold uppercase">
            Money is unavailable right now
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {moneyQuery.error?.message ??
              "Nothing about this project's money is inferred while the service is unreachable."}
          </p>
        </div>
      </div>
    );
  }

  return <MoneyTabContent money={moneyQuery.data} />;
}

function MoneyTabContent({ money }: { money: ProjectMoneyView }) {
  const [filter, setFilter] = useState<EntryFilter>("all");

  const entries = useMemo(
    () =>
      filter === "all"
        ? money.entries
        : money.entries.filter((entry) => entry.denomination === filter),
    [filter, money.entries],
  );

  return (
    <div className="space-y-6">
      <BalanceCard money={money} />

      <SupportSummary money={money} />

      <div className="flex flex-wrap items-center gap-2">
        {ENTRY_FILTERS.map((option) => (
          <Button
            key={option.key}
            type="button"
            size="sm"
            variant={filter === option.key ? "default" : "outline"}
            onClick={() => setFilter(option.key)}
            aria-pressed={filter === option.key}
          >
            {option.label}
          </Button>
        ))}
      </div>

      {!money.entriesVisible ? (
        <EntriesWithheld note={money.entriesHiddenNote} />
      ) : entries.length === 0 ? (
        <EmptyEntries hasAnyEntries={money.entries.length > 0} />
      ) : (
        <ul className="space-y-3">
          {entries.map((entry) => (
            <li key={entry.id}>
              <MoneyEntryRow entry={entry} />
            </li>
          ))}
        </ul>
      )}

      {money.entriesVisible && money.counterpartyNamesNote ? (
        <p className="text-muted-foreground text-xs">
          {money.counterpartyNamesNote}
        </p>
      ) : null}
    </div>
  );
}

/**
 * The balance of record. When it could not be read the reason is shown instead of a figure:
 * a zero standing in for an unavailable read is how a missing answer becomes a wrong one.
 */
function BalanceCard({ money }: { money: ProjectMoneyView }) {
  const { balance } = money;

  return (
    <Card className="border-foreground">
      <CardHeader className="pb-2">
        <CardTitle className="flex items-center gap-2 font-mono text-base tracking-wide uppercase">
          <Landmark className="size-4" />
          What this project holds
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {balance.isAvailable && balance.fundsText ? (
          <>
            <p className="font-mono text-3xl leading-none font-bold">
              {balance.fundsText}{" "}
              <span className="text-muted-foreground text-lg">
                {balance.fundsUnitLabel}
              </span>
            </p>
            {balance.observedAt ? (
              <p className="text-muted-foreground text-xs">
                Read from this project&rsquo;s funds account on{" "}
                {formatDateTime(balance.observedAt)}.
              </p>
            ) : null}
          </>
        ) : (
          <div className="border-border flex items-start gap-3 border p-4">
            <AlertCircle className="text-muted-foreground mt-0.5 size-4 shrink-0" />
            <p className="text-muted-foreground text-xs">
              {balance.unavailableReason ??
                "This project's funds account could not be read, so no balance is shown."}
            </p>
          </div>
        )}

        <p className="text-muted-foreground text-xs leading-relaxed">
          {money.balanceProvenance}
        </p>
      </CardContent>
    </Card>
  );
}

/** One movement, with its own unit printed beside it so two denominations never blur. */
function MoneyEntryRow({ entry }: { entry: ProjectMoneyEntry }) {
  const [showReference, setShowReference] = useState(false);
  const incoming = entry.direction === "In";
  const DirectionIcon = incoming ? ArrowDownLeft : ArrowUpRight;

  return (
    <div className="border-border flex flex-col gap-3 border p-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex items-start gap-3">
        <span
          className="border-border mt-0.5 flex size-8 shrink-0 items-center justify-center border"
          aria-hidden="true"
        >
          <DirectionIcon className="text-muted-foreground size-4" />
        </span>
        <div className="space-y-1">
          <p className="font-mono text-xs font-bold tracking-wide uppercase">
            {entry.headline}
          </p>
          <p className="text-sm leading-relaxed">{entry.description}</p>
          <p className="text-muted-foreground text-xs">
            {formatDateTime(entry.occurredAt)}
            {entry.exchangedForText ? ` · for ${entry.exchangedForText}` : ""}
          </p>

          {entry.pendingReason ? (
            <p className="text-muted-foreground flex items-center gap-1.5 text-xs">
              <Clock className="size-3.5 shrink-0" aria-hidden="true" />
              {entry.pendingReason}
            </p>
          ) : null}

          {entry.verifyReference ? (
            <div className="pt-1">
              <button
                type="button"
                className="text-muted-foreground hover:text-foreground text-xs underline underline-offset-2"
                onClick={() => setShowReference((shown) => !shown)}
                aria-expanded={showReference}
              >
                {showReference ? "Hide reference" : "Verify"}
              </button>
              {showReference ? (
                <p className="text-muted-foreground mt-1 font-mono text-xs break-all">
                  {entry.verifyReference}
                </p>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>

      <div className="shrink-0 text-left sm:text-right">
        <p className="font-mono text-sm font-bold">
          {incoming ? "+" : "−"}
          {entry.amountText}{" "}
          <span className="text-muted-foreground font-normal">
            {entry.unitLabel}
          </span>
        </p>
        {entry.usdReferenceText && entry.unitLabel !== "USD" ? (
          <p className="text-muted-foreground text-xs">
            about ${entry.usdReferenceText} at the time
          </p>
        ) : null}
        {entry.isSettled ? null : (
          <Badge variant="outline" className="mt-1">
            Not confirmed
          </Badge>
        )}
      </div>
    </div>
  );
}

/**
 * Support this project has, shown to everyone including a signed-in stranger. It is the whole
 * answer a non-member gets, and without it an empty entry list reads as "nothing has moved
 * yet" on a project that HAS been sponsored — a false statement to the very person being
 * asked for money.
 */
function SupportSummary({ money }: { money: ProjectMoneyView }) {
  const { summary } = money;
  const hasSupport = summary.sponsorCount > 0 || summary.contributorCount > 0;

  return (
    <Card className="border-foreground">
      <CardHeader className="pb-2">
        <CardTitle className="font-mono text-base tracking-wide uppercase">
          Support
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {hasSupport ? (
          <>
            <p className="font-mono text-2xl font-bold">
              {summary.sponsorshipTotalUsdText}
            </p>
            <p className="text-muted-foreground text-xs">
              {formatCount(summary.sponsorCount, "sponsor", "sponsors")}
              {" · "}
              {formatCount(
                summary.contributorCount,
                "person paid in stake",
                "people paid in stake",
              )}
            </p>
          </>
        ) : (
          <p className="text-muted-foreground text-xs">
            No sponsorship has settled yet.
          </p>
        )}
      </CardContent>
    </Card>
  );
}

/** Plural-safe count. "1 sponsor", never "1 sponsors". */
function formatCount(count: number, one: string, many: string): string {
  return `${count} ${count === 1 ? one : many}`;
}

/**
 * The reader is not on this project, so the server built no entries for them at all. This is
 * NOT the same statement as "nothing has moved", and saying the latter here would contradict
 * the support figure directly above it.
 */
function EntriesWithheld({ note }: { note?: string | null }) {
  return (
    <div className="border-border flex flex-col items-center justify-center gap-2 border p-8 text-center">
      <p className="text-muted-foreground font-mono text-sm font-bold uppercase">
        Individual movements are private
      </p>
      <p className="text-muted-foreground text-xs">
        {note ??
          "Individual movements are shown to project members only. Everyone else sees how much support this project has, not who gave what."}
      </p>
    </div>
  );
}

function EmptyEntries({ hasAnyEntries }: { hasAnyEntries: boolean }) {
  return (
    <div className="border-border flex flex-col items-center justify-center gap-2 border p-8 text-center">
      <p className="text-muted-foreground font-mono text-sm font-bold uppercase">
        {hasAnyEntries ? "Nothing of this kind yet" : "Nothing has moved yet"}
      </p>
      <p className="text-muted-foreground text-xs">
        {hasAnyEntries
          ? "Try another view above."
          : "When someone sponsors this project or a worker is paid in stake, it appears here."}
      </p>
    </div>
  );
}

/** A moment a person reads. Falls back to the raw value rather than inventing a date. */
function formatDateTime(value: string): string {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(parsed);
}
