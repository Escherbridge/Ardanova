"use client";

import { useState } from "react";
import {
  Plus,
  ChevronRight,
  ChevronDown,
  Calendar,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Target,
  RefreshCw,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Avatar, AvatarFallback } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { api, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";
import type { ProjectMilestoneDto } from "~/lib/contracts/project-contract";

interface RoadmapTabProps {
  projectId: string;
  isOwner: boolean;
}

type Epic = RouterOutputs["epic"]["getByMilestoneId"][number];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getOptionalFiniteNumber(value: unknown, key: string): number | null {
  if (!isRecord(value)) return null;
  const candidate = value[key];
  return typeof candidate === "number" && Number.isFinite(candidate)
    ? candidate
    : null;
}

const milestoneStatusVariant: Record<
  string,
  "outline" | "neon" | "neon-green" | "destructive"
> = {
  PLANNED: "outline",
  IN_PROGRESS: "neon",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const epicStatusVariant: Record<
  string,
  "outline" | "neon" | "neon-green" | "neon-purple" | "destructive"
> = {
  PLANNED: "outline",
  IN_PROGRESS: "neon",
  COMPLETED: "neon-green",
  REVIEW: "neon-purple",
  CANCELLED: "destructive",
};

export default function RoadmapTab({ projectId, isOwner }: RoadmapTabProps) {
  const {
    data: milestones,
    isLoading,
    error,
    refetch,
  } = api.project.getMilestones.useQuery({ projectId });

  const [showAddMilestone, setShowAddMilestone] = useState(false);
  const [newMilestoneTitle, setNewMilestoneTitle] = useState("");
  const [newMilestoneDate, setNewMilestoneDate] = useState("");

  const utils = api.useUtils();
  const addMilestoneMutation = api.project.addMilestone.useMutation({
    onSuccess: () => {
      toast.success("Milestone added");
      setNewMilestoneTitle("");
      setNewMilestoneDate("");
      setShowAddMilestone(false);
      void utils.project.getMilestones.invalidate({ projectId });
    },
    onError: (err) => {
      toast.error(`Failed to add milestone: ${err.message}`);
    },
  });

  const handleAddMilestone = () => {
    if (!newMilestoneTitle.trim()) return;
    addMilestoneMutation.mutate({
      projectId,
      title: newMilestoneTitle.trim(),
      targetDate: newMilestoneDate
        ? new Date(newMilestoneDate).toISOString()
        : undefined,
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="text-primary size-6 animate-spin" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center">
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div className="flex-1">
          <p className="text-destructive font-mono text-sm font-bold">
            FAILED TO LOAD ROADMAP
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

  const milestoneList = milestones ?? [];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
          PROJECT ROADMAP
        </h3>
        {isOwner && (
          <Button
            variant="neon"
            size="sm"
            onClick={() => setShowAddMilestone(!showAddMilestone)}
            className="min-h-11 font-mono text-xs"
          >
            <Plus className="mr-1 size-4" />
            ADD MILESTONE
          </Button>
        )}
      </div>

      {/* Add milestone form */}
      {showAddMilestone && (
        <div className="bg-card border-border space-y-3 border-2 p-4">
          <input
            type="text"
            placeholder="Milestone title..."
            value={newMilestoneTitle}
            onChange={(e) => setNewMilestoneTitle(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleAddMilestone();
              if (e.key === "Escape") setShowAddMilestone(false);
            }}
            className="border-border focus:border-primary min-h-11 w-full border-2 bg-transparent px-3 py-2 text-sm focus:outline-none"
            autoFocus
          />
          <div className="flex items-center gap-3">
            <input
              type="date"
              value={newMilestoneDate}
              onChange={(e) => setNewMilestoneDate(e.target.value)}
              className="bg-card border-border focus:border-primary min-h-11 border-2 px-3 py-2 text-sm focus:outline-none"
            />
            <Button
              size="sm"
              variant="neon"
              onClick={handleAddMilestone}
              disabled={
                addMilestoneMutation.isPending || !newMilestoneTitle.trim()
              }
              className="min-h-11 font-mono text-xs"
            >
              {addMilestoneMutation.isPending ? (
                <Loader2 className="size-3 animate-spin" />
              ) : (
                "CREATE"
              )}
            </Button>
            <Button
              size="sm"
              variant="ghost"
              onClick={() => setShowAddMilestone(false)}
              className="min-h-11 text-xs"
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      {/* Timeline */}
      {milestoneList.length === 0 ? (
        <div className="border-border bg-card border-2 p-8 text-center">
          <Target className="text-muted-foreground mx-auto mb-2 size-8" />
          <p className="text-muted-foreground font-mono text-sm">
            NO MILESTONES YET
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            Add milestones to track your project roadmap.
          </p>
        </div>
      ) : (
        <div className="relative">
          {/* Vertical timeline line */}
          <div className="border-border absolute top-0 bottom-0 left-[7px] border-l-2" />

          <div className="space-y-0">
            {milestoneList.map((milestone) => (
              <MilestoneNode
                key={milestone.id}
                milestone={milestone}
                isOwner={isOwner}
                projectId={projectId}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

/* ─── Milestone Node ─── */
function MilestoneNode({
  milestone,
  isOwner,
  projectId,
}: {
  milestone: ProjectMilestoneDto;
  isOwner: boolean;
  projectId: string;
}) {
  const [isExpanded, setIsExpanded] = useState(
    milestone.status === "IN_PROGRESS",
  );
  const [showAddEpic, setShowAddEpic] = useState(false);
  const [newEpicTitle, setNewEpicTitle] = useState("");

  const {
    data: epics,
    error: epicsError,
    isLoading: epicsLoading,
    refetch: retryEpics,
  } = api.epic.getByMilestoneId.useQuery(
    { milestoneId: milestone.id },
    { enabled: isExpanded },
  );

  const utils = api.useUtils();
  const createEpicMutation = api.epic.create.useMutation({
    onSuccess: () => {
      toast.success("Epic added");
      setNewEpicTitle("");
      setShowAddEpic(false);
      void utils.epic.getByMilestoneId.invalidate({
        milestoneId: milestone.id,
      });
    },
    onError: (err) => {
      toast.error(`Failed to create epic: ${err.message}`);
    },
  });

  const handleAddEpic = () => {
    if (!newEpicTitle.trim()) return;
    createEpicMutation.mutate({
      milestoneId: milestone.id,
      title: newEpicTitle.trim(),
      projectId,
    });
  };

  const statusColor =
    milestone.status === "COMPLETED"
      ? "bg-neon-green border-neon-green"
      : milestone.status === "IN_PROGRESS"
        ? "bg-primary border-primary"
        : milestone.status === "CANCELLED"
          ? "bg-destructive border-destructive"
          : "bg-muted border-border";

  const isActive = milestone.status === "IN_PROGRESS";

  return (
    <div className="relative pb-6 pl-8">
      {/* Timeline node */}
      <div
        className={cn(
          "absolute top-1 left-0 z-10 h-4 w-4 border-2",
          statusColor,
          isActive && "animate-pulse",
        )}
      />

      {/* Milestone card */}
      <div className="bg-card border-border hover:border-primary border-2 transition-colors">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="flex min-h-11 w-full items-center gap-3 p-4 text-left"
        >
          {isExpanded ? (
            <ChevronDown className="text-muted-foreground size-4 shrink-0" />
          ) : (
            <ChevronRight className="text-muted-foreground size-4 shrink-0" />
          )}

          <div className="min-w-0 flex-1">
            <div className="mb-1 flex items-center gap-2">
              <h4 className="text-foreground truncate text-sm font-bold">
                {milestone.title}
              </h4>
              <Badge
                variant={milestoneStatusVariant[milestone.status] ?? "outline"}
                size="sm"
              >
                {milestone.status?.replace("_", " ")}
              </Badge>
            </div>
            <div className="text-muted-foreground flex items-center gap-3 text-xs">
              {milestone.targetDate && (
                <span className="flex items-center gap-1">
                  <Calendar className="size-3" />
                  {new Date(milestone.targetDate).toLocaleDateString("en-US", {
                    month: "short",
                    day: "numeric",
                    year: "numeric",
                  })}
                </span>
              )}
            </div>
          </div>

          {/* Completion indicator */}
          {milestone.status === "COMPLETED" && (
            <CheckCircle2 className="text-neon-green size-5 shrink-0" />
          )}
        </button>

        {/* Expanded content: Epics */}
        {isExpanded && (
          <div className="border-border space-y-3 border-t-2 p-4">
            {epicsLoading ? (
              <div className="flex items-center justify-center gap-2 py-4">
                <Loader2 className="text-muted-foreground size-4 animate-spin" />
                <span className="text-muted-foreground text-xs">
                  Loading epics...
                </span>
              </div>
            ) : epicsError ? (
              <div className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-3 border p-3 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-destructive text-xs">
                  Epics could not be loaded: {epicsError.message}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11 shrink-0 text-xs"
                  onClick={() => void retryEpics()}
                >
                  <RefreshCw className="mr-1 size-3" />
                  Retry
                </Button>
              </div>
            ) : epics?.length === 0 ? (
              <p className="text-muted-foreground py-4 text-center font-mono text-xs">
                NO EPICS IN THIS MILESTONE
              </p>
            ) : (
              epics?.map((epic) => <EpicCard key={epic.id} epic={epic} />)
            )}

            {/* Add epic */}
            {isOwner && (
              <div className="mt-2">
                {showAddEpic ? (
                  <div className="border-border space-y-2 border-2 border-dashed p-3">
                    <input
                      type="text"
                      placeholder="Epic title..."
                      value={newEpicTitle}
                      onChange={(e) => setNewEpicTitle(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") handleAddEpic();
                        if (e.key === "Escape") setShowAddEpic(false);
                      }}
                      className="border-border focus:border-primary min-h-11 w-full border-2 bg-transparent px-3 py-2 text-sm focus:outline-none"
                      autoFocus
                    />
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        variant="neon"
                        onClick={handleAddEpic}
                        disabled={
                          createEpicMutation.isPending || !newEpicTitle.trim()
                        }
                        className="min-h-11 font-mono text-xs"
                      >
                        {createEpicMutation.isPending ? (
                          <Loader2 className="size-3 animate-spin" />
                        ) : (
                          "ADD EPIC"
                        )}
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => setShowAddEpic(false)}
                        className="min-h-11 text-xs"
                      >
                        Cancel
                      </Button>
                    </div>
                  </div>
                ) : (
                  <button
                    onClick={() => setShowAddEpic(true)}
                    className="text-muted-foreground hover:text-foreground border-border hover:border-primary flex min-h-11 w-full items-center gap-1 border-2 border-dashed px-3 py-2 text-xs transition-colors"
                  >
                    <Plus className="size-3" />
                    Add epic
                  </button>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Epic Card ─── */
function EpicCard({ epic }: { epic: Epic }) {
  const [isExpanded, setIsExpanded] = useState(false);

  const {
    data: sprints,
    error: sprintsError,
    isLoading: sprintsLoading,
    refetch: retrySprints,
  } = api.sprint.getByEpicId.useQuery(
    { epicId: epic.id },
    { enabled: isExpanded },
  );

  const progress = getOptionalFiniteNumber(epic, "progress") ?? 0;

  return (
    <div className="bg-muted/30 border-border border-2">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex min-h-11 w-full items-center gap-3 p-3 text-left"
      >
        {isExpanded ? (
          <ChevronDown className="text-muted-foreground size-3 shrink-0" />
        ) : (
          <ChevronRight className="text-muted-foreground size-3 shrink-0" />
        )}

        <div className="min-w-0 flex-1">
          <div className="mb-1 flex items-center gap-2">
            <span className="text-foreground truncate text-sm font-medium">
              {epic.title}
            </span>
            {epic.status && (
              <Badge
                variant={epicStatusVariant[epic.status] ?? "outline"}
                size="sm"
              >
                {epic.status?.replace("_", " ")}
              </Badge>
            )}
            {epic.priority && (
              <Badge
                variant={
                  epic.priority === "CRITICAL"
                    ? "destructive"
                    : epic.priority === "HIGH"
                      ? "warning"
                      : "secondary"
                }
                size="sm"
              >
                {epic.priority}
              </Badge>
            )}
          </div>
          {progress > 0 && (
            <Progress value={progress} variant="neon" className="h-1.5" />
          )}
        </div>

        {epic.assigneeId && (
          <Avatar className="size-5 shrink-0" title="Assigned">
            <AvatarFallback className="text-[8px]">A</AvatarFallback>
          </Avatar>
        )}
      </button>

      {/* Sprints under epic */}
      {isExpanded && (
        <div className="border-border space-y-2 border-t-2 p-3">
          {sprintsLoading ? (
            <div className="flex items-center justify-center gap-2 py-2">
              <Loader2 className="text-muted-foreground size-3 animate-spin" />
              <span className="text-muted-foreground text-[10px]">
                Loading sprints...
              </span>
            </div>
          ) : sprintsError ? (
            <div className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-3 border p-3 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-destructive text-[10px]">
                Sprints could not be loaded: {sprintsError.message}
              </p>
              <Button
                type="button"
                variant="outline"
                className="min-h-11 shrink-0 text-xs"
                onClick={() => void retrySprints()}
              >
                <RefreshCw className="mr-1 size-3" />
                Retry
              </Button>
            </div>
          ) : sprints?.length === 0 ? (
            <p className="text-muted-foreground py-2 text-center font-mono text-[10px]">
              NO SPRINTS
            </p>
          ) : (
            sprints?.map((sprint) => (
              <div
                key={sprint.id}
                className="border-border bg-card flex items-center gap-2 border-2 px-3 py-2 text-xs"
              >
                <Badge
                  variant={
                    sprint.status === "ACTIVE"
                      ? "neon"
                      : sprint.status === "COMPLETED"
                        ? "neon-green"
                        : sprint.status === "CANCELLED"
                          ? "destructive"
                          : "outline"
                  }
                  size="sm"
                >
                  {sprint.status}
                </Badge>
                <span className="text-foreground flex-1 truncate font-medium">
                  {sprint.name ?? "Untitled sprint"}
                </span>
                {sprint.startDate && sprint.endDate && (
                  <span className="text-muted-foreground whitespace-nowrap">
                    {new Date(sprint.startDate).toLocaleDateString("en-US", {
                      month: "short",
                      day: "numeric",
                    })}
                    {" - "}
                    {new Date(sprint.endDate).toLocaleDateString("en-US", {
                      month: "short",
                      day: "numeric",
                    })}
                  </span>
                )}
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
