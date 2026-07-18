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
  MoreHorizontal,
  Search,
  FolderKanban,
  Zap,
  Target,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { buildSignInHref } from "~/lib/auth-navigation";
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
import { TaskEconomicState } from "~/components/tasks/task-economic-state";

type ApiTask = RouterOutputs["task"]["getMyTasks"]["items"][number];
type ApiPbi = RouterOutputs["backlog"]["getPbisByProjectId"][number];

type TaskColumnId = "todo" | "in_progress" | "review" | "done";

const taskStatuses: {
  id: TaskColumnId;
  label: string;
  color: string;
  bgColor: string;
}[] = [
  {
    id: "todo",
    label: "To Do",
    color: "text-muted-foreground",
    bgColor: "bg-muted/50",
  },
  {
    id: "in_progress",
    label: "In Progress",
    color: "text-neon",
    bgColor: "bg-neon/10",
  },
  {
    id: "review",
    label: "In Review",
    color: "text-neon-purple",
    bgColor: "bg-neon-purple/10",
  },
  {
    id: "done",
    label: "Done",
    color: "text-neon-green",
    bgColor: "bg-neon-green/10",
  },
];

const priorityConfig = {
  urgent: {
    label: "Urgent",
    color: "text-destructive",
    badge: "destructive" as const,
  },
  high: { label: "High", color: "text-neon-pink", badge: "neon-pink" as const },
  medium: { label: "Medium", color: "text-warning", badge: "warning" as const },
  low: {
    label: "Low",
    color: "text-muted-foreground",
    badge: "secondary" as const,
  },
};

function apiStatusToColumn(status: string): TaskColumnId {
  const u = status.toUpperCase();
  if (u === "COMPLETED") return "done";
  if (u === "IN_PROGRESS") return "in_progress";
  if (u === "REVIEW") return "review";
  if (u === "BLOCKED") return "todo";
  return "todo";
}

