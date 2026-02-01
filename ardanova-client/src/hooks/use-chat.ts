"use client";

import { useCallback, useMemo } from "react";
import { api } from "~/trpc/react";
import { useEventSubscription } from "./use-event-subscription";
import type { ChatMessageSentEvent, ChatConversationCreatedEvent } from "~/lib/websocket/types";

export function useChat() {
  const utils = api.useUtils();

  // Fetch conversations with infinite scroll
  const conversationsQuery = api.chat.getConversations.useInfiniteQuery(
    { pageSize: 20 },
    {
      getNextPageParam: (lastPage) =>
        lastPage.hasMore ? (lastPage.items?.length ?? 0) / 20 + 1 : undefined,
    }
  );

  // Real-time: new message updates conversation list
  useEventSubscription<ChatMessageSentEvent>(
    "chat.message_sent",
    useCallback(() => {
      void utils.chat.getConversations.invalidate();
    }, [utils]),
    []
  );

  // Real-time: new conversation
  useEventSubscription<ChatConversationCreatedEvent>(
    "chat.conversation_created",
    useCallback(() => {
      void utils.chat.getConversations.invalidate();
    }, [utils]),
    []
  );

  // Create direct conversation mutation
  const createDirect = api.chat.getOrCreateDirect.useMutation({
    onSuccess: () => {
      void utils.chat.getConversations.invalidate();
    },
  });

  // Create group conversation mutation
  const createGroup = api.chat.createGroup.useMutation({
    onSuccess: () => {
      void utils.chat.getConversations.invalidate();
    },
  });

  const conversations = useMemo(
    () => conversationsQuery.data?.pages.flatMap(p => p.items) ?? [],
    [conversationsQuery.data]
  );

  return {
    conversations,
    isLoading: conversationsQuery.isLoading,
    isFetchingNextPage: conversationsQuery.isFetchingNextPage,
    hasNextPage: conversationsQuery.hasNextPage,
    fetchNextPage: conversationsQuery.fetchNextPage,
    refetch: conversationsQuery.refetch,
    createDirectConversation: createDirect.mutateAsync,
    createGroupConversation: createGroup.mutateAsync,
    isCreating: createDirect.isPending || createGroup.isPending,
  };
}
