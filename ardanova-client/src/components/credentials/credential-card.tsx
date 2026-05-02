"use client";

import { Card, CardContent, CardFooter } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { CredentialBadge } from "./credential-badge";
import { ExternalLink, Link2, Clock, CheckCircle2, XCircle, AlertCircle } from "lucide-react";
import { cn } from "~/lib/utils";
import type { MembershipCredential } from "~/lib/api/ardanova/endpoints/membership-credentials";

interface CredentialCardProps {
  credential: MembershipCredential;
  scopeName?: string;
  onClick?: () => void;
  className?: string;
}

const grantMethodLabels: Record<string, string> = {
  FOUNDER: "Founder",
  DAO_VOTE: "DAO Vote",
  CONTRIBUTION_THRESHOLD: "Contribution",
  APPLICATION_APPROVED: "Application",
  GAME_SDK_THRESHOLD: "Game SDK",
};

function MintStatusIndicator({ credential }: { credential: MembershipCredential }) {
  if (credential.mintTxHash) {
    return (
      <div className="flex items-center gap-1 text-neon-green">
        <CheckCircle2 className="size-3" />
        <span className="text-[10px]">On-chain</span>
      </div>
    );
  }
  if (credential.status === "ACTIVE" && !credential.mintTxHash) {
    return (
      <div className="flex items-center gap-1 text-neon-yellow">
        <Clock className="size-3" />
        <span className="text-[10px]">Pending mint</span>
      </div>
    );
  }
  if (credential.status === "REVOKED") {
    return (
      <div className="flex items-center gap-1 text-destructive">
        <XCircle className="size-3" />
        <span className="text-[10px]">Revoked</span>
      </div>
    );
  }
  return (
    <div className="flex items-center gap-1 text-muted-foreground">
      <AlertCircle className="size-3" />
      <span className="text-[10px]">Off-chain</span>
    </div>
  );
}

export function CredentialCard({
  credential,
  scopeName,
  onClick,
  className,
}: CredentialCardProps) {
  const isActive = credential.status === "ACTIVE";
  const scope = credential.projectId ? "PROJECT" : "GUILD";
  const grantLabel = grantMethodLabels[credential.grantedVia] ?? credential.grantedVia;

  return (
    <Card
      variant={isActive ? "neon" : "ghost"}
      padding="sm"
      className={cn(
        onClick && "cursor-pointer hover:border-primary/50",
        !isActive && "opacity-60",
        className,
      )}
      onClick={onClick}
    >
      <CardContent className="space-y-3">
        {/* Header: scope + status */}
        <div className="flex items-center justify-between">
          <Badge variant={scope === "PROJECT" ? "neon-pink" : "neon-purple"} size="sm">
            {scope}
          </Badge>
          <CredentialBadge
            tier={credential.tier}
            status={credential.status}
            size="sm"
            showStatus
          />
        </div>

        {/* Scope name */}
        {scopeName && (
          <div className="font-medium text-sm truncate">{scopeName}</div>
        )}

        {/* Grant method + date */}
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>{grantLabel}</span>
          <span>{new Date(credential.createdAt).toLocaleDateString()}</span>
        </div>
      </CardContent>

      <CardFooter className="pt-2 border-t border-border">
        <MintStatusIndicator credential={credential} />
        {credential.assetId && (
          <div className="ml-auto flex items-center gap-1 text-xs text-muted-foreground">
            <Link2 className="size-3" />
            <span className="font-mono truncate max-w-[80px]">
              ASA #{credential.assetId}
            </span>
          </div>
        )}
      </CardFooter>
    </Card>
  );
}

export type { CredentialCardProps };
