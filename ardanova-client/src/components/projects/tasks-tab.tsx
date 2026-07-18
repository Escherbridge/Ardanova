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
  RefreshCw,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";
import { TaskEconomicState } from "~/components/tasks/task-economic-state";

interface TasksTabProps {
  projectId: string;
  isOwner: boolean;
}

const KANBAN_COLUMNS = [
  "TODO",
  "IN_PROGRESS",
  "REVIEW",
  "COMPLETED",
  "BLOCKED",
] as const;
type TaskStatus = RouterInputs["task"]["updateStatus"]["status"];
type TaskPriority = NonNullable<RouterInputs["task"]["create"]["priority"]>;
type TaskType = RouterInputs["task"]["create"]["type"];
type ProjectTask = RouterOutputs["task"]["getAll"]["items"][number];

type TaskAssignee = {
  name?: string | null;
  image?: string | null;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getTaskAssignee(task: ProjectTask): TaskAssignee | null {
  if (!isRecord(task)) return null;
  const value = task.assignedTo;
  if (!isRecord(value)) return null;

  return {
    name: typeof value.name === "string" ? value.name : null,
    image: typeof value.image === "string" ? value.image : null,
  };
}

function isTaskStatus(value: string): value is TaskStatus {
  return KANBAN_COLUMNS.some((status) => status === value);
}

const statusVariant: Record<
  TaskStatus,
  "outline" | "neon" | "neon-purple" | "neon-green" | "destructive"
> = {
  TODO: "outline",
  IN_PROGRESS: "neon",
  REVIEW: "neon-purple",
  COMPLETED: "neon-green",
  BLOCKED: "destructive",
};

const priorityVariant: Record<
  TaskPriority,
  "secondary" | "outline" | "warning" | "destructive"
> = {
  LOW: "secondary",
  MEDIUM: "outline",
  HIGH: "warning",
  URGENT: "destructive",
};

const statusLabel: Record<TaskStatus, string> = {
  TODO: "TODO",
  IN_PROGRESS: "IN PROGRESS",
  REVIEW: "REVIEW",
  COMPLETED: "COMPLETED",
  BLOCKED: "BLOCKED",
};

const priorityOptions = [
  "LOW",
  "MEDIUM",
  "HIGH",
  "URGENT",
] as const satisfies readonly TaskPriority[];
const typeOptions = [
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
] as const satisfies readonly TaskType[];

function isTaskPriority(value: string): value is TaskPriority {
  return priorityOptions.some((priority) => priority === value);
}

function isTaskType(value: string): value is TaskType {
  return typeOptions.some((type) => type === value);
}

function getTaskStatusLabel(status: string): string {
  return isTaskStatus(status) ? statusLabel[status] : status;
}

function getTaskStatusVariant(status: string) {
  return isTaskStatus(status) ? statusVariant[status] : "outline";
}

function getTaskPriorityVariant(priority: string) {
  return isTaskPriority(priority) ? priorityVariant[priority] : "outline";
}

export default function TasksTab({ projectId, isOwner }: TasksTabProps) {
  const [viewMode, setViewMode] = useState<"board" | "list">("board");
  const [searchText, setSearchText] = useState("");
  const [filterStatus, setFilterStatus] = useState<TaskStatus | "">("");
  const [filterPriority, setFilterPriority] = useState<TaskPriority | "">("");
  const [filterType, setFilterType] = useState<TaskType | "">("");
  const [showFilters, setShowFilters] = useState(false);

  const {
    data: tasks,
    isLoading,
    error,
    refetch,
  } = api.task.getAll.useQuery({ projectId, limit: 100 });

  const filteredTasks = useMemo(() => {
    return (tasks?.items ?? []).filter((task) => {
      if (
        searchText &&
        !task.title?.toLowerCase().includes(searchText.toLowerCase())
      )
        return false;
      if (filterStatus && task.status !== filterStatus) return false;
      if (filterPriority && task.priority !== filterPriority) return false;
      if (filterType && task.taskType !== filterType) return false;
      return true;
    });
  }, [tasks, searchText, filterStatus, filterPriority, filterType]);

  const tasksByStatus = useMemo(() => {
    const grouped: Record<TaskStatus, ProjectTask[]> = {
      TODO: [],
      IN_PROGRESS: [],
      REVIEW: [],
      COMPLETED: [],
      BLOCKED: [],
    };
    for (const task of filteredTasks) {
      if (isTaskStatus(task.status)) {
        grouped[task.status].push(task);
      } else {
        grouped.TODO.push(task);
      }
    }
    return grouped;
  }, [filteredTasks]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="text-primary size-6 animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="border-destructive/40 bg-destructive/5 flex items-start gap-3 border-2 p-6">
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div className="flex-1">
          <p className="text-destructive font-mono text-sm font-bold">
            FAILED TO LOAD TASKS
          </p>
          <p className="text-muted-foreground mt-1 text-xs">{error.message}</p>
        </div>
        <Button
          type="button"
          variant="outline"
          className="min-h-11 shrink-0"
          onClick={() => void refetch()}
        >
          <RefreshCw className="mr-2 size-4" />
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Search */}
        <div className="relative min-w-[200px] flex-1">
          <label htmlFor="project-task-search" className="sr-only">
            Search project tasks
          </label>
          <Search
            className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2"
            aria-hidden="true"
          />
          <input
            id="project-task-search"
            type="text"
            placeholder="Search tasks..."
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            className="bg-card border-border focus:border-primary min-h-11 w-full border-2 py-2 pr-3 pl-9 text-sm focus:outline-none"
          />
        </div>

        {/* Filter toggle */}
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowFilters(!showFilters)}
          className="min-h-11 font-mono text-xs"
        >
          <Filter className="mr-1 size-4" aria-hidden="true" />
          FILTERS
        </Button>

        {/* View mode toggle */}
        <div
          className="border-border flex border-2"
          role="group"
          aria-label="Task view"
        >
          <button
            type="button"
            onClick={() => setViewMode("board")}
            aria-label="Board view"
            aria-pressed={viewMode === "board"}
            className={cn(
              "min-h-11 min-w-11 px-3 py-2 text-sm",
              viewMode === "board"
                ? "bg-primary text-primary-foreground"
                : "bg-card text-muted-foreground hover:text-foreground",
            )}
          >
            <LayoutGrid className="size-4" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={() => setViewMode("list")}
            aria-label="List view"
            aria-pressed={viewMode === "list"}
            className={cn(
              "border-border min-h-11 min-w-11 border-l-2 px-3 py-2 text-sm",
              viewMode === "list"
                ? "bg-primary text-primary-foreground"
                : "bg-card text-muted-foreground hover:text-foreground",
            )}
          >
            <List className="size-4" aria-hidden="true" />
          </button>
        </div>

        {/* Create task */}
        {isOwner && (
          <Button
            variant="neon"
            size="sm"
            asChild
            className="min-h-11 font-mono text-xs"
          >
            <Link href={`/tasks/create?projectId=${projectId}`}>
              <Plus className="mr-1 size-4" aria-hidden="true" />
              CREATE TASK
            </Link>
          </Button>
        )}
      </div>

      {/* Filters row */}
      {showFilters && (
        <div className="border-border bg-card flex flex-wrap items-center gap-3 border-2 p-3">
          <select
            aria-label="Filter tasks by status"
            value={filterStatus}
            onChange={(e) => {
              const value = e.target.value;
              if (value === "" || isTaskStatus(value)) setFilterStatus(value);
            }}
            className="bg-card border-border focus:border-primary min-h-11 border-2 px-3 py-2 text-sm focus:outline-none"
          >
            <option value="">All Statuses</option>
            {KANBAN_COLUMNS.map((s) => (
              <option key={s} value={s}>
                {statusLabel[s]}
              </option>
            ))}
          </select>

          <select
            aria-label="Filter tasks by priority"
            value={filterPriority}
            onChange={(e) => {
              const value = e.target.value;
              if (value === "" || isTaskPriority(value))
                setFilterPriority(value);
            }}
            className="bg-card border-border focus:border-primary min-h-11 border-2 px-3 py-2 text-sm focus:outline-none"
          >
            <option value="">All Priorities</option>
            {priorityOptions.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>

          <select
            aria-label="Filter tasks by type"
            value={filterType}
            onChange={(e) => {
              const value = e.target.value;
              if (value === "" || isTaskType(value)) setFilterType(value);
            }}
            className="bg-card border-border focus:border-primary min-h-11 border-2 px-3 py-2 text-sm focus:outline-none"
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
            className="text-muted-foreground text-xs"
          >
            Clear
          </Button>
        </div>
      )}

      {/* Board View */}
      {viewMode === "board" && (
        <div className="flex max-w-full gap-4 overflow-x-auto overscroll-x-contain pb-4">
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
            <div className="border-border bg-card border-2 p-8 text-center">
              <p className="text-muted-foreground font-mono text-sm">
                NO TASKS FOUND
              </p>
            </div>
          ) : (
            filteredTasks.map((task) => (
              <TaskListItem key={task.id} task={task} />
            ))
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
  tasks: ProjectTask[];
  projectId: string;
  isOwner: boolean;
}) {
  const [showQuickAdd, setShowQuickAdd] = useState(false);
  const [quickTitle, setQuickTitle] = useState("");
  const [quickPriority, setQuickPriority] = useState<TaskPriority>("MEDIUM");

  const utils = api.useUtils();
  const createMutation = api.task.create.useMutation();
  const moveMutation = api.task.updateStatus.useMutation();
  const quickAddPending = createMutation.isPending || moveMutation.isPending;

  const handleQuickAdd = async () => {
    if (!quickTitle.trim() || quickAddPending) return;
    try {
      const createdTask = await createMutation.mutateAsync({
        projectId,
        title: quickTitle.trim(),
        priority: quickPriority,
        type: "FEATURE",
      });

      setQuickTitle("");
      setShowQuickAdd(false);

      if (status !== "TODO") {
        try {
          await moveMutation.mutateAsync({ id: createdTask.id, status });
          toast.success(`Task created in ${statusLabel[status]}`);
        } catch (moveError) {
          const message =
            moveError instanceof Error ? moveError.message : "Unknown error";
          toast.error(
            `Task was created in TODO, but could not be moved to ${statusLabel[status]}: ${message}`,
          );
        }
      } else {
        toast.success("Task created in TODO");
      }
    } catch (createError) {
      const message =
        createError instanceof Error ? createError.message : "Unknown error";
      toast.error(`Task was not created: ${message}`);
    } finally {
      await utils.task.getAll.invalidate({ projectId });
    }
  };

  return (
    <div className="w-[280px] flex-shrink-0">
      {/* Column header */}
      <div className="mb-3 flex items-center justify-between px-1">
        <div className="flex items-center gap-2">
          <span className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
            {statusLabel[status]}
          </span>
          <Badge variant={statusVariant[status]} size="sm">
            {tasks.length}
          </Badge>
        </div>
      </div>

      {/* Cards */}
      <div className="min-h-[100px] space-y-2">
        {tasks.map((task) => (
          <TaskCard key={task.id} task={task} />
        ))}
      </div>

      {/* Quick-add */}
      {isOwner && (
        <div className="mt-2">
          {showQuickAdd ? (
            <div className="bg-card border-border space-y-2 border-2 p-3">
              <input
                aria-label={`New task title in ${getTaskStatusLabel(status)}`}
                type="text"
                placeholder="Task title..."
                value={quickTitle}
                onChange={(e) => setQuickTitle(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.nativeEvent.isComposing) {
                    e.preventDefault();
                    void handleQuickAdd();
                  }
                  if (e.key === "Escape") setShowQuickAdd(false);
                }}
                className="border-border focus:border-primary min-h-11 w-full border-2 bg-transparent px-3 py-2 text-sm focus:outline-none"
                autoFocus
              />
              <div className="flex items-center gap-2">
                <select
                  aria-label={`Priority for new task in ${getTaskStatusLabel(status)}`}
                  value={quickPriority}
                  onChange={(e) => {
                    if (isTaskPriority(e.target.value)) {
                      setQuickPriority(e.target.value);
                    }
                  }}
                  className="bg-card border-border focus:border-primary min-h-11 flex-1 border-2 px-2 py-1 text-xs focus:outline-none"
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
                  onClick={() => void handleQuickAdd()}
                  disabled={quickAddPending || !quickTitle.trim()}
                  className="min-h-11 font-mono text-xs"
                >
                  {quickAddPending ? (
                    <Loader2 className="size-3 animate-spin" />
                  ) : (
                    "ADD"
                  )}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setShowQuickAdd(false)}
                  className="min-h-11 text-xs"
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <button
              onClick={() => setShowQuickAdd(true)}
              className="text-muted-foreground hover:text-foreground border-border hover:border-primary flex min-h-11 w-full items-center gap-1 border-2 border-dashed px-3 py-2 text-xs transition-colors"
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
function TaskCard({ task }: { task: ProjectTask }) {
  const [showStatusMenu, setShowStatusMenu] = useState(false);
  const utils = api.useUtils();
  const assignee = getTaskAssignee(task);

  const updateStatusMutation = api.task.updateStatus.useMutation({
    onSuccess: () => {
      toast.success("Status updated");
      void utils.task.getAll.invalidate();
    },
    onError: (err) => {
      toast.error(`Failed to update: ${err.message}`);
    },
  });

  return (
    <div className="bg-card border-border hover:border-primary space-y-2 border-2 p-3 transition-colors">
      {/* Title */}
      <p className="text-foreground text-sm leading-tight font-medium">
        {task.title}
      </p>

      {/* Badges row */}
      <div className="flex flex-wrap items-center gap-1.5">
        {task.priority && (
          <Badge variant={getTaskPriorityVariant(task.priority)} size="sm">
            {task.priority}
          </Badge>
        )}
        {task.taskType && (
          <Badge variant="secondary" size="sm">
            {task.taskType}
          </Badge>
        )}
      </div>

      {/* Meta row */}
      <div className="text-muted-foreground flex items-center justify-between text-xs">
        <div className="flex items-center gap-2">
          {assignee && (
            <div className="flex items-center gap-1">
              <Avatar className="size-4">
                <AvatarImage src={assignee.image ?? undefined} />
                <AvatarFallback className="text-[8px]">
                  {assignee.name?.charAt(0) ?? <User className="size-3" />}
                </AvatarFallback>
              </Avatar>
              <span className="max-w-[60px] truncate">{assignee.name}</span>
            </div>
          )}
          {task.estimatedHours != null && (
            <span className="flex items-center gap-0.5">
              <Clock className="size-3" />
              {task.estimatedHours}h estimate
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {task.equityReward != null && Number(task.equityReward) > 0 && (
            <span className="text-neon-green flex items-center gap-0.5">
              <Coins className="size-3" />
              {task.equityReward}
            </span>
          )}
          <TaskEconomicState
            allocationUnits={Number(task.equityReward ?? 0)}
            escrowStatus={task.escrowStatus}
          />
          {task.dueDate && (
            <span className="flex items-center gap-0.5">
              <Clock className="size-3" />
              {new Date(task.dueDate).toLocaleDateString("en-US", {
                month: "short",
                day: "numeric",
              })}
            </span>
          )}
        </div>
      </div>

      {/* Status dropdown */}
      <div className="relative">
        <button
          onClick={() => setShowStatusMenu(!showStatusMenu)}
          className="text-muted-foreground hover:text-foreground flex min-h-11 items-center gap-1 font-mono text-[10px]"
        >
          <ChevronDown className="size-3" />
          {getTaskStatusLabel(task.status)}
        </button>
        {showStatusMenu && (
          <div className="bg-card border-border absolute top-full left-0 z-20 mt-1 border-2">
            {KANBAN_COLUMNS.map((s) => (
              <button
                key={s}
                onClick={() => {
                  updateStatusMutation.mutate({ id: task.id, status: s });
                  setShowStatusMenu(false);
                }}
                className={cn(
                  "hover:bg-muted block min-h-11 w-full px-3 py-2 text-left font-mono text-xs",
                  task.status === s && "text-primary font-bold",
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
function TaskListItem({ task }: { task: ProjectTask }) {
  const utils = api.useUtils();
  const [showStatusMenu, setShowStatusMenu] = useState(false);
  const assignee = getTaskAssignee(task);

  const updateStatusMutation = api.task.updateStatus.useMutation({
    onSuccess: () => {
      toast.success("Status updated");
      void utils.task.getAll.invalidate();
    },
    onError: (err) => {
      toast.error(`Failed to update: ${err.message}`);
    },
  });

  return (
    <div className="bg-card border-border hover:border-primary flex min-w-0 flex-col items-stretch gap-3 border-2 p-3 transition-colors sm:flex-row sm:items-center sm:gap-4">
      {/* Status badge */}
      <div className="relative">
        <button
          className="flex min-h-11 items-center"
          onClick={() => setShowStatusMenu(!showStatusMenu)}
        >
          <Badge
            variant={getTaskStatusVariant(task.status)}
            size="sm"
            className="cursor-pointer"
          >
            {getTaskStatusLabel(task.status)}
          </Badge>
        </button>
        {showStatusMenu && (
          <div className="bg-card border-border absolute top-full left-0 z-20 mt-1 border-2">
            {KANBAN_COLUMNS.map((s) => (
              <button
                key={s}
                onClick={() => {
                  updateStatusMutation.mutate({ id: task.id, status: s });
                  setShowStatusMenu(false);
                }}
                className={cn(
                  "hover:bg-muted block min-h-11 w-full px-3 py-2 text-left font-mono text-xs whitespace-nowrap",
                  task.status === s && "text-primary font-bold",
                )}
              >
                {statusLabel[s]}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Title */}
      <p className="text-foreground min-w-0 flex-1 text-sm font-medium break-words sm:truncate">
        {task.title}
      </p>

      {/* Priority */}
      {task.priority && (
        <Badge variant={getTaskPriorityVariant(task.priority)} size="sm">
          {task.priority}
        </Badge>
      )}

      {/* Type */}
      {task.taskType && (
        <Badge variant="secondary" size="sm">
          {task.taskType}
        </Badge>
      )}

      {/* Assignee */}
      {assignee && (
        <div className="flex items-center gap-1">
          <Avatar className="size-5">
            <AvatarImage src={assignee.image ?? undefined} />
            <AvatarFallback className="text-[8px]">
              {assignee.name?.charAt(0) ?? "?"}
            </AvatarFallback>
          </Avatar>
        </div>
      )}

      {/* Project-token allocation */}
      {task.equityReward != null && Number(task.equityReward) > 0 && (
        <span className="text-neon-green flex items-center gap-0.5 text-xs">
          <Coins className="size-3" />
          {task.equityReward}
        </span>
      )}
      <TaskEconomicState
        allocationUnits={Number(task.equityReward ?? 0)}
        escrowStatus={task.escrowStatus}
      />

      {/* Due date */}
      {task.dueDate && (
        <span className="text-muted-foreground text-xs whitespace-nowrap">
          {new Date(task.dueDate).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
          })}
        </span>
      )}
    </div>
  );
}
