"use client";

import { useState } from "react";
import { ArrowUpDown, FileX } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import type { TokenAllocationDto } from "~/lib/contracts/tokenomics-contract";
import { cn } from "~/lib/utils";

interface AllocationTableProps {
  allocations: TokenAllocationDto[];
}

const holderClassLabel = {
  CONTRIBUTOR: "Contributor",
  INVESTOR: "Investor",
  FOUNDER: "Founder",
} as const;

const statusVariant = {
  RESERVED: "outline",
  DISTRIBUTED: "neon-green",
  REVOKED: "secondary",
  BURNED: "destructive",
} as const;

function formatPct(value: number) {
  return `${value.toFixed(4)}%`;
}

function formatTokens(value: number) {
  return new Intl.NumberFormat("en-US").format(value);
}

function shortReference(value: string) {
  return value.length > 14 ? `${value.slice(0, 10)}…` : value;
}

type SortKey = "equityPercentage" | "tokenAmount" | "status" | "holderClass";

export default function AllocationTable({ allocations }: AllocationTableProps) {
  const [filterClass, setFilterClass] = useState<
    "ALL" | TokenAllocationDto["holderClass"]
  >("ALL");
  const [sortKey, setSortKey] = useState<SortKey>("equityPercentage");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc");

  const classes: Array<"ALL" | TokenAllocationDto["holderClass"]> = [
    "ALL",
    ...Array.from(
      new Set(allocations.map((allocation) => allocation.holderClass)),
    ),
  ];
  const filtered = allocations.filter(
    (allocation) =>
      filterClass === "ALL" || allocation.holderClass === filterClass,
  );
  const sorted = [...filtered].sort((left, right) => {
    const direction = sortDirection === "desc" ? -1 : 1;
    const leftValue = left[sortKey];
    const rightValue = right[sortKey];

    return typeof leftValue === "number" && typeof rightValue === "number"
      ? direction * (leftValue - rightValue)
      : direction * String(leftValue).localeCompare(String(rightValue));
  });

  const handleSort = (nextKey: SortKey) => {
    if (nextKey === sortKey) {
      setSortDirection((current) => (current === "desc" ? "asc" : "desc"));
      return;
    }

    setSortKey(nextKey);
    setSortDirection("desc");
  };

  return (
    <div>
      <div className="border-border flex flex-wrap gap-2 border-b p-3">
        {classes.map((holderClass) => (
          <button
            key={holderClass}
            type="button"
            onClick={() => setFilterClass(holderClass)}
            aria-pressed={filterClass === holderClass}
            className={cn(
              "border-border min-h-11 border px-3 font-mono text-xs font-bold uppercase",
              filterClass === holderClass
                ? "bg-primary text-primary-foreground border-primary"
                : "hover:border-foreground",
            )}
          >
            {holderClass === "ALL" ? "All" : holderClassLabel[holderClass]}
          </button>
        ))}
      </div>

      <div className="border-border bg-muted/10 hidden grid-cols-[minmax(0,1fr)_auto_auto_auto_auto] gap-3 border-b px-4 py-2 text-xs md:grid">
        <span className="text-muted-foreground">Recorded for</span>
        <SortButton
          label="Class"
          sortKey="holderClass"
          current={sortKey}
          direction={sortDirection}
          onSort={handleSort}
        />
        <SortButton
          label="Status"
          sortKey="status"
          current={sortKey}
          direction={sortDirection}
          onSort={handleSort}
        />
        <SortButton
          label="Token units"
          sortKey="tokenAmount"
          current={sortKey}
          direction={sortDirection}
          onSort={handleSort}
        />
        <SortButton
          label="Allocation %"
          sortKey="equityPercentage"
          current={sortKey}
          direction={sortDirection}
          onSort={handleSort}
        />
      </div>

      {sorted.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
          <FileX className="text-muted-foreground size-8" />
          <p className="text-muted-foreground font-mono text-xs">
            No recorded allocations in this view.
          </p>
        </div>
      ) : (
        <div className="divide-border divide-y">
          {sorted.map((allocation) => {
            const reference = allocation.pbiId
              ? `PBI ${shortReference(allocation.pbiId)}`
              : allocation.recipientUserId
                ? `User ${shortReference(allocation.recipientUserId)}`
                : "Unassigned record";

            return (
              <div
                key={allocation.id}
                className="grid grid-cols-2 gap-3 p-4 md:grid-cols-[minmax(0,1fr)_auto_auto_auto_auto] md:items-center"
              >
                <div className="col-span-2 min-w-0 md:col-span-1">
                  <p className="truncate text-sm font-semibold">{reference}</p>
                  <p
                    className="text-muted-foreground truncate font-mono text-[10px]"
                    title={allocation.id}
                  >
                    allocation {shortReference(allocation.id)}
                  </p>
                </div>
                <div>
                  <span className="text-muted-foreground block text-[10px] md:hidden">
                    Class
                  </span>
                  <span className="text-xs">
                    {holderClassLabel[allocation.holderClass]}
                  </span>
                </div>
                <div className="text-right md:text-left">
                  <span className="text-muted-foreground block text-[10px] md:hidden">
                    Status
                  </span>
                  <Badge variant={statusVariant[allocation.status]} size="sm">
                    {allocation.status}
                  </Badge>
                </div>
                <div>
                  <span className="text-muted-foreground block text-[10px] md:hidden">
                    Token units
                  </span>
                  <span className="font-mono text-xs">
                    {formatTokens(allocation.tokenAmount)}
                  </span>
                </div>
                <div className="text-right">
                  <span className="text-muted-foreground block text-[10px] md:hidden">
                    Allocation percentage
                  </span>
                  <span className="text-primary font-mono text-xs font-bold">
                    {formatPct(allocation.equityPercentage)}
                  </span>
                </div>
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
  direction,
  onSort,
}: {
  label: string;
  sortKey: SortKey;
  current: SortKey;
  direction: "asc" | "desc";
  onSort: (key: SortKey) => void;
}) {
  return (
    <button
      type="button"
      onClick={() => onSort(sortKey)}
      className={cn(
        "flex min-h-11 items-center justify-end gap-1 font-mono text-xs",
        current === sortKey ? "text-foreground" : "text-muted-foreground",
      )}
      aria-label={`Sort by ${label}, ${current === sortKey ? direction : "not selected"}`}
    >
      {label}
      <ArrowUpDown className="size-3" aria-hidden="true" />
    </button>
  );
}
