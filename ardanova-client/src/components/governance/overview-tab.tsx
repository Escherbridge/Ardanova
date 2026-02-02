"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { FileText, Scale, ThumbsUp, ThumbsDown, Vote, Timer, Loader2 } from "lucide-react";
import { api } from "~/trpc/react";

interface OverviewTabProps {
  proposal: {
    id: string;
    description: string;
    options: string;
    quorum: number;
    threshold: number;
    votingStart?: string;
    votingEnd?: string;
    executionDelay?: number;
    status: string;
    votesCount: number;
    totalVotingPower: number;
    createdAt: string;
  };
  isCreator: boolean;
}

// Parse voting options from JSON string
function parseVotingOptions(optionsStr: string, votesCount: number): Array<{label: string; choice: number; votes: number; percentage: number}> {
  try {
    const parsed = JSON.parse(optionsStr);
    if (Array.isArray(parsed)) {
      const totalVotes = votesCount || 1;
      return parsed.map((opt: any, idx: number) => ({
        label: opt.label || `Option ${idx + 1}`,
        choice: opt.choice ?? idx,
        votes: opt.votes || 0,
        percentage: Math.round(((opt.votes || 0) / totalVotes) * 100) || 0,
      }));
    }
  } catch {
    // Fallback to default options
  }
  return [
    { label: "For", choice: 0, votes: 0, percentage: 0 },
    { label: "Against", choice: 1, votes: 0, percentage: 0 },
    { label: "Abstain", choice: 2, votes: 0, percentage: 0 },
  ];
}

function mapChoiceToVote(choice: number): "for" | "against" | "abstain" {
  const mapping: Record<number, "for" | "against" | "abstain"> = {
    0: "for",
    1: "against",
    2: "abstain",
  };
  return mapping[choice] ?? "abstain";
}

export default function OverviewTab({ proposal, isCreator }: OverviewTabProps) {
  const [selectedVote, setSelectedVote] = useState<number | null>(null);
  const [voteReason, setVoteReason] = useState("");

  const utils = api.useUtils();

  const voteMutation = api.governance.vote.useMutation({
    onSuccess: () => {
      setSelectedVote(null);
      setVoteReason("");
      void utils.governance.getById.invalidate({ id: proposal.id });
    },
    onError: (error) => {
      alert(error.message);
    },
  });

  const votingOptions = parseVotingOptions(proposal.options, proposal.votesCount);
  const isActive = proposal.status === "Active";

  // Calculate quorum progress
  const currentQuorum = proposal.totalVotingPower > 0
    ? (proposal.votesCount / proposal.totalVotingPower) * 100
    : 0;

  const handleVote = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedVote === null) return;

    voteMutation.mutate({
      proposalId: proposal.id,
      vote: mapChoiceToVote(selectedVote),
      reason: voteReason || undefined,
    });
  };

  return (
    <div className="space-y-6">
      {/* Description */}
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
              <FileText className="size-4 text-primary" />
            </div>
            Description
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {proposal.description}
          </p>
        </CardContent>
      </Card>

      {/* Voting Results */}
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
              <Vote className="size-4 text-neon-purple" />
            </div>
            Voting Results
          </CardTitle>
        </CardHeader>
        <CardContent>
          {/* Quorum Progress */}
          <div className="mb-6">
            <div className="flex justify-between text-sm mb-2">
              <span className="text-muted-foreground">Quorum Progress</span>
              <span className="font-medium text-foreground">
                {currentQuorum.toFixed(1)}% of {proposal.quorum}% required
              </span>
            </div>
            <Progress
              value={Math.min(currentQuorum, 100)}
              variant={currentQuorum >= proposal.quorum ? "success" : "neon"}
            />
          </div>

          {/* Vote Breakdown */}
          <div className="space-y-4">
            {votingOptions.map((option, i) => (
              <div key={i} className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {option.label === "For" && <ThumbsUp className="size-4 text-neon-green" />}
                    {option.label === "Against" && <ThumbsDown className="size-4 text-destructive" />}
                    <span className="font-medium text-foreground">{option.label}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground">{option.votes} votes</span>
                    <span className="text-lg font-bold text-foreground">{option.percentage}%</span>
                  </div>
                </div>
                <Progress
                  value={option.percentage}
                  variant={option.label === "For" ? "success" : option.label === "Against" ? "warning" : "default"}
                  className="h-3"
                />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Proposal Details */}
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
              <Scale className="size-4 text-warning" />
            </div>
            Proposal Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Quorum Required</span>
              <span className="font-medium text-foreground">{proposal.quorum}%</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Passing Threshold</span>
              <span className="font-medium text-foreground">{proposal.threshold}%</span>
            </div>
            {proposal.votingStart && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Voting Started</span>
                <span className="font-medium text-foreground">
                  {new Date(proposal.votingStart).toLocaleDateString()}
                </span>
              </div>
            )}
            {proposal.votingEnd && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Voting Ends</span>
                <span className="font-medium text-foreground">
                  {new Date(proposal.votingEnd).toLocaleDateString()}
                </span>
              </div>
            )}
            {proposal.executionDelay && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Execution Delay</span>
                <span className="font-medium text-foreground">{proposal.executionDelay} hours</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">Created</span>
              <span className="font-medium text-foreground">
                {new Date(proposal.createdAt).toLocaleDateString()}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Cast Vote Section */}
      {isActive && (
        <Card className="bg-card border-2 border-primary">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
                <Vote className="size-4 text-neon-green" />
              </div>
              Cast Your Vote
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleVote} className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Select your voting choice and optionally provide a reason. Your vote is final and cannot be changed.
              </p>

              {/* Vote Options */}
              <div className="flex flex-wrap gap-3">
                {votingOptions.map((option, i) => (
                  <Button
                    key={i}
                    type="button"
                    variant={
                      selectedVote === option.choice
                        ? option.label === "For"
                          ? "neon-green"
                          : option.label === "Against"
                          ? "destructive"
                          : "secondary"
                        : "outline"
                    }
                    className="flex-1 min-w-[150px]"
                    onClick={() => setSelectedVote(option.choice)}
                  >
                    {option.label === "For" && <ThumbsUp className="size-4 mr-2" />}
                    {option.label === "Against" && <ThumbsDown className="size-4 mr-2" />}
                    {option.label}
                  </Button>
                ))}
              </div>

              {/* Reason (Optional) */}
              {selectedVote !== null && (
                <div>
                  <label htmlFor="voteReason" className="text-sm font-medium block mb-1.5">
                    Reason (Optional)
                  </label>
                  <textarea
                    id="voteReason"
                    value={voteReason}
                    onChange={(e) => setVoteReason(e.target.value)}
                    placeholder="Share why you voted this way..."
                    className="w-full px-3 py-2 border border-border rounded bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[80px] resize-y"
                  />
                </div>
              )}

              {/* Submit Button */}
              {selectedVote !== null && (
                <Button type="submit" variant="neon" className="w-full" disabled={voteMutation.isPending}>
                  {voteMutation.isPending ? (
                    <>
                      <Loader2 className="size-4 mr-2 animate-spin" />
                      Submitting Vote...
                    </>
                  ) : (
                    <>
                      <Vote className="size-4 mr-2" />
                      Submit Vote: {votingOptions.find(o => o.choice === selectedVote)?.label}
                    </>
                  )}
                </Button>
              )}
            </form>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
