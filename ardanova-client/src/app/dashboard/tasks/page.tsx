"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  CheckSquare,
  Plus,
  LayoutGrid,
  List,
  Circle,
  Search,
  FolderKanban,
  User,
  Coins,
  ArrowUpDown,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { cn } from "~/lib/utils";
import { buildSignInHref } from "~/lib/auth-navigation";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { api } from "~/trpc/react";
import type { RouterOutputs } from "~/trpc/react";

// ─── Types ────────────────────────────────────────────────────────────────────

type ApiTask = RouterOutputs["task"]["getMyTasks"]["items"][number];
type AllApiTask = RouterOutputs["task"]["getAll"]["items"][number];

type TaskStatus = "TODO" | "IN_PROGRESS" | "REVIEW" | "COMPLETED" | "BLOCKED";
type TaskPriority = "LOW" | "MEDIUM" | "HIGH" | "URGENT";
type TaskType =
  | "FEATURE"
  | "BUG"
  | "ENHANCEMENT"
  | "DOCUMENTATION"
  | "RESEARCH"
  | "DESIGN"
  | "TESTING"
  | "REVIEW"
  | "MAINTENANCE"
  | "OTHER";

type NormalizedTask = {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  type: TaskType | null;
  project: { id: string; name: string };
  assignee: { id: string; name: string; avatar?: string } | null;
  dueDate: Date | null;
  equityReward: number;
  estimatedHours: number | null;
};

// ─── Badge variant maps ────────────────────────────────────────────────────────

const statusVariant: Record<TaskStatus, string> = {
  TODO: "outline",
  IN_PROGRESS: "neon",
  REVIEW: "neon-purple",
  COMPLETED: "neon-green",
  BLOCKED: "destructive",
};

const statusLabel: Record<TaskStatus, string> = {
  TODO: "To Do",
  IN_PROGRESS: "In Progress",
  REVIEW: "Review",
  COMPLETED: "Done",
  BLOCKED: "Blocked",
};

const priorityVariant: Record<TaskPriority, string> = {
  LOW: "secondary",
  MEDIUM: "outline",
  HIGH: "warning",
  URGENT: "destructive",
};

// ─── Helpers ──────────────────────────────────────────────────────────────────

function normalizeStatus(raw: string | null | undefined): TaskStatus {
  const u = (raw ?? "TODO").toUpperCase();
  if (u === "IN_PROGRESS") return "IN_PROGRESS";
  if (u === "REVIEW") return "REVIEW";
  if (u === "COMPLETED" || u === "DONE") return "COMPLETED";
  if (u === "BLOCKED") return "BLOCKED";
  return "TODO";
}

function normalizePriority(raw: string | null | undefined): TaskPriority {
  const u = (raw ?? "MEDIUM").toUpperCase();
  if (u === "LOW") return "LOW";
  if (u === "HIGH") return "HIGH";
  if (u === "URGENT" || u === "CRITICAL") return "URGENT";
  return "MEDIUM";
}

function normalizeType(raw: string | null | undefined): TaskType | null {
  if (!raw) return null;
  const u = raw.toUpperCase() as TaskType;
  const valid: TaskType[] = [
    "FEATURE",
    "BUG",
    "ENHANCEMENT",
    "DOCUMENTATION",
    "RESEARCH",
    "DESIGN",
    "TESTING",
    "REVIEW",
    "MAINTENANCE",
    "OTHER",
  ];
  return valid.includes(u) ? u : null;
}

function mapMyTask(task: ApiTask): NormalizedTask {
  const ext = task as ApiTask & {
    project?: { id: string; title: string };
    assignedTo?: { id: string; name?: string | null; image?: string | null };
    dueDate?: string | null;
    equityReward?: number | null;
    estimatedHours?: number | null;
  };

  return {
    id: String(task.id),
    title: String(task.title ?? ""),
    description: String(task.description ?? ""),
    status: normalizeStatus(String(task.status ?? "TODO")),
    priority: normalizePriority(task.priority),
    type: normalizeType(task.taskType),
    project: ext.project
      ? { id: String(ext.project.id), name: String(ext.project.title) }
      : { id: String(task.projectId), name: "Project" },
    assignee: ext.assignedTo
      ? {
          id: String(ext.assignedTo.id),
          name: String(ext.assignedTo.name ?? "User"),
          avatar: ext.assignedTo.image ?? undefined,
        }
      : null,
    dueDate: ext.dueDate ? new Date(ext.dueDate) : null,
    equityReward: typeof ext.equityReward === "number" ? ext.equityReward : 0,
    estimatedHours:
      typeof ext.estimatedHours === "number" ? ext.estimatedHours : null,
  };
}

