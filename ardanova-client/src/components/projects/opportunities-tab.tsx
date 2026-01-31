"use client";

import { useMemo, useEffect } from "react";
import Link from "next/link";
import { api } from "~/trpc/react";
import { toast } from "sonner";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  Briefcase,
  Plus,
  DollarSign,
  MapPin,
  Clock,
  Users,
  Loader2,
  Zap,
} from "lucide-react";

interface OpportunitiesTabProps {
  projectId: string;
  projectSlug: string;
  isOwner: boolean;
  userRole?: string;
}

type OpportunityStatus = "DRAFT" | "OPEN" | "IN_REVIEW" | "FILLED" | "CLOSED" | "CANCELLED";

const statusColors: Record<OpportunityStatus, string> = {
  DRAFT: "bg-gray-500",
  OPEN: "bg-blue-500",
  IN_REVIEW: "bg-yellow-500",
  FILLED: "bg-green-500",
  CLOSED: "bg-gray-500",
  CANCELLED: "bg-red-500",
};

const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
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

function formatTimeLeft(deadline: string): string {
  const now = new Date();
  const deadlineDate = new Date(deadline);
  const diffMs = deadlineDate.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return "Expired";
  if (diffDays === 0) return "Today";
  if (diffDays === 1) return "1 day left";
  if (diffDays < 7) return `${diffDays} days left`;
  const weeks = Math.ceil(diffDays / 7);
  return `${weeks} week${weeks > 1 ? 's' : ''} left`;
}

function formatCompensation(amount?: number, details?: string): string {
  if (!amount) return "Negotiable";
  const formatted = amount >= 1000 ? `$${(amount / 1000).toFixed(amount % 1000 === 0 ? 0 : 1)}k` : `$${amount}`;
  if (details === "hourly") return `${formatted}/hr`;
  return formatted;
}

function isUrgent(status: string, deadline?: string): boolean {
  if (status !== "OPEN" || !deadline) return false;
  const now = new Date();
  const deadlineDate = new Date(deadline);
  const diffMs = deadlineDate.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
  return diffDays >= 0 && diffDays < 3;
}

