"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import {
  Plus,
  Search,
  Filter,
  LayoutGrid,
  List,
  Clock,
  User,
  Coins,
  Loader2,
  AlertCircle,
  ChevronDown,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { toast } from "sonner";
import { TaskEconomicState } from "~/components/tasks/task-economic-state";

interface TasksTabProps {
  projectId: string;
  isOwner: boolean;
}

const KANBAN_COLUMNS = ["TODO", "IN_PROGRESS", "REVIEW", "COMPLETED", "BLOCKED"] as const;
type TaskStatus = (typeof KANBAN_COLUMNS)[number];

const statusVariant: Record<string, "outline" | "neon" | "neon-purple" | "neon-green" | "destructive"> = {
  TODO: "outline",
  IN_PROGRESS: "neon",
  REVIEW: "neon-purple",
  COMPLETED: "neon-green",
  BLOCKED: "destructive",
};

const priorityVariant: Record<string, "secondary" | "outline" | "warning" | "destructive"> = {
  LOW: "secondary",
  MEDIUM: "outline",
  HIGH: "warning",
  URGENT: "destructive",
};

const statusLabel: Record<string, string> = {
  TODO: "TODO",
  IN_PROGRESS: "IN PROGRESS",
  REVIEW: "REVIEW",
  COMPLETED: "COMPLETED",
  BLOCKED: "BLOCKED",
};

const priorityOptions = ["LOW", "MEDIUM", "HIGH", "URGENT"];
const typeOptions = ["FEATURE", "BUG", "TASK", "IMPROVEMENT", "RESEARCH"];

