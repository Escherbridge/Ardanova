"use client";

import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import {
  Loader2,
  ThumbsUp,
  ThumbsDown,
  MinusCircle,
  MessageSquare,
} from "lucide-react";

interface VotesTabProps {
  proposalId: string;
}

// Get badge variant based on vote choice
const getVoteBadgeVariant = (choice: number) => {
  switch (choice) {
    case 0: // For
      return "neon-green" as const;
    case 1: // Against
      return "destructive" as const;
    case 2: // Abstain
      return "secondary" as const;
    default:
      return "default" as const;
  }
};

// Get vote label based on choice
const getVoteLabel = (choice: number): string => {
  switch (choice) {
    case 0:
      return "For";
    case 1:
      return "Against";
    case 2:
      return "Abstain";
    default:
      return "Unknown";
  }
};

// Get vote icon based on choice
const VoteIcon = ({ choice }: { choice: number }) => {
  switch (choice) {
    case 0:
      return <ThumbsUp className="size-4" />;
    case 1:
      return <ThumbsDown className="size-4" />;
    case 2:
      return <MinusCircle className="size-4" />;
    default:
      return null;
  }
};

export default function VotesTab({ proposalId }: VotesTabProps) {
  const { data: votes, isLoading } = api.governance.getVotes.useQuery({
    proposalId,
    limit: 50,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-muted-foreground size-6 animate-spin" />
      </div>
    );
  }

  if (!votes || votes.length === 0) {
    return (
      <div className="py-12 text-center">
        <div className="bg-muted mb-4 inline-flex h-16 w-16 items-center justify-center rounded-none">
          <ThumbsUp className="text-muted-foreground size-8" />
        </div>
        <h3 className="mb-2 text-lg font-semibold">No votes yet</h3>
        <p className="text-muted-foreground text-sm">
          Be the first to vote on this proposal
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-neon-purple/20 border-neon-purple/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <ThumbsUp className="text-neon-purple size-4" />
            </div>
            All Votes ({votes.length})
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {votes.map((vote) => (
              <div
                key={vote.id}
                className="border-border hover:bg-muted/50 flex gap-4 rounded-lg border p-4 transition-colors"
              >
                {/* Avatar */}
                <Avatar className="size-12 flex-shrink-0">
                  <AvatarImage src={vote.voter?.image ?? undefined} />
                  <AvatarFallback className="text-base">
                    {vote.voter?.name?.charAt(0).toUpperCase() ?? "U"}
                  </AvatarFallback>
                </Avatar>

                {/* Vote Content */}
                <div className="min-w-0 flex-1 space-y-2">
                  {/* Header Row */}
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="flex items-center gap-2">
                      <span className="text-foreground font-medium">
                        {vote.voter?.name ?? "Unknown Voter"}
                      </span>
                      <Badge
                        variant={getVoteBadgeVariant(vote.choice)}
                        className="flex items-center gap-1"
                      >
                        <VoteIcon choice={vote.choice} />
                        {getVoteLabel(vote.choice)}
                      </Badge>
                    </div>
                    <div className="text-muted-foreground flex items-center gap-3 text-sm">
                      <span>Weight: {vote.weight}</span>
                      <span>•</span>
                      <span>
                        {new Date(vote.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  </div>

                  {/* Vote Reason */}
                  {vote.reason && (
                    <div className="bg-muted/30 border-border/50 rounded-md border p-3">
                      <div className="flex items-start gap-2">
                        <MessageSquare className="text-muted-foreground mt-0.5 size-4 flex-shrink-0" />
                        <p className="text-foreground text-sm leading-relaxed">
                          {vote.reason}
                        </p>
                      </div>
                    </div>
                  )}

                  {/* Transaction Hash */}
                  {vote.txHash && (
                    <div className="text-muted-foreground font-mono text-xs">
                      TX: {vote.txHash.slice(0, 10)}...{vote.txHash.slice(-8)}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
