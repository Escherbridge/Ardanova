"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { api } from "~/trpc/react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Loader2, Lock, TrendingUp } from "lucide-react";
import { cn } from "~/lib/utils";

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

interface EquityPreviewProps {
  configId: string;
  projectSlug: string;
  gateStatus: GateStatus;
  unitName: string;
}

const QUICK_AMOUNTS = [100, 500, 1000, 5000];

function formatUSD(n: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", minimumFractionDigits: 2 }).format(n);
}

function formatToken(n: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(Math.round(n))} ${unit}`;
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

export default function EquityPreview({ configId, projectSlug, gateStatus, unitName }: EquityPreviewProps) {
  const router = useRouter();
  const [usdInput, setUsdInput] = useState("100");
  const [debouncedUsd, setDebouncedUsd] = useState(100);

  // Debounce input
  useEffect(() => {
    const timer = setTimeout(() => {
      const parsed = parseFloat(usdInput);
      if (!isNaN(parsed) && parsed > 0) {
        setDebouncedUsd(parsed);
      }
    }, 400);
    return () => clearTimeout(timer);
  }, [usdInput]);

  const { data: tokenValueRaw } = api.exchange.getProjectTokenValue.useQuery({ configId });
  const tokenValue = tokenValueRaw as unknown as number | undefined;

  // Compute token amount from usd / tokenValue
  const tokenAmount = tokenValue && tokenValue > 0 ? Math.max(1, Math.round(debouncedUsd / tokenValue)) : null;

  const { data: previewRaw, isLoading: previewLoading } = api.exchange.getConversionPreview.useQuery(
    { projectTokenConfigId: configId, tokenAmount: tokenAmount ?? 1 },
    { enabled: !!tokenAmount && tokenAmount >= 1 }
  );
  const preview = previewRaw as unknown as { projectTokens: number; ardaAmount: number; usdAmount: number; tokenRate: number; ardaRate: number } | undefined;

  const { data: supplyRaw } = api.projectTokens.getSupply.useQuery({ id: configId });
  const supply = supplyRaw as unknown as { totalSupply: number } | undefined;

  const equityPct =
    preview && supply && supply.totalSupply > 0
      ? (preview.projectTokens / supply.totalSupply) * 100
      : null;

  const isDisabled = gateStatus !== "FUNDING";

  const handleQuickAmount = useCallback((amount: number) => {
    setUsdInput(String(amount));
  }, []);

  const handleInvest = () => {
    router.push(`/projects/${projectSlug}/invest?amount=${usdInput}`);
  };

  return (
    <div className="border-2 border-border p-4 space-y-4">
      <div className="flex items-center justify-between">
        <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground">
          INVESTMENT PREVIEW
        </span>
        {isDisabled && (
          <span className="inline-flex items-center gap-1 text-xs text-muted-foreground border border-border px-2 py-0.5">
            <Lock className="size-3" />
            {gateStatus}
          </span>
        )}
      </div>

      {/* Quick amounts */}
      <div className="flex gap-2 flex-wrap">
        {QUICK_AMOUNTS.map((amt) => (
          <button
            key={amt}
            onClick={() => handleQuickAmount(amt)}
            disabled={isDisabled}
            className={cn(
              "font-mono text-xs border-2 px-3 py-1 transition-colors",
              usdInput === String(amt)
                ? "border-[#00d4ff] bg-[#00d4ff]/10 text-[#00d4ff]"
                : "border-border text-muted-foreground hover:border-foreground hover:text-foreground",
              isDisabled && "opacity-40 cursor-not-allowed",
            )}
          >
            ${amt.toLocaleString()}
          </button>
        ))}
      </div>

      {/* Custom amount */}
      <div className="flex gap-2">
        <div className="relative flex-1">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground font-mono text-sm">$</span>
          <Input
            type="number"
            value={usdInput}
            onChange={(e) => setUsdInput(e.target.value)}
            disabled={isDisabled}
            className="pl-7 font-mono border-2"
            placeholder="0.00"
            min="1"
          />
        </div>
      </div>

      {/* Preview results */}
      <div className="border-2 border-border bg-background/50 p-3 space-y-2">
        {previewLoading ? (
          <div className="flex items-center gap-2 text-muted-foreground text-sm">
            <Loader2 className="size-4 animate-spin" />
            <span>Calculating...</span>
          </div>
        ) : preview ? (
          <>
            <PreviewRow
              label="Tokens received"
              value={formatToken(preview.projectTokens, unitName)}
              accent="#00d4ff"
            />
            <PreviewRow
              label="Equity stake"
              value={equityPct !== null ? formatPct(equityPct) : "—"}
              accent="#00ff88"
            />
            <PreviewRow
              label="ARDA equivalent"
              value={preview.ardaAmount ? `${new Intl.NumberFormat("en-US").format(Math.round(preview.ardaAmount))} ARDA` : "—"}
              accent="#a855f7"
            />
            <PreviewRow
              label="Current USD value"
              value={formatUSD(preview.usdAmount ?? debouncedUsd)}
              accent="#fbbf24"
            />
          </>
        ) : (
          <p className="text-xs text-muted-foreground">Enter an amount to see your equity preview.</p>
        )}
      </div>

      {/* Lock notice */}
      <div className="flex items-start gap-2 text-xs text-muted-foreground border border-border/40 p-2">
        <Lock className="size-3 mt-0.5 shrink-0" />
        <span>Tokens are locked under trust protection until Gate 2 is cleared.</span>
      </div>

      <Button
        variant="neon"
        className="w-full font-mono font-bold tracking-wider"
        disabled={isDisabled || !preview}
        onClick={handleInvest}
      >
        <TrendingUp className="size-4 mr-2" />
        BACK THIS PROJECT
      </Button>
    </div>
  );
}

function PreviewRow({ label, value, accent }: { label: string; value: string; accent: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="font-mono text-xs font-bold" style={{ color: accent }}>
        {value}
      </span>
    </div>
  );
}
