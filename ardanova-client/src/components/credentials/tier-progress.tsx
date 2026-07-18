"use client";

import { Progress } from "~/components/ui/progress";
import {
  CredentialTierIcon,
  normalizeCredentialTier,
  tierConfig,
  type CredentialTier,
} from "./credential-badge";
import { cn } from "~/lib/utils";

const TIER_ORDER: CredentialTier[] = [
  "BRONZE",
  "SILVER",
  "GOLD",
  "PLATINUM",
  "DIAMOND",
];

interface TierProgressProps {
  currentTier?: string | null;
  className?: string;
}

export function TierProgress({ currentTier, className }: TierProgressProps) {
  const normalizedTier = normalizeCredentialTier(currentTier);
  const currentIndex = normalizedTier ? TIER_ORDER.indexOf(normalizedTier) : -1;
  const progressValue =
    currentIndex >= 0 ? ((currentIndex + 1) / TIER_ORDER.length) * 100 : 0;

  return (
    <div className={cn("space-y-3", className)}>
      <Progress
        value={progressValue}
        variant="neon"
        aria-label="Credential tier progress"
      />
      <div className="flex justify-between">
        {TIER_ORDER.map((tier, index) => {
          const config = tierConfig[tier];
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
                  "flex size-6 items-center justify-center border-2 transition-all",
                  isCurrent && "ring-offset-background ring-2 ring-offset-2",
                  isCurrent ? config.accent : "border-border",
                )}
              >
                <CredentialTierIcon tier={tier} className="size-3.5" />
              </div>
              <span
                className={cn(
                  "text-[10px] font-medium",
                  isCurrent
                    ? config.accent
                        .split(" ")
                        .find((c) => c.startsWith("text-"))
                    : "text-muted-foreground",
                )}
              >
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
