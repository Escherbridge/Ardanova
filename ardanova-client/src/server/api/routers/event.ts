import { z } from "zod";
import { createTRPCRouter, protectedProcedure, publicProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

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
  projectId: z.string().optional(),
  guildId: z.string().optional(),
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

      // Combine date and time into ISO format
      const startDate = combineDateTime(input.date, input.startTime, input.timezone);
      const endDate = input.endTime
        ? combineDateTime(input.date, input.endTime, input.timezone)
        : new Date(new Date(startDate).getTime() + 2 * 60 * 60 * 1000).toISOString();

      const response = await apiClient.events.create({
        organizerId: userId,
        title: input.title,
        description: input.description,
        type: mapEventType(input.type),
        isOnline: input.format === "virtual" || input.format === "hybrid",
        location: input.location,
        meetingUrl: input.virtualLink,
        timezone: input.timezone,
        startDate,
        endDate,
        maxAttendees: input.maxAttendees,
        projectId: input.projectId,
        guildId: input.guildId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create event");
      }

      return response.data;
    }),

  // Get all events with pagination and filters
  getAll: publicProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(100).default(20),
        page: z.number().min(1).default(1),
        search: z.string().optional(),
        type: EventType.optional(),
        format: EventFormat.optional(),
        upcoming: z.boolean().optional(),
      })
    )
    .query(async ({ input }) => {
      const response = await apiClient.events.search({
        searchTerm: input.search,
        type: input.type ? mapEventType(input.type) : undefined,
        upcoming: input.upcoming,
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

  // Get upcoming events
  getUpcoming: publicProcedure
    .input(z.object({ limit: z.number().min(1).max(20).default(10) }))
    .query(async ({ input }) => {
      const response = await apiClient.events.getUpcoming(input.limit);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Get event by ID or slug
  getById: publicProcedure
    .input(z.object({ id: z.string() }))
    .query(async ({ input }) => {
      let response = await apiClient.events.getById(input.id);

      // Fallback to slug lookup
      if (response.status === 404 || !response.data) {
        response = await apiClient.events.getBySlug(input.id);
      }

      if (!response.data) {
        throw new Error("Event not found");
      }

      return response.data;
    }),

  // Get user's events (organized by user)
  getMyEvents: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.events.getByOrganizerId(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get user's registered events
  getRegisteredEvents: protectedProcedure.query(async ({ ctx }) => {
    const userId = ctx.session.user.id;
    const response = await apiClient.events.getRegisteredEvents(userId);

    if (response.error) {
      throw new Error(response.error);
    }

    return response.data ?? [];
  }),

  // Get event attendees
  getAttendees: publicProcedure
    .input(z.object({ eventId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.events.getAttendees(input.eventId);

      if (response.error) {
        throw new Error(response.error);
      }

      return response.data ?? [];
    }),

  // Register for an event
  register: protectedProcedure
    .input(z.object({ eventId: z.string(), notes: z.string().optional() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.events.register(input.eventId, {
        userId,
        notes: input.notes,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to register for event");
      }

      return { success: true, eventId: input.eventId };
    }),

  // Unregister from an event
  unregister: protectedProcedure
    .input(z.object({ eventId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.events.unregister(input.eventId, userId);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),

  // Update event
  update: protectedProcedure
    .input(updateEventSchema)
    .mutation(async ({ input, ctx }) => {
      const { id, ...data } = input;
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.events.getById(id);
      if (existing.error || !existing.data) {
        throw new Error("Event not found");
      }

      if (existing.data.organizerId !== userId) {
        throw new Error("Access denied: You do not own this event");
      }

      // Build update payload
      const updateData: Parameters<typeof apiClient.events.update>[1] = {
        title: data.title,
        description: data.description,
        type: data.type ? mapEventType(data.type) : undefined,
        isOnline: data.format ? (data.format === "virtual" || data.format === "hybrid") : undefined,
        location: data.location,
        meetingUrl: data.virtualLink,
        timezone: data.timezone,
        maxAttendees: data.maxAttendees,
      };

      // Handle date/time updates
      if (data.date && data.startTime) {
        updateData.startDate = combineDateTime(data.date, data.startTime, data.timezone ?? existing.data.timezone);
      }
      if (data.date && data.endTime) {
        updateData.endDate = combineDateTime(data.date, data.endTime, data.timezone ?? existing.data.timezone);
      }

      const response = await apiClient.events.update(id, updateData);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update event");
      }

      return response.data;
    }),

  // Delete event
  delete: protectedProcedure
    .input(z.object({ id: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Verify ownership
      const existing = await apiClient.events.getById(input.id);
      if (existing.error || !existing.data) {
        throw new Error("Event not found");
      }

      if (existing.data.organizerId !== userId) {
        throw new Error("Access denied: You do not own this event");
      }

      const response = await apiClient.events.delete(input.id);

      if (response.error) {
        throw new Error(response.error);
      }

      return { success: true };
    }),
});

// Helper function to combine date and time into ISO string
function combineDateTime(date: string, time: string, timezone: string): string {
  // Simple implementation - in production, use a proper date library like date-fns or luxon
  const dateTime = new Date(`${date}T${time}`);
  return dateTime.toISOString();
}

// Helper function to map frontend event type to backend format
function mapEventType(type: string): string {
  const mapping: Record<string, string> = {
    meetup: "Meeting",
    workshop: "Workshop",
    hackathon: "Hackathon",
    conference: "Conference",
    webinar: "Webinar",
    ama: "Ama",
  };
  return mapping[type] ?? type;
}
