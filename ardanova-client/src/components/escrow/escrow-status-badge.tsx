"use client";

import {
  Clock,
  Coins,
  AlertTriangle,
  ArrowDownCircle,
  RotateCcw,
} from "lucide-react";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

import type { TaskEscrowStatus } from "~/lib/contracts/task-escrow-contract";

export type EscrowStatus = TaskEscrowStatus;

const STATUS_CONFIG: Record<
  EscrowStatus,
  {
    label: string;
    variant: Parameters<typeof Badge>[0]["variant"];
    icon: "clock" | "coins" | "alert" | "release" | "refund";
    className?: string;
  }
> = {
  NONE: {
    label: "NO ESCROW",
    variant: "outline",
    icon: "clock",
  },
  FUNDED: {
    label: "FUNDED",
    variant: "neon",
    icon: "coins",
  },
  DISPUTED: {
    label: "DISPUTED",
    variant: "neon-pink",
    icon: "alert",
    className: "animate-pulse",
  },
  RELEASED: {
    label: "RELEASE AUTHORIZED",
    variant: "success",
    icon: "release",
  },
  REFUNDED: {
    label: "REFUND AUTHORIZED",
    variant: "warning",
    icon: "refund",
  },
};

interface EscrowStatusBadgeProps {
  status: EscrowStatus;
  size?: "default" | "sm" | "lg";
  className?: string;
}

export function EscrowStatusBadge({
  status,
  size = "default",
  className,
}: EscrowStatusBadgeProps) {
  const config = STATUS_CONFIG[status];

  return (
    <Badge
      variant={config.variant}
      size={size}
      className={cn(config.className, className)}
    >
      <StatusIcon name={config.icon} />
      {config.label}
    </Badge>
  );
}

function StatusIcon({
  name,
}: {
  name: "clock" | "coins" | "alert" | "release" | "refund";
}) {
  const className = "shrink-0";
  switch (name) {
    case "clock":
      return <Clock className={className} aria-hidden="true" />;
    case "coins":
      return <Coins className={className} aria-hidden="true" />;
    case "alert":
      return <AlertTriangle className={className} aria-hidden="true" />;
    case "release":
      return <ArrowDownCircle className={className} aria-hidden="true" />;
    case "refund":
      return <RotateCcw className={className} aria-hidden="true" />;
  }
}
