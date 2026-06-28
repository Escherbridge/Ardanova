"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Loader2, CheckCircle2, CreditCard, Banknote, ExternalLink } from "lucide-react";
import { cn } from "~/lib/utils";

// Placeholder: real integration will use /api/stripe/connect-account
// Once Stripe Connect is wired up, replace this handler with a redirect to
// the Stripe onboarding URL returned by that endpoint.

type ConnectionStatus = "disconnected" | "connected";

// For demo/placeholder: derive status from session or local state.
// In production this would come from a tRPC query (e.g. api.payout.getStatus).
function usePayoutConnectionStatus() {
  // Placeholder — always shows "disconnected" until backend is wired.
  const [status] = useState<ConnectionStatus>("disconnected");
  const [maskedAccount] = useState<string | null>(null);
  return { status, maskedAccount };
}

export default function PayoutsSettingsPage() {
  const { data: session, status: sessionStatus } = useSession();
  const { status: connectionStatus, maskedAccount } =
    usePayoutConnectionStatus();

  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [isConnecting, setIsConnecting] = useState(false);

  function showToast(message: string) {
    setToastMessage(message);
    setTimeout(() => setToastMessage(null), 4000);
  }

  function handleConnect() {
    setIsConnecting(true);
    // Placeholder: real implementation calls /api/stripe/connect-account
    // and redirects to Stripe onboarding flow.
    setTimeout(() => {
      setIsConnecting(false);
      showToast("Stripe Connect integration coming soon");
    }, 600);
  }

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-neon-cyan" />
      </div>
    );
  }

  if (!session) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="border-2 border-neon-pink px-8 py-6 text-center">
          <p className="font-mono text-neon-pink">Sign in required</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-2xl space-y-8 py-8">
      {/* Header */}
      <div className="space-y-2 border-b-2 border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <Banknote className="h-7 w-7 text-neon-cyan" />
          <h1 className="font-mono text-2xl font-bold uppercase tracking-tight">
            Payout Account
          </h1>
        </div>
        <p className="text-sm text-muted-foreground">
          Connect a bank account to receive equity payouts from your ARDA
          holdings.
        </p>
      </div>

      {/* Connection card */}
      {connectionStatus === "disconnected" ? (
        <div className="border-2 border-white/20 p-6 space-y-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center border-2 border-white/20 bg-white/5">
              <CreditCard className="h-6 w-6 text-muted-foreground" />
            </div>
            <div className="space-y-1">
              <p className="font-mono font-semibold">No payout account connected</p>
              <p className="text-sm text-muted-foreground">
                Connect a bank account to receive equity payouts. Powered by
                Stripe Connect.
              </p>
            </div>
          </div>

          <div className="space-y-3">
            <Button
              className={cn(
                "w-full border-2 border-neon-cyan bg-transparent font-mono font-bold",
                "text-neon-cyan hover:bg-neon-cyan/10 active:bg-neon-cyan/20",
                "flex items-center justify-center gap-2",
              )}
              onClick={handleConnect}
              disabled={isConnecting}
            >
              {isConnecting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <ExternalLink className="h-4 w-4" />
              )}
              CONNECT PAYOUT ACCOUNT
            </Button>

            <p className="text-center font-mono text-xs text-muted-foreground/60">
              You will be redirected to Stripe to complete setup.
            </p>
          </div>

          {/* Info bullets */}
          <div className="space-y-2 border-t border-white/10 pt-4">
            <p className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
              What to expect
            </p>
            <ul className="space-y-1 text-sm text-muted-foreground">
              <li className="flex items-center gap-2">
                <span className="text-neon-cyan">—</span>
                Bank-level encryption via Stripe
              </li>
              <li className="flex items-center gap-2">
                <span className="text-neon-cyan">—</span>
                Supports checking and savings accounts
              </li>
              <li className="flex items-center gap-2">
                <span className="text-neon-cyan">—</span>
                Payouts processed within 2–5 business days
              </li>
            </ul>
          </div>
        </div>
      ) : (
        /* Connected state */
        <div className="border-2 border-neon-green/50 p-6 space-y-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center border-2 border-neon-green/50 bg-neon-green/10">
              <CheckCircle2 className="h-6 w-6 text-neon-green" />
            </div>
            <div className="space-y-2">
              <div className="flex items-center gap-3">
                <p className="font-mono font-semibold">Payout account connected</p>
                <span className="border border-neon-green/50 bg-neon-green/10 px-2 py-0.5 font-mono text-xs font-bold text-neon-green">
                  ACTIVE
                </span>
              </div>
              {maskedAccount && (
                <p className="font-mono text-sm text-muted-foreground">
                  Account ending in{" "}
                  <span className="font-bold text-white">{maskedAccount}</span>
                </p>
              )}
            </div>
          </div>

          <Button
            variant="outline"
            className="border-2 border-white/20 font-mono hover:bg-white/10"
            onClick={handleConnect}
            disabled={isConnecting}
          >
            {isConnecting && (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            )}
            UPDATE ACCOUNT
          </Button>
        </div>
      )}

      {/* Toast notification */}
      {toastMessage && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 border-2 border-neon-cyan bg-black px-6 py-3 shadow-lg">
          <p className="font-mono text-sm text-neon-cyan">{toastMessage}</p>
        </div>
      )}
    </div>
  );
}
