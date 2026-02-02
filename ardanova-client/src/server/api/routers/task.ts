import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Task type enum
const TaskType = z.enum([
  "feature",
  "bug",
  "improvement",
  "documentation",
  "research",
  "design",
  "other",
]);

// Task priority enum
const TaskPriority = z.enum(["low", "medium", "high", "critical"]);

// Task status enum
const TaskStatus = z.enum([
  "backlog",
  "todo",
  "in_progress",
  "in_review",
  "done",
  "cancelled",
]);

// Task effort enum
const TaskEffort = z.enum(["xs", "s", "m", "l", "xl"]);

// Task creation input schema
const createTaskSchema = z.object({
  title: z.string().min(1, "Task title is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  type: TaskType,
  priority: TaskPriority.default("medium"),
  effort: TaskEffort.optional(),
  dueDate: z.string().optional(),
  assigneeId: z.string().optional(),
  projectId: z.string(),
  pbiId: z.string().optional(),
  tags: z.string().optional(),
});

// Task update input schema
const updateTaskSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  type: TaskType.optional(),
  priority: TaskPriority.optional(),
  status: TaskStatus.optional(),
  effort: TaskEffort.optional(),
  dueDate: z.string().optional(),
  assigneeId: z.string().optional(),
  pbiId: z.string().optional(),
  tags: z.string().optional(),
});

export const taskRouter = createTRPCRouter({
  // Create a new task
  create: protectedProcedure
    .input(createTaskSchema)
    .mutation(async ({ input, ctx }) => {
      const response = await apiClient.tasks.create({
        projectId: input.projectId,
        pbiId: input.pbiId,
        title: input.title,
        description: input.description,
        taskType: input.type,
        priority: input.priority,
        estimatedHours: input.effort ? effortToHours(input.effort) : undefined,
        dueDate: input.dueDate,
        assignedToId: input.assigneeId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create task");
      }

      const taskData = response.data;

      // Auto-create a draft opportunity for this task
      try {
        const slug = `task-${taskData.id}-${Date.now()}`;
        await apiClient.opportunities.create({
          title: taskData.title,
          slug,
          description: taskData.description || taskData.title,
          type: 'TASK_BOUNTY',
          experienceLevel: 'MID',
          origin: 'TASK_GENERATED',
          status: 'DRAFT',
          projectId: input.projectId,
          taskId: taskData.id,
          posterId: ctx.session.user.id,
        });
      } catch {
        // Don't fail task creation if opportunity auto-gen fails
        console.error('Failed to auto-generate opportunity for task:', taskData.id);
      }

      return taskData;
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

      // Filter by status if provided
      if (input.status) {
        items = items.filter(task => task.status === input.status);
      }

      // Limit results
      items = items.slice(0, input.limit);

      return { items, nextCursor: undefined };
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
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify access (assignee can update)
      const existing = await apiClient.tasks.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Task not found");
      }

      // Allow if user is assigned to the task
      if (existing.data.assignedToId && existing.data.assignedToId !== userId) {
        throw new Error("Access denied: You are not assigned to this task");
      }

      const response = await apiClient.tasks.update(id, {
        title: data.title,
        description: data.description,
        taskType: data.type,
        priority: data.priority,
        status: data.status,
        estimatedHours: data.effort ? effortToHours(data.effort) : undefined,
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
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify access
      const existing = await apiClient.tasks.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Task not found");
      }

      if (existing.data.assignedToId && existing.data.assignedToId !== userId) {
        throw new Error("Access denied: You are not assigned to this task");
      }

      const response = await apiClient.tasks.updateStatus(input.id, input.status);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update task status");
      }

      return response.data;
    }),

  // Delete task
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify access
      const existing = await apiClient.tasks.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Task not found");
      }

      if (existing.data.assignedToId && existing.data.assignedToId !== userId) {
        throw new Error("Access denied: You are not assigned to this task");
      }

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
    xs: 1,
    s: 2,
    m: 4,
    l: 8,
    xl: 16,
  };
  return mapping[effort] ?? 4;
}
