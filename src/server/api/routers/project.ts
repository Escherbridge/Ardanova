import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { db } from "~/server/db";
import { ProjectStatus, ProjectCategory } from "@prisma/client";

// Project creation input schema
const createProjectSchema = z.object({
  title: z.string().min(1, "Project title is required"),
  description: z.string().min(10, "Description must be at least 10 characters"),
  problemStatement: z.string().min(10, "Problem statement must be at least 10 characters"),
  solution: z.string().min(10, "Solution must be at least 10 characters"),
  category: z.nativeEnum(ProjectCategory),
  targetAudience: z.string().optional(),
  expectedImpact: z.string().optional(),
  timeline: z.string().optional(),
  tags: z.string().optional(),
  images: z.string().optional(),
  videos: z.string().optional(),
  documents: z.string().optional(),
});

// Project update input schema
const updateProjectSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(10).optional(),
  problemStatement: z.string().min(10).optional(),
  solution: z.string().min(10).optional(),
  category: z.nativeEnum(ProjectCategory).optional(),
  status: z.nativeEnum(ProjectStatus).optional(),
  targetAudience: z.string().optional(),
  expectedImpact: z.string().optional(),
  timeline: z.string().optional(),
  tags: z.string().optional(),
  images: z.string().optional(),
  videos: z.string().optional(),
  documents: z.string().optional(),
});

export const projectRouter = createTRPCRouter({
  // Create a new project
  create: protectedProcedure
    .input(createProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Generate slug from title
      const slug = input.title
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/(^-|-$)/g, "");

      const project = await db.project.create({
        data: {
          ...input,
          slug,
          createdById: userId,
          status: ProjectStatus.DRAFT,
        },
        include: {
          createdBy: {
            select: {
              id: true,
              name: true,
              email: true,
              image: true,
            },
          },
        },
      });

      return project;
    }),

  // Get all projects (public)
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        cursor: z.string().nullish(),
        category: z.nativeEnum(ProjectCategory).optional(),
        status: z.nativeEnum(ProjectStatus).optional(),
        search: z.string().optional(),
      })
    )
    .query(async ({ input }) => {
      const { limit, cursor, category, status, search } = input;

      const where = {
        ...(category && { category }),
        ...(status && { status }),
        ...(search && {
          OR: [
            { title: { contains: search, mode: "insensitive" } },
            { description: { contains: search, mode: "insensitive" } },
            { problemStatement: { contains: search, mode: "insensitive" } },
            { solution: { contains: search, mode: "insensitive" } },
          ],
        }),
      };

      const projects = await db.project.findMany({
        take: limit + 1,
        cursor: cursor ? { id: cursor } : undefined,
        where,
        orderBy: { createdAt: "desc" },
        include: {
          createdBy: {
            select: {
              id: true,
              name: true,
              email: true,
              image: true,
            },
          },
          _count: {
            select: {
              supports: true,
              comments: true,
              tasks: true,
            },
          },
        },
      });

      let nextCursor: typeof cursor | undefined = undefined;
      if (projects.length > limit) {
        const nextItem = projects.pop();
        nextCursor = nextItem?.id;
      }

      return {
        items: projects,
        nextCursor,
      };
    }),

  // Get project by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const project = await db.project.findFirst({
        where: {
          OR: [
            { id: input.id },
            { slug: input.id },
          ],
        },
        include: {
          createdBy: {
            select: {
              id: true,
              name: true,
              email: true,
              image: true,
              bio: true,
            },
          },
          tasks: {
            include: {
              assignedTo: {
                select: {
                  id: true,
                  name: true,
                  image: true,
                },
              },
            },
            orderBy: { createdAt: "asc" },
          },
          resources: {
            orderBy: { createdAt: "asc" },
          },
          milestones: {
            orderBy: { targetDate: "asc" },
          },
          supports: {
            include: {
              user: {
                select: {
                  id: true,
                  name: true,
                  image: true,
                },
              },
            },
            orderBy: { createdAt: "desc" },
          },
          comments: {
            include: {
              user: {
                select: {
                  id: true,
                  name: true,
                  image: true,
                },
              },
              replies: {
                include: {
                  user: {
                    select: {
                      id: true,
                      name: true,
                      image: true,
                    },
                  },
                },
              },
            },
            where: { parentId: null },
            orderBy: { createdAt: "desc" },
          },
          updates: {
            include: {
              user: {
                select: {
                  id: true,
                  name: true,
                  image: true,
                },
              },
            },
            orderBy: { createdAt: "desc" },
          },
          _count: {
            select: {
              supports: true,
              comments: true,
              tasks: true,
            },
          },
        },
      });

      if (!project) {
        throw new Error("Project not found");
      }

      // Increment view count
      try {
        await db.project.update({
          where: { id: project.id },
          data: { viewsCount: { increment: 1 } },
        });
      } catch (error) {
        console.warn("Could not increment view count:", error);
      }

      return project;
    }),

  // Get user's projects
  getMyProjects: protectedProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        cursor: z.string().nullish(),
        status: z.nativeEnum(ProjectStatus).optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      const { limit, cursor, status } = input;
      const userId = ctx.session.user.id;

      const where = {
        createdById: userId,
        ...(status && { status }),
      };

      const projects = await db.project.findMany({
        take: limit + 1,
        cursor: cursor ? { id: cursor } : undefined,
        where,
        orderBy: { updatedAt: "desc" },
        include: {
          _count: {
            select: {
              supports: true,
              comments: true,
              tasks: true,
            },
          },
        },
      });

      let nextCursor: typeof cursor | undefined = undefined;
      if (projects.length > limit) {
        const nextItem = projects.pop();
        nextCursor = nextItem?.id;
      }

      return {
        items: projects,
        nextCursor,
      };
    }),

  // Update project
  update: protectedProcedure
    .input(updateProjectSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Check if user owns the project
      const existingProject = await db.project.findFirst({
        where: { id, createdById: userId },
      });

      if (!existingProject) {
        throw new Error("Project not found or access denied");
      }

      const project = await db.project.update({
        where: { id },
        data,
        include: {
          createdBy: {
            select: {
              id: true,
              name: true,
              email: true,
              image: true,
            },
          },
        },
      });

      return project;
    }),

  // Delete project
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const { id } = input;
      const userId = ctx.session.user.id;

      // Check if user owns the project
      const existingProject = await db.project.findFirst({
        where: { id, createdById: userId },
      });

      if (!existingProject) {
        throw new Error("Project not found or access denied");
      }

      await db.project.delete({
        where: { id },
      });

      return { success: true };
    }),

  // Publish project (change status from DRAFT to PUBLISHED)
  publish: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const { id } = input;
      const userId = ctx.session.user.id;

      // Check if user owns the project
      const existingProject = await db.project.findFirst({
        where: { id, createdById: userId },
      });

      if (!existingProject) {
        throw new Error("Project not found or access denied");
      }

      if (existingProject.status !== ProjectStatus.DRAFT) {
        throw new Error("Only draft projects can be published");
      }

      const project = await db.project.update({
        where: { id },
        data: {
          status: ProjectStatus.PUBLISHED,
          publishedAt: new Date(),
        },
      });

      return project;
    }),

  // Get project statistics
  getStats: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      const stats = await db.project.findUnique({
        where: { id: input.id },
        select: {
          supportersCount: true,
          votesCount: true,
          currentFunding: true,
          fundingGoal: true,
          _count: {
            select: {
              supports: true,
              comments: true,
              tasks: true,
              resources: true,
              milestones: true,
            },
          },
        },
      });

      return stats;
    }),
});
