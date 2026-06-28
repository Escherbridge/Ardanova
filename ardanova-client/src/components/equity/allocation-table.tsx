"use client";

import { useState } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { cn } from "~/lib/utils";
import { ArrowUpDown, FileX } from "lucide-react";

type HolderClass = "CONTRIBUTOR" | "INVESTOR" | "FOUNDER" | string;
type AllocationStatus = "PENDING" | "ACTIVE" | "VESTED" | "BURNED" | string;

interface Allocation {
  id: string;
  taskId?: string | null;
  userId?: string | null;
  equityPercentage: number;
  tokenAmount: number;
  status: AllocationStatus;
  holderClass: HolderClass;
  description?: string | null;
}

interface AllocationTableProps {
  allocations: Allocation[];
}

const holderClassConfig: Record<string, { label: string; color: string }> = {
  CONTRIBUTOR: { label: "Earned", color: "text-[#00ff88]" },
  INVESTOR: { label: "Invested", color: "text-[#00d4ff]" },
  FOUNDER: { label: "Founder", color: "text-[#ff0080]" },
};

const statusConfig: Record<string, { label: string; color: string }> = {
  PENDING: { label: "Pending", color: "text-muted-foreground" },
  ACTIVE: { label: "Active", color: "text-[#00d4ff]" },
  VESTED: { label: "Vested", color: "text-[#00ff88]" },
  BURNED: { label: "Burned", color: "text-[#ff0080]" },
};

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

function formatTokens(n: number) {
  return new Intl.NumberFormat("en-US").format(Math.round(n));
}

type SortKey = "equityPercentage" | "tokenAmount" | "status" | "holderClass";

export default function AllocationTable({ allocations }: AllocationTableProps) {
  const [filterClass, setFilterClass] = useState<string>("ALL");
  const [sortKey, setSortKey] = useState<SortKey>("equityPercentage");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");

  const uniqueClasses = ["ALL", ...Array.from(new Set(allocations.map((a) => a.holderClass)))];

  const filtered = allocations.filter(
    (a) => filterClass === "ALL" || a.holderClass === filterClass,
  );

  const sorted = [...filtered].sort((a, b) => {
    const mult = sortDir === "desc" ? -1 : 1;
    if (sortKey === "equityPercentage") return mult * (a.equityPercentage - b.equityPercentage);
    if (sortKey === "tokenAmount") return mult * (a.tokenAmount - b.tokenAmount);
    if (sortKey === "holderClass") return mult * a.holderClass.localeCompare(b.holderClass);
    if (sortKey === "status") return mult * a.status.localeCompare(b.status);
    return 0;
  });

  const handleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(key);
      setSortDir("desc");
    }
  };

  return (
    <div className="border-2 border-border">
      {/* Toolbar */}
      <div className="border-b-2 border-border px-4 py-2 flex items-center gap-3 bg-muted/30">
        <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground flex-1">
          ALLOCATIONS ({filtered.length})
        </span>
        <Select value={filterClass} onValueChange={setFilterClass}>
          <SelectTrigger className="h-7 text-xs border-2 w-36">
            <SelectValue placeholder="All Classes" />
          </SelectTrigger>
          <SelectContent>
            {uniqueClasses.map((c) => (
              <SelectItem key={c} value={c} className="text-xs">
                {c === "ALL" ? "All Classes" : (holderClassConfig[c]?.label ?? c)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Header */}
      <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-2 px-4 py-2 border-b border-border bg-muted/10 text-xs text-muted-foreground">
        <span>Description</span>
        <SortButton label="Class" sortKey="holderClass" current={sortKey} dir={sortDir} onSort={handleSort} />
        <SortButton label="Status" sortKey="status" current={sortKey} dir={sortDir} onSort={handleSort} />
        <SortButton label="Tokens" sortKey="tokenAmount" current={sortKey} dir={sortDir} onSort={handleSort} />
        <SortButton label="Equity" sortKey="equityPercentage" current={sortKey} dir={sortDir} onSort={handleSort} />
      </div>

      {/* Rows */}
      {sorted.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
          <FileX className="size-8 text-muted-foreground" />
          <p className="font-mono text-xs text-muted-foreground">No allocations found.</p>
        </div>
      ) : (
        <div className="divide-y divide-border">
          {sorted.map((alloc) => {
            const hc = holderClassConfig[alloc.holderClass] ?? { label: alloc.holderClass, color: "text-foreground" };
            const sc = statusConfig[alloc.status] ?? { label: alloc.status, color: "text-muted-foreground" };

            return (
              <div
                key={alloc.id}
                className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-2 px-4 py-3 items-center hover:bg-muted/10 transition-colors"
              >
                {/* Description */}
                <div className="min-w-0">
                  <p className="text-sm truncate">
                    {alloc.description ?? (alloc.taskId ? `Task ${alloc.taskId.slice(0, 8)}` : `User allocation`)}
                  </p>
                  {alloc.taskId && (
                    <p className="font-mono text-[10px] text-muted-foreground">
                      task: {alloc.taskId.slice(0, 8)}...
                    </p>
                  )}
                </div>

                {/* Holder class */}
                <span className={cn("font-mono text-xs font-bold whitespace-nowrap", hc.color)}>
                  {hc.label}
                </span>

                {/* Status */}
                <span className={cn("font-mono text-xs whitespace-nowrap", sc.color)}>
                  {sc.label}
                </span>

                {/* Token amount */}
                <span className="font-mono text-xs text-right whitespace-nowrap">
                  {formatTokens(alloc.tokenAmount)}
                </span>

                {/* Equity pct */}
                <span className="font-mono text-xs font-bold text-[#00d4ff] text-right whitespace-nowrap">
                  {formatPct(alloc.equityPercentage)}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

function SortButton({
  label,
  sortKey,
  current,
  dir,
  onSort,
}: {
  label: string;
  sortKey: SortKey;
  current: SortKey;
  dir: "asc" | "desc";
  onSort: (k: SortKey) => void;
}) {
  const active = current === sortKey;
  return (
    <button
      onClick={() => onSort(sortKey)}
      className={cn(
        "flex items-center gap-1 font-mono text-xs whitespace-nowrap",
        active ? "text-foreground" : "text-muted-foreground hover:text-foreground",
      )}
    >
      {label}
      <ArrowUpDown className={cn("size-3", active ? "opacity-100" : "opacity-40")} />
    </button>
  );
}
