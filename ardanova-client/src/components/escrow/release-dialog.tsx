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
import { cn } from "~/lib/utils";

interface ReleaseDialogProps {
  escrowId: string;
  amount: number;
  onConfirm: () => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ReleaseDialog({
  escrowId,
  amount,
  onConfirm,
  open,
  onOpenChange,
}: ReleaseDialogProps) {
  const [txHash, setTxHash] = useState("");
  const [error, setError] = useState<string | null>(null);

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
    setError(null);
    releaseMutation.mutate({
      id: escrowId,
      txHash: txHash.trim() || undefined,
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
      <DialogContent className="border-2 border-neon-green/30 sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ArrowDownCircle className="h-5 w-5 text-neon-green" />
            Release Escrow Funds
          </DialogTitle>
          <DialogDescription>
            This will release the escrowed funds to the contributor. This action
            cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="rounded-md border-2 border-neon-green/20 bg-neon-green/5 p-4">
            <p className="text-xs text-muted-foreground">Amount to Release</p>
            <p className="font-mono text-2xl font-bold text-neon-green">
              {amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </p>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">
              Transaction Hash{" "}
              <span className="text-muted-foreground">(optional)</span>
            </label>
            <Input
              placeholder="0x..."
              value={txHash}
              onChange={(e) => setTxHash(e.target.value)}
              className={cn(
                "font-mono text-sm",
                txHash && !/^0x[0-9a-fA-F]{64}$/.test(txHash) &&
                  "border-yellow-500/50"
              )}
              disabled={releaseMutation.isPending}
            />
            <p className="text-xs text-muted-foreground">
              Provide the on-chain transaction hash for this release.
            </p>
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
              <p className="text-sm text-destructive">{error}</p>
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
            disabled={releaseMutation.isPending}
            className="flex items-center gap-2"
          >
            {releaseMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <ArrowDownCircle className="h-4 w-4" />
            )}
            Confirm Release
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
