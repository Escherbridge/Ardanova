"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import { Button } from "~/components/ui/button";
import { buildSignInHref } from "~/lib/auth-navigation";
import { cn } from "~/lib/utils";

import { describeMutationError, describeQueryError } from "./query-error-copy";

/** Honest-failure card for a failed query (extracted from the chats inbox pattern). See `src/components/ui/AGENTS.md` §query-error-state. */
export function QueryErrorState({
  title,
  error,
  onRetry,
  retryLabel = "Try again",
  className,
}: {
  /** What failed, e.g. "Your project records could not be loaded". */
  title: string;
  error: unknown;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}) {
  const pathname = usePathname();
  const copy = describeQueryError(error);

  return (
    <div
      className={cn(
        "border-destructive bg-destructive/10 space-y-3 border-2 p-4",
        className,
      )}
      role="alert"
    >
      <p className="text-foreground font-bold">{title}</p>
      <p className="text-muted-foreground text-sm leading-6">
        {copy.explanation}
      </p>
      {copy.kind === "signed-out" ? (
        <Button asChild variant="outline" size="sm">
          <Link href={buildSignInHref(pathname ?? "/dashboard")}>Sign in</Link>
        </Button>
      ) : copy.retryable && onRetry ? (
        <Button type="button" variant="outline" size="sm" onClick={onRetry}>
          {retryLabel}
        </Button>
      ) : null}
    </div>
  );
}

/** Inline notice for a failed mutation; never renders the raw error message. See `src/components/ui/AGENTS.md` §mutation-error. */
export function MutationErrorNotice({
  error,
  action,
  className,
}: {
  error: unknown;
  /** Verb phrase completing "We couldn't …", e.g. "create the PBI". */
  action: string;
  className?: string;
}) {
  if (!error) return null;
  return (
    <p
      className={cn(
        "border-destructive bg-destructive/10 text-foreground border-2 p-3 text-sm leading-6",
        className,
      )}
      role="alert"
    >
      {describeMutationError(error, action)}
    </p>
  );
}
