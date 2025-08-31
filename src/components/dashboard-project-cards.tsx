"use client";

import { useRouter } from "next/navigation";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { 
  Clock, 
  Globe, 
  Loader2, 
  CheckCircle, 
  AlertCircle,
  Plus
} from "lucide-react";
import { api } from "~/trpc/react";
import { ProjectStatus } from "@prisma/client";
import type { RouterOutputs } from "~/trpc/react";

type Project = RouterOutputs["project"]["getMyProjects"]["items"][0];

export function DashboardProjectCards() {
  const router = useRouter();

  // Fetch user's projects (limited to 3 for dashboard overview)
  const { data: projectsData, isLoading } = api.project.getMyProjects.useQuery({
    limit: 3,
  });

  const projects = projectsData?.items || [];

  const getStatusIcon = (status: ProjectStatus) => {
    switch (status) {
      case ProjectStatus.DRAFT:
        return <Clock className="h-3 w-3 text-gray-500" />;
      case ProjectStatus.PUBLISHED:
        return <Globe className="h-3 w-3 text-green-500" />;
      case ProjectStatus.IN_PROGRESS:
        return <Loader2 className="h-3 w-3 text-blue-500" />;
      case ProjectStatus.COMPLETED:
        return <CheckCircle className="h-3 w-3 text-purple-500" />;
      default:
        return <AlertCircle className="h-3 w-3 text-gray-500" />;
    }
  };

  const getStatusText = (status: ProjectStatus) => {
    switch (status) {
      case ProjectStatus.DRAFT:
        return "Draft";
      case ProjectStatus.PUBLISHED:
        return "Published";
      case ProjectStatus.IN_PROGRESS:
        return "In Progress";
      case ProjectStatus.COMPLETED:
        return "Completed";
      default:
        return "Unknown";
    }
  };

  const getStatusColor = (status: ProjectStatus) => {
    switch (status) {
      case ProjectStatus.DRAFT:
        return "bg-gray-100 text-gray-800";
      case ProjectStatus.PUBLISHED:
        return "bg-green-100 text-green-800";
      case ProjectStatus.IN_PROGRESS:
        return "bg-blue-100 text-blue-800";
      case ProjectStatus.COMPLETED:
        return "bg-purple-100 text-purple-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  if (isLoading) {
    return (
      <div className="text-center py-8">
        <Loader2 className="h-6 w-6 animate-spin mx-auto text-slate-400" />
        <p className="text-sm text-slate-500 mt-2">Loading projects...</p>
      </div>
    );
  }

  if (projects.length === 0) {
    return (
      <div className="text-center py-8 text-slate-500">
        <div className="w-12 h-12 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-3">
          <Plus className="h-6 w-6" />
        </div>
        <h3 className="font-medium mb-2">No projects yet</h3>
        <p className="text-sm mb-4">Create your first project to get started</p>
        <Button size="sm" asChild>
          <a href="/dashboard/create">Create Project</a>
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {projects.map((project) => (
        <Card 
          key={project.id} 
          className="hover:shadow-md transition-shadow cursor-pointer border-l-4 border-l-blue-500"
          onClick={() => router.push(`/projects/${project.slug}`)}
        >
          <CardContent className="p-4">
            <div className="flex items-start justify-between">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-2">
                  <Badge 
                    variant="secondary" 
                    className={`text-xs ${getStatusColor(project.status)}`}
                  >
                    <span className="flex items-center gap-1">
                      {getStatusIcon(project.status)}
                      {getStatusText(project.status)}
                    </span>
                  </Badge>
                </div>
                
                <h4 className="font-medium text-sm text-slate-900 mb-1 line-clamp-1">
                  {project.title}
                </h4>
                
                <p className="text-xs text-slate-600 line-clamp-2 mb-2">
                  {project.description}
                </p>
                
                <div className="flex items-center gap-4 text-xs text-slate-500">
                  <span>{project.category.replace("_", " ")}</span>
                  <span>•</span>
                  <span>{project._count?.supports || 0} supporters</span>
                  <span>•</span>
                  <span>{project._count?.comments || 0} comments</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
      
      {projects.length >= 3 && (
        <div className="text-center pt-2">
          <Button variant="outline" size="sm" asChild>
            <a href="/projects">View All Projects</a>
          </Button>
        </div>
      )}
    </div>
  );
}
