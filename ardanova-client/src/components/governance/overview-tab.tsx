"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Progress } from "~/components/ui/progress";
import {
  FileText,
  Scale,
  ThumbsUp,
  ThumbsDown,
  Vote,
  Loader2,
} from "lucide-react";
import { api } from "~/trpc/react";
import { quorumProgressPercent, readProposalOptions } from "./proposal-tally";

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

export default function OverviewTab({
  proposal,
  isCreator: _isCreator,
}: OverviewTabProps) {
  const [selectedVote, setSelectedVote] = useState<number | null>(null);
  const [voteReason, setVoteReason] = useState("");

  const utils = api.useUtils();

  const voteMutation = api.governance.vote.useMutation({
    onSuccess: () => {
      setSelectedVote(null);
      setVoteReason("");
      void utils.governance.getById.invalidate({ id: proposal.id });
      void utils.governance.getVoteSummary.invalidate({
        proposalId: proposal.id,
      });
    },
    onError: (error) => {
      alert(describeMutationError(error, "record your vote"));
    },
  });

  // The API serialises enums by name (JsonStringEnumConverter), so the wire value is "ACTIVE".
  const isActive = proposal.status.toUpperCase() === "ACTIVE";

  // Quorum is turnout among the project's confirmed members, not a share of any voting power:
  // the summary endpoint owns that arithmetic so the page and the tally can never disagree.
  const { data: voteSummary } = api.governance.getVoteSummary.useQuery({
    proposalId: proposal.id,
  });
  const eligibleMembers = voteSummary?.eligibleMembers ?? 0;
  const votesCast = voteSummary?.totalVotes ?? 0;
  const votesNeededForQuorum = voteSummary?.votesNeededForQuorum ?? 0;
  const quorumProgress = quorumProgressPercent(votesCast, votesNeededForQuorum);
  const quorumReached = voteSummary?.quorumReached ?? false;

  // Labels are the proposal's own options; counts are the API tally per choice number.
  const votesByChoice = Object.fromEntries(
    Object.entries(voteSummary?.optionSummaries ?? {}).map(([choice, entry]) => [
      choice,
      entry.voteCount,
    ]),
  );
  const votingOptions = readProposalOptions(proposal.options, votesByChoice);

  const handleVote = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedVote === null) return;

    voteMutation.mutate({
      proposalId: proposal.id,
      choice: selectedVote,
      reason: voteReason || undefined,
    });
  };

  return (
    <div className="space-y-6">
      {/* Description */}
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <FileText className="text-primary size-4" />
            </div>
            Description
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground leading-relaxed whitespace-pre-wrap">
            {proposal.description}
          </p>
        </CardContent>
      </Card>

      {/* Voting Results */}
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-neon-purple/20 border-neon-purple/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <Vote className="text-neon-purple size-4" />
            </div>
            Voting Results
          </CardTitle>
        </CardHeader>
        <CardContent>
          {/* Quorum Progress */}
          <div className="mb-6">
            <div className="mb-2 flex justify-between text-sm">
              <span className="text-muted-foreground">Quorum Progress</span>
              <span className="text-foreground font-medium">
                {eligibleMembers > 0
                  ? `${votesCast} of ${votesNeededForQuorum} members needed`
                  : "No confirmed members yet"}
              </span>
            </div>
            <Progress
              aria-label="Quorum progress"
              value={quorumProgress}
              variant={quorumReached ? "success" : "neon"}
            />
          </div>

          {/* Vote Breakdown */}
          <div className="space-y-4">
            {votingOptions.length === 0 && (
              <p className="text-muted-foreground text-sm">
                This proposal&apos;s options can&apos;t be read, so no votes can
                be recorded against it.
              </p>
            )}
            {votingOptions.map((option, i) => (
              <div key={i} className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {option.label === "For" && (
                      <ThumbsUp className="text-neon-green size-4" />
                    )}
                    {option.label === "Against" && (
                      <ThumbsDown className="text-destructive size-4" />
                    )}
                    <span className="text-foreground font-medium">
                      {option.label}
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-muted-foreground text-sm">
                      {option.votes} votes
                    </span>
                    <span className="text-foreground text-lg font-bold">
                      {option.percentage}%
                    </span>
                  </div>
                </div>
                <Progress
                  aria-label={`${option.label} vote result`}
                  value={option.percentage}
                  variant={
                    option.label === "For"
                      ? "success"
                      : option.label === "Against"
                        ? "warning"
                        : "default"
                  }
                  className="h-3"
                />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Proposal Details */}
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-warning/20 border-warning/30 flex h-8 w-8 items-center justify-center rounded-lg border">
              <Scale className="text-warning size-4" />
            </div>
            Proposal Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Quorum Required</span>
              <span className="text-foreground font-medium">
                {proposal.quorum}%
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Passing Threshold</span>
              <span className="text-foreground font-medium">
                {proposal.threshold}%
              </span>
            </div>
            {proposal.votingStart && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Voting Started</span>
                <span className="text-foreground font-medium">
                  {new Date(proposal.votingStart).toLocaleDateString()}
                </span>
              </div>
            )}
            {proposal.votingEnd && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Voting Ends</span>
                <span className="text-foreground font-medium">
                  {new Date(proposal.votingEnd).toLocaleDateString()}
                </span>
              </div>
            )}
            {proposal.executionDelay && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">Execution Delay</span>
                <span className="text-foreground font-medium">
                  {proposal.executionDelay} hours
                </span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">Created</span>
              <span className="text-foreground font-medium">
                {new Date(proposal.createdAt).toLocaleDateString()}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Cast Vote Section */}
      {isActive && (
        <Card className="bg-card border-primary border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-neon-green/20 border-neon-green/30 flex h-8 w-8 items-center justify-center rounded-lg border">
                <Vote className="text-neon-green size-4" />
              </div>
              Cast Your Vote
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleVote} className="space-y-4">
              <p className="text-muted-foreground text-sm">
                Select your voting choice and optionally provide a reason. Your
                vote is final and cannot be changed.
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
                    className="min-w-[150px] flex-1"
                    onClick={() => setSelectedVote(option.choice)}
                  >
                    {option.label === "For" && (
                      <ThumbsUp className="mr-2 size-4" />
                    )}
                    {option.label === "Against" && (
                      <ThumbsDown className="mr-2 size-4" />
                    )}
                    {option.label}
                  </Button>
                ))}
              </div>

              {/* Reason (Optional) */}
              {selectedVote !== null && (
                <div>
                  <label
                    htmlFor="voteReason"
                    className="mb-1.5 block text-sm font-medium"
                  >
                    Reason (Optional)
                  </label>
                  <textarea
                    id="voteReason"
                    value={voteReason}
                    onChange={(e) => setVoteReason(e.target.value)}
                    placeholder="Share why you voted this way..."
                    className="border-border bg-background text-foreground focus:ring-primary min-h-[80px] w-full resize-y rounded border px-3 py-2 focus:ring-2 focus:outline-none"
                  />
                </div>
              )}

              {/* Submit Button */}
              {selectedVote !== null && (
                <Button
                  type="submit"
                  variant="neon"
                  className="w-full"
                  disabled={voteMutation.isPending}
                >
                  {voteMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 size-4 animate-spin" />
                      Submitting Vote...
                    </>
                  ) : (
                    <>
                      <Vote className="mr-2 size-4" />
                      Submit Vote:{" "}
                      {
                        votingOptions.find((o) => o.choice === selectedVote)
                          ?.label
                      }
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
