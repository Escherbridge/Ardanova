import { z } from "zod";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";
import { apiClient } from "~/lib/api";
import { toPublicUser } from "./user";

// Create group conversation schema
const createGroupSchema = z.object({
  name: z.string().min(1, "Group name is required"),
  participantIds: z
    .array(z.string())
    .min(1, "At least one participant is required"),
  avatarUrl: z.string().url().optional(),
});

// Send message schema
const sendMessageSchema = z.object({
  conversationId: z.string(),
  content: z
    .string()
    .min(1, "Message content is required")
    .max(10000, "Message cannot exceed 10000 characters"),
  replyToId: z.string().optional(),
});

export const chatRouter = createTRPCRouter({
  // Search users for starting new conversations
  searchUsers: protectedProcedure
    .input(
      z.object({
        query: z.string().default(""),
        limit: z.number().min(1).max(50).default(20),
      }),
    )
    .query(async ({ input, ctx }) => {
      const userId = ctx.session.user.id;

      const response = await apiClient.users.search(
        input.query,
        1,
        input.limit,
      );

      if (response.error) {
        throw new Error(response.error);
      }

      // Same directory redaction as user.search; an exact-email match must not echo the address back.
      const users = (response.data?.items ?? []).map(toPublicUser);

      return users.map((u) => ({
        id: u.id,
        name: u.name ?? "Unnamed member",
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
      }),
    )
    .query(async ({ input }) => {
      const { limit, cursor } = input;

      const page = cursor ? parseInt(cursor, 10) : 1;

      const response = await apiClient.chat.getConversations({
        page,
        pageSize: limit,
      });

      if (response.error) {
        throw new Error(response.error);
      }

      const data = response.data;
      return {
        items: data?.items ?? [],
        nextCursor: (data as { hasMore?: boolean })?.hasMore
          ? String(page + 1)
          : undefined,
      };
    }),

  // Get a single conversation by ID
  getConversation: protectedProcedure
    .input(z.object({ conversationId: z.string() }))
    .query(async ({ input }) => {
      const response = await apiClient.chat.getConversation(
        input.conversationId,
      );

      if (response.error || !response.data) {
        throw new Error(response.error ?? "Conversation not found");
      }

      return response.data;
    }),

  // Get or create a direct conversation with another user
  getOrCreateDirect: protectedProcedure
    .input(z.object({ otherUserId: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.getOrCreateDirect({
        participantUserId: input.otherUserId,
      });

      if (response.error || !response.data) {
        throw new Error(
          response.error ?? "Failed to get or create conversation",
        );
      }

      return response.data;
    }),

  // Create a group conversation
  createGroup: protectedProcedure
    .input(createGroupSchema)
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.createGroup({
        name: input.name,
        avatarUrl: input.avatarUrl,
        memberUserIds: [...new Set(input.participantIds)],
      });

      if (response.error || !response.data) {
        throw new Error(
          response.error ?? "Failed to create group conversation",
        );
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
      }),
    )
    .query(async ({ input }) => {
      const { conversationId, limit, cursor } = input;

      const response = await apiClient.chat.getMessages(
        conversationId,
        limit,
        cursor,
      );

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
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.sendMessage({
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
        readUpTo: z.string().datetime({ offset: true }),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.markAsRead({
        conversationId: input.conversationId,
        readUpTo: input.readUpTo,
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
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.sendTypingIndicator({
        conversationId: input.conversationId,
        isTyping: input.isTyping,
      });

      if (response.error) {
        throw new Error(response.error ?? "Failed to send typing indicator");
      }

      return { success: true };
    }),

  updateMessage: protectedProcedure
    .input(
      z.object({
        messageId: z.string(),
        message: z.string().min(1).max(10000),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.updateMessage(input.messageId, {
        message: input.message,
      });
      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update message");
      }
      return response.data;
    }),

  deleteMessage: protectedProcedure
    .input(z.object({ messageId: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.deleteMessage(input.messageId);
      if (response.error) {
        throw new Error(response.error ?? "Failed to delete message");
      }
      return { success: true };
    }),

  updateGroup: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
        name: z.string().min(1).optional(),
        avatarUrl: z.string().url().optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const { conversationId, ...dto } = input;
      const response = await apiClient.chat.updateGroup(conversationId, dto);
      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to update group");
      }
      return response.data;
    }),

  addMember: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
        memberUserId: z.string(),
        role: z.enum(["OWNER", "ADMIN", "MEMBER"]).optional(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.addMember(input.conversationId, {
        userId: input.memberUserId,
        role: input.role,
      });
      if (response.error || !response.data) {
        throw new Error(response.error ?? "Failed to add member");
      }
      return response.data;
    }),

  removeMember: protectedProcedure
    .input(
      z.object({
        conversationId: z.string(),
        memberUserId: z.string(),
      }),
    )
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.removeMember(
        input.conversationId,
        input.memberUserId,
      );
      if (response.error) {
        throw new Error(response.error ?? "Failed to remove member");
      }
      return { success: true };
    }),

  leaveConversation: protectedProcedure
    .input(z.object({ conversationId: z.string() }))
    .mutation(async ({ input }) => {
      const response = await apiClient.chat.leaveConversation(
        input.conversationId,
      );
      if (response.error) {
        throw new Error(response.error ?? "Failed to leave conversation");
      }
      return response.data ?? { success: true };
    }),
});
