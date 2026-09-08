import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  PROJECT_MEDIA_MAX_FILES,
  PROJECT_MEDIA_MAX_FILE_BYTES,
  isSupportedProjectMediaType,
} from "~/lib/contracts/project-create-contract";

/**
 * Folders are part of the storage key the backend builds, so they are restricted to a
 * plain slug here: no separators, no traversal, no absolute paths.
 */
const uploadFolderSchema = z
  .string()
  .min(1)
  .max(64)
  .regex(/^[a-z0-9][a-z0-9-]*$/, "Folder must be a lowercase slug");

/**
 * Every presigned upload passes this shape. The type allow-list and the size cap are
 * enforced here, on the server, for both the single and bulk entry points - a client
 * that skips the wizard gets the same answer as one that does not.
 */
const uploadFileSchema = z
  .object({
    fileName: z.string().min(1).max(256),
    contentType: z
      .string()
      .min(1)
      .max(128)
      .refine(isSupportedProjectMediaType, "Unsupported file type"),
    fileSize: z
      .number()
      .int()
      .positive()
      .max(
        PROJECT_MEDIA_MAX_FILE_BYTES,
        `Files must be ${Math.floor(PROJECT_MEDIA_MAX_FILE_BYTES / 1_000_000)}MB or smaller`,
      ),
    folder: uploadFolderSchema.optional(),
  })
  .strict();

export const attachmentRouter = createTRPCRouter({
  getUploadUrl: protectedProcedure
    .input(uploadFileSchema)
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
      z
        .object({
          files: z.array(uploadFileSchema).min(1).max(PROJECT_MEDIA_MAX_FILES),
        })
        .strict()
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const files = input.files.map((file) => ({
        fileName: file.fileName,
        contentType: file.contentType,
        fileSize: file.fileSize,
        folder: file.folder,
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

  // The uploader is the authenticated caller. A caller-supplied owner is not accepted,
  // so an attachment can never be attributed to someone else's account.
  create: protectedProcedure
    .input(
      z
        .object({
          bucketPath: z.string().min(1).max(1_024),
          type: z.string().min(1).max(128),
          fileName: z.string().max(256).optional(),
          fileSize: z.number().int().positive().optional(),
        })
        .strict()
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.attachments.create({
        uploadedById: userId,
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
