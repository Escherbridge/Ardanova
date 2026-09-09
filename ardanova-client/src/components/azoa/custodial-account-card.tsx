"use client";

import Link from "next/link";
import {
  ArrowRight,
  Check,
  Cpu,
  LifeBuoy,
  Loader2,
  LockKeyhole,
  ShieldCheck,
  TriangleAlert,
  WalletCards,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import type { AzoaCustodialAccountStatusDto } from "~/lib/api/ardanova";
import { evaluateValueActionAccount } from "~/lib/contracts/project-create-contract";
import { cn } from "~/lib/utils";

type AccountStepIcon = "account" | "identity" | "wallet";

/**
 * Where a person held for review reaches the reviewer who owes them a decision. The address
 * belongs to whoever operates the review console, so it is configured per deployment rather
 * than compiled in.
 *
 * Set `NEXT_PUBLIC_REVIEW_CONTACT_EMAIL` to switch the link on. A BLANK or unset value
 * resolves to no link at all - never a `mailto:` that goes nowhere. An empty environment
 * variable is the shape a deploy produces most easily, so the empty case is decided here in
 * code and resolves to the narrowest outcome: no route is offered, and no copy claims one.
 */
export function reviewContactHref(): string | null {
  const configuredAddress = process.env.NEXT_PUBLIC_REVIEW_CONTACT_EMAIL?.trim();
  if (!configuredAddress) return null;
  const subject = encodeURIComponent("Account review - request for an update");
  return `mailto:${configuredAddress}?subject=${subject}`;
}

/**
 * Plain words for the identity step, in the same shape as the other steps' "Ready" /
 * "Not created". The wire values are AZOA's TenantKycStatus members; printing one puts
 * "UnderReview" in front of a sponsor who never asked to read an enum. Exported because
 * the project wizard shows the same row and must not re-derive it.
 */
export function describeIdentityStep(kycStatus: string | null | undefined): string {
  switch (kycStatus) {
    case "Approved":
      return "Complete";
    case "Pending":
      return "In progress";
    case "UnderReview":
      return "Complete, final review open";
    case "Rejected":
      return "Not approved";
    default:
      return "Not started";
  }
}

interface AccountStep {
  label: string;
  detail: string;
  complete: boolean;
  icon: AccountStepIcon;
}

interface CustodialAccountCardProps {
  status?: AzoaCustodialAccountStatusDto;
  isLoading?: boolean;
  isEnsuring?: boolean;
  statusError?: string | null;
  ensureError?: string | null;
  verificationHref?: string | null;
  onEnsure: () => void;
}

export function CustodialAccountCard({
  status,
  isLoading = false,
  isEnsuring = false,
  statusError,
  ensureError,
  verificationHref = "/settings/verification",
  onEnsure,
}: CustodialAccountCardProps) {
  // One classification for the whole card, from the shared contract. Rendering
  // `status.kycStatus` or `status.unavailableReason` directly is what put a machine
  // token and a provider diagnostic on a sponsor's own account screen.
  const evaluation = evaluateValueActionAccount(status, {
    loadFailed: Boolean(statusError),
  });
  const underReview = evaluation.state === "under-review";
  const reviewContact = underReview ? reviewContactHref() : null;
  const kycApproved = status?.kycStatus === "Approved";
  const accountSetupComplete = evaluation.ready;
  const needsProvisioning =
    status?.identityReady !== true ||
    status.walletReady !== true ||
    (!accountSetupComplete &&
      (!status?.avatarId?.trim() ||
        !status?.walletId?.trim() ||
        !status?.walletAddress?.trim()));
  const stateLabel = isLoading
    ? "Checking"
    : accountSetupComplete
      ? "Account setup complete"
      : underReview
        ? "Final review in progress"
        : status?.identityReady && status.walletReady
          ? "Identity check needed"
          : status?.identityReady
            ? "Account completion pending"
            : "Setup required";
  const steps: AccountStep[] = [
    {
      label: "Account setup",
      detail: status?.identityReady === true ? "Ready" : "Not created",
      complete: status?.identityReady === true,
      icon: "account",
    },
    {
      label: "Identity check",
      detail: `Identity check: ${describeIdentityStep(status?.kycStatus).toLowerCase()}`,
      complete: kycApproved,
      icon: "identity",
    },
    {
      label: "Managed payment account",
      detail: status?.walletReady === true ? "Ready" : "Not created",
      complete: status?.walletReady === true,
      icon: "wallet",
    },
  ];
  // A problem is worth an alarm; a held review is not. Neither ever carries the node's
  // own words: "service-unavailable" is the contract's classification of a blank read OR
  // an unavailableReason, and the copy that goes with it says nothing about this person.
  const unavailable =
    ensureError ??
    statusError ??
    (evaluation.state === "service-unavailable" ? evaluation.message : null);

  return (
    <section
      className="border-foreground bg-card border-2"
      aria-labelledby="azoa-account-title"
    >
      <div className="border-foreground grid gap-5 border-b-2 p-5 sm:grid-cols-[1fr_auto] sm:items-end sm:p-6">
        <div>
          <div className="mb-3 flex flex-wrap items-center gap-2">
            <Badge variant="outline" className="font-mono uppercase">
              Account setup
            </Badge>
            <span className="text-muted-foreground font-mono text-[0.65rem] tracking-[0.14em] uppercase">
              Managed securely
            </span>
          </div>
          <h2
            id="azoa-account-title"
            className="font-mono text-2xl font-black tracking-[-0.045em] uppercase sm:text-3xl"
          >
            Complete your account setup
          </h2>
          <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed">
            ArdaNova coordinates the setup. A trusted verification partner
            checks identity and manages the protected account used for
            funding and rewards. ArdaNova keeps only account references and
            readiness status.
          </p>
        </div>
        <Badge
          variant={accountSetupComplete ? "success" : "secondary"}
          className="min-h-8 w-fit rounded-none px-3 font-mono uppercase"
        >
          {stateLabel}
        </Badge>
      </div>

      <ol className="border-foreground grid border-b-2 sm:grid-cols-3">
        {steps.map((step, index) => {
          return (
            <li
              key={step.label}
              className={cn(
                "min-w-0 p-4 sm:p-5",
                index > 0 &&
                  "border-foreground border-t-2 sm:border-t-0 sm:border-l-2",
              )}
            >
              <div className="mb-4 flex items-center justify-between gap-3">
                <span className="font-mono text-xs font-black tracking-[0.12em] uppercase">
                  0{index + 1} / {step.label}
                </span>
                <span
                  className={cn(
                    "border-foreground inline-flex size-8 items-center justify-center border-2",
                    step.complete
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted",
                  )}
                  aria-label={step.complete ? "Complete" : "Not complete"}
                >
                  {step.complete ? (
                    <Check className="size-4" aria-hidden="true" />
                  ) : step.icon === "account" ? (
                    <Cpu className="size-4" aria-hidden="true" />
                  ) : step.icon === "identity" ? (
                    <ShieldCheck className="size-4" aria-hidden="true" />
                  ) : (
                    <WalletCards className="size-4" aria-hidden="true" />
                  )}
                </span>
              </div>
              <p className="text-muted-foreground overflow-hidden font-mono text-xs text-ellipsis whitespace-nowrap">
                {step.detail}
              </p>
            </li>
          );
        })}
      </ol>

      <div className="grid gap-4 p-5 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:p-6">
        <div className="flex items-start gap-3">
          {unavailable ? (
            <TriangleAlert
              className="text-destructive mt-0.5 size-5 shrink-0"
              aria-hidden="true"
            />
          ) : (
            <LockKeyhole
              className="text-primary mt-0.5 size-5 shrink-0"
              aria-hidden="true"
            />
          )}
          <p
            className="text-sm leading-relaxed"
            role="status"
            aria-live="polite"
          >
            {unavailable
              ? unavailable
              : underReview
                ? evaluation.message
                : accountSetupComplete
                  ? "Account setup is complete. Funding, paid work, and payouts still require their own server-side project checks."
                  : "Creating this account does not move money or approve a payment. You can create projects and collaborate while setup is pending; funding, paid work, and rewards stay unavailable."}
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:min-w-52">
          {needsProvisioning && (
            <Button
              className="min-h-11 w-full"
              onClick={onEnsure}
              disabled={isEnsuring || isLoading}
            >
              {isEnsuring ? (
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
              ) : (
                <Cpu className="size-4" aria-hidden="true" />
              )}
              {status?.identityReady
                ? "Continue account setup"
                : "Start account setup"}
            </Button>
          )}
          {underReview && reviewContact && (
            <Button variant="outline" className="min-h-11 w-full" asChild>
              <a href={reviewContact}>
                <LifeBuoy className="size-4" aria-hidden="true" />
                Contact the review team
              </a>
            </Button>
          )}
          {!kycApproved && !underReview && verificationHref && (
            <Button variant="outline" className="min-h-11 w-full" asChild>
              <Link href={verificationHref}>
                Review identity setup
                <ArrowRight className="size-4" aria-hidden="true" />
              </Link>
            </Button>
          )}
        </div>
      </div>
    </section>
  );
}
