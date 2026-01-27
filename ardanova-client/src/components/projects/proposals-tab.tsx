"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Vote, Plus, Check, X, Loader2 } from "lucide-react";

interface ProposalsTabProps {
  projectId: string;
  isOwner: boolean;
  isMember: boolean;
}

type ProposalType = "TREASURY" | "GOVERNANCE" | "STRATEGIC" | "OPERATIONAL" | "EMERGENCY";

export default function ProposalsTab({ projectId, isOwner, isMember }: ProposalsTabProps) {
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [formData, setFormData] = useState({
    type: "GOVERNANCE" as ProposalType,
    title: "",
    description: "",
    options: ["Yes", "No"],
    quorum: 50,
    threshold: 66,
    votingDays: 7,
  });

  const utils = api.useUtils();

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
    onMutate: async ({ proposalId, choice }) => {
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
    voteMutation.mutate({ proposalId, choice });
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
                    />
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
                    />
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
                    />
                  </div>
                </div>
                <Button type="submit" disabled={createMutation.isPending} className="w-full">
                  {createMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    "Create Proposal"
                  )}
                </Button>
                {createMutation.error && (
                  <p className="text-sm text-red-600">{createMutation.error.message}</p>
                )}
              </form>
            </CardContent>
          )}
        </Card>
      )}

      {proposals && proposals.length === 0 ? (
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
          {proposals?.map((proposal) => (
            <Card key={proposal.id}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      {getTypeBadge(proposal.type)}
                      {getStatusBadge(proposal.status)}
                    </div>
                    <CardTitle>{proposal.title}</CardTitle>
                  </div>
                  {isOwner && proposal.status === "ACTIVE" && (
                    <Button
                      onClick={() => handleCloseProposal(proposal.id)}
                      disabled={closeMutation.isPending}
                      variant="outline"
                      size="sm"
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
                <p className="text-gray-700">{proposal.description}</p>

                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <Check className="h-4 w-4 text-green-600" />
                    <span className="text-sm font-medium">
                      Yes: {proposal.votesFor || 0}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <X className="h-4 w-4 text-red-600" />
                    <span className="text-sm font-medium">
                      No: {proposal.votesAgainst || 0}
                    </span>
                  </div>
                </div>

                {isMember && proposal.status === "ACTIVE" && !proposal.userVoted && (
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
                      Vote Yes
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
                      Vote No
                    </Button>
                  </div>
                )}

                {proposal.userVoted && (
                  <div className="rounded-md bg-blue-50 border border-blue-200 p-3 text-sm text-blue-800">
                    You have already voted on this proposal.
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
