import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Shop category enum (matches .NET backend)
const ShopCategory = z.enum([
  "RETAIL",
  "SERVICES",
  "DIGITAL_PRODUCTS",
  "FOOD_BEVERAGE",
  "HEALTH_WELLNESS",
  "TECHNOLOGY",
  "FASHION",
  "HOME_GARDEN",
  "ARTS_CRAFTS",
  "EDUCATION",
  "OTHER",
]);

// Shop creation input schema (matches backend CreateShopDto)
const createShopSchema = z.object({
  name: z.string().min(1, "Shop name is required"),
  description: z.string().optional(),
  category: ShopCategory,
  industry: z.string().optional(),
});

// Shop update input schema (matches backend UpdateShopDto)
const updateShopSchema = z.object({
  id: z.string(),
  name: z.string().min(1).optional(),
  description: z.string().optional(),
  category: ShopCategory.optional(),
  industry: z.string().optional(),
  address: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email().optional(),
  website: z.string().url().optional(),
  logo: z.string().optional(),
});

// Product schemas
const createProductSchema = z.object({
  shopId: z.string(),
  name: z.string().min(1, "Product name is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  price: z.number().positive("Price must be positive"),
  stock: z.number().int().nonnegative("Stock must be non-negative").default(0),
  images: z.string().optional(),
  category: z.string().optional(),
  isActive: z.boolean().default(true),
});

const updateProductSchema = z.object({
  productId: z.string(),
  name: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  price: z.number().positive().optional(),
  stock: z.number().int().nonnegative().optional(),
  images: z.string().optional(),
  category: z.string().optional(),
  isActive: z.boolean().optional(),
});

// Review schema
const createReviewSchema = z.object({
  shopId: z.string(),
  rating: z.number().int().min(1).max(5, "Rating must be between 1 and 5"),
  title: z.string().min(1, "Review title is required"),
  content: z.string().min(10, "Review must be at least 10 characters"),
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
        industry: input.industry,
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
        industry: data.industry,
        address: data.address,
        phone: data.phone,
        email: data.email,
        website: data.website,
        logo: data.logo,
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

  // ========================================
  // SHOP PRODUCTS
  // ========================================

  // Get products for a shop
  getProducts: publicProcedure
    .input(z.object({ shopId: z.string() }))
    .query(async ({ input }) => {
      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.getProducts(input.shopId);

      // Placeholder implementation
      throw new Error("Not implemented: Shop products endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error) {
      //   throw new Error(response.error);
      // }
      // return response.data ?? [];
    }),

  // Get product by ID
  getProductById: publicProcedure
    .input(z.object({ productId: z.string() }))
    .query(async ({ input }) => {
      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.getProductById(input.productId);

      // Placeholder implementation
      throw new Error("Not implemented: Shop product details endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error || !response.data) {
      //   throw new Error(response.error ?? "Product not found");
      // }
      // return response.data;
    }),

  // Create product (owner only)
  createProduct: protectedProcedure
    .input(createProductSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify shop ownership
      const shop = await apiClient.shops.getById(input.shopId);
      if (shop.error || !shop.data) {
        throw new Error("Shop not found");
      }

      if (shop.data.ownerId !== userId) {
        throw new Error("Access denied: You do not own this shop");
      }

      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.createProduct(input.shopId, {
      //   name: input.name,
      //   description: input.description,
      //   price: input.price,
      //   stock: input.stock,
      //   images: input.images,
      //   category: input.category,
      //   isActive: input.isActive,
      // });

      // Placeholder implementation
      throw new Error("Not implemented: Create product endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error || !response.data) {
      //   throw new Error(response.error ?? "Failed to create product");
      // }
      // return response.data;
    }),

  // Update product (owner only)
  updateProduct: protectedProcedure
    .input(updateProductSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { productId, ...data } = input;

      // TODO: Replace with actual API call when backend endpoint is ready
      // Get product to verify shop ownership
      // const product = await apiClient.shops.getProductById(productId);
      // if (product.error || !product.data) {
      //   throw new Error("Product not found");
      // }

      // Verify shop ownership
      // const shop = await apiClient.shops.getById(product.data.shopId);
      // if (shop.error || !shop.data) {
      //   throw new Error("Shop not found");
      // }

      // if (shop.data.ownerId !== userId) {
      //   throw new Error("Access denied: You do not own this shop");
      // }

      // const response = await apiClient.shops.updateProduct(productId, data);

      // Placeholder implementation
      throw new Error("Not implemented: Update product endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error || !response.data) {
      //   throw new Error(response.error ?? "Failed to update product");
      // }
      // return response.data;
    }),

  // Delete product (owner only)
  deleteProduct: protectedProcedure
    .input(z.object({ productId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Replace with actual API call when backend endpoint is ready
      // Get product to verify shop ownership
      // const product = await apiClient.shops.getProductById(input.productId);
      // if (product.error || !product.data) {
      //   throw new Error("Product not found");
      // }

      // Verify shop ownership
      // const shop = await apiClient.shops.getById(product.data.shopId);
      // if (shop.error || !shop.data) {
      //   throw new Error("Shop not found");
      // }

      // if (shop.data.ownerId !== userId) {
      //   throw new Error("Access denied: You do not own this shop");
      // }

      // const response = await apiClient.shops.deleteProduct(input.productId);

      // Placeholder implementation
      throw new Error("Not implemented: Delete product endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error) {
      //   throw new Error(response.error ?? "Failed to delete product");
      // }
      // return { success: true };
    }),

  // ========================================
  // SHOP REVIEWS
  // ========================================

  // Get reviews for a shop
  getReviews: publicProcedure
    .input(z.object({ shopId: z.string() }))
    .query(async ({ input }) => {
      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.getReviews(input.shopId);

      // Placeholder implementation
      throw new Error("Not implemented: Shop reviews endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error) {
      //   throw new Error(response.error);
      // }
      // return response.data ?? [];
    }),

  // Create review
  createReview: protectedProcedure
    .input(createReviewSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.createReview(input.shopId, {
      //   userId: userId,
      //   rating: input.rating,
      //   title: input.title,
      //   content: input.content,
      // });

      // Placeholder implementation
      throw new Error("Not implemented: Create review endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error || !response.data) {
      //   throw new Error(response.error ?? "Failed to create review");
      // }
      // return response.data;
    }),

  // ========================================
  // SHOP FOLLOW/UNFOLLOW
  // ========================================

  // Follow a shop
  follow: protectedProcedure
    .input(z.object({ shopId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.follow(input.shopId, userId);

      // Placeholder implementation
      throw new Error("Not implemented: Follow shop endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error || !response.data) {
      //   throw new Error(response.error ?? "Failed to follow shop");
      // }
      // return response.data;
    }),

  // Unfollow a shop
  unfollow: protectedProcedure
    .input(z.object({ shopId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.unfollow(input.shopId, userId);

      // Placeholder implementation
      throw new Error("Not implemented: Unfollow shop endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error) {
      //   throw new Error(response.error ?? "Failed to unfollow shop");
      // }
      // return { success: true };
    }),

  // Get followers of a shop
  getFollowers: publicProcedure
    .input(z.object({ shopId: z.string() }))
    .query(async ({ input }) => {
      // TODO: Replace with actual API call when backend endpoint is ready
      // const response = await apiClient.shops.getFollowers(input.shopId);

      // Placeholder implementation
      throw new Error("Not implemented: Shop followers endpoint not available in backend yet");

      // When backend is ready, uncomment:
      // if (response.error) {
      //   throw new Error(response.error);
      // }
      // return response.data ?? [];
    }),
});
