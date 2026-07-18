import { type VariantProps } from "class-variance-authority";
import { Shield, ShieldCheck, Crown, Star, Gem } from "lucide-react";
import { Badge, type badgeVariants } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

const CREDENTIAL_TIERS = [
  "BRONZE",
  "SILVER",
  "GOLD",
  "PLATINUM",
  "DIAMOND",
] as const;
const CREDENTIAL_STATUSES = ["ACTIVE", "SUSPENDED", "REVOKED"] as const;

type CredentialTier = (typeof CREDENTIAL_TIERS)[number];
type CredentialStatus = (typeof CREDENTIAL_STATUSES)[number];

function isCredentialTier(value: string): value is CredentialTier {
  return CREDENTIAL_TIERS.some((tier) => tier === value);
}

function isCredentialStatus(value: string): value is CredentialStatus {
  return CREDENTIAL_STATUSES.some((status) => status === value);
}

type BadgeVariant = NonNullable<VariantProps<typeof badgeVariants>["variant"]>;

interface CredentialBadgeProps {
  tier?: string | null;
  status?: string | null;
  size?: "sm" | "md" | "lg";
  showStatus?: boolean;
  className?: string;
}

const tierConfig: Record<
  CredentialTier,
  { variant: BadgeVariant; label: string; accent: string }
> = {
  BRONZE: {
    variant: "outline",
    label: "Bronze",
    accent: "border-amber-500 text-amber-500",
  },
  SILVER: {
    variant: "secondary",
    label: "Silver",
    accent: "border-slate-400 text-slate-400",
  },
  GOLD: {
    variant: "neon",
    label: "Gold",
    accent: "border-yellow-400 text-yellow-400",
  },
  PLATINUM: {
    variant: "neon-purple",
    label: "Platinum",
    accent: "border-purple-500 text-purple-500",
  },
  DIAMOND: {
    variant: "neon-pink",
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

export function normalizeCredentialTier(
  value: string | null | undefined,
): CredentialTier | undefined {
  const normalized = value?.toUpperCase();
  return normalized && isCredentialTier(normalized) ? normalized : undefined;
}

function normalizeCredentialStatus(
  value: string | null | undefined,
): CredentialStatus {
  const normalized = value?.toUpperCase();
  return normalized && isCredentialStatus(normalized) ? normalized : "ACTIVE";
}

export function CredentialTierIcon({
  tier,
  className,
}: {
  tier?: CredentialTier;
  className: string;
}) {
  switch (tier) {
    case "SILVER":
      return <ShieldCheck className={className} aria-hidden="true" />;
    case "GOLD":
      return <Crown className={className} aria-hidden="true" />;
    case "PLATINUM":
      return <Star className={className} aria-hidden="true" />;
    case "DIAMOND":
      return <Gem className={className} aria-hidden="true" />;
    case "BRONZE":
    default:
      return <Shield className={className} aria-hidden="true" />;
  }
}

export function CredentialBadge({
  tier,
  status = "ACTIVE",
  size = "md",
  showStatus = false,
  className,
}: CredentialBadgeProps) {
  const normalizedTier = normalizeCredentialTier(tier);
  const normalizedStatus = normalizeCredentialStatus(status);
  const config = normalizedTier ? tierConfig[normalizedTier] : null;
  const statusCfg = statusConfig[normalizedStatus] ?? statusConfig.ACTIVE;

  return (
    <div className={cn("inline-flex items-center gap-1", className)}>
      <Badge
        variant={config?.variant ?? "outline"}
        size={size === "lg" ? "lg" : size === "sm" ? "sm" : "default"}
        className={cn(config?.accent, sizeClasses[size])}
      >
        <CredentialTierIcon tier={normalizedTier} className={iconSizes[size]} />
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
