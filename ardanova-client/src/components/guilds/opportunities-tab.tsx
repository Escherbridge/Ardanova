"use client";

import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Loader2, Briefcase, Plus, Users, DollarSign, MapPin, Calendar } from "lucide-react";
import Link from "next/link";

interface OpportunitiesTabProps {
  guildId: string;
  guildSlug: string;
  isOwner: boolean;
  userRole?: string;
}

// Type badge variants
const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
};

// Status badge variants
const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  DRAFT: "secondary",
  OPEN: "neon",
  IN_REVIEW: "warning",
  FILLED: "neon-green",
  CLOSED: "secondary",
  CANCELLED: "destructive",
};

export function OpportunitiesTab({ guildId, guildSlug, isOwner, userRole }: OpportunitiesTabProps) {
  // Check if user has permission to create opportunities
  const canCreateOpportunity = isOwner || userRole === "OWNER" || userRole === "ADMIN" || userRole === "RECRUITER";

  // Query for fetching all opportunities and filter by guildId
  const { data: opportunitiesResult, isLoading, error } = api.opportunity.getAll.useQuery({
    limit: 100,
  });

  // Filter opportunities by guildId
  const opportunities = (opportunitiesResult?.items || []).filter((opp) => opp.guildId === guildId);

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
              <Button
                asChild
                variant="default"
                size="sm"
              >
                <Link href={`/opportunities/create?entityType=guild&entityId=${guildId}&entitySlug=${guildSlug}`}>
                  <Plus className="size-4 mr-2" />
                  Post Opportunity
                </Link>
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {opportunities.length === 0 ? (
            <div className="text-center py-12">
              <Briefcase className="size-12 mx-auto text-muted-foreground mb-4" />
              <h3 className="text-lg font-semibold mb-2">No opportunities yet</h3>
              <p className="text-sm text-muted-foreground mb-4">
                {canCreateOpportunity
                  ? "Post your first opportunity to attract talented developers."
                  : "This guild hasn't posted any opportunities yet."}
              </p>
              {canCreateOpportunity && (
                <Button asChild variant="neon" size="sm">
                  <Link href={`/opportunities/create?entityType=guild&entityId=${guildId}&entitySlug=${guildSlug}`}>
                    <Plus className="size-4 mr-2" />
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
                  className="border border-border hover:bg-muted/50 transition-colors"
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
                            <h3 className="text-lg font-semibold group-hover:text-primary transition-colors">
                              {opportunity.title}
                            </h3>
                          </Link>
                          {opportunity.description && (
                            <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                              {opportunity.description}
                            </p>
                          )}
                        </div>
                        <div className="flex flex-col gap-2 items-end">
                          <Badge variant={statusVariants[opportunity.status] || "secondary"}>
                            {opportunity.status.replace("_", " ")}
                          </Badge>
                          <Badge variant={typeVariants[opportunity.type] || "secondary"}>
                            {opportunity.type}
                          </Badge>
                        </div>
                      </div>

                      {/* Skills */}
                      {opportunity.skills && (
                        <div className="flex flex-wrap gap-2">
                          {opportunity.skills.split(",").slice(0, 5).map((skill, i) => (
                            <Badge key={i} variant="outline" size="sm">
                              {skill.trim()}
                            </Badge>
                          ))}
                        </div>
                      )}

                      {/* Details */}
                      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                        {/* Applicants count */}
                        <div className="flex items-center gap-1.5">
                          <Users className="size-4" />
                          <span>{opportunity.applicationsCount || 0} applicants</span>
                        </div>

                        {/* Compensation */}
                        {opportunity.compensation && (
                          <div className="flex items-center gap-1.5">
                            <DollarSign className="size-4" />
                            <span>${Number(opportunity.compensation).toLocaleString()}</span>
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
                            <span>Deadline: {new Date(opportunity.deadline).toLocaleDateString()}</span>
                          </div>
                        )}
                      </div>

                      {/* Actions */}
                      <div className="flex items-center gap-2 pt-2">
                        <Button asChild variant="neon" size="sm">
                          <Link href={`/opportunities/${opportunity.slug || opportunity.id}`}>
                            View Details
                          </Link>
                        </Button>
                        {opportunity.status === "OPEN" && (
                          <Button asChild variant="outline" size="sm">
                            <Link href={`/opportunities/${opportunity.slug || opportunity.id}`}>
                              Apply Now
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
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-primary">
                  {opportunities.length}
                </div>
                <div className="text-sm text-muted-foreground">Total Opportunities</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-neon">
                  {opportunities.filter((o) => o.status === "OPEN").length}
                </div>
                <div className="text-sm text-muted-foreground">Open</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-neon-green">
                  {opportunities.filter((o) => o.status === "FILLED").length}
                </div>
                <div className="text-sm text-muted-foreground">Filled</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-foreground">
                  {opportunities.reduce((sum, o) => sum + (o.applicationsCount || 0), 0)}
                </div>
                <div className="text-sm text-muted-foreground">Total Applicants</div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
