import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { CreateProduct } from "~/lib/api/ardanova/endpoints/products";

const createProductSchema = z.object({
  projectId: z.string().min(1),
  name: z.string().min(1).max(200),
  price: z.number().min(0),
  description: z.string().optional(),
  sku: z.string().optional(),
  cost: z.number().optional(),
  category: z.string().optional(),
});

const updateProductSchema = z.object({
  name: z.string().min(1).max(200).optional(),
  description: z.string().optional(),
  sku: z.string().optional(),
  price: z.number().min(0).optional(),
  cost: z.number().optional(),
  category: z.string().optional(),
  isActive: z.boolean().optional(),
});

export const productRouter = createTRPCRouter({
  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.products.getByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.products.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Product not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createProductSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify project ownership
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.products.create({
        ...input,
        userId,
      } as CreateProduct);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create product");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateProductSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get product to verify ownership
      const product = await apiClient.products.getById(input.id);
      if (product.error || !product.data) {
        throw new Error("Product not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(product.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.products.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update product");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get product to verify ownership
      const product = await apiClient.products.getById(input.id);
      if (product.error || !product.data) {
        throw new Error("Product not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(product.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.products.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete product");
      }

      return { success: true };
    }),

  toggleActive: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get product to verify ownership
      const product = await apiClient.products.getById(input.id);
      if (product.error || !product.data) {
        throw new Error("Product not found");
      }

      // Verify project ownership
      const project = await apiClient.projects.getById(product.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }
      if (project.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      const response = await apiClient.products.toggleActive(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to toggle product");
      }

      return response.data;
    }),
});
