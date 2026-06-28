"use client";

import { useParams, useSearchParams } from "next/navigation";
import Link from "next/link";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { CheckCircle2, ArrowLeft, Wallet, TrendingUp, Lock } from "lucide-react";

function formatUSD(n: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(n);
}

function formatToken(n: number, unit: string) {
  return `${new Intl.NumberFormat("en-US").format(Math.round(n))} ${unit}`;
}

function formatPct(n: number) {
  return `${n.toFixed(4)}%`;
}

export default function InvestSuccessPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const slug = params.slug as string;

  // These params would be passed by the Stripe redirect callback
  const amount = searchParams.get("amount");
  const tokens = searchParams.get("tokens");
  const equity = searchParams.get("equity");
  const unitName = searchParams.get("unit") ?? "TOKEN";

  const usdAmount = amount ? parseFloat(amount) : null;
  const tokenCount = tokens ? parseFloat(tokens) : null;
  const equityPct = equity ? parseFloat(equity) : null;

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="max-w-md w-full space-y-6">
        {/* Success hero */}
        <div className="text-center space-y-3">
          <div className="size-20 rounded-full border-2 border-[#00ff88] bg-[#00ff88]/10 flex items-center justify-center mx-auto shadow-[0_0_24px_rgba(0,255,136,0.3)]">
            <CheckCircle2 className="size-10 text-[#00ff88]" />
          </div>
          <h1 className="text-2xl font-bold font-mono tracking-wide text-[#00ff88]">
            INVESTMENT CONFIRMED
          </h1>
          <p className="text-muted-foreground text-sm">
            Your investment has been processed successfully. Welcome to the project!
          </p>
        </div>

        {/* Investment summary */}
        {(usdAmount ?? tokenCount ?? equityPct) && (
          <Card className="border-2 border-[#00ff88]/40 bg-[#00ff88]/5">
            <CardContent className="p-5 space-y-3">
              <span className="font-mono text-xs font-bold tracking-widest text-[#00ff88] block">
                INVESTMENT SUMMARY
              </span>

              {usdAmount && (
                <SummaryRow
                  icon={<TrendingUp className="size-4 text-[#00d4ff]" />}
                  label="Amount invested"
                  value={formatUSD(usdAmount)}
                  valueColor="text-[#00d4ff]"
                />
              )}
              {tokenCount && (
                <SummaryRow
                  icon={<Wallet className="size-4 text-[#00ff88]" />}
                  label="Tokens received"
                  value={formatToken(tokenCount, unitName)}
                  valueColor="text-[#00ff88]"
                />
              )}
              {equityPct && (
                <SummaryRow
                  icon={<TrendingUp className="size-4 text-[#00ff88]" />}
                  label="Equity stake"
                  value={formatPct(equityPct)}
                  valueColor="text-[#00ff88]"
                />
              )}

              <div className="flex items-start gap-2 text-xs text-muted-foreground border border-border/40 p-2 mt-1">
                <Lock className="size-3 mt-0.5 shrink-0 text-muted-foreground" />
                <span>
                  Your tokens are locked under trust protection until Gate 2 is cleared by the project founder.
                </span>
              </div>
            </CardContent>
          </Card>
        )}

        {/* What happens next */}
        <Card className="border-2 border-border">
          <CardContent className="p-5 space-y-3">
            <span className="font-mono text-xs font-bold tracking-widest text-muted-foreground block">
              WHAT HAPPENS NEXT
            </span>
            <ol className="space-y-2 text-sm text-muted-foreground list-none">
              <li className="flex items-start gap-2">
                <span className="font-mono text-xs font-bold text-[#00d4ff] mt-0.5">01</span>
                Your tokens are held in trust and will be added to your portfolio once Gate 1 is cleared.
              </li>
              <li className="flex items-start gap-2">
                <span className="font-mono text-xs font-bold text-[#00d4ff] mt-0.5">02</span>
                When the project reaches its funding goal and Gate 2 is cleared, tokens are unlocked.
              </li>
              <li className="flex items-start gap-2">
                <span className="font-mono text-xs font-bold text-[#00d4ff] mt-0.5">03</span>
                If the project fails to meet its goal, you will receive a full refund.
              </li>
            </ol>
          </CardContent>
        </Card>

        {/* CTAs */}
        <div className="flex flex-col gap-3">
          <Button asChild variant="neon" className="w-full font-mono font-bold h-12">
            <Link href="/portfolio">
              <Wallet className="size-4 mr-2" />
              VIEW MY PORTFOLIO
            </Link>
          </Button>

          <Button asChild variant="outline" className="w-full font-mono h-12">
            <Link href={`/projects/${slug}`}>
              <ArrowLeft className="size-4 mr-2" />
              BACK TO PROJECT
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}

function SummaryRow({
  icon,
  label,
  value,
  valueColor,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  valueColor: string;
}) {
  return (
    <div className="flex items-center gap-3">
      <div className="shrink-0">{icon}</div>
      <span className="text-sm text-muted-foreground flex-1">{label}</span>
      <span className={`font-mono text-sm font-bold ${valueColor}`}>{value}</span>
    </div>
  );
}
