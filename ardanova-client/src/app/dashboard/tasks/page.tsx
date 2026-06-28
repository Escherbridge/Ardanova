"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  CheckSquare,
  Plus,
  LayoutGrid,
  List,
  Calendar,
  AlertCircle,
  CheckCircle2,
  Circle,
  Search,
  FolderKanban,
  Timer,
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
  effortEstimate: string | null;
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
    "FEATURE", "BUG", "ENHANCEMENT", "DOCUMENTATION",
    "RESEARCH", "DESIGN", "TESTING", "REVIEW", "MAINTENANCE", "OTHER",
  ];
  return valid.includes(u) ? u : null;
}

function mapMyTask(task: ApiTask): NormalizedTask {
  const ext = task as ApiTask & {
    project?: { id: string; title: string };
    assignedTo?: { id: string; name?: string | null; image?: string | null };
    dueDate?: string | null;
    equityReward?: number | null;
    effortEstimate?: string | null;
  };

  return {
    id: String(task.id),
    title: String(task.title ?? ""),
    description: String((task as { description?: string | null }).description ?? ""),
    status: normalizeStatus(String(task.status ?? "TODO")),
    priority: normalizePriority(task.priority as string | null),
    type: normalizeType(task.taskType as string | null),
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
    effortEstimate: ext.effortEstimate ? String(ext.effortEstimate) : null,
  };
}

function mapAllTask(task: AllApiTask): NormalizedTask {
  return mapMyTask(task as unknown as ApiTask);
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
  return !!task.dueDate && task.dueDate < new Date() && task.status !== "COMPLETED";
}

// ─── Stat card ────────────────────────────────────────────────────────────────

