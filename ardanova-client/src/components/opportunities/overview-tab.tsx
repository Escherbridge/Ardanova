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
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
              <FileText className="size-4 text-neon-pink" />
            </div>
            Description
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {opportunity.description}
          </p>
        </CardContent>
      </Card>

      {opportunity.requirements && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
                <CheckCircle className="size-4 text-neon-green" />
              </div>
              Requirements
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {opportunity.requirements}
            </p>
          </CardContent>
        </Card>
      )}

      {opportunity.skills && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <Award className="size-4 text-primary" />
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
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
                <Award className="size-4 text-neon-purple" />
              </div>
              Benefits
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {opportunity.benefits}
            </p>
          </CardContent>
        </Card>
      )}

      {(opportunity.compensation !== undefined || opportunity.compensationDetails) && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
                <DollarSign className="size-4 text-warning" />
              </div>
              Compensation
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {opportunity.compensation !== undefined && (
              <p className="text-2xl font-semibold text-foreground">
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

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <MapPin className="size-4 text-muted-foreground" />
              Location
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground">
              {opportunity.isRemote ? "Remote" : opportunity.location || "Not specified"}
            </p>
          </CardContent>
        </Card>

        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Briefcase className="size-4 text-muted-foreground" />
              Experience Level
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Badge variant="outline">{opportunity.experienceLevel}</Badge>
          </CardContent>
        </Card>

        {opportunity.deadline && (
          <Card className="bg-card border-2 border-border">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <Calendar className="size-4 text-muted-foreground" />
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
          <Card className="bg-card border-2 border-border">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <Users className="size-4 text-muted-foreground" />
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
