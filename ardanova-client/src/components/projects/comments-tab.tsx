"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  MessageCircle,
  Send,
  Trash2,
  Heart,
  Loader2,
} from "lucide-react";

interface CommentsTabProps {
  projectId: string;
}

export default function CommentsTab({ projectId }: CommentsTabProps) {
  const [newComment, setNewComment] = useState("");
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [replyContent, setReplyContent] = useState("");

  const utils = api.useUtils();

  const { data: comments, isLoading } = api.project.getComments.useQuery({
    projectId,
  });

  const addComment = api.project.addComment.useMutation({
    onMutate: async (newCommentData) => {
      await utils.project.getComments.cancel({ projectId });
      const previous = utils.project.getComments.getData({ projectId });

      utils.project.getComments.setData({ projectId }, (old) => [
        ...(old ?? []),
        {
          id: "temp-" + Date.now(),
          ...newCommentData,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          author: {
            id: "current-user",
            name: "You",
            email: null,
            image: null,
          },
          likes: 0,
          replies: [],
        } as any,
      ]);

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getComments.setData({ projectId }, context.previous);
      }
    },
    onSettled: () => {
      utils.project.getComments.invalidate({ projectId });
    },
  });

  const deleteComment = api.project.deleteComment.useMutation({
    onSuccess: () => {
      utils.project.getComments.invalidate({ projectId });
    },
  });

  const handleAddComment = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    addComment.mutate({
      projectId,
      content: newComment,
    });

    setNewComment("");
  };

  const handleAddReply = (parentId: string) => {
    if (!replyContent.trim()) return;

    addComment.mutate({
      projectId,
      content: replyContent,
      parentId,
    });

    setReplyContent("");
    setReplyingTo(null);
  };

  const handleDeleteComment = (commentId: string) => {
    if (confirm("Are you sure you want to delete this comment?")) {
      deleteComment.mutate({ projectId, commentId });
    }
  };

  // Organize comments into threads
  const topLevelComments = comments?.filter((c: any) => !c.parentId) ?? [];
  const getReplies = (commentId: string) =>
    comments?.filter((c: any) => c.parentId === commentId) ?? [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-2">
        <MessageCircle className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-xl font-semibold">
          Comments {comments && comments.length > 0 && `(${comments.length})`}
        </h2>
      </div>

      {/* Add Comment Form */}
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={handleAddComment} className="space-y-4">
            <div>
              <textarea
                value={newComment}
                onChange={(e) => setNewComment(e.target.value)}
                className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                placeholder="Share your thoughts..."
              />
            </div>
            <div className="flex justify-end">
              <Button
                type="submit"
                disabled={!newComment.trim() || addComment.isPending}
              >
                {addComment.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Send className="mr-2 h-4 w-4" />
                )}
                Post Comment
              </Button>
            </div>
            {addComment.error && (
              <div className="p-3 bg-destructive/10 border border-destructive/30 rounded text-sm text-destructive">
                Error: {addComment.error.message}
              </div>
            )}
          </form>
        </CardContent>
      </Card>

      {/* Comments List */}
      {topLevelComments.length === 0 ? (
        <div className="rounded-lg border-2 border-dashed py-12 text-center">
          <MessageCircle className="mx-auto h-12 w-12 text-muted-foreground/50" />
          <p className="mt-4 text-lg font-medium text-muted-foreground">
            No comments yet
          </p>
          <p className="mt-1 text-sm text-muted-foreground">
            Be the first to share your thoughts on this project
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {topLevelComments.map((comment: any) => (
            <div key={comment.id}>
              {/* Top-level Comment */}
              <Card>
                <CardContent className="pt-6">
                  <div className="flex gap-4">
                    <Avatar className="h-10 w-10">
                      <AvatarImage src={comment.author?.image ?? undefined} />
                      <AvatarFallback>
                        {comment.author?.name?.substring(0, 2).toUpperCase() ??
                          "U"}
                      </AvatarFallback>
                    </Avatar>

                    <div className="flex-1">
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="font-semibold">
                            {comment.author?.name ?? "Anonymous"}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {new Date(comment.createdAt).toLocaleDateString(
                              "en-US",
                              {
                                month: "short",
                                day: "numeric",
                                year: "numeric",
                                hour: "numeric",
                                minute: "2-digit",
                              }
                            )}
                          </p>
                        </div>
                        {comment.author?.id === "current-user" && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-destructive hover:text-destructive"
                            onClick={() => handleDeleteComment(comment.id)}
                            disabled={deleteComment.isPending}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>

                      <p className="mt-2 text-sm">{comment.content}</p>

                      <div className="mt-3 flex items-center gap-4">
                        <button className="flex items-center gap-1 text-sm text-muted-foreground transition-colors hover:text-foreground">
                          <Heart className="h-4 w-4" />
                          <span>{comment.likes ?? 0}</span>
                        </button>
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-auto p-0 text-sm text-muted-foreground hover:text-foreground"
                          onClick={() =>
                            setReplyingTo(
                              replyingTo === comment.id ? null : comment.id
                            )
                          }
                        >
                          Reply
                        </Button>
                      </div>

                      {/* Reply Form */}
                      {replyingTo === comment.id && (
                        <div className="mt-4 space-y-2">
                          <textarea
                            value={replyContent}
                            onChange={(e) => setReplyContent(e.target.value)}
                            className="min-h-20 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            placeholder="Write a reply..."
                          />
                          <div className="flex justify-end gap-2">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setReplyingTo(null);
                                setReplyContent("");
                              }}
                            >
                              Cancel
                            </Button>
                            <Button
                              size="sm"
                              onClick={() => handleAddReply(comment.id)}
                              disabled={
                                !replyContent.trim() || addComment.isPending
                              }
                            >
                              {addComment.isPending ? (
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                              ) : (
                                <Send className="mr-2 h-4 w-4" />
                              )}
                              Reply
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Replies */}
              {getReplies(comment.id).length > 0 && (
                <div className="ml-12 mt-3 space-y-3">
                  {getReplies(comment.id).map((reply: any) => (
                    <Card key={reply.id} className="border-l-2 border-primary">
                      <CardContent className="pt-4">
                        <div className="flex gap-4">
                          <Avatar className="h-8 w-8">
                            <AvatarImage
                              src={reply.author?.image ?? undefined}
                            />
                            <AvatarFallback>
                              {reply.author?.name
                                ?.substring(0, 2)
                                .toUpperCase() ?? "U"}
                            </AvatarFallback>
                          </Avatar>

                          <div className="flex-1">
                            <div className="flex items-center justify-between">
                              <div>
                                <p className="text-sm font-semibold">
                                  {reply.author?.name ?? "Anonymous"}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  {new Date(reply.createdAt).toLocaleDateString(
                                    "en-US",
                                    {
                                      month: "short",
                                      day: "numeric",
                                      year: "numeric",
                                      hour: "numeric",
                                      minute: "2-digit",
                                    }
                                  )}
                                </p>
                              </div>
                              {reply.author?.id === "current-user" && (
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  className="text-destructive hover:text-destructive"
                                  onClick={() => handleDeleteComment(reply.id)}
                                  disabled={deleteComment.isPending}
                                >
                                  <Trash2 className="h-3 w-3" />
                                </Button>
                              )}
                            </div>

                            <p className="mt-2 text-sm">{reply.content}</p>

                            <div className="mt-2 flex items-center gap-4">
                              <button className="flex items-center gap-1 text-xs text-muted-foreground transition-colors hover:text-foreground">
                                <Heart className="h-3 w-3" />
                                <span>{reply.likes ?? 0}</span>
                              </button>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
