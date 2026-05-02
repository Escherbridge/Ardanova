"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { api } from "~/trpc/react";
import type { CredentialChainDataResponse } from "~/lib/api/ardanova/endpoints/credential-utility";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { CredentialBadge, statusConfig } from "~/components/credentials/credential-badge";
import { TierProgress } from "~/components/credentials/tier-progress";
import {
  ArrowLeft,
  ExternalLink,
  Loader2,
  Shield,
  Link2,
  CheckCircle2,
  XCircle,
  Clock,
} from "lucide-react";

export default function CredentialDetailPage() {
  const params = useParams();
  const id = params.id as string;

  const { data: chainData, isLoading } = api.credentialUtility.getChainData.useQuery({ id });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!chainData) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <Shield className="size-12 text-muted-foreground mb-4" />
        <h1 className="text-2xl font-bold text-foreground">Credential not found</h1>
        <p className="text-muted-foreground mt-2">
          This credential may have been revoked or doesn't exist.
        </p>
        <Link href="/dashboard/profile">
          <Button variant="outline" className="mt-4">
            <ArrowLeft className="size-4 mr-2" />
            Back to Profile
          </Button>
        </Link>
      </div>
    );
  }

  const { credential, asaInfo, isOnChain, chainVerified } = chainData as CredentialChainDataResponse;
  const scope = credential.projectId ? "PROJECT" : "GUILD";
  const statusCfg = statusConfig[credential.status as keyof typeof statusConfig];

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-3xl mx-auto p-6">
        {/* Back button */}
        <Link href="/dashboard/profile" className="inline-flex items-center gap-2 text-muted-foreground hover:text-foreground mb-6 transition-colors">
          <ArrowLeft className="size-4" />
          Back to Profile
        </Link>

        {/* Credential Header */}
        <Card className="mb-6" variant={credential.status === "ACTIVE" ? "neon" : "default"}>
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
              {scope} credential {credential.status === "ACTIVE" ? "granted" : "revoked"} via {credential.grantedVia.replace(/_/g, " ").toLowerCase()}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Tier Progress */}
            {credential.tier && (
              <TierProgress currentTier={credential.tier} />
            )}

            {/* Details grid */}
            <div className="grid grid-cols-2 gap-4">
              <DetailItem label="Scope" value={scope} />
              <DetailItem label="Status">
                <Badge variant={statusCfg?.variant ?? "outline"}>
                  {credential.status}
                </Badge>
              </DetailItem>
              <DetailItem label="Grant Method" value={credential.grantedVia.replace(/_/g, " ")} />
              <DetailItem
                label="Granted"
                value={new Date(credential.createdAt).toLocaleDateString()}
              />
              {credential.revokedAt && (
                <DetailItem
                  label="Revoked"
                  value={new Date(credential.revokedAt).toLocaleDateString()}
                />
              )}
              <DetailItem label="Transferable" value={credential.isTransferable ? "Yes" : "No (Soulbound)"} />
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
            <div className="flex items-center gap-3 p-3 border border-border rounded">
              {isOnChain ? (
                <CheckCircle2 className="size-5 text-neon-green" />
              ) : (
                <Clock className="size-5 text-neon-yellow" />
              )}
              <div>
                <div className="font-medium">
                  {isOnChain ? "Minted on Algorand" : "Pending mint"}
                </div>
                <div className="text-xs text-muted-foreground">
                  {isOnChain
                    ? "This credential is verified on the Algorand blockchain"
                    : "This credential has not yet been minted on-chain"}
                </div>
              </div>
            </div>

            {chainVerified && (
              <div className="flex items-center gap-3 p-3 border border-neon-green/30 rounded bg-neon-green/5">
                <CheckCircle2 className="size-5 text-neon-green" />
                <div>
                  <div className="font-medium text-neon-green">Chain Verified</div>
                  <div className="text-xs text-muted-foreground">
                    On-chain data matches platform records
                  </div>
                </div>
              </div>
            )}

            {/* Mint/Revoke transaction hashes */}
            <div className="grid grid-cols-2 gap-4">
              {credential.mintTxHash && (
                <DetailItem label="Mint Transaction">
                  <span className="font-mono text-xs truncate max-w-[200px] inline-block">
                    {credential.mintTxHash}
                  </span>
                </DetailItem>
              )}
              {credential.revokeTxHash && (
                <DetailItem label="Revoke Transaction">
                  <span className="font-mono text-xs truncate max-w-[200px] inline-block">
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
              <CardDescription>
                ASA #{asaInfo.assetId}
              </CardDescription>
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
                <DetailItem label="Total Supply" value={String(asaInfo.total)} />
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
                    <span className="font-mono text-xs truncate max-w-[200px] inline-block">
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
      <div className="text-xs font-medium text-muted-foreground mb-1">{label}</div>
      {children ?? <div className="text-sm font-medium">{value}</div>}
    </div>
  );
}
