import { z } from "zod";
import { TRPCError } from "@trpc/server";
import { createTRPCRouter, protectedProcedure, adminProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import type { KycDocumentType } from "~/lib/api/ardanova/endpoints/kyc";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const KycDocumentTypeSchema = z.enum([
  "GOVERNMENT_ID",
  "PASSPORT",
  "DRIVERS_LICENSE",
  "SELFIE",
  "PROOF_OF_ADDRESS",
]);

const submitKycDocumentSchema = z.object({
  type: KycDocumentTypeSchema,
  fileUrl: z.string().url(),
  fileName: z.string().min(1),
  mimeType: z.string().optional(),
  fileSizeBytes: z.number().int().positive().optional(),
  metadata: z.string().optional(),
});

const submitKycSchema = z.object({
  documents: z.array(submitKycDocumentSchema).min(1),
});

const reviewKycSchema = z.object({
  reviewNotes: z.string().optional(),
  rejectionReason: z.string().optional(),
});

// ---------------------------------------------------------------------------
// Router - thin proxy to .NET API via apiClient
// ---------------------------------------------------------------------------

export const kycRouter = createTRPCRouter({
  // ---- Queries ----

  getMyStatus: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.kyc.getStatus(userId);
    // Return null if no submission exists (expected for users who haven't submitted)
    if (response.error || !response.data) {
      return null;
    }
    return response.data;
  }),

  getById: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input }) => {
      const response = await apiClient.kyc.getById(input.id);
      if (response.error || !response.data) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: response.error ?? "KYC submission not found",
        });
      }
      return response.data;
    }),

  getPending: adminProcedure.query(async () => {
    const response = await apiClient.kyc.getPending();
    if (response.error) {
      throw new TRPCError({
        code: "INTERNAL_SERVER_ERROR",
        message: response.error,
      });
    }
    return response.data ?? [];
  }),

  // ---- Mutations ----

  submit: protectedProcedure
    .input(submitKycSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.kyc.submit({
        userId,
        documents: input.documents.map((doc) => ({
          type: doc.type as KycDocumentType,
          fileUrl: doc.fileUrl,
          fileName: doc.fileName,
          mimeType: doc.mimeType,
          fileSizeBytes: doc.fileSizeBytes,
          metadata: doc.metadata,
        })),
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to submit KYC",
        });
      }

      return response.data;
    }),

  approve: adminProcedure
    .input(z.object({ id: z.string().min(1) }).merge(reviewKycSchema))
    .mutation(async ({ input, ctx }) => {
      const reviewerId = ctx.session.user.id;
      const response = await apiClient.kyc.approve(input.id, {
        reviewerId,
        reviewNotes: input.reviewNotes,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to approve KYC submission",
        });
      }

      return response.data;
    }),

  reject: adminProcedure
    .input(z.object({ id: z.string().min(1) }).merge(reviewKycSchema))
    .mutation(async ({ input, ctx }) => {
      const reviewerId = ctx.session.user.id;
      const response = await apiClient.kyc.reject(input.id, {
        reviewerId,
        reviewNotes: input.reviewNotes,
        rejectionReason: input.rejectionReason,
      });

      if (response.error || !response.data) {
        throw new TRPCError({
          code: "BAD_REQUEST",
          message: response.error ?? "Failed to reject KYC submission",
        });
      }

      return response.data;
    }),
});
