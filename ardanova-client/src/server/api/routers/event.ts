import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";

// Event type enum
const EventType = z.enum([
  "meetup",
  "workshop",
  "hackathon",
  "conference",
  "webinar",
  "ama",
]);

// Event format enum
const EventFormat = z.enum(["virtual", "in-person", "hybrid"]);

// Event creation input schema
const createEventSchema = z.object({
  title: z.string().min(1, "Event title is required"),
  description: z.string().min(20, "Description must be at least 20 characters"),
  type: EventType,
  format: EventFormat,
  date: z.string(),
  startTime: z.string(),
  endTime: z.string().optional(),
  timezone: z.string().default("UTC"),
  location: z.string().optional(),
  virtualLink: z.string().url().optional(),
  maxAttendees: z.number().positive().optional(),
  tags: z.string().optional(),
});

// Event update input schema
const updateEventSchema = z.object({
  id: z.string(),
  title: z.string().min(1).optional(),
  description: z.string().min(20).optional(),
  type: EventType.optional(),
  format: EventFormat.optional(),
  date: z.string().optional(),
  startTime: z.string().optional(),
  endTime: z.string().optional(),
  timezone: z.string().optional(),
  location: z.string().optional(),
  virtualLink: z.string().url().optional(),
  maxAttendees: z.number().positive().optional(),
  tags: z.string().optional(),
});

export const eventRouter = createTRPCRouter({
  // Create a new event
  create: protectedProcedure
    .input(createEventSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // TODO: Implement API call when backend endpoint is ready
      return {
        id: crypto.randomUUID(),
        slug: input.title.toLowerCase().replace(/\s+/g, "-"),
        ...input,
        organizerId: userId,
        attendeesCount: 0,
        isPublished: true,
        createdAt: new Date().toISOString(),
      };
    }),

  // Get all events with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        type: EventType.optional(),
        format: EventFormat.optional(),
        upcoming: z.boolean().optional(),
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

  // Get upcoming events
  getUpcoming: publicProcedure
    .input(z.object({ limit: z.number().min(1).max(20).default(10) }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      return [];
    }),

  // Get event by ID
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Event not found");
    }),

  // Get user's events (organized by user)
  getMyEvents: protectedProcedure.query(async ({ ctx }) => {
    // TODO: Implement API call when backend endpoint is ready
    return [];
  }),

  // Get user's registered events
  getRegisteredEvents: protectedProcedure.query(async ({ ctx }) => {
    // TODO: Implement API call when backend endpoint is ready
    return [];
  }),

  // Register for an event
  register: protectedProcedure
    .input(z.object({ eventId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true, eventId: input.eventId };
    }),

  // Unregister from an event
  unregister: protectedProcedure
    .input(z.object({ eventId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),

  // Update event
  update: protectedProcedure
    .input(updateEventSchema)
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      throw new Error("Not implemented");
    }),

  // Delete event
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      // TODO: Implement API call when backend endpoint is ready
      return { success: true };
    }),
});
