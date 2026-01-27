"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  Briefcase,
  Plus,
  Check,
  X,
  DollarSign,
  Clock,
  Loader2,
} from "lucide-react";

interface BidsTabProps {
  projectId: string;
  isOwner: boolean;
}

type BidStatus = "SUBMITTED" | "UNDER_REVIEW" | "ACCEPTED" | "REJECTED" | "WITHDRAWN";

const statusColors: Record<BidStatus, string> = {
  SUBMITTED: "bg-blue-500",
  UNDER_REVIEW: "bg-yellow-500",
  ACCEPTED: "bg-green-500",
  REJECTED: "bg-red-500",
  WITHDRAWN: "bg-gray-500",
};

export default function BidsTab({ projectId, isOwner }: BidsTabProps) {
  const [showBidForm, setShowBidForm] = useState(false);
  const [proposal, setProposal] = useState("");
  const [budget, setBudget] = useState("");
  const [timeline, setTimeline] = useState("");
  const [deliverables, setDeliverables] = useState("");

  const utils = api.useUtils();

  const { data: bids, isLoading } = api.project.getBids.useQuery({
    projectId,
  });

  const submitBid = api.project.submitBid.useMutation({
    onSuccess: () => {
      utils.project.getBids.invalidate({ projectId });
      setShowBidForm(false);
      setProposal("");
      setBudget("");
      setTimeline("");
      setDeliverables("");
    },
  });

  const reviewBid = api.project.reviewBid.useMutation({
    onSuccess: () => {
      utils.project.getBids.invalidate({ projectId });
    },
  });

  const handleSubmitBid = (e: React.FormEvent) => {
    e.preventDefault();

    // For now, show message about guild membership
    alert("Guild membership feature coming soon! You'll need to join or create a guild to submit bids.");
    return;

    // Future implementation:
    // submitBid.mutate({
    //   projectId,
    //   guildId: "user-guild-id",
    //   proposal,
    //   budget: parseFloat(budget),
    //   timeline: timeline || undefined,
    //   deliverables: deliverables || undefined,
    // });
  };

  const handleReviewBid = (bidId: string, status: "ACCEPTED" | "REJECTED") => {
    reviewBid.mutate({ bidId, status });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Briefcase className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">
            Bids {bids && bids.length > 0 && `(${bids.length})`}
          </h2>
        </div>
        {!isOwner && (
          <Button
            onClick={() => setShowBidForm(!showBidForm)}
            variant={showBidForm ? "outline" : "default"}
          >
            <Plus className="mr-2 h-4 w-4" />
            {showBidForm ? "Cancel" : "Submit Bid"}
          </Button>
        )}
      </div>

      {/* Bid Submission Form */}
      {showBidForm && !isOwner && (
        <Card className="border-2 border-primary/20">
          <CardContent className="pt-6">
            <form onSubmit={handleSubmitBid} className="space-y-4">
              <div>
                <label className="mb-2 block text-sm font-medium">
                  Proposal *
                </label>
                <textarea
                  value={proposal}
                  onChange={(e) => setProposal(e.target.value)}
                  className="min-h-32 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  placeholder="Describe your approach to this project..."
                  required
                />
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-medium">
                    <DollarSign className="mr-1 inline-block h-4 w-4" />
                    Budget *
                  </label>
                  <input
                    type="number"
                    value={budget}
                    onChange={(e) => setBudget(e.target.value)}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    placeholder="5000"
                    required
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">
                    <Clock className="mr-1 inline-block h-4 w-4" />
                    Timeline
                  </label>
                  <input
                    type="text"
                    value={timeline}
                    onChange={(e) => setTimeline(e.target.value)}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    placeholder="4 weeks"
                  />
                </div>
              </div>

              <div>
                <label className="mb-2 block text-sm font-medium">
                  Deliverables
                </label>
                <textarea
                  value={deliverables}
                  onChange={(e) => setDeliverables(e.target.value)}
                  className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  placeholder="List key deliverables..."
                />
              </div>

              <div className="rounded-md bg-muted p-4 text-sm text-muted-foreground">
                <p className="font-medium">Guild Membership Required</p>
                <p className="mt-1">
                  You need to be a member of a guild to submit bids. Guild
                  functionality is coming soon!
                </p>
              </div>

              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setShowBidForm(false)}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={submitBid.isPending}>
                  {submitBid.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  Submit Bid
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Bids List */}
      {bids && bids.length === 0 ? (
        <div className="rounded-lg border-2 border-dashed py-12 text-center">
          <Briefcase className="mx-auto h-12 w-12 text-muted-foreground/50" />
          <p className="mt-4 text-lg font-medium text-muted-foreground">
            No bids yet
          </p>
          <p className="mt-1 text-sm text-muted-foreground">
            {isOwner
              ? "Waiting for guilds to submit their proposals"
              : "Be the first to submit a bid for this project"}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {bids?.map((bid: any) => (
            <Card key={bid.id}>
              <CardContent className="pt-6">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex gap-4">
                    <Avatar className="h-12 w-12">
                      <AvatarImage src={bid.guild?.avatarUrl} />
                      <AvatarFallback>
                        {bid.guild?.name?.substring(0, 2).toUpperCase() ?? "GU"}
                      </AvatarFallback>
                    </Avatar>

                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold">
                          {bid.guild?.name ?? "Guild"}
                        </h3>
                        <Badge
                          className={`${statusColors[bid.status as BidStatus]} text-white`}
                        >
                          {bid.status}
                        </Badge>
                      </div>

                      <div className="mt-2 flex items-center gap-4 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <DollarSign className="h-4 w-4" />
                          <span className="font-medium">
                            {bid.budget?.toLocaleString() ?? "N/A"}
                          </span>
                        </div>
                        {bid.timeline && (
                          <div className="flex items-center gap-1">
                            <Clock className="h-4 w-4" />
                            <span>{bid.timeline}</span>
                          </div>
                        )}
                      </div>

                      <div className="mt-4">
                        <p className="text-sm font-medium">Proposal</p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          {bid.proposal}
                        </p>
                      </div>

                      {bid.deliverables && (
                        <div className="mt-3">
                          <p className="text-sm font-medium">Deliverables</p>
                          <p className="mt-1 text-sm text-muted-foreground">
                            {bid.deliverables}
                          </p>
                        </div>
                      )}

                      <div className="mt-3 text-xs text-muted-foreground">
                        Submitted{" "}
                        {new Date(bid.createdAt).toLocaleDateString("en-US", {
                          month: "short",
                          day: "numeric",
                          year: "numeric",
                        })}
                      </div>
                    </div>
                  </div>

                  {/* Owner Review Actions */}
                  {isOwner && bid.status === "SUBMITTED" && (
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        className="border-green-500 text-green-600 hover:bg-green-50"
                        onClick={() => handleReviewBid(bid.id, "ACCEPTED")}
                        disabled={reviewBid.isPending}
                      >
                        <Check className="mr-1 h-4 w-4" />
                        Accept
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="border-red-500 text-red-600 hover:bg-red-50"
                        onClick={() => handleReviewBid(bid.id, "REJECTED")}
                        disabled={reviewBid.isPending}
                      >
                        <X className="mr-1 h-4 w-4" />
                        Reject
                      </Button>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
