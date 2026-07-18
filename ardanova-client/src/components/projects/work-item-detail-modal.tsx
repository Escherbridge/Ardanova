"use client";

import { useId, useState } from "react";
import Link from "next/link";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "~/components/ui/dialog";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import {
  Flag,
  Target,
  Zap,
  Box,
  Layers,
  CheckSquare,
  Send,
  MessageSquare,
  Clock,
  Coins,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export type WorkItemLevel =
  | "milestone"
  | "epic"
  | "sprint"
  | "feature"
  | "pbi"
  | "task";

type ProjectMilestone = RouterOutputs["project"]["getMilestones"][number];
type Epic = RouterOutputs["epic"]["getByMilestoneId"][number];
type Sprint = RouterOutputs["sprint"]["getByEpicId"][number];
type Feature = RouterOutputs["feature"]["getBySprintId"][number];
type Pbi = RouterOutputs["backlog"]["getPbisByFeatureId"][number];
type ProjectTask = RouterOutputs["task"]["getAll"]["items"][number];

export type WorkItemDetailSelection =
  | { level: "milestone"; item: ProjectMilestone }
  | { level: "epic"; item: Epic }
  | { level: "sprint"; item: Sprint }
  | { level: "feature"; item: Feature }
  | { level: "pbi"; item: Pbi }
  | { level: "task"; item: ProjectTask };

export interface WorkItemDetailProps {
  open: boolean;
  onClose: () => void;
  selection: WorkItemDetailSelection;
  projectId: string;
  canComment: boolean;
  signInHref: string;
}

type CommentTargetType = RouterInputs["comment"]["getByTarget"]["targetType"];

interface WorkItemMetadata {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  assigneeId: string | null;
  guildId: string | null;
  equityBudget: number | null;
  storyPoints: number | null;
  startDate: string | null;
  endDate: string | null;
  type: string | null;
  taskType: string | null;
  acceptanceCriteria: string;
}

function getWorkItemMetadata(
  selection: WorkItemDetailSelection,
): WorkItemMetadata {
  switch (selection.level) {
    case "milestone": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.title,
        description: item.description ?? "",
        status: item.status,
        priority: item.priority,
        assigneeId: null,
        guildId: null,
        equityBudget: null,
        storyPoints: null,
        startDate: null,
        endDate: item.targetDate,
        type: null,
        taskType: null,
        acceptanceCriteria: "",
      };
    }
    case "epic": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.title,
        description: item.description ?? "",
        status: item.status,
        priority: item.priority,
        assigneeId: item.assigneeId ?? null,
        guildId: null,
        equityBudget: item.equityBudget ?? null,
        storyPoints: null,
        startDate: item.startDate ?? null,
        endDate: item.targetDate ?? null,
        type: null,
        taskType: null,
        acceptanceCriteria: "",
      };
    }
    case "sprint": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.name,
        description: item.goal ?? "",
        status: item.status,
        priority: "",
        assigneeId: item.assigneeId ?? null,
        guildId: null,
        equityBudget: item.equityBudget ?? null,
        storyPoints: null,
        startDate: item.startDate ?? null,
        endDate: item.endDate ?? null,
        type: null,
        taskType: null,
        acceptanceCriteria: "",
      };
    }
    case "feature": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.title,
        description: item.description ?? "",
        status: item.status,
        priority: item.priority,
        assigneeId: item.assigneeId ?? null,
        guildId: null,
        equityBudget: null,
        storyPoints: null,
        startDate: null,
        endDate: null,
        type: null,
        taskType: null,
        acceptanceCriteria: "",
      };
    }
    case "pbi": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.title,
        description: item.description ?? "",
        status: item.status,
        priority: item.priority,
        assigneeId: item.assigneeId ?? null,
        guildId: item.guildId ?? null,
        equityBudget: null,
        storyPoints: item.storyPoints ?? null,
        startDate: null,
        endDate: null,
        type: item.type,
        taskType: null,
        acceptanceCriteria: item.acceptanceCriteria ?? "",
      };
    }
    case "task": {
      const { item } = selection;
      return {
        id: item.id,
        title: item.title,
        description: item.description ?? "",
        status: item.status,
        priority: item.priority,
        assigneeId: item.assignedToId ?? null,
        guildId: item.guildId ?? null,
        equityBudget: item.equityReward ?? null,
        storyPoints: null,
        startDate: null,
        endDate: item.dueDate ?? null,
        type: null,
        taskType: item.taskType,
        acceptanceCriteria: "",
      };
    }
  }
}

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleDateString();
}

const LEVEL_LABELS: Record<WorkItemLevel, string> = {
  milestone: "Milestone",
  epic: "Epic",
  sprint: "Sprint",
  feature: "Feature",
  pbi: "PBI",
  task: "Task",
};

