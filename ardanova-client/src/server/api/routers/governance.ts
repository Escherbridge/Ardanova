import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";

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
  summary: z.string().min(20, "Summary must be at least 20 characters"),
  description: z.string().min(50, "Description must be at least 50 characters"),
  type: ProposalType,
  rationale: z.string().optional(),
  votingDuration: z.number().min(1).max(30).default(7), // days
  quorum: z.number().min(1).max(100).default(25), // percentage
  projectId: z.string().optional(),
});

// Proposal update input schema
const updateProposalSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  summary: z.string().min(20).optional(),
  description: z.string().min(50).optional(),
  rationale: z.string().optional(),
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

      // TODO: Implement API call when backend endpoint is ready
      return {
        id: crypto.randomUUID(),
        ...input,
        status: "Active" as const,
        proposerId: userId,
        votesFor: 0,
        votesAgainst: 0,
        votesAbstain: 0,
        currentQuorum: 0,
        votingEnds: votingEnds.toISOString(),
        createdAt: new Date().toISOString(),
      };
    }),

  // Get all proposals with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        type: ProposalType.optional(),
        status: ProposalStatus.optional(),
        projectId: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      return {
        items: [],
        nextCursor: undefined,
        totalCount: 0,
        totalPages: 0,
      };
    }),

  // Get active proposals
  getActive: publicProcedure
    .input(z.object({ limit: z.number().min(1).max(20).default(10) }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      return [];
    }),

  // Get proposal by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Proposal not found");
    }),

  // Get user's proposals
  getMyProposals: protectedProcedure.query(async ({ ctx }) => {
    // TODO: Implement API call when backend endpoint is ready
    return [];
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
      // TODO: Implement API call when backend endpoint is ready
      return { success: true, voteId: crypto.randomUUID() };
    }),

  // Get user's vote on a proposal
  getMyVote: protectedProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return null;
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
      // TODO: Implement API call when backend endpoint is ready
      return [];
    }),

  // Update proposal (only draft proposals)
  update: protectedProcedure
    .input(updateProposalSchema)
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Not implemented");
    }),

  // Execute a passed proposal
  execute: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),

  // Cancel a proposal
  cancel: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),
});
