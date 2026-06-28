"use client";

import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Lock, Unlock } from "lucide-react";
import { cn } from "~/lib/utils";

interface FounderAllocationProps {
  founder: {
    displayName: string;
    avatarUrl?: string | null;
    equityPct: number;
  };
  isLocked: boolean;
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

export default function FounderAllocation({ founder, isLocked }: FounderAllocationProps) {
  return (
    <div className="border-2 border-[#ff0080]/40 bg-[#ff0080]/5 p-4">
      <div className="flex items-center justify-between mb-3">
        <span className="font-mono text-xs font-bold tracking-widest text-[#ff0080]">
          FOUNDER ALLOCATION
        </span>
        <div
          className={cn(
            "inline-flex items-center gap-1 text-xs border px-2 py-0.5 font-mono",
            isLocked
              ? "border-[#ff0080]/40 text-[#ff0080] bg-[#ff0080]/10"
              : "border-[#00ff88]/40 text-[#00ff88] bg-[#00ff88]/10",
          )}
        >
          {isLocked ? <Lock className="size-3" /> : <Unlock className="size-3" />}
          {isLocked ? "LOCKED" : "VESTED"}
        </div>
      </div>

      <div className="flex items-center gap-3">
        <Avatar className="size-10 border-2 border-[#ff0080]/40">
          <AvatarImage src={founder.avatarUrl ?? undefined} />
          <AvatarFallback className="font-mono text-sm bg-[#ff0080]/20 text-[#ff0080]">
            {founder.displayName.charAt(0).toUpperCase()}
          </AvatarFallback>
        </Avatar>

        <div className="flex-1">
          <p className="font-medium text-sm">{founder.displayName}</p>
          <p className="font-mono text-[10px] text-[#ff0080]">Project Founder</p>
        </div>

        <div className="text-right">
          <p className="font-mono text-lg font-bold text-[#ff0080]">
            {formatPct(founder.equityPct)}
          </p>
          <p className="font-mono text-[10px] text-muted-foreground">equity stake</p>
        </div>
      </div>

      {isLocked && (
        <p className="text-xs text-muted-foreground mt-3 flex items-center gap-1">
          <Lock className="size-3 shrink-0" />
          Founder allocation is locked until Gate 2 is cleared.
        </p>
      )}
    </div>
  );
}
