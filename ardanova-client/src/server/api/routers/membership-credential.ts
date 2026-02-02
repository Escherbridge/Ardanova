import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

const MembershipCredentialStatus = z.enum(['ACTIVE', 'REVOKED', 'SUSPENDED']);
const MembershipGrantType = z.enum(['FOUNDER', 'DAO_VOTE', 'CONTRIBUTION_THRESHOLD', 'APPLICATION_APPROVED', 'GAME_SDK_THRESHOLD']);

const grantMembershipCredentialSchema = z.object({
  projectId: z.string().min(1),
  userId: z.string().min(1),
  grantedVia: MembershipGrantType,
  grantedByProposalId: z.string().min(1).optional(),
});

export const membershipCredentialRouter = createTRPCRouter({
  getByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getActiveByProjectId: publicProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getActiveByProjectId(input.projectId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Membership credential not found");
      }

      return response.data;
    }),

  getByUserId: protectedProcedure
    .input(z.object({ userId: z.string().optional() }))
    .query(async ({ input, ctx }) => {
      const userId = input.userId ?? ctx.session.user.id;
      const response = await apiClient.membershipCredentials.getByUserId(userId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getMyCredential: protectedProcedure
    .input(z.object({ projectId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.membershipCredentials.getByProjectAndUser(input.projectId, userId);

      // Not having a credential is a valid state, not an error
      if (response.error || !response.data) {
        return null;
      }

      return response.data;
    }),

  checkCredential: publicProcedure
    .input(z.object({ projectId: z.string(), userId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.membershipCredentials.getByProjectAndUser(input.projectId, input.userId);

      if (response.error || !response.data) {
        return { hasCredential: false, status: null as string | null };
      }

      return { hasCredential: true, status: response.data.status };
    }),

  grant: protectedProcedure
    .input(grantMembershipCredentialSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify the caller has authority to grant credentials
      // Must be project founder or an approved DAO proposal execution
      const project = await apiClient.projects.getById(input.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      const isProjectOwner = project.data.createdById === userId;

      // For DAO_VOTE grants, the proposal system handles authorization
      // For other grants, only the project owner can grant
      if (input.grantedVia !== 'DAO_VOTE' && !isProjectOwner) {
        throw new Error("Only project owner can grant membership credentials");
      }

      const response = await apiClient.membershipCredentials.grant({
        projectId: input.projectId,
        userId: input.userId,
        grantedVia: input.grantedVia,
        grantedByProposalId: input.grantedByProposalId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to grant membership credential");
      }

      return response.data;
    }),

  revoke: protectedProcedure
    .input(z.object({ id: z.string(), revokeTxHash: z.string().optional() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get the credential to find the project
      const credential = await apiClient.membershipCredentials.getById(input.id);
      if (credential.error || !credential.data) {
        throw new Error("Membership credential not found");
      }

      // Verify the caller has authority (project owner or DAO execution)
      const project = await apiClient.projects.getById(credential.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      if (project.data.createdById !== userId) {
        throw new Error("Only project owner can revoke membership credentials");
      }

      const response = await apiClient.membershipCredentials.revoke(
        input.id,
        input.revokeTxHash ? { revokeTxHash: input.revokeTxHash } : undefined
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to revoke membership credential");
      }

      return response.data;
    }),

  suspend: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const credential = await apiClient.membershipCredentials.getById(input.id);
      if (credential.error || !credential.data) {
        throw new Error("Membership credential not found");
      }

      const project = await apiClient.projects.getById(credential.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      if (project.data.createdById !== userId) {
        throw new Error("Only project owner can suspend membership credentials");
      }

      const response = await apiClient.membershipCredentials.suspend(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to suspend membership credential");
      }

      return response.data;
    }),

  reactivate: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const credential = await apiClient.membershipCredentials.getById(input.id);
      if (credential.error || !credential.data) {
        throw new Error("Membership credential not found");
      }

      const project = await apiClient.projects.getById(credential.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      if (project.data.createdById !== userId) {
        throw new Error("Only project owner can reactivate membership credentials");
      }

      const response = await apiClient.membershipCredentials.reactivate(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to reactivate membership credential");
      }

      return response.data;
    }),
});
