"use client";

import { useState } from "react";
import Link from "next/link";
import {
  CheckSquare,
  Plus,
  Filter,
  LayoutGrid,
  List,
  Calendar,
  Clock,
  AlertCircle,
  CheckCircle2,
  Circle,
  MoreHorizontal,
  ChevronDown,
  Search,
  FolderKanban,
  Users,
  Zap,
  Timer,
  Target,
  TrendingUp,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "~/components/ui/dropdown-menu";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { cn } from "~/lib/utils";

// Task status configuration
const taskStatuses = [
  { id: "todo", label: "To Do", icon: Circle, color: "text-muted-foreground", bgColor: "bg-muted/50" },
  { id: "in_progress", label: "In Progress", icon: Timer, color: "text-neon", bgColor: "bg-neon/10" },
  { id: "review", label: "In Review", icon: AlertCircle, color: "text-neon-purple", bgColor: "bg-neon-purple/10" },
  { id: "done", label: "Done", icon: CheckCircle2, color: "text-neon-green", bgColor: "bg-neon-green/10" },
];

// Priority configuration
const priorityConfig = {
  critical: { label: "Critical", color: "text-destructive", badge: "destructive" as const },
  high: { label: "High", color: "text-neon-pink", badge: "neon-pink" as const },
  medium: { label: "Medium", color: "text-warning", badge: "warning" as const },
  low: { label: "Low", color: "text-muted-foreground", badge: "secondary" as const },
};

// Sample tasks data
const sampleTasks = [
  {
    id: "t1",
    title: "Update landing page hero section",
    description: "Redesign the hero section with new brand colors and messaging",
    status: "in_progress",
    priority: "high",
    project: { id: "p1", name: "EcoWaste Solutions", color: "neon-green" },
    assignee: { id: "u6", name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma" },
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 2),
    reward: 150,
    tags: ["Design", "Frontend"],
    storyPoints: 5,
  },
  {
    id: "t2",
    title: "Implement user authentication flow",
    description: "Add OAuth2 login with Google and GitHub providers",
    status: "todo",
    priority: "critical",
    project: { id: "p2", name: "HealthTrack", color: "neon-pink" },
    assignee: { id: "u7", name: "David Park", avatar: "https://i.pravatar.cc/150?u=david" },
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 1),
    reward: 300,
    tags: ["Backend", "Security"],
    storyPoints: 8,
  },
  {
    id: "t3",
    title: "Write API documentation",
    description: "Document all REST endpoints using OpenAPI spec",
    status: "review",
    priority: "medium",
    project: { id: "p2", name: "HealthTrack", color: "neon-pink" },
    assignee: { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 3),
    reward: 100,
    tags: ["Documentation"],
    storyPoints: 3,
  },
  {
    id: "t4",
    title: "Fix mobile responsiveness issues",
    description: "Address layout bugs on tablets and mobile devices",
    status: "todo",
    priority: "high",
    project: { id: "p1", name: "EcoWaste Solutions", color: "neon-green" },
    assignee: null,
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 4),
    reward: 200,
    tags: ["Bug", "Mobile"],
    storyPoints: 5,
  },
  {
    id: "t5",
    title: "Create onboarding tutorial",
    description: "Build interactive walkthrough for new users",
    status: "done",
    priority: "low",
    project: { id: "p3", name: "EduConnect", color: "neon-purple" },
    assignee: { id: "u5", name: "Jordan Lee", avatar: "https://i.pravatar.cc/150?u=jordan" },
    dueDate: new Date(Date.now() - 1000 * 60 * 60 * 24 * 1),
    reward: 175,
    tags: ["UX", "Onboarding"],
    storyPoints: 5,
  },
  {
    id: "t6",
    title: "Optimize database queries",
    description: "Improve performance of slow database operations",
    status: "in_progress",
    priority: "critical",
    project: { id: "p2", name: "HealthTrack", color: "neon-pink" },
    assignee: { id: "u7", name: "David Park", avatar: "https://i.pravatar.cc/150?u=david" },
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 1),
    reward: 250,
    tags: ["Backend", "Performance"],
    storyPoints: 8,
  },
  {
    id: "t7",
    title: "Add dark mode support",
    description: "Implement theme switching with system preference detection",
    status: "todo",
    priority: "medium",
    project: { id: "p3", name: "EduConnect", color: "neon-purple" },
    assignee: { id: "u6", name: "Emma Watson", avatar: "https://i.pravatar.cc/150?u=emma" },
    dueDate: new Date(Date.now() + 1000 * 60 * 60 * 24 * 7),
    reward: 150,
    tags: ["Design", "Frontend"],
    storyPoints: 3,
  },
  {
    id: "t8",
    title: "Set up CI/CD pipeline",
    description: "Configure automated testing and deployment workflows",
    status: "done",
    priority: "high",
    project: { id: "p1", name: "EcoWaste Solutions", color: "neon-green" },
    assignee: { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
    dueDate: new Date(Date.now() - 1000 * 60 * 60 * 24 * 3),
    reward: 200,
    tags: ["DevOps"],
    storyPoints: 5,
  },
];

