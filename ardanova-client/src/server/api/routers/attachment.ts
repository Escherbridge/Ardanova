import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const attachmentRouter = createTRPCRouter({
  getUploadUrl: protectedProcedure
    .input(
      z.object({
        fileName: z.string().min(1),
        contentType: z.string().min(1),
        fileSize: z.number().optional(),
        folder: z.string().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.attachments.getUploadUrl(userId, {
        fileName: input.fileName,
        contentType: input.contentType,
        fileSize: input.fileSize,
        folder: input.folder,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to get upload URL" });
      }
      return response.data;
    }),

  getUploadUrls: protectedProcedure
    .input(
      z.object({
        files: z.array(
          z.object({
            fileName: z.string().min(1),
            contentType: z.string().min(1),
            fileSize: z.number().optional(),
            folder: z.string().optional(),
          })
        ),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const files = input.files.map((f) => ({
        fileName: f.fileName,
        contentType: f.contentType,
        fileSize: f.fileSize,
        folder: f.folder,
      }));
      const response = await apiClient.attachments.getUploadUrls(userId, { files });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to get upload URLs" });
      }
      return response.data;
    }),

  getDownloadUrl: protectedProcedure
    .input(
      z.object({
        id: z.string(),
        expirationMinutes: z.number().min(1).max(24 * 60).optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.attachments.getDownloadUrl(input.id, input.expirationMinutes ?? 60);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to get download URL" });
      }
      return response.data;
    }),

  getPublicUrl: protectedProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.attachments.getPublicUrl(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to get public URL" });
      }
      return response.data;
    }),

  create: protectedProcedure
    .input(
      z.object({
        uploadedById: z.string().optional(),
        bucketPath: z.string().min(1),
        type: z.string().min(1),
        fileName: z.string().optional(),
        fileSize: z.number().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.attachments.create({
        uploadedById: input.uploadedById ?? userId,
        bucketPath: input.bucketPath,
        type: input.type,
        fileName: input.fileName,
        fileSize: input.fileSize,
      });
      if (response.error || !response.data) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error ?? "Failed to create attachment" });
      }
      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.attachments.delete(input.id);
      if (response.error) {
        throw new TRPCError({ code: "INTERNAL_SERVER_ERROR", message: response.error });
      }
      return { success: true };
    }),
});
