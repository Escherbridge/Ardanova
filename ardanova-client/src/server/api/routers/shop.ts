import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

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

      const response = await apiClient.shops.create({
        ownerId: userId,
        name: input.name,
        description: input.description,
        category: input.category,
        contactEmail: input.email,
        website: input.website,
        logoUrl: input.logo,
        tags: input.tags,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create shop");
      }

      return response.data;
    }),

  // Get all shops with pagination and search
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        category: ShopCategory.optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.shops.search({
        searchTerm: input.search,
        category: input.category,
        page: input.page,
        pageSize: input.limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      return {
        items: response.data?.items ?? [],
        nextCursor: response.data?.hasNextPage ? String(input.page + 1) : undefined,
        totalCount: response.data?.totalCount ?? 0,
        totalPages: response.data?.totalPages ?? 0,
      };
    }),

  // Get user's shops
  getMyShops: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.shops.getByOwnerId(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get shop by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      let response = await apiClient.shops.getById(input.id);

      // Fallback to slug lookup
      if (response.status === 404 || !response.data) {
        response = await apiClient.shops.getBySlug(input.id);
      }

      if (!response.data) {
        throw new Error("Shop not found");
      }

      return response.data;
    }),

  // Update shop
  update: protectedProcedure
    .input(updateShopSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.shops.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Shop not found");
      }

      if (existing.data.ownerId !== userId) {
        throw new Error("Access denied: You do not own this shop");
      }

      const response = await apiClient.shops.update(id, {
        name: data.name,
        description: data.description,
        category: data.category,
        contactEmail: data.email,
        website: data.website,
        logoUrl: data.logo,
        tags: data.tags,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update shop");
      }

      return response.data;
    }),

  // Delete shop
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.shops.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Shop not found");
      }

      if (existing.data.ownerId !== userId) {
        throw new Error("Access denied: You do not own this shop");
      }

      const response = await apiClient.shops.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});