function formatDueDate(date: Date): string {
  const now = new Date();
  const diffMs = date.getTime() - now.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays < -1) return `${Math.abs(diffDays)}d overdue`;
  if (diffDays === -1) return "1d overdue";
  if (diffDays === 0) return "Due today";
  if (diffDays === 1) return "Due tomorrow";
  if (diffDays < 7) return `${diffDays}d left`;
  return date.toLocaleDateString();
}

function TaskCard({ task, isDragging = false }: { task: typeof sampleTasks[0]; isDragging?: boolean }) {
  const priority = priorityConfig[task.priority as keyof typeof priorityConfig];
  const isOverdue = new Date(task.dueDate) < new Date() && task.status !== "done";

  return (
    <div
      className={cn(
        "bg-card border-2 border-border p-3 cursor-grab active:cursor-grabbing transition-all",
        isDragging && "opacity-50 rotate-2",
        "hover:border-primary"
      )}
    >
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-2">
          <Badge variant={priority.badge} size="sm">
            {priority.label}
          </Badge>
          {task.tags.slice(0, 1).map((tag) => (
            <Badge key={tag} variant="secondary" size="sm">
              {tag}
            </Badge>
          ))}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" className="size-6">
              <MoreHorizontal className="size-3" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem>Edit task</DropdownMenuItem>
            <DropdownMenuItem>Move to...</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="text-destructive">Delete</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <h4 className="font-medium text-sm text-foreground mb-1 line-clamp-2">
        {task.title}
      </h4>
      <p className="text-xs text-muted-foreground line-clamp-2 mb-3">
        {task.description}
      </p>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {task.assignee ? (
            <Avatar className="size-6 border border-border">
              <AvatarImage src={task.assignee.avatar} />
              <AvatarFallback className="text-xs">{task.assignee.name.charAt(0)}</AvatarFallback>
            </Avatar>
          ) : (
            <div className="size-6 border-2 border-dashed border-border rounded-full flex items-center justify-center">
              <Plus className="size-3 text-muted-foreground" />
            </div>
          )}
          <span className={cn(
            "text-xs",
            isOverdue ? "text-destructive" : "text-muted-foreground"
          )}>
            {formatDueDate(task.dueDate)}
          </span>
        </div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span className="text-neon-green font-medium">{task.reward} tokens</span>
          <Badge variant="secondary" size="sm" className="text-[10px] px-1">
            {task.storyPoints} SP
          </Badge>
        </div>
      </div>
    </div>
  );
}

