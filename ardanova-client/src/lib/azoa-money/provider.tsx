"use client";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useSession } from "next-auth/react";
import { AzoaMoneyProvider } from "azoa-sdk/react";
import { safeRedirectUrl } from "azoa-sdk/money";
import type { KeyStore } from "azoa-sdk/money";
import { api } from "~/trpc/react";
import { createArdaMoneyTransport, type MoneyHost } from "./transport";
import { durableMoneyStore } from "./storage";
import { ardaMoneySlots } from "./slots";
import type { MoneyHolding } from "./contracts";

export function ArdaMoneyProvider({
  children,
  holding,
}: {
  children: ReactNode;
  holding?: MoneyHolding;
}) {
  const { data: session, status } = useSession();
  const subject = session?.user?.id;
  if (status === "loading") return <p role="status">Loading your account.</p>;
  if (!subject) return <p>Sign in to continue.</p>;
  return (
    <SubjectMoney key={subject} subject={subject} holding={holding}>
      {children}
    </SubjectMoney>
  );
}

function SubjectMoney({
  subject,
  holding,
  children,
}: {
  subject: string;
  holding?: MoneyHolding;
  children: ReactNode;
}) {
  const client = api.useUtils().client;
  const active = useRef(true);
  useEffect(() => {
    active.current = true;
    return () => {
      active.current = false;
    };
  }, []);
  function guard<T>(call: () => Promise<T>): Promise<T> {
    if (!active.current)
      return Promise.reject(
        new Error("The original account is no longer active."),
      );
    return call();
  }
  const [storage, setStorage] = useState<KeyStore | null>(null);
  const [storageError, setStorageError] = useState<string | null>(null);
  useEffect(() => {
    try {
      setStorage(durableMoneyStore(subject, window.localStorage));
    } catch {
      setStorageError(
        "This device could not read or save payment requests safely. Keep its saved data; allow local storage if blocked, or contact support before starting another.",
      );
    }
  }, [subject]);
  const host = useMemo<MoneyHost>(
    () => ({
      voidCashOut: (requestKey) =>
        guard(() =>
          client.money.voidCashOut.mutate({
            requestKey,
            expectedSubject: subject,
          }),
        ),
      voidSale: (input) =>
        guard(() =>
          client.money.voidSale.mutate({ ...input, expectedSubject: subject }),
        ),
      accountCapabilities: () =>
        guard(() =>
          client.money.accountCapabilities.query({ expectedSubject: subject }),
        ),
      accountStatus: () =>
        guard(() =>
          client.money.accountStatus.query({ expectedSubject: subject }),
        ),
      ensureAccount: () =>
        guard(() =>
          client.money.ensureAccount.mutate({ expectedSubject: subject }),
        ),
      beginKyc: () =>
        guard(() => client.money.beginKyc.mutate({ expectedSubject: subject })),
      revokeConsent: () =>
        guard(() =>
          client.money.revokeConsent.mutate({ expectedSubject: subject }),
        ),
      checkoutCapabilities: () =>
        guard(() =>
          client.money.checkoutCapabilities.query({ expectedSubject: subject }),
        ),
      beginCheckout: (input) =>
        guard(() =>
          client.money.beginCheckout.mutate({
            ...input,
            expectedSubject: subject,
          }),
        ),
      getCheckout: (id) =>
        guard(() =>
          client.money.getCheckout.query({ id, expectedSubject: subject }),
        ),
      voidCheckout: (requestKey) =>
        guard(() =>
          client.money.voidCheckout.mutate({
            requestKey,
            expectedSubject: subject,
          }),
        ),
      cashOutAvailability: () =>
        guard(() =>
          client.money.cashOutAvailability.query({ expectedSubject: subject }),
        ),
      cashOutDestination: () =>
        guard(() =>
          client.money.cashOutDestination.query({ expectedSubject: subject }),
        ),
      startCashOut: (input) =>
        guard(() =>
          client.money.startCashOut.mutate({
            ...input,
            expectedSubject: subject,
          }),
        ),
      cashOutHistory: () =>
        guard(() =>
          client.money.cashOutHistory.query({ expectedSubject: subject }),
        ),
      sellable: () =>
        guard(() => client.money.sellable.query({ expectedSubject: subject })),
      quoteSale: (input) =>
        guard(() =>
          client.money.quoteSale.mutate({ ...input, expectedSubject: subject }),
        ),
      sell: (input) =>
        guard(() =>
          client.money.sell.mutate({ ...input, expectedSubject: subject }),
        ),
    }),
    [client, subject],
  );
  const transport = useMemo(
    () => createArdaMoneyTransport(host, holding),
    [host, holding],
  );
  if (storageError) return <p role="alert">{storageError}</p>;
  if (!storage) return <p role="status">Preparing secure requests.</p>;
  return (
    <AzoaMoneyProvider
      subject={subject}
      transport={transport}
      keyStore={storage}
      slots={ardaMoneySlots}
      messages={{
        balance: {
          title: "Cash-out balance",
          availableLabel: "Available to cash out",
        },
      }}
      onRedirect={(url) => {
        const safe = safeRedirectUrl(url);
        if (safe) window.location.assign(safe);
      }}
    >
      {children}
    </AzoaMoneyProvider>
  );
}
