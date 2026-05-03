"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
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
import { Avatar, AvatarFallback } from "~/components/ui/avatar";
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

export interface WorkItemDetailProps {
  open: boolean;
  onClose: () => void;
  level: WorkItemLevel;
  item: any;
  projectId: string;
  isOwner: boolean;
}

const LEVEL_LABELS: Record<WorkItemLevel, string> = {
  milestone: "Milestone",
  epic: "Epic",
  sprint: "Sprint",
  feature: "Feature",
  pbi: "PBI",
  task: "Task",
};

const LEVEL_ICONS: Record<WorkItemLevel, React.ElementType> = {
  milestone: Flag,
  epic: Target,
  sprint: Zap,
  feature: Box,
  pbi: Layers,
  task: CheckSquare,
};

const LEVEL_COLORS: Record<WorkItemLevel, string> = {
  milestone: "text-neon-pink",
  epic: "text-neon-purple",
  sprint: "text-neon-cyan",
  feature: "text-neon-green",
  pbi: "text-neon-yellow",
  task: "text-foreground",
};

function CommentTargetTypeFromLevel(level: WorkItemLevel): string {
  return level.toUpperCase() === "PBI" ? "PBI" : level.toUpperCase();
}

function statusBadgeVariant(
  status: string
): "secondary" | "neon" | "neon-green" | "destructive" {
  const s = status?.toUpperCase() ?? "";
  if (["PLANNED", "DRAFT", "NEW", "TODO"].includes(s)) return "secondary";
  if (["IN_PROGRESS", "ACTIVE", "READY"].includes(s)) return "neon";
  if (["COMPLETED", "DONE"].includes(s)) return "neon-green";
  if (["CANCELLED"].includes(s)) return "destructive";
  return "secondary";
}

