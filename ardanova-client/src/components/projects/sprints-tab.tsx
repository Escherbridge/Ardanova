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
  Plus,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { toast } from "sonner";

interface SprintsTabProps {
  projectId: string;
  isOwner: boolean;
}

const sprintStatusVariant: Record<string, "outline" | "neon" | "neon-green" | "destructive"> = {
  PLANNED: "outline",
  ACTIVE: "neon",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const pbiTypeVariant: Record<string, "neon" | "neon-pink" | "neon-purple" | "secondary" | "warning"> = {
  FEATURE: "neon",
  BUG: "neon-pink",
  TASK: "secondary",
  IMPROVEMENT: "neon-purple",
  RESEARCH: "warning",
};

export default function SprintsTab({ projectId, isOwner }: SprintsTabProps) {
  const { data: milestones, isLoading, error } = api.project.getMilestones.useQuery({ projectId });
  const [selectedSprintId, setSelectedSprintId] = useState<string | null>(null);

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
          <p className="font-mono text-sm font-bold text-destructive">FAILED TO LOAD SPRINTS</p>
          <p className="text-xs text-muted-foreground mt-1">{error.message}</p>
        </div>
      </div>
    );
  }

  const milestoneList = (milestones as any[]) ?? [];

  if (milestoneList.length === 0) {
    return (
      <div className="border-2 border-border bg-card p-8 text-center">
        <Zap className="size-8 text-muted-foreground mx-auto mb-2" />
        <p className="text-sm text-muted-foreground font-mono">NO SPRINTS AVAILABLE</p>
        <p className="text-xs text-muted-foreground mt-1">
          Create milestones and epics in the Roadmap tab first, then add sprints.
        </p>
      </div>
    );
  }

  return (
    <div className="flex gap-4 min-h-[400px]">
      {/* Left sidebar: Epic accordion with sprints */}
      <div className="w-[300px] shrink-0 space-y-2 overflow-y-auto">
        <h3 className="text-xs font-mono uppercase tracking-widest text-muted-foreground px-1 mb-3">
          EPICS &amp; SPRINTS
        </h3>
        {milestoneList.map((milestone: any) => (
          <MilestoneAccordion
            key={milestone.id}
            milestone={milestone}
            selectedSprintId={selectedSprintId}
            onSelectSprint={setSelectedSprintId}
          />
        ))}
      </div>

      {/* Main area: Selected sprint detail */}
      <div className="flex-1 min-w-0">
        {selectedSprintId ? (
          <SprintDetail sprintId={selectedSprintId} isOwner={isOwner} projectId={projectId} />
        ) : (
          <div className="border-2 border-border bg-card p-8 text-center h-full flex flex-col items-center justify-center">
            <Zap className="size-8 text-muted-foreground mb-2" />
            <p className="text-sm text-muted-foreground font-mono">SELECT A SPRINT</p>
            <p className="text-xs text-muted-foreground mt-1">
              Choose a sprint from the sidebar to view details.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Milestone Accordion (sidebar) ─── */
function MilestoneAccordion({
  milestone,
  selectedSprintId,
  onSelectSprint,
}: {
  milestone: any;
  selectedSprintId: string | null;
  onSelectSprint: (id: string) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(
    milestone.status === "IN_PROGRESS" || milestone.status === "ACTIVE"
  );

  const { data: epics, isLoading: epicsLoading } = api.epic.getByMilestoneId.useQuery(
    { milestoneId: milestone.id },
    { enabled: isExpanded }
  );

  return (
    <div className="border-2 border-border bg-card">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center gap-2 p-3 text-left hover:bg-muted/50 transition-colors"
      >
        {isExpanded ? (
          <ChevronDown className="size-3 text-muted-foreground shrink-0" />
        ) : (
          <ChevronRight className="size-3 text-muted-foreground shrink-0" />
        )}
        <Target className="size-3 text-muted-foreground shrink-0" />
        <span className="text-xs font-medium text-foreground truncate flex-1">
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
        <div className="border-t-2 border-border">
          {epicsLoading ? (
            <div className="flex items-center gap-2 p-3 justify-center">
              <Loader2 className="size-3 animate-spin text-muted-foreground" />
            </div>
          ) : (epics as any[])?.length === 0 ? (
            <p className="text-[10px] text-muted-foreground font-mono text-center p-3">
              NO EPICS
            </p>
          ) : (
            (epics as any[])?.map((epic: any) => (
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

/* ─── Epic Accordion (sidebar) ─── */
function EpicAccordion({
  epic,
  selectedSprintId,
  onSelectSprint,
}: {
  epic: any;
  selectedSprintId: string | null;
  onSelectSprint: (id: string) => void;
}) {
  const [isExpanded, setIsExpanded] = useState(
    epic.status === "IN_PROGRESS" || epic.status === "ACTIVE"
  );

  const { data: sprints, isLoading: sprintsLoading } = api.sprint.getByEpicId.useQuery(
    { epicId: epic.id },
    { enabled: isExpanded }
  );

  return (
    <div className="border-t border-border/50">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center gap-2 px-4 py-2 text-left hover:bg-muted/30 transition-colors"
      >
        {isExpanded ? (
          <ChevronDown className="size-3 text-muted-foreground shrink-0" />
        ) : (
          <ChevronRight className="size-3 text-muted-foreground shrink-0" />
        )}
        <span className="text-xs text-foreground truncate flex-1">{epic.title}</span>
      </button>

      {isExpanded && (
        <div className="pl-6">
          {sprintsLoading ? (
            <div className="flex items-center gap-1 px-2 py-2">
              <Loader2 className="size-3 animate-spin text-muted-foreground" />
            </div>
          ) : (sprints as any[])?.length === 0 ? (
            <p className="text-[10px] text-muted-foreground font-mono px-2 py-2">
              No sprints
            </p>
          ) : (
            (sprints as any[])?.map((sprint: any) => (
              <button
                key={sprint.id}
                onClick={() => onSelectSprint(sprint.id)}
                className={cn(
                  "w-full flex items-center gap-2 px-2 py-1.5 text-left text-xs transition-colors",
                  selectedSprintId === sprint.id
                    ? "bg-primary/10 text-primary border-l-2 border-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-muted/30"
                )}
              >
                <Zap className="size-3 shrink-0" />
                <span className="truncate flex-1">{sprint.name ?? sprint.title}</span>
                <Badge variant={sprintStatusVariant[sprint.status] ?? "outline"} size="sm">
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

/* ─── Sprint Detail (main area) ─── */
function SprintDetail({
  sprintId,
  isOwner,
  projectId,
}: {
  sprintId: string;
  isOwner: boolean;
  projectId: string;
}) {
  const { data: sprint, isLoading, error } = api.sprint.getById.useQuery({ id: sprintId });
  const utils = api.useUtils();

  const startMutation = api.sprint.start.useMutation({
    onSuccess: () => {
      toast.success("Sprint started");
      utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  const completeMutation = api.sprint.complete.useMutation({
    onSuccess: () => {
      toast.success("Sprint completed");
      utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  const cancelMutation = api.sprint.cancel.useMutation({
    onSuccess: () => {
      toast.success("Sprint cancelled");
      utils.sprint.getById.invalidate({ id: sprintId });
    },
    onError: (err) => toast.error(`Failed: ${err.message}`),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-6 animate-spin text-primary" />
      </div>
    );
  }

  if (error || !sprint) {
    return (
      <div className="border-2 border-destructive/40 bg-destructive/5 p-6 flex items-start gap-3">
        <AlertCircle className="size-5 text-destructive shrink-0 mt-0.5" />
        <div>
          <p className="font-mono text-sm font-bold text-destructive">SPRINT NOT FOUND</p>
          <p className="text-xs text-muted-foreground mt-1">{error?.message ?? "Sprint data unavailable."}</p>
        </div>
      </div>
    );
  }

  const s = sprint as any;
  const progress = s.completionPercentage ?? s.progress ?? 0;
  const features = (s.features as any[]) ?? [];
  const pbis = (s.productBacklogItems as any[]) ?? (s.pbis as any[]) ?? [];

  return (
    <div className="space-y-4">
      {/* Sprint header */}
      <div className="bg-card border-2 border-border p-4 space-y-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Zap className="size-5 text-primary" />
            <h3 className="text-lg font-bold text-foreground">{s.name ?? s.title}</h3>
            <Badge variant={sprintStatusVariant[s.status] ?? "outline"}>
              {s.status}
            </Badge>
          </div>

          {/* Sprint lifecycle buttons (owner only) */}
          {isOwner && (
            <div className="flex items-center gap-2">
              {s.status === "PLANNED" && (
                <Button
                  size="sm"
                  variant="neon"
                  onClick={() => startMutation.mutate({ id: sprintId })}
                  disabled={startMutation.isPending}
                  className="font-mono text-xs"
                >
                  {startMutation.isPending ? (
                    <Loader2 className="size-3 animate-spin mr-1" />
                  ) : (
                    <Play className="size-3 mr-1" />
                  )}
                  START
                </Button>
              )}
              {s.status === "ACTIVE" && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => completeMutation.mutate({ id: sprintId })}
                  disabled={completeMutation.isPending}
                  className="font-mono text-xs border-neon-green text-neon-green hover:bg-neon-green/10"
                >
                  {completeMutation.isPending ? (
                    <Loader2 className="size-3 animate-spin mr-1" />
                  ) : (
                    <CheckCircle2 className="size-3 mr-1" />
                  )}
                  COMPLETE
                </Button>
              )}
              {(s.status === "PLANNED" || s.status === "ACTIVE") && (
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => cancelMutation.mutate({ id: sprintId })}
                  disabled={cancelMutation.isPending}
                  className="font-mono text-xs border-destructive text-destructive hover:bg-destructive/10"
                >
                  {cancelMutation.isPending ? (
                    <Loader2 className="size-3 animate-spin mr-1" />
                  ) : (
                    <XCircle className="size-3 mr-1" />
                  )}
                  CANCEL
                </Button>
              )}
            </div>
          )}
        </div>

        {/* Sprint meta */}
        <div className="flex items-center gap-4 text-xs text-muted-foreground flex-wrap">
          {s.goal && (
            <span className="text-foreground italic">&quot;{s.goal}&quot;</span>
          )}
          {s.startDate && s.endDate && (
            <span className="flex items-center gap-1">
              <Calendar className="size-3" />
              {new Date(s.startDate).toLocaleDateString("en-US", { month: "short", day: "numeric" })}
              {" - "}
              {new Date(s.endDate).toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })}
            </span>
          )}
          {s.velocity != null && (
            <span className="font-mono">Velocity: {s.velocity}</span>
          )}
        </div>

        {/* Progress bar */}
        {progress > 0 && (
          <div className="space-y-1">
            <div className="flex justify-between text-[10px] text-muted-foreground">
              <span>Progress</span>
              <span>{progress}%</span>
            </div>
            <Progress value={progress} variant="neon" className="h-2" />
          </div>
        )}
      </div>

      {/* Features */}
      {features.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-xs font-mono uppercase tracking-widest text-muted-foreground px-1">
            FEATURES
          </h4>
          <div className="grid gap-2 sm:grid-cols-2">
            {features.map((feature: any) => (
              <div
                key={feature.id}
                className="bg-card border-2 border-border p-3 space-y-2"
              >
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-foreground truncate flex-1">
                    {feature.title ?? feature.name}
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
                  <p className="text-xs text-muted-foreground line-clamp-2">{feature.description}</p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* PBIs */}
      {pbis.length > 0 && (
        <div className="space-y-2">
          <h4 className="text-xs font-mono uppercase tracking-widest text-muted-foreground px-1">
            BACKLOG ITEMS
          </h4>
          <div className="space-y-1">
            {pbis.map((pbi: any) => (
              <div
                key={pbi.id}
                className="bg-card border-2 border-border hover:border-primary transition-colors p-3 flex items-center gap-3"
              >
                {/* Type badge */}
                {pbi.type && (
                  <Badge variant={pbiTypeVariant[pbi.type] ?? "secondary"} size="sm">
                    {pbi.type}
                  </Badge>
                )}

                {/* Title */}
                <span className="text-sm font-medium text-foreground truncate flex-1">
                  {pbi.title}
                </span>

                {/* Status */}
                {pbi.status && (
                  <Badge
                    variant={
                      pbi.status === "COMPLETED" || pbi.status === "DONE"
                        ? "neon-green"
                        : pbi.status === "IN_PROGRESS"
                          ? "neon"
                          : pbi.status === "BLOCKED"
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
                  <span className="text-xs font-mono text-muted-foreground">
                    {pbi.storyPoints} pts
                  </span>
                )}

                {/* Assignee */}
                {pbi.assignee && (
                  <Avatar className="size-5 shrink-0">
                    <AvatarImage src={pbi.assignee?.image} />
                    <AvatarFallback className="text-[8px]">
                      {pbi.assignee?.name?.charAt(0) ?? <User className="size-3" />}
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
        <div className="border-2 border-border bg-card p-8 text-center">
          <p className="text-sm text-muted-foreground font-mono">NO ITEMS IN THIS SPRINT</p>
          <p className="text-xs text-muted-foreground mt-1">
            Add features and backlog items to plan this sprint.
          </p>
        </div>
      )}
    </div>
  );
}
