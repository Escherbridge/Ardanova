"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Star, MessageSquare, Plus } from "lucide-react";

interface ReviewsTabProps {
  shopId: string;
}

// TODO: API Integration Point - Replace with actual Review entity from backend
// Placeholder review data structure for UI demonstration
interface Review {
  id: string;
  userId: string;
  userName: string;
  userImage?: string;
  rating: number;
  comment: string;
  date: string;
}

// Placeholder reviews for demonstration
const PLACEHOLDER_REVIEWS: Review[] = [
  {
    id: "1",
    userId: "user1",
    userName: "Sarah Johnson",
    rating: 5,
    comment: "Absolutely love the quality of products from this shop! The craftsmanship is outstanding and customer service is excellent.",
    date: "2026-01-20",
  },
  {
    id: "2",
    userId: "user2",
    userName: "Mike Chen",
    rating: 4,
    comment: "Great products and fast shipping. Only minor issue was with packaging, but the items arrived in perfect condition.",
    date: "2026-01-15",
  },
  {
    id: "3",
    userId: "user3",
    userName: "Emma Williams",
    rating: 5,
    comment: "This shop is amazing! Every product I've purchased has exceeded my expectations. Highly recommend!",
    date: "2026-01-10",
  },
];

export default function ReviewsTab({ shopId }: ReviewsTabProps) {
  const [showReviewForm, setShowReviewForm] = useState(false);

  // TODO: API Integration Point - Replace with actual API call
  // Example: const { data: reviews } = api.shop.getReviews.useQuery({ shopId });
  const reviews = PLACEHOLDER_REVIEWS;
  const hasReviews = reviews.length > 0;

  // Calculate average rating
  const averageRating = hasReviews
    ? reviews.reduce((acc, review) => acc + review.rating, 0) / reviews.length
    : 0;

  const getInitials = (name: string) => {
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    });
  };

  const renderStars = (rating: number, size: "sm" | "lg" = "sm") => {
    const sizeClass = size === "sm" ? "h-4 w-4" : "h-5 w-5";
    return (
      <div className="flex gap-0.5">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`${sizeClass} ${
              star <= rating
                ? "fill-warning text-warning"
                : "fill-muted text-muted"
            }`}
          />
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {/* Header with Average Rating */}
      <Card className="bg-card border-2 border-border">
        <CardContent className="pt-6">
          <div className="flex items-center justify-between">
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <MessageSquare className="h-5 w-5 text-muted-foreground" />
                <h2 className="text-xl font-semibold">Customer Reviews</h2>
              </div>
              {hasReviews && (
                <div className="flex items-center gap-3">
                  {renderStars(Math.round(averageRating), "lg")}
                  <span className="text-2xl font-bold text-foreground">
                    {averageRating.toFixed(1)}
                  </span>
                  <span className="text-sm text-muted-foreground">
                    ({reviews.length} {reviews.length === 1 ? "review" : "reviews"})
                  </span>
                </div>
              )}
            </div>
            <Button onClick={() => setShowReviewForm(!showReviewForm)} size="sm">
              <Plus className="mr-2 h-4 w-4" />
              Write Review
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Add Review Form Placeholder */}
      {showReviewForm && (
        <Card className="border-2 border-neon-purple/30">
          <CardHeader>
            <CardTitle>Write a Review</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-sm text-muted-foreground">
              Review submission form will be implemented with backend integration.
            </div>
            <Button
              variant="outline"
              onClick={() => setShowReviewForm(false)}
              className="mt-4"
            >
              Close
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {!hasReviews && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <MessageSquare className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No reviews yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm mb-4">
              Be the first to share your experience with this shop!
            </p>
            <Button onClick={() => setShowReviewForm(true)}>
              <Plus className="mr-2 h-4 w-4" />
              Write First Review
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Reviews List */}
      {hasReviews && (
        <div className="space-y-4">
          {reviews.map((review) => (
            <Card key={review.id} className="bg-card border-2 border-border">
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Avatar className="h-10 w-10">
                    <AvatarImage src={review.userImage} alt={review.userName} />
                    <AvatarFallback>{getInitials(review.userName)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <h3 className="font-semibold">{review.userName}</h3>
                        <div className="flex items-center gap-2 mt-1">
                          {renderStars(review.rating)}
                          <span className="text-xs text-muted-foreground">
                            {formatDate(review.date)}
                          </span>
                        </div>
                      </div>
                    </div>
                    <p className="text-sm text-foreground leading-relaxed">
                      {review.comment}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Placeholder Notice */}
      {hasReviews && (
        <Card className="border-dashed border-2 border-border">
          <CardContent className="py-4">
            <p className="text-sm text-muted-foreground text-center">
              Currently showing placeholder reviews. Real review data will be loaded from the backend.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
