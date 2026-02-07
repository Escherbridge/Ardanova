"use client";

import { useState, useEffect, useRef } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Textarea } from "~/components/ui/textarea";
import { Vote, Plus, Check, X, Loader2, Clock, Users, MessageSquare, ChevronDown, ChevronUp, AlertCircle, Pencil, Send } from "lucide-react";

interface ProposalsTabProps {
  projectId: string;
  isOwner: boolean;
  isMember: boolean;
  selectedProposalId?: string;
  userId?: string;
}

type ProposalType = "TREASURY" | "GOVERNANCE" | "STRATEGIC" | "OPERATIONAL" | "EMERGENCY";

function ProposalDiscussion({ proposalId, isMember }: { proposalId: string; isMember: boolean }) {
  const [newComment, setNewComment] = useState("");
  const utils = api.useUtils();

  const { data: comments, isLoading } = api.project.getProposalComments.useQuery(
    { proposalId },
    { enabled: !!proposalId }
  );

  const commentMutation = api.project.createProposalComment.useMutation({
    onSuccess: () => {
      utils.project.getProposalComments.invalidate({ proposalId });
      setNewComment("");
    },
  });

  const handleSubmit = () => {
    const content = newComment.trim();
    if (!content) return;
    commentMutation.mutate({ proposalId, content });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-gray-400" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {!comments || comments.length === 0 ? (
        <div className="text-center py-6 text-sm text-gray-500">
          No comments yet. Be the first to start the discussion.
        </div>
      ) : (
        <div className="space-y-3">
          {comments.map((comment: any) => (
            <div key={comment.id} className="rounded-md border border-gray-200 p-3">
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium text-gray-900">
                  {comment.user?.name ?? "Anonymous"}
                </span>
                <span className="text-xs text-gray-500">
                  {new Date(comment.createdAt).toLocaleDateString()}
                </span>
              </div>
              <p className="text-sm text-gray-700 whitespace-pre-wrap">{comment.content}</p>
              {comment.replies && comment.replies.length > 0 && (
                <div className="mt-2 ml-4 space-y-2 border-l-2 border-gray-100 pl-3">
                  {comment.replies.map((reply: any) => (
                    <div key={reply.id} className="text-sm">
                      <div className="flex items-center justify-between">
                        <span className="font-medium text-gray-900">{reply.user?.name ?? "Anonymous"}</span>
                        <span className="text-xs text-gray-500">
                          {new Date(reply.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                      <p className="text-gray-700 whitespace-pre-wrap">{reply.content}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {isMember && (
        <div className="flex gap-2 pt-2 border-t">
          <Textarea
            placeholder="Add a comment..."
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            className="min-h-16 flex-1"
          />
          <Button
            onClick={handleSubmit}
            disabled={commentMutation.isPending || !newComment.trim()}
            size="sm"
            className="self-end"
          >
            {commentMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Send className="h-4 w-4" />
            )}
          </Button>
        </div>
      )}

      {commentMutation.error && (
        <div className="rounded-md bg-red-50 border border-red-200 p-3">
          <p className="text-sm text-red-800">{commentMutation.error.message}</p>
        </div>
      )}
    </div>
  );
}

export default function ProposalsTab({ projectId, isOwner, isMember, selectedProposalId, userId }: ProposalsTabProps) {
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
  const [editingProposalId, setEditingProposalId] = useState<string | null>(null);
  const [editFormData, setEditFormData] = useState<typeof formData | null>(null);

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

  const publishMutation = api.project.publishProposal.useMutation({
    onSuccess: () => {
      utils.project.getProposals.invalidate({ projectId });
    },
  });

  const updateMutation = api.project.updateProposal.useMutation({
    onSuccess: () => {
      utils.project.getProposals.invalidate({ projectId });
      setEditingProposalId(null);
      setEditFormData(null);
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

  const handlePublishProposal = (proposalId: string) => {
    publishMutation.mutate({ projectId, proposalId });
  };

  const handleStartEdit = (proposal: any) => {
    let parsedOpts: string[] = [];
    try {
      const raw = typeof proposal.options === 'string' ? JSON.parse(proposal.options) : proposal.options;
      parsedOpts = Array.isArray(raw) ? raw : ["Yes", "No"];
    } catch { parsedOpts = ["Yes", "No"]; }

    setEditingProposalId(proposal.id);
    setEditFormData({
      type: proposal.type,
      title: proposal.title,
      description: proposal.description,
      options: parsedOpts,
      quorum: proposal.quorum ?? 50,
      threshold: proposal.threshold ?? 66,
      votingDays: 7,
    });
  };

  const handleSaveEdit = (proposalId: string) => {
    if (!editFormData) return;
    updateMutation.mutate({
      projectId,
      proposalId,
      title: editFormData.title,
      description: editFormData.description,
      options: editFormData.options,
      quorum: editFormData.quorum,
      threshold: editFormData.threshold,
      votingDays: editFormData.votingDays,
    });
  };

  const handleCancelEdit = () => {
    setEditingProposalId(null);
    setEditFormData(null);
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: "default" | "secondary" | "destructive" | "outline", label: string }> = {
      DRAFT: { variant: "outline", label: "Draft" },
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
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Voting Options
                  </label>
                  <div className="space-y-2">
                    {formData.options.map((option, index) => (
                      <div key={index} className="flex items-center gap-2">
                        <span className="text-xs text-gray-500 w-5 text-right">{index + 1}.</span>
                        <input
                          type="text"
                          value={option}
                          onChange={(e) => {
                            const newOptions = [...formData.options];
                            newOptions[index] = e.target.value;
                            setFormData({ ...formData, options: newOptions });
                          }}
                          className="flex-1 rounded-md border border-gray-300 px-3 py-1.5 text-sm"
                          placeholder={`Option ${index + 1}`}
                          required
                        />
                        {formData.options.length > 2 && (
                          <button
                            type="button"
                            onClick={() => {
                              const newOptions = formData.options.filter((_, i) => i !== index);
                              setFormData({ ...formData, options: newOptions });
                            }}
                            className="p-1 text-gray-400 hover:text-red-500"
                          >
                            <X className="h-4 w-4" />
                          </button>
                        )}
                      </div>
                    ))}
                    {formData.options.length < 10 && (
                      <button
                        type="button"
                        onClick={() => setFormData({ ...formData, options: [...formData.options, ""] })}
                        className="flex items-center gap-1 text-sm text-blue-600 hover:text-blue-700 mt-1"
                      >
                        <Plus className="h-3.5 w-3.5" />
                        Add option
                      </button>
                    )}
                  </div>
                  <p className="text-xs text-gray-500 mt-1">At least 2 options required</p>
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

            // Parse options from JSON string
            let parsedOptions: string[] = [];
            try {
              const raw = typeof proposal.options === 'string' ? JSON.parse(proposal.options) : proposal.options;
              parsedOptions = Array.isArray(raw) ? raw : [];
            } catch { parsedOptions = []; }


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
                    {proposal.status === "DRAFT" && userId && proposal.creatorId === userId && (
                      <div className="flex gap-1 ml-2">
                        <Button
                          onClick={() => handleStartEdit(proposal)}
                          variant="outline"
                          size="sm"
                        >
                          <Pencil className="h-3.5 w-3.5 mr-1" />
                          Edit
                        </Button>
                        <Button
                          onClick={() => handlePublishProposal(proposal.id)}
                          disabled={publishMutation.isPending}
                          size="sm"
                        >
                          {publishMutation.isPending ? (
                            <Loader2 className="h-3.5 w-3.5 animate-spin mr-1" />
                          ) : null}
                          Publish
                        </Button>
                      </div>
                    )}
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
                  {editingProposalId === proposal.id && editFormData ? (
                    <div className="space-y-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Title</label>
                        <input
                          type="text"
                          value={editFormData.title}
                          onChange={(e) => setEditFormData({ ...editFormData, title: e.target.value })}
                          className="w-full rounded-md border border-gray-300 px-3 py-2"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                        <textarea
                          value={editFormData.description}
                          onChange={(e) => setEditFormData({ ...editFormData, description: e.target.value })}
                          className="w-full rounded-md border border-gray-300 px-3 py-2"
                          rows={4}
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Voting Options</label>
                        <div className="space-y-2">
                          {editFormData.options.map((option, index) => (
                            <div key={index} className="flex items-center gap-2">
                              <span className="text-xs text-gray-500 w-5 text-right">{index + 1}.</span>
                              <input
                                type="text"
                                value={option}
                                onChange={(e) => {
                                  const newOptions = [...editFormData.options];
                                  newOptions[index] = e.target.value;
                                  setEditFormData({ ...editFormData, options: newOptions });
                                }}
                                className="flex-1 rounded-md border border-gray-300 px-3 py-1.5 text-sm"
                                placeholder={`Option ${index + 1}`}
                              />
                              {editFormData.options.length > 2 && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    const newOptions = editFormData.options.filter((_, i) => i !== index);
                                    setEditFormData({ ...editFormData, options: newOptions });
                                  }}
                                  className="p-1 text-gray-400 hover:text-red-500"
                                >
                                  <X className="h-4 w-4" />
                                </button>
                              )}
                            </div>
                          ))}
                          {editFormData.options.length < 10 && (
                            <button
                              type="button"
                              onClick={() => setEditFormData({ ...editFormData, options: [...editFormData.options, ""] })}
                              className="flex items-center gap-1 text-sm text-blue-600 hover:text-blue-700 mt-1"
                            >
                              <Plus className="h-3.5 w-3.5" />
                              Add option
                            </button>
                          )}
                        </div>
                      </div>
                      <div className="grid grid-cols-3 gap-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">Quorum (%)</label>
                          <input
                            type="number" min="1" max="100"
                            value={editFormData.quorum}
                            onChange={(e) => setEditFormData({ ...editFormData, quorum: Number(e.target.value) })}
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">Threshold (%)</label>
                          <input
                            type="number" min="1" max="100"
                            value={editFormData.threshold}
                            onChange={(e) => setEditFormData({ ...editFormData, threshold: Number(e.target.value) })}
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                          />
                        </div>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-1">Voting Days</label>
                          <input
                            type="number" min="1" max="30"
                            value={editFormData.votingDays}
                            onChange={(e) => setEditFormData({ ...editFormData, votingDays: Number(e.target.value) })}
                            className="w-full rounded-md border border-gray-300 px-3 py-2"
                          />
                        </div>
                      </div>
                      <div className="flex gap-2">
                        <Button onClick={handleCancelEdit} variant="outline" className="flex-1">
                          Cancel
                        </Button>
                        <Button
                          onClick={() => handleSaveEdit(proposal.id)}
                          disabled={updateMutation.isPending}
                          className="flex-1"
                        >
                          {updateMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                          Save Changes
                        </Button>
                      </div>
                      {updateMutation.error && (
                        <div className="rounded-md bg-red-50 border border-red-200 p-3 flex items-start gap-2">
                          <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                          <p className="text-sm text-red-800">{updateMutation.error.message}</p>
                        </div>
                      )}
                    </div>
                  ) : (
                    <>
                      {!isExpanded ? (
                        <>
                          <p className="text-gray-700 line-clamp-2">{proposal.description}</p>
                          {parsedOptions.length > 0 && (
                            <div className="rounded-md border border-gray-200 bg-gray-50 p-3">
                              <div className="flex items-center justify-between mb-2">
                                <span className="text-xs font-medium text-gray-500 uppercase">Options</span>
                                <span className="text-xs text-gray-500">
                                  {quorumInfo.totalVotes} vote{quorumInfo.totalVotes !== 1 ? "s" : ""}
                                </span>
                              </div>
                              <div className="space-y-1">
                                {parsedOptions.map((opt, i) => (
                                  <div key={i} className="flex items-center justify-between text-sm">
                                    <span className="text-gray-700">{opt}</span>
                                    <span className="text-gray-500 font-medium">0</span>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}
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
                                <h4 className="font-semibold">Voting Options</h4>
                                <span className="text-sm text-gray-600">
                                  {quorumInfo.totalVotes} total votes
                                </span>
                              </div>

                              {parsedOptions.length > 0 ? (
                                <div className="rounded-md border border-gray-200 bg-gray-50 p-3 space-y-2">
                                  {parsedOptions.map((opt, i) => (
                                    <div key={i} className="flex items-center justify-between text-sm">
                                      <span className="text-gray-700">{opt}</span>
                                      <span className="text-gray-500 font-medium">0</span>
                                    </div>
                                  ))}
                                </div>
                              ) : (
                                <p className="text-sm text-gray-500">No options defined</p>
                              )}
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

                            {isMember && proposal.status === "ACTIVE" && !proposal.userVoted && parsedOptions.length > 0 && (
                              <div className="space-y-3 pt-2 border-t">
                                <h4 className="font-semibold">Cast Your Vote</h4>
                                <Textarea
                                  placeholder="Optional: Explain your vote (visible to other members)"
                                  value={voteReasons[proposal.id] || ""}
                                  onChange={(e) => setVoteReasons({ ...voteReasons, [proposal.id]: e.target.value })}
                                  className="min-h-20"
                                />
                                <div className="flex gap-2 flex-wrap">
                                  {parsedOptions.map((opt, i) => (
                                    <Button
                                      key={i}
                                      onClick={() => handleVote(proposal.id, i)}
                                      disabled={voteMutation.isPending}
                                      variant="outline"
                                      className="flex-1 min-w-[120px]"
                                    >
                                      {voteMutation.isPending ? (
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                      ) : null}
                                      {opt}
                                    </Button>
                                  ))}
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
                            <ProposalDiscussion proposalId={proposal.id} isMember={isMember} />
                          </TabsContent>
                        </Tabs>
                      )}
                    </>
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