function StatCard({
  label,
  value,
  icon: Icon,
  accent,
}: {
  label: string;
  value: number;
  icon: typeof Circle;
  accent?: string;
}) {
  return (
    <div className="bg-card border-2 border-border p-3 flex items-center gap-3 min-w-[110px]">
      <Icon className={cn("size-4 shrink-0", accent ?? "text-muted-foreground")} />
      <div>
        <p className={cn("text-xl font-bold font-mono leading-none", accent ?? "text-foreground")}>
          {value}
        </p>
        <p className="text-[10px] font-mono uppercase tracking-widest text-muted-foreground mt-0.5">
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
    <div className="bg-card border-2 border-border hover:border-primary transition-colors p-4 flex flex-col gap-3">
      {/* title */}
      <div>
        <p className="font-medium text-sm text-foreground line-clamp-2 leading-snug">
          {task.title}
        </p>
        <p className="text-xs text-muted-foreground mt-0.5 line-clamp-1">
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
      <div className="flex items-center justify-between mt-auto pt-1 border-t border-border/50">
        <div className="flex items-center gap-1.5">
          {task.assignee ? (
            <Avatar className="size-5 border border-border">
              <AvatarImage src={task.assignee.avatar} />
              <AvatarFallback className="text-[10px]">
                {task.assignee.name.charAt(0)}
              </AvatarFallback>
            </Avatar>
          ) : (
            <User className="size-4 text-muted-foreground" />
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
            <span className="text-xs text-neon-green font-mono font-medium">
              {task.equityReward}t
            </span>
          )}
          {task.effortEstimate && (
            <Badge variant="secondary" size="sm" className="text-[10px] px-1">
              {task.effortEstimate}
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
    <tr className="border-b border-border hover:bg-card/50 transition-colors">
      <td className="p-3">
        <div className="font-medium text-sm text-foreground line-clamp-1">{task.title}</div>
        {task.description && (
          <div className="text-xs text-muted-foreground line-clamp-1">{task.description}</div>
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
          <span className="text-xs text-muted-foreground">—</span>
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
            <span className="text-xs text-muted-foreground">{task.assignee.name}</span>
          </div>
        ) : (
          <span className="text-xs text-muted-foreground">Unassigned</span>
        )}
      </td>
      <td className="p-3">
        <span className={cn("text-xs font-mono", overdue ? "text-destructive" : "text-muted-foreground")}>
          {formatDue(task.dueDate)}
        </span>
      </td>
      <td className="p-3">
        <span className="text-xs text-neon-green font-mono font-medium">
          {task.equityReward > 0 ? `${task.equityReward}t` : "—"}
        </span>
      </td>
    </tr>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

type ActiveTab = "mine" | "assigned_by_me" | "all";
type ViewMode = "list" | "grid";
type SortBy = "dueDate" | "priority" | "status" | "createdAt";

const PRIORITY_RANK: Record<TaskPriority, number> = { URGENT: 0, HIGH: 1, MEDIUM: 2, LOW: 3 };
const STATUS_RANK: Record<TaskStatus, number> = {
  BLOCKED: 0, IN_PROGRESS: 1, REVIEW: 2, TODO: 3, COMPLETED: 4,
};

export default function DashboardTasksPage() {
  const { status: sessionStatus } = useSession();

  const [activeTab, setActiveTab] = useState<ActiveTab>("mine");
  const [viewMode, setViewMode] = useState<ViewMode>("list");

  // Filters
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "ALL">("ALL");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "ALL">("ALL");
  const [typeFilter, setTypeFilter] = useState<TaskType | "ALL">("ALL");
  const [sortBy, setSortBy] = useState<SortBy>("dueDate");

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => clearTimeout(t);
  }, [search]);

  // ── Queries ──────────────────────────────────────────────────────────────────

  const { data: myTasksData, isLoading: myLoading } = api.task.getMyTasks.useQuery(
    { limit: 50 },
    { enabled: sessionStatus === "authenticated" },
  );

  // "Assigned by Me" — backend lacks createdByMe filter; using getAll as placeholder.
  // TODO: add createdById filter to the backend getAll endpoint and thread it through here.
  const { data: assignedByMeData, isLoading: assignedByMeLoading } =
    api.task.getAll.useQuery(
      { limit: 50 },
      { enabled: sessionStatus === "authenticated" && activeTab === "assigned_by_me" },
    );

  const { data: allTasksData, isLoading: allLoading } = api.task.getAll.useQuery(
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

  const assignedByMeTasks = useMemo(
    () => (assignedByMeData?.items ?? []).map(mapAllTask),
    [assignedByMeData],
  );

  const allTasks = useMemo(
    () => (allTasksData?.items ?? []).map(mapAllTask),
    [allTasksData],
  );

  // ── Active data set ───────────────────────────────────────────────────────────

  const rawTasks = useMemo(() => {
    if (activeTab === "mine") return myTasks;
    if (activeTab === "assigned_by_me") return assignedByMeTasks;
    return allTasks;
  }, [activeTab, myTasks, assignedByMeTasks, allTasks]);

  const isLoading =
    activeTab === "mine"
      ? myLoading
      : activeTab === "assigned_by_me"
        ? assignedByMeLoading
        : allLoading;

  // ── Client-side filter (for mine / assigned_by_me tabs) ───────────────────────

  const filtered = useMemo(() => {
    // For "all" tab, filtering is server-side; only sort client-side.
    let rows = activeTab === "all" ? rawTasks : rawTasks.filter((t) => {
      if (statusFilter !== "ALL" && t.status !== statusFilter) return false;
      if (priorityFilter !== "ALL" && t.priority !== priorityFilter) return false;
      if (typeFilter !== "ALL" && t.type !== typeFilter) return false;
      if (debouncedSearch) {
        const q = debouncedSearch.toLowerCase();
        if (
          !t.title.toLowerCase().includes(q) &&
          !t.project.name.toLowerCase().includes(q)
        ) return false;
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
  }, [rawTasks, activeTab, statusFilter, priorityFilter, typeFilter, debouncedSearch, sortBy]);

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
    { id: "assigned_by_me", label: "Assigned by Me" },
    { id: "all", label: "All Tasks" },
  ];

  // ── Auth guard ────────────────────────────────────────────────────────────────

  if (sessionStatus === "loading") {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <p className="text-muted-foreground font-mono text-sm">Loading…</p>
      </div>
    );
  }

  if (sessionStatus === "unauthenticated") {
    return (
      <div className="min-h-[60vh] flex flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Sign in to view your tasks.</p>
        <Button asChild variant="neon">
          <Link href="/api/auth/signin">Sign in</Link>
        </Button>
      </div>
    );
  }

  // ── Render ────────────────────────────────────────────────────────────────────

  return (
    <div className="min-h-screen bg-background">
      {/* ── Sticky header ── */}
      <div className="sticky top-0 z-10 bg-background border-b-2 border-border">
        <div className="p-4 pb-0">
          {/* Title row */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-2xl font-bold text-foreground flex items-center gap-2 font-mono">
                <CheckSquare className="size-6 text-primary" />
                Tasks
                <span className="inline-block w-3 h-5 bg-primary ml-1 animate-pulse" />
              </h1>
              <p className="text-sm text-muted-foreground mt-0.5">
                Track your work across all projects
              </p>
            </div>

            <div className="flex items-center gap-2">
              {/* View toggle */}
              <div className="flex border-2 border-border">
                <button
                  type="button"
                  onClick={() => setViewMode("list")}
                  className={cn(
                    "px-3 py-1.5 transition-colors",
                    viewMode === "list"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-card",
                  )}
                  aria-label="List view"
                >
                  <List className="size-4" />
                </button>
                <button
                  type="button"
                  onClick={() => setViewMode("grid")}
                  className={cn(
                    "px-3 py-1.5 border-l-2 border-border transition-colors",
                    viewMode === "grid"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-card",
                  )}
                  aria-label="Grid view"
                >
                  <LayoutGrid className="size-4" />
                </button>
              </div>

              <Button asChild variant="neon" size="sm">
                <Link href="/tasks/create">
                  <Plus className="size-4 mr-2" />
                  New Task
                </Link>
              </Button>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex gap-0 border-b-0">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={cn(
                  "px-4 py-2 text-sm font-mono transition-colors border-b-2",
                  activeTab === tab.id
                    ? "text-primary border-primary"
                    : "text-muted-foreground border-transparent hover:text-foreground hover:border-border",
                )}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>

        {/* Filter bar */}
        <div className="px-4 py-3 flex flex-wrap items-center gap-2 bg-background">
          {/* Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search tasks…"
              className="pl-9 pr-4 py-1.5 bg-card border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none w-52"
            />
          </div>

          {/* Status */}
          <Select
            value={statusFilter}
            onValueChange={(v) => setStatusFilter(v as TaskStatus | "ALL")}
          >
            <SelectTrigger className="w-36 border-2 h-8 text-xs">
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
            <SelectTrigger className="w-36 border-2 h-8 text-xs">
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
            <SelectTrigger className="w-40 border-2 h-8 text-xs">
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
          <div className="flex items-center gap-1.5 ml-auto">
            <ArrowUpDown className="size-3.5 text-muted-foreground" />
            <Select value={sortBy} onValueChange={(v) => setSortBy(v as SortBy)}>
              <SelectTrigger className="w-36 border-2 h-8 text-xs">
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
      <div className="px-4 py-3 flex flex-wrap gap-3 border-b-2 border-border bg-background/80">
        <StatCard label="Total" value={stats.total} icon={CheckSquare} />
        <StatCard
          label="In Progress"
          value={stats.inProgress}
          icon={Timer}
          accent="text-neon"
        />
        <StatCard
          label="Blocked"
          value={stats.blocked}
          icon={AlertCircle}
          accent="text-destructive"
        />
        <StatCard
          label="Done This Week"
          value={stats.completedThisWeek}
          icon={CheckCircle2}
          accent="text-neon-green"
        />
        <div className="flex items-center gap-2 ml-auto text-xs font-mono text-muted-foreground self-center">
          <FolderKanban className="size-3.5" />
          <span>
            Showing{" "}
            <span className="text-foreground font-semibold">{filtered.length}</span>{" "}
            tasks
          </span>
        </div>
      </div>

      {/* ── Content ── */}
      <div className="p-4">
        {isLoading ? (
          <div className="py-16 text-center text-muted-foreground font-mono text-sm">
            Loading tasks…
          </div>
        ) : filtered.length === 0 ? (
          <div className="border-2 border-dashed border-border py-16 text-center">
            <Circle className="size-8 text-border mx-auto mb-3" />
            <p className="text-muted-foreground text-sm font-mono">No tasks found</p>
            <p className="text-muted-foreground text-xs mt-1">
              Try adjusting your filters or{" "}
              <Link href="/tasks/create" className="text-primary underline">
                create a task
              </Link>
            </p>
          </div>
        ) : viewMode === "grid" ? (
          /* ── Grid view ── */
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filtered.map((task) => (
              <TaskGridCard key={task.id} task={task} />
            ))}
          </div>
        ) : (
          /* ── List view ── */
          <Card>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full min-w-[800px]">
                  <thead>
                    <tr className="border-b-2 border-border">
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Title
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Project
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Status
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Priority
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Type
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Assignee
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        Due
                      </th>
                      <th className="text-left p-3 text-xs font-mono uppercase tracking-widest text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Coins className="size-3" />
                          Equity
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
        )}
      </div>
    </div>
  );
}
