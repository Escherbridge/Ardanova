"use client";

import { useEffect } from "react";
import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { toast } from "sonner";
import {
  Briefcase,
  Plus,
  DollarSign,
  MapPin,
  Users,
  Calendar,
  Clock,
} from "lucide-react";
import { api } from "~/trpc/react";

interface OpportunitiesTabProps {
  shopId: string;
  shopSlug: string;
  isOwner: boolean;
}

// Type badge variants matching opportunities page
const typeVariants: Record<
  string,
  "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"
> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
};

// Status badge variants
const statusVariants: Record<
  string,
  | "neon"
  | "neon-pink"
  | "neon-green"
  | "neon-purple"
  | "warning"
  | "secondary"
  | "destructive"
> = {
  DRAFT: "secondary",
  OPEN: "neon",
  IN_REVIEW: "warning",
  FILLED: "neon-green",
  CLOSED: "secondary",
  CANCELLED: "destructive",
};

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffSec < 60) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHour < 24) return `${diffHour}h ago`;
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

export default function OpportunitiesTab({
  shopId,
  shopSlug,
  isOwner,
}: OpportunitiesTabProps) {
  // Fetch all opportunities and filter by shop on client side
  const { data: opportunitiesResult, isLoading, error } =
    api.opportunity.getAll.useQuery({
      limit: 100,
    });

  // Filter opportunities for this shop
  // Note: This assumes opportunities have entityId/entityType fields
  // If the backend doesn't support filtering by shop yet, this will show all opportunities
  // Once backend supports getByShopId or filtering, update this query
  const allOpportunities = opportunitiesResult?.items || [];
  const opportunities = allOpportunities.filter(
    (opp) =>
      (opp as any).entityId === shopId ||
      (opp as any).shopId === shopId ||
      (opp as any).postedByShopId === shopId
  );

  // Show toast notification on error
  useEffect(() => {
    if (error) {
      toast.error(error.message || "Failed to load opportunities");
    }
  }, [error]);

  const hasOpportunities = opportunities.length > 0;

  return (
    <div className="space-y-6">
      {/* Header with Create Button */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Briefcase className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">Opportunities</h2>
        </div>
        {isOwner && (
          <Button asChild size="sm">
            <Link
              href={`/opportunities/create?entityType=shop&entityId=${shopId}&entitySlug=${shopSlug}`}
            >
              <Plus className="mr-2 h-4 w-4" />
              Post Opportunity
            </Link>
          </Button>
        )}
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !hasOpportunities && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Briefcase className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No opportunities yet</h3>
            <p className="text-sm text-muted-foreground text-center max-w-sm">
              {isOwner
                ? "Post your first opportunity to attract talent and grow your team."
                : "This shop hasn't posted any opportunities yet. Check back soon!"}
            </p>
            {isOwner && (
              <Button asChild className="mt-4">
                <Link
                  href={`/opportunities/create?entityType=shop&entityId=${shopId}&entitySlug=${shopSlug}`}
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Post Your First Opportunity
                </Link>
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {/* Opportunities List */}
      {!isLoading && hasOpportunities && (
        <div className="space-y-4">
          {opportunities.map((opportunity) => (
            <Card
              key={opportunity.id}
              className="bg-card border-2 border-border hover:border-neon-pink/50 transition-all group"
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <Link href={`/opportunities/${opportunity.id}`}>
                      <CardTitle className="text-lg hover:text-primary transition-colors line-clamp-1">
                        {opportunity.title}
                      </CardTitle>
                    </Link>
                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                      <Badge
                        variant={
                          typeVariants[opportunity.type] || "secondary"
                        }
                        size="sm"
                      >
                        {opportunity.type}
                      </Badge>
                      <Badge
                        variant={
                          statusVariants[opportunity.status] || "secondary"
                        }
                        size="sm"
                      >
                        {opportunity.status.replace("_", " ")}
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        Posted {formatRelativeTime(new Date(opportunity.createdAt))}
                      </span>
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm text-foreground/80 line-clamp-2">
                  {opportunity.description}
                </p>

                {/* Opportunity Details */}
                <div className="flex items-center gap-4 text-sm text-muted-foreground flex-wrap">
                  {opportunity.compensation && (
                    <div className="flex items-center gap-1">
                      <DollarSign className="size-4 text-neon-green" />
                      <span>
                        ${Number(opportunity.compensation).toLocaleString()}
                      </span>
                    </div>
                  )}
                  {opportunity.location && (
                    <div className="flex items-center gap-1">
                      <MapPin className="size-4" />
                      <span>{opportunity.location}</span>
                      {opportunity.isRemote && (
                        <Badge variant="neon-green" size="sm">
                          Remote
                        </Badge>
                      )}
                    </div>
                  )}
                  {opportunity.isRemote && !opportunity.location && (
                    <div className="flex items-center gap-1">
                      <MapPin className="size-4 text-neon-green" />
                      <span>Remote</span>
                    </div>
                  )}
                  {opportunity.deadline && (
                    <div className="flex items-center gap-1">
                      <Calendar className="size-4" />
                      <span>
                        Deadline:{" "}
                        {new Date(opportunity.deadline).toLocaleDateString()}
                      </span>
                    </div>
                  )}
                </div>

                {/* Stats & Actions */}
                <div className="flex items-center justify-between pt-2 border-t border-border">
                  <div className="flex items-center gap-1 text-sm text-muted-foreground">
                    <Users className="size-4" />
                    <span>
                      {opportunity.applicationsCount || 0} applicant
                      {opportunity.applicationsCount !== 1 ? "s" : ""}
                    </span>
                  </div>
                  <Button size="sm" variant="neon" asChild>
                    <Link href={`/opportunities/${opportunity.id}`}>
                      View Details
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Note about API integration */}
      {!isLoading && hasOpportunities && (
        <Card className="border-dashed border-2 border-border">
          <CardContent className="py-4">
            <p className="text-sm text-muted-foreground text-center">
              Note: Opportunity filtering by shop will be enhanced once backend
              supports entity-based filtering.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
