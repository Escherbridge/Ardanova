"use client";

import { useState } from "react";
import Link from "next/link";
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
import { api } from "~/trpc/react";
import { describeEscrowActionError } from "~/lib/commerce/escrow-release";

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
  const [error, setError] = useState<string | null>(null);
  const amountIsValid = Number.isFinite(amount) && amount > 0;

  const utils = api.useUtils();
  const releaseMutation = api.taskEscrow.release.useMutation({
    onSuccess: () => {
      void utils.taskEscrow.getByTaskId.invalidate();
      void utils.taskEscrow.getByFunderId.invalidate();
      setError(null);
      onConfirm();
      onOpenChange(false);
    },
    onError: (err) => {
      setError(err.message ?? "Failed to release escrow. Please try again.");
    },
  });

  const handleConfirm = () => {
    if (!amountIsValid) return;
    setError(null);
    releaseMutation.mutate({ id: escrowId });
  };

  const handleOpenChange = (nextOpen: boolean) => {
    if (!releaseMutation.isPending) {
      setError(null);
      onOpenChange(nextOpen);
    }
  };

  const errorPresentation = error ? describeEscrowActionError(error) : null;

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
            contributor. Release evidence is the shared node&apos;s own
            confirmed record, not anything you enter here. This action cannot
            be undone.
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

          {errorPresentation && (
            <div
              className="border-destructive/50 bg-destructive/10 flex items-start gap-2 border p-3"
              role="alert"
            >
              <AlertCircle
                className="text-destructive mt-0.5 h-4 w-4 shrink-0"
                aria-hidden="true"
              />
              <div className="space-y-2">
                <p className="text-destructive text-sm">
                  {errorPresentation.message}
                </p>
                {errorPresentation.requiresAccountSetup && (
                  <Button asChild size="sm" variant="outline">
                    <Link href="/settings/verification">Complete setup</Link>
                  </Button>
                )}
              </div>
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
            disabled={releaseMutation.isPending || !amountIsValid}
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
