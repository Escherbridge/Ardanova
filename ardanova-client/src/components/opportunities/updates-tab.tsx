"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Loader2, Bell, Plus, Trash2, Send, CheckCircle, XCircle } from "lucide-react";

interface UpdatesTabProps {
  opportunityId: string;
  isOwner: boolean;
}

export default function UpdatesTab({ opportunityId, isOwner }: UpdatesTabProps) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null);

  const utils = api.useUtils();

  // Query for fetching updates
  const { data: updates, isLoading, error } = api.opportunity.getUpdates.useQuery({ opportunityId });

  // Mutation for creating update with optimistic updates
  const createMutation = api.opportunity.createUpdate.useMutation({
    onMutate: async (newUpdate) => {
      if (!newUpdate) return;
      setFeedback(null);
      // Cancel any outgoing refetches to prevent overwriting optimistic update
      await utils.opportunity.getUpdates.cancel({ opportunityId });

      // Snapshot the previous value
      const previous = utils.opportunity.getUpdates.getData({ opportunityId });

      // Optimistically update to the new value
      utils.opportunity.getUpdates.setData({ opportunityId }, (old) => [
        {
          id: "temp-" + Date.now(),
          opportunityId: newUpdate.opportunityId,
          userId: "current-user",
          title: newUpdate.title,
          content: newUpdate.content,
          images: newUpdate.images,
          createdAt: new Date().toISOString(),
          user: { id: "current-user", name: "You", image: undefined },
        },
        ...(old ?? []),
      ]);

      return { previous };
    },
    onError: (err, newUpdate, context) => {
      // If the mutation fails, use the context returned from onMutate to roll back
      if (context?.previous) {
        utils.opportunity.getUpdates.setData({ opportunityId }, context.previous);
      }
      setFeedback({ type: "error", message: err.message || "Failed to create update" });
      setTimeout(() => setFeedback(null), 3000);
    },
    onSettled: () => {
      // Always refetch after error or success to ensure we have the correct data
      void utils.opportunity.getUpdates.invalidate({ opportunityId });
    },
    onSuccess: () => {
      // Reset form on success
      setTitle("");
      setContent("");
      setIsFormOpen(false);
      setFeedback({ type: "success", message: "Update posted successfully" });
      setTimeout(() => setFeedback(null), 2000);
    },
  });

  // Mutation for deleting update with optimistic updates
  const deleteMutation = api.opportunity.deleteUpdate.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      setFeedback(null);
      await utils.opportunity.getUpdates.cancel({ opportunityId });
      const previous = utils.opportunity.getUpdates.getData({ opportunityId });

      utils.opportunity.getUpdates.setData({ opportunityId }, (old) =>
        (old ?? []).filter((update) => update.id !== variables.updateId)
      );

      return { previous };
    },
    onError: (err, variables, context) => {
      if (context?.previous) {
        utils.opportunity.getUpdates.setData({ opportunityId }, context.previous);
      }
      setFeedback({ type: "error", message: err.message || "Failed to delete update" });
      setTimeout(() => setFeedback(null), 3000);
    },
    onSettled: () => {
      void utils.opportunity.getUpdates.invalidate({ opportunityId });
    },
    onSuccess: () => {
      setFeedback({ type: "success", message: "Update deleted successfully" });
      setTimeout(() => setFeedback(null), 2000);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !content.trim()) return;

    createMutation.mutate({
      opportunityId,
      title: title.trim(),
      content: content.trim(),
    });
  };

  const handleDelete = (updateId: string) => {
    if (confirm("Are you sure you want to delete this update?")) {
      deleteMutation.mutate({ updateId, opportunityId });
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
      {/* Feedback notification */}
      {feedback && (
        <div
          className={`flex items-center gap-2 p-3 rounded-lg text-sm ${
            feedback.type === "success"
              ? "bg-green-500/10 text-green-500 border border-green-500/30"
              : "bg-red-500/10 text-red-500 border border-red-500/30"
          }`}
        >
          {feedback.type === "success" ? (
            <CheckCircle className="size-4" />
          ) : (
            <XCircle className="size-4" />
          )}
          {feedback.message}
        </div>
      )}

      {/* Header with Create Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Bell className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">Opportunity Updates</h2>
        </div>
        {isOwner && !isFormOpen && (
          <Button onClick={() => setIsFormOpen(true)} size="sm">
            <Plus className="mr-2 h-4 w-4" />
            Post Update
          </Button>
        )}
      </div>

      {/* Create Update Form */}
      {isOwner && isFormOpen && (
        <Card className="bg-card border-2 border-border">
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
                  placeholder="Share progress, announcements, or news about this opportunity..."
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
        <Card className="bg-card border-2 border-border">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Bell className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No updates yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm">
              {isOwner
                ? "Share progress and announcements with applicants by posting updates."
                : "Check back later for updates from the opportunity owner."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Updates List */}
      {!isLoading && !error && updates && updates.length > 0 && (
        <div className="space-y-4">
          {updates.map((update) => (
            <Card key={update.id} className="bg-card border-2 border-border">
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Avatar className="h-10 w-10">
                    <AvatarImage src={update.user?.image} alt={update.user?.name} />
                    <AvatarFallback>{getInitials(update.user?.name)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <h3 className="font-semibold">{update.title}</h3>
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <span>{update.user?.name ?? "Unknown User"}</span>
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
                    {update.images && (
                      <div className="pt-2">
                        <p className="text-xs text-muted-foreground">
                          Images: {update.images}
                        </p>
                      </div>
                    )}
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
