"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  CheckSquare,
  Plus,
  Search,
  LayoutGrid,
  List,
  Circle,
  Timer,
  AlertCircle,
  CheckCircle2,
  MoreHorizontal,
  Clock,
  Loader2,
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
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { toast } from "sonner";

// ─── Types & Config ──────────────────────────────────────────────────────────

type TaskColumnId = "todo" | "in_progress" | "review" | "done" | "blocked";

const columns: {
  id: TaskColumnId;
  apiStatus: string;
  label: string;
  icon: typeof Circle;
  color: string;
  bgColor: string;
}[] = [
  { id: "todo", apiStatus: "TODO", label: "To Do", icon: Circle, color: "text-muted-foreground", bgColor: "bg-muted/50" },
  { id: "in_progress", apiStatus: "IN_PROGRESS", label: "In Progress", icon: Timer, color: "text-neon-cyan", bgColor: "bg-neon-cyan/10" },
  { id: "review", apiStatus: "REVIEW", label: "Review", icon: AlertCircle, color: "text-neon-purple", bgColor: "bg-neon-purple/10" },
  { id: "done", apiStatus: "COMPLETED", label: "Done", icon: CheckCircle2, color: "text-neon-green", bgColor: "bg-neon-green/10" },
  { id: "blocked", apiStatus: "BLOCKED", label: "Blocked", icon: AlertCircle, color: "text-destructive", bgColor: "bg-destructive/10" },
];

const priorityConfig = {
  URGENT: { label: "Urgent", badge: "destructive" as const },
  HIGH: { label: "High", badge: "neon-pink" as const },
  MEDIUM: { label: "Medium", badge: "warning" as const },
  LOW: { label: "Low", badge: "secondary" as const },
};

function apiStatusToColumn(status: string): TaskColumnId {
  const u = status.toUpperCase();
  if (u === "IN_PROGRESS") return "in_progress";
  if (u === "REVIEW") return "review";
  if (u === "COMPLETED") return "done";
  if (u === "BLOCKED") return "blocked";
  return "todo";
}

function columnToApiStatus(col: TaskColumnId): string {
  const found = columns.find((c) => c.id === col);
  return found?.apiStatus ?? "TODO";
}

function normalizePriority(p: string | null | undefined): keyof typeof priorityConfig {
  const u = (p ?? "MEDIUM").toUpperCase();
  if (u === "URGENT" || u === "CRITICAL") return "URGENT";
  if (u === "HIGH") return "HIGH";
  if (u === "LOW") return "LOW";
  return "MEDIUM";
}

