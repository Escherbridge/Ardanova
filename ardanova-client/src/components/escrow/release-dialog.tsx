"use client";

import { useState } from "react";
import { Loader2, ArrowDownCircle, AlertCircle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "~/components/ui/dialog";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { api } from "~/trpc/react";
import { validateTransactionReference } from "~/lib/commerce/escrow-release";
import { cn } from "~/lib/utils";

interface ReleaseDialogProps {
  escrowId: string;
  amount: number;
  shareId: string;
  onConfirm: () => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ReleaseDialog({
  escrowId,
  amount,
  shareId,
  onConfirm,
  open,
  onOpenChange,
}: ReleaseDialogProps) {
  const [txHash, setTxHash] = useState("");
  const [error, setError] = useState<string | null>(null);
  const transactionReference = validateTransactionReference(txHash);
  const amountIsValid = Number.isFinite(amount) && amount > 0;

  const utils = api.useUtils();
  const releaseMutation = api.taskEscrow.release.useMutation({
    onSuccess: () => {
      void utils.taskEscrow.getByTaskId.invalidate();
      void utils.taskEscrow.getByFunderId.invalidate();
      setTxHash("");
      setError(null);
      onConfirm();
      onOpenChange(false);
    },
    onError: (err) => {
      setError(err.message ?? "Failed to release escrow. Please try again.");
    },
  });

  const handleConfirm = () => {
    if (!transactionReference.isValid || !amountIsValid) return;
    setError(null);
    releaseMutation.mutate({
      id: escrowId,
      txHash: transactionReference.value,
    });
  };

  const handleOpenChange = (nextOpen: boolean) => {
    if (!releaseMutation.isPending) {
      setTxHash("");
      setError(null);
      onOpenChange(nextOpen);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="border-neon-green/30 border-2 sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ArrowDownCircle
              className="text-neon-green h-5 w-5"
              aria-hidden="true"
            />
            Authorize Escrow Release
          </DialogTitle>
          <DialogDescription>
            This authorizes release of the escrowed share units to the
            contributor. This action cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="border-neon-green/20 bg-neon-green/5 border-2 p-4">
            <p className="text-muted-foreground text-xs">
              Amount to release (share units)
            </p>
            <p className="text-neon-green font-mono text-2xl font-bold">
              {amountIsValid
                ? `${amount.toLocaleString("en-US", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 6,
                  })} share units`
                : "Amount unavailable"}
            </p>
            <p className="text-muted-foreground mt-2 text-xs break-all">
              Share reference: <span className="font-mono">{shareId}</span>
            </p>
            {!amountIsValid && (
              <p className="text-destructive mt-2 text-xs" role="alert">
                The escrow does not contain a valid positive amount. Release is
                blocked.
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label
              htmlFor="release-transaction-reference"
              className="text-sm font-medium"
            >
              Transaction reference{" "}
              <span className="text-muted-foreground">(optional)</span>
            </label>
            <Input
              id="release-transaction-reference"
              placeholder="Chain hash or operation ID"
              value={txHash}
              onChange={(e) => {
                setTxHash(e.target.value);
                setError(null);
              }}
              className={cn(
                "font-mono text-sm",
                !transactionReference.isValid && "border-destructive",
              )}
              disabled={releaseMutation.isPending}
              aria-invalid={!transactionReference.isValid}
              aria-describedby={
                transactionReference.isValid
                  ? "release-transaction-reference-help"
                  : "release-transaction-reference-help release-transaction-reference-error"
              }
            />
            <p
              id="release-transaction-reference-help"
              className="text-muted-foreground text-xs"
            >
              Provide the chain hash or operation ID recorded for this release.
            </p>
            {!transactionReference.isValid && (
              <p
                id="release-transaction-reference-error"
                className="text-destructive text-xs"
              >
                Invalid transaction reference. {transactionReference.error}
              </p>
            )}
          </div>

          {error && (
            <div
              className="border-destructive/50 bg-destructive/10 flex items-start gap-2 border p-3"
              role="alert"
            >
              <AlertCircle
                className="text-destructive mt-0.5 h-4 w-4 shrink-0"
                aria-hidden="true"
              />
              <p className="text-destructive text-sm">{error}</p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => handleOpenChange(false)}
            disabled={releaseMutation.isPending}
          >
            Cancel
          </Button>
          <Button
            variant="neon-green"
            onClick={handleConfirm}
            disabled={
              releaseMutation.isPending ||
              !transactionReference.isValid ||
              !amountIsValid
            }
            className="flex items-center gap-2"
          >
            {releaseMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <ArrowDownCircle className="h-4 w-4" aria-hidden="true" />
            )}
            Confirm Release
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
