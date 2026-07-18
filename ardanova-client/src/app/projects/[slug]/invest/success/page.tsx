"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { api } from "~/trpc/react";
import Link from "next/link";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Clock3, ArrowLeft, Wallet, Lock } from "lucide-react";
import { getFundingIntentPresentation } from "~/lib/commerce/funding-intent-status";

export default function InvestSuccessPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [intentId, setIntentId] = useState<string>();

  useEffect(() => {
    setIntentId(
      window.sessionStorage.getItem(`ardanova:funding-intent:${slug}`) ??
        undefined,
    );
  }, [slug]);

  const { data: intent, error } = api.fundingIntent.getStatus.useQuery(
    { intentId: intentId ?? "" },
    {
      enabled: Boolean(intentId),
      refetchInterval: (query) =>
        getFundingIntentPresentation(query.state.data?.status).poll
          ? 5000
          : false,
    },
  );
  const presentation = error
    ? getFundingIntentPresentation()
    : getFundingIntentPresentation(intent?.status);

  return (
    <div className="bg-background flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        {/* A redirect is not a payment receipt. See conductor gated-commerce track. */}
        <div className="space-y-3 text-center">
          <div className="border-system bg-system/10 mx-auto flex size-20 items-center justify-center border-2">
            <Clock3 className="text-system size-10" />
          </div>
          <h1 className="text-foreground font-mono text-2xl font-bold tracking-wide">
            {presentation.heading}
          </h1>
          <p className="text-muted-foreground text-sm">{presentation.detail}</p>
        </div>

        <Card className="border-system bg-system/5 border-2">
          <CardContent className="space-y-3 p-5">
            <span className="text-muted-foreground block font-mono text-xs font-bold tracking-widest">
              WHAT HAPPENS NEXT
            </span>
            <ol className="text-muted-foreground list-none space-y-2 text-sm">
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  01
                </span>
                The server verifies the signed payment-provider event and
                records your funding intent.
              </li>
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  02
                </span>
                A verified payment is not an investment, token allocation,
                equity issuance, or settlement confirmation.
              </li>
              <li className="flex items-start gap-2">
                <span className="text-system mt-0.5 font-mono text-xs font-bold">
                  03
                </span>
                Project tokens and equity are never confirmed from this redirect
                or from URL parameters.
              </li>
            </ol>

            <div className="text-muted-foreground border-border/40 mt-1 flex items-start gap-2 border p-2 text-xs">
              <Lock className="text-muted-foreground mt-0.5 size-3 shrink-0" />
              <span>
                A verified funding intent, project gate, and confirmed
                settlement determine any portfolio availability.
              </span>
            </div>
          </CardContent>
        </Card>

        {/* CTAs */}
        <div className="flex flex-col gap-3">
          <Button
            asChild
            variant="default"
            className="h-12 w-full font-mono font-bold"
          >
            <Link href="/portfolio">
              <Wallet className="mr-2 size-4" />
              VIEW MY PORTFOLIO
            </Link>
          </Button>

          <Button asChild variant="outline" className="h-12 w-full font-mono">
            <Link href={`/projects/${slug}`}>
              <ArrowLeft className="mr-2 size-4" />
              BACK TO PROJECT
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
