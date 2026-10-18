"use client";

import { Loader2 } from "lucide-react";

import ApplicationsTab from "~/components/opportunities/applications-tab";
import { BidAcceptButton } from "~/components/opportunities/bid-accept-button";
import { OpportunityStatusControl } from "~/components/opportunities/opportunity-status-control";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import type { OpportunityBid } from "~/lib/api/ardanova/endpoints/opportunity-bids";
import { api } from "~/trpc/react";

interface OpportunityBidsPanelProps {
  opportunityId: string;
}

const actionableStatuses = new Set(["SUBMITTED", "UNDER_REVIEW"]);

/** Task commerce refuses a bid without a positive award (TaskCommerceService.AcceptBidAsync). */
function isPaid(bid: OpportunityBid): boolean {
  return bid.proposedAmount != null && bid.proposedAmount > 0;
}

/** The owner's hiring surface: open/close the role, review applications, then paid proposals. */
export function OpportunityBidsPanel({
  opportunityId,
}: OpportunityBidsPanelProps) {
  return (
    <div className="space-y-6">
      <OpportunityStatusControl opportunityId={opportunityId} />
      <section className="space-y-3" aria-labelledby="role-applications-heading">
        <h2 id="role-applications-heading" className="text-lg font-semibold">
          Applications
        </h2>
        <ApplicationsTab opportunityId={opportunityId} isOwner />
      </section>
      <PaidProposals opportunityId={opportunityId} />
    </div>
  );
}

/** Paid-work proposals; accepting one opens task commerce, which needs a positive award. */
function PaidProposals({ opportunityId }: OpportunityBidsPanelProps) {
  const { data, isLoading, error } =
    api.opportunityBid.getByOpportunityId.useQuery({
      opportunityId,
    });
  const bids: OpportunityBid[] = data ?? [];

  return (
    <Card className="bg-card border-border">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Paid proposals</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {isLoading ? (
          <div className="text-muted-foreground flex items-center gap-2 text-sm">
            <Loader2 className="size-4 animate-spin" />
            Loading bids...
          </div>
        ) : error ? (
          <p className="text-destructive text-sm">Unable to load bids.</p>
        ) : bids.length ? (
          bids.map((bid) => (
            <div key={bid.id} className="border-border rounded-md border p-3">
              <div className="flex items-start justify-between gap-3">
                <div className="space-y-1">
                  <p className="text-sm whitespace-pre-wrap">{bid.proposal}</p>
                  {bid.proposedAmount != null ? (
                    <p className="text-muted-foreground text-xs">
                      Proposed amount: {bid.proposedAmount}
                    </p>
                  ) : null}
                </div>
                <Badge variant="outline">{bid.status}</Badge>
              </div>
              {actionableStatuses.has(bid.status) && !isPaid(bid) ? (
                <p className="text-muted-foreground mt-3 text-xs">
                  No award proposed, so this cannot start paid work. Unpaid
                  help joins through an application above.
                </p>
              ) : actionableStatuses.has(bid.status) ? (
                <div className="mt-3">
                  <BidAcceptButton
                    bidId={bid.id}
                    requiresAccountSetup
                  />
                </div>
              ) : null}
            </div>
          ))
        ) : (
          <p className="text-muted-foreground text-sm">No paid proposals yet.</p>
        )}
      </CardContent>
    </Card>
  );
}
