import { z } from "zod";
import { apiClient } from "~/lib/api";
import { moneyRefusalError } from "~/server/api/money-refusal-error";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

const createCheckoutInput = z.object({
  projectTokenConfigId: z.string().min(1),
  amount: z.string().regex(/^\d+(?:\.\d{1,2})?$/, "Amount must be USD with at most two decimal places"),
  disclosureVersion: z.string().min(1).max(100),
  /** Default false, never bare `.optional()`: an omitted preference means "leave me unnamed". */
  displayNameConsent: z.boolean().default(false),
  /**
   * Required - the server dedupes a sponsorship on this exact key (`FundingIntentService`
   * replays an existing intent rather than creating a second one), so a missing key is a client
   * bug that must fail loudly rather than quietly mint a fresh admission.
   */
  idempotencyKey: z.string().uuid(),
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
          displayNameConsent: input.displayNameConsent,
        },
        input.idempotencyKey,
      );

      if (response.error || !response.data) {
        // The BFF's money code travels with the error; the page decides the key's fate on it.
        throw moneyRefusalError(
          response,
          response.error ?? "Unable to create funding checkout",
        );
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
