"use client";

import Link from "next/link";
import {
  ArrowLeft,
  Briefcase,
  Calendar,
  DollarSign,
  Edit,
  MapPin,
  Shield,
  Users,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { formatEnumLabel } from "~/hooks/use-enum";

interface OpportunityData {
  id: string;
  title: string;
  slug: string;
  description: string;
  type: string;
  status: string;
  experienceLevel: string;
  requirements?: string;
  skills?: string;
  benefits?: string;
  location?: string;
  isRemote: boolean;
  compensation?: number;
  compensationDetails?: string;
  deadline?: string;
  maxApplications?: number;
  applicationsCount: number;
  coverImage?: string;
  createdAt: string;
  updatedAt: string;
  closedAt?: string;
  posterId: string;
  guildId?: string;
  projectId?: string;
  taskId?: string;
  projectRole?: string;
  poster?: { id: string; name?: string; image?: string };
}

interface OpportunityDetailViewProps {
  opportunity: OpportunityData;
  isOwner: boolean;
}

const statusColors: Record<string, string> = {
  OPEN: "bg-neon-green/20 text-neon-green border-neon-green/30",
  CLOSED: "bg-muted text-muted-foreground border-border",
  FILLED: "bg-neon-pink/20 text-neon-pink border-neon-pink/30",
  DRAFT: "bg-yellow-500/20 text-yellow-500 border-yellow-500/30",
};

const roleLabels: Record<string, string> = {
  FOUNDER: "Founder",
  LEADER: "Leader",
  CORE_CONTRIBUTOR: "Core Contributor",
  CONTRIBUTOR: "Contributor",
  OBSERVER: "Observer",
};

export function OpportunityDetailView({ opportunity, isOwner }: OpportunityDetailViewProps) {
  const skills = opportunity.skills
    ? opportunity.skills.split(",").map((s) => s.trim()).filter(Boolean)
    : [];

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-3xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/opportunities">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Opportunities
            </Link>
          </Button>

          <div className="flex items-start justify-between gap-4">
            <div>
              <div className="flex items-center gap-3 mb-2">
                <div className="w-10 h-10 bg-neon-pink/20 rounded-lg flex items-center justify-center">
                  <Briefcase className="h-5 w-5 text-neon-pink" />
                </div>
                <Badge
                  variant="outline"
                  className={statusColors[opportunity.status] ?? ""}
                >
                  {formatEnumLabel(opportunity.status)}
                </Badge>
              </div>
              <h1 className="text-3xl font-bold">{opportunity.title}</h1>
              {opportunity.poster?.name && (
                <p className="text-muted-foreground mt-1">
                  Posted by {opportunity.poster.name}
                </p>
              )}
            </div>

            {isOwner && (
              <Button asChild variant="outline">
                <Link href={`/opportunities/${opportunity.slug}/edit`}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit
                </Link>
              </Button>
            )}
          </div>
        </div>

        <div className="space-y-6">
          {/* Quick Info Bar */}
          <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-1.5">
              <Briefcase className="h-4 w-4" />
              {formatEnumLabel(opportunity.type)}
            </span>
            <span className="flex items-center gap-1.5">
              <Shield className="h-4 w-4" />
              {formatEnumLabel(opportunity.experienceLevel)}
            </span>
            {opportunity.location && (
              <span className="flex items-center gap-1.5">
                <MapPin className="h-4 w-4" />
                {opportunity.location}
              </span>
            )}
            {opportunity.isRemote && (
              <Badge variant="secondary" className="text-xs">Remote</Badge>
            )}
            {opportunity.projectRole && (
              <span className="flex items-center gap-1.5">
                <Users className="h-4 w-4" />
                {roleLabels[opportunity.projectRole] ?? opportunity.projectRole}
              </span>
            )}
          </div>

          {/* Description */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Description</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="whitespace-pre-wrap text-sm leading-relaxed">
                {opportunity.description}
              </p>
            </CardContent>
          </Card>

          {/* Skills */}
          {skills.length > 0 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">Required Skills</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2">
                  {skills.map((skill) => (
                    <Badge key={skill} variant="secondary">
                      {skill}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Compensation */}
          {(opportunity.compensationDetails || opportunity.compensation) && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg flex items-center gap-2">
                  <DollarSign className="h-5 w-5 text-neon-green" />
                  Compensation
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 gap-4">
                  {opportunity.compensationDetails && (
                    <div>
                      <p className="text-sm text-muted-foreground mb-1">Model</p>
                      <p className="font-medium">
                        {formatEnumLabel(opportunity.compensationDetails)}
                      </p>
                    </div>
                  )}
                  {opportunity.compensation != null && (
                    <div>
                      <p className="text-sm text-muted-foreground mb-1">Amount</p>
                      <p className="font-medium">{opportunity.compensation}</p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Deadline & Stats */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <Calendar className="h-5 w-5" />
                Details
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4 text-sm">
                {opportunity.deadline && (
                  <div>
                    <p className="text-muted-foreground mb-1">Deadline</p>
                    <p className="font-medium">
                      {new Date(opportunity.deadline).toLocaleDateString()}
                    </p>
                  </div>
                )}
                <div>
                  <p className="text-muted-foreground mb-1">Applications</p>
                  <p className="font-medium">
                    {opportunity.applicationsCount}
                    {opportunity.maxApplications
                      ? ` / ${opportunity.maxApplications}`
                      : ""}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground mb-1">Posted</p>
                  <p className="font-medium">
                    {new Date(opportunity.createdAt).toLocaleDateString()}
                  </p>
                </div>
                {opportunity.closedAt && (
                  <div>
                    <p className="text-muted-foreground mb-1">Closed</p>
                    <p className="font-medium">
                      {new Date(opportunity.closedAt).toLocaleDateString()}
                    </p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
