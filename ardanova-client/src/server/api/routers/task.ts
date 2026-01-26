import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";

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
  projectId: z.string().optional(),
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
  tags: z.string().optional(),
});

export const taskRouter = createTRPCRouter({
  // Create a new task
  create: protectedProcedure
    .input(createTaskSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Implement API call when backend endpoint is ready
      return {
        id: crypto.randomUUID(),
        ...input,
        status: "backlog" as const,
        createdById: userId,
        createdAt: new Date().toISOString(),
      };
    }),

  // Get all tasks with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        status: TaskStatus.optional(),
        priority: TaskPriority.optional(),
        type: TaskType.optional(),
        projectId: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      return {
        items: [],
        nextCursor: undefined,
        totalCount: 0,
        totalPages: 0,
      };
    }),

  // Get user's tasks
  getMyTasks: protectedProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        status: TaskStatus.optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { items: [], nextCursor: undefined };
    }),

  // Get task by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Task not found");
    }),

  // Update task
  update: protectedProcedure
    .input(updateTaskSchema)
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Not implemented");
    }),

  // Update task status
  updateStatus: protectedProcedure
    .input(z.object({ id: z.string(), status: TaskStatus }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { id: input.id, status: input.status };
    }),

  // Delete task
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),
});
