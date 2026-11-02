"use client";
import { useCallback, useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AddFundsForm,
  BalanceSummary,
  KycStep,
  MoneyGate,
  SellUnitsForm,
  WithdrawForm,
  useCustodialAccount,
} from "azoa-sdk/react";
import { api } from "~/trpc/react";
import { normalizeInternalReturnTo } from "~/lib/auth-navigation";
import { ArdaMoneyProvider } from "./provider";
import { AccountAccessStep } from "./access-step";
import { ArdaConsentStatus } from "./consent-status";
import type { MoneyHolding } from "./contracts";

function PageFrame({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <main className="mx-auto max-w-3xl space-y-6 p-4 md:p-8">
      <Link
        className="inline-flex min-h-11 items-center text-sm underline"
        href="/portfolio"
      >
        Portfolio
      </Link>
      <h1 className="font-mono text-2xl font-semibold">{title}</h1>
      {children}
    </main>
  );
}

export function AddFundsScreen() {
  const params = useSearchParams();
  return (
    <PageFrame title="Add funds">
      <ArdaMoneyProvider>
        <AccountAccessStep />
        <MoneyGate>
          <AddFundsForm
            currencyCode="USD"
            presets={[1000, 2500, 5000, 10000, 25000]}
            min={100}
            max={1000000}
            returnedIntentId={params.get("intent")}
          />
        </MoneyGate>
        <Link
          className="inline-flex min-h-11 items-center underline"
          href="/settings/verification?returnTo=%2Fportfolio%2Fadd-funds"
        >
          Account setup
        </Link>
      </ArdaMoneyProvider>
    </PageFrame>
  );
}

export function VerificationScreen() {
  const params = useSearchParams();
  const candidate = normalizeInternalReturnTo(
    params.get("returnTo") ?? undefined,
  );
  const returnTo = candidate?.startsWith("/settings/verification")
    ? "/portfolio"
    : (candidate ?? "/portfolio");
  return (
    <PageFrame title="Account setup">
      <ArdaMoneyProvider>
        <AccountAccessStep />
        <VerificationSteps />
      </ArdaMoneyProvider>
      <Link
        className="inline-flex min-h-11 items-center underline"
        href={returnTo}
      >
        Return to your work
      </Link>
    </PageFrame>
  );
}

export function WithdrawScreen({
  salesOnly = false,
}: { salesOnly?: boolean } = {}) {
  const params = useSearchParams();
  const { data: session } = useSession();
  const expectedSubject = session?.user?.id ?? "";
  const holdings = api.money.sellable.useQuery(
    { expectedSubject },
    { enabled: Boolean(expectedSubject), retry: false },
  );
  const [selected, setSelected] = useState("");
  const [revision, setRevision] = useState(0);
  const refresh = useCallback(() => setRevision((value) => value + 1), []);
  const requested = params.get("configId");
  const options =
    holdings.data?.filter((row) => row.holderClass !== null) ?? [];
  const selection =
    selected ||
    options.find((row) => row.projectTokenConfigId === requested)
      ?.projectTokenConfigId;
  const chosen =
    options.find(
      (row) => `${row.projectTokenConfigId}:${row.holderClass}` === selected,
    ) ??
    options.find((row) => row.projectTokenConfigId === selection) ??
    options[0];
  const holding = useMemo<MoneyHolding | undefined>(
    () =>
      chosen?.holderClass
        ? {
            projectTokenConfigId: chosen.projectTokenConfigId,
            holderClass: chosen.holderClass,
          }
        : undefined,
    [chosen?.projectTokenConfigId, chosen?.holderClass],
  );
  return (
    <PageFrame title={salesOnly ? "Sell units" : "Cash out"}>
      <ArdaMoneyProvider holding={holding}>
        <AccountAccessStep payout={!salesOnly} onApproved={refresh} />
        <MoneyGate>
          {!salesOnly ? (
            <>
              <BalanceSummary />
              <WithdrawForm key={revision} />
            </>
          ) : null}
          {holding ? (
            <section className="space-y-3">
              <label className="block text-sm" htmlFor="sale-holding">
                Project holding
              </label>
              <select
                id="sale-holding"
                className="border-border bg-background min-h-11 w-full border p-2"
                value={`${holding.projectTokenConfigId}:${holding.holderClass}`}
                onChange={(event) => setSelected(event.target.value)}
              >
                {options.map((row) => (
                  <option
                    key={`${row.projectTokenConfigId}:${row.holderClass}`}
                    value={`${row.projectTokenConfigId}:${row.holderClass}`}
                  >
                    {row.unitName} ({row.holderClass?.toLowerCase()})
                  </option>
                ))}
              </select>
              <SellUnitsForm
                key={`${holding.projectTokenConfigId}:${holding.holderClass}`}
              />
            </section>
          ) : holdings.error ? (
            <p role="alert">
              Project holdings could not be loaded. This does not mean you have
              none.
            </p>
          ) : holdings.isLoading ? (
            <p role="status">Loading project holdings.</p>
          ) : (
            <p>No project holdings are available to sell.</p>
          )}
        </MoneyGate>
        <Link
          className="inline-flex min-h-11 items-center underline"
          href="/settings/verification?returnTo=%2Fportfolio%2Fwithdraw"
        >
          Account setup
        </Link>
      </ArdaMoneyProvider>
    </PageFrame>
  );
}

function VerificationSteps() {
  const account = useCustodialAccount();
  return (
    <>
      {account.gate === "ready" ? (
        <p role="status">Your account is ready.</p>
      ) : account.gate === "revoked" ? null : (
        <KycStep />
      )}
      {account.account || account.gate === "revoked" ? (
        <ArdaConsentStatus />
      ) : null}
    </>
  );
}

export function SellUnitsScreen() {
  return <WithdrawScreen salesOnly />;
}
