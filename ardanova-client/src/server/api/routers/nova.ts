import { TRPCError } from "@trpc/server";
import { z } from "zod";
import { apiClient } from "~/lib/api";
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
  state: z.string().max(512).optional(),
  sessionId: z.string().max(64).optional(),
});

/** Maps the .NET Nova status to a tRPC code; member-facing copy lives in the component. */
export function novaErrorCode(status: number): TRPCError["code"] {
  if (status === 400) return "BAD_REQUEST";
  if (status === 401) return "UNAUTHORIZED";
  if (status === 404) return "NOT_FOUND";
  if (status === 429) return "TOO_MANY_REQUESTS";
  return "INTERNAL_SERVER_ERROR";
}

/** Thin proxy to POST /api/nova/project-refinement (actor-signed). */
export const novaRouter = createTRPCRouter({
  refineProject: protectedProcedure
    .input(refineInput)
    .mutation(async ({ input }) => {
      const response = await apiClient.nova.refineProject(input);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: novaErrorCode(response.status),
          message: "Nova AI could not answer this turn.",
        });
      }
      return response.data;
    }),
});
