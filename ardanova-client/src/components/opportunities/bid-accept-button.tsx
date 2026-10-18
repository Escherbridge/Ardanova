"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { navigateToAcceptedBidCommerce } from "~/lib/commerce/bid-accept-navigation";
import { evaluateValueActionAccount } from "~/lib/contracts/project-create-contract";
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
  const accountStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: requiresAccountSetup,
    retry: false,
  });
  // Three-way, never a boolean: somebody held for review has finished every step, so telling
  // them to "complete account setup" is an instruction they cannot act on (OI-10).
  const accountEvaluation = evaluateValueActionAccount(accountStatus.data, {
    loadFailed: Boolean(accountStatus.error),
  });
  const accountSetupComplete =
    !requiresAccountSetup || accountEvaluation.state === "ready";
  const accountUnderReview =
    requiresAccountSetup && accountEvaluation.state === "under-review";
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
    onError: (error) =>
      toast.error(describeMutationError(error, "accept the proposal")),
  });

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        size="sm"
        onClick={() => acceptBid.mutate({ id: bidId })}
        disabled={acceptBid.isPending || !accountSetupComplete}
        title={
          accountSetupComplete
            ? undefined
            : (accountEvaluation.message ??
              "Complete account setup before accepting paid work.")
        }
      >
        {acceptBid.isPending ? <Loader2 className="animate-spin" /> : null}
        Accept bid
      </Button>
      {/* Only somebody with setup genuinely left to do is offered the setup link. */}
      {requiresAccountSetup && !accountSetupComplete && !accountUnderReview ? (
        <Button asChild size="sm" variant="outline">
          <Link href="/settings/verification">Complete setup</Link>
        </Button>
      ) : null}
    </div>
  );
}