function priorityBadgeVariant(
  priority: string
): "destructive" | "warning" | "secondary" | "outline" {
  const p = priority?.toUpperCase() ?? "";
  if (p === "CRITICAL") return "destructive";
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
}: {
  projectId: string;
  targetType: string;
  targetId: string;
}) {
  const [newComment, setNewComment] = useState("");
  const utils = api.useUtils();

  const { data: comments = [], isLoading } = api.comment.getByTarget.useQuery(
    { targetType: targetType as any, targetId },
    { enabled: !!targetId }
  );

  const addComment = api.comment.add.useMutation({
    onSuccess: () => {
      setNewComment("");
      void utils.comment.getByTarget.invalidate({ targetType: targetType as any, targetId });
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.trim()) return;

    addComment.mutate({
      projectId,
      targetType: targetType as any,
      targetId,
      content: newComment.trim(),
    });
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <MessageSquare className="h-4 w-4" />
        Comments ({comments.length})
      </div>

      {!isLoading && comments.length === 0 && (
        <p className="text-xs text-muted-foreground">No comments yet.</p>
      )}

      <div className="space-y-2 max-h-48 overflow-y-auto">
        {comments.map((comment: any) => {
          const authorName = comment.author?.name ?? comment.user?.name ?? "User";
          const authorImage = comment.author?.image ?? comment.user?.image;
          return (
            <div
              key={comment.id}
              className="flex gap-2 p-2 rounded bg-muted/30"
            >
              <Avatar className="h-6 w-6 shrink-0">
                {authorImage && <img src={authorImage} alt="" className="rounded-full" />}
                <AvatarFallback className="text-[10px]">
                  {authorName.charAt(0)}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-medium">{authorName}</span>
                  <span className="text-[10px] text-muted-foreground">
                    {new Date(comment.createdAt).toLocaleString()}
                  </span>
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {comment.content}
                </p>
              </div>
            </div>
          );
        })}
      </div>

      <form onSubmit={handleSubmit} className="flex gap-2">
        <input
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
          placeholder="Add a comment..."
          className="flex-1 rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
        />
        <Button type="submit" size="sm" disabled={!newComment.trim() || addComment.isPending}>
          <Send className="h-3.5 w-3.5" />
        </Button>
      </form>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Detail Modal
// ---------------------------------------------------------------------------

export default function WorkItemDetailModal({
  open,
  onClose,
  level,
  item,
  projectId,
  isOwner,
}: WorkItemDetailProps) {
  if (!item) return null;

  const Icon = LEVEL_ICONS[level];
  const colorClass = LEVEL_COLORS[level];
  const label = LEVEL_LABELS[level];

  const title = item.title ?? item.name ?? "Untitled";
  const description = item.description ?? item.goal ?? "";
  const status = item.status ?? "";
  const priority = item.priority ?? "";
  const assigneeId = item.assigneeId ?? item.assignedToId ?? null;
  const guildId = item.guildId ?? null;
  const equityBudget = item.equityBudget ?? item.equityReward ?? null;
  const storyPoints = item.storyPoints ?? null;
  const startDate = item.startDate ?? null;
  const endDate = item.endDate ?? item.targetDate ?? null;
  const dueDate = item.dueDate ?? null;

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <Icon className={cn("h-5 w-5", colorClass)} />
            <Badge variant="outline" size="sm">
              {label}
            </Badge>
            {status && (
              <Badge variant={statusBadgeVariant(status)} size="sm">
                {status.replace(/_/g, " ")}
              </Badge>
            )}
          </div>
          <DialogTitle className="text-lg mt-1">{title}</DialogTitle>
          {description && (
            <DialogDescription className="text-sm whitespace-pre-wrap">
              {description}
            </DialogDescription>
          )}
        </DialogHeader>

        {/* Metadata grid */}
        <div className="grid grid-cols-2 gap-3 py-3 border-t border-border">
          {priority && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
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
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Assignee
              </span>
              <div className="mt-0.5 flex items-center gap-1.5">
                <Avatar className="h-5 w-5">
                  <AvatarFallback className="text-[9px]">A</AvatarFallback>
                </Avatar>
                <span className="text-xs text-muted-foreground truncate">
                  {assigneeId.slice(0, 8)}...
                </span>
              </div>
            </div>
          )}

          {guildId && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
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
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Token Reward
              </span>
              <div className="mt-0.5 flex items-center gap-1">
                <Coins className="h-3.5 w-3.5 text-neon-green" />
                <span className="text-sm font-medium text-neon-green">
                  {Number(equityBudget).toLocaleString()}
                </span>
              </div>
            </div>
          )}

          {storyPoints !== null && storyPoints > 0 && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Story Points
              </span>
              <div className="mt-0.5 text-sm font-medium">{storyPoints} SP</div>
            </div>
          )}

          {(startDate || endDate || dueDate) && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Dates
              </span>
              <div className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />
                {startDate && (
                  <span>{new Date(startDate).toLocaleDateString()}</span>
                )}
                {startDate && endDate && <span>-</span>}
                {(endDate || dueDate) && (
                  <span>
                    {new Date(endDate ?? dueDate).toLocaleDateString()}
                  </span>
                )}
              </div>
            </div>
          )}

          {item.type && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Type
              </span>
              <div className="mt-0.5">
                <Badge variant="outline" size="sm">
                  {item.type}
                </Badge>
              </div>
            </div>
          )}

          {item.taskType && (
            <div>
              <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
                Task Type
              </span>
              <div className="mt-0.5">
                <Badge variant="outline" size="sm">
                  {item.taskType}
                </Badge>
              </div>
            </div>
          )}
        </div>

        {/* Acceptance Criteria */}
        {item.acceptanceCriteria && (
          <div className="py-3 border-t border-border">
            <span className="text-[10px] uppercase tracking-wider text-muted-foreground">
              Acceptance Criteria
            </span>
            <p className="text-xs text-muted-foreground mt-1 whitespace-pre-wrap">
              {item.acceptanceCriteria}
            </p>
          </div>
        )}

        {/* Comments */}
        <div className="pt-3 border-t border-border">
          <CommentSection
            projectId={projectId}
            targetType={CommentTargetTypeFromLevel(level)}
            targetId={item.id}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}
