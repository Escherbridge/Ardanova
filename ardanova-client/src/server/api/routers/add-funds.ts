import { randomUUID } from "node:crypto";
import { TRPCError } from "@trpc/server";
import { z } from "zod";

import { apiClient } from "~/lib/api";
import {
  ADD_FUNDS_UNAVAILABLE_MESSAGE,
  toSponsorSafeMessage,
} from "~/lib/commerce/on-ramp-status";
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
   * for a repeated key, so a fresh key per tap would open a second payment page.
   */
  requestKey: requestKeySchema.optional(),
});

function toAddFundsError(status: number | undefined, message: string) {
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
  return new TRPCError({ code, message });
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
      input.requestKey ?? randomUUID(),
    );

    if (!response.data) {
      throw toAddFundsError(
        response.status,
        toSponsorSafeMessage(response.error, ADD_FUNDS_UNAVAILABLE_MESSAGE),
      );
    }

    // A payment with nowhere to send the person is not a started payment. Refusing here stops the
    // page rendering a Continue that goes nowhere and calling it progress.
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
          response.status,
          toSponsorSafeMessage(
            response.error,
            "We couldn't confirm this payment just now, so we won't say either way.",
          ),
        );
      }

      return response.data;
    }),
});