export default function TasksTab({ projectId, isOwner }: TasksTabProps) {
  const [viewMode, setViewMode] = useState<"board" | "list">("board");
  const [searchText, setSearchText] = useState("");
  const [filterStatus, setFilterStatus] = useState<string>("");
  const [filterPriority, setFilterPriority] = useState<string>("");
  const [filterType, setFilterType] = useState<string>("");
  const [showFilters, setShowFilters] = useState(false);

  const { data: tasks, isLoading, error } = api.task.getAll.useQuery({ projectId });

  const filteredTasks = useMemo(() => {
    if (!tasks) return [];
    return (tasks as any[]).filter((task) => {
      if (searchText && !task.title?.toLowerCase().includes(searchText.toLowerCase())) return false;
      if (filterStatus && task.status !== filterStatus) return false;
      if (filterPriority && task.priority !== filterPriority) return false;
      if (filterType && task.type !== filterType) return false;
      return true;
    });
  }, [tasks, searchText, filterStatus, filterPriority, filterType]);

  const tasksByStatus = useMemo(() => {
    const grouped: Record<string, any[]> = {};
    for (const col of KANBAN_COLUMNS) {
      grouped[col] = [];
    }
    for (const task of filteredTasks) {
      const status = task.status as string;
      if (grouped[status]) {
        grouped[status]!.push(task);
      } else {
        grouped["TODO"]!.push(task);
      }
    }
    return grouped;
  }, [filteredTasks]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-6 animate-spin text-primary" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="border-2 border-destructive/40 bg-destructive/5 p-6 flex items-start gap-3">
        <AlertCircle className="size-5 text-destructive shrink-0 mt-0.5" />
        <div>
          <p className="font-mono text-sm font-bold text-destructive">FAILED TO LOAD TASKS</p>
          <p className="text-xs text-muted-foreground mt-1">{error.message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center gap-3 flex-wrap">
        {/* Search */}
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search tasks..."
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            className="w-full bg-card border-2 border-border focus:border-primary focus:outline-none pl-9 pr-3 py-2 text-sm"
          />
        </div>

        {/* Filter toggle */}
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowFilters(!showFilters)}
          className="font-mono text-xs"
        >
          <Filter className="size-4 mr-1" />
          FILTERS
        </Button>

        {/* View mode toggle */}
        <div className="flex border-2 border-border">
          <button
            onClick={() => setViewMode("board")}
            className={cn(
              "px-3 py-2 text-sm",
              viewMode === "board"
                ? "bg-primary text-primary-foreground"
                : "bg-card text-muted-foreground hover:text-foreground"
            )}
          >
            <LayoutGrid className="size-4" />
          </button>
          <button
            onClick={() => setViewMode("list")}
            className={cn(
              "px-3 py-2 text-sm border-l-2 border-border",
              viewMode === "list"
                ? "bg-primary text-primary-foreground"
                : "bg-card text-muted-foreground hover:text-foreground"
            )}
          >
            <List className="size-4" />
          </button>
        </div>

        {/* Create task */}
        {isOwner && (
          <Button variant="neon" size="sm" asChild className="font-mono text-xs">
            <Link href={`/tasks/create?projectId=${projectId}`}>
              <Plus className="size-4 mr-1" />
              CREATE TASK
            </Link>
          </Button>
        )}
      </div>

      {/* Filters row */}
      {showFilters && (
        <div className="flex items-center gap-3 flex-wrap p-3 border-2 border-border bg-card">
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="bg-card border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
          >
            <option value="">All Statuses</option>
            {KANBAN_COLUMNS.map((s) => (
              <option key={s} value={s}>
                {statusLabel[s]}
              </option>
            ))}
          </select>

          <select
            value={filterPriority}
            onChange={(e) => setFilterPriority(e.target.value)}
            className="bg-card border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
          >
            <option value="">All Priorities</option>
            {priorityOptions.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>

          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="bg-card border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
          >
            <option value="">All Types</option>
            {typeOptions.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>

          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setFilterStatus("");
              setFilterPriority("");
              setFilterType("");
              setSearchText("");
            }}
            className="text-xs text-muted-foreground"
          >
            Clear
          </Button>
        </div>
      )}

      {/* Board View */}
      {viewMode === "board" && (
        <div className="flex gap-4 overflow-x-auto pb-4">
          {KANBAN_COLUMNS.map((column) => (
            <KanbanColumn
              key={column}
              status={column}
              tasks={tasksByStatus[column] ?? []}
              projectId={projectId}
              isOwner={isOwner}
            />
          ))}
        </div>
      )}

      {/* List View */}
      {viewMode === "list" && (
        <div className="space-y-2">
          {filteredTasks.length === 0 ? (
            <div className="border-2 border-border bg-card p-8 text-center">
              <p className="text-sm text-muted-foreground font-mono">NO TASKS FOUND</p>
            </div>
          ) : (
            filteredTasks.map((task: any) => <TaskListItem key={task.id} task={task} />)
          )}
        </div>
      )}
    </div>
  );
}

