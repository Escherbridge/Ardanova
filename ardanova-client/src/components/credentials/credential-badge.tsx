import { type VariantProps } from "class-variance-authority";
import { Shield, ShieldCheck, Crown, Star, Gem } from "lucide-react";
import { Badge, type badgeVariants } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

type CredentialTier = "BRONZE" | "SILVER" | "GOLD" | "PLATINUM" | "DIAMOND";
type CredentialStatus = "ACTIVE" | "SUSPENDED" | "REVOKED";

type BadgeVariant = NonNullable<VariantProps<typeof badgeVariants>["variant"]>;

interface CredentialBadgeProps {
  tier?: CredentialTier | string | null;
  status?: CredentialStatus | string;
  size?: "sm" | "md" | "lg";
  showStatus?: boolean;
  className?: string;
}

const tierConfig: Record<
  CredentialTier,
  { variant: BadgeVariant; icon: typeof Shield; label: string; accent: string }
> = {
  BRONZE: {
    variant: "outline",
    icon: Shield,
    label: "Bronze",
    accent: "border-amber-500 text-amber-500",
  },
  SILVER: {
    variant: "secondary",
    icon: ShieldCheck,
    label: "Silver",
    accent: "border-slate-400 text-slate-400",
  },
  GOLD: {
    variant: "neon",
    icon: Crown,
    label: "Gold",
    accent: "border-yellow-400 text-yellow-400",
  },
  PLATINUM: {
    variant: "neon-purple",
    icon: Star,
    label: "Platinum",
    accent: "border-purple-500 text-purple-500",
  },
  DIAMOND: {
    variant: "neon-pink",
    icon: Gem,
    label: "Diamond",
    accent: "border-pink-500 text-pink-500",
  },
};

const statusConfig: Record<
  CredentialStatus,
  { variant: BadgeVariant; label: string }
> = {
  ACTIVE: { variant: "success", label: "Active" },
  SUSPENDED: { variant: "warning", label: "Suspended" },
  REVOKED: { variant: "destructive", label: "Revoked" },
};

const sizeClasses = {
  sm: "text-[10px]",
  md: "text-xs",
  lg: "text-sm",
} as const;

const iconSizes = {
  sm: "size-3",
  md: "size-3.5",
  lg: "size-4",
} as const;

export function CredentialBadge({
  tier,
  status = "ACTIVE",
  size = "md",
  showStatus = false,
  className,
}: CredentialBadgeProps) {
  const normalizedTier = tier?.toUpperCase() as CredentialTier | undefined;
  const normalizedStatus = (status?.toUpperCase() ?? "ACTIVE") as CredentialStatus;
  const config = normalizedTier ? tierConfig[normalizedTier] : null;
  const statusCfg = statusConfig[normalizedStatus] ?? statusConfig.ACTIVE;
  const Icon = config?.icon ?? Shield;

  return (
    <div className={cn("inline-flex items-center gap-1", className)}>
      <Badge
        variant={config?.variant ?? "outline"}
        size={size === "lg" ? "lg" : size === "sm" ? "sm" : "default"}
        className={cn(config?.accent, sizeClasses[size])}
      >
        <Icon className={iconSizes[size]} />
        {config?.label ?? "No Tier"}
      </Badge>
      {showStatus && (
        <Badge
          variant={statusCfg.variant}
          size={size === "lg" ? "lg" : size === "sm" ? "sm" : "default"}
          className={sizeClasses[size]}
        >
          {statusCfg.label}
        </Badge>
      )}
    </div>
  );
}

export { tierConfig, statusConfig };
export type { CredentialTier, CredentialStatus, CredentialBadgeProps };
