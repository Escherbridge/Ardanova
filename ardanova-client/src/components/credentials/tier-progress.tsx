"use client";

import { Progress } from "~/components/ui/progress";
import { Badge } from "~/components/ui/badge";
import { tierConfig, type CredentialTier } from "./credential-badge";
import { cn } from "~/lib/utils";

const TIER_ORDER: CredentialTier[] = ["BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND"];

interface TierProgressProps {
  currentTier?: CredentialTier | string | null;
  className?: string;
}

export function TierProgress({ currentTier, className }: TierProgressProps) {
  const normalizedTier = currentTier?.toUpperCase() as CredentialTier | undefined;
  const currentIndex = normalizedTier ? TIER_ORDER.indexOf(normalizedTier) : -1;
  const progressValue = currentIndex >= 0 ? ((currentIndex + 1) / TIER_ORDER.length) * 100 : 0;

  return (
    <div className={cn("space-y-3", className)}>
      <Progress value={progressValue} variant="neon" />
      <div className="flex justify-between">
        {TIER_ORDER.map((tier, index) => {
          const config = tierConfig[tier];
          const Icon = config.icon;
          const isActive = index <= currentIndex;
          const isCurrent = index === currentIndex;

          return (
            <div
              key={tier}
              className={cn(
                "flex flex-col items-center gap-1",
                isActive ? "opacity-100" : "opacity-30",
              )}
            >
              <div
                className={cn(
                  "size-6 flex items-center justify-center border-2 transition-all",
                  isCurrent && "ring-2 ring-offset-2 ring-offset-background",
                  isCurrent ? config.accent : "border-border",
                )}
              >
                <Icon className="size-3.5" />
              </div>
              <span className={cn(
                "text-[10px] font-medium",
                isCurrent ? config.accent.split(" ").find(c => c.startsWith("text-")) : "text-muted-foreground",
              )}>
                {config.label}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export { TIER_ORDER };
export type { TierProgressProps };