function columnToApiStatus(
  col: TaskColumnId,
): "TODO" | "IN_PROGRESS" | "REVIEW" | "COMPLETED" | "BLOCKED" {
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

function mapPriority(
  p: string | null | undefined,
): keyof typeof priorityConfig {
  const u = (p ?? "MEDIUM").toUpperCase();
  if (u === "URGENT" || u === "CRITICAL") return "urgent";
  if (u === "HIGH") return "high";
  if (u === "LOW") return "low";
  return "medium";
}

type WorkItemKind = "task" | "pbi";

type TaskRow = {
  id: string;
  kind: WorkItemKind;
  title: string;
  description: string;
  column: TaskColumnId;
  priority: keyof typeof priorityConfig;
  project: { id: string; name: string; color: string };
  assignee: { id: string; name: string; avatar?: string } | null;
  dueDate: Date | null;
  reward: number;
  escrowStatus?: string | null;
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

  const due = task.dueDate;
  const equity = task.equityReward;
  const hours = task.estimatedHours;
  const escrowStatus = task.escrowStatus;

  return {
    id: String(task.id),
    kind: "task",
    title: String(task.title ?? ""),
    description: task.description ?? "",
    column: apiStatusToColumn(String(task.status ?? "TODO")),
    priority: mapPriority(task.priority),
    project: {
      ...project,
      id: String(project.id),
      name: String(project.name),
    },
    assignee,
    dueDate: due ? new Date(due) : null,
    reward: typeof equity === "number" ? Number(equity) : 0,
    escrowStatus,
    tags: task.taskType ? [String(task.taskType)] : [],
    storyPoints:
      typeof hours === "number" && hours > 0
        ? Math.min(13, Math.max(1, Math.round(hours)))
        : 5,
  };
}

function pbiStatusToColumn(status: string): TaskColumnId | null {
  const u = status.toUpperCase();
  if (u === "DONE") return "done";
  if (u === "IN_PROGRESS") return "in_progress";
  if (u === "CANCELLED") return null; // exclude cancelled
  // NEW and READY both map to todo
  return "todo";
}

function mapPbiToRow(pbi: ApiPbi): TaskRow | null {
  const column = pbiStatusToColumn(String(pbi.status ?? "NEW"));
  if (column === null) return null; // exclude CANCELLED
  return {
    id: String(pbi.id),
    kind: "pbi",
    title: String(pbi.title ?? ""),
    description: pbi.description ?? "",
    column,
    priority: mapPriority(pbi.priority),
    project: {
      id: String(pbi.projectId ?? ""),
      name: "Project",
      color: "neon-yellow",
    },
    assignee: null,
    dueDate: null,
    reward: 0,
    escrowStatus: null,
    tags: pbi.type ? [String(pbi.type)] : [],
    storyPoints: typeof pbi.storyPoints === "number" ? pbi.storyPoints : 3,
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
        "bg-card border-border cursor-grab border-2 p-3 transition-all active:cursor-grabbing",
        isDragging && "rotate-2 opacity-50",
        "hover:border-primary",
      )}
    >
      <div className="mb-2 flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <Badge
            variant={task.kind === "pbi" ? "neon-purple" : "info"}
            size="sm"
            className="text-[10px]"
          >
            {task.kind === "pbi" ? "PBI" : "Task"}
          </Badge>
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
            <Button
              variant="ghost"
              size="icon"
              className="size-11"
              aria-label={`Task actions for ${task.title}`}
            >
              <MoreHorizontal className="size-4" aria-hidden="true" />
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

      <h4 className="text-foreground mb-1 line-clamp-2 text-sm font-medium">
        {task.title}
      </h4>
      <p className="text-muted-foreground mb-3 line-clamp-2 text-xs">
        {task.description}
      </p>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {task.assignee ? (
            <Avatar className="border-border size-6 border">
              <AvatarImage src={task.assignee.avatar} />
              <AvatarFallback className="text-xs">
                {task.assignee.name.charAt(0)}
              </AvatarFallback>
            </Avatar>
          ) : (
            <div className="border-border flex size-6 items-center justify-center rounded-none border-2 border-dashed">
              <Plus className="text-muted-foreground size-3" />
            </div>
          )}
          <span
            className={cn(
              "text-xs",
              isOverdue ? "text-destructive" : "text-muted-foreground",
            )}
          >
            {formatDueDate(task.dueDate)}
          </span>
        </div>
        <div className="text-muted-foreground flex items-center gap-2 text-xs">
          <span className="text-neon-green font-medium">
            {task.reward} tokens
          </span>
          <TaskEconomicState
            allocationUnits={task.reward}
            escrowStatus={task.escrowStatus}
          />
          <Badge variant="secondary" size="sm" className="px-1 text-[10px]">
            {task.storyPoints} SP
          </Badge>
        </div>
      </div>
    </div>
  );
}

