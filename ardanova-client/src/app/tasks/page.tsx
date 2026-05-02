"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
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
  Search,
  FolderKanban,
  Zap,
  Timer,
  Target,
  TrendingUp,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
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
import { api } from "~/trpc/react";
import type { RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";

type ApiTask = RouterOutputs["task"]["getMyTasks"]["items"][number];

type TaskColumnId = "todo" | "in_progress" | "review" | "done";

const taskStatuses: {
  id: TaskColumnId;
  label: string;
  icon: typeof Circle;
  color: string;
  bgColor: string;
}[] = [
  { id: "todo", label: "To Do", icon: Circle, color: "text-muted-foreground", bgColor: "bg-muted/50" },
  { id: "in_progress", label: "In Progress", icon: Timer, color: "text-neon", bgColor: "bg-neon/10" },
  { id: "review", label: "In Review", icon: AlertCircle, color: "text-neon-purple", bgColor: "bg-neon-purple/10" },
  { id: "done", label: "Done", icon: CheckCircle2, color: "text-neon-green", bgColor: "bg-neon-green/10" },
];

const priorityConfig = {
  critical: { label: "Critical", color: "text-destructive", badge: "destructive" as const },
  high: { label: "High", color: "text-neon-pink", badge: "neon-pink" as const },
  medium: { label: "Medium", color: "text-warning", badge: "warning" as const },
  low: { label: "Low", color: "text-muted-foreground", badge: "secondary" as const },
};

function apiStatusToColumn(status: string): TaskColumnId {
  const u = status.toUpperCase();
  if (u === "COMPLETED") return "done";
  if (u === "IN_PROGRESS") return "in_progress";
  if (u === "REVIEW") return "review";
  if (u === "BLOCKED") return "todo";
  return "todo";
}

function columnToApiStatus(col: TaskColumnId): "TODO" | "IN_PROGRESS" | "REVIEW" | "COMPLETED" | "BLOCKED" {
  switch (col) {
    case "todo":
      return "TODO";
    case "in_progress":
      return "IN_PROGRESS";
    case "review":
      return "REVIEW";
    case "done":
      return "COMPLETED";
    default:
      return "TODO";
  }
}

function mapPriority(p: string | null | undefined): keyof typeof priorityConfig {
  const u = (p ?? "MEDIUM").toUpperCase();
  if (u === "URGENT") return "critical";
  if (u === "HIGH") return "high";
  if (u === "LOW") return "low";
  return "medium";
}

type TaskRow = {
  id: string;
  title: string;
  description: string;
  column: TaskColumnId;
  priority: keyof typeof priorityConfig;
  project: { id: string; name: string; color: string };
  assignee: { id: string; name: string; avatar?: string } | null;
  dueDate: Date | null;
  reward: number;
  tags: string[];
  storyPoints: number;
};

function mapApiTaskToRow(task: ApiTask): TaskRow {
  const ext = task as ApiTask & {
    project?: { id: string; title: string };
    assignedTo?: { id: string; name?: string | null; image?: string | null };
  };
  const project = ext.project
    ? { id: ext.project.id, name: ext.project.title, color: "neon-green" }
    : { id: task.projectId, name: "Project", color: "muted" };

  const assignee = ext.assignedTo
    ? {
        id: String(ext.assignedTo.id),
        name: ext.assignedTo.name ?? "User",
        avatar: ext.assignedTo.image ?? undefined,
      }
    : null;

  const due = (task as { dueDate?: string | null }).dueDate;
  const equity = (task as { equityReward?: number | null }).equityReward;
  const hours = (task as { estimatedHours?: number | null }).estimatedHours;

  return {
    id: String(task.id),
    title: String(task.title ?? ""),
    description: (task.description as string | null | undefined) ?? "",
    column: apiStatusToColumn(String(task.status ?? "TODO")),
    priority: mapPriority(task.priority as string | null),
    project: {
      ...project,
      id: String(project.id),
      name: String(project.name),
    },
    assignee,
    dueDate: due ? new Date(due) : null,
    reward: typeof equity === "number" ? Number(equity) : 0,
    tags: task.type ? [String(task.type)] : [],
    storyPoints: typeof hours === "number" && hours > 0 ? Math.min(13, Math.max(1, Math.round(hours))) : 5,
  };
}

function formatDueDate(date: Date | null): string {
  if (!date || Number.isNaN(date.getTime())) return "—";
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

function TaskCard({
  task,
  isDragging = false,
  onMove,
}: {
  task: TaskRow;
  isDragging?: boolean;
  onMove: (taskId: string, column: TaskColumnId) => void;
}) {
  const priority = priorityConfig[task.priority];
  const isOverdue =
    task.dueDate && task.dueDate < new Date() && task.column !== "done";

  return (
    <div
      className={cn(
        "bg-card border-2 border-border p-3 cursor-grab active:cursor-grabbing transition-all",
        isDragging && "opacity-50 rotate-2",
        "hover:border-primary",
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
            <DropdownMenuItem disabled>Move to</DropdownMenuItem>
            {taskStatuses.map((s) => (
              <DropdownMenuItem
                key={s.id}
                disabled={task.column === s.id}
                onClick={() => onMove(task.id, s.id)}
              >
                {s.label}
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem className="text-destructive" disabled>
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <h4 className="font-medium text-sm text-foreground mb-1 line-clamp-2">{task.title}</h4>
      <p className="text-xs text-muted-foreground line-clamp-2 mb-3">{task.description}</p>

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
          <span className={cn("text-xs", isOverdue ? "text-destructive" : "text-muted-foreground")}>
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
  const { data: session, status: sessionStatus } = useSession();
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [viewMode, setViewMode] = useState<"board" | "list">("board");
  const [selectedProject, setSelectedProject] = useState(
    () => searchParams.get("project") ?? "all",
  );
  const [searchQuery, setSearchQuery] = useState(() => searchParams.get("q") ?? "");

  useEffect(() => {
    const params = new URLSearchParams();
    const t = searchQuery.trim();
    if (t) params.set("q", t);
    if (selectedProject !== "all") params.set("project", selectedProject);
    const qs = params.toString();
    const next = qs ? `${pathname}?${qs}` : pathname;
    router.replace(next, { scroll: false });
  }, [searchQuery, selectedProject, pathname, router]);

  const { data: tasksData, isLoading, error, refetch } = api.task.getMyTasks.useQuery(
    { limit: 100 },
    { enabled: sessionStatus === "authenticated" },
  );

  const utils = api.useUtils();
  const updateStatus = api.task.updateStatus.useMutation({
    onSuccess: () => {
      void utils.task.getMyTasks.invalidate();
      toast.success("Task updated");
    },
    onError: (e) => toast.error(e.message),
  });

  const rows = useMemo(() => (tasksData?.items ?? []).map(mapApiTaskToRow), [tasksData?.items]);

  const filteredRows = useMemo(() => {
    return rows.filter((task) => {
      if (selectedProject !== "all" && task.project.id !== selectedProject) return false;
      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        if (
          !task.title.toLowerCase().includes(q) &&
          !task.description.toLowerCase().includes(q) &&
          !task.project.name.toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      return true;
    });
  }, [rows, selectedProject, searchQuery]);

  const tasksByStatus = useMemo(() => {
    const acc = {} as Record<TaskColumnId, TaskRow[]>;
    taskStatuses.forEach((s) => {
      acc[s.id] = filteredRows.filter((t) => t.column === s.id);
    });
    return acc;
  }, [filteredRows]);

  const stats = useMemo(() => {
    const total = filteredRows.length;
    const completed = filteredRows.filter((t) => t.column === "done").length;
    const inProgress = filteredRows.filter((t) => t.column === "in_progress").length;
    const totalPoints = filteredRows.reduce((sum, t) => sum + t.storyPoints, 0);
    const completedPoints = filteredRows.filter((t) => t.column === "done").reduce((sum, t) => sum + t.storyPoints, 0);
    return { total, completed, inProgress, totalPoints, completedPoints };
  }, [filteredRows]);

  const projects = useMemo(() => {
    const map = new Map<string, { id: string; name: string; color: string }>();
    rows.forEach((t) => map.set(t.project.id, t.project));
    return Array.from(map.values());
  }, [rows]);

  const handleMove = (taskId: string, column: TaskColumnId) => {
    updateStatus.mutate({ id: taskId, status: columnToApiStatus(column) });
  };

  if (sessionStatus === "loading") {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <p className="text-muted-foreground">Loading…</p>
      </div>
    );
  }

  if (sessionStatus === "unauthenticated") {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center gap-4 p-6">
        <p className="text-muted-foreground">Sign in to view your tasks.</p>
        <Button asChild variant="neon">
          <Link href="/api/auth/signin">Sign in</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="sticky top-0 z-10 bg-background border-b-2 border-border">
        <div className="p-4">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-4">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <CheckSquare className="size-5 text-primary" />
                My Tasks
              </h1>
              <div className="flex items-center gap-2 border-l-2 border-border pl-4">
                <Button variant={viewMode === "board" ? "secondary" : "ghost"} size="sm" onClick={() => setViewMode("board")}>
                  <LayoutGrid className="size-4 mr-1" />
                  Board
                </Button>
                <Button variant={viewMode === "list" ? "secondary" : "ghost"} size="sm" onClick={() => setViewMode("list")}>
                  <List className="size-4 mr-1" />
                  List
                </Button>
              </div>
            </div>
            <Button variant="neon" size="sm" asChild>
              <Link href="/tasks/create">
                <Plus className="size-4 mr-2" />
                New Task
              </Link>
            </Button>
          </div>

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
                  <SelectItem key={project.id} value={project.id}>
                    {project.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button variant="outline" size="sm" type="button" onClick={() => void refetch()}>
              <Filter className="size-4 mr-2" />
              Refresh
            </Button>
          </div>
        </div>

        <div className="px-4 pb-4 flex items-center gap-6">
          <div className="flex items-center gap-2 text-sm">
            <Target className="size-4 text-muted-foreground" />
            <span className="text-muted-foreground">Sprint Progress:</span>
            <span className="font-medium text-foreground">
              {stats.completedPoints}/{stats.totalPoints || 1} SP
            </span>
          </div>
          <Progress
            value={stats.totalPoints ? (stats.completedPoints / stats.totalPoints) * 100 : 0}
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
              <span className="font-medium text-foreground">{stats.total - stats.completed - stats.inProgress}</span>{" "}
              remaining
            </span>
          </div>
        </div>
      </div>

      {error && (
        <div className="p-4 text-destructive text-sm">
          {error.message}{" "}
          <button type="button" className="underline" onClick={() => void refetch()}>
            Retry
          </button>
        </div>
      )}

      {isLoading ? (
        <div className="p-8 text-center text-muted-foreground">Loading tasks…</div>
      ) : viewMode === "board" ? (
        <div className="p-4">
          <div className="flex gap-4 overflow-x-auto pb-4">
            {taskStatuses.map((status) => {
              const StatusIcon = status.icon;
              const tasks = tasksByStatus[status.id] || [];

              return (
                <div key={status.id} className="flex-shrink-0 w-80">
                  <div className={cn("flex items-center justify-between p-3 mb-3 border-2 border-border", status.bgColor)}>
                    <div className="flex items-center gap-2">
                      <StatusIcon className={cn("size-4", status.color)} />
                      <span className="font-medium text-foreground">{status.label}</span>
                      <Badge variant="secondary" size="sm">
                        {tasks.length}
                      </Badge>
                    </div>
                  </div>

                  <div className="space-y-3 min-h-[200px]">
                    {tasks.map((task) => (
                      <TaskCard key={task.id} task={task} onMove={handleMove} />
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
      ) : (
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
                  {filteredRows.map((task) => {
                    const status = taskStatuses.find((s) => s.id === task.column);
                    const priority = priorityConfig[task.priority];
                    const isOverdue =
                      task.dueDate && task.dueDate < new Date() && task.column !== "done";

                    return (
                      <tr key={task.id} className="border-b border-border hover:bg-card/50 transition-colors">
                        <td className="p-3">
                          <div className="font-medium text-sm text-foreground">{task.title}</div>
                          <div className="text-xs text-muted-foreground line-clamp-1">{task.description}</div>
                        </td>
                        <td className="p-3">
                          <Badge variant="secondary" size="sm">
                            {task.project.name}
                          </Badge>
                        </td>
                        <td className="p-3">
                          <Badge variant="secondary" size="sm" className={status?.color}>
                            {status?.label}
                          </Badge>
                        </td>
                        <td className="p-3">
                          <Badge variant={priority.badge} size="sm">
                            {priority.label}
                          </Badge>
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
