"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { ArrowUpRight, ShieldCheck, X } from "lucide-react";

import { Button } from "~/components/ui/button";
import { isValueActionAccountReady } from "~/lib/contracts/project-create-contract";
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
  if (custodyStatus.isLoading || custodyStatus.error) return null;
  if (isValueActionAccountReady(custodyStatus.data)) return null;

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
            Before you sponsor or get paid
          </p>
          <h2
            id="setup-prompt-title"
            className="mt-1 text-xl font-semibold sm:text-2xl"
          >
            Finish account setup
          </h2>
          <p className="text-muted-foreground mt-2 max-w-xl text-sm leading-relaxed">
            Platform policy requires a completed identity check before any project can be
            funded or any worker can be paid — there is no smaller-sponsor exception. Handle
            it now, while you have a minute, instead of at the moment a project is waiting on
            it.
          </p>
        </div>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <Button asChild className="min-h-11">
          <Link href="/settings/verification?returnTo=%2Fdashboard">
            Finish account setup
            <ArrowUpRight className="size-4" aria-hidden="true" />
          </Link>
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="min-h-11 min-w-11"
          onClick={handleDismiss}
          aria-label="Dismiss account setup reminder"
        >
          <X className="size-4" aria-hidden="true" />
        </Button>
      </div>
    </section>
  );
}
