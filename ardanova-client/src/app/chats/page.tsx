"use client";

import { useState, useRef, useEffect } from "react";
import Link from "next/link";
import {
  MessageCircle,
  Search,
  MoreHorizontal,
  Send,
  Phone,
  Video,
  Image,
  Paperclip,
  Smile,
  Plus,
  Settings,
  Archive,
  Users,
  Bell,
  BellOff,
  Pin,
  Trash2,
  Loader2,
} from "lucide-react";

import { useChat } from "~/hooks/use-chat";
import { useConversation } from "~/hooks/use-conversation";
import { NewConversationModal } from "~/components/chats/new-conversation-modal";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { useSession } from "next-auth/react";

interface ConversationMember {
  id: string;
  name: string;
  image?: string | null;
  isOnline?: boolean;
}

interface LastMessage {
  message: string;
  sentAt: Date | string;
  userFromId: string;
  userFromName?: string;
}

interface Conversation {
  id: string;
  type: "DIRECT" | "GROUP";
  name?: string | null;
  members: ConversationMember[];
  lastMessage?: LastMessage | null;
  unreadCount: number;
  isPinned?: boolean;
}

interface Message {
  id: string;
  userFromId: string;
  userFromName: string;
  userFromImage?: string | null;
  message: string;
  sentAt: Date | string;
  status?: "sent" | "delivered" | "read";
}

function formatMessageTime(date: Date | string): string {
  const d = date instanceof Date ? date : new Date(date);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / (1000 * 60));
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffMin < 1) return "now";
  if (diffMin < 60) return `${diffMin}m`;
  if (diffHour < 24) return `${diffHour}h`;
  if (diffDay < 7) return `${diffDay}d`;
  return d.toLocaleDateString();
}

