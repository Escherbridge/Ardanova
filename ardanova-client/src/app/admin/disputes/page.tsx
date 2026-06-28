"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import {
  AlertTriangle,
  Gavel,
  ChevronDown,
  ChevronUp,
  Loader2,
  Info,
} from "lucide-react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { EscrowStatusBadge } from "~/components/escrow/escrow-status-badge";
import { DisputeDetail } from "~/components/escrow/dispute-detail";
import { EscrowTimeline } from "~/components/escrow/escrow-timeline";
import { cn } from "~/lib/utils";

type EscrowStatus =
  | "PENDING"
  | "FUNDED"
  | "PARTIALLY_RELEASED"
  | "DISPUTED"
  | "RESOLVED"
  | "RELEASED";

interface EscrowRecord {
  id: string;
  taskId: string;
  funderId: string;
  shareId?: string | null;
  amount: number;
  status: EscrowStatus;
  txHashFund?: string | null;
  txHashRelease?: string | null;
  createdAt: Date | string;
  releasedAt?: Date | string | null;
  disputedAt?: Date | string | null;
}

function formatDate(date: Date | string | null | undefined): string {
  if (!date) return "—";
  return new Date(date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function DisputeCard({
  escrow,
  expanded,
  onToggle,
}: {
  escrow: EscrowRecord;
  expanded: boolean;
  onToggle: () => void;
}) {
  const [resolved, setResolved] = useState(false);

  if (resolved) return null;

  return (
    <Card
      className={cn(
        "border-2 transition-colors",
        expanded ? "border-neon-pink/40" : "border-white/10 hover:border-white/20"
      )}
    >
      {/* Summary row */}
      <CardHeader className="pb-3">
        <div className="flex items-start gap-4">
          {/* Alert icon */}
          <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full border-2 border-neon-pink/30 bg-neon-pink/10">
            <AlertTriangle className="h-4 w-4 text-neon-pink" />
          </div>

          {/* Info */}
          <div className="flex-1 min-w-0 space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-mono text-sm font-medium">
                Task{" "}
                <span className="text-muted-foreground">
                  {escrow.taskId.slice(0, 12)}...
                </span>
              </p>
              <EscrowStatusBadge status={escrow.status} size="sm" />
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
              <span>
                Funder:{" "}
                <span className="font-mono">{escrow.funderId.slice(0, 12)}...</span>
              </span>
              <span>
                Amount:{" "}
                <span className="font-mono font-bold text-neon-pink">
                  {escrow.amount.toLocaleString("en-US", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 6,
                  })}
                </span>
              </span>
              {escrow.disputedAt && (
                <span>Disputed: {formatDate(escrow.disputedAt)}</span>
              )}
            </div>
          </div>

          {/* Toggle */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onToggle}
            className="shrink-0 gap-1 text-xs"
          >
            {expanded ? (
              <>
                Collapse <ChevronUp className="h-3.5 w-3.5" />
              </>
            ) : (
              <>
                View Details <ChevronDown className="h-3.5 w-3.5" />
              </>
            )}
          </Button>
        </div>
      </CardHeader>

      {/* Expanded panel */}
      {expanded && (
        <CardContent className="space-y-6 border-t border-white/10 pt-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <DisputeDetail
              escrow={escrow}
              onResolveRelease={() => setResolved(true)}
              onResolveRefund={() => setResolved(true)}
            />
            <EscrowTimeline
              escrow={{
                createdAt: escrow.createdAt,
                fundedAt: escrow.txHashFund ? escrow.createdAt : undefined,
                releasedAt: escrow.releasedAt,
                disputedAt: escrow.disputedAt,
              }}
            />
          </div>
        </CardContent>
      )}
    </Card>
  );
}

export default function AdminDisputesPage() {
  const { data: session } = useSession();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const adminFunderId = session?.user?.id ?? "";

  const { data: escrows, isLoading, error } = api.taskEscrow.getByFunderId.useQuery(
    { funderId: adminFunderId },
    { enabled: !!adminFunderId }
  );

  const escrowList = (escrows as unknown as EscrowRecord[] | undefined) ?? [];
  const disputedEscrows = escrowList.filter((e) => e.status === "DISPUTED");

  const toggleExpand = (id: string) => {
    setExpandedId((prev) => (prev === id ? null : id));
  };

  if (!session?.user) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-neon-green" />
      </div>
    );
  }

  if (session.user.role !== "ADMIN") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-white/60">You do not have permission to view this page.</p>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-5xl space-y-8 py-8">
      {/* Header */}
      <div className="space-y-2 border-b border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <Gavel className="h-8 w-8 text-neon-pink" />
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              Dispute Resolution
            </h1>
            <p className="text-sm text-muted-foreground">
              Review and resolve active escrow disputes.
            </p>
          </div>
          {!isLoading && (
            <Badge
              variant="neon-pink"
              size="lg"
              className="ml-auto shrink-0"
            >
              {disputedEscrows.length} open
            </Badge>
          )}
        </div>
      </div>

      {/* Production note */}
      <div className="flex items-start gap-3 rounded-md border border-neon-cyan/20 bg-neon-cyan/5 p-4 text-sm">
        <Info className="mt-0.5 h-4 w-4 shrink-0 text-neon-cyan" />
        <p className="text-neon-cyan/80">
          <span className="font-semibold">Development note:</span> This page
          queries escrows by the current admin&apos;s funder ID as a demo. A
          production deployment requires a <code className="font-mono text-xs">getAllDisputed</code> endpoint on
          the .NET API to surface all disputed escrows across all funders.
        </p>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-neon-pink" />
        </div>
      )}

      {/* Error */}
      {error && !isLoading && (
        <Card className="border-2 border-destructive/30">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <AlertTriangle className="mb-3 h-10 w-10 text-destructive" />
            <p className="font-medium text-destructive">Failed to load disputes</p>
            <p className="mt-1 text-sm text-muted-foreground">{error.message}</p>
          </CardContent>
        </Card>
      )}

      {/* Empty state */}
      {!isLoading && !error && disputedEscrows.length === 0 && (
        <Card className="border-2 border-white/10">
          <CardContent className="flex flex-col items-center justify-center py-20">
            <Gavel className="mb-4 h-16 w-16 text-muted-foreground/30" />
            <p className="text-lg font-medium text-muted-foreground">
              No open disputes
            </p>
            <p className="mt-1 text-sm text-muted-foreground/60">
              All escrows are in good standing.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Dispute list */}
      {!isLoading && !error && disputedEscrows.length > 0 && (
        <div className="space-y-4">
          {disputedEscrows.map((escrow) => (
            <DisputeCard
              key={escrow.id}
              escrow={escrow}
              expanded={expandedId === escrow.id}
              onToggle={() => toggleExpand(escrow.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}
