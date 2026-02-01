"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Calendar,
  User,
  Target,
  Lightbulb,
  AlertCircle,
  CheckCircle2,
  Edit,
  Trash2,
  Eye,
  Users,
  MessageCircle,
  Heart,
  Loader2
} from "lucide-react";
import { ProjectStatus } from "@prisma/client";
import type { RouterOutputs } from "~/trpc/react";
import { api } from "~/trpc/react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "~/components/ui/dialog";

type Project = RouterOutputs["project"]["getById"] & {
  createdBy: {
    name: string | null;
    email: string | null;
    image: string | null;
  };
};

interface ProjectDetailProps {
  project: Project;
}

export function ProjectDetail({ project }: ProjectDetailProps) {
  const { data: session } = useSession();
  const router = useRouter();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  const isOwner = session?.user?.id === project.createdById;
  const isPublished = project.status === ProjectStatus.PUBLISHED;

  const publishMutation = api.project.publish.useMutation({
    onSuccess: () => {
      router.refresh();
    },
  });

  const deleteMutation = api.project.delete.useMutation({
    onSuccess: () => {
      router.push("/dashboard");
    },
  });

  const handlePublish = async (projectId: string) => {
    try {
      await publishMutation.mutateAsync({ id: projectId });
    } catch (error) {
      console.error("Failed to publish project:", error);
    }
  };

  const handleDelete = async (projectId: string) => {
    try {
      await deleteMutation.mutateAsync({ id: projectId });
      setShowDeleteDialog(false);
    } catch (error) {
      console.error("Failed to delete project:", error);
    }
  };

  const formatDate = (date: Date | string) => {
    return new Date(date).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
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

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case "TECHNOLOGY":
        return "💻";
      case "HEALTHCARE":
        return "🏥";
      case "EDUCATION":
        return "📚";
      case "ENVIRONMENT":
        return "🌱";
      case "SOCIAL_IMPACT":
        return "🤝";
      case "BUSINESS":
        return "💼";
      case "ARTS_CULTURE":
        return "🎨";
      case "AGRICULTURE":
        return "🌾";
      case "FINANCE":
        return "💰";
      default:
        return "📋";
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="py-4 sm:py-6 lg:py-8">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-6 sm:mb-8">
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
              <div className="flex-1">
                <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-3 sm:mb-4">
                  <Badge
                    variant="secondary"
                    className={`text-xs sm:text-sm ${getStatusColor(project.status as ProjectStatus)}`}
                  >
                    {project.status.replace("_", " ")}
                  </Badge>
                  {(project.categories ?? []).map((cat) => (
                    <Badge key={cat} variant="outline" className="text-xs sm:text-sm flex items-center gap-1">
                      {getCategoryIcon(cat)}
                      {cat.replace("_", " ")}
                    </Badge>
                  ))}
                  {project.featured && (
                    <Badge variant="default" className="text-xs sm:text-sm bg-yellow-100 text-yellow-800">
                      ⭐ Featured
                    </Badge>
                  )}
                </div>

                <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-gray-900 mb-3 sm:mb-4 leading-tight">
                  {project.title}
                </h1>

                <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-6 text-xs sm:text-sm text-gray-600">
                  <div className="flex items-center gap-2">
                    <User className="h-3 w-3 sm:h-4 sm:w-4" />
                    <span>By {project.createdBy.name}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Calendar className="h-3 w-3 sm:h-4 sm:w-4" />
                    <span>Created {formatDate(project.createdAt)}</span>
                  </div>
                  {project.publishedAt && (
                    <div className="flex items-center gap-2">
                      <Eye className="h-3 w-3 sm:h-4 sm:w-4" />
                      <span>Published {formatDate(project.publishedAt)}</span>
                    </div>
                  )}
                </div>
              </div>

              {/* Action Buttons */}
              {isOwner && (
                <div className="flex flex-col sm:flex-row gap-2 sm:gap-3">
                  {!isPublished && (
                    <Button
                      variant="default"
                      onClick={() => handlePublish(project.id)}
                      className="flex items-center justify-center gap-2 text-sm"
                      size="sm"
                    >
                      <Eye className="h-3 w-3 sm:h-4 sm:w-4" />
                      Publish
                    </Button>
                  )}
                  <Button
                    variant="outline"
                    onClick={() => router.push(`/dashboard/edit/${project.id}`)}
                    className="flex items-center justify-center gap-2 text-sm"
                    size="sm"
                  >
                    <Edit className="h-3 w-3 sm:h-4 sm:w-4" />
                    Edit
                  </Button>
                  <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
                    <DialogTrigger asChild>
                      <Button
                        variant="destructive"
                        className="flex items-center justify-center gap-2 text-sm"
                        size="sm"
                      >
                        <Trash2 className="h-3 w-3 sm:h-4 sm:w-4" />
                        Delete
                      </Button>
                    </DialogTrigger>
                    <DialogContent>
                      <DialogHeader>
                        <DialogTitle className="text-red-600">Delete Project</DialogTitle>
                        <DialogDescription>
                          Are you sure you want to delete "{project.title}"? This action cannot be undone.
                        </DialogDescription>
                      </DialogHeader>
                      <DialogFooter>
                        <Button
                          variant="outline"
                          onClick={() => setShowDeleteDialog(false)}
                          disabled={deleteMutation.isPending}
                        >
                          Cancel
                        </Button>
                        <Button
                          variant="destructive"
                          onClick={() => handleDelete(project.id)}
                          disabled={deleteMutation.isPending}
                        >
                          {deleteMutation.isPending ? (
                            <>
                              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                              Deleting...
                            </>
                          ) : (
                            "Delete"
                          )}
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 lg:gap-8">
            {/* Main Content */}
            <div className="lg:col-span-2 space-y-6 sm:space-y-8">
              {/* Problem Definition */}
              <Card>
                <CardHeader className="pb-3 sm:pb-6">
                  <CardTitle className="flex items-center gap-2 text-lg sm:text-xl">
                    <AlertCircle className="h-4 w-4 sm:h-5 sm:w-5 text-red-500" />
                    Problem Statement
                  </CardTitle>
                </CardHeader>
                <CardContent className="px-4 sm:px-6">
                  <p className="text-gray-700 leading-relaxed text-sm sm:text-base">
                    {project.problemStatement}
                  </p>
                  {project.targetAudience && (
                    <div className="mt-3 sm:mt-4 p-3 sm:p-4 bg-red-50 rounded-lg">
                      <h4 className="font-medium text-red-800 mb-2 text-sm sm:text-base">Target Audience</h4>
                      <p className="text-red-700 text-sm sm:text-base">{project.targetAudience}</p>
                    </div>
                  )}
                  {project.expectedImpact && (
                    <div className="mt-3 sm:mt-4 p-3 sm:p-4 bg-red-50 rounded-lg">
                      <h4 className="font-medium text-red-800 mb-2 text-sm sm:text-base">Expected Impact</h4>
                      <p className="text-red-700 text-sm sm:text-base">{project.expectedImpact}</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Solution */}
              <Card>
                <CardHeader className="pb-3 sm:pb-6">
                  <CardTitle className="flex items-center gap-2 text-lg sm:text-xl">
                    <Lightbulb className="h-4 w-4 sm:h-5 sm:w-5 text-yellow-500" />
                    Solution
                  </CardTitle>
                </CardHeader>
                <CardContent className="px-4 sm:px-6">
                  <p className="text-gray-700 leading-relaxed text-sm sm:text-base mb-4">
                    {project.solution}
                  </p>
                  {project.timeline && (
                    <div className="mt-3 sm:mt-4 p-3 sm:p-4 bg-yellow-50 rounded-lg">
                      <h4 className="font-medium text-yellow-800 mb-2 text-sm sm:text-base">Timeline</h4>
                      <p className="text-yellow-700 text-sm sm:text-base">{project.timeline}</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Description */}
              <Card>
                <CardHeader className="pb-3 sm:pb-6">
                  <CardTitle className="text-lg sm:text-xl">Project Description</CardTitle>
                </CardHeader>
                <CardContent className="px-4 sm:px-6">
                  <p className="text-gray-700 leading-relaxed text-sm sm:text-base">
                    {project.description}
                  </p>
                </CardContent>
              </Card>

              {/* Tags */}
              {project.tags && (
                <Card>
                  <CardHeader className="pb-3 sm:pb-6">
                    <CardTitle className="text-lg sm:text-xl">Tags & Skills</CardTitle>
                  </CardHeader>
                  <CardContent className="px-4 sm:px-6">
                    <div className="flex flex-wrap gap-2">
                      {project.tags.split(", ").map((tag, index) => (
                        <Badge key={index} variant="secondary">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Attachments */}
              {(project.images || project.videos || project.documents) && (
                <Card>
                  <CardHeader className="pb-3 sm:pb-6">
                    <CardTitle className="text-lg sm:text-xl">Project Attachments</CardTitle>
                  </CardHeader>
                  <CardContent className="px-4 sm:px-6 space-y-4">
                    {project.images && (
                      <div>
                        <h4 className="font-medium mb-2 text-sm sm:text-base">Images</h4>
                        <p className="text-xs sm:text-sm text-gray-600">{project.images}</p>
                      </div>
                    )}
                    {project.videos && (
                      <div>
                        <h4 className="font-medium mb-2 text-sm sm:text-base">Videos</h4>
                        <p className="text-xs sm:text-sm text-gray-600">{project.videos}</p>
                      </div>
                    )}
                    {project.documents && (
                      <div>
                        <h4 className="font-medium mb-2 text-sm sm:text-base">Documents</h4>
                        <p className="text-xs sm:text-sm text-gray-600">{project.documents}</p>
                      </div>
                    )}
                  </CardContent>
                </Card>
              )}
            </div>

            {/* Sidebar */}
            <div className="space-y-4 sm:space-y-6">
              {/* Project Stats */}
              <Card>
                <CardHeader className="pb-3 sm:pb-6">
                  <CardTitle className="text-lg sm:text-xl">Project Stats</CardTitle>
                </CardHeader>
                <CardContent className="px-4 sm:px-6 space-y-3 sm:space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-xs sm:text-sm text-gray-600">Views</span>
                    <span className="font-medium text-sm sm:text-base">{project.viewsCount}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-xs sm:text-sm text-gray-600">Supporters</span>
                    <span className="font-medium text-sm sm:text-base">{project.supportersCount}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-xs sm:text-sm text-gray-600">Votes</span>
                    <span className="font-medium text-sm sm:text-base">{project.votesCount}</span>
                  </div>
                  {project.currentFunding && (
                    <div className="flex items-center justify-between">
                      <span className="text-xs sm:text-sm text-gray-600">Current Funding</span>
                      <span className="font-medium text-sm sm:text-base">${project.currentFunding.toString()}</span>
                    </div>
                  )}
                  {project.fundingGoal && (
                    <div className="flex items-center justify-between">
                      <span className="text-xs sm:text-sm text-gray-600">Funding Goal</span>
                      <span className="font-medium text-sm sm:text-base">${project.fundingGoal.toString()}</span>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Creator Info */}
              <Card>
                <CardHeader className="pb-3 sm:pb-6">
                  <CardTitle className="text-lg sm:text-xl">Project Creator</CardTitle>
                </CardHeader>
                <CardContent className="px-4 sm:px-6">
                  <div className="flex items-center gap-3">
                    {project.createdBy.image ? (
                      <img
                        src={project.createdBy.image}
                        alt={project.createdBy.name || "Project creator"}
                        className="w-12 h-12 rounded-full"
                      />
                    ) : (
                      <div className="w-12 h-12 bg-gray-200 rounded-full flex items-center justify-center">
                        <User className="h-6 w-6 text-gray-500" />
                      </div>
                    )}
                    <div>
                      <h4 className="font-medium text-sm sm:text-base">{project.createdBy.name}</h4>
                      <p className="text-xs sm:text-sm text-gray-600">{project.createdBy.email}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Support Project */}
              {isPublished && (
                <Card>
                  <CardHeader className="pb-3 sm:pb-6">
                    <CardTitle className="text-lg sm:text-xl">Support This Project</CardTitle>
                    <CardDescription className="text-sm">
                      Help bring this project to life
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="px-4 sm:px-6">
                    <Button className="w-full" size="lg">
                      <Heart className="h-4 w-4 mr-2" />
                      Support Project
                    </Button>
                  </CardContent>
                </Card>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
