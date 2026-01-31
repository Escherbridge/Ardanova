"use client";

import { useCallback, useState } from "react";
import { api } from "~/trpc/react";
import { useEventSubscription } from "./use-event-subscription";
import type {
  ChatMessageSentEvent,
  ChatMessageReadEvent,
  ChatTypingEvent
} from "~/lib/websocket/types";

interface UseConversationOptions {
  conversationId: string;
  enabled?: boolean;
}

export function useConversation({ conversationId, enabled = true }: UseConversationOptions) {
  const utils = api.useUtils();
  const [typingUsers, setTypingUsers] = useState<Map<string, { name: string; timeout: NodeJS.Timeout }>>(new Map());

  // Fetch conversation details
  const conversationQuery = api.chat.getConversation.useQuery(
    { conversationId },
    { enabled: enabled && !!conversationId }
  );

  // Fetch messages with infinite scroll (older messages)
  const messagesQuery = api.chat.getMessages.useInfiniteQuery(
    { conversationId, limit: 50 },
    {
      enabled: enabled && !!conversationId,
      getNextPageParam: (lastPage) => lastPage.nextCursor,
    }
  );

  // Send message mutation
  const sendMessage = api.chat.sendMessage.useMutation({
    onSuccess: () => {
      void utils.chat.getMessages.invalidate({ conversationId });
      void utils.chat.getConversations.invalidate();
    },
  });

  // Mark as read mutation
  const markAsRead = api.chat.markAsRead.useMutation();

  // Typing indicator mutation
  const sendTyping = api.chat.sendTypingIndicator.useMutation();

  // Real-time: new message in this conversation
  useEventSubscription<ChatMessageSentEvent>(
    "chat.message_sent",
    useCallback((event) => {
      if (event.conversationId !== conversationId) return;
      void utils.chat.getMessages.invalidate({ conversationId });
    }, [conversationId, utils]),
    [conversationId]
  );

  // Real-time: message read
  useEventSubscription<ChatMessageReadEvent>(
    "chat.message_read",
    useCallback((event) => {
      if (event.conversationId !== conversationId) return;
      void utils.chat.getMessages.invalidate({ conversationId });
    }, [conversationId, utils]),
    [conversationId]
  );

  // Real-time: typing indicator
  useEventSubscription<ChatTypingEvent>(
    "chat.typing",
    useCallback((event) => {
      if (event.conversationId !== conversationId) return;

      setTypingUsers(prev => {
        const updated = new Map(prev);

        // Clear existing timeout
        const existing = updated.get(event.userId);
        if (existing) {
          clearTimeout(existing.timeout);
        }

        if (event.isTyping) {
          // Add/update with new timeout (3 seconds)
          const timeout = setTimeout(() => {
            setTypingUsers(p => {
              const u = new Map(p);
              u.delete(event.userId);
              return u;
            });
          }, 3000);

          updated.set(event.userId, { name: event.userName, timeout });
        } else {
          updated.delete(event.userId);
        }

        return updated;
      });
    }, [conversationId]),
    [conversationId]
  );

  return {
    conversation: conversationQuery.data,
    messages: messagesQuery.data?.pages.flatMap(p => p.items).reverse() ?? [],
    isLoading: conversationQuery.isLoading || messagesQuery.isLoading,
    isFetchingNextPage: messagesQuery.isFetchingNextPage,
    hasNextPage: messagesQuery.hasNextPage,
    fetchNextPage: messagesQuery.fetchNextPage,
    typingUsers: Array.from(typingUsers.values()).map(v => v.name),
    sendMessage: (message: string, replyToId?: string) =>
      sendMessage.mutateAsync({ conversationId, message, replyToId }),
    markAsRead: (readUpTo: string) =>
      markAsRead.mutateAsync({ conversationId, readUpTo }),
    sendTypingIndicator: (isTyping: boolean) =>
      sendTyping.mutateAsync({ conversationId, isTyping }),
    isSending: sendMessage.isPending,
  };
}
