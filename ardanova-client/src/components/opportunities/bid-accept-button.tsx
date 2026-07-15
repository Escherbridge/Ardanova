"use client";

import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { navigateToAcceptedBidCommerce } from "~/lib/commerce/bid-accept-navigation";
import { api } from "~/trpc/react";

interface BidAcceptButtonProps {
  bidId: string;
}

/** Accepts a bid and opens only the commerce route returned by the BFF. */
export function BidAcceptButton({ bidId }: BidAcceptButtonProps) {
  const router = useRouter();
  const acceptBid = api.opportunityBid.accept.useMutation({
    onSuccess: ({ commerceUrl }) => {
      try {
        navigateToAcceptedBidCommerce(commerceUrl, router.push);
      } catch (error) {
        toast.error(error instanceof Error ? error.message : "Unable to open task commerce.");
      }
    },
    onError: (error) => toast.error(error.message),
  });

  return (
    <Button
      size="sm"
      onClick={() => acceptBid.mutate({ id: bidId })}
      disabled={acceptBid.isPending}
    >
      {acceptBid.isPending ? <Loader2 className="animate-spin" /> : null}
      Accept bid
    </Button>
  );
}
