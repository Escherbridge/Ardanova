import { randomUUID } from "node:crypto";
import { z } from "zod";
import { apiClient } from "~/lib/api";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

const createCheckoutInput = z.object({
  projectTokenConfigId: z.string().min(1),
  amount: z.string().regex(/^\d+(?:\.\d{1,2})?$/, "Amount must be USD with at most two decimal places"),
  disclosureVersion: z.string().min(1).max(100),
  idempotencyKey: z.string().uuid().optional(),
});

export const fundingIntentRouter = createTRPCRouter({
  createCheckout: protectedProcedure
    .input(createCheckoutInput)
    .mutation(async ({ input }) => {
      const response = await apiClient.fundingIntents.createCheckout(
        {
          projectTokenConfigId: input.projectTokenConfigId,
          amount: input.amount,
          disclosureVersion: input.disclosureVersion,
        },
        input.idempotencyKey ?? randomUUID(),
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Unable to create funding checkout");
      }

      return response.data;
    }),

  getStatus: protectedProcedure
    .input(z.object({ intentId: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.fundingIntents.getStatus(input.intentId);
      if (response.error || !response.data) {
        throw new Error(response.error ?? "Funding intent not found");
      }

      return response.data;
    }),
});
