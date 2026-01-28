"use client";

import { api } from "~/trpc/react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Loader2, Briefcase, Calendar, DollarSign } from "lucide-react";
import Link from "next/link";

interface ProjectsTabProps {
  guildId: string;
}

type BidStatus = "PENDING" | "ACCEPTED" | "REJECTED" | "WITHDRAWN";

interface Bid {
  id: string;
  projectId: string;
  guildId: string;
  budget?: number | null;
  proposal: string;
  timeline?: string | null;
  status: BidStatus;
  submittedAt: string;
  project: {
    id: string;
    title: string;
    slug: string;
    description?: string | null;
    status?: string | null;
  };
}

const getBidStatusVariant = (status: BidStatus) => {
  switch (status) {
    case "ACCEPTED":
      return "success" as const;
    case "PENDING":
      return "warning" as const;
    case "REJECTED":
      return "destructive" as const;
    case "WITHDRAWN":
      return "outline" as const;
    default:
      return "default" as const;
  }
};

const getBidStatusLabel = (status: BidStatus) => {
  switch (status) {
    case "ACCEPTED":
      return "Accepted";
    case "PENDING":
      return "Pending Review";
    case "REJECTED":
      return "Rejected";
    case "WITHDRAWN":
      return "Withdrawn";
    default:
      return status;
  }
};

export function ProjectsTab({ guildId }: ProjectsTabProps) {
  // Query for fetching bids/projects
  const { data: bids, isLoading, error } = api.guild.getBids.useQuery({
    guildId,
  });

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
            Failed to load projects: {error.message}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Briefcase className="size-5" />
            Guild Projects & Bids
          </CardTitle>
          <CardDescription>
            Projects the guild has bid on or is currently working on
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!bids || bids.length === 0 ? (
            <div className="text-center py-12">
              <Briefcase className="size-12 mx-auto text-muted-foreground mb-4" />
              <h3 className="text-lg font-semibold mb-2">No projects yet</h3>
              <p className="text-sm text-muted-foreground">
                This guild hasn't bid on any projects yet.
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {bids.map((bid: Bid) => (
                <Card
                  key={bid.id}
                  className="border border-border hover:bg-muted/50 transition-colors"
                >
                  <CardContent className="pt-6">
                    <div className="space-y-4">
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1">
                          <Link
                            href={`/projects/${bid.project.slug}`}
                            className="group"
                          >
                            <h3 className="text-lg font-semibold group-hover:text-primary transition-colors">
                              {bid.project.title}
                            </h3>
                          </Link>
                          {bid.project.description && (
                            <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                              {bid.project.description}
                            </p>
                          )}
                        </div>
                        <Badge variant={getBidStatusVariant(bid.status)}>
                          {getBidStatusLabel(bid.status)}
                        </Badge>
                      </div>

                      <div className="border-t border-border pt-3">
                        <h4 className="text-sm font-medium mb-2">Guild Proposal</h4>
                        <p className="text-sm text-muted-foreground line-clamp-3">
                          {bid.proposal}
                        </p>
                      </div>

                      <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
                        {bid.budget && (
                          <div className="flex items-center gap-1.5">
                            <DollarSign className="size-4" />
                            <span>${bid.budget.toLocaleString()}</span>
                          </div>
                        )}
                        {bid.timeline && (
                          <div className="flex items-center gap-1.5">
                            <Calendar className="size-4" />
                            <span>{bid.timeline}</span>
                          </div>
                        )}
                        <div className="flex items-center gap-1.5">
                          <span>Submitted {new Date(bid.submittedAt).toLocaleDateString()}</span>
                        </div>
                      </div>

                      {bid.project.status && (
                        <div className="pt-2">
                          <Badge variant="outline">
                            Project Status: {bid.project.status}
                          </Badge>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Stats Card */}
      {bids && bids.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Project Statistics</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-primary">
                  {bids.length}
                </div>
                <div className="text-sm text-muted-foreground">Total Bids</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-success">
                  {bids.filter((b: Bid) => b.status === "ACCEPTED").length}
                </div>
                <div className="text-sm text-muted-foreground">Accepted</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-warning">
                  {bids.filter((b: Bid) => b.status === "PENDING").length}
                </div>
                <div className="text-sm text-muted-foreground">Pending</div>
              </div>
              <div className="text-center p-4 border border-border rounded">
                <div className="text-2xl font-bold text-muted-foreground">
                  {bids.filter((b: Bid) => b.status === "REJECTED").length}
                </div>
                <div className="text-sm text-muted-foreground">Rejected</div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
