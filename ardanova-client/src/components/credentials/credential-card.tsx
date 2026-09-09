"use client";

import { Card, CardContent, CardFooter } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { CredentialBadge } from "./credential-badge";
import { BadgeCheck, Clock, PauseCircle, XCircle } from "lucide-react";
import { cn } from "~/lib/utils";
import {
  getCredentialProofPresentation,
  getCredentialProofReference,
} from "~/lib/credentials/credential-proof";
import type { MembershipCredential } from "~/lib/api/ardanova/endpoints/membership-credentials";

/**
 * A credential plus the server's issuance-path flag.
 *
 * `isIndependentlyVerifiable` is true only when the credential was issued through the credential
 * issuance node - the one path that puts a proof in the MEMBER's hands. It is read as `=== true`,
 * so a credential from an API that does not publish the field yet reads as NOT verifiable.
 *
 * That fail-closed default is the point of this type. The presence of a stored identifier is not
 * evidence of anything: the shipped simulated provider fabricated one outright, and the
 * address-based mint it replaced put a real asset in the PLATFORM's own hands, which proves
 * nothing about the member. `CredentialUtilityService.GrantAndMintAsync` now refuses to record any
 * proof reference unless the issuance node produced it; this flag is how a screen asks.
 */
type CredentialWithIssuancePath = MembershipCredential & {
  isIndependentlyVerifiable?: boolean | null;
};

interface CredentialCardProps {
  credential: CredentialWithIssuancePath;
  scopeName?: string;
  onClick?: () => void;
  className?: string;
}

/**
 * How a member was given this credential, in the words they would use themselves.
 * The API's enum names are internal and are never shown.
 */
const grantMethodLabels: Record<string, string> = {
  FOUNDER: "Founding member",
  DAO_VOTE: "Approved by member vote",
  CONTRIBUTION_THRESHOLD: "Earned by contributing",
  APPLICATION_APPROVED: "Joined and approved",
  GAME_SDK_THRESHOLD: "Earned in-app",
};

type VerificationState = {
  label: string;
  detail: string;
  icon: typeof BadgeCheck;
  tone: string;
};

/** How each proof reading is drawn. The words themselves come from the shared translator. */
const proofStyles = {
  withdrawn: { icon: XCircle, tone: "text-destructive" },
  paused: { icon: PauseCircle, tone: "text-muted-foreground" },
  verified: { icon: BadgeCheck, tone: "text-neon-green" },
  recorded: { icon: BadgeCheck, tone: "text-neon-cyan" },
  preparing: { icon: Clock, tone: "text-neon-yellow" },
} as const;

/** Whether the server says this credential went through the issuance path. Absent means no. */
function wasIndependentlyIssued(credential: CredentialWithIssuancePath): boolean {
  return credential.isIndependentlyVerifiable === true;
}

/**
 * One reading of a credential, in plain words.
 *
 * The words come from ~/lib/credentials/credential-proof.ts, which is the single place the
 * node's chain vocabulary is translated - the card and the credential detail page must not
 * drift into saying different things about the same credential.
 */
function readVerification(
  credential: CredentialWithIssuancePath,
): VerificationState {
  // The proof reading is gated on the issuance path, never on the presence of an identifier.
  // Withdrawn and paused still outrank everything, which is why status is passed through.
  const issued = wasIndependentlyIssued(credential);
  const { state, label, detail } = getCredentialProofPresentation({
    status: credential.status,
    assetId: issued ? credential.assetId : null,
    mintTxHash: issued ? credential.mintTxHash : null,
  });
  return { label, detail, ...proofStyles[state] };
}

/**
 * The single reference a member can quote to prove this credential, and only when there is
 * something to prove: an un-issued credential has no reference, however many identifiers the
 * record happens to carry.
 */
function proofReference(credential: CredentialWithIssuancePath): string | null {
  if (!wasIndependentlyIssued(credential)) return null;
  return getCredentialProofReference({
    assetId: credential.assetId,
    mintTxHash: credential.mintTxHash,
  });
}

function formatDate(value?: string | null): string | null {
  if (!value) return null;
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed.toLocaleDateString();
}

/**
 * The disclosure behind "View proof". Everything a member needs to show someone
 * else, and nothing that asks them to learn what any of it is made of.
 */
