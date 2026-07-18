"use client";

import { useState, useEffect, useRef, type ReactNode } from "react";
import { useSession } from "next-auth/react";
import { z } from "zod";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Textarea } from "~/components/ui/textarea";
import {
  Vote,
  Plus,
  Check,
  X,
  Loader2,
  Clock,
  MessageSquare,
  ChevronDown,
  ChevronUp,
  AlertCircle,
  Pencil,
  Send,
} from "lucide-react";

interface ProposalsTabProps {
  projectId: string;
  isOwner: boolean;
  isMember: boolean;
  selectedProposalId?: string;
  userId?: string;
}

type ProposalType = RouterInputs["project"]["createProposal"]["type"];
type ProjectProposal = RouterOutputs["project"]["getProposals"][number];
type ProjectProposalVote = RouterOutputs["project"]["getProposalVotes"][number];

const proposalTypes = [
  "TREASURY",
  "GOVERNANCE",
  "STRATEGIC",
  "OPERATIONAL",
  "EMERGENCY",
] as const satisfies readonly ProposalType[];

const storedProposalOptionsSchema = z.array(
  z.union([
    z.string().trim().min(1),
    z
      .object({
        label: z.string().trim().min(1),
        choice: z.number().int().nonnegative(),
      })
      .strict(),
  ]),
);

interface ProposalOption {
  label: string;
  choice: number;
}

interface VoteOptionStats {
  voteCount: number;
  votingPower: number;
  percentage: number;
}

interface ProposalVoteSnapshot {
  byChoice: ReadonlyMap<number, VoteOptionStats>;
  totalVotes: number;
  totalVotingPower: number;
  currentUserVote: ProjectProposalVote | undefined;
  hasData: boolean;
  isLoading: boolean;
  errorMessage: string | null;
}

function isProposalType(value: string): value is ProposalType {
  return proposalTypes.some((type) => type === value);
}

function parseProposalOptions(options: string): ProposalOption[] {
  try {
    const parsed: unknown = JSON.parse(options);
    const result = storedProposalOptionsSchema.safeParse(parsed);
    if (!result.success) return [];

    return result.data.map((option, index) =>
      typeof option === "string"
        ? { label: option, choice: index }
        : { label: option.label, choice: option.choice },
    );
  } catch {
    return [];
  }
}

function ProposalVoteData({
  proposalId,
  userId,
  children,
}: {
  proposalId: string;
  userId?: string;
  children: (snapshot: ProposalVoteSnapshot) => ReactNode;
}) {
  const votesQuery = api.project.getProposalVotes.useQuery({ proposalId });
  const votes = votesQuery.data ?? [];
  const totalVotingPower = votes.reduce((sum, vote) => sum + vote.weight, 0);
  const rawByChoice = new Map<number, Omit<VoteOptionStats, "percentage">>();

  for (const vote of votes) {
    const current = rawByChoice.get(vote.choice) ?? {
      voteCount: 0,
      votingPower: 0,
    };
    rawByChoice.set(vote.choice, {
      voteCount: current.voteCount + 1,
      votingPower: current.votingPower + vote.weight,
    });
  }

  const byChoice = new Map<number, VoteOptionStats>();
  for (const [choice, stats] of rawByChoice) {
    byChoice.set(choice, {
      ...stats,
      percentage:
        totalVotingPower > 0
          ? Math.round((stats.votingPower / totalVotingPower) * 100)
          : 0,
    });
  }

  return children({
    byChoice,
    totalVotes: votes.length,
    totalVotingPower,
    currentUserVote: userId
      ? votes.find((vote) => vote.voterId === userId)
      : undefined,
    hasData: votesQuery.data !== undefined,
    isLoading: votesQuery.isLoading,
    errorMessage: votesQuery.error?.message ?? null,
  });
}

function calculateQuorumProgress(
  proposal: ProjectProposal,
  totalVotingPower: number,
) {
  const quorumTarget = Math.max(proposal.quorum, 0);
  return {
    progress:
      quorumTarget === 0
        ? 100
        : Math.min((totalVotingPower / quorumTarget) * 100, 100),
    votingPowerNeeded: Math.max(quorumTarget - totalVotingPower, 0),
    quorumTarget,
  };
}

