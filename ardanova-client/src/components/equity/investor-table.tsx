"use client";

import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { Users } from "lucide-react";

interface Investor {
  displayName: string;
  avatarUrl?: string | null;
  usdAmount: number;
  equityPct: number;
  holderClass?: string;
}

interface InvestorTableProps {
  investors: Investor[];
}

function formatUSD(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", minimumFractionDigits: 0 }).format(n);
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

export default function InvestorTable({ investors }: InvestorTableProps) {
  if (investors.length === 0) {
    return (
      <div className="border-2 border-border p-8 flex flex-col items-center justify-center text-center gap-3">
        <div className="size-12 rounded-full border-2 border-border flex items-center justify-center">
          <Users className="size-5 text-muted-foreground" />
        </div>
        <div>
          <p className="font-mono text-sm font-bold text-muted-foreground">NO INVESTORS YET</p>
          <p className="text-xs text-muted-foreground mt-1">Be the first to back this project.</p>
        </div>
      </div>
    );
  }

  const sorted = [...investors].sort((a, b) => b.usdAmount - a.usdAmount);

  return (
    <div className="border-2 border-border">
      <div className="border-b-2 border-border px-4 py-2 bg-muted/30">
        <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground">
          INVESTORS ({investors.length})
        </span>
      </div>

      <div className="divide-y-2 divide-border">
        {sorted.map((investor, idx) => (
          <div
            key={`${investor.displayName}-${idx}`}
            className="flex items-center gap-3 px-4 py-3 hover:bg-muted/20 transition-colors"
          >
            {/* Rank */}
            <span className="font-mono text-xs text-muted-foreground w-5 shrink-0">
              #{idx + 1}
            </span>

            {/* Avatar */}
            <Avatar className="size-8 border-2 border-border shrink-0">
              <AvatarImage src={investor.avatarUrl ?? undefined} />
              <AvatarFallback className="font-mono text-xs">
                {investor.displayName.charAt(0).toUpperCase()}
              </AvatarFallback>
            </Avatar>

            {/* Name */}
            <div className="flex-1 min-w-0">
              <p className="font-medium text-sm truncate">{investor.displayName}</p>
              {investor.holderClass && (
                <HolderClassBadge holderClass={investor.holderClass} />
              )}
            </div>

            {/* Amount */}
            <div className="text-right shrink-0">
              <p className="font-mono text-sm font-bold text-[#00d4ff]">
                {formatUSD(investor.usdAmount)}
              </p>
              <p className="font-mono text-xs text-muted-foreground">
                {formatPct(investor.equityPct)}
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function HolderClassBadge({ holderClass }: { holderClass: string }) {
  const config: Record<string, { label: string; color: string }> = {
    CONTRIBUTOR: { label: "Earned", color: "text-[#00ff88]" },
    INVESTOR: { label: "Invested", color: "text-[#00d4ff]" },
    FOUNDER: { label: "Founder", color: "text-[#ff0080]" },
  };
  const c = config[holderClass] ?? { label: holderClass, color: "text-muted-foreground" };
  return (
    <span className={cn("font-mono text-[10px]", c.color)}>{c.label}</span>
  );
}
