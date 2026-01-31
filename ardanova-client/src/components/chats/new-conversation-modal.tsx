"use client";

import { useState, useDeferredValue } from "react";
import { Search, MessageCircle, User, Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "~/components/ui/dialog";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { api } from "~/trpc/react";

interface NewConversationModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConversationCreated: (conversationId: string) => void;
}

export function NewConversationModal({
  open,
  onOpenChange,
  onConversationCreated,
}: NewConversationModalProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const deferredQuery = useDeferredValue(searchQuery);
  const [isCreating, setIsCreating] = useState(false);

  const { data: users, isLoading } = api.chat.searchUsers.useQuery(
    { query: deferredQuery, limit: 30 },
    { enabled: open }
  );

  const createDirect = api.chat.getOrCreateDirect.useMutation();
  const utils = api.useUtils();

  async function handleSelectUser(userId: string) {
    if (isCreating) return;
    setIsCreating(true);

    try {
      const conversation = await createDirect.mutateAsync({
        otherUserId: userId,
      });
      void utils.chat.getConversations.invalidate();
      onConversationCreated(conversation.id);
      onOpenChange(false);
      setSearchQuery("");
    } catch (error) {
      console.error("Failed to create conversation:", error);
    } finally {
      setIsCreating(false);
    }
  }

  // Put self at top of list
  const sortedUsers = users
    ? [...users].sort((a, b) => {
        if (a.isSelf && !b.isSelf) return -1;
        if (!a.isSelf && b.isSelf) return 1;
        return 0;
      })
    : [];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MessageCircle className="size-5 text-primary" />
            New Message
          </DialogTitle>
          <DialogDescription>
            Search for a user to start a conversation
          </DialogDescription>
        </DialogHeader>

        {/* Search input */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search by name or email..."
            autoFocus
            className="w-full pl-10 pr-4 py-2.5 bg-background border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
          />
        </div>

        {/* User list */}
        <div className="max-h-72 overflow-y-auto -mx-6 px-6">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-8">
              <Loader2 className="size-6 text-primary animate-spin mb-2" />
              <p className="text-sm text-muted-foreground">Loading users...</p>
            </div>
          ) : sortedUsers.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8">
              <User className="size-8 text-muted-foreground mb-2" />
              <p className="text-sm text-muted-foreground">
                {searchQuery ? "No users found" : "No users available"}
              </p>
            </div>
          ) : (
            <div className="space-y-1">
              {sortedUsers.map((user) => (
                <button
                  key={user.id}
                  onClick={() => void handleSelectUser(user.id)}
                  disabled={isCreating}
                  className="w-full flex items-center gap-3 p-2.5 rounded-lg hover:bg-accent/50 transition-colors text-left disabled:opacity-50"
                >
                  <Avatar className="size-10">
                    <AvatarImage src={user.image ?? undefined} />
                    <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
                      {user.name.charAt(0).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-medium text-foreground truncate">
                        {user.name}
                      </p>
                      {user.isSelf && (
                        <Badge variant="outline" className="text-xs shrink-0">
                          You
                        </Badge>
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground truncate">
                      {user.isSelf ? "Message yourself" : user.email}
                    </p>
                  </div>
                  {isCreating && (
                    <Loader2 className="size-4 text-muted-foreground animate-spin shrink-0" />
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
