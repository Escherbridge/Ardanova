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
import type { TaskEscrowDto } from "~/lib/contracts/task-escrow-contract";

function DisputeCard({
  escrow,
  expanded,
  onToggle,
}: {
  escrow: TaskEscrowDto;
  expanded: boolean;
  onToggle: () => void;
}) {
  return (
    <Card
      className={cn(
        "border-2 transition-colors",
        expanded
          ? "border-neon-pink/40"
          : "border-white/10 hover:border-white/20",
      )}
    >
      {/* Summary row */}
      <CardHeader className="pb-3">
        <div className="flex items-start gap-4">
          {/* Alert icon */}
          <div className="border-neon-pink/30 bg-neon-pink/10 mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-none border-2">
            <AlertTriangle className="text-neon-pink h-4 w-4" />
          </div>

          {/* Info */}
          <div className="min-w-0 flex-1 space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-mono text-sm font-medium">
                Task{" "}
                <span className="text-muted-foreground">
                  {escrow.taskId.slice(0, 12)}...
                </span>
              </p>
              <EscrowStatusBadge status={escrow.status} size="sm" />
            </div>
            <div className="text-muted-foreground flex flex-wrap gap-x-4 gap-y-1 text-xs">
              <span>
                Funder:{" "}
                <span className="font-mono">
                  {escrow.funderId.slice(0, 12)}...
                </span>
              </span>
              <span>
                Amount:{" "}
                <span className="text-neon-pink font-mono font-bold">
                  {escrow.amount.toLocaleString("en-US", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 6,
                  })}
                </span>
              </span>
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
            <DisputeDetail escrow={escrow} />
            <EscrowTimeline
              escrow={{
                createdAt: escrow.createdAt,
                fundedAt: escrow.txHashFund ? escrow.createdAt : undefined,
                releasedAt: escrow.releasedAt,
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

  const {
    data: escrows,
    isLoading,
    error,
  } = api.taskEscrow.getByFunderId.useQuery(undefined, {
    enabled: !!session?.user?.id,
  });

  const disputedEscrows = (escrows ?? []).filter(
    (escrow) => escrow.status === "DISPUTED",
  );

  const toggleExpand = (id: string) => {
    setExpandedId((prev) => (prev === id ? null : id));
  };

  if (!session?.user) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="text-neon-green h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (session.user.role !== "ADMIN") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-white/60">
          You do not have permission to view this page.
        </p>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-5xl space-y-8 py-8">
      {/* Header */}
      <div className="space-y-2 border-b border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <Gavel className="text-neon-pink h-8 w-8" />
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              Dispute Review
            </h1>
            <p className="text-muted-foreground text-sm">
              Review active escrow disputes without moving funds.
            </p>
          </div>
          {!isLoading && (
            <Badge variant="neon-pink" size="lg" className="ml-auto shrink-0">
              {disputedEscrows.length} open
            </Badge>
          )}
        </div>
      </div>

      {/* Production note */}
      <div className="border-neon-cyan/20 bg-neon-cyan/5 flex items-start gap-3 rounded-md border p-4 text-sm">
        <Info className="text-neon-cyan mt-0.5 h-4 w-4 shrink-0" />
        <p className="text-neon-cyan/80">
          <span className="font-semibold">Read-only contract:</span> This page
          currently queries escrows by the signed-in administrator&apos;s funder
          ID. The .NET API needs authenticated{" "}
          <code className="font-mono text-xs">getAllDisputed</code> and
          auditable administrator-resolution endpoints before this can become a
          platform-wide adjudication workspace.
        </p>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="text-neon-pink h-8 w-8 animate-spin" />
        </div>
      )}

      {/* Error */}
      {error && !isLoading && (
        <Card className="border-destructive/30 border-2">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <AlertTriangle className="text-destructive mb-3 h-10 w-10" />
            <p className="text-destructive font-medium">
              Failed to load disputes
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              {error.message}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Empty state */}
      {!isLoading && !error && disputedEscrows.length === 0 && (
        <Card className="border-2 border-white/10">
          <CardContent className="flex flex-col items-center justify-center py-20">
            <Gavel className="text-muted-foreground/30 mb-4 h-16 w-16" />
            <p className="text-muted-foreground text-lg font-medium">
              No open disputes
            </p>
            <p className="text-muted-foreground/60 mt-1 text-sm">
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
