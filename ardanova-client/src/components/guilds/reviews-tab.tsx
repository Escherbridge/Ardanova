"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Loader2, Star, MessageSquare, Trash2, Send } from "lucide-react";

interface ReviewsTabProps {
  guildId: string;
  isOwner: boolean;
  currentUserId?: string;
}

interface Review {
  id: string;
  guildId: string;
  userId: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  user?: {
    id: string;
    name?: string | null;
    email?: string | null;
    image?: string | null;
  };
}

export function ReviewsTab({ guildId, isOwner, currentUserId }: ReviewsTabProps) {
  const [showReviewForm, setShowReviewForm] = useState(false);
  const [rating, setRating] = useState(5);
  const [content, setContent] = useState("");
  const [hoveredRating, setHoveredRating] = useState(0);

  const utils = api.useUtils();

  // Query for fetching reviews
  const { data: reviews, isLoading, error } = api.guild.getReviews.useQuery({
    guildId,
  });

  // Mutation for creating review
  const createReviewMutation = api.guild.createReview.useMutation({
    onMutate: async (newReview) => {
      await utils.guild.getReviews.cancel({ guildId });
      const previous = utils.guild.getReviews.getData({ guildId });

      utils.guild.getReviews.setData({ guildId }, (old) => [
        {
          id: 'temp-' + Date.now(),
          guildId: newReview.guildId,
          userId: currentUserId ?? 'current-user',
          rating: newReview.rating,
          comment: newReview.comment ?? null,
          createdAt: new Date().toISOString(),
          user: { id: currentUserId ?? 'current-user', name: 'You', email: '', image: undefined },
        },
        ...(old ?? []),
      ]);

      return { previous };
    },
    onError: (err, newReview, context) => {
      if (context?.previous) {
        utils.guild.getReviews.setData({ guildId }, context.previous);
      }
      alert(err.message);
    },
    onSettled: () => {
      void utils.guild.getReviews.invalidate({ guildId });
    },
    onSuccess: () => {
      setShowReviewForm(false);
      setRating(5);
      setContent("");
    },
  });

  // Mutation for deleting review
  const deleteReviewMutation = api.guild.deleteReview.useMutation({
    onMutate: async (variables) => {
      await utils.guild.getReviews.cancel({ guildId });
      const previous = utils.guild.getReviews.getData({ guildId });

      utils.guild.getReviews.setData({ guildId }, (old) =>
        (old ?? []).filter((review) => review.id !== variables.reviewId)
      );

      return { previous };
    },
    onError: (err, variables, context) => {
      if (context?.previous) {
        utils.guild.getReviews.setData({ guildId }, context.previous);
      }
      alert(err.message);
    },
    onSettled: () => {
      void utils.guild.getReviews.invalidate({ guildId });
    },
  });

  const handleSubmitReview = (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;

    createReviewMutation.mutate({
      guildId,
      rating,
      comment: content.trim(),
    });
  };

  const handleDeleteReview = (reviewId: string) => {
    if (confirm("Are you sure you want to delete this review?")) {
      deleteReviewMutation.mutate({ guildId, reviewId });
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      month: "long",
      day: "numeric",
      year: "numeric",
    });
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

  const averageRating = reviews && reviews.length > 0
    ? reviews.reduce((acc: number, r: Review) => acc + r.rating, 0) / reviews.length
    : 0;

  const StarRating = ({ rating: starRating, interactive = false, onChange }: { rating: number; interactive?: boolean; onChange?: (rating: number) => void }) => {
    return (
      <div className="flex gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type={interactive ? "button" : undefined}
            disabled={!interactive}
            onClick={() => interactive && onChange?.(star)}
            onMouseEnter={() => interactive && setHoveredRating(star)}
            onMouseLeave={() => interactive && setHoveredRating(0)}
            className={interactive ? "cursor-pointer transition-transform hover:scale-110" : "cursor-default"}
          >
            <Star
              className={`size-5 ${
                star <= (interactive ? (hoveredRating || starRating) : starRating)
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-gray-300"
              }`}
            />
          </button>
        ))}
      </div>
    );
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="size-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <p className="text-sm text-destructive">
            Failed to load reviews: {error.message}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header with Stats */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <MessageSquare className="size-5" />
                Client Reviews
              </CardTitle>
              <CardDescription>
                {reviews && reviews.length > 0
                  ? `${reviews.length} ${reviews.length === 1 ? "review" : "reviews"}`
                  : "No reviews yet"}
              </CardDescription>
            </div>
            {!isOwner && (
              <Button
                onClick={() => setShowReviewForm(!showReviewForm)}
                variant="default"
                size="sm"
              >
                <MessageSquare className="size-4 mr-2" />
                Write Review
              </Button>
            )}
          </div>
        </CardHeader>
        {reviews && reviews.length > 0 && (
          <CardContent>
            <div className="flex items-center gap-4">
              <div className="text-center">
                <div className="text-4xl font-bold text-primary">
                  {averageRating.toFixed(1)}
                </div>
                <StarRating rating={Math.round(averageRating)} />
                <div className="text-sm text-muted-foreground mt-1">
                  Average Rating
                </div>
              </div>
              <div className="flex-1 space-y-2">
                {[5, 4, 3, 2, 1].map((stars) => {
                  const count = reviews.filter((r: Review) => r.rating === stars).length;
                  const percentage = (count / reviews.length) * 100;
                  return (
                    <div key={stars} className="flex items-center gap-2">
                      <span className="text-sm w-8">{stars} ★</span>
                      <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
                        <div
                          className="h-full bg-yellow-400"
                          style={{ width: `${percentage}%` }}
                        />
                      </div>
                      <span className="text-sm text-muted-foreground w-8">{count}</span>
                    </div>
                  );
                })}
              </div>
            </div>
          </CardContent>
        )}
      </Card>

      {/* Create Review Form */}
      {showReviewForm && !isOwner && (
        <Card>
          <CardHeader>
            <CardTitle>Write a Review</CardTitle>
            <CardDescription>
              Share your experience working with this guild
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmitReview} className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-2">
                  Rating *
                </label>
                <StarRating rating={rating} interactive onChange={setRating} />
              </div>
              <div>
                <label htmlFor="content" className="block text-sm font-medium mb-1">
                  Review *
                </label>
                <textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="Share your thoughts about working with this guild..."
                  rows={4}
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  required
                />
              </div>
              <div className="flex gap-2">
                <Button type="submit" disabled={createReviewMutation.isPending}>
                  {createReviewMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Submitting...
                    </>
                  ) : (
                    <>
                      <Send className="mr-2 h-4 w-4" />
                      Submit Review
                    </>
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setShowReviewForm(false);
                    setRating(5);
                    setContent("");
                  }}
                  disabled={createReviewMutation.isPending}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {(!reviews || reviews.length === 0) && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <MessageSquare className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No reviews yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm">
              {isOwner
                ? "Your guild doesn't have any reviews yet. Encourage clients to share their experiences."
                : "Be the first to review this guild and share your experience."}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Reviews List */}
      {reviews && reviews.length > 0 && (
        <div className="space-y-4">
          {reviews.map((review: Review) => (
            <Card key={review.id}>
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Avatar className="h-10 w-10">
                    <AvatarImage src={review.user?.image ?? undefined} alt={review.user?.name ?? "User"} />
                    <AvatarFallback>{getInitials(review.user?.name)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold">
                            {review.user?.name ?? "Anonymous"}
                          </span>
                          <StarRating rating={review.rating} />
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {formatDate(review.createdAt)}
                        </div>
                      </div>
                      {currentUserId === review.userId && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteReview(review.id)}
                          disabled={deleteReviewMutation.isPending}
                          className="h-8 w-8 p-0"
                        >
                          {deleteReviewMutation.isPending ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4 text-muted-foreground hover:text-destructive" />
                          )}
                        </Button>
                      )}
                    </div>
                    <p className="text-sm text-foreground whitespace-pre-wrap">
                      {review.comment ?? "No comment provided"}
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
