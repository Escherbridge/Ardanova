"use client";

import { useState } from "react";
import {
  AlertTriangle,
  ArrowDownCircle,
  RotateCcw,
  GitMerge,
  Loader2,
  CheckCircle2,
  AlertCircle,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { EscrowStatusBadge } from "./escrow-status-badge";
import type { EscrowData } from "./escrow-detail-card";
import { api } from "~/trpc/react";
import { cn } from "~/lib/utils";

type ResolutionAction = "release" | "refund" | null;

interface DisputeDetailProps {
  escrow: EscrowData;
  onResolveRelease: () => void;
  onResolveRefund: () => void;
}

function ConfirmActionPanel({
  action,
  amount,
  onConfirm,
  onCancel,
  isPending,
  error,
}: {
  action: "release" | "refund";
  amount: number;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
  error: string | null;
}) {
  const isRelease = action === "release";

  return (
    <div
      className={cn(
        "rounded-md border-2 p-4 space-y-3",
        isRelease
          ? "border-neon-green/30 bg-neon-green/5"
          : "border-neon-yellow/30 bg-neon-yellow/5"
      )}
    >
      <div className="flex items-center gap-2">
        {isRelease ? (
          <ArrowDownCircle className="h-5 w-5 text-neon-green" />
        ) : (
          <RotateCcw className="h-5 w-5 text-neon-yellow" />
        )}
        <p className="font-medium">
          {isRelease
            ? "Confirm: Release to Contributor"
            : "Confirm: Return to Funder"}
        </p>
      </div>
      <p className="text-sm text-muted-foreground">
        {isRelease
          ? `Funds (${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 6 })}) will be released to the contributor. This cannot be undone.`
          : `Funds (${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 6 })}) will be returned to the original funder. This cannot be undone.`}
      </p>

      {error && (
        <div className="flex items-start gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <div className="flex gap-2">
        <Button
          size="sm"
          variant={isRelease ? "neon-green" : "outline"}
          onClick={onConfirm}
          disabled={isPending}
          className="flex items-center gap-2"
        >
          {isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <CheckCircle2 className="h-4 w-4" />
          )}
          Yes, Confirm
        </Button>
        <Button
          size="sm"
          variant="ghost"
          onClick={onCancel}
          disabled={isPending}
        >
          Cancel
        </Button>
      </div>
    </div>
  );
}

export function DisputeDetail({
  escrow,
  onResolveRelease,
  onResolveRefund,
}: DisputeDetailProps) {
  const [pendingAction, setPendingAction] = useState<ResolutionAction>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const utils = api.useUtils();

  const releaseMutation = api.taskEscrow.release.useMutation({
    onSuccess: () => {
      void utils.taskEscrow.getByTaskId.invalidate();
      void utils.taskEscrow.getByFunderId.invalidate();
      setPendingAction(null);
      setActionError(null);
      onResolveRelease();
    },
    onError: (err) => {
      setActionError(err.message ?? "Release failed. Please try again.");
    },
  });

  const refundMutation = api.taskEscrow.refund.useMutation({
    onSuccess: () => {
      void utils.taskEscrow.getByTaskId.invalidate();
      void utils.taskEscrow.getByFunderId.invalidate();
      setPendingAction(null);
      setActionError(null);
      onResolveRefund();
    },
    onError: (err) => {
      setActionError(err.message ?? "Refund failed. Please try again.");
    },
  });

  const isMutating = releaseMutation.isPending || refundMutation.isPending;

  const handleConfirm = () => {
    setActionError(null);
    if (pendingAction === "release") {
      releaseMutation.mutate({ id: escrow.id });
    } else if (pendingAction === "refund") {
      refundMutation.mutate({ id: escrow.id });
    }
  };

  const handleCancel = () => {
    if (!isMutating) {
      setPendingAction(null);
      setActionError(null);
    }
  };

  const disputedDate = escrow.disputedAt
    ? new Date(escrow.disputedAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "Unknown";

  return (
    <Card className="border-2 border-neon-pink/20">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2 text-base">
            <AlertTriangle className="h-5 w-5 text-neon-pink" />
            Active Dispute
          </CardTitle>
          <EscrowStatusBadge status={escrow.status} />
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-3 rounded-md border border-white/10 bg-white/5 p-3 text-sm">
          <div>
            <p className="text-xs text-muted-foreground">Escrow ID</p>
            <p className="font-mono text-xs">{escrow.id.slice(0, 16)}...</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Amount</p>
            <p className="font-mono font-bold text-neon-pink">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Task</p>
            <p className="font-mono text-xs">{escrow.taskId.slice(0, 16)}...</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Disputed At</p>
            <p className="text-xs">{disputedDate}</p>
          </div>
        </div>

        {pendingAction ? (
          <ConfirmActionPanel
            action={pendingAction}
            amount={escrow.amount}
            onConfirm={handleConfirm}
            onCancel={handleCancel}
            isPending={isMutating}
            error={actionError}
          />
        ) : (
          <div className="space-y-3">
            <p className="text-xs font-medium uppercase tracking-widest text-muted-foreground">
              Resolution Actions
            </p>
            <div className="grid gap-2">
              <Button
                variant="neon-green"
                size="sm"
                onClick={() => setPendingAction("release")}
                disabled={isMutating || escrow.status !== "DISPUTED"}
                className="flex items-center justify-start gap-2"
              >
                <ArrowDownCircle className="h-4 w-4" />
                Release to Contributor
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPendingAction("refund")}
                disabled={isMutating || escrow.status !== "DISPUTED"}
                className="flex items-center justify-start gap-2"
              >
                <RotateCcw className="h-4 w-4" />
                Return to Funder
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled
                className="flex cursor-not-allowed items-center justify-start gap-2 opacity-50"
                title="Split resolution not yet supported via API"
              >
                <GitMerge className="h-4 w-4" />
                Split 50/50{" "}
                <span className="ml-auto text-xs text-muted-foreground">
                  (requires API support)
                </span>
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
