"use client";

import Link from "next/link";
import { useSession } from "next-auth/react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Calendar,
  User,
  Eye,
  Users,
  MessageCircle,
  Heart,
  Edit,
  Trash2,
  Globe,
  Clock
} from "lucide-react";
import { ProjectStatus, ProjectCategory } from "@prisma/client";
import type { RouterOutputs } from "~/trpc/react";

type Project = (RouterOutputs["project"]["getAll"]["items"][0] | RouterOutputs["project"]["getMyProjects"]["items"][0]) & {
  _count?: {
    comments: number;
  };
};

interface ProjectCardProps {
  project: Project;
  showActions?: boolean;
  onEdit?: (project: Project) => void;
  onDelete?: (project: Project) => void;
  onPublish?: (project: Project) => void;
}

export function ProjectCard({
  project,
  showActions = false,
  onEdit,
  onDelete,
  onPublish
}: ProjectCardProps) {
  const { data: session } = useSession();
  const isOwner = session?.user?.id === project.createdById;

  const formatDate = (date: Date | string) => {
    return new Date(date).toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    });
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

  const getCategoryIcon = (category: ProjectCategory) => {
    switch (category) {
      case ProjectCategory.TECHNOLOGY:
        return "💻";
      case ProjectCategory.HEALTHCARE:
        return "🏥";
      case ProjectCategory.EDUCATION:
        return "📚";
      case ProjectCategory.ENVIRONMENT:
        return "🌱";
      case ProjectCategory.SOCIAL_IMPACT:
        return "🤝";
      case ProjectCategory.BUSINESS:
        return "💼";
      case ProjectCategory.ARTS_CULTURE:
        return "🎨";
      case ProjectCategory.AGRICULTURE:
        return "🌾";
      case ProjectCategory.FINANCE:
        return "💰";
      default:
        return "📋";
    }
  };

  const truncateText = (text: string, maxLength: number) => {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + "...";
  };

  return (
    <Card className="h-full hover:shadow-lg transition-shadow duration-200">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-3">
              <Badge
                variant="secondary"
                className={`text-xs ${getStatusColor(project.status as ProjectStatus)}`}
              >
                {project.status.replace("_", " ")}
              </Badge>
              <Badge variant="outline" className="text-xs flex items-center gap-1">
                {getCategoryIcon(project.category as ProjectCategory)}
                {project.category.replace("_", " ")}
              </Badge>
            </div>

            <CardTitle className="text-lg leading-tight mb-2">
              <Link
                href={`/projects/${project.slug}`}
                className="hover:text-blue-600 transition-colors"
              >
                {truncateText(project.title, 60)}
              </Link>
            </CardTitle>

            <CardDescription className="text-sm leading-relaxed">
              {truncateText(project.description, 120)}
            </CardDescription>
          </div>
        </div>
      </CardHeader>

      <CardContent className="pt-0">
        {/* Project Stats */}
        <div className="flex items-center justify-between text-sm text-gray-600 mb-4">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1">
              <Eye className="h-4 w-4" />
              <span>{project.viewsCount || 0}</span>
            </div>
            <div className="flex items-center gap-1">
              <Users className="h-4 w-4" />
              <span>{project.supportersCount || 0}</span>
            </div>
            <div className="flex items-center gap-1">
              <MessageCircle className="h-4 w-4" />
              <span>{project._count?.comments || 0}</span>
            </div>
          </div>

          <div className="flex items-center gap-1">
            <Calendar className="h-4 w-4" />
            <span>{formatDate(project.createdAt)}</span>
          </div>
        </div>

        {/* Creator Info */}
        <div className="flex items-center gap-2 mb-4">
          <div className="w-6 h-6 bg-gray-200 rounded-full flex items-center justify-center">
            <User className="h-3 w-3 text-gray-500" />
          </div>
          <span className="text-sm text-gray-600">
            Project Creator
          </span>
        </div>

        {/* Action Buttons */}
        {showActions && isOwner && (
          <div className="flex gap-2 pt-3 border-t">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onEdit?.(project)}
              className="flex-1"
            >
              <Edit className="h-3 w-3 mr-1" />
              Edit
            </Button>

            {project.status === ProjectStatus.DRAFT && (
              <Button
                variant="default"
                size="sm"
                onClick={() => onPublish?.(project)}
                className="flex-1"
              >
                <Globe className="h-3 w-3 mr-1" />
                Publish
              </Button>
            )}

            <Button
              variant="destructive"
              size="sm"
              onClick={() => onDelete?.(project)}
              className="flex-1"
            >
              <Trash2 className="h-3 w-3 mr-1" />
              Delete
            </Button>
          </div>
        )}

        {/* View Project Button */}
        {!showActions && (
          <div className="pt-3 border-t">
            <Button
              variant="outline"
              size="sm"
              className="w-full"
              asChild
            >
              <Link href={`/projects/${project.slug}`}>
                <Eye className="h-3 w-3 mr-1" />
                View Project
              </Link>
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
