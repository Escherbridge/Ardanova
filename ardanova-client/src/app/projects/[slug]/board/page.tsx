"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
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
  RefreshCw,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";

// ─── Types & Config ──────────────────────────────────────────────────────────

type TaskColumnId = "todo" | "in_progress" | "review" | "done" | "blocked";
type TaskStatus = RouterInputs["task"]["updateStatus"]["status"];
type TaskPriority = NonNullable<RouterInputs["task"]["create"]["priority"]>;
type BoardTask = RouterOutputs["task"]["getAll"]["items"][number];
type TaskAssignee = { name?: string | null; image?: string | null };

function TodoColumnIcon({ className }: { className?: string }) {
  return <Circle className={className} aria-hidden="true" />;
}

function ActiveColumnIcon({ className }: { className?: string }) {
  return <Timer className={className} aria-hidden="true" />;
}

function AttentionColumnIcon({ className }: { className?: string }) {
  return <AlertCircle className={className} aria-hidden="true" />;
}

function DoneColumnIcon({ className }: { className?: string }) {
  return <CheckCircle2 className={className} aria-hidden="true" />;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getTaskAssignee(task: BoardTask): TaskAssignee | null {
  if (!isRecord(task)) return null;

  const assignedTo = task.assignedTo;
  if (!isRecord(assignedTo)) return null;

  const name = assignedTo.name;
  const image = assignedTo.image;
  return {
    name: typeof name === "string" ? name : null,
    image: typeof image === "string" ? image : null,
  };
}

const columns: {
  id: TaskColumnId;
  apiStatus: TaskStatus;
  label: string;
  icon: typeof TodoColumnIcon;
  color: string;
}[] = [
  {
    id: "todo",
    apiStatus: "TODO",
    label: "To Do",
    icon: TodoColumnIcon,
    color: "text-muted-foreground",
  },
  {
    id: "in_progress",
    apiStatus: "IN_PROGRESS",
    label: "In Progress",
    icon: ActiveColumnIcon,
    color: "text-neon-cyan",
  },
  {
    id: "review",
    apiStatus: "REVIEW",
    label: "Review",
    icon: AttentionColumnIcon,
    color: "text-neon-purple",
  },
  {
    id: "done",
    apiStatus: "COMPLETED",
    label: "Done",
    icon: DoneColumnIcon,
    color: "text-neon-green",
  },
  {
    id: "blocked",
    apiStatus: "BLOCKED",
    label: "Blocked",
    icon: AttentionColumnIcon,
    color: "text-destructive",
  },
];

const priorityConfig = {
  URGENT: { label: "Urgent", badge: "destructive" as const },
  HIGH: { label: "High", badge: "neon-pink" as const },
  MEDIUM: { label: "Medium", badge: "warning" as const },
  LOW: { label: "Low", badge: "secondary" as const },
};

const taskPriorities = [
  "URGENT",
  "HIGH",
  "MEDIUM",
  "LOW",
] as const satisfies readonly TaskPriority[];

function isTaskPriority(value: string): value is TaskPriority {
  return taskPriorities.some((priority) => priority === value);
}

function apiStatusToColumn(status: string): TaskColumnId {
  const u = status.toUpperCase();
  if (u === "IN_PROGRESS") return "in_progress";
  if (u === "REVIEW") return "review";
  if (u === "COMPLETED") return "done";
  if (u === "BLOCKED") return "blocked";
  return "todo";
}

function normalizePriority(
  p: string | null | undefined,
): keyof typeof priorityConfig {
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
  task: BoardTask;
  onMove: (taskId: string, status: TaskStatus) => void;
}) {
  const assignedTo = getTaskAssignee(task);
  const priority = normalizePriority(task.priority);
  const pCfg = priorityConfig[priority];
  const dueStr = formatDueDate(task.dueDate ? new Date(task.dueDate) : null);
  const isOverdue =
    task.dueDate &&
    new Date(task.dueDate) < new Date() &&
    task.status !== "COMPLETED";
  const equity = typeof task.equityReward === "number" ? task.equityReward : 0;
  const hours =
    typeof task.estimatedHours === "number" ? task.estimatedHours : null;

  return (
    <div className="bg-card border-border hover:border-primary border-2 p-3 transition-colors">
      <div className="mb-2 flex items-start justify-between gap-2">
        <div className="flex flex-wrap items-center gap-1.5">
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
            <Button
              variant="ghost"
              size="icon-sm"
              className="size-11"
              aria-label={`Move ${task.title}`}
            >
              <MoreHorizontal className="size-4" aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="border-border border-2">
            <DropdownMenuItem
              disabled
              className="text-muted-foreground font-mono text-xs tracking-widest uppercase"
            >
              Move to
            </DropdownMenuItem>
            {columns.map((col) => (
              <DropdownMenuItem
                key={col.id}
                disabled={apiStatusToColumn(task.status ?? "TODO") === col.id}
                onClick={() => onMove(String(task.id), col.apiStatus)}
              >
                <col.icon
                  className={cn("mr-2 size-3", col.color)}
                  aria-hidden="true"
                />
                {col.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <h4 className="text-foreground mb-1 line-clamp-2 text-sm font-medium">
        {task.title}
      </h4>
      {task.description && (
        <p className="text-muted-foreground mb-3 line-clamp-2 text-xs">
          {task.description}
        </p>
      )}

      <div className="mt-2 flex items-center justify-between">
        <div className="flex items-center gap-2">
          {assignedTo ? (
            <Avatar className="border-border size-6 border">
              <AvatarImage src={assignedTo.image ?? undefined} />
              <AvatarFallback className="text-xs">
                {(assignedTo.name ?? "U").charAt(0)}
              </AvatarFallback>
            </Avatar>
          ) : (
            <div className="border-border flex size-6 items-center justify-center border-2 border-dashed">
              <Plus className="text-muted-foreground size-3" />
            </div>
          )}
          {dueStr && (
            <span
              className={cn(
                "flex items-center gap-1 text-xs",
                isOverdue ? "text-destructive" : "text-muted-foreground",
              )}
            >
              <Clock className="size-3" />
              {dueStr}
            </span>
          )}
        </div>
        <div className="text-muted-foreground flex items-center gap-2 text-xs">
          {equity > 0 && (
            <span className="text-neon-green font-medium">{equity} tokens</span>
          )}
          {hours !== null && (
            <Badge variant="secondary" size="sm" className="px-1 text-[10px]">
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
  status,
  onCreated,
}: {
  projectId: string;
  status: TaskStatus;
  onCreated: () => void;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [title, setTitle] = useState("");

  const createTask = api.task.create.useMutation();
  const moveTask = api.task.updateStatus.useMutation();
  const isPending = createTask.isPending || moveTask.isPending;

  const submitTask = async () => {
    if (!title.trim() || isPending) return;

    try {
      const createdTask = await createTask.mutateAsync({
        title: title.trim(),
        projectId,
        priority: "MEDIUM",
        type: "FEATURE",
      });

      setTitle("");
      setIsOpen(false);

      if (status !== "TODO") {
        try {
          await moveTask.mutateAsync({ id: createdTask.id, status });
          const label = columns.find(
            (column) => column.apiStatus === status,
          )?.label;
          toast.success(`Task created in ${label ?? status}`);
        } catch (moveError) {
          const message =
            moveError instanceof Error ? moveError.message : "Unknown error";
          toast.error(
            `Task was created in To Do, but could not be moved: ${message}`,
          );
        }
      } else {
        toast.success("Task created in To Do");
      }

      onCreated();
    } catch (createError) {
      const message =
        createError instanceof Error ? createError.message : "Unknown error";
      toast.error(`Task was not created: ${message}`);
    }
  };

  if (!isOpen) {
    return (
      <button
        onClick={() => setIsOpen(true)}
        className="border-border hover:border-primary text-muted-foreground hover:text-primary flex min-h-11 w-full items-center justify-center gap-1 border-2 border-dashed p-2 text-xs transition-colors"
      >
        <Plus className="size-3" />
        Add task
      </button>
    );
  }

  return (
    <div className="border-primary bg-card border-2 p-2">
      <input
        autoFocus
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter" && title.trim() && !e.nativeEvent.isComposing) {
            e.preventDefault();
            void submitTask();
          }
          if (e.key === "Escape") {
            setIsOpen(false);
            setTitle("");
          }
        }}
        placeholder="Task title, press Enter"
        className="text-foreground placeholder:text-muted-foreground min-h-11 w-full border-none bg-transparent text-sm focus:outline-none"
      />
      <div className="mt-2 flex items-center justify-between">
        <span className="text-muted-foreground font-mono text-[10px]">
          ESC to cancel
        </span>
        <Button
          variant="neon"
          size="sm"
          className="min-h-11 text-xs"
          disabled={!title.trim() || isPending}
          onClick={() => void submitTask()}
        >
          {isPending ? "..." : "Add"}
        </Button>
      </div>
    </div>
  );
}

// ─── Main Board Page ─────────────────────────────────────────────────────────

export default function ProjectBoardPage() {
  const { slug } = useParams<{ slug: string }>();

  const [searchQuery, setSearchQuery] = useState("");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "all">(
    "all",
  );
  const [viewMode, setViewMode] = useState<"board" | "list">("board");

  const {
    data: project,
    error: projectError,
    isLoading: projectLoading,
    refetch: retryProject,
  } = api.project.getById.useQuery({ id: slug }, { enabled: !!slug });

  const projectId = project?.id ? String(project.id) : "";

  const {
    data: tasksData,
    error: tasksError,
    isLoading: tasksLoading,
    refetch,
  } = api.task.getAll.useQuery(
    { projectId, limit: 100 },
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
    return tasks.filter((task) => {
      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        if (
          !task.title.toLowerCase().includes(q) &&
          !(task.description ?? "").toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      if (priorityFilter !== "all") {
        if ((task.priority ?? "MEDIUM").toUpperCase() !== priorityFilter)
          return false;
      }
      return true;
    });
  }, [tasks, searchQuery, priorityFilter]);

  const tasksByColumn = useMemo(() => {
    const acc: Record<TaskColumnId, BoardTask[]> = {
      todo: [],
      in_progress: [],
      review: [],
      done: [],
      blocked: [],
    };
    filteredTasks.forEach((task) => {
      const col = apiStatusToColumn(task.status ?? "TODO");
      acc[col].push(task);
    });
    return acc;
  }, [filteredTasks]);

  const stats = useMemo(() => {
    const total = filteredTasks.length;
    const done = tasksByColumn.done.length;
    return {
      total,
      done,
      progress: total > 0 ? Math.round((done / total) * 100) : 0,
    };
  }, [filteredTasks, tasksByColumn]);

  const handleMove = (taskId: string, status: TaskStatus) => {
    updateTaskStatus.mutate({
      id: taskId,
      status,
    });
  };

  const isLoading = projectLoading || tasksLoading;

  if (isLoading) {
    return (
      <div className="bg-background flex min-h-screen items-center justify-center">
        <Loader2 className="text-primary size-6 animate-spin" />
      </div>
    );
  }

  if (projectError || tasksError) {
    const loadError = projectError ?? tasksError;
    return (
      <div className="bg-background flex min-h-screen items-center justify-center p-6">
        <div className="border-destructive/40 bg-destructive/5 flex max-w-lg flex-col items-start gap-4 border-2 p-6">
          <div className="flex items-start gap-3">
            <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
            <div>
              <p className="text-destructive font-mono text-sm font-bold">
                BOARD COULD NOT BE LOADED
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                {loadError?.message ?? "Unknown error"}
              </p>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            onClick={() => {
              if (projectError) void retryProject();
              if (tasksError) void refetch();
            }}
          >
            <RefreshCw className="mr-2 size-4" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="bg-background flex min-h-screen flex-col items-center justify-center gap-4 p-6">
        <p className="text-muted-foreground">Project not found.</p>
        <Button asChild variant="outline">
          <Link href="/projects">Back to Projects</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen">
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="border-border bg-background relative z-10 border-b-2">
        <div className="p-4">
          <div className="mb-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex min-w-0 items-center gap-3">
              <Link
                href={`/projects/${slug}`}
                aria-label={`Back to ${project.title}`}
                className="text-muted-foreground hover:text-foreground inline-flex size-11 shrink-0 items-center justify-center transition-colors"
              >
                <ArrowLeft className="size-5" />
              </Link>
              <div className="min-w-0">
                <h1 className="text-foreground flex items-start gap-2 text-base font-bold sm:text-lg">
                  <CheckSquare className="text-primary mt-0.5 size-5 shrink-0" />
                  <span className="min-w-0 break-words">
                    {project.title} — Board
                  </span>
                </h1>
                <p className="text-muted-foreground font-mono text-xs">
                  {stats.done}/{stats.total} completed · {stats.progress}%
                </p>
              </div>
            </div>
            <div className="flex w-full flex-wrap items-center gap-2 sm:w-auto sm:flex-nowrap">
              <div className="border-border flex min-w-0 flex-1 items-center gap-1 border-2 sm:flex-none">
                <Button
                  variant={viewMode === "board" ? "secondary" : "ghost"}
                  size="sm"
                  className="min-h-11 flex-1 sm:flex-none"
                  onClick={() => setViewMode("board")}
                >
                  <LayoutGrid className="mr-1 size-4" />
                  Board
                </Button>
                <Button
                  variant={viewMode === "list" ? "secondary" : "ghost"}
                  size="sm"
                  className="min-h-11 flex-1 sm:flex-none"
                  onClick={() => setViewMode("list")}
                >
                  <List className="mr-1 size-4" />
                  List
                </Button>
              </div>
              <Button
                asChild
                variant="neon"
                size="sm"
                className="min-h-11 flex-1 sm:flex-none"
              >
                <Link href={`/tasks/create?projectId=${projectId}`}>
                  <Plus className="mr-1 size-4" />
                  New Task
                </Link>
              </Button>
            </div>
          </div>

          {/* Filters */}
          <div className="flex items-center gap-3">
            <div className="relative max-w-xs flex-1">
              <label htmlFor="project-board-task-search" className="sr-only">
                Search project board tasks
              </label>
              <Search
                className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2"
                aria-hidden="true"
              />
              <input
                id="project-board-task-search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search tasks..."
                className="bg-card border-border focus:border-primary min-h-11 w-full border-2 py-1.5 pr-3 pl-9 text-sm focus:outline-none"
              />
            </div>
            <label htmlFor="project-board-priority-filter" className="sr-only">
              Filter project board tasks by priority
            </label>
            <select
              id="project-board-priority-filter"
              value={priorityFilter}
              onChange={(e) => {
                const value = e.target.value;
                if (value === "all" || isTaskPriority(value)) {
                  setPriorityFilter(value);
                }
              }}
              className="bg-card border-border focus:border-primary min-h-11 border-2 px-3 py-1.5 text-sm focus:outline-none"
            >
              <option value="all">All Priorities</option>
              <option value="URGENT">Urgent</option>
              <option value="HIGH">High</option>
              <option value="MEDIUM">Medium</option>
              <option value="LOW">Low</option>
            </select>
            <div className="border-border hidden items-center gap-3 border-l-2 pl-3 sm:flex">
              <Progress
                value={stats.progress}
                className="h-2 w-24"
                aria-label="Project task completion progress"
              />
              <span className="text-muted-foreground font-mono text-xs">
                {stats.progress}%
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* ── Board View ──────────────────────────────────────────────────── */}
      {viewMode === "board" ? (
        <div className="overflow-x-auto p-4">
          <div className="flex min-w-[900px] gap-4">
            {columns.map((col) => {
              const colTasks = tasksByColumn[col.id];
              return (
                <div key={col.id} className="min-w-[200px] flex-1">
                  <div className="mb-3 flex items-center justify-between px-1">
                    <div className="flex items-center gap-2">
                      <col.icon className={cn("size-4", col.color)} />
                      <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                        {col.label}
                      </span>
                    </div>
                    <Badge variant="secondary" size="sm">
                      {colTasks.length}
                    </Badge>
                  </div>
                  <div
                    className={cn(
                      "space-y-2 border-t-2 pt-3",
                      col.color.replace("text-", "border-"),
                    )}
                  >
                    {colTasks.map((task) => (
                      <TaskCard key={task.id} task={task} onMove={handleMove} />
                    ))}
                    <QuickAddForm
                      projectId={projectId}
                      status={col.apiStatus}
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
        <div className="max-w-full overflow-x-auto overscroll-x-contain p-4">
          <div className="border-border min-w-[640px] border-2">
            {/* Table header */}
            <div className="border-border bg-muted/30 grid grid-cols-[1fr_120px_100px_100px_80px] gap-2 border-b-2 p-3">
              <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                Task
              </span>
              <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                Status
              </span>
              <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                Priority
              </span>
              <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                Assignee
              </span>
              <span className="text-muted-foreground text-right font-mono text-xs tracking-widest uppercase">
                Token units
              </span>
            </div>
            {filteredTasks.length === 0 ? (
              <div className="text-muted-foreground p-8 text-center text-sm">
                No tasks found. Create one to get started.
              </div>
            ) : (
              filteredTasks.map((task) => {
                const assignedTo = getTaskAssignee(task);
                const col = columns.find(
                  (c) => c.id === apiStatusToColumn(task.status ?? "TODO"),
                );
                const priority = normalizePriority(task.priority);
                const pCfg = priorityConfig[priority];
                const equity =
                  typeof task.equityReward === "number" ? task.equityReward : 0;
                return (
                  <div
                    key={task.id}
                    className="border-border hover:bg-muted/20 grid grid-cols-[1fr_120px_100px_100px_80px] gap-2 border-b p-3 transition-colors last:border-b-0"
                  >
                    <div>
                      <p className="text-foreground line-clamp-1 text-sm font-medium">
                        {task.title}
                      </p>
                      {task.description && (
                        <p className="text-muted-foreground mt-0.5 line-clamp-1 text-xs">
                          {task.description}
                        </p>
                      )}
                    </div>
                    <div>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <button className="flex min-h-11 items-center gap-1.5 text-xs">
                            {col && (
                              <col.icon className={cn("size-3", col.color)} />
                            )}
                            <span className={col?.color}>
                              {col?.label ?? "Unknown"}
                            </span>
                          </button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="border-border border-2">
                          {columns.map((c) => (
                            <DropdownMenuItem
                              key={c.id}
                              disabled={
                                apiStatusToColumn(task.status ?? "TODO") ===
                                c.id
                              }
                              onClick={() =>
                                handleMove(String(task.id), c.apiStatus)
                              }
                            >
                              <c.icon className={cn("mr-2 size-3", c.color)} />
                              {c.label}
                            </DropdownMenuItem>
                          ))}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                    <div>
                      <Badge variant={pCfg.badge} size="sm">
                        {pCfg.label}
                      </Badge>
                    </div>
                    <div>
                      {assignedTo ? (
                        <div className="flex items-center gap-1.5">
                          <Avatar className="border-border size-5 border">
                            <AvatarImage src={assignedTo.image ?? undefined} />
                            <AvatarFallback className="text-[10px]">
                              {(assignedTo.name ?? "U").charAt(0)}
                            </AvatarFallback>
                          </Avatar>
                          <span className="text-muted-foreground truncate text-xs">
                            {assignedTo.name ?? "User"}
                          </span>
                        </div>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
                      )}
                    </div>
                    <div className="text-right">
                      {equity > 0 ? (
                        <span className="text-neon-green text-xs font-medium">
                          {equity}
                        </span>
                      ) : (
                        <span className="text-muted-foreground text-xs">—</span>
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
