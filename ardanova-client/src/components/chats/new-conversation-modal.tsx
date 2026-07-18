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
  const [createError, setCreateError] = useState<string | null>(null);

  const {
    data: users,
    isLoading,
    error: usersError,
    refetch: refetchUsers,
  } = api.chat.searchUsers.useQuery(
    { query: deferredQuery, limit: 30 },
    { enabled: open, retry: false },
  );

  const createDirect = api.chat.getOrCreateDirect.useMutation();
  const utils = api.useUtils();

  async function handleSelectUser(userId: string) {
    if (isCreating) return;
    setIsCreating(true);
    setCreateError(null);

    try {
      const conversation = await createDirect.mutateAsync({
        otherUserId: userId,
      });
      void utils.chat.getConversations.invalidate();
      onConversationCreated(conversation.id);
      onOpenChange(false);
      setSearchQuery("");
    } catch {
      setCreateError(
        "The conversation could not be created. No message was sent. Try again when the service is available.",
      );
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

  function handleDialogOpenChange(nextOpen: boolean) {
    if (isCreating) return;
    if (!nextOpen) {
      setCreateError(null);
      setSearchQuery("");
    }
    onOpenChange(nextOpen);
  }

  return (
    <Dialog open={open} onOpenChange={handleDialogOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MessageCircle className="text-primary size-5" />
            New Message
          </DialogTitle>
          <DialogDescription>
            Search for a user to start a conversation
          </DialogDescription>
        </DialogHeader>

        {/* Search input */}
        <div className="relative">
          <label htmlFor="new-conversation-search" className="sr-only">
            Search people by name or email
          </label>
          <Search
            className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2"
            aria-hidden="true"
          />
          <input
            id="new-conversation-search"
            type="search"
            value={searchQuery}
            onChange={(e) => {
              setSearchQuery(e.target.value);
              setCreateError(null);
            }}
            placeholder="Search by name or email..."
            autoFocus
            aria-describedby={
              createError ? "new-conversation-create-error" : undefined
            }
            className="border-border bg-background text-foreground placeholder:text-muted-foreground focus:border-primary min-h-11 w-full border-2 py-2.5 pr-4 pl-10 text-sm focus:outline-none"
          />
        </div>

        {createError && (
          <div
            id="new-conversation-create-error"
            className="border-destructive bg-destructive/10 text-destructive border-2 p-3 text-sm"
            role="alert"
          >
            {createError}
          </div>
        )}

        {/* User list */}
        <div className="-mx-6 max-h-72 overflow-y-auto px-6">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-8">
              <Loader2 className="text-primary mb-2 size-6 animate-spin" />
              <p className="text-muted-foreground text-sm">Loading users...</p>
            </div>
          ) : usersError ? (
            <div
              className="border-destructive bg-destructive/10 my-3 space-y-3 border-2 p-4"
              role="alert"
            >
              <p className="text-foreground font-bold">
                People could not be loaded
              </p>
              <p className="text-muted-foreground text-sm">
                This is a service error, not an empty directory.
              </p>
              <button
                type="button"
                onClick={() => void refetchUsers()}
                className="border-foreground min-h-11 border-2 px-3 text-sm font-bold"
              >
                Retry search
              </button>
            </div>
          ) : sortedUsers.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8">
              <User className="text-muted-foreground mb-2 size-8" />
              <p className="text-muted-foreground text-sm">
                {searchQuery ? "No users found" : "No users available"}
              </p>
            </div>
          ) : (
            <div className="space-y-1">
              {sortedUsers.map((user) => (
                <button
                  key={user.id}
                  type="button"
                  onClick={() => void handleSelectUser(user.id)}
                  disabled={isCreating}
                  className="hover:bg-accent/50 flex min-h-11 w-full items-center gap-3 p-2.5 text-left transition-colors disabled:opacity-50"
                >
                  <Avatar className="size-10">
                    <AvatarImage src={user.image ?? undefined} />
                    <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
                      {user.name.charAt(0).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="text-foreground truncate text-sm font-medium">
                        {user.name}
                      </p>
                      {user.isSelf && (
                        <Badge variant="outline" className="shrink-0 text-xs">
                          You
                        </Badge>
                      )}
                    </div>
                    <p className="text-muted-foreground truncate text-xs">
                      {user.isSelf ? "Message yourself" : user.email}
                    </p>
                  </div>
                  {isCreating && (
                    <Loader2 className="text-muted-foreground size-4 shrink-0 animate-spin" />
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
