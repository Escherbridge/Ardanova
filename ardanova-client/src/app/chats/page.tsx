"use client";

import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Loader2,
  MessageCircle,
  Plus,
  Search,
  Send,
  UsersRound,
} from "lucide-react";

import { NewConversationModal } from "~/components/chats/new-conversation-modal";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button";
import { useChat } from "~/hooks/use-chat";
import { useConversation } from "~/hooks/use-conversation";
import type {
  ChatConversation,
  ChatParticipant,
} from "~/lib/api/ardanova/endpoints/chat";
import {
  intersectsVertically,
  isChatViewportNearBottom,
} from "~/lib/chat/read-receipt";
import { cn } from "~/lib/utils";

const conversationFilters = [
  { id: "all", label: "All" },
  { id: "unread", label: "Unread" },
  { id: "groups", label: "Groups" },
] as const;

type ConversationFilter = (typeof conversationFilters)[number]["id"];

function formatRelativeTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  const elapsedMinutes = Math.max(
    0,
    Math.floor((Date.now() - date.getTime()) / 60_000),
  );
  if (elapsedMinutes < 1) return "now";
  if (elapsedMinutes < 60) return `${elapsedMinutes}m`;

  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) return `${elapsedHours}h`;

  const elapsedDays = Math.floor(elapsedHours / 24);
  if (elapsedDays < 7) return `${elapsedDays}d`;

  return date.toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
  });
}

function formatMessageTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  return date.toLocaleTimeString([], {
    hour: "numeric",
    minute: "2-digit",
  });
}

function getOtherMember(
  conversation: ChatConversation,
  currentUserId?: string,
): ChatParticipant | undefined {
  return (
    conversation.members.find((member) => member.userId !== currentUserId) ??
    conversation.members.find((member) => member.userId === currentUserId)
  );
}

function getConversationName(
  conversation: ChatConversation,
  currentUserId?: string,
): string {
  if (conversation.type === "GROUP") {
    return conversation.name?.trim() || "Group conversation";
  }

  const member = getOtherMember(conversation, currentUserId);
  if (!member) return conversation.name?.trim() || "Direct conversation";

  const isConversationWithSelf = conversation.members.every(
    (participant) => participant.userId === currentUserId,
  );
  return isConversationWithSelf ? `${member.userName} (you)` : member.userName;
}

function ConversationAvatar({
  conversation,
  currentUserId,
  size = "default",
}: {
  conversation: ChatConversation;
  currentUserId?: string;
  size?: "default" | "small";
}) {
  const member = getOtherMember(conversation, currentUserId);
  const name = getConversationName(conversation, currentUserId);
  const image =
    conversation.type === "GROUP" ? conversation.avatarUrl : member?.userImage;

  return (
    <Avatar className={size === "small" ? "size-9" : "size-11"}>
      <AvatarImage src={image ?? undefined} alt="" />
      <AvatarFallback className="border-border bg-secondary text-foreground border">
        {conversation.type === "GROUP" ? (
          <UsersRound className="size-4" aria-hidden="true" />
        ) : (
          name.charAt(0).toUpperCase()
        )}
      </AvatarFallback>
    </Avatar>
  );
}

