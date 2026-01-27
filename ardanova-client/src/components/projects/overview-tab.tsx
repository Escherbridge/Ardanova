"use client";

import {
  Target,
  Lightbulb,
  Clock,
  Users,
  TrendingUp,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";

interface OverviewTabProps {
  project: {
    problemStatement?: string | null;
    solution?: string | null;
    expectedImpact?: string | null;
    targetAudience?: string | null;
    timeline?: string | null;
    tags?: string | null;
  };
}

export default function OverviewTab({ project }: OverviewTabProps) {
  return (
    <div className="space-y-6">
      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center border border-neon-pink/30">
              <Target className="size-4 text-neon-pink" />
            </div>
            Problem Statement
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {project.problemStatement || "No problem statement provided."}
          </p>
        </CardContent>
      </Card>

      <Card className="bg-card border-2 border-border">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center border border-neon-green/30">
              <Lightbulb className="size-4 text-neon-green" />
            </div>
            Proposed Solution
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-foreground whitespace-pre-wrap leading-relaxed">
            {project.solution || "No solution provided."}
          </p>
        </CardContent>
      </Card>

      {project.expectedImpact && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <TrendingUp className="size-4 text-primary" />
              </div>
              Expected Impact
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.expectedImpact}
            </p>
          </CardContent>
        </Card>
      )}

      {project.targetAudience && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-neon-purple/20 rounded-lg flex items-center justify-center border border-neon-purple/30">
                <Users className="size-4 text-neon-purple" />
              </div>
              Target Audience
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.targetAudience}
            </p>
          </CardContent>
        </Card>
      )}

      {project.timeline && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <div className="w-8 h-8 bg-warning/20 rounded-lg flex items-center justify-center border border-warning/30">
                <Clock className="size-4 text-warning" />
              </div>
              Timeline
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-foreground whitespace-pre-wrap leading-relaxed">
              {project.timeline}
            </p>
          </CardContent>
        </Card>
      )}

      {project.tags && (
        <Card className="bg-card border-2 border-border">
          <CardHeader>
            <CardTitle className="text-lg">Tags & Skills</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {project.tags.split(",").map((tag: string, i: number) => (
                <Badge key={i} variant="secondary">
                  {tag.trim()}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
