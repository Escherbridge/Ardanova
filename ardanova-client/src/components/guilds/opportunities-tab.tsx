"use client";

import { useEffect } from "react";
import { api } from "~/trpc/react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import {
  Loader2,
  Briefcase,
  Plus,
  Users,
  DollarSign,
  MapPin,
  Calendar,
} from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";

interface OpportunitiesTabProps {
  guildId: string;
  guildSlug: string;
  isOwner: boolean;
  userRole?: string;
}

// Type badge variants
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

export function OpportunitiesTab({
  guildId,
  guildSlug,
  isOwner,
  userRole,
}: OpportunitiesTabProps) {
  // Check if user has permission to create opportunities
  const canCreateOpportunity =
    isOwner ||
    userRole === "OWNER" ||
    userRole === "ADMIN" ||
    userRole === "RECRUITER";

  // Query for fetching all opportunities and filter by guildId
  const {
    data: opportunitiesResult,
    isLoading,
    error,
  } = api.opportunity.getAll.useQuery({
    limit: 100,
  });

  // Filter opportunities by guildId
  const opportunities = (opportunitiesResult?.items || []).filter(
    (opp) => opp.guildId === guildId,
  );

  // Show toast notification on error
  useEffect(() => {
    if (error) {
      toast.error(error.message || "Failed to load opportunities");
    }
  }, [error]);

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
            Failed to load opportunities: {error.message}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Briefcase className="size-5" />
                Guild Opportunities
              </CardTitle>
              <CardDescription>
                Open positions and opportunities posted by this guild
              </CardDescription>
            </div>
            {canCreateOpportunity && (
              <Button asChild variant="default" size="sm">
                <Link
                  href={`/opportunities/create?entityType=guild&entityId=${guildId}&entitySlug=${guildSlug}`}
                >
                  <Plus className="mr-2 size-4" />
                  Post Opportunity
                </Link>
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {opportunities.length === 0 ? (
            <div className="py-12 text-center">
              <Briefcase className="text-muted-foreground mx-auto mb-4 size-12" />
              <h3 className="mb-2 text-lg font-semibold">
                No opportunities yet
              </h3>
              <p className="text-muted-foreground mb-4 text-sm">
                {canCreateOpportunity
                  ? "Post your first opportunity to attract talented developers."
                  : "This guild hasn't posted any opportunities yet."}
              </p>
              {canCreateOpportunity && (
                <Button asChild variant="neon" size="sm">
                  <Link
                    href={`/opportunities/create?entityType=guild&entityId=${guildId}&entitySlug=${guildSlug}`}
                  >
                    <Plus className="mr-2 size-4" />
                    Post Opportunity
                  </Link>
                </Button>
              )}
            </div>
          ) : (
            <div className="space-y-4">
              {opportunities.map((opportunity) => (
                <Card
                  key={opportunity.id}
                  className="border-border hover:bg-muted/50 border transition-colors"
                >
                  <CardContent className="pt-6">
                    <div className="space-y-4">
                      {/* Header */}
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1">
                          <Link
                            href={`/opportunities/${opportunity.slug || opportunity.id}`}
                            className="group"
                          >
                            <h3 className="group-hover:text-primary text-lg font-semibold transition-colors">
                              {opportunity.title}
                            </h3>
                          </Link>
                          {opportunity.description && (
                            <p className="text-muted-foreground mt-1 line-clamp-2 text-sm">
                              {opportunity.description}
                            </p>
                          )}
                        </div>
                        <div className="flex flex-col items-end gap-2">
                          <Badge
                            variant={
                              statusVariants[opportunity.status ?? ""] ||
                              "secondary"
                            }
                          >
                            {(opportunity.status ?? "Unknown").replace(
                              "_",
                              " ",
                            )}
                          </Badge>
                          <Badge
                            variant={
                              typeVariants[opportunity.type ?? ""] ||
                              "secondary"
                            }
                          >
                            {opportunity.type ?? "Unspecified"}
                          </Badge>
                        </div>
                      </div>

                      {/* Skills */}
                      {opportunity.skills && (
                        <div className="flex flex-wrap gap-2">
                          {opportunity.skills
                            .split(",")
                            .slice(0, 5)
                            .map((skill, i) => (
                              <Badge key={i} variant="outline" size="sm">
                                {skill.trim()}
                              </Badge>
                            ))}
                        </div>
                      )}

                      {/* Details */}
                      <div className="text-muted-foreground flex flex-wrap items-center gap-4 text-sm">
                        {/* Applicants count */}
                        <div className="flex items-center gap-1.5">
                          <Users className="size-4" />
                          <span>
                            {opportunity.applicationsCount || 0} applicants
                          </span>
                        </div>

                        {/* Compensation */}
                        {opportunity.compensation && (
                          <div className="flex items-center gap-1.5">
                            <DollarSign className="size-4" />
                            <span>
                              $
                              {Number(
                                opportunity.compensation,
                              ).toLocaleString()}
                            </span>
                          </div>
                        )}

                        {/* Location */}
                        {opportunity.location && (
                          <div className="flex items-center gap-1.5">
                            <MapPin className="size-4" />
                            <span>{opportunity.location}</span>
                          </div>
                        )}

                        {/* Remote badge */}
                        {opportunity.isRemote && (
                          <Badge variant="neon-green" size="sm">
                            Remote
                          </Badge>
                        )}

                        {/* Deadline */}
                        {opportunity.deadline && (
                          <div className="flex items-center gap-1.5">
                            <Calendar className="size-4" />
                            <span>
                              Deadline:{" "}
                              {new Date(
                                opportunity.deadline,
                              ).toLocaleDateString()}
                            </span>
                          </div>
                        )}
                      </div>

                      {/* Actions */}
                      <div className="flex items-center gap-2 pt-2">
                        <Button asChild variant="neon" size="sm">
                          <Link
                            href={`/opportunities/${opportunity.slug || opportunity.id}`}
                          >
                            View Details
                          </Link>
                        </Button>
                        {opportunity.status === "OPEN" && (
                          <Button asChild variant="outline" size="sm">
                            <Link
                              href={`/opportunities/${opportunity.slug || opportunity.id}`}
                            >
                              Review &amp; apply
                            </Link>
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Stats Card */}
      {opportunities.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Opportunity Statistics</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              <div className="border-border rounded border p-4 text-center">
                <div className="text-primary text-2xl font-bold">
                  {opportunities.length}
                </div>
                <div className="text-muted-foreground text-sm">
                  Total Opportunities
                </div>
              </div>
              <div className="border-border rounded border p-4 text-center">
                <div className="text-system text-2xl font-bold">
                  {opportunities.filter((o) => o.status === "OPEN").length}
                </div>
                <div className="text-muted-foreground text-sm">Open</div>
              </div>
              <div className="border-border rounded border p-4 text-center">
                <div className="text-success text-2xl font-bold">
                  {opportunities.filter((o) => o.status === "FILLED").length}
                </div>
                <div className="text-muted-foreground text-sm">Filled</div>
              </div>
              <div className="border-border rounded border p-4 text-center">
                <div className="text-foreground text-2xl font-bold">
                  {opportunities.reduce(
                    (sum, o) => sum + (o.applicationsCount || 0),
                    0,
                  )}
                </div>
                <div className="text-muted-foreground text-sm">
                  Total Applicants
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