export default function TasksPage() {
  const { status: sessionStatus } = useSession();
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [viewMode, setViewMode] = useState<"board" | "list">("board");
  const [itemFilter, setItemFilter] = useState<"all" | "task" | "pbi">("all");
  const [selectedProject, setSelectedProject] = useState(
    () => searchParams.get("project") ?? "all",
  );
  const [searchQuery, setSearchQuery] = useState(
    () => searchParams.get("q") ?? "",
  );

  useEffect(() => {
    const params = new URLSearchParams();
    const t = searchQuery.trim();
    if (t) params.set("q", t);
    if (selectedProject !== "all") params.set("project", selectedProject);
    const qs = params.toString();
    const next = qs ? `${pathname}?${qs}` : pathname;
    router.replace(next, { scroll: false });
  }, [searchQuery, selectedProject, pathname, router]);

  const {
    data: tasksData,
    isLoading: tasksLoading,
    error,
    refetch,
  } = api.task.getMyTasks.useQuery(
    { limit: 100 },
    { enabled: sessionStatus === "authenticated" },
  );

  // Collect unique project IDs from user's tasks for PBI fetching
  const projectIds = useMemo(() => {
    const ids = new Set<string>();
    (tasksData?.items ?? []).forEach((t) => {
      if (t.projectId) ids.add(String(t.projectId));
    });
    return Array.from(ids);
  }, [tasksData?.items]);

  // Fetch PBIs for the user's projects using imperative fetch to avoid dynamic hook count
  const [pbiRows, setPbiRows] = useState<TaskRow[]>([]);
  const [pbisLoading, setPbisLoading] = useState(false);
  const [pbiFetchKey, setPbiFetchKey] = useState(0);
  const isLoading = tasksLoading || pbisLoading;

  const utils = api.useUtils();

  useEffect(() => {
    if (projectIds.length === 0) {
      setPbiRows([]);
      return;
    }
    let cancelled = false;
    setPbisLoading(true);
    void Promise.all(
      projectIds.map((pid) =>
        utils.backlog.getPbisByProjectId
          .fetch({ projectId: pid })
          .catch((): ApiPbi[] => []),
      ),
    )
      .then((results) => {
        if (cancelled) return;
        const mapped = results
          .flat()
          .map(mapPbiToRow)
          .filter((r): r is TaskRow => r !== null);
        setPbiRows(mapped);
      })
      .finally(() => {
        if (!cancelled) setPbisLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectIds, pbiFetchKey]);

  const updateTaskStatus = api.task.updateStatus.useMutation({
    onSuccess: () => {
      void utils.task.getMyTasks.invalidate();
      toast.success("Task updated");
    },
    onError: (e) => toast.error(e.message),
  });

  const updatePbiStatus = api.backlog.updatePbiStatus.useMutation({
    onSuccess: () => {
      setPbiFetchKey((k) => k + 1); // re-trigger PBI fetch
      toast.success("PBI updated");
    },
    onError: (e) => toast.error(e.message),
  });

  const taskRows = useMemo(
    () => (tasksData?.items ?? []).map(mapApiTaskToRow),
    [tasksData?.items],
  );
  const rows = useMemo(() => [...taskRows, ...pbiRows], [taskRows, pbiRows]);

  const filteredRows = useMemo(() => {
    return rows.filter((task) => {
      if (itemFilter !== "all" && task.kind !== itemFilter) return false;
      if (selectedProject !== "all" && task.project.id !== selectedProject)
        return false;
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
  }, [rows, selectedProject, searchQuery, itemFilter]);

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
    const inProgress = filteredRows.filter(
      (t) => t.column === "in_progress",
    ).length;
    const totalPoints = filteredRows.reduce((sum, t) => sum + t.storyPoints, 0);
    const completedPoints = filteredRows
      .filter((t) => t.column === "done")
      .reduce((sum, t) => sum + t.storyPoints, 0);
    return { total, completed, inProgress, totalPoints, completedPoints };
  }, [filteredRows]);

  const projects = useMemo(() => {
    const map = new Map<string, { id: string; name: string; color: string }>();
    rows.forEach((t) => map.set(t.project.id, t.project));
    return Array.from(map.values());
  }, [rows]);

  const handleMove = (itemId: string, column: TaskColumnId) => {
    // Determine if this is a PBI or task by checking against known PBI rows
    const item = rows.find((r) => r.id === itemId);
    if (item?.kind === "pbi") {
      const pbiStatusMap: Record<
        TaskColumnId,
        "NEW" | "READY" | "IN_PROGRESS" | "DONE"
      > = {
        todo: "NEW",
        in_progress: "IN_PROGRESS",
        review: "READY",
        done: "DONE",
      };
      updatePbiStatus.mutate({ id: itemId, status: pbiStatusMap[column] });
    } else {
      updateTaskStatus.mutate({
        id: itemId,
        status: columnToApiStatus(column),
      });
    }
  };

  if (sessionStatus === "loading") {
    return (
      <div className="bg-background flex min-h-screen items-center justify-center">
        <p className="text-muted-foreground">Loading…</p>
      </div>
    );
  }

  if (sessionStatus === "unauthenticated") {
    return (
      <div className="bg-background flex min-h-screen flex-col items-center justify-center gap-4 p-6">
        <p className="text-muted-foreground">Sign in to view your tasks.</p>
        <Button asChild variant="neon">
          <Link href={buildSignInHref("/tasks")}>Sign in</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="border-border bg-background relative z-10 border-b-2">
        <div className="p-4">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-3 sm:gap-4">
              <h1 className="text-foreground flex items-center gap-2 text-xl font-bold">
                <CheckSquare className="text-primary size-5" />
                My Tasks
              </h1>
              <div
                className="border-border flex items-center gap-2 sm:border-l-2 sm:pl-4"
                role="group"
                aria-label="Task view"
              >
                <Button
                  variant={viewMode === "board" ? "secondary" : "ghost"}
                  size="sm"
                  onClick={() => setViewMode("board")}
                  aria-pressed={viewMode === "board"}
                >
                  <LayoutGrid className="mr-1 size-4" />
                  Board
                </Button>
                <Button
                  variant={viewMode === "list" ? "secondary" : "ghost"}
                  size="sm"
                  onClick={() => setViewMode("list")}
                  aria-pressed={viewMode === "list"}
                >
                  <List className="mr-1 size-4" />
                  List
                </Button>
              </div>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="neon" size="sm" className="w-full sm:w-auto">
                  <Plus className="mr-2 size-4" />
                  New Item
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem asChild>
                  <Link href="/tasks/create?type=task">
                    <CheckSquare className="mr-2 size-4" />
                    New Task
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/tasks/create?type=pbi">
                    <Zap className="mr-2 size-4" />
                    New PBI
                  </Link>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:flex lg:items-center lg:gap-4">
            <div className="relative min-w-0 lg:max-w-xs lg:flex-1">
              <label htmlFor="tasks-search" className="sr-only">
                Search tasks
              </label>
              <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
              <input
                type="text"
                id="tasks-search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search tasks..."
                className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary w-full border-2 py-2 pr-4 pl-10 text-sm focus:outline-none"
              />
            </div>
            <Select
              value={itemFilter}
              onValueChange={(v) => setItemFilter(v as "all" | "task" | "pbi")}
            >
              <SelectTrigger
                aria-label="Filter by work item type"
                className="w-full border-2 lg:w-36"
              >
                <SelectValue placeholder="All Items" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Items</SelectItem>
                <SelectItem value="task">Tasks Only</SelectItem>
                <SelectItem value="pbi">PBIs Only</SelectItem>
              </SelectContent>
            </Select>
            <Select value={selectedProject} onValueChange={setSelectedProject}>
              <SelectTrigger
                aria-label="Filter by project"
                className="w-full border-2 lg:w-48"
              >
                <FolderKanban className="text-muted-foreground mr-2 size-4" />
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
            <Button
              variant="outline"
              size="sm"
              type="button"
              onClick={() => void refetch()}
              className="w-full sm:w-auto"
            >
              <Filter className="mr-2 size-4" />
              Refresh
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 px-4 pb-4 sm:grid-cols-2 lg:flex lg:items-center lg:gap-6">
          <div className="flex min-w-0 items-center gap-2 text-sm">
            <Target className="text-muted-foreground size-4" />
            <span className="text-muted-foreground">Sprint Progress:</span>
            <span className="text-foreground font-medium">
              {stats.completedPoints}/{stats.totalPoints || 1} SP
            </span>
          </div>
          <Progress
            aria-label="Sprint progress"
            value={
              stats.totalPoints
                ? (stats.completedPoints / stats.totalPoints) * 100
                : 0
            }
            variant="neon"
            className="h-2 w-full lg:max-w-xs lg:flex-1"
          />
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm sm:col-span-2 lg:col-span-1">
            <span className="text-muted-foreground">
              <span className="text-foreground font-medium">
                {stats.completed}
              </span>{" "}
              completed
            </span>
            <span className="text-muted-foreground">
              <span className="text-neon font-medium">{stats.inProgress}</span>{" "}
              in progress
            </span>
            <span className="text-muted-foreground">
              <span className="text-foreground font-medium">
                {stats.total - stats.completed - stats.inProgress}
              </span>{" "}
              remaining
            </span>
          </div>
        </div>
      </div>

      {isLoading ? (
        <div className="text-muted-foreground p-8 text-center">
          Loading tasks…
        </div>
      ) : error ? (
        <div role="alert" className="text-destructive p-4 text-sm">
          Tasks could not be loaded. {error.message}{" "}
          <button
            type="button"
            className="min-h-11 underline"
            onClick={() => void refetch()}
          >
            Retry
          </button>
        </div>
      ) : filteredRows.length === 0 ? (
        <div className="text-muted-foreground p-8 text-center">
          No tasks match the current view.
        </div>
      ) : viewMode === "board" ? (
        <div className="p-4">
          <div className="flex gap-4 overflow-x-auto pb-4">
            {taskStatuses.map((status) => {
              const tasks = tasksByStatus[status.id] || [];

              return (
                <div
                  key={status.id}
                  className="w-[min(20rem,calc(100vw-2rem))] flex-shrink-0"
                >
                  <div
                    className={cn(
                      "border-border mb-3 flex items-center justify-between border-2 p-3",
                      status.bgColor,
                    )}
                  >
                    <div className="flex items-center gap-2">
                      <span className={cn("font-medium", status.color)}>
                        {status.label}
                      </span>
                      <Badge variant="secondary" size="sm">
                        {tasks.length}
                      </Badge>
                    </div>
                  </div>

                  <div className="min-h-[200px] space-y-3">
                    {tasks.map((task) => (
                      <TaskCard key={task.id} task={task} onMove={handleMove} />
                    ))}
                    {tasks.length === 0 && (
                      <div className="border-border text-muted-foreground border-2 border-dashed p-4 text-center text-sm">
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
          <div className="space-y-3 md:hidden">
            {filteredRows.map((task) => (
              <TaskCard key={task.id} task={task} onMove={handleMove} />
            ))}
          </div>
          <Card className="hidden md:block">
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[800px]">
                  <thead>
                    <tr className="border-border border-b-2">
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Type
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Task
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Project
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Status
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Priority
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Assignee
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Due Date
                      </th>
                      <th className="text-muted-foreground p-3 text-left text-sm font-medium">
                        Reward
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredRows.map((task) => {
                      const status = taskStatuses.find(
                        (s) => s.id === task.column,
                      );
                      const priority = priorityConfig[task.priority];
                      const isOverdue =
                        task.dueDate &&
                        task.dueDate < new Date() &&
                        task.column !== "done";

                      return (
                        <tr
                          key={task.id}
                          className="border-border hover:bg-card/50 border-b transition-colors"
                        >
                          <td className="p-3">
                            <Badge
                              variant={
                                task.kind === "pbi" ? "neon-purple" : "info"
                              }
                              size="sm"
                              className="text-[10px]"
                            >
                              {task.kind === "pbi" ? "PBI" : "Task"}
                            </Badge>
                          </td>
                          <td className="p-3">
                            <div className="text-foreground text-sm font-medium">
                              {task.title}
                            </div>
                            <div className="text-muted-foreground line-clamp-1 text-xs">
                              {task.description}
                            </div>
                          </td>
                          <td className="p-3">
                            <Badge variant="secondary" size="sm">
                              {task.project.name}
                            </Badge>
                          </td>
                          <td className="p-3">
                            <Badge
                              variant="secondary"
                              size="sm"
                              className={status?.color}
                            >
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
                                  <AvatarFallback className="text-xs">
                                    {task.assignee.name.charAt(0)}
                                  </AvatarFallback>
                                </Avatar>
                                <span className="text-muted-foreground text-sm">
                                  {task.assignee.name}
                                </span>
                              </div>
                            ) : (
                              <span className="text-muted-foreground text-sm">
                                Unassigned
                              </span>
                            )}
                          </td>
                          <td className="p-3">
                            <span
                              className={cn(
                                "text-sm",
                                isOverdue
                                  ? "text-destructive"
                                  : "text-muted-foreground",
                              )}
                            >
                              {formatDueDate(task.dueDate)}
                            </span>
                          </td>
                          <td className="p-3">
                            <div className="flex items-center gap-2">
                              <span className="text-neon-green text-sm font-medium">
                                {task.reward} tokens
                              </span>
                              <TaskEconomicState
                                allocationUnits={task.reward}
                                escrowStatus={task.escrowStatus}
                              />
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
