import { TRPCError } from "@trpc/server";
import { z } from "zod";

import { apiClient } from "~/lib/api";
import { parseMoneyRefusalCode } from "~/lib/commerce/money-refusal";
import {
  addFundsFallbackMessage,
  toSponsorSafeMessage,
} from "~/lib/commerce/on-ramp-status";
import { moneyRefusalError } from "~/server/api/money-refusal-error";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

/** Reached through the shared client, so these routes inherit its actor signing. */
const addFunds = apiClient.addFunds;

/** The server's own key alphabet, so a key it would refuse never leaves this process. */
const requestKeySchema = z
  .string()
  .min(1)
  .max(128)
  .regex(/^[A-Za-z0-9\-_:.]+$/u, "A request key may only contain plain characters");

/**
 * A payment id as the server writes it - 32 hex characters - or as a dashed UUID. `z.string().uuid()`
 * would reject every real return, because the address a person comes back on carries the compact
 * form.
 */
const paymentIdSchema = z
  .string()
  .regex(
    /^(?:[0-9a-fA-F]{32}|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$/u,
    "That isn't a payment we could look up",
  );

const beginInput = z.object({
  /** Whole cents. The server owns the minimum and maximum and says them in its own words. */
  amountMinorUnits: z.number().int().positive(),
  /**
   * Reuse the SAME key when retrying one attempt: the server resumes that person's open payment
   * for a repeated key, so a fresh key per tap would open a second payment page. Required - a
   * missing key is a client bug (the page failed to persist one before this call), and minting
   * one here would hide a lost-ref reload behind a silent second admission.
   */
  requestKey: requestKeySchema,
});

/** A payment as the BFF returns it; anything else in a 200 is not an answer this page can act on. */
const paymentSchema = z
  .object({
    paymentId: z.string().min(1),
    status: z.string(),
    settlementState: z.string(),
    paymentUrl: z.string().nullish(),
    resumed: z.boolean(),
    amountPaidMinorUnits: z.number().nullish(),
    currencyCode: z.string().nullish(),
    expiresAt: z.string().nullish(),
  })
  .passthrough();

/** The void route's two 200 answers. An outcome word this app does not know is never "voided". */
const voidAnswerSchema = z.discriminatedUnion("outcome", [
  z.object({ outcome: z.literal("voided") }),
  z.object({ outcome: z.literal("revealed"), payment: paymentSchema }),
]);

/** The fallback sentence for a failed call, picked by the BFF's money code - never by the status. */
function fallbackFor(response: { code?: string }) {
  return addFundsFallbackMessage(parseMoneyRefusalCode(response.code));
}

/** The tRPC class by status (presentation), carrying the BFF's money code (the key's fate). */
function toAddFundsError(
  response: { status: number; code?: string },
  message: string,
) {
  const status = response.status;
  const code =
    status === 403
      ? "FORBIDDEN"
      : status === 404
        ? "NOT_FOUND"
        : status === 400
          ? "BAD_REQUEST"
          : status === 409
            ? "PRECONDITION_FAILED"
            : "INTERNAL_SERVER_ERROR";
  return moneyRefusalError(response, message, code);
}

/**
 * Adding funds, and reading what happened to one payment.
 *
 * Every message that leaves here passes through `toSponsorSafeMessage`, so a transport-shaped
 * failure written for an operator is replaced rather than shown. Nothing here ever infers an
 * outcome: a failed read throws, and the page renders "we're confirming this" - never "done".
 */
export const addFundsRouter = createTRPCRouter({
  begin: protectedProcedure.input(beginInput).mutation(async ({ input }) => {
    const response = await addFunds.begin(
      { amountMinorUnits: input.amountMinorUnits },
      input.requestKey,
    );

    if (!response.data) {
      throw toAddFundsError(
        response,
        toSponsorSafeMessage(response.error, fallbackFor(response)),
      );
    }

    // A RESUMED payment with no page left is one already paid or closed under this key: returned
    // as-is so the page reads its real state instead of looping on a refusal for money that moved.
    if (!response.data.paymentUrl?.trim() && response.data.resumed) {
      return response.data;
    }

    // A NEW payment with nowhere to send the person is not a started payment. Refusing here stops
    // the page rendering a Continue that goes nowhere and calling it progress.
    if (!response.data.paymentUrl?.trim()) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message:
          "We couldn't open a payment page just now, so nothing was charged. Please try again in a moment.",
      });
    }

    return response.data;
  }),

  getPayment: protectedProcedure
    .input(z.object({ paymentId: paymentIdSchema }))
    .query(async ({ input }) => {
      const response = await addFunds.getPayment(input.paymentId);

      if (!response.data) {
        throw toAddFundsError(
          response,
          toSponsorSafeMessage(
            response.error,
            "We couldn't confirm this payment just now, so we won't say either way.",
          ),
        );
      }

      return response.data;
    }),

  /**
   * Void-or-reveal for a saved request key: the ONLY answer that lets the page drop a key it holds
   * without a server id (`voided`), and the way it learns the payment behind one (`revealed`).
   */
  voidRequest: protectedProcedure
    .input(z.object({ requestKey: requestKeySchema }))
    .mutation(async ({ input }) => {
      const response = await addFunds.voidRequest(input.requestKey);

      if (!response.data) {
        throw toAddFundsError(
          response,
          toSponsorSafeMessage(response.error, fallbackFor(response)),
        );
      }

      const answer = voidAnswerSchema.safeParse(response.data);
      if (!answer.success) {
        // No money code: an answer nobody can read keeps the key.
        throw new TRPCError({
          code: "INTERNAL_SERVER_ERROR",
          message: addFundsFallbackMessage(null),
        });
      }

      return answer.data;
    }),
});
