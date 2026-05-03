import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Enums matching DBML/backend exactly (SCREAMING_SNAKE_CASE)
const TaskType = z.enum([
  "FEATURE",
  "BUG",
  "ENHANCEMENT",
  "DOCUMENTATION",
  "RESEARCH",
  "DESIGN",
  "TESTING",
  "REVIEW",
  "MAINTENANCE",
  "OTHER",
]);

const TaskPriority = z.enum(["LOW", "MEDIUM", "HIGH", "URGENT"]);

const TaskStatus = z.enum([
  "TODO",
  "IN_PROGRESS",
  "REVIEW",
  "COMPLETED",
  "BLOCKED",
]);

const EffortEstimate = z.enum(["XS", "S", "M", "L", "XL"]);

// Task creation input schema
const createTaskSchema = z.object({
  title: z.string().min(1, "Task title is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  type: TaskType,
  priority: TaskPriority.default("MEDIUM"),
  effortEstimate: EffortEstimate.optional(),
  dueDate: z.string().optional(),
  assigneeId: z.string().optional(),
  projectId: z.string(),
  pbiId: z.string().optional(),
  featureId: z.string().optional(),
  sprintId: z.string().optional(),
  epicId: z.string().optional(),
  milestoneId: z.string().optional(),
  guildId: z.string().optional(),
});

// Task update input schema
const updateTaskSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  type: TaskType.optional(),
  priority: TaskPriority.optional(),
  status: TaskStatus.optional(),
  effortEstimate: EffortEstimate.optional(),
  dueDate: z.string().optional(),
  assigneeId: z.string().optional(),
  pbiId: z.string().optional(),
});

export const taskRouter = createTRPCRouter({
  // Create a new task (opportunity auto-creation handled by .NET backend)
  create: protectedProcedure
    .input(createTaskSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.tasks.create({
        projectId: input.projectId,
        pbiId: input.pbiId,
        featureId: input.featureId,
        sprintId: input.sprintId,
        epicId: input.epicId,
        milestoneId: input.milestoneId,
        guildId: input.guildId,
        title: input.title,
        description: input.description,
        taskType: input.type,
        priority: input.priority,
        effortEstimate: input.effortEstimate,
        estimatedHours: input.effortEstimate ? effortToHours(input.effortEstimate) : undefined,
        dueDate: input.dueDate,
        assignedToId: input.assigneeId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create task");
      }

      return response.data;
    }),

  // Get all tasks with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        status: TaskStatus.optional(),
        priority: TaskPriority.optional(),
        type: TaskType.optional(),
        projectId: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.tasks.search({
        searchTerm: input.search,
        status: input.status,
        priority: input.priority,
        taskType: input.type,
        projectId: input.projectId,
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

  // Get user's tasks (assigned to them)
  getMyTasks: protectedProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        status: TaskStatus.optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const response = await apiClient.tasks.getByUserId(userId);

      if (response.error) {
        throw new Error(response.error);
      }

      let items = response.data ?? [];

      if (input.status) {
        items = items.filter(task => task.status === input.status);
      }

      items = items.slice(0, input.limit);

      return { items, nextCursor: undefined };
    }),

  // Get tasks by PBI ID
  getByPbiId: publicProcedure
    .input(z.object({ pbiId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.tasks.getByPbiId(input.pbiId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Get task by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.tasks.getById(input.id);

      if (!response.data) {
        throw new Error("Task not found");
      }

      return response.data;
    }),

  // Update task
  update: protectedProcedure
    .input(updateTaskSchema)
    .mutation(async ({ input }) => {
      const { id, ...data } = input;

      const response = await apiClient.tasks.update(id, {
        title: data.title,
        description: data.description,
        taskType: data.type,
        priority: data.priority,
        status: data.status,
        effortEstimate: data.effortEstimate,
        estimatedHours: data.effortEstimate ? effortToHours(data.effortEstimate) : undefined,
        dueDate: data.dueDate,
        assignedToId: data.assigneeId,
        pbiId: data.pbiId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update task");
      }

      return response.data;
    }),

  // Update task status
  updateStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: TaskStatus }))
    .mutation(async ({ input }) => {
      const response = await apiClient.tasks.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update task status");
      }

      return response.data;
    }),

  // Delete task
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.tasks.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});

// Helper function to convert effort enum to estimated hours
function effortToHours(effort: string): number {
  const mapping: Record<string, number> = {
    XS: 1,
    S: 3,
    M: 8,
    L: 20,
    XL: 40,
  };
  return mapping[effort] ?? 8;
}
