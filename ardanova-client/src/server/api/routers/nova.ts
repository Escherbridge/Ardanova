import { TRPCError } from "@trpc/server";
import { z } from "zod";
import { apiClient } from "~/lib/api";
import { NOVA_STATE_MAX_CHARS } from "~/lib/nova-state-limits";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

const refineInput = z.object({
  idea: z.string().trim().min(20).max(2000),
  turns: z
    .array(
      z.object({
        question: z.string().min(1).max(400),
        answer: z.string().trim().min(1).max(1500),
      }),
    )
    .max(8),
  state: z.string().max(NOVA_STATE_MAX_CHARS).optional(),
  sessionId: z.string().max(64).optional(),
});

/** Maps the .NET Nova status (and its error code) to a tRPC code; member-facing copy lives in the component. */
export function novaErrorCode(
  status: number,
  error?: string,
): TRPCError["code"] {
  if (status === 400) return "BAD_REQUEST";
  if (status === 401) return "UNAUTHORIZED";
  if (status === 404) return "NOT_FOUND";
  if (status === 408) return "TIMEOUT";
  if (status === 429 && error === "nova_turn_in_flight") return "CONFLICT";
  if (status === 429) return "TOO_MANY_REQUESTS";
  if (status === 503 && error === "nova_capacity") return "SERVICE_UNAVAILABLE";
  return "INTERNAL_SERVER_ERROR";
}

/** Thin proxy to the actor-signed .NET Nova routes. */
export const novaRouter = createTRPCRouter({
  status: protectedProcedure.query(async () => {
    const response = await apiClient.nova.status();
    // Unknown (API unreachable) is null, not "off": a brief outage must not read as "not on this deployment".
    return response.data ?? null;
  }),

  refineProject: protectedProcedure
    .input(refineInput)
    .mutation(async ({ input }) => {
      const response = await apiClient.nova.refineProject(input);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: novaErrorCode(response.status, response.error),
          message: "Nova AI could not answer this turn.",
        });
      }
      return response.data;
    }),
});
