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
  ArrowLeft,
  ExternalLink,
  Loader2,
  Shield,
  Link2,
  CheckCircle2,
  Clock,
} from "lucide-react";

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
          This credential may have been revoked or doesn&apos;t exist.
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
              {credential.status === "ACTIVE" ? "granted" : "revoked"} via{" "}
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
                  label="Revoked"
                  value={new Date(credential.revokedAt).toLocaleDateString()}
                />
              )}
              <DetailItem
                label="Transferable"
                value={credential.isTransferable ? "Yes" : "No (Soulbound)"}
              />
            </div>
          </CardContent>
        </Card>

        {/* On-Chain Status */}
        <Card className="mb-6">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Link2 className="size-5" />
              Blockchain Status
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="border-border flex items-center gap-3 rounded border p-3">
              {isOnChain ? (
                <CheckCircle2 className="text-neon-green size-5" />
              ) : (
                <Clock className="text-neon-yellow size-5" />
              )}
              <div>
                <div className="font-medium">
                  {isOnChain ? "Minted on Algorand" : "Pending mint"}
                </div>
                <div className="text-muted-foreground text-xs">
                  {isOnChain
                    ? "This credential is verified on the Algorand blockchain"
                    : "This credential has not yet been minted on-chain"}
                </div>
              </div>
            </div>

            {chainVerified && (
              <div className="border-neon-green/30 bg-neon-green/5 flex items-center gap-3 rounded border p-3">
                <CheckCircle2 className="text-neon-green size-5" />
                <div>
                  <div className="text-neon-green font-medium">
                    Chain Verified
                  </div>
                  <div className="text-muted-foreground text-xs">
                    On-chain data matches platform records
                  </div>
                </div>
              </div>
            )}

            {/* Mint/Revoke transaction hashes */}
            <div className="grid grid-cols-2 gap-4">
              {credential.mintTxHash && (
                <DetailItem label="Mint Transaction">
                  <span className="inline-block max-w-[200px] truncate font-mono text-xs">
                    {credential.mintTxHash}
                  </span>
                </DetailItem>
              )}
              {credential.revokeTxHash && (
                <DetailItem label="Revoke Transaction">
                  <span className="inline-block max-w-[200px] truncate font-mono text-xs">
                    {credential.revokeTxHash}
                  </span>
                </DetailItem>
              )}
            </div>
          </CardContent>
        </Card>

        {/* ASA Details (if minted) */}
        {asaInfo && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ExternalLink className="size-5" />
                Algorand Standard Asset
              </CardTitle>
              <CardDescription>ASA #{asaInfo.assetId}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                <DetailItem label="Asset ID" value={asaInfo.assetId} />
                {asaInfo.assetName && (
                  <DetailItem label="Asset Name" value={asaInfo.assetName} />
                )}
                {asaInfo.unitName && (
                  <DetailItem label="Unit Name" value={asaInfo.unitName} />
                )}
                <DetailItem
                  label="Total Supply"
                  value={String(asaInfo.total)}
                />
                <DetailItem label="Decimals" value={String(asaInfo.decimals)} />
                <DetailItem
                  label="Default Frozen"
                  value={asaInfo.defaultFrozen ? "Yes (Soulbound)" : "No"}
                />
                <DetailItem
                  label="Deleted"
                  value={asaInfo.isDeleted ? "Yes" : "No"}
                />
                {asaInfo.creatorAddress && (
                  <DetailItem label="Creator">
                    <span className="inline-block max-w-[200px] truncate font-mono text-xs">
                      {asaInfo.creatorAddress}
                    </span>
                  </DetailItem>
                )}
              </div>
            </CardContent>
          </Card>
        )}
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
