"use client";

import { useState } from "react";
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
} from "lucide-react";

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

// Sample conversation data
const sampleConversations = [
  {
    id: "c1",
    type: "direct" as const,
    user: {
      id: "u1",
      name: "Sarah Chen",
      avatar: "https://i.pravatar.cc/150?u=sarah",
      isOnline: true,
    },
    lastMessage: {
      text: "Thanks for the feedback on the design! I'll incorporate those changes.",
      timestamp: new Date(Date.now() - 1000 * 60 * 5),
      isRead: false,
      senderId: "u1",
    },
    unreadCount: 2,
    isPinned: true,
  },
  {
    id: "c2",
    type: "direct" as const,
    user: {
      id: "u2",
      name: "Marcus Rodriguez",
      avatar: "https://i.pravatar.cc/150?u=marcus",
      isOnline: true,
    },
    lastMessage: {
      text: "The mobile app build is ready for testing.",
      timestamp: new Date(Date.now() - 1000 * 60 * 30),
      isRead: true,
      senderId: "u2",
    },
    unreadCount: 0,
    isPinned: false,
  },
  {
    id: "c3",
    type: "group" as const,
    name: "DeFi Dashboard Team",
    avatar: null,
    members: [
      { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
      { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
      { id: "u3", name: "Alex Kim", avatar: "https://i.pravatar.cc/150?u=alex" },
    ],
    lastMessage: {
      text: "Alex: Let's schedule the sprint review for Friday",
      timestamp: new Date(Date.now() - 1000 * 60 * 45),
      isRead: true,
      senderId: "u3",
    },
    unreadCount: 0,
    isPinned: true,
  },
  {
    id: "c4",
    type: "direct" as const,
    user: {
      id: "u6",
      name: "Emma Watson",
      avatar: "https://i.pravatar.cc/150?u=emma",
      isOnline: false,
    },
    lastMessage: {
      text: "Let me know if you need help with the UX review.",
      timestamp: new Date(Date.now() - 1000 * 60 * 60 * 2),
      isRead: true,
      senderId: "u6",
    },
    unreadCount: 0,
    isPinned: false,
  },
  {
    id: "c5",
    type: "direct" as const,
    user: {
      id: "u7",
      name: "David Park",
      avatar: "https://i.pravatar.cc/150?u=david",
      isOnline: false,
    },
    lastMessage: {
      text: "The API endpoints are all documented now.",
      timestamp: new Date(Date.now() - 1000 * 60 * 60 * 24),
      isRead: true,
      senderId: "u7",
    },
    unreadCount: 0,
    isPinned: false,
  },
  {
    id: "c6",
    type: "group" as const,
    name: "Web3 Guild Chat",
    avatar: null,
    members: [
      { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
      { id: "u4", name: "James Lee", avatar: "https://i.pravatar.cc/150?u=james" },
      { id: "u5", name: "Mia Johnson", avatar: "https://i.pravatar.cc/150?u=mia" },
    ],
    lastMessage: {
      text: "James: New bounty posted for smart contract audit",
      timestamp: new Date(Date.now() - 1000 * 60 * 60 * 3),
      isRead: false,
      senderId: "u4",
    },
    unreadCount: 5,
    isPinned: false,
  },
];

const sampleMessages = [
  {
    id: "m1",
    senderId: "u1",
    senderName: "Sarah Chen",
    senderAvatar: "https://i.pravatar.cc/150?u=sarah",
    text: "Hey! I just finished the initial mockups for the landing page.",
    timestamp: new Date(Date.now() - 1000 * 60 * 60),
  },
  {
    id: "m2",
    senderId: "current-user",
    senderName: "You",
    text: "That's great! Can you share them?",
    timestamp: new Date(Date.now() - 1000 * 60 * 55),
  },
  {
    id: "m3",
    senderId: "u1",
    senderName: "Sarah Chen",
    senderAvatar: "https://i.pravatar.cc/150?u=sarah",
    text: "Of course! I'll upload them to the project drive. The hero section uses the new brand colors and I added an interactive element for the value proposition.",
    timestamp: new Date(Date.now() - 1000 * 60 * 50),
  },
  {
    id: "m4",
    senderId: "current-user",
    senderName: "You",
    text: "Sounds perfect. I especially like the interactive elements idea - it should help with engagement.",
    timestamp: new Date(Date.now() - 1000 * 60 * 10),
  },
  {
    id: "m5",
    senderId: "u1",
    senderName: "Sarah Chen",
    senderAvatar: "https://i.pravatar.cc/150?u=sarah",
    text: "Thanks for the feedback on the design! I'll incorporate those changes.",
    timestamp: new Date(Date.now() - 1000 * 60 * 5),
  },
];

function formatMessageTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / (1000 * 60));
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffMin < 1) return "now";
  if (diffMin < 60) return `${diffMin}m`;
  if (diffHour < 24) return `${diffHour}h`;
  if (diffDay < 7) return `${diffDay}d`;
  return date.toLocaleDateString();
}

