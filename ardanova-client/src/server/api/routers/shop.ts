import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";

// Shop category enum
const ShopCategory = z.enum([
  "Technology",
  "Design",
  "Sustainability",
  "Handmade",
  "Digital",
  "Services",
  "Food",
  "Fashion",
]);

// Shop creation input schema
const createShopSchema = z.object({
  name: z.string().min(1, "Shop name is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  category: ShopCategory,
  email: z.string().email("Invalid email address"),
  website: z.string().url().optional(),
  logo: z.string().optional(),
  tags: z.string().optional(),
});

// Shop update input schema
const updateShopSchema = z.object({
  id: z.string(),
  name: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  category: ShopCategory.optional(),
  email: z.string().email().optional(),
  website: z.string().url().optional(),
  logo: z.string().optional(),
  tags: z.string().optional(),
});

export const shopRouter = createTRPCRouter({
  // Create a new shop
  create: protectedProcedure
    .input(createShopSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Implement API call when backend endpoint is ready
      // For now, return a mock response
      return {
        id: crypto.randomUUID(),
        slug: input.name.toLowerCase().replace(/\s+/g, "-"),
        ...input,
        ownerId: userId,
        createdAt: new Date().toISOString(),
      };
    }),

  // Get all shops with pagination
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        category: ShopCategory.optional(),
      })
    )
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      return {
        items: [],
        nextCursor: undefined,
        totalCount: 0,
        totalPages: 0,
      };
    }),

  // Get shop by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Shop not found");
    }),

  // Update shop
  update: protectedProcedure
    .input(updateShopSchema)
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Not implemented");
    }),

  // Delete shop
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),
});