function mapAllTask(task: AllApiTask): NormalizedTask {
  return mapMyTask(task);
}

function formatDue(date: Date | null): string {
  if (!date || Number.isNaN(date.getTime())) return "—";
  const now = new Date();
  const diff = Math.floor((date.getTime() - now.getTime()) / 86400000);
  if (diff < -1) return `${Math.abs(diff)}d overdue`;
  if (diff === -1) return "1d overdue";
  if (diff === 0) return "Today";
  if (diff === 1) return "Tomorrow";
  if (diff < 7) return `${diff}d left`;
  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

function isOverdue(task: NormalizedTask): boolean {
  return (
    !!task.dueDate && task.dueDate < new Date() && task.status !== "COMPLETED"
  );
}

// ─── Stat card ────────────────────────────────────────────────────────────────

function StatCard({
  label,
  value,
  accent,
}: {
  label: string;
  value: number;
  accent?: string;
}) {
  return (
    <div className="bg-card border-border flex min-w-[110px] items-center border-2 p-3">
      <div>
        <p
          className={cn(
            "font-mono text-xl leading-none font-bold",
            accent ?? "text-foreground",
          )}
        >
          {value}
        </p>
        <p className="text-muted-foreground mt-0.5 font-mono text-[10px] tracking-widest uppercase">
          {label}
        </p>
      </div>
    </div>
  );
}

// ─── Grid task card ───────────────────────────────────────────────────────────

function TaskGridCard({ task }: { task: NormalizedTask }) {
  const overdue = isOverdue(task);
  return (
    <div className="bg-card border-border hover:border-primary flex flex-col gap-3 border-2 p-4 transition-colors">
      {/* title */}
      <div>
        <p className="text-foreground line-clamp-2 text-sm leading-snug font-medium">
          {task.title}
        </p>
        <p className="text-muted-foreground mt-0.5 line-clamp-1 text-xs">
          {task.project.name}
        </p>
      </div>

      {/* badges */}
      <div className="flex flex-wrap gap-1">
        <Badge variant={statusVariant[task.status] as "outline"} size="sm">
          {statusLabel[task.status]}
        </Badge>
        <Badge variant={priorityVariant[task.priority] as "outline"} size="sm">
          {task.priority}
        </Badge>
        {task.type && (
          <Badge variant="secondary" size="sm">
            {task.type}
          </Badge>
        )}
      </div>

      {/* footer */}
      <div className="border-border/50 mt-auto flex items-center justify-between border-t pt-1">
        <div className="flex items-center gap-1.5">
          {task.assignee ? (
            <Avatar className="border-border size-5 border">
              <AvatarImage src={task.assignee.avatar} />
              <AvatarFallback className="text-[10px]">
                {task.assignee.name.charAt(0)}
              </AvatarFallback>
            </Avatar>
          ) : (
            <User className="text-muted-foreground size-4" />
          )}
          <span
            className={cn(
              "text-xs",
              overdue ? "text-destructive" : "text-muted-foreground",
            )}
          >
            {formatDue(task.dueDate)}
          </span>
        </div>
        <div className="flex items-center gap-1.5">
          {task.equityReward > 0 && (
            <span className="text-neon-green font-mono text-xs font-medium">
              {task.equityReward} units
            </span>
          )}
          {task.estimatedHours !== null && (
            <Badge variant="secondary" size="sm" className="px-1 text-[10px]">
              {task.estimatedHours}h estimate
            </Badge>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── List row ─────────────────────────────────────────────────────────────────

function TaskListRow({ task }: { task: NormalizedTask }) {
  const overdue = isOverdue(task);
  return (
    <tr className="border-border hover:bg-card/50 border-b transition-colors">
      <td className="p-3">
        <div className="text-foreground line-clamp-1 text-sm font-medium">
          {task.title}
        </div>
        {task.description && (
          <div className="text-muted-foreground line-clamp-1 text-xs">
            {task.description}
          </div>
        )}
      </td>
      <td className="p-3">
        <Badge variant="secondary" size="sm">
          {task.project.name}
        </Badge>
      </td>
      <td className="p-3">
        <Badge variant={statusVariant[task.status] as "outline"} size="sm">
          {statusLabel[task.status]}
        </Badge>
      </td>
      <td className="p-3">
        <Badge variant={priorityVariant[task.priority] as "outline"} size="sm">
          {task.priority}
        </Badge>
      </td>
      <td className="p-3">
        {task.type ? (
          <Badge variant="secondary" size="sm">
            {task.type}
          </Badge>
        ) : (
          <span className="text-muted-foreground text-xs">—</span>
        )}
      </td>
      <td className="p-3">
        {task.assignee ? (
          <div className="flex items-center gap-1.5">
            <Avatar className="size-5">
              <AvatarImage src={task.assignee.avatar} />
              <AvatarFallback className="text-[10px]">
                {task.assignee.name.charAt(0)}
              </AvatarFallback>
            </Avatar>
            <span className="text-muted-foreground text-xs">
              {task.assignee.name}
            </span>
          </div>
        ) : (
          <span className="text-muted-foreground text-xs">Unassigned</span>
        )}
      </td>
      <td className="p-3">
        <span
          className={cn(
            "font-mono text-xs",
            overdue ? "text-destructive" : "text-muted-foreground",
          )}
        >
          {formatDue(task.dueDate)}
        </span>
      </td>
      <td className="p-3">
        <span className="text-neon-green font-mono text-xs font-medium">
          {task.equityReward > 0 ? `${task.equityReward} units` : "—"}
        </span>
      </td>
    </tr>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

type ActiveTab = "mine" | "all";
type ViewMode = "list" | "grid";
type SortBy = "dueDate" | "priority" | "status" | "createdAt";

const PRIORITY_RANK: Record<TaskPriority, number> = {
  URGENT: 0,
  HIGH: 1,
  MEDIUM: 2,
  LOW: 3,
};
const STATUS_RANK: Record<TaskStatus, number> = {
  BLOCKED: 0,
  IN_PROGRESS: 1,
  REVIEW: 2,
  TODO: 3,
  COMPLETED: 4,
};

export default function DashboardTasksPage() {
  const { status: sessionStatus } = useSession();

  const [activeTab, setActiveTab] = useState<ActiveTab>("mine");
  const [viewMode, setViewMode] = useState<ViewMode>("list");

  // Filters
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "ALL">("ALL");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "ALL">(
    "ALL",
  );
  const [typeFilter, setTypeFilter] = useState<TaskType | "ALL">("ALL");
  const [sortBy, setSortBy] = useState<SortBy>("dueDate");

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => clearTimeout(t);
  }, [search]);

  // ── Queries ──────────────────────────────────────────────────────────────────

  const {
    data: myTasksData,
    isLoading: myLoading,
    error: myError,
    refetch: refetchMine,
  } = api.task.getMyTasks.useQuery(
    { limit: 50 },
    { enabled: sessionStatus === "authenticated" },
  );

  const {
    data: allTasksData,
    isLoading: allLoading,
    error: allError,
    refetch: refetchAll,
  } = api.task.getAll.useQuery(
    {
      limit: 50,
      search: debouncedSearch || undefined,
      status: statusFilter !== "ALL" ? statusFilter : undefined,
      priority: priorityFilter !== "ALL" ? priorityFilter : undefined,
      type: typeFilter !== "ALL" ? typeFilter : undefined,
    },
    { enabled: sessionStatus === "authenticated" && activeTab === "all" },
  );

  // ── Normalize ─────────────────────────────────────────────────────────────────

  const myTasks = useMemo(
    () => (myTasksData?.items ?? []).map(mapMyTask),
    [myTasksData],
  );

  const allTasks = useMemo(
    () => (allTasksData?.items ?? []).map(mapAllTask),
    [allTasksData],
  );

  // ── Active data set ───────────────────────────────────────────────────────────

  const rawTasks = useMemo(() => {
    if (activeTab === "mine") return myTasks;
    return allTasks;
  }, [activeTab, myTasks, allTasks]);

  const isLoading = activeTab === "mine" ? myLoading : allLoading;
  const activeError = activeTab === "mine" ? myError : allError;
  const refetchActive = activeTab === "mine" ? refetchMine : refetchAll;

  // ── Client-side filter for assigned tasks ─────────────────────────────────────

  const filtered = useMemo(() => {
    // For "all" tab, filtering is server-side; only sort client-side.
    let rows =
      activeTab === "all"
        ? rawTasks
        : rawTasks.filter((t) => {
            if (statusFilter !== "ALL" && t.status !== statusFilter)
              return false;
            if (priorityFilter !== "ALL" && t.priority !== priorityFilter)
              return false;
            if (typeFilter !== "ALL" && t.type !== typeFilter) return false;
            if (debouncedSearch) {
              const q = debouncedSearch.toLowerCase();
              if (
                !t.title.toLowerCase().includes(q) &&
                !t.project.name.toLowerCase().includes(q)
              )
                return false;
            }
            return true;
          });

    // Sort
    rows = [...rows].sort((a, b) => {
      switch (sortBy) {
        case "priority":
          return PRIORITY_RANK[a.priority] - PRIORITY_RANK[b.priority];
        case "status":
          return STATUS_RANK[a.status] - STATUS_RANK[b.status];
        case "dueDate": {
          if (!a.dueDate && !b.dueDate) return 0;
          if (!a.dueDate) return 1;
          if (!b.dueDate) return -1;
          return a.dueDate.getTime() - b.dueDate.getTime();
        }
        default:
          return 0;
      }
    });

    return rows;
  }, [
    rawTasks,
    activeTab,
    statusFilter,
    priorityFilter,
    typeFilter,
    debouncedSearch,
    sortBy,
  ]);

  // ── Stats ─────────────────────────────────────────────────────────────────────

  const stats = useMemo(() => {
    const base = myTasks; // stats always from "my tasks"
    const now = new Date();
    const weekAgo = new Date(now.getTime() - 7 * 86400000);
    return {
      total: base.length,
      inProgress: base.filter((t) => t.status === "IN_PROGRESS").length,
      blocked: base.filter((t) => t.status === "BLOCKED").length,
      completedThisWeek: base.filter(
        (t) => t.status === "COMPLETED" && t.dueDate && t.dueDate >= weekAgo,
      ).length,
    };
  }, [myTasks]);

  // ── Tab config ────────────────────────────────────────────────────────────────

  const tabs: { id: ActiveTab; label: string }[] = [
    { id: "mine", label: "My Tasks" },
    { id: "all", label: "Workspace Tasks" },
  ];

  // ── Auth guard ────────────────────────────────────────────────────────────────

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground font-mono text-sm">Loading…</p>
      </div>
    );
  }

  if (sessionStatus === "unauthenticated") {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Sign in to view your tasks.</p>
        <Button asChild variant="neon">
          <Link href={buildSignInHref("/dashboard/tasks")}>Sign in</Link>
        </Button>
      </div>
    );
  }

  // ── Render ────────────────────────────────────────────────────────────────────

  return (
    <div className="bg-background min-h-screen">
      {/* ── Sticky header ── */}
      <div className="border-border bg-background relative z-10 border-b-2">
        <div className="p-4 pb-0">
          {/* Title row */}
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h1 className="text-foreground flex items-center gap-2 font-mono text-2xl font-bold">
                <CheckSquare className="text-primary size-6" />
                Tasks
                <span className="bg-primary ml-1 inline-block h-5 w-3 animate-pulse" />
              </h1>
              <p className="text-muted-foreground mt-0.5 text-sm">
                Track your work across all projects
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              {/* View toggle */}
              <div
                className="border-border flex border-2"
                role="group"
                aria-label="Task view"
              >
                <button
                  type="button"
                  onClick={() => setViewMode("list")}
                  className={cn(
                    "min-h-11 min-w-11 px-3 py-1.5 transition-colors",
                    viewMode === "list"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-card",
                  )}
                  aria-label="List view"
                  aria-pressed={viewMode === "list"}
                >
                  <List className="size-4" />
                </button>
                <button
                  type="button"
                  onClick={() => setViewMode("grid")}
                  className={cn(
                    "border-border min-h-11 min-w-11 border-l-2 px-3 py-1.5 transition-colors",
                    viewMode === "grid"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-card",
                  )}
                  aria-label="Grid view"
                  aria-pressed={viewMode === "grid"}
                >
                  <LayoutGrid className="size-4" />
                </button>
              </div>

              <Button asChild variant="neon" size="sm">
                <Link href="/tasks/create">
                  <Plus className="mr-2 size-4" />
                  New Task
                </Link>
              </Button>
            </div>
          </div>

          {/* Tabs */}
          <div
            className="flex gap-0 overflow-x-auto border-b-0"
            role="tablist"
            aria-label="Task scope"
            onKeyDown={handleTabListKeyDown}
          >
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                role="tab"
                id={`dashboard-tasks-tab-${tab.id}`}
                aria-selected={activeTab === tab.id}
                aria-controls={`dashboard-tasks-panel-${tab.id}`}
                tabIndex={activeTab === tab.id ? 0 : -1}
                className={cn(
                  "min-h-11 border-b-2 px-4 py-2 font-mono text-sm whitespace-nowrap transition-colors",
                  activeTab === tab.id
                    ? "text-primary border-primary"
                    : "text-muted-foreground hover:text-foreground hover:border-border border-transparent",
                )}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {/* Filter bar */}
        <div className="bg-background grid grid-cols-1 gap-2 px-4 py-3 sm:grid-cols-2 lg:flex lg:flex-wrap lg:items-center">
          {/* Search */}
          <div className="relative min-w-0">
            <label htmlFor="dashboard-task-search" className="sr-only">
              Search tasks
            </label>
            <Search className="text-muted-foreground absolute top-1/2 left-3 size-3.5 -translate-y-1/2" />
            <input
              type="text"
              id="dashboard-task-search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search tasks…"
              className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary min-h-11 w-full border-2 py-1.5 pr-4 pl-9 text-sm focus:outline-none lg:w-52"
            />
          </div>

          {/* Status */}
          <Select
            value={statusFilter}
            onValueChange={(v) => setStatusFilter(v as TaskStatus | "ALL")}
          >
            <SelectTrigger
              aria-label="Filter by status"
              className="min-h-11 w-full border-2 text-xs lg:w-36"
            >
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">All Statuses</SelectItem>
              <SelectItem value="TODO">To Do</SelectItem>
              <SelectItem value="IN_PROGRESS">In Progress</SelectItem>
              <SelectItem value="REVIEW">Review</SelectItem>
              <SelectItem value="COMPLETED">Completed</SelectItem>
              <SelectItem value="BLOCKED">Blocked</SelectItem>
            </SelectContent>
          </Select>

          {/* Priority */}
          <Select
            value={priorityFilter}
            onValueChange={(v) => setPriorityFilter(v as TaskPriority | "ALL")}
          >
            <SelectTrigger
              aria-label="Filter by priority"
              className="min-h-11 w-full border-2 text-xs lg:w-36"
            >
              <SelectValue placeholder="Priority" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">All Priorities</SelectItem>
              <SelectItem value="LOW">Low</SelectItem>
              <SelectItem value="MEDIUM">Medium</SelectItem>
              <SelectItem value="HIGH">High</SelectItem>
              <SelectItem value="URGENT">Urgent</SelectItem>
            </SelectContent>
          </Select>

          {/* Type */}
          <Select
            value={typeFilter}
            onValueChange={(v) => setTypeFilter(v as TaskType | "ALL")}
          >
            <SelectTrigger
              aria-label="Filter by task type"
              className="min-h-11 w-full border-2 text-xs lg:w-40"
            >
              <SelectValue placeholder="Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">All Types</SelectItem>
              <SelectItem value="FEATURE">Feature</SelectItem>
              <SelectItem value="BUG">Bug</SelectItem>
              <SelectItem value="ENHANCEMENT">Enhancement</SelectItem>
              <SelectItem value="DOCUMENTATION">Documentation</SelectItem>
              <SelectItem value="RESEARCH">Research</SelectItem>
              <SelectItem value="DESIGN">Design</SelectItem>
              <SelectItem value="TESTING">Testing</SelectItem>
              <SelectItem value="REVIEW">Review</SelectItem>
              <SelectItem value="MAINTENANCE">Maintenance</SelectItem>
              <SelectItem value="OTHER">Other</SelectItem>
            </SelectContent>
          </Select>

          {/* Sort */}
          <div className="flex min-w-0 items-center gap-1.5 lg:ml-auto">
            <ArrowUpDown className="text-muted-foreground size-3.5" />
            <Select
              value={sortBy}
              onValueChange={(v) => setSortBy(v as SortBy)}
            >
              <SelectTrigger
                aria-label="Sort tasks"
                className="min-h-11 w-full border-2 text-xs lg:w-36"
              >
                <SelectValue placeholder="Sort by" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="dueDate">Due Date</SelectItem>
                <SelectItem value="priority">Priority</SelectItem>
                <SelectItem value="status">Status</SelectItem>
                <SelectItem value="createdAt">Created Date</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* ── Stats bar ── */}
      {!activeError && (
        <div className="border-border bg-background/80 flex flex-wrap gap-3 border-b-2 px-4 py-3">
          <StatCard label="Total" value={stats.total} />
          <StatCard
            label="In Progress"
            value={stats.inProgress}
            accent="text-neon"
          />
          <StatCard
            label="Blocked"
            value={stats.blocked}
            accent="text-destructive"
          />
          <StatCard
            label="Done This Week"
            value={stats.completedThisWeek}
            accent="text-neon-green"
          />
          <div className="text-muted-foreground ml-auto flex items-center gap-2 self-center font-mono text-xs">
            <FolderKanban className="size-3.5" />
            <span>
              Showing{" "}
              <span className="text-foreground font-semibold">
                {filtered.length}
              </span>{" "}
              tasks
            </span>
          </div>
        </div>
      )}

      {/* ── Content ── */}
      <div
        className="p-4"
        role="tabpanel"
        id={`dashboard-tasks-panel-${activeTab}`}
        aria-labelledby={`dashboard-tasks-tab-${activeTab}`}
      >
        {isLoading ? (
          <div className="text-muted-foreground py-16 text-center font-mono text-sm">
            Loading tasks…
          </div>
        ) : activeError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 border-2 p-8 text-center"
          >
            <p className="text-destructive font-medium">
              Tasks could not be loaded.
            </p>
            <Button
              type="button"
              variant="outline"
              className="mt-4"
              onClick={() => void refetchActive()}
            >
              Retry
            </Button>
          </div>
        ) : filtered.length === 0 ? (
          <div className="border-border border-2 border-dashed py-16 text-center">
            <Circle className="text-border mx-auto mb-3 size-8" />
            <p className="text-muted-foreground font-mono text-sm">
              No tasks found
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              Try adjusting your filters or{" "}
              <Link href="/tasks/create" className="text-primary underline">
                create a task
              </Link>
            </p>
          </div>
        ) : viewMode === "grid" ? (
          /* ── Grid view ── */
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {filtered.map((task) => (
              <TaskGridCard key={task.id} task={task} />
            ))}
          </div>
        ) : (
          /* ── List view ── */
          <>
            <div className="grid grid-cols-1 gap-4 md:hidden">
              {filtered.map((task) => (
                <TaskGridCard key={task.id} task={task} />
              ))}
            </div>
            <Card className="hidden md:block">
              <CardContent className="p-0">
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[800px]">
                    <thead>
                      <tr className="border-border border-b-2">
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Title
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Project
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Status
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Priority
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Type
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Assignee
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          Due
                        </th>
                        <th className="text-muted-foreground p-3 text-left font-mono text-xs tracking-widest uppercase">
                          <div className="flex items-center gap-1">
                            <Coins className="size-3" />
                            Project-token allocation
                          </div>
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {filtered.map((task) => (
                        <TaskListRow key={task.id} task={task} />
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </div>
  );
}