export default function ChatsPage() {
  const { data: session } = useSession();
  const currentUserId = session?.user?.id;
  const [selectedChatId, setSelectedChatId] = useState<string | null>(null);
  const [messageInput, setMessageInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [activeFilter, setActiveFilter] = useState<ConversationFilter>("all");
  const [isNewConversationOpen, setIsNewConversationOpen] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [conversationPageError, setConversationPageError] = useState<
    string | null
  >(null);
  const [messagePageError, setMessagePageError] = useState<string | null>(null);
  const messagesViewportRef = useRef<HTMLDivElement>(null);
  const pendingScrollRestoreRef = useRef<{
    scrollHeight: number;
    scrollTop: number;
  } | null>(null);
  const shouldStickToBottomRef = useRef(true);
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const {
    conversations,
    isLoading: isLoadingConversations,
    error: conversationsError,
    refetch: refetchConversations,
    isFetchingNextPage: isFetchingMoreConversations,
    hasNextPage: hasMoreConversations,
    fetchNextPage: fetchMoreConversations,
  } = useChat();

  const {
    conversation: selectedConversation,
    conversationError,
    refetchConversation,
    messages,
    messagesError,
    refetchMessages,
    isLoading: isLoadingMessages,
    isFetchingNextPage: isFetchingEarlierMessages,
    hasNextPage: hasEarlierMessages,
    fetchNextPage: fetchEarlierMessages,
    typingUsers,
    sendMessage,
    isSending,
    sendTypingIndicator,
    markAsRead,
  } = useConversation({
    conversationId: selectedChatId ?? "",
    enabled: selectedChatId !== null,
  });

  const latestMessage = messages.at(-1);

  const acknowledgeVisibleIncomingMessage = useCallback(() => {
    const viewport = messagesViewportRef.current;
    if (
      !selectedChatId ||
      !currentUserId ||
      !viewport ||
      document.visibilityState !== "visible" ||
      !document.hasFocus() ||
      !isChatViewportNearBottom(viewport)
    ) {
      return;
    }

    const viewportBounds = viewport.getBoundingClientRect();
    const visibleIncomingMessage = [
      ...viewport.querySelectorAll<HTMLElement>("[data-chat-message-id]"),
    ]
      .reverse()
      .find(
        (element) =>
          element.dataset.chatSenderId !== currentUserId &&
          intersectsVertically(element.getBoundingClientRect(), viewportBounds),
      );
    const messageId = visibleIncomingMessage?.dataset.chatMessageId;
    const sentAt = visibleIncomingMessage?.dataset.chatSentAt;
    if (messageId && sentAt) markAsRead(messageId, sentAt);
  }, [currentUserId, markAsRead, selectedChatId]);

  useLayoutEffect(() => {
    const viewport = messagesViewportRef.current;
    if (!viewport) return;

    const pendingRestore = pendingScrollRestoreRef.current;
    if (pendingRestore) {
      viewport.scrollTop =
        pendingRestore.scrollTop +
        (viewport.scrollHeight - pendingRestore.scrollHeight);
      pendingScrollRestoreRef.current = null;
      return;
    }

    if (
      shouldStickToBottomRef.current ||
      latestMessage?.userFromId === currentUserId
    ) {
      viewport.scrollTop = viewport.scrollHeight;
      shouldStickToBottomRef.current = true;
    }
  }, [currentUserId, latestMessage?.userFromId, messages, selectedChatId]);

  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

  useEffect(() => {
    acknowledgeVisibleIncomingMessage();
    const acknowledgeOnFocus = () => acknowledgeVisibleIncomingMessage();
    document.addEventListener("visibilitychange", acknowledgeOnFocus);
    window.addEventListener("focus", acknowledgeOnFocus);
    return () => {
      document.removeEventListener("visibilitychange", acknowledgeOnFocus);
      window.removeEventListener("focus", acknowledgeOnFocus);
    };
  }, [acknowledgeVisibleIncomingMessage, latestMessage?.id]);

  const normalizedSearch = searchQuery.trim().toLocaleLowerCase();
  const filteredConversations = conversations.filter((conversation) => {
    if (activeFilter === "unread" && conversation.unreadCount === 0) {
      return false;
    }
    if (activeFilter === "groups" && conversation.type !== "GROUP") {
      return false;
    }
    if (!normalizedSearch) return true;

    return getConversationName(conversation, currentUserId)
      .toLocaleLowerCase()
      .includes(normalizedSearch);
  });

  const listedConversation =
    conversations.find((conversation) => conversation.id === selectedChatId) ??
    null;
  const activeConversation = selectedConversation ?? listedConversation;
  const activeMember = activeConversation
    ? getOtherMember(activeConversation, currentUserId)
    : undefined;
  const activeMemberIsOnline =
    activeConversation?.type === "DIRECT" && Boolean(activeMember?.isOnline);

  async function handleSendMessage() {
    const content = messageInput.trim();
    if (!content || isSending) return;

    setMessageInput("");
    setSendError(null);
    shouldStickToBottomRef.current = true;

    try {
      await sendMessage(content);
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
        typingTimeoutRef.current = null;
      }
      await sendTypingIndicator(false).catch(() => undefined);
    } catch {
      setMessageInput((currentDraft) =>
        !currentDraft || currentDraft === content
          ? content
          : `${content}\n${currentDraft}`,
      );
      setSendError(
        "Your message was not sent. Check your connection and try again.",
      );
    }
  }

  function handleInputChange(value: string) {
    setMessageInput(value);
    setSendError(null);

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
      typingTimeoutRef.current = null;
    }

    const isTyping = value.trim().length > 0;
    void sendTypingIndicator(isTyping).catch(() => undefined);
    if (!isTyping) return;

    typingTimeoutRef.current = setTimeout(() => {
      void sendTypingIndicator(false).catch(() => undefined);
    }, 2_000);
  }

  async function handleLoadMoreConversations() {
    setConversationPageError(null);
    try {
      const result = await fetchMoreConversations();
      if (result.isError) {
        setConversationPageError("More conversations could not be loaded.");
      }
    } catch {
      setConversationPageError("More conversations could not be loaded.");
    }
  }

  async function handleLoadEarlierMessages() {
    setMessagePageError(null);
    const viewport = messagesViewportRef.current;
    const scrollSnapshot = viewport
      ? {
          scrollHeight: viewport.scrollHeight,
          scrollTop: viewport.scrollTop,
        }
      : null;
    pendingScrollRestoreRef.current = scrollSnapshot;
    try {
      const result = await fetchEarlierMessages();
      if (result.isError) {
        pendingScrollRestoreRef.current = null;
        setMessagePageError("Earlier messages could not be loaded.");
      }
    } catch {
      pendingScrollRestoreRef.current = null;
      setMessagePageError("Earlier messages could not be loaded.");
    } finally {
      if (scrollSnapshot) {
        window.requestAnimationFrame(() => {
          if (pendingScrollRestoreRef.current !== scrollSnapshot) return;
          const currentViewport = messagesViewportRef.current;
          if (currentViewport) {
            currentViewport.scrollTop =
              scrollSnapshot.scrollTop +
              (currentViewport.scrollHeight - scrollSnapshot.scrollHeight);
          }
          pendingScrollRestoreRef.current = null;
        });
      }
    }
  }

  function selectConversation(conversationId: string) {
    shouldStickToBottomRef.current = true;
    pendingScrollRestoreRef.current = null;
    setSelectedChatId(conversationId);
    setMessageInput("");
    setSendError(null);
    setMessagePageError(null);
  }

  return (
    <div className="bg-background h-[calc(100dvh-4rem)] min-h-0 overflow-hidden">
      <div className="flex h-full min-h-0">
        <aside
          aria-label="Conversations"
          className={cn(
            "border-border bg-card min-h-0 flex-col border-r-2 md:flex md:w-[22rem] md:shrink-0",
            selectedChatId ? "hidden" : "flex w-full",
          )}
        >
          <header className="border-border shrink-0 border-b-2 p-4">
            <div className="mb-4 flex items-start justify-between gap-4">
              <div>
                <p className="text-primary text-[0.68rem] font-bold tracking-[0.18em] uppercase">
                  Coordination, not distraction
                </p>
                <h1 className="text-foreground mt-1 flex items-center gap-2 text-2xl font-black tracking-tight">
                  <MessageCircle className="size-5" aria-hidden="true" />
                  Messages
                </h1>
              </div>
              <Button
                type="button"
                size="sm"
                onClick={() => setIsNewConversationOpen(true)}
              >
                <Plus className="size-4" aria-hidden="true" />
                New
              </Button>
            </div>

            <label className="relative block">
              <span className="sr-only">Search conversations</span>
              <Search
                className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2"
                aria-hidden="true"
              />
              <input
                type="search"
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder="Search people or groups"
                className="border-border bg-background text-foreground placeholder:text-muted-foreground focus:border-primary h-11 w-full border-2 pr-3 pl-10 text-sm outline-none"
              />
            </label>

            <div
              className="mt-3 flex gap-2"
              role="group"
              aria-label="Conversation filters"
            >
              {conversationFilters.map((filter) => (
                <button
                  key={filter.id}
                  type="button"
                  aria-pressed={activeFilter === filter.id}
                  onClick={() => setActiveFilter(filter.id)}
                  className={cn(
                    "min-h-11 border-2 px-3 text-xs font-bold tracking-wide uppercase transition-colors",
                    activeFilter === filter.id
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-background text-muted-foreground hover:border-foreground hover:text-foreground",
                  )}
                >
                  {filter.label}
                </button>
              ))}
            </div>
          </header>

          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain">
            {isLoadingConversations ? (
              <div
                className="flex min-h-48 flex-col items-center justify-center gap-3 px-5 text-center"
                role="status"
              >
                <Loader2
                  className="text-primary size-6 animate-spin"
                  aria-hidden="true"
                />
                <p className="text-muted-foreground text-sm">
                  Loading conversations…
                </p>
              </div>
            ) : conversationsError ? (
              <div
                className="border-destructive bg-destructive/10 m-4 space-y-3 border-2 p-4"
                role="alert"
              >
                <p className="text-foreground font-bold">
                  Conversations could not be loaded
                </p>
                <p className="text-muted-foreground text-sm leading-6">
                  This is a connection or service error, not an empty inbox.
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => void refetchConversations()}
                >
                  Retry conversations
                </Button>
              </div>
            ) : filteredConversations.length === 0 ? (
              <div className="border-border flex min-h-56 flex-col items-start justify-center gap-3 border-b p-6">
                <MessageCircle
                  className="text-muted-foreground size-8"
                  aria-hidden="true"
                />
                <div>
                  <p className="text-foreground font-bold">
                    {normalizedSearch || activeFilter !== "all"
                      ? "No matching conversations"
                      : "No conversations available"}
                  </p>
                  <p className="text-muted-foreground mt-1 text-sm leading-6">
                    {normalizedSearch || activeFilter !== "all"
                      ? "Change the search or filter to widen the list."
                      : "Start a direct conversation when you have something concrete to move forward."}
                  </p>
                </div>
                {!normalizedSearch && activeFilter === "all" && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setIsNewConversationOpen(true)}
                  >
                    <Plus className="size-4" aria-hidden="true" />
                    Start a conversation
                  </Button>
                )}
              </div>
            ) : (
              <div>
                {filteredConversations.map((conversation) => {
                  const otherMember = getOtherMember(
                    conversation,
                    currentUserId,
                  );
                  const isOnline =
                    conversation.type === "DIRECT" &&
                    Boolean(otherMember?.isOnline);
                  const lastMessageText = conversation.lastMessage?.isDeleted
                    ? "Message removed"
                    : conversation.lastMessage?.message?.trim() ||
                      "No messages yet";

                  return (
                    <button
                      key={conversation.id}
                      type="button"
                      aria-current={
                        selectedChatId === conversation.id ? "true" : undefined
                      }
                      onClick={() => selectConversation(conversation.id)}
                      className={cn(
                        "border-border hover:bg-secondary focus-visible:ring-primary flex w-full gap-3 border-b p-4 text-left transition-colors focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset",
                        selectedChatId === conversation.id &&
                          "border-l-primary bg-secondary border-l-4 pl-3",
                      )}
                    >
                      <div className="relative shrink-0">
                        <ConversationAvatar
                          conversation={conversation}
                          currentUserId={currentUserId}
                        />
                        {isOnline && (
                          <span
                            className="border-card bg-neon-green absolute -right-0.5 -bottom-0.5 size-3 border-2"
                            aria-label="Online"
                          />
                        )}
                      </div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-start justify-between gap-2">
                          <p className="text-foreground truncate text-sm font-bold">
                            {getConversationName(conversation, currentUserId)}
                          </p>
                          <span className="text-muted-foreground shrink-0 text-xs">
                            {conversation.lastMessage?.sentAt
                              ? formatRelativeTime(
                                  conversation.lastMessage.sentAt,
                                )
                              : ""}
                          </span>
                        </div>
                        <div className="mt-1 flex items-center justify-between gap-2">
                          <p
                            className={cn(
                              "truncate text-xs",
                              conversation.unreadCount > 0
                                ? "text-foreground font-semibold"
                                : "text-muted-foreground",
                            )}
                          >
                            {lastMessageText}
                          </p>
                          {conversation.unreadCount > 0 && (
                            <span className="border-primary bg-primary text-primary-foreground min-w-6 shrink-0 border px-1.5 py-0.5 text-center text-[0.68rem] font-black">
                              {conversation.unreadCount > 99
                                ? "99+"
                                : conversation.unreadCount}
                              <span className="sr-only"> unread messages</span>
                            </span>
                          )}
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            )}

            {conversationPageError && (
              <p
                className="border-destructive text-destructive border-b p-3 text-sm"
                role="alert"
              >
                {conversationPageError}
              </p>
            )}

            {hasMoreConversations && (
              <div className="p-3">
                <Button
                  type="button"
                  variant="outline"
                  className="w-full"
                  disabled={isFetchingMoreConversations}
                  onClick={() => void handleLoadMoreConversations()}
                >
                  {isFetchingMoreConversations && (
                    <Loader2
                      className="size-4 animate-spin"
                      aria-hidden="true"
                    />
                  )}
                  {isFetchingMoreConversations
                    ? "Loading…"
                    : "Load more conversations"}
                </Button>
              </div>
            )}
          </div>
        </aside>

        <section
          aria-label="Conversation"
          className={cn(
            "bg-background min-h-0 min-w-0 flex-1 flex-col md:flex",
            selectedChatId ? "flex" : "hidden",
          )}
        >
          {!selectedChatId ? (
            <div className="flex h-full items-center justify-center p-8">
              <div className="border-border bg-card max-w-md border-2 p-8">
                <p className="text-primary text-xs font-bold tracking-[0.18em] uppercase">
                  A useful conversation starts with intent
                </p>
                <h2 className="text-foreground mt-3 text-2xl font-black">
                  Choose a conversation
                </h2>
                <p className="text-muted-foreground mt-3 text-sm leading-6">
                  Coordinate the next decision, share context, or invite someone
                  into the work.
                </p>
                <Button
                  type="button"
                  className="mt-6"
                  onClick={() => setIsNewConversationOpen(true)}
                >
                  <Plus className="size-4" aria-hidden="true" />
                  New conversation
                </Button>
              </div>
            </div>
          ) : isLoadingMessages && !activeConversation ? (
            <div
              className="flex h-full items-center justify-center"
              role="status"
            >
              <Loader2
                className="text-primary size-7 animate-spin"
                aria-hidden="true"
              />
              <span className="sr-only">Loading conversation</span>
            </div>
          ) : conversationError && !activeConversation ? (
            <div className="flex h-full items-center justify-center p-6">
              <div
                className="border-destructive bg-card max-w-sm border-2 p-6"
                role="alert"
              >
                <h2 className="text-foreground font-black">
                  Conversation could not be loaded
                </h2>
                <p className="text-muted-foreground mt-2 text-sm leading-6">
                  The conversation service returned an error. Access has not
                  been inferred or changed.
                </p>
                <div className="mt-4 flex flex-wrap gap-2">
                  <Button
                    type="button"
                    onClick={() => void refetchConversation()}
                  >
                    Retry conversation
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setSelectedChatId(null)}
                  >
                    <ArrowLeft className="size-4" aria-hidden="true" />
                    Back
                  </Button>
                </div>
              </div>
            </div>
          ) : !activeConversation ? (
            <div className="flex h-full items-center justify-center p-6">
              <div
                className="border-destructive bg-card max-w-sm border-2 p-6"
                role="alert"
              >
                <h2 className="text-foreground font-black">
                  Conversation unavailable
                </h2>
                <p className="text-muted-foreground mt-2 text-sm leading-6">
                  It may have been removed, or you may no longer have access.
                </p>
                <Button
                  type="button"
                  variant="outline"
                  className="mt-4"
                  onClick={() => setSelectedChatId(null)}
                >
                  <ArrowLeft className="size-4" aria-hidden="true" />
                  Back to conversations
                </Button>
              </div>
            </div>
          ) : (
            <>
              <header className="border-border bg-card flex shrink-0 items-center gap-3 border-b-2 p-3 sm:p-4">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="md:hidden"
                  aria-label="Back to conversations"
                  onClick={() => setSelectedChatId(null)}
                >
                  <ArrowLeft className="size-5" aria-hidden="true" />
                </Button>

                {activeConversation.type === "DIRECT" &&
                activeMember?.userId ? (
                  <Link
                    href={`/dashboard/profile/${activeMember.userId}`}
                    className="focus-visible:ring-primary flex min-w-0 items-center gap-3 outline-none focus-visible:ring-2"
                  >
                    <ConversationAvatar
                      conversation={activeConversation}
                      currentUserId={currentUserId}
                      size="small"
                    />
                    <div className="min-w-0">
                      <h2 className="text-foreground hover:text-primary truncate font-black">
                        {getConversationName(activeConversation, currentUserId)}
                      </h2>
                      <p className="text-muted-foreground text-xs">
                        {activeMemberIsOnline
                          ? "Online now"
                          : "Direct conversation"}
                      </p>
                    </div>
                  </Link>
                ) : (
                  <div className="flex min-w-0 items-center gap-3">
                    <ConversationAvatar
                      conversation={activeConversation}
                      currentUserId={currentUserId}
                      size="small"
                    />
                    <div className="min-w-0">
                      <h2 className="text-foreground truncate font-black">
                        {getConversationName(activeConversation, currentUserId)}
                      </h2>
                      <p className="text-muted-foreground text-xs">
                        {activeConversation.members.length} members
                      </p>
                    </div>
                  </div>
                )}
              </header>

              {conversationError && (
                <div
                  className="border-destructive bg-destructive/10 flex shrink-0 flex-wrap items-center justify-between gap-3 border-b-2 p-3"
                  role="alert"
                >
                  <p className="text-destructive text-sm">
                    Current conversation details could not be refreshed. The
                    list snapshot may be stale.
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => void refetchConversation()}
                  >
                    Retry details
                  </Button>
                </div>
              )}

              <div
                ref={messagesViewportRef}
                role="log"
                aria-label="Conversation messages"
                aria-live="polite"
                aria-relevant="additions text"
                className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-3 sm:p-5"
                onScroll={(event) => {
                  const viewport = event.currentTarget;
                  shouldStickToBottomRef.current =
                    isChatViewportNearBottom(viewport);
                  if (shouldStickToBottomRef.current) {
                    acknowledgeVisibleIncomingMessage();
                  }
                }}
              >
                {hasEarlierMessages && (
                  <div className="mb-5 flex justify-center">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={isFetchingEarlierMessages}
                      onClick={() => void handleLoadEarlierMessages()}
                    >
                      {isFetchingEarlierMessages && (
                        <Loader2
                          className="size-4 animate-spin"
                          aria-hidden="true"
                        />
                      )}
                      {isFetchingEarlierMessages
                        ? "Loading…"
                        : "Load earlier messages"}
                    </Button>
                  </div>
                )}

                {messagePageError && (
                  <p
                    className="border-destructive bg-card text-destructive mx-auto mb-5 max-w-lg border-2 p-3 text-sm"
                    role="alert"
                  >
                    {messagePageError}
                  </p>
                )}

                {isLoadingMessages ? (
                  <div
                    className="flex min-h-48 items-center justify-center"
                    role="status"
                  >
                    <Loader2
                      className="text-primary size-6 animate-spin"
                      aria-hidden="true"
                    />
                    <span className="sr-only">Loading messages</span>
                  </div>
                ) : messagesError ? (
                  <div
                    className="border-destructive bg-card mx-auto mt-8 max-w-lg space-y-3 border-2 p-5"
                    role="alert"
                  >
                    <h3 className="text-foreground font-black">
                      Messages could not be loaded
                    </h3>
                    <p className="text-muted-foreground text-sm leading-6">
                      This is a service error, not an empty conversation. No
                      message history is shown until the request succeeds.
                    </p>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => void refetchMessages()}
                    >
                      Retry messages
                    </Button>
                  </div>
                ) : messages.length === 0 ? (
                  <div className="border-border bg-card mx-auto mt-12 max-w-md border-2 p-6 text-left">
                    <MessageCircle
                      className="text-primary size-7"
                      aria-hidden="true"
                    />
                    <h3 className="text-foreground mt-3 text-lg font-black">
                      Start with the next useful thing
                    </h3>
                    <p className="text-muted-foreground mt-2 text-sm leading-6">
                      Share the problem, ask a precise question, or name the
                      decision this conversation can move forward.
                    </p>
                  </div>
                ) : (
                  <div className="mx-auto flex w-full max-w-4xl flex-col gap-4">
                    {messages.map((message, index) => {
                      const isOwnMessage = message.userFromId === currentUserId;
                      const previousMessage = messages[index - 1];
                      const startsMessageGroup =
                        !previousMessage ||
                        previousMessage.userFromId !== message.userFromId;
                      const body = message.isDeleted
                        ? "Message removed"
                        : message.message?.trim() || "Message unavailable";

                      return (
                        <article
                          key={message.id}
                          data-chat-message-id={message.id}
                          data-chat-sender-id={message.userFromId}
                          data-chat-sent-at={message.sentAt}
                          className={cn(
                            "flex gap-2",
                            isOwnMessage ? "justify-end" : "justify-start",
                          )}
                        >
                          {!isOwnMessage && (
                            <div className="w-9 shrink-0">
                              {startsMessageGroup && (
                                <Avatar className="size-9">
                                  <AvatarImage
                                    src={message.userFromImage ?? undefined}
                                    alt=""
                                  />
                                  <AvatarFallback className="border-border bg-secondary border text-xs font-bold">
                                    {message.userFromName
                                      .charAt(0)
                                      .toUpperCase()}
                                  </AvatarFallback>
                                </Avatar>
                              )}
                            </div>
                          )}

                          <div
                            className={cn(
                              "max-w-[82%] sm:max-w-[70%]",
                              isOwnMessage && "text-right",
                            )}
                          >
                            {!isOwnMessage && startsMessageGroup && (
                              <p className="text-muted-foreground mb-1 text-left text-xs font-bold">
                                {message.userFromName}
                              </p>
                            )}
                            <div
                              className={cn(
                                "border-2 px-3 py-2 text-left",
                                isOwnMessage
                                  ? "border-primary bg-primary text-primary-foreground"
                                  : "border-border bg-card text-foreground",
                                message.isDeleted && "italic opacity-70",
                              )}
                            >
                              <p className="text-sm leading-6 break-words whitespace-pre-wrap">
                                {body}
                              </p>
                            </div>
                            <p className="text-muted-foreground mt-1 text-xs">
                              {formatMessageTime(message.sentAt)}
                              {message.editedAt && !message.isDeleted
                                ? " · edited"
                                : ""}
                              {isOwnMessage
                                ? ` · ${message.status.toLocaleLowerCase()}`
                                : ""}
                            </p>
                          </div>
                        </article>
                      );
                    })}

                    {typingUsers.length > 0 && (
                      <p
                        className="border-primary text-muted-foreground ml-11 border-l-4 pl-3 text-sm"
                        role="status"
                      >
                        {typingUsers.length === 1
                          ? `${typingUsers[0]} is typing…`
                          : `${typingUsers.length} people are typing…`}
                      </p>
                    )}
                    <div />
                  </div>
                )}
              </div>

              <footer className="border-border bg-card shrink-0 border-t-2 p-3 sm:p-4">
                <form
                  className="mx-auto flex max-w-4xl items-end gap-2"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void handleSendMessage();
                  }}
                >
                  <label className="min-w-0 flex-1">
                    <span className="sr-only">Message</span>
                    <textarea
                      value={messageInput}
                      rows={1}
                      maxLength={10_000}
                      disabled={Boolean(messagesError || conversationError)}
                      aria-describedby={
                        sendError ? "chat-send-error" : undefined
                      }
                      onChange={(event) =>
                        handleInputChange(event.target.value)
                      }
                      onKeyDown={(event) => {
                        if (
                          event.key === "Enter" &&
                          !event.shiftKey &&
                          !event.nativeEvent.isComposing
                        ) {
                          event.preventDefault();
                          void handleSendMessage();
                        }
                      }}
                      placeholder="Write a useful message…"
                      className="border-border bg-background text-foreground placeholder:text-muted-foreground focus:border-primary block max-h-32 min-h-11 w-full resize-y border-2 px-3 py-2.5 text-sm outline-none"
                    />
                  </label>
                  <Button
                    type="submit"
                    size="icon"
                    aria-label="Send message"
                    disabled={
                      !messageInput.trim() ||
                      isSending ||
                      Boolean(messagesError || conversationError)
                    }
                  >
                    {isSending ? (
                      <Loader2
                        className="size-4 animate-spin"
                        aria-hidden="true"
                      />
                    ) : (
                      <Send className="size-4" aria-hidden="true" />
                    )}
                  </Button>
                </form>
                {sendError && (
                  <p
                    id="chat-send-error"
                    className="text-destructive mx-auto mt-2 max-w-4xl text-sm"
                    role="alert"
                  >
                    {sendError}
                  </p>
                )}
                <p className="text-muted-foreground mx-auto mt-2 max-w-4xl text-xs">
                  Enter to send · Shift + Enter for a new line
                </p>
              </footer>
            </>
          )}
        </section>
      </div>

      <NewConversationModal
        open={isNewConversationOpen}
        onOpenChange={setIsNewConversationOpen}
        onConversationCreated={(conversationId) => {
          selectConversation(conversationId);
          setIsNewConversationOpen(false);
        }}
      />
    </div>
  );
}
