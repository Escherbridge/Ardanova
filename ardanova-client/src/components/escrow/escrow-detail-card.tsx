"use client";

import {
  Calendar,
  User,
  Hash,
  DollarSign,
  AlertTriangle,
  ArrowDownCircle,
  RotateCcw,
  Loader2,
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
  shareId?: string | null;
  amount: number;
  status: EscrowStatus;
  txHashFund?: string | null;
  txHashRelease?: string | null;
  txHashRefund?: string | null;
  createdAt: Date | string;
  fundedAt?: Date | string | null;
  releasedAt?: Date | string | null;
  refundedAt?: Date | string | null;
  disputedAt?: Date | string | null;
}

interface EscrowDetailCardProps {
  escrow: EscrowData;
  onRelease?: () => void;
  onDispute?: () => void;
  onRefund?: () => void;
  isFunder?: boolean;
  isLoading?: boolean;
  className?: string;
}

function InfoRow({
  icon,
  label,
  value,
  mono,
}: {
  icon: "amount" | "identifier" | "user" | "calendar";
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="flex items-start gap-3 py-2">
      <InfoIcon name={icon} />
      <div className="min-w-0 flex-1">
        <p className="text-muted-foreground text-xs">{label}</p>
        <p className={cn("truncate text-sm font-medium", mono && "font-mono")}>
          {value}
        </p>
      </div>
    </div>
  );
}

function InfoIcon({
  name,
}: {
  name: "amount" | "identifier" | "user" | "calendar";
}) {
  const className = "text-muted-foreground mt-0.5 h-4 w-4 shrink-0";
  switch (name) {
    case "amount":
      return <DollarSign className={className} aria-hidden="true" />;
    case "identifier":
      return <Hash className={className} aria-hidden="true" />;
    case "user":
      return <User className={className} aria-hidden="true" />;
    case "calendar":
      return <Calendar className={className} aria-hidden="true" />;
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

function TransactionReference({ hash }: { hash: string | null | undefined }) {
  if (!hash) return <span className="text-muted-foreground">Not recorded</span>;
  return (
    <code
      className="text-foreground block font-mono text-xs break-all"
      aria-label={`Transaction reference ${hash}`}
    >
      {hash}
    </code>
  );
}

export function EscrowDetailCard({
  escrow,
  onRelease,
  onDispute,
  onRefund,
  isFunder = false,
  isLoading = false,
  className,
}: EscrowDetailCardProps) {
  const canRelease = isFunder && escrow.status === "FUNDED";
  const canDispute = isFunder && escrow.status === "FUNDED";
  const canRefund =
    isFunder && (escrow.status === "FUNDED" || escrow.status === "DISPUTED");

  return (
    <Card className={cn("border-2 border-white/10", className)}>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base">Escrow Details</CardTitle>
          <EscrowStatusBadge status={escrow.status} />
        </div>
      </CardHeader>

      <CardContent className="space-y-1 divide-y divide-white/5">
        <InfoRow
          icon="amount"
          label="Amount"
          value={
            <span className="text-neon-green font-mono text-lg">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </span>
          }
        />
        <InfoRow icon="identifier" label="Escrow ID" value={escrow.id} mono />
        <InfoRow icon="identifier" label="Task ID" value={escrow.taskId} mono />
        <InfoRow icon="user" label="Funder ID" value={escrow.funderId} mono />
        {escrow.shareId && (
          <InfoRow
            icon="identifier"
            label="Share ID"
            value={escrow.shareId}
            mono
          />
        )}
        <InfoRow
          icon="calendar"
          label="Created"
          value={formatDate(escrow.createdAt)}
        />
        {escrow.releasedAt && (
          <InfoRow
            icon="calendar"
            label="Release authorization recorded"
            value={formatDate(escrow.releasedAt)}
          />
        )}
        {escrow.disputedAt && (
          <InfoRow
            icon="calendar"
            label="Disputed"
            value={
              <span className="text-neon-pink">
                {formatDate(escrow.disputedAt)}
              </span>
            }
          />
        )}
        <div className="pt-2">
          <p className="text-muted-foreground mb-1 text-xs">
            Funding transaction reference
          </p>
          <TransactionReference hash={escrow.txHashFund} />
        </div>
        {escrow.txHashRelease && (
          <div className="pt-2">
            <p className="text-muted-foreground mb-1 text-xs">
              Release authorization transaction reference
            </p>
            <TransactionReference hash={escrow.txHashRelease} />
          </div>
        )}
      </CardContent>

      {(canRelease || canDispute || canRefund) && (
        <CardFooter className="flex flex-wrap gap-2 border-t border-white/10 pt-4">
          {canRelease && (
            <p className="text-muted-foreground w-full text-xs">
              Authorizing release records approval to move funds. Confirm the
              resulting transaction and contributor settlement separately.
            </p>
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
              Authorize Release
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
              Raise Dispute
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
              Request Refund
            </Button>
          )}
        </CardFooter>
      )}
    </Card>
  );
}
