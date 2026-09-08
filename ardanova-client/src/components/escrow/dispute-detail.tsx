"use client";

import { useState } from "react";
import { AlertTriangle, Loader2, RotateCcw, Undo2 } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { EscrowStatusBadge } from "./escrow-status-badge";
import type { EscrowData } from "./escrow-detail-card";
import { api } from "~/trpc/react";

interface DisputeDetailProps {
  escrow: EscrowData;
  /** Called after the administrator's decision is recorded, so a parent list can refresh. */
  onResolved?: () => void;
}

export function DisputeDetail({ escrow, onResolved }: DisputeDetailProps) {
  const [error, setError] = useState<string | null>(null);
  const disputedDate = escrow.disputedAt
    ? new Date(escrow.disputedAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "Not exposed by the current API";

  const utils = api.useUtils();
  const resolveMutation = api.taskEscrow.resolve.useMutation({
    onSuccess: () => {
      setError(null);
      void utils.taskEscrow.listDisputed.invalidate();
      onResolved?.();
    },
    onError: (err) => {
      setError(err.message ?? "Failed to resolve dispute. Please try again.");
    },
  });

  const resolve = (outcome: "REFUND" | "REJECT") => {
    setError(null);
    resolveMutation.mutate({ id: escrow.id, outcome });
  };

  const canResolve = escrow.status === "DISPUTED";

  return (
    <Card className="border-neon-pink/20 border-2">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <AlertTriangle className="text-neon-pink h-5 w-5" />
            Active Dispute
          </CardTitle>
          <EscrowStatusBadge status={escrow.status} />
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid gap-3 rounded-md border border-white/10 bg-white/5 p-3 text-sm sm:grid-cols-2">
          <div>
            <p className="text-muted-foreground text-xs">Escrow ID</p>
            <p className="font-mono text-xs break-all">
              {escrow.id.slice(0, 16)}...
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Amount</p>
            <p className="text-neon-pink font-mono font-bold">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Task</p>
            <p className="font-mono text-xs break-all">
              {escrow.taskId.slice(0, 16)}...
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Disputed at</p>
            <p className="text-xs">{disputedDate}</p>
          </div>
        </div>

        {canResolve ? (
          <div className="space-y-3 rounded-md border-2 border-white/10 bg-white/5 p-4">
            <p className="font-medium">Resolve this dispute</p>
            <p className="text-muted-foreground text-sm">
              Refund returns the held units to the pool they were reserved
              from. Reject dismisses the dispute and restores the escrow to
              funded, so the funder&apos;s own release action pays the
              contributor next.
            </p>
            <div className="flex flex-wrap gap-2 pt-1">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => resolve("REFUND")}
                disabled={resolveMutation.isPending}
                className="flex min-h-11 items-center gap-2"
              >
                {resolveMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RotateCcw className="h-4 w-4" />
                )}
                Refund funder
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => resolve("REJECT")}
                disabled={resolveMutation.isPending}
                className="flex min-h-11 items-center gap-2"
              >
                {resolveMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Undo2 className="h-4 w-4" />
                )}
                Reject dispute
              </Button>
            </div>
            {error && (
              <p className="text-destructive text-sm" role="alert">
                {error}
              </p>
            )}
          </div>
        ) : (
          <p className="text-muted-foreground text-sm">
            This dispute has already been resolved; the escrow is now{" "}
            {escrow.status}.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