/* ─── Kanban Column ─── */
function KanbanColumn({
  status,
  tasks,
  projectId,
  isOwner,
}: {
  status: TaskStatus;
  tasks: any[];
  projectId: string;
  isOwner: boolean;
}) {
  const [showQuickAdd, setShowQuickAdd] = useState(false);
  const [quickTitle, setQuickTitle] = useState("");
  const [quickPriority, setQuickPriority] = useState<"LOW" | "MEDIUM" | "HIGH" | "URGENT">("MEDIUM");

  const utils = api.useUtils();
  const createMutation = api.task.create.useMutation({
    onSuccess: () => {
      toast.success("Task created");
      setQuickTitle("");
      setShowQuickAdd(false);
      utils.task.getAll.invalidate({ projectId });
    },
    onError: (err) => {
      toast.error(`Failed to create task: ${err.message}`);
    },
  });

  const handleQuickAdd = () => {
    if (!quickTitle.trim()) return;
    createMutation.mutate({
      projectId,
      title: quickTitle.trim(),
      priority: quickPriority,
      type: "FEATURE",
    });
  };

  return (
    <div className="flex-shrink-0 w-[280px]">
      {/* Column header */}
      <div className="flex items-center justify-between mb-3 px-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
            {statusLabel[status]}
          </span>
          <Badge variant={statusVariant[status]} size="sm">
            {tasks.length}
          </Badge>
        </div>
      </div>

      {/* Cards */}
      <div className="space-y-2 min-h-[100px]">
        {tasks.map((task: any) => (
          <TaskCard key={task.id} task={task} />
        ))}
      </div>

      {/* Quick-add */}
      {isOwner && (
        <div className="mt-2">
          {showQuickAdd ? (
            <div className="bg-card border-2 border-border p-3 space-y-2">
              <input
                type="text"
                placeholder="Task title..."
                value={quickTitle}
                onChange={(e) => setQuickTitle(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") handleQuickAdd();
                  if (e.key === "Escape") setShowQuickAdd(false);
                }}
                className="w-full bg-transparent border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
                autoFocus
              />
              <div className="flex items-center gap-2">
                <select
                  value={quickPriority}
                  onChange={(e) => setQuickPriority(e.target.value as "LOW" | "MEDIUM" | "HIGH" | "URGENT")}
                  className="bg-card border-2 border-border focus:border-primary focus:outline-none px-2 py-1 text-xs flex-1"
                >
                  {priorityOptions.map((p) => (
                    <option key={p} value={p}>
                      {p}
                    </option>
                  ))}
                </select>
                <Button
                  size="sm"
                  variant="neon"
                  onClick={handleQuickAdd}
                  disabled={createMutation.isPending || !quickTitle.trim()}
                  className="text-xs font-mono"
                >
                  {createMutation.isPending ? (
                    <Loader2 className="size-3 animate-spin" />
                  ) : (
                    "ADD"
                  )}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setShowQuickAdd(false)}
                  className="text-xs"
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <button
              onClick={() => setShowQuickAdd(true)}
              className="w-full flex items-center gap-1 px-3 py-2 text-xs text-muted-foreground hover:text-foreground border-2 border-dashed border-border hover:border-primary transition-colors"
            >
              <Plus className="size-3" />
              Quick add
            </button>
          )}
        </div>
      )}
    </div>
  );
}

