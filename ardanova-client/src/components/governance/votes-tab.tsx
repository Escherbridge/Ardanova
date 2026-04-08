"use client";

import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { Loader2, ThumbsUp, ThumbsDown, MinusCircle, MessageSquare } from "lucide-react";

interface VotesTabProps {
  proposalId: string;
}

interface Vote {
  id: string;
  proposalId: string;
  voterId: string;
  choice: number; // 0=For, 1=Against, 2=Abstain
  weight: number;
  reason?: string;
  txHash?: string;
  createdAt: string;
  voter?: {
    id: string;
    name?: string;
    image?: string;
  };
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
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!votes || votes.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-muted mb-4">
          <ThumbsUp className="size-8 text-muted-foreground" />
        </div>
        <h3 className="text-lg font-semibold mb-2">No votes yet</h3>
        <p className="text-muted-foreground text-sm">
          Be the first to vote on this proposal
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
              <ThumbsUp className="size-4 text-neon-purple" />
            </div>
            All Votes ({votes.length})
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {votes.map((vote) => (
              <div
                key={vote.id}
                className="flex gap-4 p-4 border border-border rounded-lg hover:bg-muted/50 transition-colors"
              >
                {/* Avatar */}
                <Avatar className="size-12 flex-shrink-0">
                  <AvatarImage src={vote.voter?.image ?? undefined} />
                  <AvatarFallback className="text-base">
                    {vote.voter?.name?.charAt(0).toUpperCase() ?? "U"}
                  </AvatarFallback>
                </Avatar>

                {/* Vote Content */}
                <div className="flex-1 min-w-0 space-y-2">
                  {/* Header Row */}
                  <div className="flex items-center justify-between gap-3 flex-wrap">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-foreground">
                        {vote.voter?.name ?? "Unknown Voter"}
                      </span>
                      <Badge variant={getVoteBadgeVariant(vote.choice)} className="flex items-center gap-1">
                        <VoteIcon choice={vote.choice} />
                        {getVoteLabel(vote.choice)}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-3 text-sm text-muted-foreground">
                      <span>Weight: {vote.weight}</span>
                      <span>•</span>
                      <span>{new Date(vote.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>

                  {/* Vote Reason */}
                  {vote.reason && (
                    <div className="bg-muted/30 border border-border/50 rounded-md p-3">
                      <div className="flex items-start gap-2">
                        <MessageSquare className="size-4 text-muted-foreground mt-0.5 flex-shrink-0" />
                        <p className="text-sm text-foreground leading-relaxed">
                          {vote.reason}
                        </p>
                      </div>
                    </div>
                  )}

                  {/* Transaction Hash */}
                  {vote.txHash && (
                    <div className="text-xs text-muted-foreground font-mono">
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
