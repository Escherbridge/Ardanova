import { z } from "zod";
import { createTRPCRouter, publicProcedure, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

export const TaskBidStatus = z.enum(['SUBMITTED', 'UNDER_REVIEW', 'ACCEPTED', 'REJECTED', 'WITHDRAWN', 'COMPLETED']);

const createTaskBidSchema = z.object({
  taskId: z.string().min(1),
  guildId: z.string().min(1),
  proposedAmount: z.number().positive().optional(),
  proposal: z.string().max(2000).optional(),
  estimatedHours: z.number().int().positive().optional(),
});

const updateTaskBidSchema = z.object({
  proposedAmount: z.number().positive().optional(),
  proposal: z.string().max(2000).optional(),
  estimatedHours: z.number().int().positive().optional(),
});

export const taskBidRouter = createTRPCRouter({
  getByTaskId: publicProcedure
    .input(z.object({ taskId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.taskBids.getByTaskId(input.taskId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getByGuildId: publicProcedure
    .input(z.object({ guildId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.taskBids.getByGuildId(input.guildId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.taskBids.getById(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Task bid not found");
      }

      return response.data;
    }),

  create: protectedProcedure
    .input(createTaskBidSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify guild membership
      const guild = await apiClient.guilds.getById(input.guildId);
      if (guild.error || !guild.data) {
        throw new Error("Guild not found");
      }

      // Verify user is a member of the guild
      const members = await apiClient.guilds.getMembers(input.guildId);
      if (members.error || !members.data) {
        throw new Error("Failed to verify guild membership");
      }

      const isMember = members.data.some((m: any) => m.userId === userId);
      if (!isMember) {
        throw new Error("Only guild members can submit bids");
      }

      const response = await apiClient.taskBids.create({
        ...input,
        createdById: userId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create task bid");
      }

      return response.data;
    }),

  update: protectedProcedure
    .input(z.object({ id: z.string(), data: updateTaskBidSchema }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid to verify ownership
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Verify user created this bid
      if (bid.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      // Can only update bids in SUBMITTED or UNDER_REVIEW status
      if (bid.data.status !== 'SUBMITTED' && bid.data.status !== 'UNDER_REVIEW') {
        throw new Error("Can only update bids that are submitted or under review");
      }

      const response = await apiClient.taskBids.update(input.id, input.data);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update task bid");
      }

      return response.data;
    }),

  accept: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Get task to verify ownership
      const task = await apiClient.tasks.getById(bid.data.taskId);
      if (task.error || !task.data) {
        throw new Error("Task not found");
      }

      // Verify user has permission to accept bids (task owner or project owner)
      const project = await apiClient.projects.getById(task.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      const isProjectOwner = project.data.createdById === userId;
      const isTaskOwner = task.data.createdById === userId;

      if (!isProjectOwner && !isTaskOwner) {
        throw new Error("Only project or task owner can accept bids");
      }

      const response = await apiClient.taskBids.accept(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to accept task bid");
      }

      return response.data;
    }),

  reject: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Get task to verify ownership
      const task = await apiClient.tasks.getById(bid.data.taskId);
      if (task.error || !task.data) {
        throw new Error("Task not found");
      }

      // Verify user has permission to reject bids (task owner or project owner)
      const project = await apiClient.projects.getById(task.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      const isProjectOwner = project.data.createdById === userId;
      const isTaskOwner = task.data.createdById === userId;

      if (!isProjectOwner && !isTaskOwner) {
        throw new Error("Only project or task owner can reject bids");
      }

      const response = await apiClient.taskBids.reject(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to reject task bid");
      }

      return response.data;
    }),

  withdraw: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid to verify ownership
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Verify user created this bid
      if (bid.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      // Can only withdraw bids in SUBMITTED or UNDER_REVIEW status
      if (bid.data.status !== 'SUBMITTED' && bid.data.status !== 'UNDER_REVIEW') {
        throw new Error("Can only withdraw bids that are submitted or under review");
      }

      const response = await apiClient.taskBids.withdraw(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to withdraw task bid");
      }

      return response.data;
    }),

  complete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Get task to verify ownership
      const task = await apiClient.tasks.getById(bid.data.taskId);
      if (task.error || !task.data) {
        throw new Error("Task not found");
      }

      // Verify user has permission to complete bids (task owner or project owner)
      const project = await apiClient.projects.getById(task.data.projectId);
      if (project.error || !project.data) {
        throw new Error("Project not found");
      }

      const isProjectOwner = project.data.createdById === userId;
      const isTaskOwner = task.data.createdById === userId;

      if (!isProjectOwner && !isTaskOwner) {
        throw new Error("Only project or task owner can complete bids");
      }

      // Can only complete ACCEPTED bids
      if (bid.data.status !== 'ACCEPTED') {
        throw new Error("Can only complete bids that have been accepted");
      }

      const response = await apiClient.taskBids.complete(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to complete task bid");
      }

      return response.data;
    }),

  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Get task bid to verify ownership
      const bid = await apiClient.taskBids.getById(input.id);
      if (bid.error || !bid.data) {
        throw new Error("Task bid not found");
      }

      // Verify user created this bid
      if (bid.data.createdById !== userId) {
        throw new Error("Access denied");
      }

      // Can only delete bids in SUBMITTED, REJECTED, or WITHDRAWN status
      if (bid.data.status !== 'SUBMITTED' && bid.data.status !== 'REJECTED' && bid.data.status !== 'WITHDRAWN') {
        throw new Error("Can only delete bids that are submitted, rejected, or withdrawn");
      }

      const response = await apiClient.taskBids.delete(input.id);

      if (response.error) {
        throw new Error(response.error ?? "Failed to delete task bid");
      }

      return { success: true };
    }),
});
