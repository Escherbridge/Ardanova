import { TRPCError } from "@trpc/server";

import { apiClient } from "~/lib/api";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

function toAccountError(status: number | undefined, message: string) {
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

function readableError(error: string | undefined, fallback: string): string {
  return !error || /^HTTP \d{3}$/u.test(error.trim()) ? fallback : error;
}

export const azoaCustodialAccountRouter = createTRPCRouter({
  getCapabilities: protectedProcedure.query(async () => {
    const response = await apiClient.azoaCustodialAccount.getCapabilities();
    if (!response.data) {
      throw toAccountError(
        response.status,
        readableError(
          response.error,
          "Azoa custody and identity capabilities are unavailable right now.",
        ),
      );
    }
    return response.data;
  }),

  getStatus: protectedProcedure.query(async () => {
    const response = await apiClient.azoaCustodialAccount.getStatus();
    if (!response.data) {
      throw toAccountError(
        response.status,
        readableError(
          response.error,
          "Secure Azoa account status is not available in this environment yet.",
        ),
      );
    }
    return response.data;
  }),

  ensure: protectedProcedure.mutation(async () => {
    const response = await apiClient.azoaCustodialAccount.ensure();
    if (!response.data) {
      throw toAccountError(
        response.status,
        readableError(
          response.error,
          "Secure account setup cannot continue until Azoa custody is ready.",
        ),
      );
    }
    return response.data;
  }),

  beginKyc: protectedProcedure.mutation(async () => {
    const response = await apiClient.azoaCustodialAccount.beginKyc();
    if (!response.data) {
      throw toAccountError(
        response.status,
        readableError(
          response.error,
          "Identity verification cannot start until the selected Azoa provider is ready.",
        ),
      );
    }
    return response.data;
  }),
});
