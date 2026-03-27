"use client";

import { Shield, ShieldCheck, ShieldAlert, Clock, AlertTriangle } from "lucide-react";
import Link from "next/link";
import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { cn } from "~/lib/utils";

interface KycStatusBannerProps {
  compact?: boolean;
  className?: string;
}

export function KycStatusBanner({ compact = false, className }: KycStatusBannerProps) {
  const { data: status, isLoading } = api.kyc.getMyStatus.useQuery();

  if (isLoading) {
    return null;
  }

  // No KYC submission yet
  if (!status) {
    if (compact) {
      return null;
    }

    return (
      <div
        className={cn(
          "flex items-center justify-between gap-4 border-2 border-cyan-500 bg-cyan-950/20 p-4",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <div className="flex items-center gap-3">
          <Shield className="h-5 w-5 text-cyan-500" />
          <div>
            <p className="font-bold text-cyan-500">Verify Your Identity</p>
            <p className="text-sm text-gray-400">
              Complete KYC verification to unlock full platform access
            </p>
          </div>
        </div>
        <Link href="/settings/verification">
          <Button variant="neon" size={compact ? "sm" : "default"}>
            Get Verified
          </Button>
        </Link>
      </div>
    );
  }

  // PENDING state
  if (status.status === "PENDING") {
    return (
      <div
        className={cn(
          "flex items-center gap-3 border-2 border-yellow-500 bg-yellow-950/20",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <Clock className={cn("text-yellow-500", compact ? "h-4 w-4" : "h-5 w-5")} />
        <div className="flex-1">
          <Badge variant="warning" className={cn(compact && "text-xs")}>
            PENDING
          </Badge>
          {!compact && (
            <p className="mt-1 text-sm text-gray-400">Verification pending review</p>
          )}
        </div>
      </div>
    );
  }

  // IN_REVIEW state
  if (status.status === "IN_REVIEW") {
    return (
      <div
        className={cn(
          "flex items-center gap-3 border-2 border-yellow-500 bg-yellow-950/20",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <Clock className={cn("text-yellow-500", compact ? "h-4 w-4" : "h-5 w-5")} />
        <div className="flex-1">
          <Badge variant="warning" className={cn(compact && "text-xs")}>
            IN REVIEW
          </Badge>
          {!compact && (
            <p className="mt-1 text-sm text-gray-400">Your verification is under review</p>
          )}
        </div>
      </div>
    );
  }

  // APPROVED state
  if (status.status === "APPROVED") {
    return (
      <div
        className={cn(
          "flex items-center gap-3 border-2 border-green-500 bg-green-950/20",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <ShieldCheck className={cn("text-green-500", compact ? "h-4 w-4" : "h-5 w-5")} />
        <div className="flex-1">
          <Badge variant="success" className={cn(compact && "text-xs")}>
            VERIFIED
          </Badge>
          {!compact && (
            <p className="mt-1 text-sm text-gray-400">Your identity is verified</p>
          )}
        </div>
      </div>
    );
  }

  // REJECTED state
  if (status.status === "REJECTED") {
    return (
      <div
        className={cn(
          "flex items-center justify-between gap-4 border-2 border-red-500 bg-red-950/20",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <div className="flex items-center gap-3">
          <ShieldAlert className={cn("text-red-500", compact ? "h-4 w-4" : "h-5 w-5")} />
          <div>
            <Badge variant="destructive" className={cn(compact && "text-xs")}>
              REJECTED
            </Badge>
            {!compact && status.rejectionReason && (
              <p className="mt-1 text-sm text-gray-400">{status.rejectionReason}</p>
            )}
          </div>
        </div>
        {!compact && (
          <Link href="/settings/verification">
            <Button variant="destructive" size="sm">
              Resubmit
            </Button>
          </Link>
        )}
      </div>
    );
  }

  // EXPIRED state
  if (status.status === "EXPIRED") {
    return (
      <div
        className={cn(
          "flex items-center justify-between gap-4 border-2 border-red-500 bg-red-950/20",
          compact ? "p-2" : "p-4",
          className
        )}
      >
        <div className="flex items-center gap-3">
          <AlertTriangle className={cn("text-red-500", compact ? "h-4 w-4" : "h-5 w-5")} />
          <div>
            <Badge variant="destructive" className={cn(compact && "text-xs")}>
              EXPIRED
            </Badge>
            {!compact && (
              <p className="mt-1 text-sm text-gray-400">
                Your verification has expired. Please verify again.
              </p>
            )}
          </div>
        </div>
        {!compact && (
          <Link href="/settings/verification">
            <Button variant="destructive" size="sm">
              Re-verify
            </Button>
          </Link>
        )}
      </div>
    );
  }

  return null;
}
