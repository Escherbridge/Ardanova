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
  Coins,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Progress } from "~/components/ui/progress";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { toast } from "sonner";

interface RoadmapTabProps {
  projectId: string;
  isOwner: boolean;
}

const milestoneStatusVariant: Record<string, "outline" | "neon" | "neon-green" | "destructive"> = {
  PLANNED: "outline",
  IN_PROGRESS: "neon",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const epicStatusVariant: Record<string, "outline" | "neon" | "neon-green" | "neon-purple" | "destructive"> = {
  PLANNED: "outline",
  IN_PROGRESS: "neon",
  COMPLETED: "neon-green",
  REVIEW: "neon-purple",
  CANCELLED: "destructive",
};

export default function RoadmapTab({ projectId, isOwner }: RoadmapTabProps) {
  const { data: milestones, isLoading, error } = api.project.getMilestones.useQuery({ projectId });

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
      utils.project.getMilestones.invalidate({ projectId });
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
      targetDate: newMilestoneDate || new Date().toISOString(),
    });
  };

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
          <p className="font-mono text-sm font-bold text-destructive">FAILED TO LOAD ROADMAP</p>
          <p className="text-xs text-muted-foreground mt-1">{error.message}</p>
        </div>
      </div>
    );
  }

  const milestoneList = (milestones as any[]) ?? [];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-mono uppercase tracking-widest text-muted-foreground">
          PROJECT ROADMAP
        </h3>
        {isOwner && (
          <Button
            variant="neon"
            size="sm"
            onClick={() => setShowAddMilestone(!showAddMilestone)}
            className="font-mono text-xs"
          >
            <Plus className="size-4 mr-1" />
            ADD MILESTONE
          </Button>
        )}
      </div>

      {/* Add milestone form */}
      {showAddMilestone && (
        <div className="bg-card border-2 border-border p-4 space-y-3">
          <input
            type="text"
            placeholder="Milestone title..."
            value={newMilestoneTitle}
            onChange={(e) => setNewMilestoneTitle(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleAddMilestone();
              if (e.key === "Escape") setShowAddMilestone(false);
            }}
            className="w-full bg-transparent border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
            autoFocus
          />
          <div className="flex items-center gap-3">
            <input
              type="date"
              value={newMilestoneDate}
              onChange={(e) => setNewMilestoneDate(e.target.value)}
              className="bg-card border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
            />
            <Button
              size="sm"
              variant="neon"
              onClick={handleAddMilestone}
              disabled={addMilestoneMutation.isPending || !newMilestoneTitle.trim()}
              className="text-xs font-mono"
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
              className="text-xs"
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      {/* Timeline */}
      {milestoneList.length === 0 ? (
        <div className="border-2 border-border bg-card p-8 text-center">
          <Target className="size-8 text-muted-foreground mx-auto mb-2" />
          <p className="text-sm text-muted-foreground font-mono">NO MILESTONES YET</p>
          <p className="text-xs text-muted-foreground mt-1">
            Add milestones to track your project roadmap.
          </p>
        </div>
      ) : (
        <div className="relative">
          {/* Vertical timeline line */}
          <div className="absolute left-[7px] top-0 bottom-0 border-l-2 border-border" />

          <div className="space-y-0">
            {milestoneList.map((milestone: any, index: number) => (
              <MilestoneNode
                key={milestone.id}
                milestone={milestone}
                isOwner={isOwner}
                isLast={index === milestoneList.length - 1}
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
  isLast,
  projectId,
}: {
  milestone: any;
  isOwner: boolean;
  isLast: boolean;
  projectId: string;
}) {
  const [isExpanded, setIsExpanded] = useState(milestone.status === "IN_PROGRESS");
  const [showAddEpic, setShowAddEpic] = useState(false);
  const [newEpicTitle, setNewEpicTitle] = useState("");

  const { data: epics, isLoading: epicsLoading } = api.epic.getByMilestoneId.useQuery(
    { milestoneId: milestone.id },
    { enabled: isExpanded }
  );

  const utils = api.useUtils();
  const createEpicMutation = api.epic.create.useMutation({
    onSuccess: () => {
      toast.success("Epic added");
      setNewEpicTitle("");
      setShowAddEpic(false);
      utils.epic.getByMilestoneId.invalidate({ milestoneId: milestone.id });
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

  const statusColor = milestone.status === "COMPLETED"
    ? "bg-neon-green border-neon-green"
    : milestone.status === "IN_PROGRESS"
      ? "bg-primary border-primary"
      : milestone.status === "CANCELLED"
        ? "bg-destructive border-destructive"
        : "bg-muted border-border";

  const isActive = milestone.status === "IN_PROGRESS";

  return (
    <div className="relative pl-8 pb-6">
      {/* Timeline node */}
      <div
        className={cn(
          "absolute left-0 top-1 w-4 h-4 border-2 z-10",
          statusColor,
          isActive && "animate-pulse"
        )}
      />

      {/* Milestone card */}
      <div className="bg-card border-2 border-border hover:border-primary transition-colors">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="w-full flex items-center gap-3 p-4 text-left"
        >
          {isExpanded ? (
            <ChevronDown className="size-4 text-muted-foreground shrink-0" />
          ) : (
            <ChevronRight className="size-4 text-muted-foreground shrink-0" />
          )}

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h4 className="text-sm font-bold text-foreground truncate">{milestone.title}</h4>
              <Badge variant={milestoneStatusVariant[milestone.status] ?? "outline"} size="sm">
                {milestone.status?.replace("_", " ")}
              </Badge>
            </div>
            <div className="flex items-center gap-3 text-xs text-muted-foreground">
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
              {milestone.equityBudget != null && Number(milestone.equityBudget) > 0 && (
                <span className="flex items-center gap-1 text-neon-green">
                  <Coins className="size-3" />
                  {milestone.equityBudget} equity
                </span>
              )}
              {milestone.completionPercentage != null && (
                <span>{milestone.completionPercentage}% complete</span>
              )}
            </div>
          </div>

          {/* Completion indicator */}
          {milestone.status === "COMPLETED" && (
            <CheckCircle2 className="size-5 text-neon-green shrink-0" />
          )}
        </button>

        {/* Expanded content: Epics */}
        {isExpanded && (
          <div className="border-t-2 border-border p-4 space-y-3">
            {epicsLoading ? (
              <div className="flex items-center gap-2 py-4 justify-center">
                <Loader2 className="size-4 animate-spin text-muted-foreground" />
                <span className="text-xs text-muted-foreground">Loading epics...</span>
              </div>
            ) : (epics as any[])?.length === 0 ? (
              <p className="text-xs text-muted-foreground font-mono text-center py-4">
                NO EPICS IN THIS MILESTONE
              </p>
            ) : (
              (epics as any[])?.map((epic: any) => (
                <EpicCard key={epic.id} epic={epic} />
              ))
            )}

            {/* Add epic */}
            {isOwner && (
              <div className="mt-2">
                {showAddEpic ? (
                  <div className="border-2 border-dashed border-border p-3 space-y-2">
                    <input
                      type="text"
                      placeholder="Epic title..."
                      value={newEpicTitle}
                      onChange={(e) => setNewEpicTitle(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") handleAddEpic();
                        if (e.key === "Escape") setShowAddEpic(false);
                      }}
                      className="w-full bg-transparent border-2 border-border focus:border-primary focus:outline-none px-3 py-2 text-sm"
                      autoFocus
                    />
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        variant="neon"
                        onClick={handleAddEpic}
                        disabled={createEpicMutation.isPending || !newEpicTitle.trim()}
                        className="text-xs font-mono"
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
                        className="text-xs"
                      >
                        Cancel
                      </Button>
                    </div>
                  </div>
                ) : (
                  <button
                    onClick={() => setShowAddEpic(true)}
                    className="w-full flex items-center gap-1 px-3 py-2 text-xs text-muted-foreground hover:text-foreground border-2 border-dashed border-border hover:border-primary transition-colors"
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
function EpicCard({ epic }: { epic: any }) {
  const [isExpanded, setIsExpanded] = useState(false);

  const { data: sprints, isLoading: sprintsLoading } = api.sprint.getByEpicId.useQuery(
    { epicId: epic.id },
    { enabled: isExpanded }
  );

  const progress = epic.completionPercentage ?? epic.progress ?? 0;

  return (
    <div className="bg-muted/30 border-2 border-border">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center gap-3 p-3 text-left"
      >
        {isExpanded ? (
          <ChevronDown className="size-3 text-muted-foreground shrink-0" />
        ) : (
          <ChevronRight className="size-3 text-muted-foreground shrink-0" />
        )}

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-sm font-medium text-foreground truncate">{epic.title}</span>
            {epic.status && (
              <Badge variant={epicStatusVariant[epic.status] ?? "outline"} size="sm">
                {epic.status?.replace("_", " ")}
              </Badge>
            )}
            {epic.priority && (
              <Badge
                variant={
                  epic.priority === "URGENT"
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

        {epic.assignee && (
          <Avatar className="size-5 shrink-0">
            <AvatarImage src={epic.assignee?.image} />
            <AvatarFallback className="text-[8px]">
              {epic.assignee?.name?.charAt(0) ?? "?"}
            </AvatarFallback>
          </Avatar>
        )}
      </button>

      {/* Sprints under epic */}
      {isExpanded && (
        <div className="border-t-2 border-border p-3 space-y-2">
          {sprintsLoading ? (
            <div className="flex items-center gap-2 py-2 justify-center">
              <Loader2 className="size-3 animate-spin text-muted-foreground" />
              <span className="text-[10px] text-muted-foreground">Loading sprints...</span>
            </div>
          ) : (sprints as any[])?.length === 0 ? (
            <p className="text-[10px] text-muted-foreground font-mono text-center py-2">
              NO SPRINTS
            </p>
          ) : (
            (sprints as any[])?.map((sprint: any) => (
              <div
                key={sprint.id}
                className="flex items-center gap-2 px-3 py-2 border-2 border-border bg-card text-xs"
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
                <span className="font-medium text-foreground truncate flex-1">{sprint.name ?? sprint.title}</span>
                {sprint.startDate && sprint.endDate && (
                  <span className="text-muted-foreground whitespace-nowrap">
                    {new Date(sprint.startDate).toLocaleDateString("en-US", { month: "short", day: "numeric" })}
                    {" - "}
                    {new Date(sprint.endDate).toLocaleDateString("en-US", { month: "short", day: "numeric" })}
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
