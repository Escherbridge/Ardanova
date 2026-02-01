"use client";

import { useCallback, useMemo, useRef, useState } from "react";
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
  const markAsReadMutation = api.chat.markAsRead.useMutation({
    onSuccess: () => {
      void utils.chat.getConversations.invalidate();
    },
  });
  // Ref to access mutation without it being a useCallback dependency
  const markAsReadRef = useRef(markAsReadMutation);
  markAsReadRef.current = markAsReadMutation;

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

  // Real-time: message read - only update conversation list (unread counts), not messages
  useEventSubscription<ChatMessageReadEvent>(
    "chat.message_read",
    useCallback((event) => {
      if (event.conversationId !== conversationId) return;
      void utils.chat.getConversations.invalidate();
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

  // Memoize messages to prevent new array reference on every render
  const messages = useMemo(
    () => messagesQuery.data?.pages.flatMap(p => p.items).reverse() ?? [],
    [messagesQuery.data]
  );

  // Dedupe markAsRead calls - track what we've already marked
  const lastMarkedKeyRef = useRef<string | null>(null);

  const handleMarkAsRead = useCallback(() => {
    const lastMsg = markAsReadRef.current;
    if (lastMarkedKeyRef.current === conversationId) return;
    if (lastMsg.isPending) return;
    lastMarkedKeyRef.current = conversationId;
    void lastMsg.mutateAsync({ conversationId });
  }, [conversationId]);

  // Reset the dedup key when a new message arrives
  const lastMessageId = messages[messages.length - 1]?.id;
  const prevLastMessageIdRef = useRef(lastMessageId);
  if (lastMessageId !== prevLastMessageIdRef.current) {
    prevLastMessageIdRef.current = lastMessageId;
    lastMarkedKeyRef.current = null;
  }

  return {
    conversation: conversationQuery.data,
    messages,
    isLoading: conversationQuery.isLoading || messagesQuery.isLoading,
    isFetchingNextPage: messagesQuery.isFetchingNextPage,
    hasNextPage: messagesQuery.hasNextPage,
    fetchNextPage: messagesQuery.fetchNextPage,
    typingUsers: Array.from(typingUsers.values()).map(v => v.name),
    sendMessage: (message: string, replyToId?: string) =>
      sendMessage.mutateAsync({ conversationId, content: message, replyToId }),
    markAsRead: handleMarkAsRead,
    sendTypingIndicator: (isTyping: boolean) =>
      sendTyping.mutateAsync({ conversationId, isTyping }),
    isSending: sendMessage.isPending,
  };
}
