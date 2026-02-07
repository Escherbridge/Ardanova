import { z } from "zod";
import { TRPCError } from "@trpc/server";
import type { PrismaClient } from "@prisma/client";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import {
  mintCredential,
  revokeCredential,
  suspendCredential,
  reactivateCredential,
  getCredentialsByProject,
  getCredentialsByUser,
  getCredentialByProjectAndUser,
} from "~/server/api/services/membership-credential.service";

// ---------------------------------------------------------------------------
// Zod schemas
// ---------------------------------------------------------------------------

const MembershipGrantTypeSchema = z.enum([
  "FOUNDER",
  "DAO_VOTE",
  "CONTRIBUTION_THRESHOLD",
  "APPLICATION_APPROVED",
  "GAME_SDK_THRESHOLD",
]);

const grantMembershipCredentialSchema = z.object({
  projectId: z.string().min(1),
  userId: z.string().min(1),
  grantedVia: MembershipGrantTypeSchema,
  grantedByProposalId: z.string().min(1).optional(),
});

// ---------------------------------------------------------------------------
// Helper: verify caller is project owner
// ---------------------------------------------------------------------------

async function verifyProjectOwner(
  db: PrismaClient,
  projectId: string,
  callerId: string
) {
  const project = await db.project.findUnique({
    where: { id: projectId },
    select: { createdById: true },
  });

  if (!project) {
    throw new TRPCError({ code: "NOT_FOUND", message: "Project not found" });
  }

  if (project.createdById !== callerId) {
    throw new TRPCError({
      code: "FORBIDDEN",
      message: "Only the project owner can perform this action",
    });
  }

  return project;
}

// ---------------------------------------------------------------------------
// Router
// ---------------------------------------------------------------------------

export const membershipCredentialRouter = createTRPCRouter({
  // ---- Queries ----

  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      return getCredentialsByProject({
        db: ctx.db,
        projectId: input.projectId,
      });
    }),

  getActiveByProjectId: publicProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      return ctx.db.membershipCredential.findMany({
        where: { projectId: input.projectId, status: "ACTIVE" },
        include: { user: true },
        orderBy: { createdAt: "desc" },
      });
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const credential = await ctx.db.membershipCredential.findUnique({
        where: { id: input.id },
      });

      if (!credential) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      return credential;
    }),

  getByUserId: protectedProcedure
    .input(z.object({ userId: z.string().optional() }))
    .query(async ({ input, ctx }) => {
      const userId = input.userId ?? ctx.session.user.id;
      return getCredentialsByUser({ db: ctx.db, userId });
    }),

  getMyCredential: protectedProcedure
    .input(z.object({ projectId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      return getCredentialByProjectAndUser({
        db: ctx.db,
        projectId: input.projectId,
        userId,
      });
    }),

  checkCredential: publicProcedure
    .input(z.object({ projectId: z.string().min(1), userId: z.string().min(1) }))
    .query(async ({ input, ctx }) => {
      const credential = await getCredentialByProjectAndUser({
        db: ctx.db,
        projectId: input.projectId,
        userId: input.userId,
      });

      if (!credential) {
        return { hasCredential: false, status: null as string | null };
      }

      return { hasCredential: true, status: credential.status };
    }),

  // ---- Mutations ----

  grant: protectedProcedure
    .input(grantMembershipCredentialSchema)
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      // For DAO_VOTE grants, the proposal system handles authorization
      // For other grants, only the project owner can grant
      if (input.grantedVia !== "DAO_VOTE") {
        await verifyProjectOwner(ctx.db, input.projectId, callerId);
      }

      return mintCredential({
        db: ctx.db,
        projectId: input.projectId,
        userId: input.userId,
        grantedVia: input.grantedVia,
        grantedByProposalId: input.grantedByProposalId,
      });
    }),

  revoke: protectedProcedure
    .input(z.object({ id: z.string().min(1), revokeTxHash: z.string().optional() }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      // Fetch credential to find associated project
      const credential = await ctx.db.membershipCredential.findUnique({
        where: { id: input.id },
      });

      if (!credential) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(ctx.db, credential.projectId, callerId);

      return revokeCredential({
        db: ctx.db,
        credentialId: input.id,
        revokeTxHash: input.revokeTxHash,
      });
    }),

  suspend: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      const credential = await ctx.db.membershipCredential.findUnique({
        where: { id: input.id },
      });

      if (!credential) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(ctx.db, credential.projectId, callerId);

      return suspendCredential({
        db: ctx.db,
        credentialId: input.id,
      });
    }),

  reactivate: protectedProcedure
    .input(z.object({ id: z.string().min(1) }))
    .mutation(async ({ input, ctx }) => {
      const callerId = ctx.session.user.id;

      const credential = await ctx.db.membershipCredential.findUnique({
        where: { id: input.id },
      });

      if (!credential) {
        throw new TRPCError({
          code: "NOT_FOUND",
          message: "Membership credential not found",
        });
      }

      await verifyProjectOwner(ctx.db, credential.projectId, callerId);

      return reactivateCredential({
        db: ctx.db,
        credentialId: input.id,
      });
    }),
});
