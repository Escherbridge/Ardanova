"use client";

import { cn } from "~/lib/utils";
import { Badge } from "~/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";

type TransactionType =
  | "FUNDING_INFLOW"
  | "ALLOCATION_INDEX"
  | "ALLOCATION_LIQUID"
  | "ALLOCATION_OPS"
  | "PAYOUT_DEBIT"
  | "INDEX_RETURN"
  | "PROFIT_SHARE"
  | "REBALANCE"
  | "TRUST_PROTECTION"
  | "FOUNDER_BURN";

interface Transaction {
  id: string;
  type: TransactionType | string;
  amountUsd: number;
  description: string;
  createdAt: Date | string;
  projectId?: string;
}

interface TreasuryTransactionLogProps {
  transactions: Transaction[];
  filterType?: string;
  onFilterChange: (type: string) => void;
}

const TYPE_LABELS: Record<string, string> = {
  FUNDING_INFLOW: "Funding Inflow",
  ALLOCATION_INDEX: "Alloc: Index",
  ALLOCATION_LIQUID: "Alloc: Liquid",
  ALLOCATION_OPS: "Alloc: Ops",
  PAYOUT_DEBIT: "Payout Debit",
  INDEX_RETURN: "Index Return",
  PROFIT_SHARE: "Profit Share",
  REBALANCE: "Rebalance",
  TRUST_PROTECTION: "Trust Protection",
  FOUNDER_BURN: "Founder Burn",
};

const TYPE_COLORS: Record<string, string> = {
  FUNDING_INFLOW: "text-neon-green border-neon-green/50 bg-neon-green/10",
  ALLOCATION_INDEX: "text-neon-cyan border-neon-cyan/50 bg-neon-cyan/10",
  ALLOCATION_LIQUID: "text-neon-cyan border-neon-cyan/50 bg-neon-cyan/10",
  ALLOCATION_OPS: "text-neon-cyan border-neon-cyan/50 bg-neon-cyan/10",
  PAYOUT_DEBIT: "text-neon-pink border-neon-pink/50 bg-neon-pink/10",
  INDEX_RETURN: "text-neon-green border-neon-green/50 bg-neon-green/10",
  PROFIT_SHARE: "text-neon-green border-neon-green/50 bg-neon-green/10",
  REBALANCE: "text-neon-cyan border-neon-cyan/50 bg-neon-cyan/10",
  TRUST_PROTECTION: "text-neon-pink border-neon-pink/50 bg-neon-pink/10",
  FOUNDER_BURN: "text-neon-pink border-neon-pink/50 bg-neon-pink/10",
};

function formatUsd(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatDate(value: Date | string): string {
  const d = typeof value === "string" ? new Date(value) : value;
  return d.toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "2-digit",
  }) + " " + d.toLocaleTimeString("en-US", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
}

const ALL_TYPES = "ALL";

export function TreasuryTransactionLog({
  transactions,
  filterType,
  onFilterChange,
}: TreasuryTransactionLogProps) {
  const filtered =
    !filterType || filterType === ALL_TYPES
      ? transactions
      : transactions.filter((t) => t.type === filterType);

  return (
    <div className="space-y-4">
      {/* Filter row */}
      <div className="flex items-center justify-between">
        <p className="font-mono text-sm text-muted-foreground">
          {filtered.length} transaction{filtered.length !== 1 ? "s" : ""}
        </p>
        <div className="w-56">
          <Select
            value={filterType ?? ALL_TYPES}
            onValueChange={onFilterChange}
          >
            <SelectTrigger className="border-2 border-white/20 font-mono text-xs">
              <SelectValue placeholder="Filter by type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL_TYPES}>All Types</SelectItem>
              {Object.entries(TYPE_LABELS).map(([key, label]) => (
                <SelectItem key={key} value={key}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <div className="border-2 border-white/20">
        {/* Header */}
        <div className="grid grid-cols-[180px_160px_120px_1fr] gap-4 border-b-2 border-white/20 bg-white/5 px-4 py-2">
          <span className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Date
          </span>
          <span className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Type
          </span>
          <span className="font-mono text-xs uppercase tracking-widest text-right text-muted-foreground">
            Amount
          </span>
          <span className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
            Description
          </span>
        </div>

        {/* Rows */}
        {filtered.length === 0 ? (
          <div className="px-4 py-12 text-center text-sm text-muted-foreground">
            No transactions found
          </div>
        ) : (
          <div className="divide-y divide-white/10">
            {filtered.map((tx) => (
              <div
                key={tx.id}
                className="grid grid-cols-[180px_160px_120px_1fr] gap-4 px-4 py-3 transition-colors hover:bg-white/5"
              >
                <span className="font-mono text-xs text-muted-foreground">
                  {formatDate(tx.createdAt)}
                </span>
                <div>
                  <span
                    className={cn(
                      "inline-flex items-center border px-2 py-0.5 font-mono text-xs font-semibold uppercase tracking-wide",
                      TYPE_COLORS[tx.type] ?? "text-white border-white/20 bg-white/5",
                    )}
                  >
                    {TYPE_LABELS[tx.type] ?? tx.type}
                  </span>
                </div>
                <span
                  className={cn(
                    "font-mono text-sm font-semibold text-right",
                    tx.type === "PAYOUT_DEBIT" ||
                      tx.type === "TRUST_PROTECTION" ||
                      tx.type === "FOUNDER_BURN"
                      ? "text-neon-pink"
                      : "text-neon-green",
                  )}
                >
                  {tx.type === "PAYOUT_DEBIT" ||
                  tx.type === "TRUST_PROTECTION" ||
                  tx.type === "FOUNDER_BURN"
                    ? "-"
                    : "+"}
                  {formatUsd(tx.amountUsd)}
                </span>
                <span className="truncate text-sm text-muted-foreground">
                  {tx.description}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
