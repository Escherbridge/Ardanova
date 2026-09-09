"use client";

import {
  Calendar,
  CircleDollarSign,
  AlertTriangle,
  ArrowDownCircle,
  RotateCcw,
  Loader2,
  ShieldCheck,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardFooter,
} from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { EscrowStatusBadge, type EscrowStatus } from "./escrow-status-badge";
import { cn } from "~/lib/utils";

export interface EscrowData {
  id: string;
  taskId: string;
  funderId: string;
  amount: number;
  status: EscrowStatus;
  createdAt: Date | string;
  fundedAt?: Date | string | null;
  releasedAt?: Date | string | null;
  refundedAt?: Date | string | null;
  disputedAt?: Date | string | null;
}

interface EscrowDetailCardProps {
  escrow: EscrowData;
  /** The work this money is set aside for, in the words the person reading already knows it by. */
  taskTitle?: string;
  onFund?: () => void;
  onRelease?: () => void;
  onDispute?: () => void;
  onRefund?: () => void;
  isFunder?: boolean;
  isLoading?: boolean;
  className?: string;
}

/**
 * What each state means to the person looking at it. Deliberately says what is true of the
 * money, not what the row says: NONE is a record with nothing behind it yet, and saying so is
 * the difference between an honest surface and a promise nobody kept.
 */
const STATUS_EXPLANATION: Record<EscrowStatus, string> = {
  NONE: "Nothing is set aside yet. This record exists, but the money behind it has not been confirmed.",
  FUNDED:
    "This money is set aside for this work and cannot be spent on anything else until it is released or returned.",
  RELEASED: "This money has been handed over to the person who did the work.",
  DISPUTED:
    "This money is frozen while the disagreement is reviewed. Nothing moves until it is resolved.",
  REFUNDED: "This money went back to where it came from. Nothing is set aside now.",
};

function DetailRow({
  icon,
  label,
  value,
}: {
  icon: "amount" | "calendar" | "shield";
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 py-2">
      <DetailIcon name={icon} />
      <div className="min-w-0 flex-1">
        <p className="text-muted-foreground text-xs">{label}</p>
        <div className="text-sm font-medium">{value}</div>
      </div>
    </div>
  );
}

function DetailIcon({ name }: { name: "amount" | "calendar" | "shield" }) {
  const className = "text-muted-foreground mt-0.5 h-4 w-4 shrink-0";
  switch (name) {
    case "amount":
      return <CircleDollarSign className={className} aria-hidden="true" />;
    case "calendar":
      return <Calendar className={className} aria-hidden="true" />;
    case "shield":
      return <ShieldCheck className={className} aria-hidden="true" />;
  }
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

/**
 * The funder's view of one task's escrow. It shows amounts, dates and what the state means —
 * never the record identifiers behind them, which are addresses to a machine and noise to the
 * person deciding whether to release someone's pay.
 */
export function EscrowDetailCard({
  escrow,
  taskTitle,
  onFund,
  onRelease,
  onDispute,
  onRefund,
  isFunder = false,
  isLoading = false,
  className,
}: EscrowDetailCardProps) {
  const canFund = isFunder && escrow.status === "NONE";
  const canRelease = isFunder && escrow.status === "FUNDED";
  const canDispute = isFunder && escrow.status === "FUNDED";
  const canRefund =
    isFunder && (escrow.status === "FUNDED" || escrow.status === "DISPUTED");

  return (
    <Card className={cn("border-2 border-white/10", className)}>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base">Money set aside</CardTitle>
          {/*
            The shared badge reads "NO ESCROW" for NONE, which is false once a record exists —
            what is true is that the money behind it is not confirmed yet. Say that instead.
          */}
          {escrow.status === "NONE" ? (
            <span className="text-muted-foreground border-border border px-2 py-0.5 text-xs font-medium tracking-wide uppercase">
              Not confirmed yet
            </span>
          ) : (
            <EscrowStatusBadge status={escrow.status} />
          )}
        </div>
      </CardHeader>

      <CardContent className="space-y-1 divide-y divide-white/5">
        <DetailRow
          icon="amount"
          label={taskTitle ? `Set aside for “${taskTitle}”` : "Set aside"}
          value={
            <span className="text-neon-green text-lg font-semibold">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}{" "}
              <span className="text-muted-foreground text-xs font-normal">
                units of this project&apos;s stake
              </span>
            </span>
          }
        />
        <DetailRow
          icon="shield"
          label="What this means right now"
          value={
            <span className="text-muted-foreground font-normal">
              {STATUS_EXPLANATION[escrow.status]}
            </span>
          }
        />
        <DetailRow
          icon="calendar"
          label="Record opened"
          value={formatDate(escrow.createdAt)}
        />
        {escrow.fundedAt && (
          <DetailRow
            icon="calendar"
            label="Money set aside"
            value={formatDate(escrow.fundedAt)}
          />
        )}
        {escrow.releasedAt && (
          <DetailRow
            icon="calendar"
            label="Handed over"
            value={formatDate(escrow.releasedAt)}
          />
        )}
        {escrow.refundedAt && (
          <DetailRow
            icon="calendar"
            label="Returned"
            value={formatDate(escrow.refundedAt)}
          />
        )}
        {escrow.disputedAt && (
          <DetailRow
            icon="calendar"
            label="Disputed"
            value={
              <span className="text-neon-pink">
                {formatDate(escrow.disputedAt)}
              </span>
            }
          />
        )}
      </CardContent>

      {(canFund || canRelease || canDispute || canRefund) && (
        <CardFooter className="flex flex-wrap gap-2 border-t border-white/10 pt-4">
          {canFund && (
            <p className="text-muted-foreground w-full text-xs">
              Confirming checks that the money for this work really is set aside
              before anyone is told it is.
            </p>
          )}
          {canRelease && (
            <p className="text-muted-foreground w-full text-xs">
              Releasing hands this money to the person who did the work. This
              cannot be undone.
            </p>
          )}
          {canFund && onFund && (
            <Button
              type="button"
              variant="neon"
              size="sm"
              onClick={onFund}
              disabled={isLoading}
              className="flex min-h-11 items-center gap-2"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <ShieldCheck className="h-4 w-4" />
              )}
              Confirm the money is set aside
            </Button>
          )}
          {canRelease && onRelease && (
            <Button
              type="button"
              variant="neon-green"
              size="sm"
              onClick={onRelease}
              disabled={isLoading}
              className="flex min-h-11 items-center gap-2"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <ArrowDownCircle className="h-4 w-4" />
              )}
              Release to the worker
            </Button>
          )}
          {canDispute && onDispute && (
            <Button
              type="button"
              variant="destructive"
              size="sm"
              onClick={onDispute}
              disabled={isLoading}
              className="flex min-h-11 items-center gap-2"
            >
              <AlertTriangle className="h-4 w-4" />
              Raise a dispute
            </Button>
          )}
          {canRefund && onRefund && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onRefund}
              disabled={isLoading}
              className="flex min-h-11 items-center gap-2"
            >
              <RotateCcw className="h-4 w-4" />
              Ask for it back
            </Button>
          )}
        </CardFooter>
      )}
    </Card>
  );
}
