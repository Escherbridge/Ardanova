import { TRPCError } from "@trpc/server";
import { z } from "zod";
import { apiClient } from "~/lib/api";
import { moneyRefusalError } from "~/server/api/money-refusal-error";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

/** The sponsorship void's two 200 answers. An outcome word this app does not know is never "voided". */
const voidAnswerSchema = z.discriminatedUnion("outcome", [
  z.object({ outcome: z.literal("voided"), intentId: z.string().nullish() }),
  z.object({
    outcome: z.literal("revealed"),
    intentId: z.string().min(1),
    intent: z
      .object({
        intentId: z.string().min(1),
        status: z.string().min(1),
        holdingReasonDetail: z.string().nullish(),
      })
      .passthrough(),
  }),
]);

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

  /**
   * Void-or-reveal for a saved sponsorship key: `voided` is the ONLY answer that lets the invest
   * page drop the key; `revealed` names the sponsorship it must follow instead.
   */
  voidRequest: protectedProcedure
    .input(z.object({ idempotencyKey: z.string().uuid() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.fundingIntents.voidRequest(input.idempotencyKey);
      if (response.error || !response.data) {
        throw moneyRefusalError(
          response,
          response.error ?? "We couldn't check that sponsorship just now, so it stays saved.",
        );
      }

      const answer = voidAnswerSchema.safeParse(response.data);
      if (!answer.success) {
        // No money code: an answer nobody can read keeps the key.
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: "We couldn't check that sponsorship just now, so it stays saved.",
        });
      }

      return answer.data;
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
