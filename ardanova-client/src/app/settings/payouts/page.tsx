import Link from "next/link";
import { Button } from "~/components/ui/button";

export default function PayoutsSettingsPage() {
  return (
    <main className="mx-auto max-w-2xl space-y-5 p-6">
      <h1 className="font-mono text-2xl font-semibold">Payouts</h1>
      <p>Check cash-out availability and continue secure bank setup.</p>
      <p className="text-muted-foreground text-sm">
        Bank details stay on the secure account page. A return to this site does
        not confirm a payment.
      </p>
      <Button asChild>
        <Link href="/portfolio/withdraw">Continue to cash out</Link>
      </Button>
    </main>
  );
}
