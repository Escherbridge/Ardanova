import Link from "next/link";
import { ArrowUpRight, ServerCog, ShieldCheck } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";

export default function KycOperationsPage() {
  return (
    <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-6 sm:px-6 sm:py-8">
      <header className="border-foreground border-b-2 pb-6">
        <Badge variant="outline" className="font-mono uppercase">
          Authority moved to Azoa
        </Badge>
        <h1 className="mt-4 font-mono text-4xl font-black tracking-[-0.055em] uppercase sm:text-5xl">
          Identity operations
        </h1>
        <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed sm:text-base">
          ArdaNova no longer reviews identity documents or approves KYC locally.
          The configured Azoa node owns provider selection, review tooling,
          retention, and the authoritative decision.
        </p>
      </header>

      <section className="border-foreground bg-card grid border-2 md:grid-cols-2">
        <div className="border-foreground space-y-4 border-b-2 p-5 md:border-r-2 md:border-b-0 md:p-6">
          <ServerCog className="text-primary size-8" aria-hidden="true" />
          <div>
            <h2 className="font-mono text-xl font-black uppercase">
              Node operator tooling
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
              Configure a supported provider and conduct provider-appropriate
              review inside Azoa. Unknown or incomplete providers remain
              unavailable.
            </p>
          </div>
        </div>
        <div className="space-y-4 p-5 md:p-6">
          <ShieldCheck className="text-primary size-8" aria-hidden="true" />
          <div>
            <h2 className="font-mono text-xl font-black uppercase">
              ArdaNova boundary
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
              This application stores safe account references and normalized
              status only. It does not expose document links, provider payloads,
              or override external-provider decisions.
            </p>
          </div>
        </div>
      </section>

      <Button variant="outline" className="min-h-11 w-full sm:w-auto" asChild>
        <Link href="/dashboard/profile">
          View the member-facing profile flow
          <ArrowUpRight className="size-4" aria-hidden="true" />
        </Link>
      </Button>
    </div>
  );
}
