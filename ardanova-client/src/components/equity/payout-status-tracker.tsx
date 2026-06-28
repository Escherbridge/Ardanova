"use client";

import { Check, Clock, Loader2, X, XCircle } from "lucide-react";
import { cn } from "~/lib/utils";
import { Button } from "~/components/ui/button";

type PayoutStatus = "PENDING" | "PROCESSING" | "COMPLETED" | "FAILED" | "CANCELLED";

interface PayoutStatusTrackerProps {
  status: PayoutStatus;
  payoutId: string;
  usdAmount?: number;
  onCancel?: () => void;
  isCancelling?: boolean;
  className?: string;
}

const STEPS: { key: PayoutStatus; label: string }[] = [
  { key: "PENDING", label: "Pending" },
  { key: "PROCESSING", label: "Processing" },
  { key: "COMPLETED", label: "Completed" },
];

function getStepIndex(status: PayoutStatus): number {
  if (status === "PENDING") return 0;
  if (status === "PROCESSING") return 1;
  if (status === "COMPLETED") return 2;
  return -1;
}

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function PayoutStatusTracker({
  status,
  payoutId,
  usdAmount,
  onCancel,
  isCancelling,
  className,
}: PayoutStatusTrackerProps) {
  const isFailed = status === "FAILED";
  const isCancelled = status === "CANCELLED";
  const isTerminal = isFailed || isCancelled || status === "COMPLETED";
  const currentStep = getStepIndex(status);

  return (
    <div
      className={cn(
        "rounded-none border-2 border-border bg-card p-5 space-y-5",
        status === "COMPLETED" && "border-neon-green/50",
        isFailed && "border-destructive/50",
        isCancelled && "border-border",
        className,
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Payout Request
          </p>
          <p className="font-mono text-xs text-muted-foreground/60 mt-0.5">
            #{payoutId.slice(0, 8).toUpperCase()}
          </p>
        </div>
        {usdAmount !== undefined && (
          <p className="font-mono text-xl font-bold text-foreground">
            {formatUsd(usdAmount)}
          </p>
        )}
      </div>

      {/* Stepper */}
      {!isFailed && !isCancelled ? (
        <div className="flex items-center gap-0">
          {STEPS.map((step, idx) => {
            const isDone = currentStep > idx;
            const isActive = currentStep === idx;

            return (
              <div key={step.key} className="flex items-center flex-1 last:flex-none">
                {/* Step dot */}
                <div className="flex flex-col items-center gap-1.5">
                  <div
                    className={cn(
                      "w-8 h-8 rounded-none border-2 flex items-center justify-center transition-colors",
                      isDone &&
                        "border-neon-green bg-neon-green/20",
                      isActive && !isDone &&
                        "border-neon-cyan bg-neon-cyan/10 animate-pulse",
                      !isDone && !isActive &&
                        "border-border bg-muted/30",
                    )}
                  >
                    {isDone ? (
                      <Check className="h-4 w-4 text-neon-green" />
                    ) : isActive ? (
                      <Loader2 className="h-4 w-4 text-neon-cyan animate-spin" />
                    ) : (
                      <Clock className="h-4 w-4 text-muted-foreground/40" />
                    )}
                  </div>
                  <span
                    className={cn(
                      "font-mono text-[10px] uppercase tracking-wide",
                      isDone && "text-neon-green",
                      isActive && "text-neon-cyan",
                      !isDone && !isActive && "text-muted-foreground/40",
                    )}
                  >
                    {step.label}
                  </span>
                </div>

                {/* Connector */}
                {idx < STEPS.length - 1 && (
                  <div
                    className={cn(
                      "flex-1 h-0.5 mx-1 mb-5 transition-colors",
                      currentStep > idx ? "bg-neon-green/60" : "bg-border",
                    )}
                  />
                )}
              </div>
            );
          })}
        </div>
      ) : (
        /* Failed / Cancelled state */
        <div
          className={cn(
            "flex items-center gap-3 rounded-none border-2 p-4",
            isFailed
              ? "border-destructive/40 bg-destructive/10"
              : "border-border bg-muted/20",
          )}
        >
          {isFailed ? (
            <XCircle className="h-5 w-5 text-destructive shrink-0" />
          ) : (
            <X className="h-5 w-5 text-muted-foreground shrink-0" />
          )}
          <div>
            <p
              className={cn(
                "font-mono text-sm font-semibold",
                isFailed ? "text-destructive" : "text-muted-foreground",
              )}
            >
              {isFailed ? "Payout Failed" : "Payout Cancelled"}
            </p>
            <p className="font-mono text-xs text-muted-foreground mt-0.5">
              {isFailed
                ? "Something went wrong. Contact support if this persists."
                : "This payout request has been cancelled."}
            </p>
          </div>
        </div>
      )}

      {/* Cancel action — only while PENDING */}
      {status === "PENDING" && onCancel && (
        <Button
          variant="outline"
          size="sm"
          onClick={onCancel}
          disabled={isCancelling}
          className="w-full border-border font-mono text-xs uppercase tracking-wide"
        >
          {isCancelling ? (
            <>
              <Loader2 className="h-3 w-3 mr-2 animate-spin" />
              Cancelling...
            </>
          ) : (
            "Cancel Payout"
          )}
        </Button>
      )}

      {/* Completed message */}
      {status === "COMPLETED" && (
        <div className="flex items-center gap-2 rounded-none border border-neon-green/30 bg-neon-green/5 px-3 py-2">
          <Check className="h-4 w-4 text-neon-green shrink-0" />
          <p className="font-mono text-xs text-neon-green">
            Funds transferred to your account
          </p>
        </div>
      )}
    </div>
  );
}
