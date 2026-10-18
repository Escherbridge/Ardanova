import { z } from "zod";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const OpportunityBidStatus = z.enum([
  "SUBMITTED",
  "UNDER_REVIEW",
  "ACCEPTED",
  "REJECTED",
  "WITHDRAWN",
  "COMPLETED",
]);

/**
 * A guild's share of its own bid, in whole units. The server decides whether a split is
 * acceptable (OpportunityBidService.CreateAsync) and whether it can be held
 * (TaskCommerceService.ResolveBeneficiariesAsync); this only keeps an obviously impossible
 * value from making the round trip.
 */
const guildAwardSplitSchema = z.number().int().positive();

const createOpportunityBidSchema = z.object({
  opportunityId: z.string().min(1),
  guildId: z.string().min(1).optional(),
  proposedAmount: z.number().positive().optional(),
  guildAwardSplit: guildAwardSplitSchema.optional(),
  proposal: z.string().max(2000),
  estimatedHours: z.number().int().positive().optional(),
  timeline: z.string().optional(),
  deliverables: z.string().optional(),
});

const updateOpportunityBidSchema = z.object({
  proposedAmount: z.number().positive().optional(),
  guildAwardSplit: guildAwardSplitSchema.optional(),
  proposal: z.string().max(2000).optional(),
  estimatedHours: z.number().int().positive().optional(),
  timeline: z.string().optional(),
  deliverables: z.string().optional(),
});

export const opportunityBidRouter = createTRPCRouter({
  getByOpportunityId: protectedProcedure
    .input(z.object({ opportunityId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunityBids.getByOpportunityId(
        input.opportunityId,
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: protectedProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunityBids.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Opportunity bid not found");
      }

      return response.data;
    }),

  getByBidderId: protectedProcedure.query(async () => {
    const response = await apiClient.opportunityBids.getMine();

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  getByGuildId: protectedProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.opportunityBids.getByGuildId(
        input.guildId,
      );

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  create: protectedProcedure
    .input(createOpportunityBidSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // If guildId is provided, verify guild membership
      if (input.guildId) {
        const guild = await apiClient.guilds.getById(input.guildId);
        if (guild.error || !guild.data) {
          throw new Error("Guild not found");
        }

        // Verify user is a member of the guild
        const members = await apiClient.guilds.getMembers(input.guildId);
        if (members.error || !members.data) {
          throw new Error("Failed to verify guild membership");
        }

        const isMember = members.data.some(
          (member) => member.userId === userId,
        );
        if (!isMember) {
          throw new Error(
            "Only guild members can submit bids on behalf of the guild",
          );
        }
      }

      // Nothing here decides whether the bid may be posted. Whether the opportunity is still
      // taking proposals, and whether a guild split adds up, are settled by
      // OpportunityBidService.CreateAsync, which is what a direct call to the API also meets.
      const response = await apiClient.opportunityBids.create({
        opportunityId: input.opportunityId,
        guildId: input.guildId,
        proposedAmount: input.proposedAmount,
        guildAwardSplit: input.guildAwardSplit,
        proposal: input.proposal,
        estimatedHours: input.estimatedHours,
        timeline: input.timeline,
        deliverables: input.deliverables,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create opportunity bid");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateOpportunityBidSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get opportunity bid to verify ownership
      const bid = await apiClient.opportunityBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Opportunity bid not found");
      }

      // Verify user is the bidder
      if (bid.data.bidderId !== userId) {
        throw new Error("Access denied");
      }

      // Can only update bids in SUBMITTED or UNDER_REVIEW status
      if (
        bid.data.status !== "SUBMITTED" &&
        bid.data.status !== "UNDER_REVIEW"
      ) {
        throw new Error(
          "Can only update bids that are submitted or under review",
        );
      }

      const response = await apiClient.opportunityBids.update(
        input.id,
        input.data,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update opportunity bid");
      }

      return response.data;
    }),

  accept: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      // Who may accept is decided by the API (the opportunity's managers), not here.
      const response = await apiClient.opportunityBids.accept(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to accept opportunity bid");
      }

      return response.data;
    }),

  reject: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      // Who may reject is decided by the API (the opportunity's managers), not here.
      const response = await apiClient.opportunityBids.reject(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to reject opportunity bid");
      }

      return response.data;
    }),

  withdraw: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get opportunity bid to verify ownership
      const bid = await apiClient.opportunityBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Opportunity bid not found");
      }

      // Verify user is the bidder
      if (bid.data.bidderId !== userId) {
        throw new Error("Access denied");
      }

      // Can only withdraw bids in SUBMITTED or UNDER_REVIEW status
      if (
        bid.data.status !== "SUBMITTED" &&
        bid.data.status !== "UNDER_REVIEW"
      ) {
        throw new Error(
          "Can only withdraw bids that are submitted or under review",
        );
      }

      const response = await apiClient.opportunityBids.withdraw(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to withdraw opportunity bid");
      }

      return response.data;
    }),

  complete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      // Who may complete is decided by the API (the opportunity's managers), not here.
      const response = await apiClient.opportunityBids.complete(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to complete opportunity bid");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get opportunity bid to verify ownership
      const bid = await apiClient.opportunityBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Opportunity bid not found");
      }

      // Verify user is the bidder
      if (bid.data.bidderId !== userId) {
        throw new Error("Access denied");
      }

      // Can only delete bids in SUBMITTED, REJECTED, or WITHDRAWN status
      if (
        bid.data.status !== "SUBMITTED" &&
        bid.data.status !== "REJECTED" &&
        bid.data.status !== "WITHDRAWN"
      ) {
        throw new Error(
          "Can only delete bids that are submitted, rejected, or withdrawn",
        );
      }

      const response = await apiClient.opportunityBids.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete opportunity bid");
      }

      return { success: true };
    }),
});