/* ─── Task Card (Board) ─── */
function TaskCard({ task }: { task: any }) {
  const [showStatusMenu, setShowStatusMenu] = useState(false);
  const utils = api.useUtils();

  const updateStatusMutation = api.task.updateStatus.useMutation({
    onSuccess: () => {
      toast.success("Status updated");
      utils.task.getAll.invalidate();
    },
    onError: (err) => {
      toast.error(`Failed to update: ${err.message}`);
    },
  });

  return (
    <div className="bg-card border-2 border-border hover:border-primary transition-colors p-3 space-y-2">
      {/* Title */}
      <p className="text-sm font-medium text-foreground leading-tight">{task.title}</p>

      {/* Badges row */}
      <div className="flex items-center gap-1.5 flex-wrap">
        {task.priority && (
          <Badge variant={priorityVariant[task.priority] ?? "outline"} size="sm">
            {task.priority}
          </Badge>
        )}
        {task.type && (
          <Badge variant="secondary" size="sm">
            {task.type}
          </Badge>
        )}
      </div>

      {/* Meta row */}
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          {task.assignee && (
            <div className="flex items-center gap-1">
              <Avatar className="size-4">
                <AvatarImage src={task.assignee?.image} />
                <AvatarFallback className="text-[8px]">
                  {task.assignee?.name?.charAt(0) ?? <User className="size-3" />}
                </AvatarFallback>
              </Avatar>
              <span className="truncate max-w-[60px]">{task.assignee?.name}</span>
            </div>
          )}
          {task.effortEstimate && (
            <span className="flex items-center gap-0.5">
              <Clock className="size-3" />
              {task.effortEstimate}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {task.equityReward != null && Number(task.equityReward) > 0 && (
            <span className="flex items-center gap-0.5 text-neon-green">
              <Coins className="size-3" />
              {task.equityReward}
            </span>
          )}
          <TaskEconomicState
            equityReward={Number(task.equityReward ?? 0)}
            escrowStatus={task.escrowStatus}
          />
          {task.dueDate && (
            <span className="flex items-center gap-0.5">
              <Clock className="size-3" />
              {new Date(task.dueDate).toLocaleDateString("en-US", { month: "short", day: "numeric" })}
            </span>
          )}
        </div>
      </div>

      {/* Status dropdown */}
      <div className="relative">
        <button
          onClick={() => setShowStatusMenu(!showStatusMenu)}
          className="flex items-center gap-1 text-[10px] font-mono text-muted-foreground hover:text-foreground"
        >
          <ChevronDown className="size-3" />
          {statusLabel[task.status] ?? task.status}
        </button>
        {showStatusMenu && (
          <div className="absolute z-20 top-full left-0 mt-1 bg-card border-2 border-border shadow-[4px_4px_0_0_var(--border)]">
            {KANBAN_COLUMNS.map((s) => (
              <button
                key={s}
                onClick={() => {
                  updateStatusMutation.mutate({ id: task.id, status: s });
                  setShowStatusMenu(false);
                }}
                className={cn(
                  "block w-full text-left px-3 py-1.5 text-xs font-mono hover:bg-muted",
                  task.status === s && "text-primary font-bold"
                )}
              >
                {statusLabel[s]}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Task List Item ─── */
function TaskListItem({ task }: { task: any }) {
  const utils = api.useUtils();
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  const updateStatusMutation = api.task.updateStatus.useMutation({
    onSuccess: () => {
      toast.success("Status updated");
      utils.task.getAll.invalidate();
    },
    onError: (err) => {
      toast.error(`Failed to update: ${err.message}`);
    },
  });

  return (
    <div className="bg-card border-2 border-border hover:border-primary transition-colors p-3 flex items-center gap-4">
      {/* Status badge */}
      <div className="relative">
        <button onClick={() => setShowStatusMenu(!showStatusMenu)}>
          <Badge variant={statusVariant[task.status] ?? "outline"} size="sm" className="cursor-pointer">
            {statusLabel[task.status] ?? task.status}
          </Badge>
        </button>
        {showStatusMenu && (
          <div className="absolute z-20 top-full left-0 mt-1 bg-card border-2 border-border shadow-[4px_4px_0_0_var(--border)]">
            {KANBAN_COLUMNS.map((s) => (
              <button
                key={s}
                onClick={() => {
                  updateStatusMutation.mutate({ id: task.id, status: s });
                  setShowStatusMenu(false);
                }}
                className={cn(
                  "block w-full text-left px-3 py-1.5 text-xs font-mono hover:bg-muted whitespace-nowrap",
                  task.status === s && "text-primary font-bold"
                )}
              >
                {statusLabel[s]}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Title */}
      <p className="text-sm font-medium text-foreground flex-1 min-w-0 truncate">{task.title}</p>

      {/* Priority */}
      {task.priority && (
        <Badge variant={priorityVariant[task.priority] ?? "outline"} size="sm">
          {task.priority}
        </Badge>
      )}

      {/* Type */}
      {task.type && (
        <Badge variant="secondary" size="sm">
          {task.type}
        </Badge>
      )}

      {/* Assignee */}
      {task.assignee && (
        <div className="flex items-center gap-1">
          <Avatar className="size-5">
            <AvatarImage src={task.assignee?.image} />
            <AvatarFallback className="text-[8px]">
              {task.assignee?.name?.charAt(0) ?? "?"}
            </AvatarFallback>
          </Avatar>
        </div>
      )}

      {/* Equity */}
      {task.equityReward != null && Number(task.equityReward) > 0 && (
        <span className="flex items-center gap-0.5 text-xs text-neon-green">
          <Coins className="size-3" />
          {task.equityReward}
        </span>
      )}
      <TaskEconomicState
        equityReward={Number(task.equityReward ?? 0)}
        escrowStatus={task.escrowStatus}
      />

      {/* Due date */}
      {task.dueDate && (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {new Date(task.dueDate).toLocaleDateString("en-US", { month: "short", day: "numeric" })}
        </span>
      )}
    </div>
  );
}
