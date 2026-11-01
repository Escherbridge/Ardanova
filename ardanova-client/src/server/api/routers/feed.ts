import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, publicProcedure } from "~/server/api/trpc";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { apiClient } from "~/lib/api";
import { feedPageSchema, feedScopeSchema } from "~/lib/contracts/feed-contract";

/**
 * The ranked feed: a thin proxy over GET /api/feed. Ranking, visibility and paging are the API's; this
 * router only attaches the reader as the signed actor (signed out: PUBLIC only) and checks the contract.
 * Shaped for `useInfiniteQuery`: input `{ cursor?, limit, scope? }`, output `{ items, nextCursor }`.
 */

type FeedReader = { id?: string | null; role?: string } | null | undefined;

async function readAsViewer<T>(reader: FeedReader, call: () => Promise<T>): Promise<T> {
  if (!reader?.id) return call();
  return runWithActorAssertion({ subject: reader.id, role: reader.role }, call);
}

export const feedRouter = createTRPCRouter({
  getPage: publicProcedure
    .input(
      z.object({
        cursor: z.string().min(1).max(4096).nullish(),
        limit: z.number().int().min(1).max(50).default(20),
        scope: feedScopeSchema.optional(),
      }),
    )
    .query(async ({ input, ctx }) => {
      const response = await readAsViewer(ctx.session?.user, () =>
        apiClient.feed.getPage({
          cursor: input.cursor ?? undefined,
          limit: input.limit,
          scope: input.scope,
        }),
      );

      if (response.error || !response.data) {
        // A project this reader may not see 404s exactly like a missing one; a stale or foreign cursor 400s.
        throw new TRPCError({
          code:
            response.status === 404
              ? "NOT_FOUND"
              : response.status === 400
                ? "BAD_REQUEST"
                : "INTERNAL_SERVER_ERROR",
          message: response.error ?? "Failed to load the feed",
        });
      }

      const page = feedPageSchema.safeParse(response.data);
      if (!page.success) {
        throw new TRPCError({
          code: "BAD_GATEWAY",
          message: "Feed response did not match the feed API contract",
        });
      }
      return page.data;
    }),
});