function formatFullTime(date: Date | string): string {
  const d = date instanceof Date ? date : new Date(date);
  return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

export default function ChatsPage() {
  const { data: session } = useSession();
  const currentUserId = session?.user?.id;

  const [selectedChatId, setSelectedChatId] = useState<string | null>(null);
  const [messageInput, setMessageInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [activeFilter, setActiveFilter] = useState<"all" | "unread" | "groups">("all");
  const [isNewConversationOpen, setIsNewConversationOpen] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Use the chat hook for conversations list
  const {
    conversations,
    isLoading: isLoadingConversations,
  } = useChat();

  // Use the conversation hook for the selected chat
  const {
    conversation: selectedConversation,
    messages,
    isLoading: isLoadingMessages,
    typingUsers,
    sendMessage,
    isSending,
    sendTypingIndicator,
    markAsRead,
  } = useConversation({
    conversationId: selectedChatId ?? "",
    enabled: !!selectedChatId,
  });

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Mark as read when viewing messages
  useEffect(() => {
    if (selectedChatId && messages.length > 0) {
      const lastMessage = messages[messages.length - 1];
      if (lastMessage && lastMessage.userFromId !== currentUserId) {
        void markAsRead(lastMessage.id);
      }
    }
  }, [selectedChatId, messages, currentUserId, markAsRead]);

  // Filter conversations
  const filteredConversations = (conversations as Conversation[]).filter((conv) => {
    if (activeFilter === "unread" && conv.unreadCount === 0) return false;
    if (activeFilter === "groups" && conv.type !== "GROUP") return false;

    const searchLower = searchQuery.toLowerCase();
    if (searchQuery) {
      const convName = getConversationName(conv);
      return convName.toLowerCase().includes(searchLower);
    }
    return true;
  });

  // Sort: pinned first, then by timestamp
  const sortedConversations = [...filteredConversations].sort((a, b) => {
    if (a.isPinned && !b.isPinned) return -1;
    if (!a.isPinned && b.isPinned) return 1;
    const aTime = a.lastMessage?.sentAt ? new Date(a.lastMessage.sentAt).getTime() : 0;
    const bTime = b.lastMessage?.sentAt ? new Date(b.lastMessage.sentAt).getTime() : 0;
    return bTime - aTime;
  });

  // Set first conversation as selected if none selected
  useEffect(() => {
    if (!selectedChatId && sortedConversations.length > 0) {
      setSelectedChatId(sortedConversations[0]?.id ?? null);
    }
  }, [selectedChatId, sortedConversations]);

  function getConversationName(conv: Conversation): string {
    if (conv.type === "GROUP") {
      return conv.name ?? "Group Chat";
    }
    // For direct messages, find the other user
    const otherMember = conv.members.find((m) => m.id !== currentUserId);
    return otherMember?.name ?? "Unknown User";
  }

  function getOtherMember(conv: Conversation): ConversationMember | undefined {
    return conv.members.find((m) => m.id !== currentUserId);
  }

  function getConversationAvatar(conv: Conversation) {
    if (conv.type === "DIRECT") {
      const otherMember = getOtherMember(conv);
      return (
        <Avatar className="size-12">
          <AvatarImage src={otherMember?.image ?? undefined} />
          <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
            {otherMember?.name?.charAt(0) ?? "?"}
          </AvatarFallback>
        </Avatar>
      );
    } else {
      // Group avatar - show stacked member avatars
      const visibleMembers = conv.members.slice(0, 2);
      return (
        <div className="relative size-12">
          <Avatar className="size-8 absolute top-0 left-0 border-2 border-background">
            <AvatarImage src={visibleMembers[0]?.image ?? undefined} />
            <AvatarFallback className="bg-neon-purple/20 text-neon-purple text-xs">
              {visibleMembers[0]?.name?.charAt(0) ?? "?"}
            </AvatarFallback>
          </Avatar>
          <Avatar className="size-8 absolute bottom-0 right-0 border-2 border-background">
            <AvatarImage src={visibleMembers[1]?.image ?? undefined} />
            <AvatarFallback className="bg-neon-green/20 text-neon-green text-xs">
              {visibleMembers[1]?.name?.charAt(0) ?? "?"}
            </AvatarFallback>
          </Avatar>
        </div>
      );
    }
  }

  function isOnline(conv: Conversation): boolean {
    if (conv.type !== "DIRECT") return false;
    const otherMember = getOtherMember(conv);
    return otherMember?.isOnline ?? false;
  }

  async function handleSendMessage() {
    if (!messageInput.trim() || isSending) return;

    const text = messageInput.trim();
    setMessageInput("");

    try {
      await sendMessage(text);
      // Clear typing indicator when message is sent
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
        typingTimeoutRef.current = null;
      }
      void sendTypingIndicator(false);
    } catch (error) {
      // Restore the message if sending fails
      setMessageInput(text);
      console.error("Failed to send message:", error);
    }
  }

  function handleInputChange(value: string) {
    setMessageInput(value);

    // Send typing indicator
    void sendTypingIndicator(true);

    // Clear previous timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    // Set timeout to stop typing indicator
    typingTimeoutRef.current = setTimeout(() => {
      void sendTypingIndicator(false);
    }, 2000);
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      void handleSendMessage();
    }
  }

  // Find the selected conversation object
  const selectedChat = sortedConversations.find((c) => c.id === selectedChatId) ?? null;

  return (
    <div className="h-screen bg-background">
      <div className="flex h-full">
        {/* Conversations Sidebar */}
        <div className="w-80 border-r-2 border-border flex flex-col bg-card">
          {/* Header */}
          <div className="p-4 border-b-2 border-border">
            <div className="flex items-center justify-between mb-3">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <MessageCircle className="size-5 text-primary" />
                Chats
              </h1>
              <div className="flex items-center gap-1">
                <Button variant="ghost" size="icon-sm" onClick={() => setIsNewConversationOpen(true)}>
                  <Plus className="size-4" />
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon-sm">
                      <Settings className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem>
                      <Archive className="size-4 mr-2" />
                      Archived chats
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <Bell className="size-4 mr-2" />
                      Notification settings
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            {/* Search */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search messages..."
                className="w-full pl-10 pr-4 py-2 bg-background border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
              />
            </div>

            {/* Filter tabs */}
            <div className="flex gap-2 mt-3">
              {[
                { id: "all", label: "All" },
                { id: "unread", label: "Unread" },
                { id: "groups", label: "Groups" },
              ].map((filter) => (
                <button
                  key={filter.id}
                  onClick={() => setActiveFilter(filter.id as typeof activeFilter)}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium rounded-full transition-colors",
                    activeFilter === filter.id
                      ? "bg-primary text-primary-foreground"
                      : "bg-background text-muted-foreground hover:text-foreground"
                  )}
                >
                  {filter.label}
                </button>
              ))}
            </div>
          </div>

          {/* Conversations List */}
          <div className="flex-1 overflow-y-auto">
            {isLoadingConversations ? (
              <div className="flex flex-col items-center justify-center py-12 px-4">
                <Loader2 className="size-8 text-primary animate-spin mb-3" />
                <p className="text-sm text-muted-foreground">Loading conversations...</p>
              </div>
            ) : sortedConversations.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 px-4">
                <MessageCircle className="size-10 text-muted-foreground mb-3" />
                <p className="text-sm text-muted-foreground text-center">
                  {searchQuery ? "No conversations found" : "No conversations yet"}
                </p>
              </div>
            ) : (
              sortedConversations.map((conversation) => (
                <button
                  key={conversation.id}
                  onClick={() => setSelectedChatId(conversation.id)}
                  className={cn(
                    "w-full p-3 flex items-center gap-3 hover:bg-background/50 transition-colors text-left",
                    selectedChatId === conversation.id && "bg-background border-l-2 border-l-primary"
                  )}
                >
                  <div className="relative shrink-0">
                    {getConversationAvatar(conversation)}
                    {isOnline(conversation) && (
                      <span className="absolute bottom-0 right-0 size-3 bg-neon-green border-2 border-card rounded-full" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex items-center gap-1.5 min-w-0">
                        <p className="font-medium text-foreground text-sm truncate">
                          {getConversationName(conversation)}
                        </p>
                        {conversation.isPinned && (
                          <Pin className="size-3 text-muted-foreground shrink-0" />
                        )}
                        {conversation.type === "GROUP" && (
                          <Users className="size-3 text-muted-foreground shrink-0" />
                        )}
                      </div>
                      <span className="text-xs text-muted-foreground shrink-0">
                        {conversation.lastMessage?.sentAt
                          ? formatMessageTime(conversation.lastMessage.sentAt)
                          : ""}
                      </span>
                    </div>
                    <div className="flex items-center justify-between gap-2 mt-0.5">
                      <p
                        className={cn(
                          "text-xs truncate",
                          conversation.unreadCount > 0
                            ? "text-foreground font-medium"
                            : "text-muted-foreground"
                        )}
                      >
                        {conversation.lastMessage?.message ?? "No messages yet"}
                      </p>
                      {conversation.unreadCount > 0 && (
                        <Badge variant="neon" size="sm" className="shrink-0">
                          {conversation.unreadCount}
                        </Badge>
                      )}
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>
        </div>

        {/* Chat Area */}
        {selectedChat ? (
          <div className="flex-1 flex flex-col">
            {/* Chat Header */}
            <div className="p-4 border-b-2 border-border bg-card flex items-center justify-between">
              <Link
                href={
                  selectedChat.type === "DIRECT"
                    ? `/dashboard/profile/${getOtherMember(selectedChat)?.id}`
                    : "#"
                }
                className="flex items-center gap-3"
              >
                <div className="relative">
                  {getConversationAvatar(selectedChat)}
                  {isOnline(selectedChat) && (
                    <span className="absolute bottom-0 right-0 size-3 bg-neon-green border-2 border-card rounded-full" />
                  )}
                </div>
                <div>
                  <p className="font-medium text-foreground hover:text-primary transition-colors">
                    {getConversationName(selectedChat)}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {selectedChat.type === "DIRECT"
                      ? isOnline(selectedChat)
                        ? "Active now"
                        : "Offline"
                      : `${selectedChat.members.length} members`}
                  </p>
                </div>
              </Link>
              <div className="flex items-center gap-1">
                <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-primary">
                  <Phone className="size-4" />
                </Button>
                <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-primary">
                  <Video className="size-4" />
                </Button>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon-sm" className="text-muted-foreground">
                      <MoreHorizontal className="size-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem>
                      <Pin className="size-4 mr-2" />
                      {selectedChat.isPinned ? "Unpin conversation" : "Pin conversation"}
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <BellOff className="size-4 mr-2" />
                      Mute notifications
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem className="text-red-500">
                      <Trash2 className="size-4 mr-2" />
                      Delete conversation
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-background">
              {isLoadingMessages ? (
                <div className="flex items-center justify-center h-full">
                  <Loader2 className="size-8 text-primary animate-spin" />
                </div>
              ) : messages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-center">
                  <MessageCircle className="size-12 text-muted-foreground mb-3" />
                  <p className="text-muted-foreground">No messages yet</p>
                  <p className="text-sm text-muted-foreground">Start the conversation!</p>
                </div>
              ) : (
                <>
                  {/* Date separator */}
                  <div className="flex items-center gap-4">
                    <div className="flex-1 h-px bg-border" />
                    <span className="text-xs text-muted-foreground">Today</span>
                    <div className="flex-1 h-px bg-border" />
                  </div>

                  {(messages as Message[]).map((message, index) => {
                    const isOwn = message.userFromId === currentUserId;
                    const showAvatar =
                      !isOwn &&
                      (index === 0 || (messages as Message[])[index - 1]?.userFromId !== message.userFromId);
                    const isLastInGroup =
                      index === messages.length - 1 ||
                      (messages as Message[])[index + 1]?.userFromId !== message.userFromId;

                    return (
                      <div
                        key={message.id}
                        className={cn("flex gap-2", isOwn ? "justify-end" : "justify-start")}
                      >
                        {!isOwn && (
                          <div className="w-8 shrink-0">
                            {showAvatar && (
                              <Avatar className="size-8">
                                <AvatarImage src={message.userFromImage ?? undefined} />
                                <AvatarFallback className="bg-neon-pink/20 text-neon-pink text-xs">
                                  {message.userFromName?.charAt(0) ?? "?"}
                                </AvatarFallback>
                              </Avatar>
                            )}
                          </div>
                        )}
                        <div
                          className={cn(
                            "max-w-[65%] group",
                            isOwn && "flex flex-col items-end"
                          )}
                        >
                          <div
                            className={cn(
                              "px-4 py-2 rounded-2xl",
                              isOwn
                                ? "bg-primary text-primary-foreground rounded-br-md"
                                : "bg-card border-2 border-border text-foreground rounded-bl-md"
                            )}
                          >
                            <p className="text-sm whitespace-pre-wrap">{message.message}</p>
                          </div>
                          {isLastInGroup && (
                            <p
                              className={cn(
                                "text-xs mt-1 opacity-0 group-hover:opacity-100 transition-opacity",
                                isOwn ? "text-muted-foreground" : "text-muted-foreground ml-1"
                              )}
                            >
                              {formatFullTime(message.sentAt)}
                            </p>
                          )}
                        </div>
                      </div>
                    );
                  })}

                  {/* Typing indicator */}
                  {typingUsers.length > 0 && (
                    <div className="flex gap-2 items-center">
                      <div className="w-8 shrink-0" />
                      <div className="bg-card border-2 border-border rounded-2xl rounded-bl-md px-4 py-2">
                        <div className="flex items-center gap-1">
                          <span className="text-sm text-muted-foreground">
                            {typingUsers.length === 1
                              ? `${typingUsers[0]} is typing`
                              : `${typingUsers.slice(0, -1).join(", ")} and ${typingUsers[typingUsers.length - 1]} are typing`}
                          </span>
                          <span className="flex gap-0.5">
                            <span className="size-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: "0ms" }} />
                            <span className="size-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: "150ms" }} />
                            <span className="size-1.5 bg-muted-foreground rounded-full animate-bounce" style={{ animationDelay: "300ms" }} />
                          </span>
                        </div>
                      </div>
                    </div>
                  )}

                  <div ref={messagesEndRef} />
                </>
              )}
            </div>

            {/* Message Input */}
            <div className="p-4 border-t-2 border-border bg-card">
              <div className="flex items-end gap-2">
                <div className="flex gap-1">
                  <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-primary">
                    <Plus className="size-5" />
                  </Button>
                  <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-primary">
                    <Image className="size-5" />
                  </Button>
                  <Button variant="ghost" size="icon-sm" className="text-muted-foreground hover:text-primary">
                    <Paperclip className="size-5" />
                  </Button>
                </div>
                <div className="flex-1 relative">
                  <input
                    type="text"
                    value={messageInput}
                    onChange={(e) => handleInputChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Type a message..."
                    className="w-full px-4 py-2.5 bg-background border-2 border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none rounded-full"
                  />
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-primary"
                  >
                    <Smile className="size-5" />
                  </Button>
                </div>
                <Button
                  variant="neon"
                  size="icon"
                  disabled={!messageInput.trim() || isSending}
                  onClick={() => void handleSendMessage()}
                  className="rounded-full"
                >
                  {isSending ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Send className="size-4" />
                  )}
                </Button>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex-1 flex items-center justify-center bg-background">
            <div className="text-center">
              <div className="size-20 bg-card border-2 border-border rounded-full flex items-center justify-center mx-auto mb-4">
                <MessageCircle className="size-10 text-muted-foreground" />
              </div>
              <h2 className="text-lg font-medium text-foreground mb-2">Your Messages</h2>
              <p className="text-muted-foreground text-sm max-w-xs">
                Select a conversation or start a new chat to connect with your team
              </p>
              <Button variant="neon" className="mt-4" onClick={() => setIsNewConversationOpen(true)}>
                <Plus className="size-4 mr-2" />
                New Message
              </Button>
            </div>
          </div>
        )}
      </div>

      <NewConversationModal
        open={isNewConversationOpen}
        onOpenChange={setIsNewConversationOpen}
        onConversationCreated={(id) => setSelectedChatId(id)}
      />
    </div>
  );
}
