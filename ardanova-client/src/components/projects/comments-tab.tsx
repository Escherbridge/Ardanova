"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { api, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  AlertCircle,
  MessageCircle,
  RefreshCw,
  Send,
  Trash2,
  Loader2,
} from "lucide-react";

interface CommentsTabProps {
  projectId: string;
}

type ProjectComment = RouterOutputs["comment"]["getByTarget"][number];

const projectCommentTarget = (projectId: string) => ({
  targetType: "PROJECT" as const,
  targetId: projectId,
});

export default function CommentsTab({ projectId }: CommentsTabProps) {
  const { data: session } = useSession();
  const currentUserId = session?.user?.id;
  const [newComment, setNewComment] = useState("");
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [replyContent, setReplyContent] = useState("");

  const utils = api.useUtils();

  const commentTarget = projectCommentTarget(projectId);
  const {
    data: comments,
    error: commentsError,
    isLoading,
    refetch: retryComments,
  } = api.comment.getByTarget.useQuery(commentTarget);

  const addComment = api.comment.add.useMutation({
    onMutate: async (newCommentData) => {
      await utils.comment.getByTarget.cancel(commentTarget);
      const previous = utils.comment.getByTarget.getData(commentTarget);

      if (!currentUserId) return { previous };

      utils.comment.getByTarget.setData(commentTarget, (old) => [
        ...(old ?? []),
        {
          id: "temp-" + Date.now(),
          ...newCommentData,
          projectId,
          userId: currentUserId,
          parentId: newCommentData.parentId ?? null,
          targetType: "PROJECT",
          targetId: projectId,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          author: {
            id: currentUserId,
            name: session?.user?.name ?? "You",
            image: session?.user?.image ?? null,
          },
        },
      ]);

      return { previous };
    },
    onError: (err, _vars, context) => {
      if (context?.previous) {
        utils.comment.getByTarget.setData(commentTarget, context.previous);
      }
      toast.error(`Comment was not posted: ${err.message}`);
    },
    onSettled: () => {
      void utils.comment.getByTarget.invalidate(commentTarget);
    },
  });

  const deleteComment = api.comment.delete.useMutation({
    onSuccess: () => {
      void utils.comment.getByTarget.invalidate(commentTarget);
    },
    onError: (err) => {
      toast.error(`Comment was not deleted: ${err.message}`);
    },
  });

  const handleAddComment = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;
    if (!currentUserId) {
      toast.error("Sign in to join the conversation");
      return;
    }

    addComment.mutate({
      projectId,
      targetType: "PROJECT",
      targetId: projectId,
      content: newComment,
    });

    setNewComment("");
  };

  const handleAddReply = (parentId: string) => {
    if (!replyContent.trim()) return;
    if (!currentUserId) {
      toast.error("Sign in to reply");
      return;
    }

    addComment.mutate({
      projectId,
      targetType: "PROJECT",
      targetId: projectId,
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
  const topLevelComments: ProjectComment[] =
    comments?.filter((comment) => !comment.parentId) ?? [];
  const getReplies = (commentId: string): ProjectComment[] =>
    comments?.filter((comment) => comment.parentId === commentId) ?? [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (commentsError) {
    return (
      <div className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
          <div>
            <p className="text-destructive font-mono text-sm font-bold">
              COMMENTS COULD NOT BE LOADED
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              {commentsError.message}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          className="min-h-11"
          onClick={() => void retryComments()}
        >
          <RefreshCw className="mr-2 size-4" />
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-2">
        <MessageCircle className="text-muted-foreground h-5 w-5" />
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
                className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring min-h-24 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:outline-none"
                placeholder="Share your thoughts..."
              />
            </div>
            <div className="flex justify-end">
              <Button
                type="submit"
                className="min-h-11"
                disabled={
                  !newComment.trim() || !currentUserId || addComment.isPending
                }
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
              <div className="bg-destructive/10 border-destructive/30 text-destructive rounded border p-3 text-sm">
                Error: {addComment.error.message}
              </div>
            )}
          </form>
        </CardContent>
      </Card>

      {/* Comments List */}
      {topLevelComments.length === 0 ? (
        <div className="rounded-lg border-2 border-dashed py-12 text-center">
          <MessageCircle className="text-muted-foreground/50 mx-auto h-12 w-12" />
          <p className="text-muted-foreground mt-4 text-lg font-medium">
            No comments yet
          </p>
          <p className="text-muted-foreground mt-1 text-sm">
            Be the first to share your thoughts on this project
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {topLevelComments.map((comment) => (
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
                          <p className="text-muted-foreground text-xs">
                            {new Date(comment.createdAt).toLocaleDateString(
                              "en-US",
                              {
                                month: "short",
                                day: "numeric",
                                year: "numeric",
                                hour: "numeric",
                                minute: "2-digit",
                              },
                            )}
                          </p>
                        </div>
                        {comment.userId === currentUserId && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-destructive hover:text-destructive min-h-11 min-w-11"
                            aria-label="Delete comment"
                            onClick={() => handleDeleteComment(comment.id)}
                            disabled={deleteComment.isPending}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>

                      <p className="mt-2 text-sm">{comment.content}</p>

                      <div className="mt-3 flex items-center gap-4">
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-muted-foreground hover:text-foreground min-h-11 px-2 text-sm"
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
                              className="min-h-11"
                              onClick={() => {
                                setReplyingTo(null);
                                setReplyContent("");
                              }}
                            >
                              Cancel
                            </Button>
                            <Button
                              size="sm"
                              className="min-h-11"
                              onClick={() => handleAddReply(comment.id)}
                              disabled={
                                !replyContent.trim() ||
                                !currentUserId ||
                                addComment.isPending
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
                <div className="mt-3 ml-4 space-y-3 sm:ml-12">
                  {getReplies(comment.id).map((reply) => (
                    <Card key={reply.id} className="border-primary border-l-2">
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
                                  {new Date(reply.createdAt).toLocaleDateString(
                                    "en-US",
                                    {
                                      month: "short",
                                      day: "numeric",
                                      year: "numeric",
                                      hour: "numeric",
                                      minute: "2-digit",
                                    },
                                  )}
                                </p>
                              </div>
                              {reply.userId === currentUserId && (
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  className="text-destructive hover:text-destructive min-h-11 min-w-11"
                                  aria-label="Delete reply"
                                  onClick={() => handleDeleteComment(reply.id)}
                                  disabled={deleteComment.isPending}
                                >
                                  <Trash2 className="h-3 w-3" />
                                </Button>
                              )}
                            </div>

                            <p className="mt-2 text-sm">{reply.content}</p>
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
