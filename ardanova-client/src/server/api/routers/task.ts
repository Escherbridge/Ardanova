import { z } from "zod";
import {
  createTRPCRouter,
  protectedProcedure,
  publicProcedure,
} from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import {
  hierarchyAuthorization,
  type HierarchyParent,
} from "~/server/api/lib/hierarchy-auth";

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

// Task creation input schema
const createTaskSchema = z.object({
  title: z.string().min(1, "Task title is required"),
  description: z.string().optional(),
  type: TaskType,
  priority: TaskPriority.default("MEDIUM"),
  estimatedHours: z.number().int().min(0).max(10_000).optional(),
  equityReward: z.number().nonnegative().optional(),
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
const updateTaskSchema = z
  .object({
    id: z.string(),
    title: z.string().min(1).optional(),
    description: z.string().optional(),
    type: TaskType.optional(),
    priority: TaskPriority.optional(),
    status: TaskStatus.optional(),
    estimatedHours: z.number().int().min(0).max(10_000).optional(),
    actualHours: z.number().int().min(0).max(10_000).optional(),
    equityReward: z.number().nonnegative().optional(),
    dueDate: z.string().optional(),
  })
  .strict();

export const taskRouter = createTRPCRouter({
  // Create a new task.
  create: protectedProcedure
    .input(createTaskSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const parents: HierarchyParent[] = [];
      if (input.milestoneId) {
        parents.push({ level: "milestone", id: input.milestoneId });
      }
      if (input.epicId) {
        parents.push({ level: "epic", id: input.epicId });
      }
      if (input.sprintId) {
        parents.push({ level: "sprint", id: input.sprintId });
      }
      if (input.featureId) {
        parents.push({ level: "feature", id: input.featureId });
      }
      if (input.pbiId) {
        parents.push({ level: "pbi", id: input.pbiId });
      }

      await hierarchyAuthorization.authorizeCreation(
        {
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        },
        parents,
      );
      if (
        input.assigneeId ||
        input.guildId ||
        input.equityReward !== undefined
      ) {
        await hierarchyAuthorization.requireProjectManager({
          userId,
          projectId: input.projectId,
          isAdmin: ctx.session.user.role === "ADMIN",
        });
      }
      if (input.assigneeId) {
        await hierarchyAuthorization.requireProjectMember(
          input.projectId,
          input.assigneeId,
        );
      }
      if (input.guildId) {
        await hierarchyAuthorization.requireAssignedGuild(
          input.projectId,
          input.guildId,
        );
      }

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
        estimatedHours: input.estimatedHours,
        equityReward: input.equityReward,
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
      }),
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
        nextCursor: response.data?.hasNextPage
          ? String(input.page + 1)
          : undefined,
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
      }),
    )
    .query(async ({ input }) => {
      const response = await apiClient.tasks.getMine();

      if (response.error) {
        throw new Error(response.error);
      }

      let items = response.data ?? [];

      if (input.status) {
        items = items.filter((task) => task.status === input.status);
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

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Task not found");
      }

      return response.data;
    }),

  getCommerce: protectedProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.tasks.getCommerce(input.id);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Commerce agreement not found");
      }

      return response.data;
    }),

  // Update task
  update: protectedProcedure
    .input(updateTaskSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      await hierarchyAuthorization.authorizeMutation(
        ctx.session.user.id,
        "task",
        id,
        data.equityReward !== undefined ? "structure" : "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.tasks.update(id, {
        title: data.title,
        description: data.description,
        taskType: data.type,
        priority: data.priority,
        status: data.status,
        estimatedHours: data.estimatedHours,
        actualHours: data.actualHours,
        equityReward: data.equityReward,
        dueDate: data.dueDate,
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
      await hierarchyAuthorization.authorizeMutation(
        ctx.session.user.id,
        "task",
        input.id,
        "work",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.tasks.updateStatus(
        input.id,
        input.status,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update task status");
      }

      return response.data;
    }),

  // Delete task
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      await hierarchyAuthorization.authorizeMutation(
        ctx.session.user.id,
        "task",
        input.id,
        "structure",
        ctx.session.user.role === "ADMIN",
      );

      const response = await apiClient.tasks.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});
