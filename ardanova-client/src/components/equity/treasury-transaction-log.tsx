"use client";

import { cn } from "~/lib/utils";
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
  type: TransactionType;
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
  FUNDING_INFLOW: "text-success border-success/50 bg-success/10",
  ALLOCATION_INDEX: "text-system border-system/50 bg-system/10",
  ALLOCATION_LIQUID: "text-system border-system/50 bg-system/10",
  ALLOCATION_OPS: "text-system border-system/50 bg-system/10",
  PAYOUT_DEBIT: "text-destructive border-destructive/50 bg-destructive/10",
  INDEX_RETURN: "text-success border-success/50 bg-success/10",
  PROFIT_SHARE: "text-success border-success/50 bg-success/10",
  REBALANCE: "text-system border-system/50 bg-system/10",
  TRUST_PROTECTION: "text-destructive border-destructive/50 bg-destructive/10",
  FOUNDER_BURN: "text-destructive border-destructive/50 bg-destructive/10",
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
  return (
    d.toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "2-digit",
    }) +
    " " +
    d.toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    })
  );
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
        <p className="text-muted-foreground font-mono text-sm">
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
          <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Date
          </span>
          <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Type
          </span>
          <span className="text-muted-foreground text-right font-mono text-xs tracking-widest uppercase">
            Amount
          </span>
          <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            Description
          </span>
        </div>

        {/* Rows */}
        {filtered.length === 0 ? (
          <div className="text-muted-foreground px-4 py-12 text-center text-sm">
            No transactions found
          </div>
        ) : (
          <div className="divide-y divide-white/10">
            {filtered.map((tx) => (
              <div
                key={tx.id}
                className="grid grid-cols-[180px_160px_120px_1fr] gap-4 px-4 py-3 transition-colors hover:bg-white/5"
              >
                <span className="text-muted-foreground font-mono text-xs">
                  {formatDate(tx.createdAt)}
                </span>
                <div>
                  <span
                    className={cn(
                      "inline-flex items-center border px-2 py-0.5 font-mono text-xs font-semibold tracking-wide uppercase",
                      TYPE_COLORS[tx.type] ??
                        "border-white/20 bg-white/5 text-white",
                    )}
                  >
                    {TYPE_LABELS[tx.type] ?? tx.type}
                  </span>
                </div>
                <span
                  className={cn(
                    "text-right font-mono text-sm font-semibold",
                    tx.type === "PAYOUT_DEBIT" ||
                      tx.type === "TRUST_PROTECTION" ||
                      tx.type === "FOUNDER_BURN"
                      ? "text-destructive"
                      : "text-success",
                  )}
                >
                  {tx.type === "PAYOUT_DEBIT" ||
                  tx.type === "TRUST_PROTECTION" ||
                  tx.type === "FOUNDER_BURN"
                    ? "-"
                    : "+"}
                  {formatUsd(tx.amountUsd)}
                </span>
                <span className="text-muted-foreground truncate text-sm">
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
