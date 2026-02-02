import { z } from "zod";
import { createTRPCRouter, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const enumRouter = createTRPCRouter({
  // Get all available enum names
  getAll: publicProcedure.query(async () => {
    const response = await apiClient.enums.getAll();

    if (response.error || !response.data) {
      throw new Error(response.error ?? "Failed to fetch enum names");
    }

    return response.data;
  }),

  // Get values for a specific enum by name
  getByName: publicProcedure
    .input(z.object({ name: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.enums.getByName(input.name);

      if (response.error || !response.data) {
        throw new Error(response.error ?? `Failed to fetch enum '${input.name}'`);
      }

      return response.data;
    }),
});
