"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Loader2, Bell, Plus, Trash2, Send } from "lucide-react";

interface UpdatesTabProps {
  guildId: string;
  isOwner: boolean;
}

export function UpdatesTab({ guildId, isOwner }: UpdatesTabProps) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isFormOpen, setIsFormOpen] = useState(false);

  const utils = api.useUtils();

  // Query for fetching updates (will be added by Worker 4)
  const { data: updates, isLoading, error } = api.guild.getUpdates.useQuery({ guildId });

  // Mutation for creating update with optimistic updates
  const createMutation = api.guild.createUpdate.useMutation({
    onMutate: async (newUpdate) => {
      if (!newUpdate) return;
      await utils.guild.getUpdates.cancel({ guildId });
      const previous = utils.guild.getUpdates.getData({ guildId });

      utils.guild.getUpdates.setData({ guildId }, (old) => [
        {
          id: 'temp-' + Date.now(),
          guildId: newUpdate.guildId,
          userId: 'current-user',
          title: newUpdate.title,
          content: newUpdate.content,
          createdAt: new Date().toISOString(),
          user: { id: 'current-user', name: 'You', email: '', image: undefined },
        },
        ...(old ?? []),
      ]);

      return { previous };
    },
    onError: (err, newUpdate, context) => {
      if (context?.previous) {
        utils.guild.getUpdates.setData({ guildId }, context.previous);
      }
    },
    onSettled: () => {
      void utils.guild.getUpdates.invalidate({ guildId });
    },
    onSuccess: () => {
      setTitle("");
      setContent("");
      setIsFormOpen(false);
    },
  });

  // Mutation for deleting update with optimistic updates
  const deleteMutation = api.guild.deleteUpdate.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      await utils.guild.getUpdates.cancel({ guildId });
      const previous = utils.guild.getUpdates.getData({ guildId });

      utils.guild.getUpdates.setData({ guildId }, (old) =>
        (old ?? []).filter((update) => update.id !== variables.updateId)
      );

      return { previous };
    },
    onError: (err, variables, context) => {
      if (context?.previous) {
        utils.guild.getUpdates.setData({ guildId }, context.previous);
      }
    },
    onSettled: () => {
      void utils.guild.getUpdates.invalidate({ guildId });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) return;

    createMutation.mutate({
      guildId,
      title: title.trim(),
      content: content.trim(),
    });
  };

  const handleDelete = (updateId: string) => {
    if (confirm("Are you sure you want to delete this update?")) {
      deleteMutation.mutate({ guildId, updateId });
    }
  };

  const getInitials = (name?: string) => {
    if (!name) return "?";
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMs = now.getTime() - date.getTime();
    const diffInHours = diffInMs / (1000 * 60 * 60);

    if (diffInHours < 24) {
      const hours = Math.floor(diffInHours);
      if (hours === 0) {
        const minutes = Math.floor(diffInMs / (1000 * 60));
        return minutes === 0 ? "Just now" : `${minutes}m ago`;
      }
      return `${hours}h ago`;
    } else if (diffInHours < 48) {
      return "Yesterday";
    } else {
      return date.toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
      });
    }
  };

  return (
    <div className="space-y-6">
      {/* Header with Create Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Bell className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">Guild Updates</h2>
        </div>
        {isOwner && !isFormOpen && (
          <Button onClick={() => setIsFormOpen(true)} size="sm">
            <Plus className="mr-2 h-4 w-4" />
            New Update
          </Button>
        )}
      </div>

      {/* Create Update Form */}
      {isOwner && isFormOpen && (
        <Card>
          <CardHeader>
            <CardTitle>Post a New Update</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="title" className="block text-sm font-medium mb-1">
                  Title
                </label>
                <input
                  id="title"
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Update title"
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  required
                />
              </div>
              <div>
                <label htmlFor="content" className="block text-sm font-medium mb-1">
                  Content
                </label>
                <textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="Share news, achievements, or announcements..."
                  rows={4}
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  required
                />
              </div>
              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Posting...
                    </>
                  ) : (
                    <>
                      <Send className="mr-2 h-4 w-4" />
                      Post Update
                    </>
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsFormOpen(false);
                    setTitle("");
                    setContent("");
                  }}
                  disabled={createMutation.isPending}
                >
                  Cancel
                </Button>
              </div>
              {createMutation.error && (
                <div className="p-3 bg-destructive/10 border border-destructive/30 rounded text-sm text-destructive">
                  Error: {createMutation.error.message}
                </div>
              )}
            </form>
          </CardContent>
        </Card>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      )}

      {/* Error State */}
      {error && (
        <Card className="border-destructive">
          <CardContent className="pt-6">
            <p className="text-sm text-destructive">
              Failed to load updates: {error.message}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {!isLoading && !error && (!updates || updates.length === 0) && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Bell className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No updates yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm">
              {isOwner
                ? "Share news and announcements with your community by posting your first update."
                : "Check back later for updates from the guild."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Updates List */}
      {!isLoading && !error && updates && updates.length > 0 && (
        <div className="space-y-4">
          {updates.map((update) => (
            <Card key={update.id}>
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Avatar className="h-10 w-10">
                    <AvatarImage src={(update as any).user?.image} alt={(update as any).user?.name} />
                    <AvatarFallback>{getInitials((update as any).user?.name)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <h3 className="font-semibold">{update.title}</h3>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <span>{(update as any).user?.name ?? "Unknown User"}</span>
                          <span>•</span>
                          <span>{formatDate(update.createdAt)}</span>
                        </div>
                      </div>
                      {isOwner && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(update.id)}
                          disabled={deleteMutation.isPending}
                          className="h-8 w-8 p-0"
                        >
                          {deleteMutation.isPending ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4 text-muted-foreground hover:text-destructive" />
                          )}
                        </Button>
                      )}
                    </div>
                    <p className="text-sm text-foreground whitespace-pre-wrap">
                      {update.content}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
