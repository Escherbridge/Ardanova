"use client";
import { useEffect, useRef, useState } from "react";
import { useSession } from "next-auth/react";
import { useCustodialAccount, useMoneyContext } from "azoa-sdk/react";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import type { MemberHandoff } from "./contracts";

export function AccountAccessStep({
  payout = false,
  onApproved,
}: {
  payout?: boolean;
  onApproved?: () => void;
}) {
  const { keyStore } = useMoneyContext();
  const { data: session } = useSession();
  const expectedSubject = session?.user?.id ?? "";
  const account = useCustodialAccount();
  const accountRef = useRef(account);
  accountRef.current = account;
  const [requestId, setRequestId] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  useEffect(() => {
    try {
      const saved = keyStore.read("member-handoff");
      if (saved && /^[A-Za-z0-9-]{1,128}$/u.test(saved)) setRequestId(saved);
    } catch {
      setProblem(
        "The setup reference could not be read. Try again before continuing.",
      );
    }
  }, [keyStore]);
  const accept = (value: MemberHandoff) => {
    try {
      keyStore.write("member-handoff", value.requestId);
      setRequestId(value.requestId);
      setProblem(null);
    } catch {
      setProblem(
        "The setup reference could not be saved. Check setup status before starting again.",
      );
    }
  };
  const begin = api.money.createHandoff.useMutation({ onSuccess: accept });
  const resend = api.money.resendHandoff.useMutation({ onSuccess: accept });
  const status = api.money.getHandoff.useQuery(
    { id: requestId ?? "", expectedSubject },
    {
      enabled: Boolean(requestId),
      retry: false,
      refetchInterval: (query) =>
        query.state.data?.status === "pending" ||
        query.state.data?.status === "approving"
          ? 5000
          : false,
    },
  );
  const stage = status.data?.status;
  const expired = Boolean(
    status.data && Date.parse(status.data.expiresAt) <= Date.now(),
  );
  useEffect(() => {
    if (stage === "approved") {
      void accountRef.current.refresh();
      onApproved?.();
    }
  }, [stage, onApproved]);
  const needsConsent = account.gate === "revoked";
  if (!payout && !needsConsent && !requestId) return null;
  if (account.gate === "loading")
    return <p role="status">Checking account access.</p>;
  const busy = begin.isPending || resend.isPending;
  return (
    <section
      className="border-border space-y-3 border p-5"
      aria-label="Secure account access"
    >
      <h2 className="font-mono text-lg font-semibold">Secure account access</h2>
      {requestId ? (
        <>
          <p role="status">
            {!status.data
              ? "Checking your access request."
              : stage === "approved"
                ? "Request approved. Current account access and your bank destination are checked separately."
                : expired && stage === "pending"
                  ? "This access request expired. Start a new setup request."
                  : stage === "declined"
                    ? "Permission was declined. Review account status before continuing."
                    : "Your access link is ready. Delivery is pending with the account operator."}
          </p>
          {status.data?.delivery === "operator_stub" ? (
            <p className="text-muted-foreground text-sm">
              Email delivery is not connected. Contact the account operator to
              receive your secure link. Keep that link private.
            </p>
          ) : null}
          {status.data ? (
            <p className="text-muted-foreground text-sm">
              Access request expires{" "}
              {new Date(status.data.expiresAt).toLocaleString()}.
            </p>
          ) : null}
          <Button
            variant="outline"
            disabled={busy || (stage === "approving" && !expired)}
            onClick={() => {
              if (expired || stage === "approved" || stage === "declined")
                begin.mutate({ expectedSubject });
              else if (requestId)
                resend.mutate({ id: requestId, expectedSubject });
            }}
          >
            {expired || stage === "approved" || stage === "declined"
              ? "Start new setup"
              : "Request a new link"}
          </Button>
          <Button
            variant="outline"
            disabled={status.isFetching}
            onClick={() => {
              void status.refetch();
              void account.refresh();
            }}
          >
            Check setup status
          </Button>
        </>
      ) : (
        <>
          <p>
            Set up account access and your bank destination on the secure
            account page. You choose whether this site can manage payments for
            you.
          </p>
          <Button
            disabled={busy || !account.account}
            onClick={() => begin.mutate({ expectedSubject })}
          >
            Continue secure setup
          </Button>
        </>
      )}
      {problem || begin.error || resend.error || status.error ? (
        <p role="alert">
          {problem ??
            "Setup could not be confirmed. Keep the current reference and check again."}
        </p>
      ) : null}
    </section>
  );
}