function ProofDisclosure({
  credential,
  scopeLabel,
}: {
  credential: CredentialWithIssuancePath;
  scopeLabel: string;
}) {
  const reference = proofReference(credential);
  const issuedOn = formatDate(credential.createdAt);
  const withdrawnOn = formatDate(credential.revokedAt);

  return (
    <details
      className="group w-full text-xs"
      // The card itself may be clickable; opening the disclosure is not "opening
      // the credential", so the toggle keeps its own events.
      onClick={(event) => event.stopPropagation()}
      onKeyDown={(event) => event.stopPropagation()}
    >
      <summary className="text-muted-foreground hover:text-foreground focus-visible:ring-ring cursor-pointer list-none rounded focus-visible:ring-2 focus-visible:outline-none">
        View proof
      </summary>
      <dl className="text-muted-foreground mt-2 space-y-1">
        <div className="flex items-start justify-between gap-2">
          <dt>Issued by</dt>
          <dd className="text-foreground text-right">{scopeLabel}</dd>
        </div>
        {issuedOn && (
          <div className="flex items-start justify-between gap-2">
            <dt>Issued on</dt>
            <dd className="text-foreground text-right">{issuedOn}</dd>
          </div>
        )}
        {withdrawnOn && (
          <div className="flex items-start justify-between gap-2">
            <dt>Withdrawn on</dt>
            <dd className="text-foreground text-right">{withdrawnOn}</dd>
          </div>
        )}
        {reference ? (
          <div className="flex items-start justify-between gap-2">
            <dt>Reference</dt>
            <dd className="text-foreground max-w-[60%] truncate text-right font-mono">
              {reference}
            </dd>
          </div>
        ) : (
          <p className="pt-1">
            A reference appears here once verification finishes.
          </p>
        )}
      </dl>
    </details>
  );
}

export function CredentialCard({
  credential,
  scopeName,
  onClick,
  className,
}: CredentialCardProps) {
  const isActive = credential.status === "ACTIVE";
  const scopeLabel = credential.projectId ? "Project" : "Guild";
  const grantLabel =
    grantMethodLabels[credential.grantedVia] ?? "Membership granted";
  const verification = readVerification(credential);
  const VerificationIcon = verification.icon;

  return (
    <Card
      variant={isActive ? "neon" : "ghost"}
      padding="sm"
      className={cn(
        onClick &&
          "hover:border-primary/50 focus-visible:ring-ring cursor-pointer focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none",
        !isActive && "opacity-60",
        className,
      )}
      onClick={onClick}
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      aria-label={
        onClick
          ? `Open ${scopeName ?? scopeLabel.toLowerCase()} credential, ${verification.label.toLowerCase()}`
          : undefined
      }
      onKeyDown={
        onClick
          ? (event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                onClick();
              }
            }
          : undefined
      }
    >
      <CardContent className="space-y-3">
        {/* Header: scope + level */}
        <div className="flex items-center justify-between">
          <Badge
            variant={credential.projectId ? "neon-pink" : "neon-purple"}
            size="sm"
          >
            {scopeLabel}
          </Badge>
          <CredentialBadge tier={credential.tier} size="sm" />
        </div>

        {/* Scope name */}
        {scopeName && (
          <div className="truncate text-sm font-medium">{scopeName}</div>
        )}

        {/* How it was granted + when */}
        <div className="text-muted-foreground flex items-center justify-between text-xs">
          <span>{grantLabel}</span>
          <span>{formatDate(credential.createdAt) ?? "Date unavailable"}</span>
        </div>

        {/* Verification, in one line the member can act on */}
        <div className={cn("flex items-center gap-1", verification.tone)}>
          <VerificationIcon className="size-3" />
          <span className="text-[10px]">{verification.label}</span>
        </div>
        <p className="text-muted-foreground text-[10px]">
          {verification.detail}
        </p>
      </CardContent>

      <CardFooter className="border-border border-t pt-2">
        <ProofDisclosure
          credential={credential}
          scopeLabel={scopeName ?? scopeLabel}
        />
      </CardFooter>
    </Card>
  );
}

export type { CredentialCardProps, CredentialWithIssuancePath };
