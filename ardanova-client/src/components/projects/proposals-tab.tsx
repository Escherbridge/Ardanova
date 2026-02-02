"use client";

import { useState, useEffect, useRef } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Textarea } from "~/components/ui/textarea";
import { Vote, Plus, Check, X, Loader2, Clock, Users, MessageSquare, ChevronDown, ChevronUp, AlertCircle } from "lucide-react";

interface ProposalsTabProps {
  projectId: string;
  isOwner: boolean;
  isMember: boolean;
  selectedProposalId?: string;
}

type ProposalType = "TREASURY" | "GOVERNANCE" | "STRATEGIC" | "OPERATIONAL" | "EMERGENCY";

export default function ProposalsTab({ projectId, isOwner, isMember, selectedProposalId }: ProposalsTabProps) {
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [expandedProposalId, setExpandedProposalId] = useState<string | null>(selectedProposalId || null);
  const [voteReasons, setVoteReasons] = useState<Record<string, string>>({});
  const [showPreview, setShowPreview] = useState(false);
  const [formData, setFormData] = useState({
    type: "GOVERNANCE" as ProposalType,
    title: "",
    description: "",
    options: ["Yes", "No"],
    quorum: 50,
    threshold: 66,
    votingDays: 7,
  });

  const selectedProposalRef = useRef<HTMLDivElement>(null);
  const utils = api.useUtils();

  // Auto-expand and scroll to selected proposal
  useEffect(() => {
    if (selectedProposalId) {
      setExpandedProposalId(selectedProposalId);
      setTimeout(() => {
        selectedProposalRef.current?.scrollIntoView({
          behavior: 'smooth',
          block: 'center'
        });
      }, 100);
    }
  }, [selectedProposalId]);

  const { data: proposals, isLoading, error } = api.project.getProposals.useQuery(
    { projectId },
    { enabled: !!projectId }
  );

  const createMutation = api.project.createProposal.useMutation({
    onSuccess: () => {
      utils.project.getProposals.invalidate({ projectId });
      setShowCreateForm(false);
      setFormData({
        type: "GOVERNANCE",
        title: "",
        description: "",
        options: ["Yes", "No"],
        quorum: 50,
        threshold: 66,
        votingDays: 7,
      });
    },
  });

  const voteMutation = api.project.castVote.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      const { proposalId, choice } = variables;
      await utils.project.getProposals.cancel({ projectId });
      const previous = utils.project.getProposals.getData({ projectId });
      utils.project.getProposals.setData({ projectId }, (old) =>
        old?.map(p => p.id === proposalId ? {
          ...p,
          votesFor: choice === 0 ? (p.votesFor || 0) + 1 : p.votesFor,
          votesAgainst: choice === 1 ? (p.votesAgainst || 0) + 1 : p.votesAgainst,
          userVoted: true
        } : p)
      );
      return { previous };
    },
    onSuccess: () => {
      // Clear the vote reason after successful vote
      setVoteReasons({});
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getProposals.setData({ projectId }, context.previous);
      }
    },
    onSettled: () => {
      utils.project.getProposals.invalidate({ projectId });
    },
  });

  const closeMutation = api.project.closeProposal.useMutation({
    onSuccess: () => {
      utils.project.getProposals.invalidate({ projectId });
    },
  });

  const handleCreateProposal = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate({
      projectId,
      ...formData,
    });
  };

  const handleVote = (proposalId: string, choice: number) => {
    const reason = voteReasons[proposalId];
    voteMutation.mutate({ proposalId, choice, reason });
  };

  const toggleProposalExpand = (proposalId: string) => {
    setExpandedProposalId(expandedProposalId === proposalId ? null : proposalId);
  };

  const calculateTimeRemaining = (endDate: string | Date) => {
    const now = new Date();
    const end = new Date(endDate);
    const diff = end.getTime() - now.getTime();

    if (diff <= 0) return "Voting ended";

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));

    if (days > 0) return `${days}d ${hours}h remaining`;
    return `${hours}h remaining`;
  };

  const calculateVotePercentages = (proposal: any) => {
    const total = (proposal.votesFor || 0) + (proposal.votesAgainst || 0) + (proposal.abstainVotes || 0);
    if (total === 0) return { forPct: 0, againstPct: 0, abstainPct: 0 };

    return {
      forPct: Math.round(((proposal.votesFor || 0) / total) * 100),
      againstPct: Math.round(((proposal.votesAgainst || 0) / total) * 100),
      abstainPct: Math.round(((proposal.abstainVotes || 0) / total) * 100),
    };
  };

  const calculateQuorumProgress = (proposal: any) => {
    const totalVotes = (proposal.votesFor || 0) + (proposal.votesAgainst || 0) + (proposal.abstainVotes || 0);
    const totalMembers = proposal.totalMembers || 100; // Fallback if not provided
    const quorumNeeded = Math.ceil((totalMembers * (proposal.quorum || 50)) / 100);
    const progress = Math.min((totalVotes / quorumNeeded) * 100, 100);

    return {
      progress,
      votesNeeded: Math.max(quorumNeeded - totalVotes, 0),
      totalVotes,
      quorumNeeded
    };
  };

  const handleCloseProposal = (proposalId: string) => {
    closeMutation.mutate({ proposalId });
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: "default" | "secondary" | "destructive" | "outline", label: string }> = {
      ACTIVE: { variant: "default", label: "Active" },
      PASSED: { variant: "secondary", label: "Passed" },
      REJECTED: { variant: "destructive", label: "Rejected" },
      CLOSED: { variant: "outline", label: "Closed" },
    };
    const config = variants[status] || { variant: "outline" as const, label: status };
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  const getTypeBadge = (type: string) => {
    const colors: Record<string, string> = {
      TREASURY: "bg-green-100 text-green-800",
      GOVERNANCE: "bg-blue-100 text-blue-800",
      STRATEGIC: "bg-purple-100 text-purple-800",
      OPERATIONAL: "bg-orange-100 text-orange-800",
      EMERGENCY: "bg-red-100 text-red-800",
    };
    return (
      <Badge className={colors[type] || "bg-gray-100 text-gray-800"}>
        {type}
      </Badge>
    );
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
        Error loading proposals: {error.message}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {isMember && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <Vote className="h-5 w-5" />
                Create New Proposal
              </CardTitle>
              <Button
                onClick={() => setShowCreateForm(!showCreateForm)}
                variant="outline"
                size="sm"
              >
                {showCreateForm ? (
                  <X className="h-4 w-4" />
                ) : (
                  <Plus className="h-4 w-4" />
                )}
              </Button>
            </div>
          </CardHeader>
          {showCreateForm && (
            <CardContent>
              <form onSubmit={handleCreateProposal} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Type
                  </label>
                  <select
                    value={formData.type}
                    onChange={(e) => setFormData({ ...formData, type: e.target.value as ProposalType })}
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    required
                  >
                    <option value="GOVERNANCE">Governance</option>
                    <option value="TREASURY">Treasury</option>
                    <option value="STRATEGIC">Strategic</option>
                    <option value="OPERATIONAL">Operational</option>
                    <option value="EMERGENCY">Emergency</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Title
                  </label>
                  <input
                    type="text"
                    value={formData.title}
                    onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full rounded-md border border-gray-300 px-3 py-2"
                    rows={4}
                    required
                  />
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Quorum (%)
                    </label>
                    <input
                      type="number"
                      min="1"
                      max="100"
                      value={formData.quorum}
                      onChange={(e) => setFormData({ ...formData, quorum: Number(e.target.value) })}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      required
                    />
                    <p className="text-xs text-gray-500 mt-1">Minimum participation required</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Threshold (%)
                    </label>
                    <input
                      type="number"
                      min="1"
                      max="100"
                      value={formData.threshold}
                      onChange={(e) => setFormData({ ...formData, threshold: Number(e.target.value) })}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      required
                    />
                    <p className="text-xs text-gray-500 mt-1">Votes needed to pass</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Voting Days
                    </label>
                    <input
                      type="number"
                      min="1"
                      max="30"
                      value={formData.votingDays}
                      onChange={(e) => setFormData({ ...formData, votingDays: Number(e.target.value) })}
                      className="w-full rounded-md border border-gray-300 px-3 py-2"
                      required
                    />
                    <p className="text-xs text-gray-500 mt-1">Duration for voting</p>
                  </div>
                </div>

                {showPreview && (
                  <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 space-y-2">
                    <h4 className="font-semibold text-blue-900">Preview</h4>
                    <div className="space-y-1 text-sm">
                      <p><span className="font-medium">Type:</span> {formData.type}</p>
                      <p><span className="font-medium">Title:</span> {formData.title || "(No title)"}</p>
                      <p><span className="font-medium">Description:</span> {formData.description || "(No description)"}</p>
                      <p><span className="font-medium">Options:</span> {formData.options.join(", ")}</p>
                      <p><span className="font-medium">Settings:</span> {formData.quorum}% quorum, {formData.threshold}% threshold, {formData.votingDays} days</p>
                    </div>
                  </div>
                )}

                <div className="flex gap-2">
                  <Button
                    type="button"
                    onClick={() => setShowPreview(!showPreview)}
                    variant="outline"
                    className="flex-1"
                  >
                    {showPreview ? "Hide Preview" : "Show Preview"}
                  </Button>
                  <Button type="submit" disabled={createMutation.isPending} className="flex-1">
                    {createMutation.isPending ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Creating...
                      </>
                    ) : (
                      "Create Proposal"
                    )}
                  </Button>
                </div>
                {createMutation.error && (
                  <div className="rounded-md bg-red-50 border border-red-200 p-3 flex items-start gap-2">
                    <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                    <p className="text-sm text-red-800">{createMutation.error.message}</p>
                  </div>
                )}
              </form>
            </CardContent>
          )}
        </Card>
      )}

      {!proposals || !Array.isArray(proposals) || proposals.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Vote className="mx-auto h-12 w-12 text-gray-400 mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No proposals yet</h3>
            <p className="text-gray-600">
              {isMember
                ? "Create the first proposal to get started with project governance."
                : "No proposals have been created for this project yet."}
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {proposals.map((proposal) => {
            const isExpanded = expandedProposalId === proposal.id;
            const isSelected = selectedProposalId === proposal.id;
            const { forPct, againstPct, abstainPct } = calculateVotePercentages(proposal);
            const quorumInfo = calculateQuorumProgress(proposal);

            return (
              <Card
                key={proposal.id}
                ref={isSelected ? selectedProposalRef : null}
                className={isSelected ? "ring-2 ring-blue-500" : ""}
              >
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="space-y-2 flex-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        {getTypeBadge(proposal.type)}
                        {getStatusBadge(proposal.status)}
                        {proposal.status === "ACTIVE" && proposal.votingEndsAt && (
                          <Badge variant="outline" className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {calculateTimeRemaining(proposal.votingEndsAt)}
                          </Badge>
                        )}
                      </div>
                      <div className="flex items-center justify-between gap-4">
                        <CardTitle className="flex-1">{proposal.title}</CardTitle>
                        <Button
                          onClick={() => toggleProposalExpand(proposal.id)}
                          variant="ghost"
                          size="sm"
                        >
                          {isExpanded ? (
                            <ChevronUp className="h-4 w-4" />
                          ) : (
                            <ChevronDown className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    </div>
                    {isOwner && proposal.status === "ACTIVE" && (
                      <Button
                        onClick={() => handleCloseProposal(proposal.id)}
                        disabled={closeMutation.isPending}
                        variant="outline"
                        size="sm"
                        className="ml-2"
                      >
                        {closeMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          "Close"
                        )}
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  {!isExpanded ? (
                    <>
                      <p className="text-gray-700 line-clamp-2">{proposal.description}</p>
                      <div className="flex items-center gap-4 text-sm text-gray-600">
                        <div className="flex items-center gap-1">
                          <Check className="h-4 w-4 text-green-600" />
                          <span>{proposal.votesFor || 0} ({forPct}%)</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <X className="h-4 w-4 text-red-600" />
                          <span>{proposal.votesAgainst || 0} ({againstPct}%)</span>
                        </div>
                        {(proposal.abstainVotes || 0) > 0 && (
                          <div className="flex items-center gap-1">
                            <span>{proposal.abstainVotes} ({abstainPct}%) abstained</span>
                          </div>
                        )}
                      </div>
                    </>
                  ) : (
                    <Tabs defaultValue="details" className="w-full">
                      <TabsList className="grid w-full grid-cols-2">
                        <TabsTrigger value="details">Details</TabsTrigger>
                        <TabsTrigger value="discussion">
                          <MessageSquare className="h-4 w-4 mr-1" />
                          Discussion
                        </TabsTrigger>
                      </TabsList>

                      <TabsContent value="details" className="space-y-4 mt-4">
                        <div>
                          <h4 className="font-semibold mb-2">Description</h4>
                          <p className="text-gray-700 whitespace-pre-wrap">{proposal.description}</p>
                        </div>

                        {proposal.createdBy && (
                          <div>
                            <h4 className="font-semibold mb-2">Proposed by</h4>
                            <p className="text-sm text-gray-600">
                              {proposal.createdBy.username || proposal.createdBy.email || "Unknown"}
                            </p>
                          </div>
                        )}

                        <div className="space-y-3">
                          <div className="flex items-center justify-between">
                            <h4 className="font-semibold">Voting Progress</h4>
                            <span className="text-sm text-gray-600">
                              {quorumInfo.totalVotes} total votes
                            </span>
                          </div>

                          <div className="space-y-2">
                            <div>
                              <div className="flex items-center justify-between text-sm mb-1">
                                <span className="flex items-center gap-1">
                                  <Check className="h-4 w-4 text-green-600" />
                                  For
                                </span>
                                <span className="font-medium">{proposal.votesFor || 0} ({forPct}%)</span>
                              </div>
                              <Progress value={forPct} className="h-2 bg-gray-200">
                                <div className="h-full bg-green-600 rounded-full" style={{ width: `${forPct}%` }} />
                              </Progress>
                            </div>

                            <div>
                              <div className="flex items-center justify-between text-sm mb-1">
                                <span className="flex items-center gap-1">
                                  <X className="h-4 w-4 text-red-600" />
                                  Against
                                </span>
                                <span className="font-medium">{proposal.votesAgainst || 0} ({againstPct}%)</span>
                              </div>
                              <Progress value={againstPct} className="h-2 bg-gray-200">
                                <div className="h-full bg-red-600 rounded-full" style={{ width: `${againstPct}%` }} />
                              </Progress>
                            </div>

                            {(proposal.abstainVotes || 0) > 0 && (
                              <div>
                                <div className="flex items-center justify-between text-sm mb-1">
                                  <span>Abstain</span>
                                  <span className="font-medium">{proposal.abstainVotes} ({abstainPct}%)</span>
                                </div>
                                <Progress value={abstainPct} className="h-2 bg-gray-200">
                                  <div className="h-full bg-gray-600 rounded-full" style={{ width: `${abstainPct}%` }} />
                                </Progress>
                              </div>
                            )}
                          </div>
                        </div>

                        <div className="space-y-2">
                          <div className="flex items-center justify-between">
                            <h4 className="font-semibold">Quorum Progress</h4>
                            <span className="text-sm text-gray-600">
                              {quorumInfo.votesNeeded > 0
                                ? `${quorumInfo.votesNeeded} more votes needed`
                                : "Quorum reached"}
                            </span>
                          </div>
                          <Progress value={quorumInfo.progress} className="h-2">
                            <div
                              className={`h-full rounded-full ${quorumInfo.progress >= 100 ? "bg-green-600" : "bg-blue-600"}`}
                              style={{ width: `${quorumInfo.progress}%` }}
                            />
                          </Progress>
                          <p className="text-xs text-gray-500">
                            {quorumInfo.totalVotes} / {quorumInfo.quorumNeeded} votes ({proposal.quorum}% quorum required)
                          </p>
                        </div>

                        <div className="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 text-sm">
                          <div className="flex justify-between">
                            <span className="text-gray-600">Threshold:</span>
                            <span className="font-medium">{proposal.threshold}%</span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-gray-600">Voting Period:</span>
                            <span className="font-medium">{proposal.votingDays || 7} days</span>
                          </div>
                          {proposal.votingEndsAt && (
                            <div className="flex justify-between">
                              <span className="text-gray-600">Ends:</span>
                              <span className="font-medium">
                                {new Date(proposal.votingEndsAt).toLocaleDateString()}
                              </span>
                            </div>
                          )}
                        </div>

                        {isMember && proposal.status === "ACTIVE" && !proposal.userVoted && (
                          <div className="space-y-3 pt-2 border-t">
                            <h4 className="font-semibold">Cast Your Vote</h4>
                            <Textarea
                              placeholder="Optional: Explain your vote (visible to other members)"
                              value={voteReasons[proposal.id] || ""}
                              onChange={(e) => setVoteReasons({ ...voteReasons, [proposal.id]: e.target.value })}
                              className="min-h-20"
                            />
                            <div className="flex gap-2">
                              <Button
                                onClick={() => handleVote(proposal.id, 0)}
                                disabled={voteMutation.isPending}
                                variant="outline"
                                className="flex-1 border-green-600 text-green-600 hover:bg-green-50"
                              >
                                {voteMutation.isPending ? (
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                ) : (
                                  <Check className="mr-2 h-4 w-4" />
                                )}
                                Vote For
                              </Button>
                              <Button
                                onClick={() => handleVote(proposal.id, 1)}
                                disabled={voteMutation.isPending}
                                variant="outline"
                                className="flex-1 border-red-600 text-red-600 hover:bg-red-50"
                              >
                                {voteMutation.isPending ? (
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                ) : (
                                  <X className="mr-2 h-4 w-4" />
                                )}
                                Vote Against
                              </Button>
                            </div>
                          </div>
                        )}

                        {proposal.userVoted && (
                          <div className="rounded-md bg-blue-50 border border-blue-200 p-4 flex items-start gap-3">
                            <Check className="h-5 w-5 text-blue-600 flex-shrink-0 mt-0.5" />
                            <div>
                              <p className="font-medium text-blue-900">You have voted on this proposal</p>
                              <p className="text-sm text-blue-700 mt-1">
                                Your vote has been recorded and will count towards the final decision.
                              </p>
                            </div>
                          </div>
                        )}
                      </TabsContent>

                      <TabsContent value="discussion" className="space-y-4 mt-4">
                        <div className="rounded-lg border border-gray-200 bg-gray-50 p-8 text-center">
                          <MessageSquare className="h-12 w-12 text-gray-400 mx-auto mb-3" />
                          <h4 className="font-semibold text-gray-900 mb-2">Discussion Coming Soon</h4>
                          <p className="text-sm text-gray-600">
                            Proposal discussion features will be available in a future update.
                          </p>
                        </div>
                      </TabsContent>
                    </Tabs>
                  )}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
