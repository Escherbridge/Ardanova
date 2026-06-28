"use client";

import {
  Clock,
  Coins,
  GitMerge,
  AlertTriangle,
  CheckCircle,
  ArrowDownCircle,
} from "lucide-react";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

export type EscrowStatus =
  | "PENDING"
  | "FUNDED"
  | "PARTIALLY_RELEASED"
  | "DISPUTED"
  | "RESOLVED"
  | "RELEASED";

const STATUS_CONFIG: Record<
  EscrowStatus,
  {
    label: string;
    variant: Parameters<typeof Badge>[0]["variant"];
    icon: React.ComponentType<{ className?: string }>;
    className?: string;
  }
> = {
  PENDING: {
    label: "PENDING",
    variant: "warning",
    icon: Clock,
  },
  FUNDED: {
    label: "FUNDED",
    variant: "neon",
    icon: Coins,
  },
  PARTIALLY_RELEASED: {
    label: "PARTIAL",
    variant: "neon-green",
    icon: GitMerge,
  },
  DISPUTED: {
    label: "DISPUTED",
    variant: "neon-pink",
    icon: AlertTriangle,
    className: "animate-pulse",
  },
  RESOLVED: {
    label: "RESOLVED",
    variant: "success",
    icon: CheckCircle,
  },
  RELEASED: {
    label: "RELEASED",
    variant: "success",
    icon: ArrowDownCircle,
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
  const Icon = config.icon;

  return (
    <Badge
      variant={config.variant}
      size={size}
      className={cn(config.className, className)}
    >
      <Icon className="shrink-0" />
      {config.label}
    </Badge>
  );
}
