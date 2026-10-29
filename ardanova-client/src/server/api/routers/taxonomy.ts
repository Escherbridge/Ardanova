import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  taxonomyKindCodeSchema,
  taxonomyTermListDtoSchema,
} from "~/lib/contracts/taxonomy-contract";

function invalidTaxonomyContract(): TRPCError {
  return new TRPCError({
    code: "BAD_GATEWAY",
    message: "Taxonomy response did not match the taxonomy API contract",
  });
}

export const taxonomyRouter = createTRPCRouter({
  // All active taxonomy terms, optionally filtered to one kind. Vocabulary is cheap to
  // read and rarely changes, so this stays public like the enum router.
  getAll: publicProcedure
    .input(z.object({ kind: taxonomyKindCodeSchema.optional() }).optional())
    .query(async ({ input }) => {
      const response = await apiClient.taxonomy.getAll(input?.kind);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to fetch taxonomy terms");
      }

      const result = taxonomyTermListDtoSchema.safeParse(response.data);
      if (!result.success) throw invalidTaxonomyContract();
      return result.data;
    }),
});
