import { z } from "zod";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";

// Create group conversation schema
const createGroupSchema = z.object({
  name: z.string().min(1, "Group name is required"),
  participantIds: z.array(z.string()).min(1, "At least one participant is required"),
  avatarUrl: z.string().url().optional(),
});

// Send message schema
const sendMessageSchema = z.object({
  conversationId: z.string(),
  content: z.string().min(1, "Message content is required").max(10000, "Message cannot exceed 10000 characters"),
  replyToId: z.string().optional(),
});

export const chatRouter = createTRPCRouter({
  // Search users for starting new conversations
  searchUsers: protectedProcedure
    .input(
      z.object({
        query: z.string().default(""),
        limit: z.number().min(1).max(50).default(20),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.users.getAll(1, input.limit);

      if (response.error) {
        throw new Error(response.error);
      }

      const users = response.data?.items ?? [];

      // Filter by search query if provided
      const query = input.query.toLowerCase();
      const filtered = query
        ? users.filter(
            (u) =>
              u.name?.toLowerCase().includes(query) ||
              u.email.toLowerCase().includes(query)
          )
        : users;

      return filtered.map((u) => ({
        id: u.id,
        name: u.name ?? u.email,
        email: u.email,
        image: u.image,
        isSelf: u.id === userId,
      }));
    }),

  // Get conversations list with cursor-based pagination
  getConversations: protectedProcedure
    .input(
      z.object({
        limit: z.number().min(1).max(50).default(20),
        cursor: z.string().optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { limit, cursor } = input;

      const page = cursor ? parseInt(cursor, 10) : 1;

      const response = await apiClient.chat.getConversations(userId, {
        page,
        pageSize: limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      const data = response.data;
      return {
        items: data?.items ?? [],
        nextCursor: (data as { hasMore?: boolean })?.hasMore ? String(page + 1) : undefined,
      };
    }),

  // Get a single conversation by ID
  getConversation: protectedProcedure
    .input(z.object({ conversationId: z.string() }))
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.chat.getConversation(input.conversationId, userId);

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Conversation not found");
      }

      return response.data;
    }),

  // Get or create a direct conversation with another user
  getOrCreateDirect: protectedProcedure
    .input(z.object({ otherUserId: z.string() }))
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.chat.getOrCreateDirect(userId, {
        participantUserId: input.otherUserId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to get or create conversation");
      }

      return response.data;
    }),

  // Create a group conversation
  createGroup: protectedProcedure
    .input(createGroupSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      // Ensure creator is included in participants
      const allParticipantIds = [...new Set([userId, ...input.participantIds])];

      const response = await apiClient.chat.createGroup(userId, {
        name: input.name,
        avatarUrl: input.avatarUrl,
        memberUserIds: allParticipantIds,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to create group conversation");
      }

      return response.data;
    }),

  // Get messages for a conversation with cursor-based pagination
  getMessages: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
        limit: z.number().min(1).max(100).default(50),
        cursor: z.string().optional(),
      })
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;
      const { conversationId, limit, cursor } = input;

      const response = await apiClient.chat.getMessages(conversationId, userId, limit, cursor);

      if (response.error) {
        throw new Error(response.error);
      }

      const data = response.data;
      return {
        items: data?.items ?? [],
        nextCursor: data?.hasMore ? data.nextCursor : undefined,
      };
    }),

  // Send a message to a conversation
  sendMessage: protectedProcedure
    .input(sendMessageSchema)
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.chat.sendMessage(userId, {
        conversationId: input.conversationId,
        message: input.content,
        replyToId: input.replyToId,
      });

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to send message");
      }

      return response.data;
    }),

  // Mark messages as read in a conversation
  markAsRead: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.chat.markAsRead(userId, {
        conversationId: input.conversationId,
        readUpTo: new Date().toISOString(),
      });

      if (response.error) {
        throw new Error(response.error ?? "Failed to mark messages as read");
      }

      return { success: true };
    }),

  // Send typing indicator
  sendTypingIndicator: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
        isTyping: z.boolean().default(true),
      })
    )
    .mutation(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.chat.sendTypingIndicator(userId, {
        conversationId: input.conversationId,
        isTyping: input.isTyping,
      });

      if (response.error) {
        throw new Error(response.error ?? "Failed to send typing indicator");
      }

      return { success: true };
    }),
});
