import { redirect } from "next/navigation";
import { Search, Filter, Heart, Users, Calendar, TrendingUp } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Input } from "~/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "~/components/ui/select";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";
import { ProjectStatus, ProjectCategory } from "@prisma/client";

export default async function ProjectsPage() {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  const user = session.user;

  // Fetch real projects from database - only published projects
  const projectsResult = await api.project.getAll({
    status: ProjectStatus.PUBLISHED,
    limit: 50,
  });
  
  const projects = projectsResult.items;

  const getStatusColor = (status: ProjectStatus) => {
    switch (status) {
      case ProjectStatus.PUBLISHED:
        return "bg-yellow-100 text-yellow-800";
      case ProjectStatus.COMPLETED:
        return "bg-green-100 text-green-800";
      case ProjectStatus.IN_PROGRESS:
        return "bg-blue-100 text-blue-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  const getCategoryColor = (category: ProjectCategory) => {
    const colors: Record<ProjectCategory, string> = {
      [ProjectCategory.ENVIRONMENT]: "bg-green-100 text-green-700",
      [ProjectCategory.HEALTHCARE]: "bg-red-100 text-red-700",
      [ProjectCategory.EDUCATION]: "bg-blue-100 text-blue-700",
      [ProjectCategory.AGRICULTURE]: "bg-yellow-100 text-yellow-700",
      [ProjectCategory.ARTS_CULTURE]: "bg-purple-100 text-purple-700",
      [ProjectCategory.TECHNOLOGY]: "bg-indigo-100 text-indigo-700",
      [ProjectCategory.SOCIAL_IMPACT]: "bg-purple-100 text-purple-700",
      [ProjectCategory.BUSINESS]: "bg-indigo-100 text-indigo-700",
      [ProjectCategory.FINANCE]: "bg-green-100 text-green-700",
      [ProjectCategory.OTHER]: "bg-gray-100 text-gray-700",
    };
    return colors[category] ?? "bg-gray-100 text-gray-700";
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-slate-900 mb-2">Discover Projects</h1>
          <p className="text-slate-600">
            Explore innovative projects and support the ones that inspire you
          </p>
        </div>

        {/* Filters and Search */}
        <div className="mb-8">
          <div className="flex flex-col sm:flex-row gap-4 mb-6">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
                <Input
                  placeholder="Search projects..."
                  className="pl-10"
                />
              </div>
            </div>
            <Select>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Category" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Categories</SelectItem>
                <SelectItem value="technology">Technology</SelectItem>
                <SelectItem value="healthcare">Healthcare</SelectItem>
                <SelectItem value="education">Education</SelectItem>
                <SelectItem value="environment">Environment</SelectItem>
                <SelectItem value="agriculture">Agriculture</SelectItem>
                <SelectItem value="arts">Arts & Culture</SelectItem>
              </SelectContent>
            </Select>
            <Select>
              <SelectTrigger className="w-full sm:w-48">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="seeking">Seeking Support</SelectItem>
                <SelectItem value="funded">Funded</SelectItem>
                <SelectItem value="progress">In Progress</SelectItem>
                <SelectItem value="completed">Completed</SelectItem>
              </SelectContent>
            </Select>
            <Button variant="outline">
              <Filter className="h-4 w-4 mr-2" />
              More Filters
            </Button>
          </div>

          {/* Quick Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">{projects.length}</div>
              <div className="text-sm text-slate-600">Published Projects</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                {projects.reduce((sum, p) => sum + (p._count?.supports || 0), 0)}
              </div>
              <div className="text-sm text-slate-600">Total Supporters</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                ${projects.reduce((sum, p) => sum + (Number(p.currentFunding || 0)), 0).toLocaleString()}
              </div>
              <div className="text-sm text-slate-600">Funds Raised</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                {projects.filter(p => p.status === ProjectStatus.COMPLETED).length}
              </div>
              <div className="text-sm text-slate-600">Completed Projects</div>
            </div>
          </div>
        </div>

        {/* Projects Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {projects.map((project) => (
            <Card key={project.id} className="hover:shadow-lg transition-shadow cursor-pointer">
              <CardHeader>
                <div className="flex items-start justify-between mb-2">
                  <Badge className={getCategoryColor(project.category)}>
                    {project.category.replace("_", " ")}
                  </Badge>
                  <Badge variant="outline" className={getStatusColor(project.status)}>
                    {project.status.replace("_", " ")}
                  </Badge>
                </div>
                <CardTitle className="text-lg line-clamp-2">{project.title}</CardTitle>
                <CardDescription className="line-clamp-3">
                  {project.description}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {/* Progress Bar */}
                  {project.fundingGoal && project.currentFunding && (
                    <div>
                      <div className="flex justify-between text-sm mb-1">
                        <span className="text-slate-600">Funding Progress</span>
                        <span className="font-medium">
                          ${Number(project.currentFunding).toLocaleString()} / ${Number(project.fundingGoal).toLocaleString()}
                        </span>
                      </div>
                      <div className="w-full bg-slate-200 rounded-full h-2">
                        <div
                          className="bg-blue-600 h-2 rounded-full"
                          style={{
                            width: `${Math.min((Number(project.currentFunding) / Number(project.fundingGoal)) * 100, 100)}%`
                          }}
                        ></div>
                      </div>
                    </div>
                  )}

                  {/* Stats */}
                  <div className="flex items-center justify-between text-sm text-slate-600">
                    <div className="flex items-center">
                      <Users className="h-4 w-4 mr-1" />
                      {project._count?.supports || 0} supporters
                    </div>
                    <div className="flex items-center">
                      <Heart className="h-4 w-4 mr-1" />
                      {project.votesCount || 0} votes
                    </div>
                  </div>

                  {/* Tags */}
                  {project.tags && (
                    <div className="flex flex-wrap gap-1">
                      {project.tags.split(',').slice(0, 3).map((tag: string, index: number) => (
                        <Badge key={index} variant="secondary" className="text-xs">
                          {tag.trim()}
                        </Badge>
                      ))}
                    </div>
                  )}

                  {/* Creator and Date */}
                  <div className="flex items-center justify-between pt-2 border-t">
                    <div className="flex items-center text-sm text-slate-600">
                      <div className="w-6 h-6 bg-slate-300 rounded-full mr-2 flex items-center justify-center">
                        {project.createdBy.name?.charAt(0) || 'U'}
                      </div>
                      {project.createdBy.name || 'Unknown User'}
                    </div>
                    <div className="flex items-center text-xs text-slate-500">
                      <Calendar className="h-3 w-3 mr-1" />
                      {new Date(project.createdAt).toLocaleDateString()}
                    </div>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex gap-2 pt-2">
                    <Button className="flex-1" size="sm">
                      Support Project
                    </Button>
                    <Button variant="outline" size="sm">
                      <Heart className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Load More */}
        <div className="text-center mt-12">
          <Button variant="outline" size="lg">
            Load More Projects
          </Button>
        </div>
      </div>
    </div>
  );
}
