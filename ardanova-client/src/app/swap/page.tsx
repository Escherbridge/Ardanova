"use client";

import { useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import { ArrowLeftRight, Loader2 } from "lucide-react";

import { SwapHistory } from "~/components/equity/swap-history";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { buildSignInHref } from "~/lib/auth-navigation";
import { api } from "~/trpc/react";

export default function SwapPage() {
  const { data: session, status: sessionStatus } = useSession();
  const router = useRouter();
  const history = api.swap.getHistory.useQuery(undefined, {
    enabled: Boolean(session?.user?.id),
    retry: false,
  });

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      router.push(buildSignInHref("/swap"));
    }
  }, [router, sessionStatus]);

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary size-8 animate-spin" />
      </div>
    );
  }

  if (!session) return null;

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-3xl space-y-8 px-4 py-8">
        <header className="border-foreground border-b-2 pb-6">
          <div className="flex items-center gap-3">
            <span className="border-foreground bg-system/10 text-system flex size-10 items-center justify-center border-2">
              <ArrowLeftRight className="size-5" aria-hidden="true" />
            </span>
            <div>
              <p className="text-primary font-mono text-xs font-bold tracking-[0.16em] uppercase">
                Project units
              </p>
              <h1 className="font-mono text-3xl font-black tracking-tight uppercase">
                Project share exchange
              </h1>
            </div>
          </div>
          <p className="text-muted-foreground mt-4 max-w-2xl text-sm leading-relaxed">
            Exchange requests are temporarily unavailable. ArdaNova will not
            move project units or record a conversion until account readiness,
            provider settlement, and reconciliation are complete.
          </p>
        </header>

        <Card className="border-foreground">
          <CardContent className="space-y-5 p-5 sm:p-6">
            <div>
              <Badge variant="outline" className="font-mono uppercase">
                Secure settlement pending
              </Badge>
              <h2 className="mt-3 text-2xl font-bold">
                Exchange requests are paused
              </h2>
              <p className="text-muted-foreground mt-2 max-w-2xl text-sm leading-relaxed">
                Your existing project records remain available below. Complete
                account setup for future funding and paid work, but do not rely
                on this page to change a balance today.
              </p>
            </div>
            <div className="flex flex-wrap gap-3">
              <Button asChild>
                <Link href="/settings/verification?returnTo=%2Fswap">
                  Review account setup
                </Link>
              </Button>
              <Button asChild variant="outline">
                <Link href="/portfolio">View portfolio</Link>
              </Button>
            </div>
          </CardContent>
        </Card>

        <section className="space-y-4" aria-labelledby="exchange-history-title">
          <div className="border-border flex items-center justify-between border-b pb-2">
            <h2
              id="exchange-history-title"
              className="font-mono text-sm font-bold tracking-widest uppercase"
            >
              Previous exchange records
            </h2>
            {!history.isLoading && !history.error && (
              <Badge variant="outline" size="sm">
                {(history.data ?? []).length} record
                {(history.data ?? []).length === 1 ? "" : "s"}
              </Badge>
            )}
          </div>

          {history.isLoading ? (
            <div className="flex items-center justify-center py-8" role="status">
              <Loader2 className="text-primary size-6 animate-spin" />
            </div>
          ) : history.error ? (
            <Card className="border-destructive">
              <CardContent className="space-y-3 p-4">
                <p className="text-destructive text-sm" role="alert">
                  Previous exchange records could not be loaded. This does not
                  mean there are no records.
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void history.refetch()}
                >
                  Retry records
                </Button>
              </CardContent>
            </Card>
          ) : (
            <SwapHistory
              swaps={(history.data ?? []).map((swap) => ({
                id: swap.id,
                sourceUnitName: swap.sourceUnitName,
                sourceTokenAmount: swap.sourceTokenAmount,
                targetUnitName: swap.targetUnitName,
                targetTokenAmount: swap.targetTokenAmount,
                sourceUsdValue: swap.sourceUsdValue,
                targetUsdValue: swap.targetUsdValue,
                createdAt: swap.createdAt,
                status: swap.status,
              }))}
            />
          )}
        </section>
      </div>
    </div>
  );
}
