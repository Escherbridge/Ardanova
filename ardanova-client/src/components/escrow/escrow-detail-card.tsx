"use client";

import {
  Calendar,
  User,
  Hash,
  DollarSign,
  ExternalLink,
  AlertTriangle,
  ArrowDownCircle,
  RotateCcw,
  Loader2,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardFooter } from "~/components/ui/card";
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
  createdAt: Date | string;
  releasedAt?: Date | string | null;
  disputedAt?: Date | string | null;
}

interface EscrowDetailCardProps {
  escrow: EscrowData;
  onRelease?: () => void;
  onDispute?: () => void;
  onRefund?: () => void;
  isOwner?: boolean;
  isFunder?: boolean;
  isLoading?: boolean;
  className?: string;
}

function InfoRow({
  icon: Icon,
  label,
  value,
  mono,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="flex items-start gap-3 py-2">
      <Icon className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
      <div className="min-w-0 flex-1">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className={cn("truncate text-sm font-medium", mono && "font-mono")}>{value}</p>
      </div>
    </div>
  );
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

function TxHashLink({ hash }: { hash: string | null | undefined }) {
  if (!hash) return <span className="text-muted-foreground">—</span>;
  return (
    <a
      href={`https://etherscan.io/tx/${hash}`}
      target="_blank"
      rel="noopener noreferrer"
      className="inline-flex items-center gap-1 font-mono text-neon-cyan hover:underline"
    >
      {hash.slice(0, 10)}...{hash.slice(-6)}
      <ExternalLink className="h-3 w-3" />
    </a>
  );
}

export function EscrowDetailCard({
  escrow,
  onRelease,
  onDispute,
  onRefund,
  isOwner = false,
  isFunder = false,
  isLoading = false,
  className,
}: EscrowDetailCardProps) {
  const canRelease =
    (isOwner || isFunder) &&
    (escrow.status === "FUNDED" || escrow.status === "PARTIALLY_RELEASED");
  const canDispute =
    (isOwner || isFunder) &&
    (escrow.status === "FUNDED" || escrow.status === "PARTIALLY_RELEASED");
  const canRefund =
    isFunder &&
    (escrow.status === "FUNDED" || escrow.status === "DISPUTED");

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
          icon={DollarSign}
          label="Amount"
          value={
            <span className="font-mono text-lg text-neon-green">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </span>
          }
        />
        <InfoRow
          icon={Hash}
          label="Escrow ID"
          value={escrow.id}
          mono
        />
        <InfoRow
          icon={Hash}
          label="Task ID"
          value={escrow.taskId}
          mono
        />
        <InfoRow
          icon={User}
          label="Funder ID"
          value={escrow.funderId}
          mono
        />
        {escrow.shareId && (
          <InfoRow
            icon={Hash}
            label="Share ID"
            value={escrow.shareId}
            mono
          />
        )}
        <InfoRow
          icon={Calendar}
          label="Created"
          value={formatDate(escrow.createdAt)}
        />
        {escrow.releasedAt && (
          <InfoRow
            icon={Calendar}
            label="Released"
            value={formatDate(escrow.releasedAt)}
          />
        )}
        {escrow.disputedAt && (
          <InfoRow
            icon={Calendar}
            label="Disputed"
            value={
              <span className="text-neon-pink">{formatDate(escrow.disputedAt)}</span>
            }
          />
        )}
        <div className="pt-2">
          <p className="mb-1 text-xs text-muted-foreground">Fund TX</p>
          <TxHashLink hash={escrow.txHashFund} />
        </div>
        {escrow.txHashRelease && (
          <div className="pt-2">
            <p className="mb-1 text-xs text-muted-foreground">Release TX</p>
            <TxHashLink hash={escrow.txHashRelease} />
          </div>
        )}
      </CardContent>

      {(canRelease || canDispute || canRefund) && (
        <CardFooter className="flex flex-wrap gap-2 border-t border-white/10 pt-4">
          {canRelease && onRelease && (
            <Button
              variant="neon-green"
              size="sm"
              onClick={onRelease}
              disabled={isLoading}
              className="flex items-center gap-2"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <ArrowDownCircle className="h-4 w-4" />
              )}
              Release Funds
            </Button>
          )}
          {canDispute && onDispute && (
            <Button
              variant="destructive"
              size="sm"
              onClick={onDispute}
              disabled={isLoading}
              className="flex items-center gap-2"
            >
              <AlertTriangle className="h-4 w-4" />
              Raise Dispute
            </Button>
          )}
          {canRefund && onRefund && (
            <Button
              variant="outline"
              size="sm"
              onClick={onRefund}
              disabled={isLoading}
              className="flex items-center gap-2"
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
