"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback } from "~/components/ui/avatar";
import { Loader2, Star, MessageSquare, Trash2, Send } from "lucide-react";

interface ReviewsTabProps {
  guildId: string;
  isOwner: boolean;
  currentUserId?: string;
}

export function ReviewsTab({
  guildId,
  isOwner,
  currentUserId,
}: ReviewsTabProps) {
  const [showReviewForm, setShowReviewForm] = useState(false);
  const [rating, setRating] = useState(5);
  const [content, setContent] = useState("");
  const [hoveredRating, setHoveredRating] = useState(0);

  const utils = api.useUtils();

  // Query for fetching reviews
  const {
    data: reviews,
    isLoading,
    error,
  } = api.guild.getReviews.useQuery({
    guildId,
  });

  // Mutation for creating review
  const createReviewMutation = api.guild.createReview.useMutation({
    onMutate: async (newReview) => {
      if (!newReview) return;
      await utils.guild.getReviews.cancel({ guildId });
      const previous = utils.guild.getReviews.getData({ guildId });

      utils.guild.getReviews.setData({ guildId }, (old) => [
        {
          id: "temp-" + Date.now(),
          guildId: newReview.guildId,
          reviewerId: currentUserId ?? "current-user",
          rating: newReview.rating,
          content: newReview.content ?? null,
          createdAt: new Date().toISOString(),
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
      if (!variables) return;
      await utils.guild.getReviews.cancel({ guildId });
      const previous = utils.guild.getReviews.getData({ guildId });

      utils.guild.getReviews.setData({ guildId }, (old) =>
        (old ?? []).filter((review) => review.id !== variables.reviewId),
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
      content: content.trim(),
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

  const averageRating =
    reviews && reviews.length > 0
      ? reviews.reduce((acc, review) => acc + review.rating, 0) / reviews.length
      : 0;

  const StarRating = ({
    rating: starRating,
    interactive = false,
    onChange,
  }: {
    rating: number;
    interactive?: boolean;
    onChange?: (rating: number) => void;
  }) => {
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
            className={
              interactive
                ? "cursor-pointer transition-transform hover:scale-110"
                : "cursor-default"
            }
          >
            <Star
              className={`size-5 ${
                star <= (interactive ? hoveredRating || starRating : starRating)
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
        <Loader2 className="text-muted-foreground size-6 animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <p className="text-destructive text-sm">
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
                <MessageSquare className="mr-2 size-4" />
                Write Review
              </Button>
            )}
          </div>
        </CardHeader>
        {reviews && reviews.length > 0 && (
          <CardContent>
            <div className="flex items-center gap-4">
              <div className="text-center">
                <div className="text-primary text-4xl font-bold">
                  {averageRating.toFixed(1)}
                </div>
                <StarRating rating={Math.round(averageRating)} />
                <div className="text-muted-foreground mt-1 text-sm">
                  Average Rating
                </div>
              </div>
              <div className="flex-1 space-y-2">
                {[5, 4, 3, 2, 1].map((stars) => {
                  const count = reviews.filter(
                    (review) => review.rating === stars,
                  ).length;
                  const percentage = (count / reviews.length) * 100;
                  return (
                    <div key={stars} className="flex items-center gap-2">
                      <span className="w-8 text-sm">{stars} ★</span>
                      <div className="bg-muted h-2 flex-1 overflow-hidden rounded-none">
                        <div
                          className="h-full bg-yellow-400"
                          style={{ width: `${percentage}%` }}
                        />
                      </div>
                      <span className="text-muted-foreground w-8 text-sm">
                        {count}
                      </span>
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
                <label className="mb-2 block text-sm font-medium">
                  Rating *
                </label>
                <StarRating rating={rating} interactive onChange={setRating} />
              </div>
              <div>
                <label
                  htmlFor="content"
                  className="mb-1 block text-sm font-medium"
                >
                  Review *
                </label>
                <textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="Share your thoughts about working with this guild..."
                  rows={4}
                  className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
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
            <MessageSquare className="text-muted-foreground mb-4 h-12 w-12" />
            <h3 className="mb-2 text-lg font-semibold">No reviews yet</h3>
            <p className="text-muted-foreground max-w-sm text-center text-sm">
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
          {reviews.map((review) => (
            <Card key={review.id}>
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Avatar className="h-10 w-10">
                    <AvatarFallback>
                      {review.reviewerId === currentUserId ? "YO" : "R"}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-semibold">
                            {review.reviewerId === currentUserId
                              ? "You"
                              : "Guild reviewer"}
                          </span>
                          <StarRating rating={review.rating} />
                        </div>
                        <div className="text-muted-foreground text-xs">
                          {formatDate(review.createdAt)}
                        </div>
                      </div>
                      {currentUserId === review.reviewerId && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteReview(review.id)}
                          disabled={deleteReviewMutation.isPending}
                          className="size-11 p-0"
                          aria-label={`Delete your ${review.rating}-star guild review`}
                        >
                          {deleteReviewMutation.isPending ? (
                            <Loader2
                              className="h-4 w-4 animate-spin"
                              aria-hidden="true"
                            />
                          ) : (
                            <Trash2
                              className="text-muted-foreground hover:text-destructive h-4 w-4"
                              aria-hidden="true"
                            />
                          )}
                        </Button>
                      )}
                    </div>
                    <p className="text-foreground text-sm whitespace-pre-wrap">
                      {review.content ?? "No comment provided"}
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
