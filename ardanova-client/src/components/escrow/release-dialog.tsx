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
  /** Who is being paid, in the words the funder knows them by. Optional: an id is never shown. */
  recipientName?: string;
  onConfirm: () => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * The funder's last look before someone is paid. It states the amount, who it goes to and that
 * it is final — never a record identifier, and never anything for the funder to transcribe.
 */
export function ReleaseDialog({
  escrowId,
  amount,
  recipientName,
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
            Release this money
          </DialogTitle>
          <DialogDescription>
            {recipientName
              ? `This hands the money set aside for this work to ${recipientName}.`
              : "This hands the money set aside for this work to the person who did it."}{" "}
            You are not asked to confirm anything about how it moves — the
            platform records that itself. This cannot be undone.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="border-neon-green/20 bg-neon-green/5 border-2 p-4">
            <p className="text-muted-foreground text-xs">Amount to release</p>
            <p className="text-neon-green text-2xl font-bold">
              {amountIsValid
                ? amount.toLocaleString("en-US", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 6,
                  })
                : "Amount unavailable"}
              {amountIsValid && (
                <span className="text-muted-foreground ml-2 text-xs font-normal">
                  units of this project&apos;s stake
                </span>
              )}
            </p>
            {!amountIsValid && (
              <p className="text-destructive mt-2 text-xs" role="alert">
                This record does not hold a valid amount, so nothing can be
                released.
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
            Confirm release
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
