"use client";

import { Loader2 } from "lucide-react";

import { BidAcceptButton } from "~/components/opportunities/bid-accept-button";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import type { OpportunityBid } from "~/lib/api/ardanova/endpoints/opportunity-bids";
import { api } from "~/trpc/react";

interface OpportunityBidsPanelProps {
  opportunityId: string;
}

const actionableStatuses = new Set(["SUBMITTED", "UNDER_REVIEW"]);

/** Shows an opportunity owner the bids eligible for the server-owned acceptance flow. */
export function OpportunityBidsPanel({ opportunityId }: OpportunityBidsPanelProps) {
  const { data, isLoading, error } = api.opportunityBid.getByOpportunityId.useQuery({
    opportunityId,
  });
  const bids = (data ?? []) as OpportunityBid[];

  return (
    <Card className="bg-card border-border">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Bids</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {isLoading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" />
            Loading bids...
          </div>
        ) : error ? (
          <p className="text-sm text-destructive">Unable to load bids.</p>
        ) : bids.length ? (
          bids.map((bid) => (
            <div key={bid.id} className="rounded-md border border-border p-3">
              <div className="flex items-start justify-between gap-3">
                <div className="space-y-1">
                  <p className="text-sm whitespace-pre-wrap">{bid.proposal}</p>
                  {bid.proposedAmount != null ? (
                    <p className="text-xs text-muted-foreground">
                      Proposed amount: {bid.proposedAmount}
                    </p>
                  ) : null}
                </div>
                <Badge variant="outline">{bid.status}</Badge>
              </div>
              {actionableStatuses.has(bid.status) ? (
                <div className="mt-3">
                  <BidAcceptButton bidId={bid.id} />
                </div>
              ) : null}
            </div>
          ))
        ) : (
          <p className="text-sm text-muted-foreground">No bids yet.</p>
        )}
      </CardContent>
    </Card>
  );
}
