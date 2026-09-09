"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { ArrowUpRight, ShieldCheck, X } from "lucide-react";

import { Button } from "~/components/ui/button";
import { reviewContactHref } from "~/components/azoa/custodial-account-card";
import { evaluateValueActionAccount } from "~/lib/contracts/project-create-contract";
import { api } from "~/trpc/react";

const DISMISS_STORAGE_PREFIX = "ardanova:onboarding:setup-prompt:dismissed:v1:";

function readDismissed(userId: string): boolean {
  try {
    return window.sessionStorage.getItem(`${DISMISS_STORAGE_PREFIX}${userId}`) === "1";
  } catch {
    // Storage can be blocked (private browsing, locked-down profile); fail open to showing the prompt.
    return false;
  }
}

/**
 * Soft, dismissible dashboard nudge that moves the identity check ahead of the moment a
 * sponsorship or paid task actually requires it. Never blocks navigation; it only reappears
 * next session (sessionStorage) for as long as account setup stays incomplete.
 *
 * Three-way, never a boolean: an account held for a human decision has nothing left to
 * finish, so it gets its own words and a route to the reviewer - never "finish account
 * setup", which is an instruction that person cannot act on.
 */
export function SetupPrompt() {
  const { data: session } = useSession();
  const userId = session?.user?.id;
  const [dismissed, setDismissed] = useState(true);

  const custodyStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: Boolean(userId),
    retry: false,
  });

  useEffect(() => {
    if (!userId) return;
    setDismissed(readDismissed(userId));
  }, [userId]);

  if (!userId || dismissed) return null;
  if (custodyStatus.isLoading) return null;

  const evaluation = evaluateValueActionAccount(custodyStatus.data, {
    loadFailed: Boolean(custodyStatus.error),
  });
  // Nothing to nudge about when setup is done, and nothing honest to say when the account
  // could not be read at all - a failed read is not evidence that setup is unfinished.
  if (evaluation.state === "ready" || evaluation.state === "service-unavailable") {
    return null;
  }

  const underReview = evaluation.state === "under-review";
  const reviewContact = underReview ? reviewContactHref() : null;

  function handleDismiss() {
    setDismissed(true);
    try {
      window.sessionStorage.setItem(`${DISMISS_STORAGE_PREFIX}${userId}`, "1");
    } catch {
      // Best-effort only; a blocked storage API just means the prompt returns on the next view.
    }
  }

  return (
    <section
      className="border-foreground bg-secondary flex flex-col gap-4 border p-5 sm:flex-row sm:items-center sm:justify-between sm:p-7"
      aria-labelledby="setup-prompt-title"
    >
      <div className="flex items-start gap-4">
        <ShieldCheck
          className="text-primary mt-1 size-6 shrink-0"
          aria-hidden="true"
        />
        <div>
          <p className="text-muted-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
            {underReview
              ? "Nothing left for you to do"
              : "Before you sponsor or get paid"}
          </p>
          <h2
            id="setup-prompt-title"
            className="mt-1 text-xl font-semibold sm:text-2xl"
          >
            {underReview ? "Your account is being reviewed" : "Finish account setup"}
          </h2>
          <p className="text-muted-foreground mt-2 max-w-xl text-sm leading-relaxed">
            {underReview
              ? evaluation.message
              : "Platform policy requires a completed identity check before any project can be funded or any worker can be paid — there is no smaller-sponsor exception. Handle it now, while you have a minute, instead of at the moment a project is waiting on it."}
          </p>
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        {underReview && reviewContact ? (
          <Button asChild className="min-h-11">
            <a href={reviewContact}>
              Contact the review team
              <ArrowUpRight className="size-4" aria-hidden="true" />
            </a>
          </Button>
        ) : (
          <Button asChild className="min-h-11">
            <Link href="/settings/verification?returnTo=%2Fdashboard">
              {underReview ? "See where this stands" : "Finish account setup"}
              <ArrowUpRight className="size-4" aria-hidden="true" />
            </Link>
          </Button>
        )}
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="min-h-11 min-w-11"
          onClick={handleDismiss}
          aria-label={
            underReview
              ? "Dismiss account review notice"
              : "Dismiss account setup reminder"
          }
        >
          <X className="size-4" aria-hidden="true" />
        </Button>
      </div>
    </section>
  );
}
