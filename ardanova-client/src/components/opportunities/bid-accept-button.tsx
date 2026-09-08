"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { navigateToAcceptedBidCommerce } from "~/lib/commerce/bid-accept-navigation";
import { isValueActionAccountReady } from "~/lib/contracts/project-create-contract";
import { api } from "~/trpc/react";

interface BidAcceptButtonProps {
  bidId: string;
  requiresAccountSetup: boolean;
}

/** Accepts a bid and opens only the commerce route returned by the BFF. */
export function BidAcceptButton({
  bidId,
  requiresAccountSetup,
}: BidAcceptButtonProps) {
  const router = useRouter();
  const accountStatus = api.azoaCustodialAccount.getStatus.useQuery(
    undefined,
    {
      enabled: requiresAccountSetup,
      retry: false,
    },
  );
  const accountSetupComplete =
    !requiresAccountSetup || isValueActionAccountReady(accountStatus.data);
  const acceptBid = api.opportunityBid.accept.useMutation({
    onSuccess: ({ commerceUrl }) => {
      try {
        navigateToAcceptedBidCommerce(commerceUrl, (href) => router.push(href));
      } catch (error) {
        toast.error(
          error instanceof Error
            ? error.message
            : "Unable to open task commerce.",
        );
      }
    },
    onError: (error) => toast.error(error.message),
  });

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        size="sm"
        onClick={() => acceptBid.mutate({ id: bidId })}
        disabled={acceptBid.isPending || !accountSetupComplete}
        title={
          !accountSetupComplete
            ? "Complete account setup before accepting paid work."
            : undefined
        }
      >
        {acceptBid.isPending ? <Loader2 className="animate-spin" /> : null}
        Accept bid
      </Button>
      {requiresAccountSetup && !accountSetupComplete ? (
        <Button asChild size="sm" variant="outline">
          <Link href="/settings/verification">Complete setup</Link>
        </Button>
      ) : null}
    </div>
  );
}
