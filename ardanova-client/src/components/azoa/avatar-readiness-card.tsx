"use client";

import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Cpu, Wallet, AlertTriangle } from "lucide-react";
// Use a partial-safe shape that matches tRPC's inferred output (required booleans
// may come through as optional after serialisation).
interface AvatarStatusShape {
  avatarId?: string | null;
  walletAddress?: string | null;
  walletId?: string | null;
  avatarLinked?: boolean | null;
  walletBound?: boolean | null;
  tier2Ready?: boolean | null;
}

interface AvatarReadinessCardProps {
  avatarStatus: AvatarStatusShape | undefined;
  isKycApproved: boolean;
  ensureError?: string | null;
}

function truncate(str: string, chars = 12): string {
  if (str.length <= chars) return str;
  return `${str.slice(0, 6)}…${str.slice(-4)}`;
}

export function AvatarReadinessCard({
  avatarStatus,
  isKycApproved,
  ensureError,
}: AvatarReadinessCardProps) {
  const avatarLinked = avatarStatus?.avatarLinked === true;
  const walletBound = avatarStatus?.walletBound === true;
  const avatarId = avatarStatus?.avatarId;
  const walletAddress = avatarStatus?.walletAddress;

  return (
    <Card className="border-neon-cyan/20 bg-neon-cyan/5">
      <CardContent className="pt-6">
        <div className="space-y-4">
          {/* Section heading */}
          <div className="flex items-center gap-2">
            <Cpu className="h-4 w-4 text-neon-cyan" />
            <h3 className="text-sm font-black tracking-tight text-foreground uppercase">
              Blockchain Readiness
            </h3>
          </div>

          {/* Avatar row */}
          <div className="flex items-center justify-between gap-4 border-b border-border/40 pb-3">
            <div className="flex items-center gap-2 min-w-0">
              <Cpu className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Avatar</span>
              {avatarLinked && avatarId && (
                <code className="rounded-none bg-muted px-1.5 py-0.5 text-xs font-mono text-foreground truncate">
                  {truncate(avatarId)}
                </code>
              )}
            </div>
            {avatarLinked ? (
              <Badge variant="success" className="rounded-none shrink-0">
                Linked ✓
              </Badge>
            ) : (
              <Badge variant="secondary" className="rounded-none shrink-0">
                Provisioning…
              </Badge>
            )}
          </div>

          {/* Wallet row */}
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-2 min-w-0">
              <Wallet className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Wallet</span>
              {walletBound && walletAddress && (
                <code className="rounded-none bg-muted px-1.5 py-0.5 text-xs font-mono text-foreground truncate">
                  {truncate(walletAddress, 14)}
                </code>
              )}
            </div>
            {walletBound ? (
              <Badge variant="success" className="rounded-none shrink-0">
                Bound ✓
              </Badge>
            ) : (
              <Badge variant="secondary" className="rounded-none shrink-0 text-xs">
                On first reward
              </Badge>
            )}
          </div>

          {/* Explainer line */}
          <p className="text-xs text-muted-foreground leading-relaxed pt-1">
            {isKycApproved
              ? "KYC permits node value actions. Project gates, funded escrow, and settlement still control when an equity reward is available."
              : "Complete KYC before any value move. Project gates and escrow remain separate safeguards, and the node enforces KYC fail-closed."}
          </p>

          {/* Silent ensure error — subtle, non-fatal */}
          {ensureError && (
            <div className="flex items-start gap-2 rounded-none border border-neon-yellow/20 bg-neon-yellow/5 p-2">
              <AlertTriangle className="h-3.5 w-3.5 shrink-0 text-neon-yellow mt-0.5" />
              <p className="text-xs text-neon-yellow">
                Avatar link pending — will retry on next visit.
              </p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
