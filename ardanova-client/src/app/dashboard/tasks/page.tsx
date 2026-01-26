"use client";

import { useState } from "react";
import Link from "next/link";
import { CheckSquare, Plus, Filter, Clock, AlertCircle, CheckCircle2 } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "~/components/ui/tabs";
import { Progress } from "~/components/ui/progress";

// Sample task data - in production this would come from API
const sampleTasks = [
  {
    id: "t1",
    title: "Update landing page design",
    project: { id: "p1", name: "EcoWaste Solutions" },
    status: "in_progress",
    priority: "high",
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 2),
    reward: "150 tokens",
  },
  {
    id: "t2",
    title: "Write API documentation",
    project: { id: "p2", name: "HealthTrack" },
    status: "todo",
    priority: "medium",
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 5),
    reward: "100 tokens",
  },
  {
    id: "t3",
    title: "Review mobile app accessibility",
    project: { id: "p2", name: "HealthTrack" },
    status: "review",
    priority: "high",
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24),
    reward: "200 tokens",
  },
  {
    id: "t4",
    title: "Create onboarding flow mockups",
    project: { id: "p3", name: "EduConnect" },
    status: "completed",
    priority: "low",
    dueDate: new Date(Date.now() - 1000 * 60 * 60 * 24),
    reward: "75 tokens",
  },
];

const statusConfig = {
  todo: { label: "To Do", icon: Clock, variant: "secondary" as const },
  in_progress: { label: "In Progress", icon: AlertCircle, variant: "neon" as const },
  review: { label: "In Review", icon: CheckSquare, variant: "neon-purple" as const },
  completed: { label: "Completed", icon: CheckCircle2, variant: "neon-green" as const },
};

const priorityConfig = {
  low: { label: "Low", variant: "secondary" as const },
  medium: { label: "Medium", variant: "warning" as const },
  high: { label: "High", variant: "destructive" as const },
};

function formatDueDate(date: Date): string {
  const now = new Date();
  const diff = date.getTime() - now.getTime();
  const days = Math.floor(diff / (1000 * 60 * 60 * 24));

  if (days < 0) return `${Math.abs(days)} days overdue`;
  if (days === 0) return "Due today";
  if (days === 1) return "Due tomorrow";
  return `Due in ${days} days`;
}

export default function TasksPage() {
  const [activeTab, setActiveTab] = useState("all");

  const filteredTasks = activeTab === "all"
    ? sampleTasks
    : sampleTasks.filter(t => t.status === activeTab);

  const taskStats = {
    total: sampleTasks.length,
    completed: sampleTasks.filter(t => t.status === "completed").length,
    inProgress: sampleTasks.filter(t => t.status === "in_progress").length,
  };

  return (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
              <CheckSquare className="size-6 text-primary" />
              My Tasks
            </h1>
            <p className="text-muted-foreground mt-1">
              Manage and track your assigned tasks
            </p>
          </div>
          <Button variant="outline">
            <Filter className="size-4 mr-2" />
            Filters
          </Button>
        </div>

        {/* Stats Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Total Tasks</p>
                  <p className="text-2xl font-bold text-foreground">{taskStats.total}</p>
                </div>
                <CheckSquare className="size-8 text-primary" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">In Progress</p>
                  <p className="text-2xl font-bold text-foreground">{taskStats.inProgress}</p>
                </div>
                <AlertCircle className="size-8 text-neon-yellow" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Completed</p>
                  <p className="text-2xl font-bold text-foreground">{taskStats.completed}</p>
                </div>
                <CheckCircle2 className="size-8 text-neon-green" />
              </div>
              <Progress
                value={(taskStats.completed / taskStats.total) * 100}
                variant="neon-green"
                className="mt-2 h-1"
              />
            </CardContent>
          </Card>
        </div>

        {/* Tasks Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="todo">To Do</TabsTrigger>
            <TabsTrigger value="in_progress">In Progress</TabsTrigger>
            <TabsTrigger value="review">In Review</TabsTrigger>
            <TabsTrigger value="completed">Completed</TabsTrigger>
          </TabsList>

          <TabsContent value={activeTab} className="mt-6">
            <div className="space-y-4">
              {filteredTasks.map((task) => {
                const status = statusConfig[task.status as keyof typeof statusConfig];
                const priority = priorityConfig[task.priority as keyof typeof priorityConfig];
                const StatusIcon = status.icon;

                return (
                  <Card key={task.id} className="hover:border-primary transition-colors">
                    <CardContent className="py-4">
                      <div className="flex items-start gap-4">
                        <div className="mt-1">
                          <StatusIcon className={`size-5 ${task.status === 'completed' ? 'text-neon-green' : 'text-muted-foreground'}`} />
                        </div>
                        <div className="flex-1">
                          <div className="flex items-start justify-between">
                            <div>
                              <h3 className="font-medium text-foreground">{task.title}</h3>
                              <Link
                                href={`/projects/${task.project.id}`}
                                className="text-sm text-muted-foreground hover:text-primary transition-colors"
                              >
                                {task.project.name}
                              </Link>
                            </div>
                            <div className="flex items-center gap-2">
                              <Badge variant={priority.variant} size="sm">
                                {priority.label}
                              </Badge>
                              <Badge variant={status.variant} size="sm">
                                {status.label}
                              </Badge>
                            </div>
                          </div>
                          <div className="flex items-center gap-4 mt-2 text-sm text-muted-foreground">
                            <span className={task.status !== 'completed' && new Date(task.dueDate) < new Date() ? 'text-destructive' : ''}>
                              {formatDueDate(task.dueDate)}
                            </span>
                            <span className="text-neon-green font-medium">{task.reward}</span>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}

              {filteredTasks.length === 0 && (
                <Card>
                  <CardContent className="py-12 text-center">
                    <CheckCircle2 className="size-12 text-muted-foreground mx-auto mb-4" />
                    <p className="text-muted-foreground">No tasks found in this category.</p>
                  </CardContent>
                </Card>
              )}
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
