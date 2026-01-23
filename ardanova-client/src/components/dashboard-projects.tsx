"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { 
  Plus, 
  FolderOpen, 
  Clock, 
  CheckCircle, 
  Globe,
  AlertCircle,
  Loader2,
  Trash2
} from "lucide-react";
import { ProjectCard } from "~/components/project-card";
import { api } from "~/trpc/react";
import { ProjectStatus } from "@prisma/client";
import type { RouterOutputs } from "~/trpc/react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "~/components/ui/dialog";

type Project = RouterOutputs["project"]["getMyProjects"]["items"][0];

export function DashboardProjects() {
  const router = useRouter();
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [projectToDelete, setProjectToDelete] = useState<Project | null>(null);

  // Fetch user's projects
  const { data: projectsData, isLoading, refetch } = api.project.getMyProjects.useQuery({
    limit: 10,
  });

  // Publish project mutation
  const publishProject = api.project.publish.useMutation({
    onSuccess: () => {
      refetch();
    },
  });

  // Delete project mutation
  const deleteProject = api.project.delete.useMutation({
    onSuccess: () => {
      refetch();
      setShowDeleteDialog(false);
      setProjectToDelete(null);
    },
  });

  const projects = projectsData?.items || [];

  const handleEdit = (project: Project) => {
    router.push(`/dashboard/edit/${project.id}`);
  };

  const handleDelete = (project: Project) => {
    setProjectToDelete(project);
    setShowDeleteDialog(true);
  };

  const handlePublish = (project: Project) => {
    publishProject.mutate({ id: project.id });
  };

  const confirmDelete = () => {
    if (projectToDelete) {
      deleteProject.mutate({ id: projectToDelete.id });
    }
  };

  const getStatusIcon = (status: ProjectStatus) => {
    switch (status) {
      case ProjectStatus.DRAFT:
        return <Clock className="h-4 w-4 text-gray-500" />;
      case ProjectStatus.PUBLISHED:
        return <Globe className="h-4 w-4 text-green-500" />;
      case ProjectStatus.IN_PROGRESS:
        return <Loader2 className="h-4 w-4 text-blue-500" />;
      case ProjectStatus.COMPLETED:
        return <CheckCircle className="h-4 w-4 text-purple-500" />;
      default:
        return <AlertCircle className="h-4 w-4 text-gray-500" />;
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

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
        <span className="ml-2 text-gray-600">Loading your projects...</span>
      </div>
    );
  }

  if (projects.length === 0) {
    return (
      <Card className="text-center py-12">
        <CardContent>
          <div className="flex flex-col items-center">
            <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mb-4">
              <FolderOpen className="h-8 w-8 text-blue-600" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">No projects yet</h3>
            <p className="text-gray-600 mb-6 max-w-md">
              You haven't created any projects yet. Start by creating your first project to get community support and feedback.
            </p>
            <Button asChild>
              <a href="/dashboard/create">
                <Plus className="h-4 w-4 mr-2" />
                Create Your First Project
              </a>
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
                    {/* Project Stats */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 sm:gap-4">
                <Card>
                  <CardContent className="p-3 sm:p-4">
                    <div className="flex items-center gap-2 sm:gap-3">
                      <div className="p-1.5 sm:p-2 bg-gray-100 rounded-lg">
                        <FolderOpen className="h-4 w-4 sm:h-5 sm:w-5 text-gray-600" />
                      </div>
                      <div>
                        <p className="text-xs sm:text-sm text-gray-600">Total Projects</p>
                        <p className="text-lg sm:text-2xl font-bold text-gray-900">{projects.length}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Clock className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">Drafts</p>
                <p className="text-2xl font-bold text-blue-600">
                  {projects.filter(p => p.status === ProjectStatus.DRAFT).length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <Globe className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">Published</p>
                <p className="text-2xl font-bold text-green-600">
                  {projects.filter(p => p.status === ProjectStatus.PUBLISHED).length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <CheckCircle className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">Completed</p>
                <p className="text-2xl font-bold text-purple-600">
                  {projects.filter(p => p.status === ProjectStatus.COMPLETED).length}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

                    {/* Projects Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
        {projects.map((project) => (
          <ProjectCard
            key={project.id}
            project={project}
            showActions={true}
            onEdit={handleEdit}
            onDelete={handleDelete}
            onPublish={handlePublish}
          />
        ))}
      </div>

      {/* Delete Confirmation Modal */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-red-600">Delete Project</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{projectToDelete?.title}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeleteDialog(false)}
              disabled={deleteProject.isPending}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={confirmDelete}
              disabled={deleteProject.isPending}
            >
              {deleteProject.isPending ? (
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
  );
}
