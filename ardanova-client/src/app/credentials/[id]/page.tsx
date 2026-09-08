"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { api } from "~/trpc/react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import {
  CredentialBadge,
  statusConfig,
} from "~/components/credentials/credential-badge";
import { TierProgress } from "~/components/credentials/tier-progress";
import {
  getCredentialProofPresentation,
  getCredentialProofReference,
  getTransferabilityLabel,
} from "~/lib/credentials/credential-proof";
import {
  ArrowLeft,
  BadgeCheck,
  Clock,
  Loader2,
  PauseCircle,
  Shield,
  XCircle,
} from "lucide-react";

// This page is how a worker's earned tier is shown to them, so it is a member surface and not an
// operator one. Everything the node reports about the credential's proof is read through
// ~/lib/credentials/credential-proof.ts; the node's own words never reach this file. See
// src/lib/AGENTS.md and scripts/verify-product-vocabulary.mjs, which fails the build if they do.

const proofPresentation = {
  withdrawn: { icon: XCircle, tone: "text-destructive" },
  paused: { icon: PauseCircle, tone: "text-muted-foreground" },
  verified: { icon: BadgeCheck, tone: "text-neon-green" },
  recorded: { icon: BadgeCheck, tone: "text-neon-cyan" },
  preparing: { icon: Clock, tone: "text-neon-yellow" },
} as const;

export default function CredentialDetailPage() {
  const params = useParams();
  const id = params.id as string;

  const { data: chainData, isLoading } =
    api.credentialUtility.getChainData.useQuery({ id });

  if (isLoading) {
    return (
      <div className="bg-background flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary size-8 animate-spin" />
      </div>
    );
  }

  if (!chainData) {
    return (
      <div className="bg-background flex min-h-screen flex-col items-center justify-center">
        <Shield className="text-muted-foreground mb-4 size-12" />
        <h1 className="text-foreground text-2xl font-bold">
          Credential not found
        </h1>
        <p className="text-muted-foreground mt-2">
          This credential may have been withdrawn or doesn&apos;t exist.
        </p>
        <Link href="/dashboard/profile">
          <Button variant="outline" className="mt-4">
            <ArrowLeft className="mr-2 size-4" />
            Back to Profile
          </Button>
        </Link>
      </div>
    );
  }

  const { credential, asaInfo, isOnChain, chainVerified } = chainData;
  const scope = credential.projectId ? "PROJECT" : "GUILD";
  const statusCfg = statusConfig[credential.status];

  const report = {
    status: credential.status,
    assetId: asaInfo?.assetId ?? credential.assetId,
    mintTxHash: credential.mintTxHash,
    isOnChain,
    chainVerified,
  };
  const proof = getCredentialProofPresentation(report);
  const reference = getCredentialProofReference(report);
  const ProofIcon = proofPresentation[proof.state].icon;
  const proofTone = proofPresentation[proof.state].tone;

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl p-6">
        {/* Back button */}
        <Link
          href="/dashboard/profile"
          className="text-muted-foreground hover:text-foreground mb-6 inline-flex items-center gap-2 transition-colors"
        >
          <ArrowLeft className="size-4" />
          Back to Profile
        </Link>

        {/* Credential Header */}
        <Card
          className="mb-6"
          variant={credential.status === "ACTIVE" ? "neon" : "default"}
        >
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-3">
                <Shield className="size-6" />
                Membership Credential
              </CardTitle>
              <CredentialBadge
                tier={credential.tier}
                status={credential.status}
                size="lg"
                showStatus
              />
            </div>
            <CardDescription>
              {scope} credential{" "}
              {credential.status === "ACTIVE" ? "granted" : "withdrawn"} via{" "}
              {credential.grantedVia.replace(/_/g, " ").toLowerCase()}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Tier Progress */}
            {credential.tier && <TierProgress currentTier={credential.tier} />}

            {/* Details grid */}
            <div className="grid grid-cols-2 gap-4">
              <DetailItem label="Scope" value={scope} />
              <DetailItem label="Status">
                <Badge variant={statusCfg?.variant ?? "outline"}>
                  {credential.status}
                </Badge>
              </DetailItem>
              <DetailItem
                label="Grant Method"
                value={credential.grantedVia.replace(/_/g, " ")}
              />
              <DetailItem
                label="Granted"
                value={
                  credential.createdAt
                    ? new Date(credential.createdAt).toLocaleDateString()
                    : "Not recorded"
                }
              />
              {credential.revokedAt && (
                <DetailItem
                  label="Withdrawn"
                  value={new Date(credential.revokedAt).toLocaleDateString()}
                />
              )}
              <DetailItem
                label="Transferable"
                value={getTransferabilityLabel(credential.isTransferable)}
              />
            </div>
          </CardContent>
        </Card>

        {/* Verification — one reading, in the member's own words */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ProofIcon className={`size-5 ${proofTone}`} />
              Verification
            </CardTitle>
            <CardDescription>
              Whether this credential can be checked by someone outside
              ArdaNova.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="border-border flex items-start gap-3 rounded border p-3">
              <ProofIcon className={`mt-0.5 size-5 shrink-0 ${proofTone}`} />
              <div>
                <div className={`font-medium ${proofTone}`}>{proof.label}</div>
                <div className="text-muted-foreground text-xs">
                  {proof.detail}
                </div>
              </div>
            </div>

            {reference ? (
              <DetailItem label="Reference">
                <span className="inline-block max-w-full truncate font-mono text-xs">
                  {reference}
                </span>
                <p className="text-muted-foreground mt-1 text-[10px]">
                  Quote this if someone asks you to prove this credential.
                </p>
              </DetailItem>
            ) : (
              <p className="text-muted-foreground text-xs">
                A reference appears here once verification finishes.
              </p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function DetailItem({
  label,
  value,
  children,
}: {
  label: string;
  value?: string;
  children?: React.ReactNode;
}) {
  return (
    <div>
      <div className="text-muted-foreground mb-1 text-xs font-medium">
        {label}
      </div>
      {children ?? <div className="text-sm font-medium">{value}</div>}
    </div>
  );
}
