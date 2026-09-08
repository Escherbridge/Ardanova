import { z } from "zod";
import { TRPCError } from "@trpc/server";
import {
  createTRPCRouter,
  protectedProcedure,
  publicProcedure,
} from "~/server/api/trpc";
import { type ApiResponse } from "~/lib/api";
import { apiClient, type Proposal } from "~/lib/api/ardanova";

// Filter labels the browse page already speaks. They are not the API's vocabulary, so they are translated below.
const ProposalTypeFilter = z.enum([
  "Treasury",
  "Governance",
  "Strategic",
  "Operational",
  "Emergency",
  "Constitutional",
  "Token",
]);

const ProposalStatusFilter = z.enum([
  "Draft",
  "Active",
  "Passed",
  "Rejected",
  "Executed",
  "Cancelled",
  "Expired",
]);

// The names a proposal can actually be created with — the .NET ProposalType members.
const ProposalTypeName = z.enum([
  "TREASURY",
  "GOVERNANCE",
  "STRATEGIC",
  "OPERATIONAL",
  "EMERGENCY",
  "CONSTITUTIONAL",
  "SHARES",
]);

/**
 * The browse page's labels, mapped onto the enum members the API will actually bind.
 * `Token` has no member of its own — it is the project-units proposal type — so filtering
 * on it used to reach the API as an unbindable value and come back as an error.
 */
const proposalTypeFilterToName: Record<
  z.infer<typeof ProposalTypeFilter>,
  z.infer<typeof ProposalTypeName>
> = {
  Treasury: "TREASURY",
  Governance: "GOVERNANCE",
  Strategic: "STRATEGIC",
  Operational: "OPERATIONAL",
  Emergency: "EMERGENCY",
  Constitutional: "CONSTITUTIONAL",
  Token: "SHARES",
};

/** The .NET ProposalStatus members, which are spelled in upper case. */
const proposalStatusFilterToName: Record<
  z.infer<typeof ProposalStatusFilter>,
  string
> = {
  Draft: "DRAFT",
  Active: "ACTIVE",
  Passed: "PASSED",
  Rejected: "REJECTED",
  Executed: "EXECUTED",
  Cancelled: "CANCELLED",
  Expired: "EXPIRED",
};

// The three-way shorthand the standalone proposal page still speaks.
const VoteType = z.enum(["for", "against", "abstain"]);
const voteTypeToChoice: Record<z.infer<typeof VoteType>, number> = {
  for: 0,
  against: 1,
  abstain: 2,
};

const createProposalSchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(50, "Description must be at least 50 characters"),
  type: ProposalTypeName,
  options: z.array(z.string().trim().min(1)).min(2).default(["Yes", "No"]),
  votingDuration: z.number().min(1).max(30).default(7), // days
  quorum: z.number().int().min(1).max(100).default(25), // share of members who must vote
  threshold: z.number().int().min(1).max(100).default(51), // share of votes cast needed to pass
  projectId: z.string().min(1, "Project ID is required"),
});

const updateProposalSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(50).optional(),
  options: z.array(z.string().trim().min(1)).min(2).optional(),
  quorum: z.number().int().min(1).max(100).optional(),
  threshold: z.number().int().min(1).max(100).optional(),
});

const castVoteSchema = z
  .object({
    proposalId: z.string(),
    // Either the option's number, or the three-way shorthand.
    choice: z.number().int().nonnegative().optional(),
    vote: VoteType.optional(),
    reason: z.string().optional(),
  })
  .refine((input) => input.choice !== undefined || input.vote !== undefined, {
    message: "Choose an option before voting",
    path: ["choice"],
  });

const votingEndFromDays = (days: number): string =>
  new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString();

/** Turns a backend refusal into the tRPC error that carries its plain-language reason to the reader. */
function apiError(response: ApiResponse<unknown>, fallback: string): TRPCError {
  const code =
    response.status === 401
      ? "UNAUTHORIZED"
      : response.status === 403
        ? "FORBIDDEN"
        : response.status === 404
          ? "NOT_FOUND"
          : response.status === 400
            ? "BAD_REQUEST"
            : "INTERNAL_SERVER_ERROR";

  return new TRPCError({ code, message: response.error ?? fallback });
}

function requireProposal(
  response: ApiResponse<Proposal>,
  fallback: string,
): Proposal {
  const proposal = response.data;
  if (response.error || !proposal) throw apiError(response, fallback);
  return proposal;
}