export default function OpportunitiesTab({
  projectId,
  projectSlug,
  isOwner,
  userRole
}: OpportunitiesTabProps) {
  const { data: opportunitiesResult, isLoading, error } = api.opportunity.getAll.useQuery({
    limit: 100,
  });

  // Filter opportunities for this project
  const opportunities = useMemo(() => {
    const allOpportunities = opportunitiesResult?.items || [];
    return allOpportunities.filter((opp) => opp.projectId === projectId);
  }, [opportunitiesResult, projectId]);

  // Show toast notification on error
  useEffect(() => {
    if (error) {
      toast.error(error.message || "Failed to load opportunities");
    }
  }, [error]);

  // Permission check: Can create opportunities?
  const canCreate = isOwner || userRole === "LEAD" || userRole === "ADMIN";

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
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Briefcase className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-xl font-semibold">
            Opportunities {opportunities.length > 0 && `(${opportunities.length})`}
          </h2>
        </div>
        {canCreate && (
          <Button asChild>
            <Link
              href={`/opportunities/create?entityType=project&entityId=${projectId}&entitySlug=${projectSlug}`}
            >
              <Plus className="mr-2 h-4 w-4" />
              Create Opportunity
            </Link>
          </Button>
        )}
      </div>

      {/* Opportunities List */}
      {opportunities.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Briefcase className="mx-auto h-12 w-12 text-muted-foreground/50 mb-4" />
            <h3 className="text-lg font-medium text-foreground mb-2">No opportunities yet</h3>
            <p className="text-muted-foreground mb-4">
              {canCreate
                ? "Create the first opportunity to start recruiting contributors."
                : "No opportunities have been posted for this project yet."}
            </p>
            {canCreate && (
              <Button asChild variant="outline">
                <Link
                  href={`/opportunities/create?entityType=project&entityId=${projectId}&entitySlug=${projectSlug}`}
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Create Opportunity
                </Link>
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {opportunities.map((opportunity) => {
            const skillsList = opportunity.skills ? opportunity.skills.split(',').map(s => s.trim()) : [];
            const isUrgentOpportunity = isUrgent(opportunity.status, opportunity.deadline);
            const typeVariant = typeVariants[opportunity.type] || "secondary";

            return (
              <Card key={opportunity.id}>
                <CardContent className="pt-6">
                  {/* Header */}
                  <div className="flex items-start justify-between gap-4 mb-3">
                    <div className="flex items-start gap-3">
                      <Avatar className="h-10 w-10">
                        <AvatarImage src={(opportunity as any).poster?.image} />
                        <AvatarFallback>
                          {(opportunity as any).poster?.name?.charAt(0).toUpperCase() || "U"}
                        </AvatarFallback>
                      </Avatar>

                      <div className="flex-1">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="font-medium">
                            {(opportunity as any).poster?.name || "Anonymous"}
                          </span>
                          <span className="text-sm text-muted-foreground">·</span>
                          <span className="text-sm text-muted-foreground">
                            {formatRelativeTime(new Date(opportunity.createdAt))}
                          </span>
                        </div>
                        <div className="flex flex-wrap gap-1.5 mt-1">
                          {isUrgentOpportunity && (
                            <Badge variant="destructive" size="sm">
                              <Zap className="mr-1 h-3 w-3" />
                              Urgent
                            </Badge>
                          )}
                          <Badge variant={typeVariant} size="sm">
                            {opportunity.type}
                          </Badge>
                          <Badge
                            variant="secondary"
                            size="sm"
                            className={`${statusColors[opportunity.status as OpportunityStatus]} text-white`}
                          >
                            {opportunity.status.replace("_", " ")}
                          </Badge>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Title */}
                  <Link
                    href={`/opportunities/${opportunity.slug || opportunity.id}`}
                    className="block mb-2"
                  >
                    <h3 className="text-lg font-semibold hover:text-primary transition-colors">
                      {opportunity.title}
                    </h3>
                  </Link>

                  {/* Description */}
                  <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                    {opportunity.description}
                  </p>

                  {/* Skills */}
                  {skillsList.length > 0 && (
                    <div className="flex flex-wrap gap-1.5 mb-3">
                      {skillsList.slice(0, 5).map((skill, idx) => (
                        <Badge key={idx} variant="outline" size="sm">
                          {skill}
                        </Badge>
                      ))}
                      {skillsList.length > 5 && (
                        <Badge variant="outline" size="sm">
                          +{skillsList.length - 5} more
                        </Badge>
                      )}
                    </div>
                  )}

                  {/* Metadata */}
                  <div className="flex flex-wrap gap-3 text-sm text-muted-foreground mb-4">
                    <div className="flex items-center gap-1">
                      <DollarSign className="h-4 w-4" />
                      <span>{formatCompensation(opportunity.compensation, opportunity.compensationDetails)}</span>
                    </div>

                    <div className="flex items-center gap-1">
                      <MapPin className="h-4 w-4" />
                      <span>{opportunity.isRemote ? "Remote" : opportunity.location || "On-site"}</span>
                    </div>

                    {opportunity.deadline && (
                      <div className="flex items-center gap-1">
                        <Clock className="h-4 w-4" />
                        <span>{formatTimeLeft(opportunity.deadline)}</span>
                      </div>
                    )}

                    <div className="flex items-center gap-1">
                      <Users className="h-4 w-4" />
                      <span>{opportunity.applicationsCount || 0} applicants</span>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex justify-end gap-2">
                    <Button variant="outline" size="sm" asChild>
                      <Link href={`/opportunities/${opportunity.slug || opportunity.id}`}>
                        View Details
                      </Link>
                    </Button>
                    {opportunity.status === "OPEN" && (
                      <Button variant="neon" size="sm" asChild>
                        <Link href={`/opportunities/${opportunity.slug || opportunity.id}`}>
                          Apply Now
                        </Link>
                      </Button>
                    )}
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
