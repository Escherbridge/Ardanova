import { redirect } from "next/navigation";
import { Plus, TrendingUp, Users, Briefcase, Settings } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { auth } from "~/server/auth";
import { DashboardProjects } from "~/components/dashboard-projects";
import { DashboardProjectCards } from "~/components/dashboard-project-cards";

export default async function DashboardPage() {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  const user = session.user;

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8">
        {/* Welcome Section */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-slate-900 mb-2">
            Welcome back, {user.name}!
          </h1>
          <p className="text-slate-600">
            Ready to turn your ideas into reality? Let&apos;s get started.
          </p>
        </div>

        {/* Quick Actions */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <a href="/dashboard/create" className="block">
            <Card className="hover:shadow-md transition-shadow cursor-pointer">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div className="p-2 bg-blue-100 rounded-lg">
                    <Plus className="h-5 w-5 text-blue-600" />
                  </div>
                  <Badge variant="secondary">New</Badge>
                </div>
              </CardHeader>
              <CardContent>
                <CardTitle className="text-lg mb-1">Create Project</CardTitle>
                <CardDescription>
                  Start a new project and get community support
                </CardDescription>
              </CardContent>
            </Card>
          </a>

          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="p-2 bg-green-100 rounded-lg">
                  <Users className="h-5 w-5 text-green-600" />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <CardTitle className="text-lg mb-1">Browse Projects</CardTitle>
              <CardDescription>
                Discover and support innovative projects
              </CardDescription>
            </CardContent>
          </Card>

          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="p-2 bg-purple-100 rounded-lg">
                  <Briefcase className="h-5 w-5 text-purple-600" />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <CardTitle className="text-lg mb-1">Business Tools</CardTitle>
              <CardDescription>
                Manage invoices, inventory, and marketing
              </CardDescription>
            </CardContent>
          </Card>

          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <div className="p-2 bg-orange-100 rounded-lg">
                  <Settings className="h-5 w-5 text-orange-600" />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <CardTitle className="text-lg mb-1">Profile Setup</CardTitle>
              <CardDescription>
                Complete your profile and skills
              </CardDescription>
            </CardContent>
          </Card>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Your Projects */}
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Your Projects</CardTitle>
                  <Button variant="outline" size="sm" asChild>
                    <a href="/dashboard/create">
                      <Plus className="h-4 w-4 mr-2" />
                      New Project
                    </a>
                  </Button>
                </div>
                <CardDescription>
                  Projects you&apos;ve created or are working on
                </CardDescription>
              </CardHeader>
              <CardContent>
                <DashboardProjectCards />
              </CardContent>
            </Card>

            {/* Recent Activity */}
            <Card>
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
                <CardDescription>
                  Latest updates from projects you&apos;re following
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-center py-8 text-slate-500">
                  <p className="text-sm">No recent activity</p>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Profile Completion */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Profile Completion</CardTitle>
                <CardDescription>
                  Complete your profile to get better project matches
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="flex items-center justify-between text-sm">
                    <span>Basic Info</span>
                    <Badge variant="secondary">Complete</Badge>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span>Skills & Experience</span>
                    <Badge variant="outline">Pending</Badge>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span>Portfolio</span>
                    <Badge variant="outline">Pending</Badge>
                  </div>
                  <Button className="w-full mt-4" size="sm">
                    Complete Profile
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Trending Projects */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg flex items-center">
                  <TrendingUp className="h-4 w-4 mr-2" />
                  Trending Projects
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-blue-500 rounded-full mt-2"></div>
                    <div>
                      <h4 className="font-medium text-sm">EcoWaste Solutions</h4>
                      <p className="text-xs text-slate-600">Sustainable waste management platform</p>
                      <div className="flex items-center mt-1">
                        <Badge variant="secondary" className="text-xs">Environment</Badge>
                        <span className="text-xs text-slate-500 ml-2">124 supporters</span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-green-500 rounded-full mt-2"></div>
                    <div>
                      <h4 className="font-medium text-sm">HealthTrack AI</h4>
                      <p className="text-xs text-slate-600">AI-powered health monitoring system</p>
                      <div className="flex items-center mt-1">
                        <Badge variant="secondary" className="text-xs">Healthcare</Badge>
                        <span className="text-xs text-slate-500 ml-2">89 supporters</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-start space-x-3">
                    <div className="w-2 h-2 bg-purple-500 rounded-full mt-2"></div>
                    <div>
                      <h4 className="font-medium text-sm">EduConnect</h4>
                      <p className="text-xs text-slate-600">Connecting students with mentors</p>
                      <div className="flex items-center mt-1">
                        <Badge variant="secondary" className="text-xs">Education</Badge>
                        <span className="text-xs text-slate-500 ml-2">67 supporters</span>
                      </div>
                    </div>
                  </div>
                </div>
                
                <Button variant="outline" className="w-full mt-4" size="sm" asChild>
                  <a href="/projects">View All Projects</a>
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>

        {/* My Projects Section */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="text-2xl font-bold text-slate-900">My Projects</h2>
              <p className="text-slate-600">Manage and track your project progress</p>
            </div>
            <Button asChild>
              <a href="/dashboard/create">
                <Plus className="h-4 w-4 mr-2" />
                Create New Project
              </a>
            </Button>
          </div>
          
          <DashboardProjects />
        </div>
      </div>
    </div>
  );
}