export default function TasksPage() {
  const [viewMode, setViewMode] = useState<"board" | "list">("board");
  const [selectedProject, setSelectedProject] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");

  // Group tasks by status
  const tasksByStatus = taskStatuses.reduce((acc, status) => {
    acc[status.id] = sampleTasks.filter((task) =>
      task.status === status.id &&
      (selectedProject === "all" || task.project.id === selectedProject)
    );
    return acc;
  }, {} as Record<string, typeof sampleTasks>);

  // Stats
  const stats = {
    total: sampleTasks.length,
    completed: sampleTasks.filter((t) => t.status === "done").length,
    inProgress: sampleTasks.filter((t) => t.status === "in_progress").length,
    totalPoints: sampleTasks.reduce((sum, t) => sum + t.storyPoints, 0),
    completedPoints: sampleTasks.filter((t) => t.status === "done").reduce((sum, t) => sum + t.storyPoints, 0),
  };

  // Get unique projects
  const projects = Array.from(new Set(sampleTasks.map((t) => t.project.id))).map((id) => {
    const task = sampleTasks.find((t) => t.project.id === id);
    return task?.project;
  }).filter(Boolean);

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="sticky top-0 z-10 bg-background border-b-2 border-border">
        <div className="p-4">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-4">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <CheckSquare className="size-5 text-primary" />
                My Tasks
              </h1>
              <div className="flex items-center gap-2 border-l-2 border-border pl-4">
                <Button
                  variant={viewMode === "board" ? "secondary" : "ghost"}
                  size="sm"
                  onClick={() => setViewMode("board")}
                >
                  <LayoutGrid className="size-4 mr-1" />
                  Board
                </Button>
                <Button
                  variant={viewMode === "list" ? "secondary" : "ghost"}
                  size="sm"
                  onClick={() => setViewMode("list")}
                >
                  <List className="size-4 mr-1" />
                  List
                </Button>
              </div>
            </div>
            <Button variant="neon" size="sm">
              <Plus className="size-4 mr-2" />
              New Task
            </Button>
          </div>

          {/* Filters and Search */}
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search tasks..."
                className="w-full pl-10 pr-4 py-2 bg-card border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
              />
            </div>
            <Select value={selectedProject} onValueChange={setSelectedProject}>
              <SelectTrigger className="w-48 border-2">
                <FolderKanban className="size-4 mr-2 text-muted-foreground" />
                <SelectValue placeholder="All Projects" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Projects</SelectItem>
                {projects.map((project) => (
                  <SelectItem key={project!.id} value={project!.id}>
                    {project!.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button variant="outline" size="sm">
              <Filter className="size-4 mr-2" />
              Filters
            </Button>
          </div>
        </div>

        {/* Stats Bar */}
        <div className="px-4 pb-4 flex items-center gap-6">
          <div className="flex items-center gap-2 text-sm">
            <Target className="size-4 text-muted-foreground" />
            <span className="text-muted-foreground">Sprint Progress:</span>
            <span className="font-medium text-foreground">{stats.completedPoints}/{stats.totalPoints} SP</span>
          </div>
          <Progress
            value={(stats.completedPoints / stats.totalPoints) * 100}
            variant="neon"
            className="flex-1 max-w-xs h-2"
          />
          <div className="flex items-center gap-4 text-sm">
            <span className="text-muted-foreground">
              <span className="font-medium text-foreground">{stats.completed}</span> completed
            </span>
            <span className="text-muted-foreground">
              <span className="font-medium text-neon">{stats.inProgress}</span> in progress
            </span>
            <span className="text-muted-foreground">
              <span className="font-medium text-foreground">{stats.total - stats.completed - stats.inProgress}</span> remaining
            </span>
          </div>
        </div>
      </div>

      {/* Board View */}
      {viewMode === "board" && (
        <div className="p-4">
          <div className="flex gap-4 overflow-x-auto pb-4">
            {taskStatuses.map((status) => {
              const StatusIcon = status.icon;
              const tasks = tasksByStatus[status.id] || [];

              return (
                <div
                  key={status.id}
                  className="flex-shrink-0 w-80"
                >
                  {/* Column Header */}
                  <div className={cn(
                    "flex items-center justify-between p-3 mb-3 border-2 border-border",
                    status.bgColor
                  )}>
                    <div className="flex items-center gap-2">
                      <StatusIcon className={cn("size-4", status.color)} />
                      <span className="font-medium text-foreground">{status.label}</span>
                      <Badge variant="secondary" size="sm">{tasks.length}</Badge>
                    </div>
                    <Button variant="ghost" size="icon-sm">
                      <Plus className="size-4" />
                    </Button>
                  </div>

                  {/* Tasks */}
                  <div className="space-y-3 min-h-[200px]">
                    {tasks.map((task) => (
                      <TaskCard key={task.id} task={task} />
                    ))}
                    {tasks.length === 0 && (
                      <div className="border-2 border-dashed border-border p-4 text-center text-sm text-muted-foreground">
                        No tasks
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* List View */}
      {viewMode === "list" && (
        <div className="p-4">
          <Card>
            <CardContent className="p-0">
              <table className="w-full">
                <thead>
                  <tr className="border-b-2 border-border">
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Task</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Project</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Status</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Priority</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Assignee</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Due Date</th>
                    <th className="text-left p-3 text-sm font-medium text-muted-foreground">Reward</th>
                  </tr>
                </thead>
                <tbody>
                  {sampleTasks
                    .filter((task) => selectedProject === "all" || task.project.id === selectedProject)
                    .map((task) => {
                      const status = taskStatuses.find((s) => s.id === task.status);
                      const priority = priorityConfig[task.priority as keyof typeof priorityConfig];
                      const isOverdue = new Date(task.dueDate) < new Date() && task.status !== "done";

                      return (
                        <tr key={task.id} className="border-b border-border hover:bg-card/50 transition-colors">
                          <td className="p-3">
                            <div className="font-medium text-sm text-foreground">{task.title}</div>
                            <div className="text-xs text-muted-foreground line-clamp-1">{task.description}</div>
                          </td>
                          <td className="p-3">
                            <Badge variant="secondary" size="sm">{task.project.name}</Badge>
                          </td>
                          <td className="p-3">
                            <Badge variant="secondary" size="sm" className={status?.color}>
                              {status?.label}
                            </Badge>
                          </td>
                          <td className="p-3">
                            <Badge variant={priority.badge} size="sm">{priority.label}</Badge>
                          </td>
                          <td className="p-3">
                            {task.assignee ? (
                              <div className="flex items-center gap-2">
                                <Avatar className="size-6">
                                  <AvatarImage src={task.assignee.avatar} />
                                  <AvatarFallback className="text-xs">{task.assignee.name.charAt(0)}</AvatarFallback>
                                </Avatar>
                                <span className="text-sm text-muted-foreground">{task.assignee.name}</span>
                              </div>
                            ) : (
                              <span className="text-sm text-muted-foreground">Unassigned</span>
                            )}
                          </td>
                          <td className="p-3">
                            <span className={cn("text-sm", isOverdue ? "text-destructive" : "text-muted-foreground")}>
                              {formatDueDate(task.dueDate)}
                            </span>
                          </td>
                          <td className="p-3">
                            <span className="text-sm text-neon-green font-medium">{task.reward} tokens</span>
                          </td>
                        </tr>
                      );
                    })}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