function formatFullTime(date: Date): string {
  return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

type Conversation = typeof sampleConversations[0];

export default function ChatsPage() {
  const [selectedChat, setSelectedChat] = useState<Conversation | null>(sampleConversations[0] ?? null);
  const [messageInput, setMessageInput] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [activeFilter, setActiveFilter] = useState<"all" | "unread" | "groups">("all");

  // Filter conversations
  const filteredConversations = sampleConversations.filter((conv) => {
    if (activeFilter === "unread" && conv.unreadCount === 0) return false;
    if (activeFilter === "groups" && conv.type !== "group") return false;

    const searchLower = searchQuery.toLowerCase();
    if (searchQuery) {
      if (conv.type === "direct") {
        return conv.user.name.toLowerCase().includes(searchLower);
      } else {
        return conv.name.toLowerCase().includes(searchLower);
      }
    }
    return true;
  });

  // Sort: pinned first, then by timestamp
  const sortedConversations = [...filteredConversations].sort((a, b) => {
    if (a.isPinned && !b.isPinned) return -1;
    if (!a.isPinned && b.isPinned) return 1;
    return b.lastMessage.timestamp.getTime() - a.lastMessage.timestamp.getTime();
  });

  const getConversationName = (conv: Conversation) => {
    return conv.type === "direct" ? conv.user.name : conv.name;
  };

  const getConversationAvatar = (conv: Conversation) => {
    if (conv.type === "direct") {
      return (
        <Avatar className="size-12">
          <AvatarImage src={conv.user.avatar} />
          <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
            {conv.user.name.charAt(0)}
          </AvatarFallback>
        </Avatar>
      );
    } else {
      // Group avatar - show stacked member avatars
      return (
        <div className="relative size-12">
          <Avatar className="size-8 absolute top-0 left-0 border-2 border-background">
            <AvatarImage src={conv.members[0]?.avatar} />
            <AvatarFallback className="bg-neon-purple/20 text-neon-purple text-xs">
              {conv.members[0]?.name.charAt(0)}
            </AvatarFallback>
          </Avatar>
          <Avatar className="size-8 absolute bottom-0 right-0 border-2 border-background">
            <AvatarImage src={conv.members[1]?.avatar} />
            <AvatarFallback className="bg-neon-green/20 text-neon-green text-xs">
              {conv.members[1]?.name.charAt(0)}
            </AvatarFallback>
          </Avatar>
        </div>
      );
    }
  };

  const isOnline = (conv: Conversation) => {
    return conv.type === "direct" && conv.user.isOnline;
  };

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
                <Button variant="ghost" size="icon-sm">
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
            {sortedConversations.length === 0 ? (
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
                  onClick={() => setSelectedChat(conversation)}
                  className={cn(
                    "w-full p-3 flex items-center gap-3 hover:bg-background/50 transition-colors text-left",
                    selectedChat?.id === conversation.id && "bg-background border-l-2 border-l-primary"
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
                        {conversation.type === "group" && (
                          <Users className="size-3 text-muted-foreground shrink-0" />
                        )}
                      </div>
                      <span className="text-xs text-muted-foreground shrink-0">
                        {formatMessageTime(conversation.lastMessage.timestamp)}
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
                        {conversation.lastMessage.text}
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
                  selectedChat.type === "direct"
                    ? `/dashboard/profile/${selectedChat.user.id}`
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
                    {selectedChat.type === "direct"
                      ? selectedChat.user.isOnline
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
              {/* Date separator */}
              <div className="flex items-center gap-4">
                <div className="flex-1 h-px bg-border" />
                <span className="text-xs text-muted-foreground">Today</span>
                <div className="flex-1 h-px bg-border" />
              </div>

              {sampleMessages.map((message, index) => {
                const isOwn = message.senderId === "current-user";
                const showAvatar =
                  !isOwn &&
                  (index === 0 || sampleMessages[index - 1]?.senderId !== message.senderId);
                const isLastInGroup =
                  index === sampleMessages.length - 1 ||
                  sampleMessages[index + 1]?.senderId !== message.senderId;

                return (
                  <div
                    key={message.id}
                    className={cn("flex gap-2", isOwn ? "justify-end" : "justify-start")}
                  >
                    {!isOwn && (
                      <div className="w-8 shrink-0">
                        {showAvatar && (
                          <Avatar className="size-8">
                            <AvatarImage src={message.senderAvatar} />
                            <AvatarFallback className="bg-neon-pink/20 text-neon-pink text-xs">
                              {message.senderName.charAt(0)}
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
                        <p className="text-sm whitespace-pre-wrap">{message.text}</p>
                      </div>
                      {isLastInGroup && (
                        <p
                          className={cn(
                            "text-xs mt-1 opacity-0 group-hover:opacity-100 transition-opacity",
                            isOwn ? "text-muted-foreground" : "text-muted-foreground ml-1"
                          )}
                        >
                          {formatFullTime(message.timestamp)}
                        </p>
                      )}
                    </div>
                  </div>
                );
              })}
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
                    onChange={(e) => setMessageInput(e.target.value)}
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
                  disabled={!messageInput.trim()}
                  className="rounded-full"
                >
                  <Send className="size-4" />
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
              <Button variant="neon" className="mt-4">
                <Plus className="size-4 mr-2" />
                New Message
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