function WorkItemLevelIcon({
  level,
  className,
}: {
  level: WorkItemLevel;
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

const LEVEL_COLORS: Record<WorkItemLevel, string> = {
  milestone: "text-neon-pink",
  epic: "text-neon-purple",
  sprint: "text-neon-cyan",
  feature: "text-neon-green",
  pbi: "text-neon-yellow",
  task: "text-foreground",
};

const COMMENT_TARGET_BY_LEVEL: Record<WorkItemLevel, CommentTargetType> = {
  milestone: "MILESTONE",
  epic: "EPIC",
  sprint: "SPRINT",
  feature: "FEATURE",
  pbi: "PBI",
  task: "TASK",
};

function statusBadgeVariant(
  status: string,
): "secondary" | "neon" | "neon-green" | "destructive" {
  const s = status?.toUpperCase() ?? "";
  if (["PLANNED", "DRAFT", "NEW", "TODO"].includes(s)) return "secondary";
  if (["IN_PROGRESS", "ACTIVE", "READY"].includes(s)) return "neon";
  if (["COMPLETED", "DONE"].includes(s)) return "neon-green";
  if (["CANCELLED", "BLOCKED"].includes(s)) return "destructive";
  return "secondary";
}

function priorityBadgeVariant(
  priority: string,
): "destructive" | "warning" | "secondary" | "outline" {
  const p = priority?.toUpperCase() ?? "";
  if (p === "CRITICAL" || p === "URGENT") return "destructive";
  if (p === "HIGH") return "warning";
  if (p === "MEDIUM") return "secondary";
  return "outline";
}

// ---------------------------------------------------------------------------
// Comment Section
// ---------------------------------------------------------------------------

function CommentSection({
  projectId,
  targetType,
  targetId,
  canComment,
  signInHref,
}: {
  projectId: string;
  targetType: CommentTargetType;
  targetId: string;
  canComment: boolean;
  signInHref: string;
}) {
  const [newComment, setNewComment] = useState("");
  const commentInputId = useId();
  const utils = api.useUtils();

  const {
    data: comments = [],
    isLoading,
    isFetching,
    error,
    refetch,
  } = api.comment.getByTarget.useQuery(
    { targetType, targetId },
    { enabled: !!targetId },
  );

  const addComment = api.comment.add.useMutation({
    onSuccess: () => {
      setNewComment("");
      void utils.comment.getByTarget.invalidate({
        targetType,
        targetId,
      });
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    addComment.mutate({
      projectId,
      targetType,
      targetId,
      content: newComment.trim(),
    });
  };

  return (
    <div className="space-y-3">
      <h3 className="text-muted-foreground flex items-center gap-2 text-sm font-medium">
        <MessageSquare className="h-4 w-4" />
        Comments ({comments.length})
      </h3>

      {isLoading && (
        <p className="text-muted-foreground text-xs" role="status">
          Loading comments...
        </p>
      )}

      {error && (
        <div
          className="border-destructive/40 bg-destructive/5 space-y-2 border p-3"
          role="alert"
        >
          <p className="text-destructive text-xs">
            Comments could not be loaded. The work item is still available.
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            className="min-h-11"
            disabled={isFetching}
            onClick={() => void refetch()}
          >
            {isFetching ? "Trying again..." : "Try comments again"}
          </Button>
        </div>
      )}

      {!isLoading && !error && comments.length === 0 && (
        <p className="text-muted-foreground text-xs">No comments yet.</p>
      )}

      <div className="max-h-48 space-y-2 overflow-y-auto">
        {comments.map((comment) => {
          const authorName = comment.author?.name ?? "User";
          const authorImage = comment.author?.image;
          return (
            <div
              key={comment.id}
              className="bg-muted/30 flex gap-2 rounded p-2"
            >
              <Avatar className="h-6 w-6 shrink-0">
                {authorImage && <AvatarImage src={authorImage} alt="" />}
                <AvatarFallback className="text-[10px]">
                  {authorName.charAt(0)}
                </AvatarFallback>
              </Avatar>
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
                  <span className="min-w-0 text-xs font-medium [overflow-wrap:anywhere] break-words">
                    {authorName}
                  </span>
                  <span className="text-muted-foreground text-[10px]">
                    {new Date(comment.createdAt).toLocaleString()}
                  </span>
                </div>
                <p className="text-muted-foreground mt-0.5 text-xs [overflow-wrap:anywhere] break-words">
                  {comment.content}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      {canComment ? (
        <form onSubmit={handleSubmit} className="flex items-end gap-2">
          <label htmlFor={commentInputId} className="sr-only">
            Add a comment
          </label>
          <input
            id={commentInputId}
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Add a comment..."
            className="border-input bg-background focus:ring-primary min-h-11 min-w-0 flex-1 rounded-md border px-3 py-2 text-sm focus:ring-2 focus:outline-none"
          />
          <Button
            type="submit"
            size="sm"
            className="size-11 shrink-0 p-0"
            aria-label="Post comment"
            disabled={!newComment.trim() || addComment.isPending}
          >
            <Send className="h-3.5 w-3.5" />
          </Button>
        </form>
      ) : (
        <div className="border-border flex flex-wrap items-center justify-between gap-3 border p-3">
          <p className="text-muted-foreground text-xs">
            Sign in to contribute to this discussion.
          </p>
          <Button asChild size="sm" variant="outline" className="min-h-11">
            <Link href={signInHref}>Sign in to comment</Link>
          </Button>
        </div>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Detail Modal
// ---------------------------------------------------------------------------

export default function WorkItemDetailModal({
  open,
  onClose,
  selection,
  projectId,
  canComment,
  signInHref,
}: WorkItemDetailProps) {
  const { level } = selection;
  const metadata = getWorkItemMetadata(selection);
  const {
    id: itemId,
    title,
    description,
    status,
    priority,
    assigneeId,
    guildId,
    equityBudget,
    storyPoints,
    startDate,
    endDate: finalDate,
    type,
    taskType,
    acceptanceCriteria,
  } = metadata;

  const colorClass = LEVEL_COLORS[level];
  const label = LEVEL_LABELS[level];

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-h-[calc(100dvh-2rem)] max-w-lg overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <WorkItemLevelIcon
              level={level}
              className={cn("h-5 w-5", colorClass)}
            />
            <Badge variant="outline" size="sm">
              {label}
            </Badge>
            {status && (
              <Badge variant={statusBadgeVariant(status)} size="sm">
                {status.replace(/_/g, " ")}
              </Badge>
            )}
          </div>
          <DialogTitle className="mt-1 text-lg">{title}</DialogTitle>
          <DialogDescription
            className={cn(
              "text-sm whitespace-pre-wrap",
              !description && "sr-only",
            )}
          >
            {description || `${label} details and discussion.`}
          </DialogDescription>
        </DialogHeader>

        {/* Metadata grid */}
        <div className="border-border grid grid-cols-1 gap-3 border-t py-3 sm:grid-cols-2">
          {priority && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Priority
              </span>
              <div className="mt-0.5">
                <Badge variant={priorityBadgeVariant(priority)} size="sm">
                  {priority}
                </Badge>
              </div>
            </div>
          )}

          {assigneeId && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Assignee
              </span>
              <div className="mt-0.5 flex items-center gap-1.5">
                <Avatar className="h-5 w-5">
                  <AvatarFallback className="text-[9px]">A</AvatarFallback>
                </Avatar>
                <span className="text-muted-foreground truncate text-xs">
                  {assigneeId.slice(0, 8)}...
                </span>
              </div>
            </div>
          )}

          {guildId && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Guild
              </span>
              <div className="mt-0.5">
                <Badge variant="neon-purple" size="sm">
                  Guild assigned
                </Badge>
              </div>
            </div>
          )}

          {equityBudget !== null && equityBudget > 0 && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Project-token allocation
              </span>
              <div className="mt-0.5 flex items-center gap-1">
                <Coins className="text-neon-green h-3.5 w-3.5" />
                <span className="text-neon-green text-sm font-medium">
                  {Number(equityBudget).toLocaleString()}
                </span>
              </div>
            </div>
          )}

          {storyPoints !== null && storyPoints > 0 && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Story Points
              </span>
              <div className="mt-0.5 text-sm font-medium">{storyPoints} SP</div>
            </div>
          )}

          {(startDate || finalDate) && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Dates
              </span>
              <div className="text-muted-foreground mt-0.5 flex items-center gap-1 text-xs">
                <Clock className="h-3 w-3" />
                {startDate && <span>{formatDate(startDate)}</span>}
                {startDate && finalDate && <span>-</span>}
                {finalDate && <span>{formatDate(finalDate)}</span>}
              </div>
            </div>
          )}

          {type && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Type
              </span>
              <div className="mt-0.5">
                <Badge variant="outline" size="sm">
                  {type}
                </Badge>
              </div>
            </div>
          )}

          {taskType && (
            <div>
              <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
                Task Type
              </span>
              <div className="mt-0.5">
                <Badge variant="outline" size="sm">
                  {taskType}
                </Badge>
              </div>
            </div>
          )}
        </div>

        {/* Acceptance Criteria */}
        {acceptanceCriteria && (
          <div className="border-border border-t py-3">
            <span className="text-muted-foreground text-[10px] tracking-wider uppercase">
              Acceptance Criteria
            </span>
            <p className="text-muted-foreground mt-1 text-xs whitespace-pre-wrap">
              {acceptanceCriteria}
            </p>
          </div>
        )}

        {/* Comments */}
        <div className="border-border border-t pt-3">
          <CommentSection
            projectId={projectId}
            targetType={COMMENT_TARGET_BY_LEVEL[level]}
            targetId={itemId}
            canComment={canComment}
            signInHref={signInHref}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}
