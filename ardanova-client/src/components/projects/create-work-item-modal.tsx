"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "~/components/ui/dialog";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
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

const LEVEL_LABELS: Record<CreateLevel, string> = {
  milestone: "Milestone",
  epic: "Epic",
  sprint: "Sprint",
  feature: "Feature",
  pbi: "PBI",
  task: "Task",
};

const LEVEL_ICONS: Record<CreateLevel, React.ElementType> = {
  milestone: Flag,
  epic: Target,
  sprint: Zap,
  feature: Box,
  pbi: Layers,
  task: CheckSquare,
};

const LEVEL_COLORS: Record<CreateLevel, string> = {
  milestone: "text-neon-pink",
  epic: "text-neon-purple",
  sprint: "text-neon-cyan",
  feature: "text-neon-green",
  pbi: "text-neon-yellow",
  task: "text-foreground",
};

const inputCn =
  "w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
const selectCn =
  "w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
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
  const [priority, setPriority] = useState("MEDIUM");
  const [type, setType] = useState("FEATURE");
  const [effort, setEffort] = useState("M");
  const [targetDate, setTargetDate] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [isPending, setIsPending] = useState(false);

  const utils = api.useUtils();

  const Icon = LEVEL_ICONS[level];
  const colorClass = LEVEL_COLORS[level];

  const resetForm = () => {
    setTitle("");
    setDescription("");
    setPriority("MEDIUM");
    setType("FEATURE");
    setEffort("m");
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

  const handleError = (e: any) => {
    toast.error(e?.message ?? "Failed to create item");
  };

  // Mutations
  const createMilestone = api.project.addMilestone.useMutation({ onSuccess: handleSuccess, onError: handleError });
  const createEpic = api.epic.create.useMutation({ onSuccess: handleSuccess, onError: handleError });
  const createSprint = api.sprint.create.useMutation({ onSuccess: handleSuccess, onError: handleError });
  const createFeature = api.feature.create.useMutation({ onSuccess: handleSuccess, onError: handleError });
  const createPbi = api.backlog.createPbi.useMutation({ onSuccess: handleSuccess, onError: handleError });
  const createTask = api.task.create.useMutation({ onSuccess: handleSuccess, onError: handleError });

  const pending =
    createMilestone.isPending ||
    createEpic.isPending ||
    createSprint.isPending ||
    createFeature.isPending ||
    createPbi.isPending ||
    createTask.isPending;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    switch (level) {
      case "milestone":
        createMilestone.mutate({
          projectId,
          title: title.trim(),
          description: description || undefined,
          targetDate: targetDate || undefined,
        });
        break;
      case "epic":
        createEpic.mutate({
          projectId,
          milestoneId: parentId || undefined,
          title: title.trim(),
          description: description || undefined,
          priority: priority as any,
          startDate: startDate ? new Date(startDate).toISOString() : undefined,
          endDate: endDate ? new Date(endDate).toISOString() : undefined,
        });
        break;
      case "sprint":
        createSprint.mutate({
          projectId,
          epicId: parentId || undefined,
          name: title.trim(),
          goal: description || undefined,
          startDate: startDate ? new Date(startDate).toISOString() : undefined,
          endDate: endDate ? new Date(endDate).toISOString() : undefined,
        });
        break;
      case "feature":
        createFeature.mutate({
          projectId,
          sprintId: parentId || undefined,
          title: title.trim(),
          description: description || undefined,
          priority: priority as any,
        });
        break;
      case "pbi":
        createPbi.mutate({
          projectId,
          featureId: parentId || undefined,
          title: title.trim(),
          description: description || undefined,
          type: type as any,
          priority: priority as any,
        });
        break;
      case "task":
        if (!description || description.length < 10) {
          toast.error("Task description must be at least 10 characters");
          return;
        }
        createTask.mutate({
          projectId,
          pbiId: parentId || undefined,
          title: title.trim(),
          description: description.trim(),
          type: type as any,
          priority: priority as any,
          effortEstimate: effort as any,
        });
        break;
    }
  };

  const needsDates = ["milestone", "epic", "sprint"].includes(level);
  const needsPriority = ["epic", "sprint", "feature", "pbi", "task"].includes(level);
  const needsType = ["pbi", "task"].includes(level);
  const needsEffort = level === "task";

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <Icon className={cn("h-5 w-5", colorClass)} />
            <DialogTitle>Create {LEVEL_LABELS[level]}</DialogTitle>
          </div>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Title */}
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-1 block">
              {level === "sprint" ? "Name" : "Title"} *
            </label>
            <input
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
            <label className="text-xs font-medium text-muted-foreground mb-1 block">
              {level === "sprint" ? "Goal" : "Description"}
              {level === "task" ? " * (min 10 chars)" : ""}
            </label>
            <textarea
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
            <div className={cn("grid gap-3", needsType && needsPriority ? "grid-cols-2" : "grid-cols-1")}>
              {needsType && (
                <div>
                  <label className="text-xs font-medium text-muted-foreground mb-1 block">
                    Type
                  </label>
                  <select
                    className={selectCn}
                    value={type}
                    onChange={(e) => setType(e.target.value)}
                  >
                    {level === "pbi" ? (
                      <>
                        <option value="FEATURE">Feature</option>
                        <option value="ENHANCEMENT">Enhancement</option>
                        <option value="BUG">Bug</option>
                        <option value="TECHNICAL_DEBT">Technical Debt</option>
                        <option value="SPIKE">Spike</option>
                      </>
                    ) : (
                      <>
                        <option value="feature">Feature</option>
                        <option value="bug">Bug</option>
                        <option value="improvement">Improvement</option>
                        <option value="documentation">Docs</option>
                        <option value="research">Research</option>
                        <option value="design">Design</option>
                        <option value="other">Other</option>
                      </>
                    )}
                  </select>
                </div>
              )}
              {needsPriority && (
                <div>
                  <label className="text-xs font-medium text-muted-foreground mb-1 block">
                    Priority
                  </label>
                  <select
                    className={selectCn}
                    value={priority}
                    onChange={(e) => setPriority(e.target.value)}
                  >
                    <option value="LOW">Low</option>
                    <option value="MEDIUM">Medium</option>
                    <option value="HIGH">High</option>
                    <option value="CRITICAL">Critical</option>
                  </select>
                </div>
              )}
            </div>
          )}

          {/* Effort (tasks only) */}
          {needsEffort && (
            <div>
              <label className="text-xs font-medium text-muted-foreground mb-1 block">
                Effort
              </label>
              <select
                className={selectCn}
                value={effort}
                onChange={(e) => setEffort(e.target.value)}
              >
                <option value="xs">XS (~1h)</option>
                <option value="s">S (~2h)</option>
                <option value="m">M (~4h)</option>
                <option value="l">L (~8h)</option>
                <option value="xl">XL (~16h)</option>
              </select>
            </div>
          )}

          {/* Dates */}
          {needsDates && (
            <div className={cn("grid gap-3", level === "milestone" ? "grid-cols-1" : "grid-cols-2")}>
              {level === "milestone" ? (
                <div>
                  <label className="text-xs font-medium text-muted-foreground mb-1 block">
                    Target Date
                  </label>
                  <input
                    type="date"
                    className={inputCn}
                    value={targetDate}
                    onChange={(e) => setTargetDate(e.target.value)}
                  />
                </div>
              ) : (
                <>
                  <div>
                    <label className="text-xs font-medium text-muted-foreground mb-1 block">
                      Start Date
                    </label>
                    <input
                      type="date"
                      className={inputCn}
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="text-xs font-medium text-muted-foreground mb-1 block">
                      End Date
                    </label>
                    <input
                      type="date"
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
          <div className="flex gap-2 pt-2">
            <Button type="submit" className="flex-1" disabled={pending}>
              {pending && <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />}
              Create {LEVEL_LABELS[level]}
            </Button>
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
