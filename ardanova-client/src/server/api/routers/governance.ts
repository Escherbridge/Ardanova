import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Proposal type enum
const ProposalType = z.enum([
  "Treasury",
  "Governance",
  "Strategic",
  "Operational",
  "Emergency",
  "Constitutional",
  "Token",
]);

// Proposal status enum
const ProposalStatus = z.enum([
  "Draft",
  "Active",
  "Passed",
  "Rejected",
  "Executed",
  "Expired",
]);

// Vote type enum
const VoteType = z.enum(["for", "against", "abstain"]);

// Proposal creation input schema
const createProposalSchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(50, "Description must be at least 50 characters"),
  type: ProposalType,
  votingDuration: z.number().min(1).max(30).default(7), // days
  quorum: z.number().min(1).max(100).default(25), // percentage
  projectId: z.string().min(1, "Project ID is required"),
});

// Proposal update input schema
const updateProposalSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(50).optional(),
});

export const governanceRouter = createTRPCRouter({
  // Create a new proposal
  create: protectedProcedure
    .input(createProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Calculate voting end date
      const votingEnds = new Date();
      votingEnds.setDate(votingEnds.getDate() + input.votingDuration);

      const response = await apiClient.governance.create({
        projectId: input.projectId,
        creatorId: userId,
        type: input.type,
        title: input.title,
        description: input.description,
        options: JSON.stringify([
          { label: "For", choice: 0 },
          { label: "Against", choice: 1 },
          { label: "Abstain", choice: 2 }
        ]),
        quorum: input.quorum,
        threshold: 51,
        votingEnd: votingEnds.toISOString(),
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create proposal");
      }

      return response.data;
    }),

  // Get all proposals with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        type: ProposalType.optional(),
        status: ProposalStatus.optional(),
        projectId: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.governance.search({
        searchTerm: input.search,
        type: input.type,
        status: input.status,
        projectId: input.projectId,
        page: input.page,
        pageSize: input.limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(input.page + 1) : undefined,
        totalCount: response.data?.totalCount ?? 0,
        totalPages: response.data?.totalPages ?? 0,
      };
    }),

  // Get active proposals
  getActive: publicProcedure
    .input(z.object({ limit: z.number().min(1).max(20).default(10) }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getActive(input.limit);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Get proposal by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getById(input.id);

      if (!response.data) {
        throw new Error("Proposal not found");
      }

      return response.data;
    }),

  // Get proposal by ID with project slug for redirects
  getByIdWithProjectSlug: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getById(input.id);
      if (!response.data) {
        throw new Error("Proposal not found");
      }

      // Get project for slug
      const projectResponse = await apiClient.projects.getById(response.data.projectId);

      return {
        ...response.data,
        projectSlug: projectResponse.data?.slug ?? response.data.projectId,
      };
    }),

  // Get user's proposals
  getMyProposals: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.governance.getByCreatorId(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get vote summary for a proposal
  getVoteSummary: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getVoteSummary(input.proposalId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data;
    }),

  // Vote on a proposal
  vote: protectedProcedure
    .input(
      z.object({
        proposalId: z.string(),
        vote: VoteType,
        reason: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.governance.vote(input.proposalId, {
        voterId: userId,
        choice: mapVoteToChoice(input.vote),
        reason: input.reason,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cast vote");
      }

      return { success: true, vote: response.data };
    }),

  // Get user's vote on a proposal
  getMyVote: protectedProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.governance.getMyVote(input.proposalId, userId);

      if (response.error && response.status !== 404) {
        throw new Error(response.error);
      }

      return response.data ?? null;
    }),

  // Get votes for a proposal
  getVotes: publicProcedure
    .input(
      z.object({
        proposalId: z.string(),
        limit: z.number().min(1).max(100).default(50),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.governance.getVotes(input.proposalId);

      if (response.error) {
        throw new Error(response.error);
      }

      // Limit results on client side since API might not support limit
      const votes = response.data ?? [];
      return votes.slice(0, input.limit);
    }),

  // Update proposal (only draft proposals)
  update: protectedProcedure
    .input(updateProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership and status
      const existing = await apiClient.governance.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Proposal not found");
      }

      if (existing.data.creatorId !== userId) {
        throw new Error("Access denied: You do not own this proposal");
      }

      if (existing.data.status !== "Draft") {
        throw new Error("Can only update draft proposals");
      }

      const response = await apiClient.governance.update(id, {
        title: data.title,
        description: data.description,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update proposal");
      }

      return response.data;
    }),

  // Execute a passed proposal
  execute: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership and status
      const existing = await apiClient.governance.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Proposal not found");
      }

      if (existing.data.creatorId !== userId) {
        throw new Error("Access denied: You do not own this proposal");
      }

      if (existing.data.status !== "Passed") {
        throw new Error("Can only execute passed proposals");
      }

      const response = await apiClient.governance.execute(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to execute proposal");
      }

      return { success: true, proposal: response.data };
    }),

  // Cancel a proposal
  cancel: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.governance.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Proposal not found");
      }

      if (existing.data.creatorId !== userId) {
        throw new Error("Access denied: You do not own this proposal");
      }

      if (existing.data.status !== "Draft" && existing.data.status !== "Active") {
        throw new Error("Can only cancel draft or active proposals");
      }

      const response = await apiClient.governance.cancel(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to cancel proposal");
      }

      return { success: true, proposal: response.data };
    }),

  // Delete proposal (only draft proposals)
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership and status
      const existing = await apiClient.governance.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Proposal not found");
      }

      if (existing.data.creatorId !== userId) {
        throw new Error("Access denied: You do not own this proposal");
      }

      if (existing.data.status !== "Draft") {
        throw new Error("Can only delete draft proposals");
      }

      const response = await apiClient.governance.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true, deletedId: input.id };
    }),
});

// Helper function to map frontend vote type to backend choice number
function mapVoteToChoice(vote: string): number {
  const mapping: Record<string, number> = {
    for: 0,
    against: 1,
    abstain: 2,
  };
  return mapping[vote] ?? 0;
}
