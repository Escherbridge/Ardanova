"use client";

import {
  FileText,
  CheckCircle,
  Award,
  DollarSign,
  Calendar,
  Users,
  MapPin,
  Briefcase,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";

// Matches the Opportunity type from the API
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
  compensationType?: string;
  deadline?: string;
  maxApplications?: number;
  applicationsCount: number;
  coverImage?: string;
  createdAt: string;
  updatedAt: string;
  closedAt?: string;
  posterId: string;
  poster?: {
    id: string;
    name?: string;
    image?: string;
  };
  guildId?: string;
  projectId?: string;
  taskId?: string;
}

interface OverviewTabProps {
  opportunity: OpportunityData;
}

export default function OverviewTab({ opportunity }: OverviewTabProps) {
  return (
    <div className="space-y-6">
      <Card className="bg-card border-border border-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-none border">
              <FileText className="text-primary size-4" />
            </div>
            Description
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground leading-relaxed whitespace-pre-wrap">
            {opportunity.description}
          </p>
        </CardContent>
      </Card>

      {opportunity.requirements && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-success/20 border-success/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <CheckCircle className="text-success size-4" />
              </div>
              Requirements
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {opportunity.requirements}
            </p>
          </CardContent>
        </Card>
      )}

      {opportunity.skills && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-primary/20 border-primary/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <Award className="text-primary size-4" />
              </div>
              Skills Required
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {opportunity.skills.split(",").map((skill: string, i: number) => (
                <Badge key={i} variant="secondary">
                  {skill.trim()}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {opportunity.benefits && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-system/20 border-system/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <Award className="text-system size-4" />
              </div>
              Benefits
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground leading-relaxed whitespace-pre-wrap">
              {opportunity.benefits}
            </p>
          </CardContent>
        </Card>
      )}

      {(opportunity.compensation !== undefined ||
        opportunity.compensationDetails) && (
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <div className="bg-warning/20 border-warning/30 flex h-8 w-8 items-center justify-center rounded-none border">
                <DollarSign className="text-warning size-4" />
              </div>
              Compensation
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {opportunity.compensation !== undefined && (
              <p className="text-foreground text-2xl font-semibold">
                ${opportunity.compensation.toLocaleString()}
              </p>
            )}
            {opportunity.compensationDetails && (
              <p className="text-muted-foreground">
                {opportunity.compensationDetails}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <MapPin className="text-muted-foreground size-4" />
              Location
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground">
              {opportunity.isRemote
                ? "Remote"
                : opportunity.location || "Not specified"}
            </p>
          </CardContent>
        </Card>

        <Card className="bg-card border-border border-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Briefcase className="text-muted-foreground size-4" />
              Experience Level
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Badge variant="outline">{opportunity.experienceLevel}</Badge>
          </CardContent>
        </Card>

        {opportunity.deadline && (
          <Card className="bg-card border-border border-2">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Calendar className="text-muted-foreground size-4" />
                Deadline
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-foreground">
                {new Date(opportunity.deadline).toLocaleDateString()}
              </p>
            </CardContent>
          </Card>
        )}

        {opportunity.maxApplications && (
          <Card className="bg-card border-border border-2">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Users className="text-muted-foreground size-4" />
                Applications
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-foreground">
                {opportunity.applicationsCount} / {opportunity.maxApplications}
              </p>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
