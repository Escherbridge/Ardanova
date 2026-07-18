"use client";

import { Card, CardContent, CardFooter } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { CredentialBadge } from "./credential-badge";
import { Link2, Clock, XCircle, AlertCircle } from "lucide-react";
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

function MintStatusIndicator({
  credential,
}: {
  credential: MembershipCredential;
}) {
  if (credential.status === "REVOKED") {
    return (
      <div className="text-destructive flex items-center gap-1">
        <XCircle className="size-3" />
        <span className="text-[10px]">Revoked</span>
      </div>
    );
  }
  if (credential.mintTxHash) {
    return (
      <div className="text-neon-cyan flex items-center gap-1">
        <Link2 className="size-3" />
        <span className="text-[10px]">Mint reference recorded</span>
      </div>
    );
  }
  if (credential.status === "ACTIVE" && !credential.mintTxHash) {
    return (
      <div className="text-neon-yellow flex items-center gap-1">
        <Clock className="size-3" />
        <span className="text-[10px]">Mint pending</span>
      </div>
    );
  }
  return (
    <div className="text-muted-foreground flex items-center gap-1">
      <AlertCircle className="size-3" />
      <span className="text-[10px]">App record only</span>
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
  const grantLabel =
    grantMethodLabels[credential.grantedVia] ?? credential.grantedVia;

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
          ? `Open ${scopeName ?? scope.toLowerCase()} credential, ${credential.tier}, ${credential.status.toLowerCase()}`
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
        {/* Header: scope + status */}
        <div className="flex items-center justify-between">
          <Badge
            variant={scope === "PROJECT" ? "neon-pink" : "neon-purple"}
            size="sm"
          >
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
          <div className="truncate text-sm font-medium">{scopeName}</div>
        )}

        {/* Grant method + date */}
        <div className="text-muted-foreground flex items-center justify-between text-xs">
          <span>{grantLabel}</span>
          <span>
            {credential.createdAt
              ? new Date(credential.createdAt).toLocaleDateString()
              : "Date unavailable"}
          </span>
        </div>
      </CardContent>

      <CardFooter className="border-border border-t pt-2">
        <MintStatusIndicator credential={credential} />
        {credential.assetId && (
          <div className="text-muted-foreground ml-auto flex items-center gap-1 text-xs">
            <Link2 className="size-3" />
            <span className="max-w-[80px] truncate font-mono">
              ASA #{credential.assetId}
            </span>
          </div>
        )}
      </CardFooter>
    </Card>
  );
}

export type { CredentialCardProps };