function ProposalDiscussion({
  projectId,
  proposalId,
  isMember,
}: {
  projectId: string;
  proposalId: string;
  isMember: boolean;
}) {
  const [newComment, setNewComment] = useState("");
  const utils = api.useUtils();

  const { data: comments, isLoading } =
    api.project.getProposalComments.useQuery(
      { projectId, proposalId },
      { enabled: !!proposalId },
    );

  const commentMutation = api.project.createProposalComment.useMutation({
    onSuccess: () => {
      void utils.project.getProposalComments.invalidate({
        projectId,
        proposalId,
      });
      setNewComment("");
    },
  });

  const handleSubmit = () => {
    const content = newComment.trim();
    if (!content) return;
    commentMutation.mutate({ projectId, proposalId, content });
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
        <div className="py-6 text-center text-sm text-gray-500">
          No comments yet. Be the first to start the discussion.
        </div>
      ) : (
        <div className="space-y-3">
          {comments.map((comment) => (
            <div
              key={comment.id}
              className="rounded-md border border-gray-200 p-3"
            >
              <div className="mb-1 flex items-center justify-between">
                <span className="text-sm font-medium text-gray-900">
                  {comment.user?.name ?? "Anonymous"}
                </span>
                <span className="text-xs text-gray-500">
                  {new Date(comment.createdAt).toLocaleDateString()}
                </span>
              </div>
              <p className="text-sm whitespace-pre-wrap text-gray-700">
                {comment.content}
              </p>
              {comment.replies && comment.replies.length > 0 && (
                <div className="mt-2 ml-4 space-y-2 border-l-2 border-gray-100 pl-3">
                  {comment.replies.map((reply) => (
                    <div key={reply.id} className="text-sm">
                      <div className="flex items-center justify-between">
                        <span className="font-medium text-gray-900">
                          {reply.user?.name ?? "Anonymous"}
                        </span>
                        <span className="text-xs text-gray-500">
                          {new Date(reply.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                      <p className="whitespace-pre-wrap text-gray-700">
                        {reply.content}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {isMember && (
        <div className="flex gap-2 border-t pt-2">
          <label htmlFor={`proposal-comment-${proposalId}`} className="sr-only">
            Add a comment
          </label>
          <Textarea
            id={`proposal-comment-${proposalId}`}
            placeholder="Add a comment..."
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            className="min-h-16 flex-1"
          />
          <Button
            type="button"
            onClick={handleSubmit}
            disabled={commentMutation.isPending || !newComment.trim()}
            size="sm"
            className="self-end"
            aria-label="Post comment"
          >
            {commentMutation.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <Send className="h-4 w-4" aria-hidden="true" />
            )}
          </Button>
        </div>
      )}

      {commentMutation.error && (
        <div className="rounded-md border border-red-200 bg-red-50 p-3">
          <p className="text-sm text-red-800">
            {commentMutation.error.message}
          </p>
        </div>
      )}
    </div>
  );
}

export default function ProposalsTab({
  projectId,
  isOwner,
  isMember,
  selectedProposalId,
  userId,
}: ProposalsTabProps) {
  const { data: session } = useSession();
  const currentUserId = session?.user?.id ?? userId;
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [expandedProposalId, setExpandedProposalId] = useState<string | null>(
    selectedProposalId || null,
  );
  const [voteReasons, setVoteReasons] = useState<Record<string, string>>({});
  const [showPreview, setShowPreview] = useState(false);
  const [formData, setFormData] = useState({
    type: "GOVERNANCE" as ProposalType,
    title: "",
    description: "",
    options: ["Yes", "No"],
    quorum: 50,
    threshold: 66,
    votingDurationDays: 7,
  });
  const [editingProposalId, setEditingProposalId] = useState<string | null>(
    null,
  );
  const [editFormData, setEditFormData] = useState<Omit<
    typeof formData,
    "votingDurationDays"
  > | null>(null);

  const selectedProposalRef = useRef<HTMLDivElement>(null);
  const utils = api.useUtils();

  // Auto-expand and scroll to selected proposal
  useEffect(() => {
    if (selectedProposalId) {
      setExpandedProposalId(selectedProposalId);
      setTimeout(() => {
        selectedProposalRef.current?.scrollIntoView({
          behavior: "smooth",
          block: "center",
        });
      }, 100);
    }
  }, [selectedProposalId]);

  const {
    data: proposals,
    isLoading,
    error,
  } = api.project.getProposals.useQuery(
    { projectId },
    { enabled: !!projectId },
  );

  const createMutation = api.project.createProposal.useMutation({
    onSuccess: () => {
      void utils.project.getProposals.invalidate({ projectId });
      setShowCreateForm(false);
      setFormData({
        type: "GOVERNANCE",
        title: "",
        description: "",
        options: ["Yes", "No"],
        quorum: 50,
        threshold: 66,
        votingDurationDays: 7,
      });
    },
  });

  const voteMutation = api.project.castVote.useMutation({
    onSuccess: (_vote, variables) => {
      setVoteReasons((current) => {
        const next = { ...current };
        delete next[variables.proposalId];
        return next;
      });
    },
    onSettled: (_vote, _error, variables) => {
      void utils.project.getProposals.invalidate({ projectId });
      if (variables) {
        void utils.project.getProposalVotes.invalidate({
          proposalId: variables.proposalId,
        });
      }
    },
  });

  const closeMutation = api.project.closeProposal.useMutation({
    onSuccess: () => {
      void utils.project.getProposals.invalidate({ projectId });
    },
  });

  const publishMutation = api.project.publishProposal.useMutation({
    onSuccess: () => {
      void utils.project.getProposals.invalidate({ projectId });
    },
  });

  const updateMutation = api.project.updateProposal.useMutation({
    onSuccess: () => {
      void utils.project.getProposals.invalidate({ projectId });
      setEditingProposalId(null);
      setEditFormData(null);
    },
  });

  const handleCreateProposal = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate({
      projectId,
      type: formData.type,
      title: formData.title,
      description: formData.description,
      options: formData.options,
      quorum: formData.quorum,
      threshold: formData.threshold,
      votingDays: formData.votingDurationDays,
    });
  };

  const handleVote = (proposalId: string, choice: number) => {
    const reason = voteReasons[proposalId];
    voteMutation.mutate({ projectId, proposalId, choice, reason });
  };

  const toggleProposalExpand = (proposalId: string) => {
    setExpandedProposalId(
      expandedProposalId === proposalId ? null : proposalId,
    );
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

  const handleCloseProposal = (proposalId: string) => {
    closeMutation.mutate({ projectId, proposalId });
  };

  const handlePublishProposal = (proposalId: string) => {
    publishMutation.mutate({ projectId, proposalId });
  };

  const handleStartEdit = (proposal: ProjectProposal) => {
    const parsedOpts = parseProposalOptions(proposal.options).map(
      (option) => option.label,
    );

    setEditingProposalId(proposal.id);
    setEditFormData({
      type: isProposalType(proposal.type) ? proposal.type : "GOVERNANCE",
      title: proposal.title,
      description: proposal.description,
      options: parsedOpts.length >= 2 ? parsedOpts : ["Yes", "No"],
      quorum: proposal.quorum ?? 50,
      threshold: proposal.threshold ?? 66,
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
    });
  };

  const handleCancelEdit = () => {
    setEditingProposalId(null);
    setEditFormData(null);
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<
      string,
      {
        variant: "default" | "secondary" | "destructive" | "outline";
        label: string;
      }
    > = {
      DRAFT: { variant: "outline", label: "Draft" },
      ACTIVE: { variant: "default", label: "Active" },
      PASSED: { variant: "secondary", label: "Passed" },
      REJECTED: { variant: "destructive", label: "Rejected" },
      EXECUTED: { variant: "secondary", label: "Executed" },
      CANCELLED: { variant: "outline", label: "Cancelled" },
      EXPIRED: { variant: "outline", label: "Expired" },
    };
    const config = variants[status] || {
      variant: "outline" as const,
      label: status,
    };
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  const getTypeBadge = (type: string) => {
    const colors: Record<string, string> = {
      TREASURY: "bg-green-100 text-green-800",
      GOVERNANCE: "bg-blue-100 text-blue-800",
      STRATEGIC: "bg-purple-100 text-purple-800",
      OPERATIONAL: "bg-orange-100 text-orange-800",
      EMERGENCY: "bg-red-100 text-red-800",
      CONSTITUTIONAL: "bg-cyan-100 text-cyan-800",
      SHARES: "bg-amber-100 text-amber-800",
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
                aria-label={
                  showCreateForm
                    ? "Close proposal creation form"
                    : "Open proposal creation form"
                }
                aria-expanded={showCreateForm}
                aria-controls={`create-proposal-form-${projectId}`}
              >
                {showCreateForm ? (
                  <X className="h-4 w-4" aria-hidden="true" />
                ) : (
                  <Plus className="h-4 w-4" aria-hidden="true" />
                )}
              </Button>
            </div>
          </CardHeader>
          {showCreateForm && (
            <CardContent>
              <form
                id={`create-proposal-form-${projectId}`}
                onSubmit={handleCreateProposal}
                className="space-y-4"
              >
                <div>
                  <label
                    htmlFor={`create-proposal-type-${projectId}`}
                    className="text-foreground mb-1 block text-sm font-medium"
                  >
                    Type
                  </label>
                  <select
                    id={`create-proposal-type-${projectId}`}
                    value={formData.type}
                    onChange={(e) => {
                      if (isProposalType(e.target.value)) {
                        setFormData({ ...formData, type: e.target.value });
                      }
                    }}
                    className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
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
                  <label
                    htmlFor={`create-proposal-title-${projectId}`}
                    className="text-foreground mb-1 block text-sm font-medium"
                  >
                    Title
                  </label>
                  <input
                    id={`create-proposal-title-${projectId}`}
                    type="text"
                    value={formData.title}
                    onChange={(e) =>
                      setFormData({ ...formData, title: e.target.value })
                    }
                    className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                    required
                  />
                </div>
                <div>
                  <label
                    htmlFor={`create-proposal-description-${projectId}`}
                    className="text-foreground mb-1 block text-sm font-medium"
                  >
                    Description
                  </label>
                  <textarea
                    id={`create-proposal-description-${projectId}`}
                    value={formData.description}
                    onChange={(e) =>
                      setFormData({ ...formData, description: e.target.value })
                    }
                    className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                    rows={4}
                    required
                  />
                </div>
                <fieldset>
                  <legend className="text-foreground mb-1 block text-sm font-medium">
                    Voting Options
                  </legend>
                  <div className="space-y-2">
                    {formData.options.map((option, index) => (
                      <div key={index} className="flex items-center gap-2">
                        <label
                          htmlFor={`create-proposal-option-${projectId}-${index}`}
                          className="text-muted-foreground w-5 text-right text-xs"
                        >
                          <span className="sr-only">Voting option </span>
                          {index + 1}.
                        </label>
                        <input
                          id={`create-proposal-option-${projectId}-${index}`}
                          type="text"
                          value={option}
                          onChange={(e) => {
                            const newOptions = [...formData.options];
                            newOptions[index] = e.target.value;
                            setFormData({ ...formData, options: newOptions });
                          }}
                          className="border-border bg-background text-foreground min-h-11 min-w-0 flex-1 border px-3 py-2 text-sm"
                          placeholder={`Option ${index + 1}`}
                          required
                        />
                        {formData.options.length > 2 && (
                          <button
                            type="button"
                            onClick={() => {
                              const newOptions = formData.options.filter(
                                (_, i) => i !== index,
                              );
                              setFormData({ ...formData, options: newOptions });
                            }}
                            className="text-muted-foreground hover:text-destructive inline-flex size-11 shrink-0 items-center justify-center"
                            aria-label={`Remove voting option ${index + 1}`}
                          >
                            <X className="h-4 w-4" aria-hidden="true" />
                          </button>
                        )}
                      </div>
                    ))}
                    {formData.options.length < 10 && (
                      <button
                        type="button"
                        onClick={() =>
                          setFormData({
                            ...formData,
                            options: [...formData.options, ""],
                          })
                        }
                        className="text-system hover:text-foreground mt-1 flex min-h-11 items-center gap-1 px-2 text-sm font-medium"
                      >
                        <Plus className="h-3.5 w-3.5" aria-hidden="true" />
                        Add option
                      </button>
                    )}
                  </div>
                  <p className="mt-1 text-xs text-gray-500">
                    At least 2 options required
                  </p>
                </fieldset>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                  <div>
                    <label
                      htmlFor={`create-proposal-quorum-${projectId}`}
                      className="text-foreground mb-1 block text-sm font-medium"
                    >
                      Quorum target
                    </label>
                    <input
                      id={`create-proposal-quorum-${projectId}`}
                      type="number"
                      min="1"
                      max="100"
                      value={formData.quorum}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          quorum: Number(e.target.value),
                        })
                      }
                      className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                      aria-describedby={`create-proposal-quorum-help-${projectId}`}
                      required
                    />
                    <p
                      id={`create-proposal-quorum-help-${projectId}`}
                      className="text-muted-foreground mt-1 text-xs"
                    >
                      Voting power required
                    </p>
                  </div>
                  <div>
                    <label
                      htmlFor={`create-proposal-threshold-${projectId}`}
                      className="text-foreground mb-1 block text-sm font-medium"
                    >
                      Threshold (%)
                    </label>
                    <input
                      id={`create-proposal-threshold-${projectId}`}
                      type="number"
                      min="1"
                      max="100"
                      value={formData.threshold}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          threshold: Number(e.target.value),
                        })
                      }
                      className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                      aria-describedby={`create-proposal-threshold-help-${projectId}`}
                      required
                    />
                    <p
                      id={`create-proposal-threshold-help-${projectId}`}
                      className="text-muted-foreground mt-1 text-xs"
                    >
                      Votes needed to pass
                    </p>
                  </div>
                  <div>
                    <label
                      htmlFor={`create-proposal-duration-${projectId}`}
                      className="text-foreground mb-1 block text-sm font-medium"
                    >
                      Voting Days
                    </label>
                    <input
                      id={`create-proposal-duration-${projectId}`}
                      type="number"
                      min="1"
                      max="30"
                      value={formData.votingDurationDays}
                      onChange={(e) =>
                        setFormData({
                          ...formData,
                          votingDurationDays: Number(e.target.value),
                        })
                      }
                      className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                      aria-describedby={`create-proposal-duration-help-${projectId}`}
                      required
                    />
                    <p
                      id={`create-proposal-duration-help-${projectId}`}
                      className="text-muted-foreground mt-1 text-xs"
                    >
                      Duration for voting
                    </p>
                  </div>
                </div>

                {showPreview && (
                  <div className="space-y-2 rounded-lg border border-blue-200 bg-blue-50 p-4">
                    <h4 className="font-semibold text-blue-900">Preview</h4>
                    <div className="space-y-1 text-sm">
                      <p>
                        <span className="font-medium">Type:</span>{" "}
                        {formData.type}
                      </p>
                      <p>
                        <span className="font-medium">Title:</span>{" "}
                        {formData.title || "(No title)"}
                      </p>
                      <p>
                        <span className="font-medium">Description:</span>{" "}
                        {formData.description || "(No description)"}
                      </p>
                      <p>
                        <span className="font-medium">Options:</span>{" "}
                        {formData.options.join(", ")}
                      </p>
                      <p>
                        <span className="font-medium">Settings:</span>{" "}
                        {formData.quorum} voting-power quorum,{" "}
                        {formData.threshold}% threshold,{" "}
                        {formData.votingDurationDays} day voting window
                      </p>
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
                  <Button
                    type="submit"
                    disabled={createMutation.isPending}
                    className="flex-1"
                  >
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
                  <div className="flex items-start gap-2 rounded-md border border-red-200 bg-red-50 p-3">
                    <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0 text-red-600" />
                    <p className="text-sm text-red-800">
                      {createMutation.error.message}
                    </p>
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
            <Vote className="mx-auto mb-4 h-12 w-12 text-gray-400" />
            <h3 className="mb-2 text-lg font-medium text-gray-900">
              No proposals yet
            </h3>
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
            const parsedOptions = parseProposalOptions(proposal.options);

            return (
              <ProposalVoteData
                key={proposal.id}
                proposalId={proposal.id}
                userId={currentUserId}
              >
                {(voteSnapshot) => {
                  const displayedVotingPower = voteSnapshot.hasData
                    ? voteSnapshot.totalVotingPower
                    : proposal.totalVotingPower;
                  const quorumInfo = calculateQuorumProgress(
                    proposal,
                    displayedVotingPower,
                  );
                  const displayedTotalVotes = voteSnapshot.hasData
                    ? voteSnapshot.totalVotes
                    : proposal.votesCount;

                  return (
                    <Card
                      ref={isSelected ? selectedProposalRef : null}
                      className={isSelected ? "ring-2 ring-blue-500" : ""}
                    >
                      <CardHeader>
                        <div className="flex items-start justify-between">
                          <div className="flex-1 space-y-2">
                            <div className="flex flex-wrap items-center gap-2">
                              {getTypeBadge(proposal.type)}
                              {getStatusBadge(proposal.status)}
                              {proposal.status === "ACTIVE" &&
                                proposal.votingEnd && (
                                  <Badge
                                    variant="outline"
                                    className="flex items-center gap-1"
                                  >
                                    <Clock className="h-3 w-3" />
                                    {calculateTimeRemaining(proposal.votingEnd)}
                                  </Badge>
                                )}
                            </div>
                            <div className="flex items-center justify-between gap-4">
                              <CardTitle className="flex-1">
                                {proposal.title}
                              </CardTitle>
                              <Button
                                onClick={() =>
                                  toggleProposalExpand(proposal.id)
                                }
                                variant="ghost"
                                size="sm"
                                aria-label={`${isExpanded ? "Collapse" : "Expand"} proposal ${proposal.title}`}
                                aria-expanded={isExpanded}
                                aria-controls={`proposal-details-${proposal.id}`}
                              >
                                {isExpanded ? (
                                  <ChevronUp
                                    className="h-4 w-4"
                                    aria-hidden="true"
                                  />
                                ) : (
                                  <ChevronDown
                                    className="h-4 w-4"
                                    aria-hidden="true"
                                  />
                                )}
                              </Button>
                            </div>
                          </div>
                          {proposal.status === "DRAFT" &&
                            userId &&
                            proposal.creatorId === userId && (
                              <div className="ml-2 flex gap-1">
                                <Button
                                  onClick={() => handleStartEdit(proposal)}
                                  variant="outline"
                                  size="sm"
                                >
                                  <Pencil className="mr-1 h-3.5 w-3.5" />
                                  Edit
                                </Button>
                                <Button
                                  onClick={() =>
                                    handlePublishProposal(proposal.id)
                                  }
                                  disabled={publishMutation.isPending}
                                  size="sm"
                                >
                                  {publishMutation.isPending ? (
                                    <Loader2 className="mr-1 h-3.5 w-3.5 animate-spin" />
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
                              aria-label="Close proposal"
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
                      <CardContent
                        id={`proposal-details-${proposal.id}`}
                        className="space-y-4"
                      >
                        {editingProposalId === proposal.id && editFormData ? (
                          <div className="space-y-4">
                            <div>
                              <label
                                htmlFor={`edit-proposal-title-${proposal.id}`}
                                className="text-foreground mb-1 block text-sm font-medium"
                              >
                                Title
                              </label>
                              <input
                                id={`edit-proposal-title-${proposal.id}`}
                                type="text"
                                value={editFormData.title}
                                onChange={(e) =>
                                  setEditFormData({
                                    ...editFormData,
                                    title: e.target.value,
                                  })
                                }
                                className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                              />
                            </div>
                            <div>
                              <label
                                htmlFor={`edit-proposal-description-${proposal.id}`}
                                className="text-foreground mb-1 block text-sm font-medium"
                              >
                                Description
                              </label>
                              <textarea
                                id={`edit-proposal-description-${proposal.id}`}
                                value={editFormData.description}
                                onChange={(e) =>
                                  setEditFormData({
                                    ...editFormData,
                                    description: e.target.value,
                                  })
                                }
                                className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                                rows={4}
                              />
                            </div>
                            <fieldset>
                              <legend className="text-foreground mb-1 block text-sm font-medium">
                                Voting Options
                              </legend>
                              <div className="space-y-2">
                                {editFormData.options.map((option, index) => (
                                  <div
                                    key={index}
                                    className="flex items-center gap-2"
                                  >
                                    <label
                                      htmlFor={`edit-proposal-option-${proposal.id}-${index}`}
                                      className="text-muted-foreground w-5 text-right text-xs"
                                    >
                                      <span className="sr-only">
                                        Voting option{" "}
                                      </span>
                                      {index + 1}.
                                    </label>
                                    <input
                                      id={`edit-proposal-option-${proposal.id}-${index}`}
                                      type="text"
                                      value={option}
                                      onChange={(e) => {
                                        const newOptions = [
                                          ...editFormData.options,
                                        ];
                                        newOptions[index] = e.target.value;
                                        setEditFormData({
                                          ...editFormData,
                                          options: newOptions,
                                        });
                                      }}
                                      className="border-border bg-background text-foreground min-h-11 min-w-0 flex-1 border px-3 py-2 text-sm"
                                      placeholder={`Option ${index + 1}`}
                                    />
                                    {editFormData.options.length > 2 && (
                                      <button
                                        type="button"
                                        onClick={() => {
                                          const newOptions =
                                            editFormData.options.filter(
                                              (_, i) => i !== index,
                                            );
                                          setEditFormData({
                                            ...editFormData,
                                            options: newOptions,
                                          });
                                        }}
                                        className="text-muted-foreground hover:text-destructive inline-flex size-11 shrink-0 items-center justify-center"
                                        aria-label={`Remove voting option ${index + 1}`}
                                      >
                                        <X
                                          className="h-4 w-4"
                                          aria-hidden="true"
                                        />
                                      </button>
                                    )}
                                  </div>
                                ))}
                                {editFormData.options.length < 10 && (
                                  <button
                                    type="button"
                                    onClick={() =>
                                      setEditFormData({
                                        ...editFormData,
                                        options: [...editFormData.options, ""],
                                      })
                                    }
                                    className="text-system hover:text-foreground mt-1 flex min-h-11 items-center gap-1 px-2 text-sm font-medium"
                                  >
                                    <Plus
                                      className="h-3.5 w-3.5"
                                      aria-hidden="true"
                                    />
                                    Add option
                                  </button>
                                )}
                              </div>
                            </fieldset>
                            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                              <div>
                                <label
                                  htmlFor={`edit-proposal-quorum-${proposal.id}`}
                                  className="text-foreground mb-1 block text-sm font-medium"
                                >
                                  Quorum target
                                </label>
                                <input
                                  id={`edit-proposal-quorum-${proposal.id}`}
                                  type="number"
                                  min="1"
                                  max="100"
                                  value={editFormData.quorum}
                                  onChange={(e) =>
                                    setEditFormData({
                                      ...editFormData,
                                      quorum: Number(e.target.value),
                                    })
                                  }
                                  className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                                />
                              </div>
                              <div>
                                <label
                                  htmlFor={`edit-proposal-threshold-${proposal.id}`}
                                  className="text-foreground mb-1 block text-sm font-medium"
                                >
                                  Threshold (%)
                                </label>
                                <input
                                  id={`edit-proposal-threshold-${proposal.id}`}
                                  type="number"
                                  min="1"
                                  max="100"
                                  value={editFormData.threshold}
                                  onChange={(e) =>
                                    setEditFormData({
                                      ...editFormData,
                                      threshold: Number(e.target.value),
                                    })
                                  }
                                  className="border-border bg-background text-foreground min-h-11 w-full border px-3 py-2"
                                />
                              </div>
                            </div>
                            <div className="flex gap-2">
                              <Button
                                onClick={handleCancelEdit}
                                variant="outline"
                                className="flex-1"
                              >
                                Cancel
                              </Button>
                              <Button
                                onClick={() => handleSaveEdit(proposal.id)}
                                disabled={updateMutation.isPending}
                                className="flex-1"
                              >
                                {updateMutation.isPending ? (
                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                ) : null}
                                Save Changes
                              </Button>
                            </div>
                            {updateMutation.error && (
                              <div className="flex items-start gap-2 rounded-md border border-red-200 bg-red-50 p-3">
                                <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0 text-red-600" />
                                <p className="text-sm text-red-800">
                                  {updateMutation.error.message}
                                </p>
                              </div>
                            )}
                          </div>
                        ) : (
                          <>
                            {!isExpanded ? (
                              <>
                                <p className="line-clamp-2 text-gray-700">
                                  {proposal.description}
                                </p>
                                {parsedOptions.length > 0 && (
                                  <div className="rounded-md border border-gray-200 bg-gray-50 p-3">
                                    <div className="mb-2 flex items-center justify-between">
                                      <span className="text-xs font-medium text-gray-500 uppercase">
                                        Options
                                      </span>
                                      <span className="text-xs text-gray-500">
                                        {displayedTotalVotes} vote
                                        {displayedTotalVotes !== 1 ? "s" : ""}
                                      </span>
                                    </div>
                                    <div className="space-y-1">
                                      {parsedOptions.map((option, index) => {
                                        const stats = voteSnapshot.byChoice.get(
                                          option.choice,
                                        );
                                        return (
                                          <div
                                            key={`${option.choice}-${index}`}
                                            className="flex items-center justify-between gap-3 text-sm"
                                          >
                                            <span className="text-gray-700">
                                              {option.label}
                                            </span>
                                            <span className="font-medium text-gray-500">
                                              {stats?.voteCount ?? 0}
                                            </span>
                                          </div>
                                        );
                                      })}
                                    </div>
                                  </div>
                                )}
                              </>
                            ) : (
                              <Tabs defaultValue="details" className="w-full">
                                <TabsList className="grid w-full grid-cols-2">
                                  <TabsTrigger value="details">
                                    Details
                                  </TabsTrigger>
                                  <TabsTrigger value="discussion">
                                    <MessageSquare className="mr-1 h-4 w-4" />
                                    Discussion
                                  </TabsTrigger>
                                </TabsList>

                                <TabsContent
                                  value="details"
                                  className="mt-4 space-y-4"
                                >
                                  <div>
                                    <h4 className="mb-2 font-semibold">
                                      Description
                                    </h4>
                                    <p className="whitespace-pre-wrap text-gray-700">
                                      {proposal.description}
                                    </p>
                                  </div>

                                  {proposal.creator && (
                                    <div>
                                      <h4 className="mb-2 font-semibold">
                                        Proposed by
                                      </h4>
                                      <p className="text-sm text-gray-600">
                                        {proposal.creator.name ??
                                          "Unnamed member"}
                                      </p>
                                    </div>
                                  )}

                                  <div className="space-y-3">
                                    <div className="flex items-center justify-between">
                                      <h4 className="font-semibold">
                                        Voting Options
                                      </h4>
                                      <span className="text-sm text-gray-600">
                                        {displayedTotalVotes} total votes
                                      </span>
                                    </div>

                                    {parsedOptions.length > 0 ? (
                                      <div className="space-y-2 rounded-md border border-gray-200 bg-gray-50 p-3">
                                        {parsedOptions.map((option, index) => {
                                          const stats =
                                            voteSnapshot.byChoice.get(
                                              option.choice,
                                            );
                                          return (
                                            <div
                                              key={`${option.choice}-${index}`}
                                              className="flex items-center justify-between gap-3 text-sm"
                                            >
                                              <span className="text-gray-700">
                                                {option.label}
                                              </span>
                                              <span className="text-right font-medium text-gray-500">
                                                {stats?.voteCount ?? 0} vote
                                                {(stats?.voteCount ?? 0) === 1
                                                  ? ""
                                                  : "s"}
                                                {stats
                                                  ? ` · ${stats.percentage}% power`
                                                  : ""}
                                              </span>
                                            </div>
                                          );
                                        })}
                                      </div>
                                    ) : (
                                      <p className="text-sm text-gray-500">
                                        No options defined
                                      </p>
                                    )}
                                  </div>

                                  <div className="space-y-2">
                                    <div className="flex items-center justify-between">
                                      <h4 className="font-semibold">
                                        Quorum Progress
                                      </h4>
                                      <span className="text-sm text-gray-600">
                                        {quorumInfo.votingPowerNeeded > 0
                                          ? `${quorumInfo.votingPowerNeeded.toLocaleString()} more voting power needed`
                                          : "Quorum reached"}
                                      </span>
                                    </div>
                                    <Progress
                                      value={quorumInfo.progress}
                                      className="h-2"
                                    >
                                      <div
                                        className={`h-full rounded-full ${quorumInfo.progress >= 100 ? "bg-green-600" : "bg-blue-600"}`}
                                        style={{
                                          width: `${quorumInfo.progress}%`,
                                        }}
                                      />
                                    </Progress>
                                    <p className="text-xs text-gray-500">
                                      {displayedVotingPower.toLocaleString()} /{" "}
                                      {quorumInfo.quorumTarget.toLocaleString()}{" "}
                                      voting power / {displayedTotalVotes} vote
                                      {displayedTotalVotes === 1 ? "" : "s"}
                                    </p>
                                  </div>

                                  <div className="space-y-1 rounded-lg border border-gray-200 bg-gray-50 p-3 text-sm">
                                    <div className="flex justify-between">
                                      <span className="text-gray-600">
                                        Threshold:
                                      </span>
                                      <span className="font-medium">
                                        {proposal.threshold}%
                                      </span>
                                    </div>
                                    {proposal.votingStart && (
                                      <div className="flex justify-between gap-4">
                                        <span className="text-gray-600">
                                          Voting started:
                                        </span>
                                        <span className="text-right font-medium">
                                          {new Date(
                                            proposal.votingStart,
                                          ).toLocaleDateString()}
                                        </span>
                                      </div>
                                    )}
                                    {proposal.votingEnd && (
                                      <div className="flex justify-between">
                                        <span className="text-gray-600">
                                          Ends:
                                        </span>
                                        <span className="font-medium">
                                          {new Date(
                                            proposal.votingEnd,
                                          ).toLocaleDateString()}
                                        </span>
                                      </div>
                                    )}
                                  </div>

                                  {isMember &&
                                    proposal.status === "ACTIVE" &&
                                    voteSnapshot.isLoading && (
                                      <div className="flex items-center gap-2 border-t pt-3 text-sm text-gray-600">
                                        <Loader2 className="h-4 w-4 animate-spin" />
                                        Checking your voting status…
                                      </div>
                                    )}

                                  {isMember &&
                                    proposal.status === "ACTIVE" &&
                                    voteSnapshot.errorMessage && (
                                      <div className="rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                                        Vote history is temporarily unavailable.
                                        Voting is paused here to prevent a
                                        duplicate submission.
                                      </div>
                                    )}

                                  {isMember &&
                                    currentUserId &&
                                    proposal.status === "ACTIVE" &&
                                    !voteSnapshot.isLoading &&
                                    !voteSnapshot.errorMessage &&
                                    !voteSnapshot.currentUserVote &&
                                    parsedOptions.length > 0 && (
                                      <div className="space-y-3 border-t pt-2">
                                        <h4 className="font-semibold">
                                          Cast Your Vote
                                        </h4>
                                        <label
                                          htmlFor={`proposal-vote-reason-${proposal.id}`}
                                          className="sr-only"
                                        >
                                          Optional reason for your vote
                                        </label>
                                        <Textarea
                                          id={`proposal-vote-reason-${proposal.id}`}
                                          placeholder="Optional: Explain your vote (visible to other members)"
                                          value={voteReasons[proposal.id] || ""}
                                          onChange={(e) =>
                                            setVoteReasons({
                                              ...voteReasons,
                                              [proposal.id]: e.target.value,
                                            })
                                          }
                                          className="min-h-20"
                                        />
                                        <div className="flex flex-wrap gap-2">
                                          {parsedOptions.map(
                                            (option, index) => (
                                              <Button
                                                key={`${option.choice}-${index}`}
                                                onClick={() =>
                                                  handleVote(
                                                    proposal.id,
                                                    option.choice,
                                                  )
                                                }
                                                disabled={
                                                  voteMutation.isPending
                                                }
                                                variant="outline"
                                                className="min-w-[120px] flex-1"
                                              >
                                                {voteMutation.isPending &&
                                                voteMutation.variables
                                                  ?.proposalId ===
                                                  proposal.id ? (
                                                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                                ) : null}
                                                {option.label}
                                              </Button>
                                            ),
                                          )}
                                        </div>
                                        {voteMutation.error &&
                                          voteMutation.variables?.proposalId ===
                                            proposal.id && (
                                            <p className="text-sm text-red-700">
                                              {voteMutation.error.message}
                                            </p>
                                          )}
                                      </div>
                                    )}

                                  {voteSnapshot.currentUserVote && (
                                    <div className="flex items-start gap-3 rounded-md border border-blue-200 bg-blue-50 p-4">
                                      <Check className="mt-0.5 h-5 w-5 flex-shrink-0 text-blue-600" />
                                      <div>
                                        <p className="font-medium text-blue-900">
                                          You have voted on this proposal
                                        </p>
                                        <p className="mt-1 text-sm text-blue-700">
                                          Your vote for{" "}
                                          {parsedOptions.find(
                                            (option) =>
                                              option.choice ===
                                              voteSnapshot.currentUserVote
                                                ?.choice,
                                          )?.label ??
                                            `option ${voteSnapshot.currentUserVote.choice + 1}`}{" "}
                                          has been recorded.
                                        </p>
                                      </div>
                                    </div>
                                  )}
                                </TabsContent>

                                <TabsContent
                                  value="discussion"
                                  className="mt-4 space-y-4"
                                >
                                  <ProposalDiscussion
                                    projectId={projectId}
                                    proposalId={proposal.id}
                                    isMember={isMember}
                                  />
                                </TabsContent>
                              </Tabs>
                            )}
                          </>
                        )}
                      </CardContent>
                    </Card>
                  );
                }}
              </ProposalVoteData>
            );
          })}
        </div>
      )}
    </div>
  );
}
