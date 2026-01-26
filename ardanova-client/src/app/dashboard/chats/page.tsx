"use client";

import { useState } from "react";
import Link from "next/link";
import { MessageCircle, Search, MoreHorizontal, Send, Phone, Video } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";

// Sample chat data - in production this would come from API
const sampleConversations = [
  {
    id: "c1",
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
    },
    unreadCount: 2,
  },
  {
    id: "c2",
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
    },
    unreadCount: 0,
  },
  {
    id: "c3",
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
    },
    unreadCount: 0,
  },
  {
    id: "c4",
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
    },
    unreadCount: 0,
  },
];

const sampleMessages = [
  {
    id: "m1",
    senderId: "u1",
    text: "Hey! I just finished the initial mockups for the landing page.",
    timestamp: new Date(Date.now() - 1000 * 60 * 60),
  },
  {
    id: "m2",
    senderId: "current-user",
    text: "That's great! Can you share them?",
    timestamp: new Date(Date.now() - 1000 * 60 * 55),
  },
  {
    id: "m3",
    senderId: "u1",
    text: "Of course! I'll upload them to the project drive. The hero section uses the new brand colors and I added an interactive element for the value proposition.",
    timestamp: new Date(Date.now() - 1000 * 60 * 50),
  },
  {
    id: "m4",
    senderId: "current-user",
    text: "Sounds perfect. I especially like the interactive elements idea - it should help with engagement.",
    timestamp: new Date(Date.now() - 1000 * 60 * 10),
  },
  {
    id: "m5",
    senderId: "u1",
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

  if (diffMin < 60) return `${diffMin}m`;
  if (diffHour < 24) return `${diffHour}h`;
  if (diffDay < 7) return `${diffDay}d`;
  return date.toLocaleDateString();
}

export default function ChatsPage() {
  const [selectedChat, setSelectedChat] = useState(sampleConversations[0]);
  const [messageInput, setMessageInput] = useState("");

  return (
    <div className="h-[calc(100vh-64px)] bg-background">
      <div className="flex h-full">
        {/* Conversations List */}
        <div className="w-80 border-r-2 border-border flex flex-col">
          <div className="p-4 border-b-2 border-border">
            <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
              <MessageCircle className="size-5 text-primary" />
              Chats
            </h1>
            <div className="mt-3 relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search conversations..."
                className="w-full pl-10 pr-4 py-2 bg-card border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
              />
            </div>
          </div>

          <div className="flex-1 overflow-y-auto">
            {sampleConversations.map((conversation) => (
              <button
                key={conversation.id}
                onClick={() => setSelectedChat(conversation)}
                className={cn(
                  "w-full p-4 flex items-center gap-3 hover:bg-card transition-colors border-b border-border text-left",
                  selectedChat?.id === conversation.id && "bg-card border-l-2 border-l-primary"
                )}
              >
                <div className="relative">
                  <Avatar className="size-10">
                    <AvatarImage src={conversation.user.avatar} />
                    <AvatarFallback>{conversation.user.name.charAt(0)}</AvatarFallback>
                  </Avatar>
                  {conversation.user.isOnline && (
                    <span className="absolute bottom-0 right-0 size-3 bg-neon-green border-2 border-background rounded-full" />
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <p className="font-medium text-foreground text-sm truncate">
                      {conversation.user.name}
                    </p>
                    <span className="text-xs text-muted-foreground">
                      {formatMessageTime(conversation.lastMessage.timestamp)}
                    </span>
                  </div>
                  <p className={cn(
                    "text-xs truncate mt-0.5",
                    conversation.unreadCount > 0 ? "text-foreground font-medium" : "text-muted-foreground"
                  )}>
                    {conversation.lastMessage.text}
                  </p>
                </div>
                {conversation.unreadCount > 0 && (
                  <Badge variant="neon" size="sm" className="ml-2">
                    {conversation.unreadCount}
                  </Badge>
                )}
              </button>
            ))}
          </div>
        </div>

        {/* Chat Area */}
        {selectedChat ? (
          <div className="flex-1 flex flex-col">
            {/* Chat Header */}
            <div className="p-4 border-b-2 border-border flex items-center justify-between">
              <Link
                href={`/dashboard/profile/${selectedChat.user.id}`}
                className="flex items-center gap-3"
              >
                <div className="relative">
                  <Avatar className="size-10">
                    <AvatarImage src={selectedChat.user.avatar} />
                    <AvatarFallback>{selectedChat.user.name.charAt(0)}</AvatarFallback>
                  </Avatar>
                  {selectedChat.user.isOnline && (
                    <span className="absolute bottom-0 right-0 size-3 bg-neon-green border-2 border-background rounded-full" />
                  )}
                </div>
                <div>
                  <p className="font-medium text-foreground hover:text-primary transition-colors">
                    {selectedChat.user.name}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {selectedChat.user.isOnline ? "Online" : "Offline"}
                  </p>
                </div>
              </Link>
              <div className="flex items-center gap-2">
                <Button variant="ghost" size="icon-sm">
                  <Phone className="size-4" />
                </Button>
                <Button variant="ghost" size="icon-sm">
                  <Video className="size-4" />
                </Button>
                <Button variant="ghost" size="icon-sm">
                  <MoreHorizontal className="size-4" />
                </Button>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {sampleMessages.map((message) => {
                const isOwn = message.senderId === "current-user";
                return (
                  <div
                    key={message.id}
                    className={cn("flex", isOwn ? "justify-end" : "justify-start")}
                  >
                    <div
                      className={cn(
                        "max-w-[70%] px-4 py-2",
                        isOwn
                          ? "bg-primary text-primary-foreground"
                          : "bg-card border-2 border-border text-foreground"
                      )}
                    >
                      <p className="text-sm">{message.text}</p>
                      <p className={cn(
                        "text-xs mt-1",
                        isOwn ? "text-primary-foreground/70" : "text-muted-foreground"
                      )}>
                        {formatMessageTime(message.timestamp)}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Message Input */}
            <div className="p-4 border-t-2 border-border">
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  value={messageInput}
                  onChange={(e) => setMessageInput(e.target.value)}
                  placeholder="Type a message..."
                  className="flex-1 px-4 py-2 bg-card border-2 border-border text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none"
                />
                <Button variant="neon" size="icon">
                  <Send className="size-4" />
                </Button>
              </div>
            </div>
          </div>
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <MessageCircle className="size-12 text-muted-foreground mx-auto mb-4" />
              <p className="text-muted-foreground">Select a conversation to start chatting</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
