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
  CheckCircle,
  XCircle,
} from "lucide-react";

interface CommentsTabProps {
  opportunityId: string;
}

export default function CommentsTab({ opportunityId }: CommentsTabProps) {
  const [newComment, setNewComment] = useState("");
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [replyContent, setReplyContent] = useState("");
  const [feedback, setFeedback] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const utils = api.useUtils();

  const {
    data: comments,
    isLoading,
    error,
  } = api.opportunity.getComments.useQuery({
    opportunityId,
  });

  const addComment = api.opportunity.addComment.useMutation({
    onMutate: async (newCommentData) => {
      if (!newCommentData) return;
      setFeedback(null);
      await utils.opportunity.getComments.cancel({ opportunityId });
      const previous = utils.opportunity.getComments.getData({ opportunityId });

      utils.opportunity.getComments.setData({ opportunityId }, (old) => [
        ...(old ?? []),
        {
          id: "temp-" + Date.now(),
          opportunityId: newCommentData.opportunityId,
          userId: "current-user",
          content: newCommentData.content,
          parentId: newCommentData.parentId,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          author: {
            id: "current-user",
            name: "You",
            email: undefined,
            image: undefined,
          },
        },
      ]);

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.opportunity.getComments.setData(
          { opportunityId },
          context.previous,
        );
      }
      setFeedback({
        type: "error",
        message: err.message || "Failed to post comment",
      });
      setTimeout(() => setFeedback(null), 3000);
    },
    onSettled: () => {
      void utils.opportunity.getComments.invalidate({ opportunityId });
    },
    onSuccess: () => {
      setFeedback({ type: "success", message: "Comment posted successfully" });
      setTimeout(() => setFeedback(null), 2000);
    },
  });

  const deleteComment = api.opportunity.deleteComment.useMutation({
    onMutate: async (variables) => {
      if (!variables) return;
      setFeedback(null);
      await utils.opportunity.getComments.cancel({ opportunityId });
      const previous = utils.opportunity.getComments.getData({ opportunityId });

      utils.opportunity.getComments.setData({ opportunityId }, (old) =>
        (old ?? []).filter((comment) => comment.id !== variables.commentId),
      );

      return { previous };
    },
    onError: (err, variables, context) => {
      if (context?.previous) {
        utils.opportunity.getComments.setData(
          { opportunityId },
          context.previous,
        );
      }
      setFeedback({
        type: "error",
        message: err.message || "Failed to delete comment",
      });
      setTimeout(() => setFeedback(null), 3000);
    },
    onSettled: () => {
      void utils.opportunity.getComments.invalidate({ opportunityId });
    },
    onSuccess: () => {
      setFeedback({ type: "success", message: "Comment deleted successfully" });
      setTimeout(() => setFeedback(null), 2000);
    },
  });

  const handleAddComment = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    addComment.mutate({
      opportunityId,
      content: newComment,
    });

    setNewComment("");
  };

  const handleAddReply = (parentId: string) => {
    if (!replyContent.trim()) return;

    addComment.mutate({
      opportunityId,
      content: replyContent,
      parentId,
    });

    setReplyContent("");
    setReplyingTo(null);
  };

  const handleDeleteComment = (commentId: string) => {
    if (confirm("Are you sure you want to delete this comment?")) {
      deleteComment.mutate({ commentId, opportunityId });
    }
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
        hour: "numeric",
        minute: "2-digit",
      });
    }
  };

  // Organize comments into threads
  const topLevelComments = comments?.filter((c) => !c.parentId) ?? [];
  const getReplies = (commentId: string) =>
    comments?.filter((c) => c.parentId === commentId) ?? [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <p className="text-destructive text-sm">
            Failed to load comments: {error.message}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Feedback notification */}
      {feedback && (
        <div
          className={`flex items-center gap-2 rounded-none p-3 text-sm ${
            feedback.type === "success"
              ? "border border-green-500/30 bg-green-500/10 text-green-500"
              : "border border-red-500/30 bg-red-500/10 text-red-500"
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

      {/* Header */}
      <div className="flex items-center gap-2">
        <MessageCircle className="text-muted-foreground h-5 w-5" />
        <h2 className="text-xl font-semibold">
          Comments {comments && comments.length > 0 && `(${comments.length})`}
        </h2>
      </div>

      {/* Add Comment Form */}
      <Card className="bg-card border-border border-2">
        <CardContent className="pt-6">
          <form onSubmit={handleAddComment} className="space-y-4">
            <div>
              <textarea
                value={newComment}
                onChange={(e) => setNewComment(e.target.value)}
                className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring min-h-24 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
                placeholder="Share your thoughts about this opportunity..."
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
          </form>
        </CardContent>
      </Card>

      {/* Comments List */}
      {topLevelComments.length === 0 ? (
        <Card className="bg-card border-border border-2">
          <CardContent className="flex flex-col items-center justify-center py-12">
            <MessageCircle className="text-muted-foreground mb-4 h-12 w-12" />
            <h3 className="mb-2 text-lg font-semibold">No comments yet</h3>
            <p className="text-muted-foreground max-w-sm text-center text-sm">
              Be the first to share your thoughts on this opportunity
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {topLevelComments.map((comment) => (
            <div key={comment.id}>
              {/* Top-level Comment */}
              <Card className="bg-card border-border border-2">
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
                          <p className="text-muted-foreground text-xs">
                            {formatDate(comment.createdAt)}
                          </p>
                        </div>
                        {comment.userId === "current-user" && (
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

                      <p className="mt-2 text-sm whitespace-pre-wrap">
                        {comment.content}
                      </p>

                      <div className="mt-3 flex items-center gap-4">
                        <button className="text-muted-foreground hover:text-foreground flex items-center gap-1 text-sm transition-colors">
                          <Heart className="h-4 w-4" />
                          <span>0</span>
                        </button>
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-muted-foreground hover:text-foreground h-auto p-0 text-sm"
                          onClick={() =>
                            setReplyingTo(
                              replyingTo === comment.id ? null : comment.id,
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
                            className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring min-h-20 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
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
                <div className="mt-3 ml-12 space-y-3">
                  {getReplies(comment.id).map((reply) => (
                    <Card
                      key={reply.id}
                      className="border-primary bg-card border-l-2"
                    >
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
                                <p className="text-muted-foreground text-xs">
                                  {formatDate(reply.createdAt)}
                                </p>
                              </div>
                              {reply.userId === "current-user" && (
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

                            <p className="mt-2 text-sm whitespace-pre-wrap">
                              {reply.content}
                            </p>

                            <div className="mt-2 flex items-center gap-4">
                              <button className="text-muted-foreground hover:text-foreground flex items-center gap-1 text-xs transition-colors">
                                <Heart className="h-3 w-3" />
                                <span>0</span>
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
