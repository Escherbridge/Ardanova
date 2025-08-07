import { redirect } from "next/navigation";
import { Search, Filter, Heart, Users, Calendar, TrendingUp } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Input } from "~/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "~/components/ui/select";
import { Navigation } from "~/components/navigation";
import { auth } from "~/server/auth";

export default async function ProjectsPage() {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  const user = session.user;

  // Mock project data - in real app, this would come from the database
  const projects = [
    {
      id: "1",
      title: "EcoWaste Solutions",
      description: "A comprehensive platform for sustainable waste management that connects households with recycling centers and tracks environmental impact.",
      category: "Environment",
      status: "Seeking Support",
      supportersCount: 124,
      votesCount: 89,
      fundingGoal: 50000,
      currentFunding: 12500,
      createdAt: "2024-01-15",
      creator: {
        name: "Sarah Johnson",
        image: null,
      },
      tags: ["Sustainability", "Environment", "Technology"],
    },
    {
      id: "2",
      title: "HealthTrack AI",
      description: "AI-powered health monitoring system that provides personalized health insights and early disease detection through wearable devices.",
      category: "Healthcare",
      status: "Funded",
      supportersCount: 89,
      votesCount: 156,
      fundingGoal: 75000,
      currentFunding: 75000,
      createdAt: "2024-01-10",
      creator: {
        name: "Dr. Michael Chen",
        image: null,
      },
      tags: ["AI", "Healthcare", "Wearables"],
    },
    {
      id: "3",
      title: "EduConnect",
      description: "A platform connecting students with mentors and tutors, providing personalized learning paths and skill development opportunities.",
      category: "Education",
      status: "In Progress",
      supportersCount: 67,
      votesCount: 45,
      fundingGoal: 30000,
      currentFunding: 18000,
      createdAt: "2024-01-20",
      creator: {
        name: "Emily Rodriguez",
        image: null,
      },
      tags: ["Education", "Mentorship", "Learning"],
    },
    {
      id: "4",
      title: "FarmTech Solutions",
      description: "Smart farming technology that helps small-scale farmers optimize crop yields through IoT sensors and data analytics.",
      category: "Agriculture",
      status: "Seeking Support",
      supportersCount: 43,
      votesCount: 32,
      fundingGoal: 40000,
      currentFunding: 8500,
      createdAt: "2024-01-25",
      creator: {
        name: "James Wilson",
        image: null,
      },
      tags: ["Agriculture", "IoT", "Data Analytics"],
    },
    {
      id: "5",
      title: "ArtSpace Community",
      description: "A digital platform for local artists to showcase their work, connect with buyers, and collaborate on community art projects.",
      category: "Arts & Culture",
      status: "Seeking Support",
      supportersCount: 78,
      votesCount: 92,
      fundingGoal: 25000,
      currentFunding: 5200,
      createdAt: "2024-01-18",
      creator: {
        name: "Maria Santos",
        image: null,
      },
      tags: ["Arts", "Community", "Digital Platform"],
    },
    {
      id: "6",
      title: "CleanEnergy Hub",
      description: "Community-driven renewable energy sharing platform that allows neighborhoods to generate and share clean energy.",
      category: "Environment",
      status: "Seeking Support",
      supportersCount: 156,
      votesCount: 203,
      fundingGoal: 100000,
      currentFunding: 35000,
      createdAt: "2024-01-12",
      creator: {
        name: "Alex Thompson",
        image: null,
      },
      tags: ["Renewable Energy", "Community", "Sustainability"],
    },
  ];

  const getStatusColor = (status: string) => {
    switch (status) {
      case "Seeking Support":
        return "bg-yellow-100 text-yellow-800";
      case "Funded":
        return "bg-green-100 text-green-800";
      case "In Progress":
        return "bg-blue-100 text-blue-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  const getCategoryColor = (category: string) => {
    const colors: Record<string, string> = {
      "Environment": "bg-green-100 text-green-700",
      "Healthcare": "bg-red-100 text-red-700",
      "Education": "bg-blue-100 text-blue-700",
      "Agriculture": "bg-yellow-100 text-yellow-700",
      "Arts & Culture": "bg-purple-100 text-purple-700",
      "Technology": "bg-indigo-100 text-indigo-700",
    };
    return colors[category] ?? "bg-gray-100 text-gray-700";
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navigation user={user} />

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
              <div className="text-sm text-slate-600">Active Projects</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                {projects.reduce((sum, p) => sum + p.supportersCount, 0)}
              </div>
              <div className="text-sm text-slate-600">Total Supporters</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                ${projects.reduce((sum, p) => sum + p.currentFunding, 0).toLocaleString()}
              </div>
              <div className="text-sm text-slate-600">Funds Raised</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-slate-900">
                {projects.filter(p => p.status === "Funded").length}
              </div>
              <div className="text-sm text-slate-600">Funded Projects</div>
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
                    {project.category}
                  </Badge>
                  <Badge variant="outline" className={getStatusColor(project.status)}>
                    {project.status}
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
                  <div>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-slate-600">Funding Progress</span>
                      <span className="font-medium">
                        ${project.currentFunding.toLocaleString()} / ${project.fundingGoal.toLocaleString()}
                      </span>
                    </div>
                    <div className="w-full bg-slate-200 rounded-full h-2">
                      <div
                        className="bg-blue-600 h-2 rounded-full"
                        style={{
                          width: `${Math.min((project.currentFunding / project.fundingGoal) * 100, 100)}%`
                        }}
                      ></div>
                    </div>
                  </div>

                  {/* Stats */}
                  <div className="flex items-center justify-between text-sm text-slate-600">
                    <div className="flex items-center">
                      <Users className="h-4 w-4 mr-1" />
                      {project.supportersCount} supporters
                    </div>
                    <div className="flex items-center">
                      <Heart className="h-4 w-4 mr-1" />
                      {project.votesCount} votes
                    </div>
                  </div>

                  {/* Tags */}
                  <div className="flex flex-wrap gap-1">
                    {project.tags.slice(0, 3).map((tag) => (
                      <Badge key={tag} variant="secondary" className="text-xs">
                        {tag}
                      </Badge>
                    ))}
                  </div>

                  {/* Creator and Date */}
                  <div className="flex items-center justify-between pt-2 border-t">
                    <div className="flex items-center text-sm text-slate-600">
                      <div className="w-6 h-6 bg-slate-300 rounded-full mr-2 flex items-center justify-center">
                        {project.creator.name.charAt(0)}
                      </div>
                      {project.creator.name}
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