function formatDueDate(date: Date | null): string {
  if (!date || Number.isNaN(date.getTime())) return "";
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

// ─── Task Card ───────────────────────────────────────────────────────────────

function TaskCard({
  task,
  onMove,
}: {
  task: any;
  onMove: (taskId: string, status: string) => void;
}) {
  const priority = normalizePriority(task.priority);
  const pCfg = priorityConfig[priority];
  const dueStr = formatDueDate(task.dueDate ? new Date(task.dueDate) : null);
  const isOverdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== "COMPLETED";
  const equity = typeof task.equityReward === "number" ? task.equityReward : 0;
  const hours = typeof task.estimatedHours === "number" ? task.estimatedHours : null;

  return (
    <div className="bg-card border-2 border-border p-3 hover:border-primary transition-colors">
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-1.5 flex-wrap">
          <Badge variant={pCfg.badge} size="sm">
            {pCfg.label}
          </Badge>
          {task.taskType && (
            <Badge variant="secondary" size="sm">
              {task.taskType}
            </Badge>
          )}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm" className="size-6">
              <MoreHorizontal className="size-3" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="border-2 border-border shadow-[4px_4px_0_0_var(--border)]">
            <DropdownMenuItem disabled className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
              Move to
            </DropdownMenuItem>
            {columns.map((col) => (
              <DropdownMenuItem
                key={col.id}
                disabled={apiStatusToColumn(task.status ?? "TODO") === col.id}
                onClick={() => onMove(String(task.id), col.apiStatus)}
              >
                <col.icon className={cn("size-3 mr-2", col.color)} />
                {col.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <h4 className="font-medium text-sm text-foreground mb-1 line-clamp-2">
        {task.title}
      </h4>
      {task.description && (
        <p className="text-xs text-muted-foreground line-clamp-2 mb-3">
          {task.description}
        </p>
      )}

      <div className="flex items-center justify-between mt-2">
        <div className="flex items-center gap-2">
          {task.assignedTo ? (
            <Avatar className="size-6 border border-border">
              <AvatarImage src={task.assignedTo.image ?? undefined} />
              <AvatarFallback className="text-xs">
                {(task.assignedTo.name ?? "U").charAt(0)}
              </AvatarFallback>
            </Avatar>
          ) : (
            <div className="size-6 border-2 border-dashed border-border flex items-center justify-center">
              <Plus className="size-3 text-muted-foreground" />
            </div>
          )}
          {dueStr && (
            <span className={cn("text-xs flex items-center gap-1", isOverdue ? "text-destructive" : "text-muted-foreground")}>
              <Clock className="size-3" />
              {dueStr}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          {equity > 0 && (
            <span className="text-neon-green font-medium">{equity} tokens</span>
          )}
          {hours !== null && (
            <Badge variant="secondary" size="sm" className="text-[10px] px-1">
              {hours}h
            </Badge>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Quick Add Form ──────────────────────────────────────────────────────────

function QuickAddForm({
  projectId,
  onCreated,
}: {
  projectId: string;
  onCreated: () => void;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState("");

  const createTask = api.task.create.useMutation({
    onSuccess: () => {
      setTitle("");
      setIsOpen(false);
      onCreated();
      toast.success("Task created");
    },
    onError: (e) => toast.error(e.message),
  });

  if (!isOpen) {
    return (
      <button
        onClick={() => setIsOpen(true)}
        className="w-full p-2 border-2 border-dashed border-border hover:border-primary text-xs text-muted-foreground hover:text-primary transition-colors flex items-center justify-center gap-1"
      >
        <Plus className="size-3" />
        Add task
      </button>
    );
  }

  return (
    <div className="border-2 border-primary p-2 bg-card">
      <input
        autoFocus
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter" && title.trim()) {
            createTask.mutate({
              title: title.trim(),
              projectId,
              priority: "MEDIUM",
              type: "FEATURE",
            });
          }
          if (e.key === "Escape") {
            setIsOpen(false);
            setTitle("");
          }
        }}
        placeholder="Task title, press Enter"
        className="w-full bg-transparent border-none focus:outline-none text-sm text-foreground placeholder:text-muted-foreground"
      />
      <div className="flex items-center justify-between mt-2">
        <span className="text-[10px] font-mono text-muted-foreground">ESC to cancel</span>
        <Button
          variant="neon"
          size="sm"
          className="h-6 text-xs"
          disabled={!title.trim() || createTask.isPending}
          onClick={() => {
            if (title.trim()) {
              createTask.mutate({
                title: title.trim(),
                projectId,
                priority: "MEDIUM",
                type: "FEATURE",
              });
            }
          }}
        >
          {createTask.isPending ? "..." : "Add"}
        </Button>
      </div>
    </div>
  );
}

// ─── Main Board Page ─────────────────────────────────────────────────────────

export default function ProjectBoardPage() {
  const params = useParams();
  const slug = params.slug as string;
  const { data: session } = useSession();

  const [searchQuery, setSearchQuery] = useState("");
  const [priorityFilter, setPriorityFilter] = useState<string>("all");
  const [viewMode, setViewMode] = useState<"board" | "list">("board");

  const { data: project, isLoading: projectLoading } = api.project.getById.useQuery(
    { id: slug },
    { enabled: !!slug },
  );

  const projectId = project?.id ? String(project.id) : "";

  const {
    data: tasksData,
    isLoading: tasksLoading,
    refetch,
  } = api.task.getAll.useQuery(
    { projectId, limit: 200 },
    { enabled: !!projectId },
  );

  const updateTaskStatus = api.task.updateStatus.useMutation({
    onSuccess: () => {
      void refetch();
      toast.success("Task updated");
    },
    onError: (e) => toast.error(e.message),
  });

  const tasks = useMemo(() => tasksData?.items ?? [], [tasksData]);

  const filteredTasks = useMemo(() => {
    return tasks.filter((t: any) => {
      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        if (
          !(t.title ?? "").toLowerCase().includes(q) &&
          !(t.description ?? "").toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      if (priorityFilter !== "all") {
        if ((t.priority ?? "MEDIUM").toUpperCase() !== priorityFilter) return false;
      }
      return true;
    });
  }, [tasks, searchQuery, priorityFilter]);

  const tasksByColumn = useMemo(() => {
    const acc: Record<TaskColumnId, any[]> = {
      todo: [],
      in_progress: [],
      review: [],
      done: [],
      blocked: [],
    };
    filteredTasks.forEach((t: any) => {
      const col = apiStatusToColumn(t.status ?? "TODO");
      acc[col].push(t);
    });
    return acc;
  }, [filteredTasks]);

  const stats = useMemo(() => {
    const total = filteredTasks.length;
    const done = tasksByColumn.done.length;
    const inProgress = tasksByColumn.in_progress.length;
    return { total, done, inProgress, progress: total > 0 ? Math.round((done / total) * 100) : 0 };
  }, [filteredTasks, tasksByColumn]);

  const handleMove = (taskId: string, status: string) => {
    updateTaskStatus.mutate({ id: taskId, status: status as "TODO" | "IN_PROGRESS" | "REVIEW" | "COMPLETED" | "BLOCKED" });
  };

  const isLoading = projectLoading || tasksLoading;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-6 animate-spin text-primary" />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center gap-4 p-6">
        <p className="text-muted-foreground">Project not found.</p>
        <Button asChild variant="outline">
          <Link href="/projects">Back to Projects</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="sticky top-0 z-10 bg-background border-b-2 border-border">
        <div className="p-4">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <Link
                href={`/projects/${slug}`}
                className="text-muted-foreground hover:text-foreground transition-colors"
              >
                <ArrowLeft className="size-5" />
              </Link>
              <div>
                <h1 className="text-lg font-bold text-foreground flex items-center gap-2">
                  <CheckSquare className="size-5 text-primary" />
                  {project.title} — Board
                </h1>
                <p className="text-xs text-muted-foreground font-mono">
                  {stats.done}/{stats.total} completed · {stats.progress}%
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <div className="flex items-center gap-1 border-2 border-border">
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
              <Button asChild variant="neon" size="sm">
                <Link href={`/tasks/create?projectId=${projectId}`}>
                  <Plus className="size-4 mr-1" />
                  New Task
                </Link>
              </Button>
            </div>
          </div>

          {/* Filters */}
          <div className="flex items-center gap-3">
            <div className="relative flex-1 max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
              <input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search tasks..."
                className="w-full bg-card border-2 border-border pl-9 pr-3 py-1.5 text-sm focus:border-primary focus:outline-none"
              />
            </div>
            <select
              value={priorityFilter}
              onChange={(e) => setPriorityFilter(e.target.value)}
              className="bg-card border-2 border-border px-3 py-1.5 text-sm focus:border-primary focus:outline-none"
            >
              <option value="all">All Priorities</option>
              <option value="URGENT">Urgent</option>
              <option value="HIGH">High</option>
              <option value="MEDIUM">Medium</option>
              <option value="LOW">Low</option>
            </select>
            <div className="hidden sm:flex items-center gap-3 border-l-2 border-border pl-3">
              <Progress value={stats.progress} className="w-24 h-2" />
              <span className="text-xs font-mono text-muted-foreground">{stats.progress}%</span>
            </div>
          </div>
        </div>
      </div>

      {/* ── Board View ──────────────────────────────────────────────────── */}
      {viewMode === "board" ? (
        <div className="p-4 overflow-x-auto">
          <div className="flex gap-4 min-w-[900px]">
            {columns.map((col) => {
              const colTasks = tasksByColumn[col.id];
              return (
                <div key={col.id} className="flex-1 min-w-[200px]">
                  <div className="flex items-center justify-between mb-3 px-1">
                    <div className="flex items-center gap-2">
                      <col.icon className={cn("size-4", col.color)} />
                      <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        {col.label}
                      </span>
                    </div>
                    <Badge variant="secondary" size="sm">
                      {colTasks.length}
                    </Badge>
                  </div>
                  <div className={cn("border-t-2 pt-3 space-y-2", col.color.replace("text-", "border-"))}>
                    {colTasks.map((task: any) => (
                      <TaskCard
                        key={task.id}
                        task={task}
                        onMove={handleMove}
                      />
                    ))}
                    <QuickAddForm
                      projectId={projectId}
                      onCreated={() => void refetch()}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ) : (
        /* ── List View ──────────────────────────────────────────────────── */
        <div className="p-4">
          <div className="border-2 border-border">
            {/* Table header */}
            <div className="grid grid-cols-[1fr_120px_100px_100px_80px] gap-2 p-3 border-b-2 border-border bg-muted/30">
              <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">Task</span>
              <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">Status</span>
              <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">Priority</span>
              <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">Assignee</span>
              <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground text-right">Reward</span>
            </div>
            {filteredTasks.length === 0 ? (
              <div className="p-8 text-center text-muted-foreground text-sm">
                No tasks found. Create one to get started.
              </div>
            ) : (
              filteredTasks.map((task: any) => {
                const col = columns.find((c) => c.id === apiStatusToColumn(task.status ?? "TODO"));
                const priority = normalizePriority(task.priority);
                const pCfg = priorityConfig[priority];
                const equity = typeof task.equityReward === "number" ? task.equityReward : 0;
                return (
                  <div
                    key={task.id}
                    className="grid grid-cols-[1fr_120px_100px_100px_80px] gap-2 p-3 border-b border-border last:border-b-0 hover:bg-muted/20 transition-colors"
                  >
                    <div>
                      <p className="text-sm font-medium text-foreground line-clamp-1">{task.title}</p>
                      {task.description && (
                        <p className="text-xs text-muted-foreground line-clamp-1 mt-0.5">{task.description}</p>
                      )}
                    </div>
                    <div>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <button className="flex items-center gap-1.5 text-xs">
                            {col && <col.icon className={cn("size-3", col.color)} />}
                            <span className={col?.color}>{col?.label ?? "Unknown"}</span>
                          </button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="border-2 border-border shadow-[4px_4px_0_0_var(--border)]">
                          {columns.map((c) => (
                            <DropdownMenuItem
                              key={c.id}
                              disabled={apiStatusToColumn(task.status ?? "TODO") === c.id}
                              onClick={() => handleMove(String(task.id), c.apiStatus)}
                            >
                              <c.icon className={cn("size-3 mr-2", c.color)} />
                              {c.label}
                            </DropdownMenuItem>
                          ))}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                    <div>
                      <Badge variant={pCfg.badge} size="sm">{pCfg.label}</Badge>
                    </div>
                    <div>
                      {task.assignedTo ? (
                        <div className="flex items-center gap-1.5">
                          <Avatar className="size-5 border border-border">
                            <AvatarImage src={task.assignedTo.image ?? undefined} />
                            <AvatarFallback className="text-[10px]">
                              {(task.assignedTo.name ?? "U").charAt(0)}
                            </AvatarFallback>
                          </Avatar>
                          <span className="text-xs text-muted-foreground truncate">
                            {task.assignedTo.name ?? "User"}
                          </span>
                        </div>
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </div>
                    <div className="text-right">
                      {equity > 0 ? (
                        <span className="text-xs text-neon-green font-medium">{equity}</span>
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}