export const governanceRouter = createTRPCRouter({
  // Raise a proposal on a project the member belongs to
  create: protectedProcedure
    .input(createProposalSchema)
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.governance.create({
        projectId: input.projectId,
        creatorId: ctx.session.user.id,
        type: input.type,
        title: input.title,
        description: input.description,
        options: JSON.stringify(input.options),
        quorum: input.quorum,
        threshold: input.threshold,
        votingEnd: votingEndFromDays(input.votingDuration),
      });

      return requireProposal(response, "Failed to create proposal");
    }),

  // Get all proposals with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        type: ProposalTypeFilter.optional(),
        status: ProposalStatusFilter.optional(),
        projectId: z.string().optional(),
      }),
    )
    .query(async ({ input }) => {
      const response = await apiClient.governance.search({
        searchTerm: input.search,
        type: input.type ? proposalTypeFilterToName[input.type] : undefined,
        status: input.status
          ? proposalStatusFilterToName[input.status]
          : undefined,
        projectId: input.projectId,
        page: input.page,
        pageSize: input.limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage
          ? String(input.page + 1)
          : undefined,
        totalCount: response.data?.totalCount ?? 0,
        totalPages: response.data?.totalPages ?? 0,
      };
    }),

  // Get active proposals
  getActive: publicProcedure
    .input(z.object({ limit: z.number().min(1).max(20).default(10) }))
    .query(async ({ input }) => {
      const response = await apiClient.governance.getActive();

      if (response.error) {
        throw new Error(response.error);
      }

      // Limit results on client side
      const proposals = response.data ?? [];
      return proposals.slice(0, input.limit);
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
      const projectResponse = await apiClient.projects.getById(
        response.data.projectId,
      );

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

  /** The tally plus the size of the electorate it is measured against, so quorum can be read as members rather than as a bare number. */
  getVoteSummary: publicProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input }) => {
      const proposalResponse = await apiClient.governance.getById(
        input.proposalId,
      );
      const proposal = requireProposal(proposalResponse, "Proposal not found");

      const [summaryResponse, membersResponse] = await Promise.all([
        apiClient.governance.getVoteSummary(input.proposalId),
        apiClient.membershipCredentials.getActiveByProjectId(
          proposal.projectId,
        ),
      ]);

      const summary = summaryResponse.data;
      if (summaryResponse.error || !summary) {
        throw apiError(summaryResponse, "Failed to read the vote summary");
      }

      const eligibleMembers = membersResponse.data?.length ?? 0;
      const votesNeededForQuorum =
        eligibleMembers > 0
          ? Math.min(
              Math.max(Math.ceil((eligibleMembers * proposal.quorum) / 100), 1),
              eligibleMembers,
            )
          : 1;

      return {
        ...summary,
        eligibleMembers,
        votesNeededForQuorum,
      };
    }),

  // Vote on a proposal
  vote: protectedProcedure
    .input(castVoteSchema)
    .mutation(async ({ input, ctx }) => {
      const choice =
        input.choice ?? voteTypeToChoice[input.vote ?? "abstain"];

      // `voterId` satisfies the request contract; the backend replaces it with the authenticated actor.
      const response = await apiClient.governance.vote(input.proposalId, {
        voterId: ctx.session.user.id,
        choice,
        reason: input.reason,
      });

      const vote = response.data;
      if (response.error || !vote) {
        throw apiError(response, "Failed to record your vote");
      }

      return vote;
    }),

  // Get user's vote on a proposal
  getMyVote: protectedProcedure
    .input(z.object({ proposalId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.governance.getMyVote(
        input.proposalId,
        userId,
      );

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
      }),
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

  /** Editing a proposal; the backend refuses every field but the closing time once voting has opened. */
  update: protectedProcedure
    .input(updateProposalSchema)
    .mutation(async ({ input }) => {
      const { id, options, ...rest } = input;
      const response = await apiClient.put<Proposal>(
        `/api/governance/proposals/${id}`,
        {
          ...rest,
          ...(options ? { options: JSON.stringify(options) } : {}),
        },
      );

      return requireProposal(response, "Failed to update proposal");
    }),

  // Carry out a decision the members passed
  execute: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.governance.execute(input.id);
      return requireProposal(response, "Failed to carry out this decision");
    }),

  publishProposal: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.governance.publishProposal(input.id);
      return requireProposal(response, "Failed to open this proposal to votes");
    }),

  /** Ends voting now and records the outcome. Distinct from `cancel`, which withdraws a proposal that no one has voted on. */
  close: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.patch<Proposal>(
        `/api/governance/proposals/${input.id}/close`,
        {},
      );

      return requireProposal(response, "Failed to close the vote");
    }),

  // Withdraw a proposal that has not been voted on
  cancel: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.governance.cancel(input.id);
      return requireProposal(response, "Failed to withdraw proposal");
    }),

  // Delete proposal (only draft proposals)
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.governance.delete(input.id);

      if (response.error) {
        throw apiError(response, "Failed to delete proposal");
      }

      return { id: input.id };
    }),
});
