"use client";
import { ConsentStatus, useCustodialAccount } from "azoa-sdk/react";
import { Button } from "~/components/ui/button";

export function ArdaConsentStatus() {
  const account = useCustodialAccount();
  return (
    <section aria-label="Payment permission" className="space-y-3">
      {account.account?.consent === "none" ? (
        <p role="status">
          Payment permission is inactive. New payment instructions are paused.
          Earlier requests may still complete.
        </p>
      ) : account.gate === "unavailable" && account.refusal ? (
        <p role="alert">
          Current payment permission could not be confirmed. Check again.
        </p>
      ) : (
        <ConsentStatus onRefusal={() => void account.refresh()} />
      )}
      <Button
        variant="secondary"
        disabled={account.busy}
        onClick={() => void account.refresh()}
      >
        Check permission
      </Button>
    </section>
  );
}
