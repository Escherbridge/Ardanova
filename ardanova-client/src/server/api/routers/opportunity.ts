import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";

// Opportunity type enum
const OpportunityType = z.enum([
  "Bounty",
  "Freelance",
  "Contract",
  "Part-time",
  "Full-time",
]);

// Experience level enum
const ExperienceLevel = z.enum(["entry", "intermediate", "senior", "expert"]);

// Compensation type enum
const CompensationType = z.enum(["fixed", "hourly", "negotiable"]);

// Opportunity creation input schema
const createOpportunitySchema = z.object({
  title: z.string().min(1, "Title is required"),
  description: z.string().min(20, "Description must be at least 20 characters"),
  type: OpportunityType,
  skills: z.array(z.string()).min(1, "At least one skill is required"),
  experienceLevel: ExperienceLevel.optional(),
  compensationType: CompensationType.optional(),
  compensationAmount: z.number().positive().optional(),
  location: z.string().optional(),
  deadline: z.string().optional(),
  projectId: z.string().optional(),
});

// Opportunity update input schema
const updateOpportunitySchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(20).optional(),
  type: OpportunityType.optional(),
  skills: z.array(z.string()).optional(),
  experienceLevel: ExperienceLevel.optional(),
  compensationType: CompensationType.optional(),
  compensationAmount: z.number().positive().optional(),
  location: z.string().optional(),
  deadline: z.string().optional(),
  isOpen: z.boolean().optional(),
});

export const opportunityRouter = createTRPCRouter({
  // Create a new opportunity
  create: protectedProcedure
    .input(createOpportunitySchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Implement API call when backend endpoint is ready
      return {
        id: crypto.randomUUID(),
        slug: input.title.toLowerCase().replace(/\s+/g, "-"),
        ...input,
        posterId: userId,
        applicantsCount: 0,
        isOpen: true,
        createdAt: new Date().toISOString(),
      };
    }),

  // Get all opportunities with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        type: OpportunityType.optional(),
        experienceLevel: ExperienceLevel.optional(),
        skill: z.string().optional(),
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

  // Get opportunity by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Opportunity not found");
    }),

  // Get user's posted opportunities
  getMyOpportunities: protectedProcedure.query(async ({ ctx }) => {
    // TODO: Implement API call when backend endpoint is ready
    return [];
  }),

  // Apply to an opportunity
  apply: protectedProcedure
    .input(
      z.object({
        opportunityId: z.string(),
        coverLetter: z.string().optional(),
        proposedRate: z.number().positive().optional(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true, applicationId: crypto.randomUUID() };
    }),

  // Get applications for an opportunity (only for poster)
  getApplications: protectedProcedure
    .input(z.object({ opportunityId: z.string() }))
    .query(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return [];
    }),

  // Update opportunity
  update: protectedProcedure
    .input(updateOpportunitySchema)
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Not implemented");
    }),

  // Close opportunity
  close: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),

  // Delete opportunity
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),
});
