"use client";

import { Check, Clock, Loader2, X, XCircle } from "lucide-react";
import { cn } from "~/lib/utils";
import { Button } from "~/components/ui/button";

type PayoutStatus =
  | "PENDING"
  | "PROCESSING"
  | "COMPLETED"
  | "FAILED"
  | "CANCELLED";

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
  const currentStep = getStepIndex(status);

  return (
    <div
      className={cn(
        "border-border bg-card space-y-5 rounded-none border-2 p-5",
        status === "COMPLETED" && "border-success/50",
        isFailed && "border-destructive/50",
        isCancelled && "border-border",
        className,
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <p className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Payout Request
          </p>
          <p className="text-muted-foreground/60 mt-0.5 font-mono text-xs">
            #{payoutId.slice(0, 8).toUpperCase()}
          </p>
        </div>
        {usdAmount !== undefined && (
          <p className="text-foreground font-mono text-xl font-bold">
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
              <div
                key={step.key}
                className="flex flex-1 items-center last:flex-none"
              >
                {/* Step dot */}
                <div className="flex flex-col items-center gap-1.5">
                  <div
                    className={cn(
                      "flex h-8 w-8 items-center justify-center rounded-none border-2 transition-colors",
                      isDone && "border-success bg-success/20",
                      isActive &&
                        !isDone &&
                        "border-system bg-system/10 animate-pulse",
                      !isDone && !isActive && "border-border bg-muted/30",
                    )}
                  >
                    {isDone ? (
                      <Check className="text-success h-4 w-4" />
                    ) : isActive ? (
                      <Loader2 className="text-system h-4 w-4 animate-spin" />
                    ) : (
                      <Clock className="text-muted-foreground/40 h-4 w-4" />
                    )}
                  </div>
                  <span
                    className={cn(
                      "font-mono text-[10px] tracking-wide uppercase",
                      isDone && "text-success",
                      isActive && "text-system",
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
                      "mx-1 mb-5 h-0.5 flex-1 transition-colors",
                      currentStep > idx ? "bg-success/60" : "bg-border",
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
            <XCircle className="text-destructive h-5 w-5 shrink-0" />
          ) : (
            <X className="text-muted-foreground h-5 w-5 shrink-0" />
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
            <p className="text-muted-foreground mt-0.5 font-mono text-xs">
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
          className="border-border w-full font-mono text-xs tracking-wide uppercase"
        >
          {isCancelling ? (
            <>
              <Loader2 className="mr-2 h-3 w-3 animate-spin" />
              Cancelling...
            </>
          ) : (
            "Cancel Payout"
          )}
        </Button>
      )}

      {/* Completed message */}
      {status === "COMPLETED" && (
        <div className="border-success/30 bg-success/5 flex items-center gap-2 rounded-none border px-3 py-2">
          <Check className="text-success h-4 w-4 shrink-0" />
          <p className="text-success font-mono text-xs">
            Funds transferred to your account
          </p>
        </div>
      )}
    </div>
  );
}
