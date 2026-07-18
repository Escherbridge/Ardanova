"use client";

import { useId, useState } from "react";
import { api, type RouterInputs } from "~/trpc/react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "~/components/ui/dialog";
import { Button } from "~/components/ui/button";
import { cn } from "~/lib/utils";
import {
  Flag,
  Target,
  Zap,
  Box,
  Layers,
  CheckSquare,
  Loader2,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type CreateLevel =
  | "milestone"
  | "epic"
  | "sprint"
  | "feature"
  | "pbi"
  | "task";

export interface CreateWorkItemModalProps {
  open: boolean;
  onClose: () => void;
  level: CreateLevel;
  projectId: string;
  /** The parent item's ID (e.g., milestoneId for epic creation) */
  parentId?: string;
  /** Callback after successful creation */
  onCreated?: () => void;
}

type EpicPriority = NonNullable<RouterInputs["epic"]["create"]["priority"]>;
type TaskPriority = NonNullable<RouterInputs["task"]["create"]["priority"]>;
type PbiType = NonNullable<RouterInputs["backlog"]["createPbi"]["type"]>;
type TaskType = RouterInputs["task"]["create"]["type"];
type TaskEstimatedHours = NonNullable<
  RouterInputs["task"]["create"]["estimatedHours"]
>;
type WorkPriority = EpicPriority | TaskPriority;
type WorkType = PbiType | TaskType;

const HIERARCHY_PRIORITIES = [
  "LOW",
  "MEDIUM",
  "HIGH",
  "CRITICAL",
] as const satisfies readonly EpicPriority[];
const TASK_PRIORITIES = [
  "LOW",
  "MEDIUM",
  "HIGH",
  "URGENT",
] as const satisfies readonly TaskPriority[];
const PBI_TYPES = [
  "FEATURE",
  "ENHANCEMENT",
  "BUG",
  "TECHNICAL_DEBT",
  "SPIKE",
] as const satisfies readonly PbiType[];
const TASK_TYPES = [
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
const TASK_ESTIMATED_HOURS = [
  1, 3, 8, 20, 40,
] as const satisfies readonly TaskEstimatedHours[];

function isOption<const T extends readonly string[]>(
  options: T,
  value: string,
): value is T[number] {
  return options.some((option) => option === value);
}

function isEstimatedHours(value: number): value is TaskEstimatedHours {
  return TASK_ESTIMATED_HOURS.some((hours) => hours === value);
}

function optionLabel(value: string): string {
  return value
    .toLowerCase()
    .replaceAll("_", " ")
    .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function dateInputToIso(value: string): string | undefined {
  return value ? new Date(`${value}T00:00:00.000Z`).toISOString() : undefined;
}

const LEVEL_LABELS: Record<CreateLevel, string> = {
  milestone: "Milestone",
  epic: "Epic",
  sprint: "Sprint",
  feature: "Feature",
  pbi: "PBI",
  task: "Task",
};

function LevelIcon({
  level,
  className,
}: {
  level: CreateLevel;
  className: string;
}) {
  switch (level) {
    case "milestone":
      return <Flag className={className} aria-hidden="true" />;
    case "epic":
      return <Target className={className} aria-hidden="true" />;
    case "sprint":
      return <Zap className={className} aria-hidden="true" />;
    case "feature":
      return <Box className={className} aria-hidden="true" />;
    case "pbi":
      return <Layers className={className} aria-hidden="true" />;
    case "task":
      return <CheckSquare className={className} aria-hidden="true" />;
  }
}

const LEVEL_COLORS: Record<CreateLevel, string> = {
  milestone: "text-neon-pink",
  epic: "text-neon-purple",
  sprint: "text-neon-cyan",
  feature: "text-neon-green",
  pbi: "text-neon-yellow",
  task: "text-foreground",
};

const inputCn =
  "min-h-11 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
const selectCn =
  "min-h-11 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
const textareaCn =
  "w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary resize-none";

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function CreateWorkItemModal({
  open,
  onClose,
  level,
  projectId,
  parentId,
  onCreated,
}: CreateWorkItemModalProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<WorkPriority>("MEDIUM");
  const [type, setType] = useState<WorkType>("FEATURE");
  const [estimatedHours, setEstimatedHours] = useState<TaskEstimatedHours>(8);
  const [targetDate, setTargetDate] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const fieldPrefix = useId();
  const fieldId = (field: string) => `${fieldPrefix}-${field}`;

  const colorClass = LEVEL_COLORS[level];

  const resetForm = () => {
    setTitle("");
    setDescription("");
    setPriority("MEDIUM");
    setType("FEATURE");
    setEstimatedHours(8);
    setTargetDate("");
    setStartDate("");
    setEndDate("");
  };

  const handleSuccess = () => {
    toast.success(`${LEVEL_LABELS[level]} created`);
    resetForm();
    onCreated?.();
    onClose();
  };

  const handleError = (error: unknown) => {
    toast.error(
      error instanceof Error ? error.message : "Failed to create item",
    );
  };

  // Mutations
  const createMilestone = api.project.addMilestone.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });
  const createEpic = api.epic.create.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });
  const createSprint = api.sprint.create.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });
  const createFeature = api.feature.create.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });
  const createPbi = api.backlog.createPbi.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });
  const createTask = api.task.create.useMutation({
    onSuccess: handleSuccess,
    onError: handleError,
  });

  const pending =
    createMilestone.isPending ||
    createEpic.isPending ||
    createSprint.isPending ||
    createFeature.isPending ||
    createPbi.isPending ||
    createTask.isPending;

  const hierarchyPriority = isOption(HIERARCHY_PRIORITIES, priority)
    ? priority
    : "MEDIUM";
  const taskPriority = isOption(TASK_PRIORITIES, priority)
    ? priority
    : "MEDIUM";
  const pbiType = isOption(PBI_TYPES, type) ? type : "FEATURE";
  const taskType = isOption(TASK_TYPES, type) ? type : "FEATURE";
  const priorityOptions: readonly WorkPriority[] =
    level === "task" ? TASK_PRIORITIES : HIERARCHY_PRIORITIES;
  const typeOptions: readonly WorkType[] =
    level === "pbi" ? PBI_TYPES : TASK_TYPES;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    if (startDate && endDate && endDate < startDate) {
      toast.error("End date must be on or after the start date");
      return;
    }

    switch (level) {
      case "milestone":
        createMilestone.mutate({
          projectId,
          title: title.trim(),
          description: description || undefined,
          targetDate: dateInputToIso(targetDate),
        });
        break;
      case "epic":
        if (!parentId) {
          toast.error("Choose a milestone before creating an epic");
          return;
        }
        createEpic.mutate({
          projectId,
          milestoneId: parentId,
          title: title.trim(),
          description: description || undefined,
          priority: hierarchyPriority,
          startDate: startDate ? new Date(startDate).toISOString() : undefined,
          targetDate: endDate ? new Date(endDate).toISOString() : undefined,
        });
        break;
      case "sprint":
        if (!parentId) {
          toast.error("Choose an epic before creating a sprint");
          return;
        }
        createSprint.mutate({
          projectId,
          epicId: parentId,
          name: title.trim(),
          goal: description || undefined,
          startDate: startDate ? new Date(startDate).toISOString() : undefined,
          endDate: endDate ? new Date(endDate).toISOString() : undefined,
        });
        break;
      case "feature":
        if (!parentId) {
          toast.error("Choose a sprint before creating a feature");
          return;
        }
        createFeature.mutate({
          projectId,
          sprintId: parentId,
          title: title.trim(),
          description: description || undefined,
          priority: hierarchyPriority,
        });
        break;
      case "pbi":
        createPbi.mutate({
          projectId,
          featureId: parentId || undefined,
          title: title.trim(),
          description: description || undefined,
          type: pbiType,
          priority: hierarchyPriority,
        });
        break;
      case "task": {
        const taskDescription = description.trim();
        if (taskDescription.length < 10) {
          toast.error("Task description must be at least 10 characters");
          return;
        }
        createTask.mutate({
          projectId,
          pbiId: parentId || undefined,
          title: title.trim(),
          description: taskDescription,
          type: taskType,
          priority: taskPriority,
          estimatedHours,
        });
        break;
      }
    }
  };

  const needsDates = ["milestone", "epic", "sprint"].includes(level);
  const needsPriority = ["epic", "feature", "pbi", "task"].includes(level);
  const needsType = ["pbi", "task"].includes(level);
  const needsEstimate = level === "task";

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] max-w-md overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <LevelIcon level={level} className={cn("h-5 w-5", colorClass)} />
            <DialogTitle>Create {LEVEL_LABELS[level]}</DialogTitle>
          </div>
          <DialogDescription className="sr-only">
            Add a {LEVEL_LABELS[level].toLowerCase()} to this project hierarchy.
            Review the fields before creating it.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Title */}
          <div>
            <label
              htmlFor={fieldId("title")}
              className="text-muted-foreground mb-1 block text-xs font-medium"
            >
              {level === "sprint" ? "Name" : "Title"} *
            </label>
            <input
              id={fieldId("title")}
              className={inputCn}
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              placeholder={`${LEVEL_LABELS[level]} ${level === "sprint" ? "name" : "title"}`}
              autoFocus
            />
          </div>

          {/* Description */}
          <div>
            <label
              htmlFor={fieldId("description")}
              className="text-muted-foreground mb-1 block text-xs font-medium"
            >
              {level === "sprint" ? "Goal" : "Description"}
              {level === "task" ? " * (min 10 chars)" : ""}
            </label>
            <textarea
              id={fieldId("description")}
              className={textareaCn}
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder={
                level === "sprint"
                  ? "Sprint goal..."
                  : level === "task"
                    ? "Describe the task in detail..."
                    : "Optional description..."
              }
              required={level === "task"}
              minLength={level === "task" ? 10 : undefined}
            />
          </div>

          {/* Type + Priority row */}
          {(needsType || needsPriority) && (
            <div
              className={cn(
                "grid gap-3",
                needsType && needsPriority
                  ? "grid-cols-1 sm:grid-cols-2"
                  : "grid-cols-1",
              )}
            >
              {needsType && (
                <div>
                  <label
                    htmlFor={fieldId("type")}
                    className="text-muted-foreground mb-1 block text-xs font-medium"
                  >
                    Type
                  </label>
                  <select
                    id={fieldId("type")}
                    className={selectCn}
                    value={level === "pbi" ? pbiType : taskType}
                    onChange={(e) => {
                      const value = e.target.value;
                      if (isOption(typeOptions, value)) setType(value);
                    }}
                  >
                    {typeOptions.map((option) => (
                      <option key={option} value={option}>
                        {optionLabel(option)}
                      </option>
                    ))}
                  </select>
                </div>
              )}
              {needsPriority && (
                <div>
                  <label
                    htmlFor={fieldId("priority")}
                    className="text-muted-foreground mb-1 block text-xs font-medium"
                  >
                    Priority
                  </label>
                  <select
                    id={fieldId("priority")}
                    className={selectCn}
                    value={level === "task" ? taskPriority : hierarchyPriority}
                    onChange={(e) => {
                      const value = e.target.value;
                      if (isOption(priorityOptions, value)) setPriority(value);
                    }}
                  >
                    {priorityOptions.map((option) => (
                      <option key={option} value={option}>
                        {optionLabel(option)}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </div>
          )}

          {/* Stored hour estimate (tasks only) */}
          {needsEstimate && (
            <div>
              <label
                htmlFor={fieldId("estimated-hours")}
                className="text-muted-foreground mb-1 block text-xs font-medium"
              >
                Estimated hours
              </label>
              <select
                id={fieldId("estimated-hours")}
                className={selectCn}
                value={estimatedHours}
                onChange={(e) => {
                  const value = Number(e.target.value);
                  if (isEstimatedHours(value)) {
                    setEstimatedHours(value);
                  }
                }}
              >
                {TASK_ESTIMATED_HOURS.map((hours) => (
                  <option key={hours} value={hours}>
                    {hours} {hours === 1 ? "hour" : "hours"}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Dates */}
          {needsDates && (
            <div
              className={cn(
                "grid gap-3",
                level === "milestone"
                  ? "grid-cols-1"
                  : "grid-cols-1 sm:grid-cols-2",
              )}
            >
              {level === "milestone" ? (
                <div>
                  <label
                    htmlFor={fieldId("target-date")}
                    className="text-muted-foreground mb-1 block text-xs font-medium"
                  >
                    Target Date
                  </label>
                  <input
                    id={fieldId("target-date")}
                    type="date"
                    className={inputCn}
                    value={targetDate}
                    onChange={(e) => setTargetDate(e.target.value)}
                  />
                </div>
              ) : (
                <>
                  <div>
                    <label
                      htmlFor={fieldId("start-date")}
                      className="text-muted-foreground mb-1 block text-xs font-medium"
                    >
                      Start Date
                    </label>
                    <input
                      id={fieldId("start-date")}
                      type="date"
                      max={endDate || undefined}
                      className={inputCn}
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                  <div>
                    <label
                      htmlFor={fieldId("end-date")}
                      className="text-muted-foreground mb-1 block text-xs font-medium"
                    >
                      End Date
                    </label>
                    <input
                      id={fieldId("end-date")}
                      type="date"
                      min={startDate || undefined}
                      className={inputCn}
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                    />
                  </div>
                </>
              )}
            </div>
          )}

          {/* Actions */}
          <div className="flex flex-col gap-2 pt-2 sm:flex-row">
            <Button
              type="submit"
              className="min-h-11 flex-1"
              disabled={pending}
            >
              {pending && (
                <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
              )}
              Create {LEVEL_LABELS[level]}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={onClose}
            >
              Cancel
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
