"use client";

import Link from "next/link";
import {
  ArrowRight,
  Check,
  Cpu,
  Loader2,
  LockKeyhole,
  ShieldCheck,
  TriangleAlert,
  WalletCards,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import type { AzoaCustodialAccountStatusDto } from "~/lib/api/ardanova";
import { isValueActionAccountReady } from "~/lib/contracts/project-create-contract";
import { cn } from "~/lib/utils";

type AccountStepIcon = "account" | "identity" | "wallet";

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
  const kycApproved = status?.kycStatus === "Approved";
  const accountSetupComplete = isValueActionAccountReady(status);
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
      detail: status
        ? `Identity status: ${status.kycStatus}`
        : "Identity status: Not checked",
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
  const unavailable = ensureError ?? statusError ?? status?.unavailableReason;

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
          {!kycApproved && verificationHref && (
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
