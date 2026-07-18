"use client";

import { useState } from "react";
import {
  ChevronRight,
  ChevronDown,
  Loader2,
  AlertCircle,
  Zap,
  Play,
  CheckCircle2,
  XCircle,
  Target,
  Calendar,
  User,
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

interface SprintsTabProps {
  projectId: string;
  canManage: boolean;
}

type Epic = RouterOutputs["epic"]["getByMilestoneId"][number];
type Sprint = RouterOutputs["sprint"]["getByEpicId"][number];

const sprintStatusVariant: Record<
  string,
  "outline" | "neon" | "neon-green" | "destructive"
> = {
  PLANNED: "outline",
  ACTIVE: "neon",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const pbiTypeVariant: Record<
  string,
  "neon" | "neon-pink" | "neon-purple" | "secondary" | "warning"
> = {
  FEATURE: "neon",
  BUG: "neon-pink",
  ENHANCEMENT: "neon-purple",
  TECHNICAL_DEBT: "secondary",
  SPIKE: "warning",
};

export default function SprintsTab({ projectId, canManage }: SprintsTabProps) {
  const {
    data: milestones,
    isLoading,
    error,
    refetch,
  } = api.project.getMilestones.useQuery({ projectId });
  const [selectedSprintId, setSelectedSprintId] = useState<string | null>(null);

  if (isLoading) {
    return (
      <div
        className="flex items-center justify-center gap-3 py-20"
        role="status"
      >
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading sprints...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center"
        role="alert"
      >
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div className="flex-1">
          <p className="text-destructive font-mono text-sm font-bold">
            FAILED TO LOAD SPRINTS
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

  if (milestoneList.length === 0) {
    return (
      <div className="border-border bg-card border-2 p-8 text-center">
        <Zap className="text-muted-foreground mx-auto mb-2 size-8" />
        <p className="text-muted-foreground font-mono text-sm">
          NO SPRINTS AVAILABLE
        </p>
        <p className="text-muted-foreground mt-1 text-xs">
          Create milestones and epics in the Roadmap tab first, then add
          sprints.
        </p>
      </div>
    );
  }

  return (
    <div className="flex min-h-[400px] flex-col gap-4 lg:flex-row">
      {/* Left sidebar: Epic accordion with sprints */}
      <div className="w-full space-y-2 overflow-y-auto lg:w-[300px] lg:shrink-0">
        <h3 className="text-muted-foreground mb-3 px-1 font-mono text-xs tracking-widest uppercase">
          EPICS &amp; SPRINTS
        </h3>
        {milestoneList.map((milestone) => (
          <MilestoneAccordion
            key={milestone.id}
            milestone={milestone}
            selectedSprintId={selectedSprintId}
            onSelectSprint={setSelectedSprintId}
          />
        ))}
      </div>

      {/* Main area: Selected sprint detail */}
      <div className="min-w-0 flex-1">
        {selectedSprintId ? (
          <SprintDetail
            sprintId={selectedSprintId}
            canManage={canManage}
            projectId={projectId}
          />
        ) : (
          <div className="border-border bg-card flex h-full flex-col items-center justify-center border-2 p-8 text-center">
            <Zap className="text-muted-foreground mb-2 size-8" />
            <p className="text-muted-foreground font-mono text-sm">
              SELECT A SPRINT
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              Choose a sprint from the sidebar to view details.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

// Milestone accordion.
function MilestoneAccordion({
  milestone,
  selectedSprintId,
  onSelectSprint,
}: {
  milestone: ProjectMilestoneDto;
  selectedSprintId: string | null;
  onSelectSprint: (id: string) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(
    milestone.status === "IN_PROGRESS",
  );

  const {
    data: epics,
    error: epicsError,
    isLoading: epicsLoading,
    refetch: retryEpics,
  } = api.epic.getByMilestoneId.useQuery(
    { milestoneId: milestone.id },
    { enabled: isExpanded },
  );

  return (
    <div className="border-border bg-card border-2">
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        aria-expanded={isExpanded}
        className="hover:bg-muted/50 flex min-h-11 w-full items-center gap-2 p-3 text-left transition-colors"
      >
        {isExpanded ? (
          <ChevronDown className="text-muted-foreground size-3 shrink-0" />
        ) : (
          <ChevronRight className="text-muted-foreground size-3 shrink-0" />
        )}
        <Target className="text-muted-foreground size-3 shrink-0" />
        <span className="text-foreground flex-1 truncate text-xs font-medium">
          {milestone.title}
        </span>
        <Badge
          variant={
            milestone.status === "COMPLETED"
              ? "neon-green"
              : milestone.status === "IN_PROGRESS"
                ? "neon"
                : "outline"
          }
          size="sm"
        >
          {milestone.status?.replace("_", " ")}
        </Badge>
      </button>

      {isExpanded && (
        <div className="border-border border-t-2">
          {epicsLoading ? (
            <div
              className="flex items-center justify-center gap-2 p-3"
              role="status"
            >
              <Loader2 className="text-muted-foreground size-3 animate-spin" />
              <span className="sr-only">Loading epics...</span>
            </div>
          ) : epicsError ? (
            <div
              className="border-destructive/40 bg-destructive/5 space-y-2 border p-3"
              role="alert"
            >
              <p className="text-destructive text-[10px]">
                Epics could not be loaded: {epicsError.message}
              </p>
              <Button
                type="button"
                variant="outline"
                className="min-h-11 text-xs"
                onClick={() => void retryEpics()}
              >
                <RefreshCw className="mr-1 size-3" />
                Retry
              </Button>
            </div>
          ) : epics?.length === 0 ? (
            <p className="text-muted-foreground p-3 text-center font-mono text-[10px]">
              NO EPICS
            </p>
          ) : (
            epics?.map((epic) => (
              <EpicAccordion
                key={epic.id}
                epic={epic}
                selectedSprintId={selectedSprintId}
                onSelectSprint={onSelectSprint}
              />
            ))
          )}
        </div>
      )}
    </div>
  );
}

// Epic accordion.
function EpicAccordion({
  epic,
  selectedSprintId,
  onSelectSprint,
}: {
  epic: Epic;
  selectedSprintId: string | null;
  onSelectSprint: (id: string) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(epic.status === "IN_PROGRESS");

  const {
    data: sprints,
    error: sprintsError,
    isLoading: sprintsLoading,
    refetch: retrySprints,
  } = api.sprint.getByEpicId.useQuery(
    { epicId: epic.id },
    { enabled: isExpanded },
  );

  return (
    <div className="border-border/50 border-t">
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        aria-expanded={isExpanded}
        className="hover:bg-muted/30 flex min-h-11 w-full items-center gap-2 px-4 py-2 text-left transition-colors"
      >
        {isExpanded ? (
          <ChevronDown className="text-muted-foreground size-3 shrink-0" />
        ) : (
          <ChevronRight className="text-muted-foreground size-3 shrink-0" />
        )}
        <span className="text-foreground flex-1 truncate text-xs">
          {epic.title}
        </span>
      </button>

      {isExpanded && (
        <div className="pl-6">
          {sprintsLoading ? (
            <div className="flex items-center gap-1 px-2 py-2" role="status">
              <Loader2 className="text-muted-foreground size-3 animate-spin" />
              <span className="sr-only">Loading nested sprints...</span>
            </div>
          ) : sprintsError ? (
            <div
              className="border-destructive/40 bg-destructive/5 space-y-2 border p-2"
              role="alert"
            >
              <p className="text-destructive text-[10px]">
                Sprints could not be loaded: {sprintsError.message}
              </p>
              <Button
                type="button"
                variant="outline"
                className="min-h-11 text-xs"
                onClick={() => void retrySprints()}
              >
                <RefreshCw className="mr-1 size-3" />
                Retry
              </Button>
            </div>
          ) : sprints?.length === 0 ? (
            <p className="text-muted-foreground px-2 py-2 font-mono text-[10px]">
              No sprints
            </p>
          ) : (
            sprints?.map((sprint: Sprint) => (
              <button
                type="button"
                key={sprint.id}
                onClick={() => onSelectSprint(sprint.id)}
                aria-current={
                  selectedSprintId === sprint.id ? "true" : undefined
                }
                className={cn(
                  "flex min-h-11 w-full items-center gap-2 px-2 py-1.5 text-left text-xs transition-colors",
                  selectedSprintId === sprint.id
                    ? "bg-primary/10 text-primary border-primary border-l-2"
                    : "text-muted-foreground hover:text-foreground hover:bg-muted/30",
                )}
              >
                <Zap className="size-3 shrink-0" />
                <span className="flex-1 truncate">
                  {sprint.name ?? "Untitled sprint"}
                </span>
                <Badge
                  variant={sprintStatusVariant[sprint.status] ?? "outline"}
                  size="sm"
                >
                  {sprint.status}
                </Badge>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  );
}

// Sprint detail.
function SprintDetail({
  sprintId,
  canManage,
  projectId,
}: {
  sprintId: string;
  canManage: boolean;
  projectId: string;
}) {
  const {
    data: sprint,
    isLoading,
    error,
    refetch: retrySprint,
  } = api.sprint.getById.useQuery({ id: sprintId });
  const {
    data: featuresData,
    isLoading: featuresLoading,
    error: featuresError,
    refetch: retryFeatures,
  } = api.feature.getBySprintId.useQuery({ sprintId });
  const {
    data: projectPbis,
    isLoading: pbisLoading,
    error: pbisError,
    refetch: retryPbis,
  } = api.backlog.getPbisByProjectId.useQuery({ projectId });
  const utils = api.useUtils();

  const startMutation = api.sprint.start.useMutation({
    onSuccess: () => {
      toast.success("Sprint started");
      void utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  const completeMutation = api.sprint.complete.useMutation({
    onSuccess: () => {
      toast.success("Sprint completed");
      void utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  const cancelMutation = api.sprint.cancel.useMutation({
    onSuccess: () => {
      toast.success("Sprint cancelled");
      void utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  if (isLoading) {
    return (
      <div
        className="flex items-center justify-center gap-3 py-20"
        role="status"
      >
        <Loader2 className="text-primary size-6 animate-spin" />
        <span className="sr-only">Loading sprint details...</span>
      </div>
    );
  }

  if (error || !sprint) {
    return (
      <div
        className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center"
        role="alert"
      >
        <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
        <div className="flex-1">
          <p className="text-destructive font-mono text-sm font-bold">
            SPRINT NOT FOUND
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {error?.message ?? "Sprint data unavailable."}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          className="min-h-11 shrink-0"
          onClick={() => void retrySprint()}
        >
          <RefreshCw className="mr-2 size-4" />
          Retry
        </Button>
      </div>
    );
  }

  if (featuresLoading || pbisLoading) {
    return (
      <div
        className="border-border bg-card flex items-center justify-center gap-3 border-2 px-6 py-12"
        role="status"
      >
        <Loader2 className="text-primary size-5 animate-spin" />
        <span className="text-muted-foreground font-mono text-xs">
          Loading sprint work...
        </span>
      </div>
    );
  }

  if (featuresError || pbisError) {
    const workError = featuresError ?? pbisError;
    return (
      <div
        className="border-destructive/40 bg-destructive/5 flex flex-col items-start gap-4 border-2 p-6 sm:flex-row sm:items-center sm:justify-between"
        role="alert"
      >
        <div className="flex items-start gap-3">
          <AlertCircle className="text-destructive mt-0.5 size-5 shrink-0" />
          <div>
            <p className="text-destructive font-mono text-sm font-bold">
              SPRINT WORK COULD NOT BE LOADED
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              {workError?.message ?? "Unknown error"}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          className="min-h-11 shrink-0"
          onClick={() => {
            if (featuresError) void retryFeatures();
            if (pbisError) void retryPbis();
          }}
        >
          <RefreshCw className="mr-2 size-4" />
          Retry
        </Button>
      </div>
    );
  }

  const features = featuresData ?? [];
  const pbis = (projectPbis ?? []).filter((pbi) => pbi.sprintId === sprintId);
  const itemCount = features.length + pbis.length;
  const completedItemCount =
    features.filter((feature) => feature.status === "COMPLETED").length +
    pbis.filter((pbi) => pbi.status === "DONE").length;
  const progress =
    itemCount > 0 ? Math.round((completedItemCount / itemCount) * 100) : 0;
  const velocity = sprint.velocity ?? null;

  return (
    <div className="space-y-4">
      {/* Sprint header */}
      <div className="bg-card border-border space-y-3 border-2 p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 flex-wrap items-center gap-3">
            <Zap className="text-primary size-5 shrink-0" />
            <h3 className="text-foreground min-w-0 text-lg font-bold break-words">
              {sprint.name ?? "Untitled sprint"}
            </h3>
            <Badge variant={sprintStatusVariant[sprint.status] ?? "outline"}>
              {sprint.status}
            </Badge>
          </div>

          {/* Sprint lifecycle buttons (project managers only) */}
          {canManage && (
            <div className="flex flex-wrap items-center gap-2">
              {sprint.status === "PLANNED" && (
                <Button
                  size="sm"
                  variant="neon"
                  onClick={() => startMutation.mutate({ id: sprintId })}
                  disabled={startMutation.isPending}
                  className="min-h-11 font-mono text-xs"
                >
                  {startMutation.isPending ? (
                    <Loader2 className="mr-1 size-3 animate-spin" />
                  ) : (
                    <Play className="mr-1 size-3" />
                  )}
                  START
                </Button>
              )}
              {sprint.status === "ACTIVE" && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => completeMutation.mutate({ id: sprintId })}
                  disabled={completeMutation.isPending}
                  className="border-neon-green text-neon-green hover:bg-neon-green/10 min-h-11 font-mono text-xs"
                >
                  {completeMutation.isPending ? (
                    <Loader2 className="mr-1 size-3 animate-spin" />
                  ) : (
                    <CheckCircle2 className="mr-1 size-3" />
                  )}
                  COMPLETE
                </Button>
              )}
              {(sprint.status === "PLANNED" || sprint.status === "ACTIVE") && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    const confirmed = window.confirm(
                      "Cancel this sprint? Its work stays available, but the sprint cannot be resumed.",
                    );
                    if (confirmed) {
                      cancelMutation.mutate({ id: sprintId });
                    }
                  }}
                  disabled={cancelMutation.isPending}
                  className="border-destructive text-destructive hover:bg-destructive/10 min-h-11 font-mono text-xs"
                >
                  {cancelMutation.isPending ? (
                    <Loader2 className="mr-1 size-3 animate-spin" />
                  ) : (
                    <XCircle className="mr-1 size-3" />
                  )}
                  CANCEL
                </Button>
              )}
            </div>
          )}
        </div>

        {/* Sprint meta */}
        <div className="text-muted-foreground flex flex-wrap items-center gap-4 text-xs">
          {sprint.goal && (
            <span className="text-foreground italic">
              &quot;{sprint.goal}&quot;
            </span>
          )}
          {sprint.startDate && sprint.endDate && (
            <span className="flex items-center gap-1">
              <Calendar className="size-3" />
              {new Date(sprint.startDate).toLocaleDateString("en-US", {
                month: "short",
                day: "numeric",
              })}
              {" - "}
              {new Date(sprint.endDate).toLocaleDateString("en-US", {
                month: "short",
                day: "numeric",
                year: "numeric",
              })}
            </span>
          )}
          {velocity !== null && (
            <span className="font-mono">Velocity: {velocity}</span>
          )}
        </div>

        {/* Progress bar */}
        {itemCount > 0 && (
          <div className="space-y-1">
            <div className="text-muted-foreground flex justify-between text-[10px]">
              <span>Progress</span>
              <span>{progress}%</span>
            </div>
            <Progress
              value={progress}
              variant="neon"
              className="h-2"
              aria-label="Sprint progress"
            />
          </div>
        )}
      </div>

      {/* Features */}
      {features.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-muted-foreground px-1 font-mono text-xs tracking-widest uppercase">
            FEATURES
          </h4>
          <div className="grid gap-2 sm:grid-cols-2">
            {features.map((feature) => (
              <div
                key={feature.id}
                className="bg-card border-border space-y-2 border-2 p-3"
              >
                <div className="flex items-center gap-2">
                  <span className="text-foreground flex-1 truncate text-sm font-medium">
                    {feature.title}
                  </span>
                  {feature.status && (
                    <Badge
                      variant={
                        feature.status === "COMPLETED"
                          ? "neon-green"
                          : feature.status === "IN_PROGRESS"
                            ? "neon"
                            : "outline"
                      }
                      size="sm"
                    >
                      {feature.status?.replace("_", " ")}
                    </Badge>
                  )}
                </div>
                {feature.description && (
                  <p className="text-muted-foreground line-clamp-2 text-xs">
                    {feature.description}
                  </p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* PBIs */}
      {pbis.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-muted-foreground px-1 font-mono text-xs tracking-widest uppercase">
            BACKLOG ITEMS
          </h4>
          <div className="space-y-1">
            {pbis.map((pbi) => (
              <div
                key={pbi.id}
                className="bg-card border-border hover:border-primary flex min-w-0 flex-wrap items-center gap-3 border-2 p-3 transition-colors sm:flex-nowrap"
              >
                {/* Type badge */}
                {pbi.type && (
                  <Badge
                    variant={pbiTypeVariant[pbi.type] ?? "secondary"}
                    size="sm"
                  >
                    {pbi.type}
                  </Badge>
                )}

                {/* Title */}
                <span className="text-foreground min-w-0 flex-1 basis-40 truncate text-sm font-medium">
                  {pbi.title}
                </span>

                {/* Status */}
                {pbi.status && (
                  <Badge
                    variant={
                      pbi.status === "DONE"
                        ? "neon-green"
                        : pbi.status === "IN_PROGRESS"
                          ? "neon"
                          : pbi.status === "CANCELLED"
                            ? "destructive"
                            : "outline"
                    }
                    size="sm"
                  >
                    {pbi.status?.replace("_", " ")}
                  </Badge>
                )}

                {/* Story points */}
                {pbi.storyPoints != null && (
                  <span className="text-muted-foreground font-mono text-xs">
                    {pbi.storyPoints} pts
                  </span>
                )}

                {/* Assignee */}
                {pbi.assigneeId && (
                  <Avatar className="size-5 shrink-0" title="Assigned">
                    <AvatarFallback className="text-[8px]">
                      <User className="size-3" />
                    </AvatarFallback>
                  </Avatar>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Empty state when no features and no PBIs */}
      {features.length === 0 && pbis.length === 0 && (
        <div className="border-border bg-card border-2 p-8 text-center">
          <p className="text-muted-foreground font-mono text-sm">
            NO ITEMS IN THIS SPRINT
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            Add features and backlog items to plan this sprint.
          </p>
        </div>
      )}
    </div>
  );
}
