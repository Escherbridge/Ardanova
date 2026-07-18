import Link from "next/link";
import { Banknote, LockKeyhole, WalletCards } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";

export default function PayoutsSettingsPage() {
  return (
    <div className="container mx-auto max-w-2xl space-y-8 py-8">
      <div className="space-y-2 border-b-2 border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <Banknote className="text-neon-cyan h-7 w-7" />
          <h1 className="font-mono text-2xl font-bold tracking-tight uppercase">
            Payouts
          </h1>
        </div>
        <p className="text-muted-foreground text-sm">
          Payout and export are not available until the regulated payout
          provider, verification policy, and settlement reconciliation are live.
        </p>
      </div>

      <Card className="border-neon-yellow/30 bg-neon-yellow/5">
        <CardContent className="flex gap-4 pt-6">
          <LockKeyhole className="text-neon-yellow mt-0.5 size-5 shrink-0" />
          <div className="space-y-2">
            <p className="font-mono font-semibold">
              Payouts are safely disabled
            </p>
            <p className="text-muted-foreground text-sm">
              ArdaNova does not collect bank details, custody a payout account,
              export assets, or claim a payout is available from this screen.
            </p>
            <p className="text-muted-foreground text-xs">
              A future provider journey must be server-issued, policy-gated, and
              backed by a confirmed settlement—not a browser redirect.
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="flex flex-col gap-3 sm:flex-row">
        <Button asChild variant="outline" className="font-mono">
          <Link href="/settings/wallets">
            <WalletCards className="mr-2 size-4" />
            MANAGE EXTERNAL WALLETS
          </Link>
        </Button>
        <Button asChild variant="outline" className="font-mono">
          <Link href="/portfolio">VIEW PORTFOLIO STATUS</Link>
        </Button>
      </div>
    </div>
  );
}
