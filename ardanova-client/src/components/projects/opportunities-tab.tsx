"use client";

import { cloneElement, useEffect, useId, useRef, useState } from "react";
import Link from "next/link";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { toast } from "sonner";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";
import {
  ChevronRight,
  ChevronDown,
  Plus,
  Trash2,
  Loader2,
  Flag,
  Target,
  Zap,
  Box,
  Layers,
  ListTodo,
  CheckSquare,
  Briefcase,
  Users,
  Eye,
} from "lucide-react";
import WorkItemDetailModal, {
  type WorkItemDetailSelection,
} from "./work-item-detail-modal";
import CreateWorkItemModal from "./create-work-item-modal";
import type { CreateLevel } from "./create-work-item-modal";
import type { ProjectRole } from "~/lib/contracts/project-contract";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface OpportunitiesTabProps {
  projectId: string;
  projectSlug: string;
  isOwner: boolean;
  canComment: boolean;
  userRole?: ProjectRole;
}

type HierarchyLevel =
  | "milestone"
  | "epic"
  | "sprint"
  | "feature"
  | "pbi"
  | "task";

interface AddFormState {
  level: HierarchyLevel;
  parentId: string;
  returnFocusId: string;
}

type ViewMode = "hierarchy" | "flat-tasks" | "team-positions";
type ProjectMilestone = RouterOutputs["project"]["getMilestones"][number];
type Epic = RouterOutputs["epic"]["getByMilestoneId"][number];
type Sprint = RouterOutputs["sprint"]["getByEpicId"][number];
type Feature = RouterOutputs["feature"]["getBySprintId"][number];
type Pbi = RouterOutputs["backlog"]["getPbisByFeatureId"][number];
type ProjectTask = RouterOutputs["task"]["getAll"]["items"][number];
type ProjectOpportunity =
  RouterOutputs["opportunity"]["getByProjectId"][number];
type OpenWorkItemDetail = (selection: WorkItemDetailSelection) => void;

type EpicPriority = NonNullable<RouterInputs["epic"]["create"]["priority"]>;
type FeaturePriority = NonNullable<
  RouterInputs["feature"]["create"]["priority"]
>;
type PbiType = NonNullable<RouterInputs["backlog"]["createPbi"]["type"]>;
type PbiPriority = NonNullable<
  RouterInputs["backlog"]["createPbi"]["priority"]
>;
type TaskType = RouterInputs["task"]["create"]["type"];
type TaskPriority = NonNullable<RouterInputs["task"]["create"]["priority"]>;
type TaskEstimatedHours = NonNullable<
  RouterInputs["task"]["create"]["estimatedHours"]
>;

const HIERARCHY_LEVELS = [
  "milestone",
  "epic",
  "sprint",
  "feature",
  "pbi",
  "task",
] as const satisfies readonly HierarchyLevel[];
const HIERARCHY_PRIORITIES = [
  "LOW",
  "MEDIUM",
  "HIGH",
  "CRITICAL",
] as const satisfies readonly EpicPriority[];
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
const TASK_PRIORITIES = [
  "LOW",
  "MEDIUM",
  "HIGH",
  "URGENT",
] as const satisfies readonly TaskPriority[];
const TASK_HOUR_OPTIONS = [
  { value: 1, label: "1h estimate" },
  { value: 3, label: "3h estimate" },
  { value: 8, label: "8h estimate" },
  { value: 20, label: "20h estimate" },
  { value: 40, label: "40h estimate" },
] as const satisfies readonly {
  value: TaskEstimatedHours;
  label: string;
}[];

function isOption<const T extends readonly string[]>(
  options: T,
  value: string,
): value is T[number] {
  return options.some((option) => option === value);
}

function parseTaskEstimatedHours(value: string): TaskEstimatedHours | null {
  const hours = Number(value);
  return TASK_HOUR_OPTIONS.some((option) => option.value === hours)
    ? hours
    : null;
}

// ---------------------------------------------------------------------------
// Badge helpers
// ---------------------------------------------------------------------------

function statusBadgeVariant(
  status: string | null | undefined,
): "secondary" | "neon" | "neon-green" | "destructive" | "info" {
  const s = status?.toUpperCase() ?? "";
  if (["PLANNED", "DRAFT", "NEW", "BACKLOG", "PLANNING", "TODO"].includes(s))
    return "secondary";
  if (["IN_PROGRESS", "ACTIVE", "READY", "IN_REVIEW"].includes(s))
    return "neon";
  if (["COMPLETED", "DONE"].includes(s)) return "neon-green";
  if (["CANCELLED", "REMOVED", "BLOCKED"].includes(s)) return "destructive";
  return "secondary";
}

function priorityBadgeVariant(
  priority: string | null | undefined,
): "destructive" | "warning" | "secondary" | "outline" {
  const p = priority?.toUpperCase() ?? "";
  if (p === "CRITICAL" || p === "URGENT") return "destructive";
  if (p === "HIGH") return "warning";
  if (p === "MEDIUM") return "secondary";
  return "outline";
}

function formatStatus(status: string | null | undefined) {
  return (status ?? "UNKNOWN").replace(/_/g, " ");
}

function getRoleBadgeVariant(role: string) {
  switch (role) {
    case "FOUNDER":
      return "neon-pink-solid" as const;
    case "LEADER":
      return "neon-purple" as const;
    case "CORE_CONTRIBUTOR":
      return "neon-green" as const;
    case "CONTRIBUTOR":
      return "info" as const;
    case "OBSERVER":
      return "outline" as const;
    default:
      return "secondary" as const;
  }
}

function formatProjectRoleName(role: string) {
  return role
    .split("_")
    .map((w) => w.charAt(0) + w.slice(1).toLowerCase())
    .join(" ");
}

// ---------------------------------------------------------------------------
// Level metadata
// ---------------------------------------------------------------------------

function HierarchyIcon({
  level,
  className,
}: {
  level: HierarchyLevel;
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

const LEVEL_COLORS: Record<HierarchyLevel, string> = {
  milestone: "text-neon-pink",
  epic: "text-neon-purple",
  sprint: "text-neon-cyan",
  feature: "text-neon-green",
  pbi: "text-neon-yellow",
  task: "text-foreground",
};

// ---------------------------------------------------------------------------
// Inline form field component
// ---------------------------------------------------------------------------

function FormField({
  label,
  children,
}: {
  label: string;
  children: React.ReactElement<{ id?: string }>;
}) {
  const generatedId = useId();
  const controlId = children.props.id ?? generatedId;

  return (
    <div>
      <label
        htmlFor={controlId}
        className="text-muted-foreground mb-1 block text-xs font-medium"
      >
        {label}
      </label>
      {cloneElement(children, { id: controlId })}
    </div>
  );
}

function QueryError({
  label,
  message,
  onRetry,
}: {
  label: string;
  message: string;
  onRetry: () => void;
}) {
  return (
    <div
      role="alert"
      className="border-destructive/40 bg-destructive/5 my-2 flex min-w-0 flex-wrap items-center gap-3 rounded-md border p-3"
    >
      <div className="min-w-0 flex-1">
        <p className="text-destructive text-sm font-medium">
          Couldn&apos;t load {label}
        </p>
        <p className="text-muted-foreground truncate text-xs" title={message}>
          {message}
        </p>
      </div>
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="min-h-11 shrink-0"
        onClick={onRetry}
        aria-label={`Retry loading ${label}`}
      >
        Retry
      </Button>
    </div>
  );
}

function LoadingState({
  label,
  className,
}: {
  label: string;
  className?: string;
}) {
  return (
    <div
      className={cn("flex items-center justify-center", className)}
      role="status"
    >
      <Loader2
        className="text-muted-foreground h-4 w-4 animate-spin"
        aria-hidden="true"
      />
      <span className="sr-only">Loading {label}...</span>
    </div>
  );
}

const inputCn =
  "min-h-11 w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
const selectCn =
  "min-h-11 w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary";

// ---------------------------------------------------------------------------
// Sub-components for each hierarchy level
// ---------------------------------------------------------------------------

function TaskRow({
  task,
  isOwner,
  opportunities,
  onOpenDetail,
}: {
  task: ProjectTask;
  isOwner: boolean;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const utils = api.useUtils();

  const deleteMutation = api.task.delete.useMutation({
    onSuccess: () => {
      void utils.task.getByPbiId.invalidate();
      toast.success("Task deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  // Match an opportunity explicitly linked to this task.
  const linkedOpp = opportunities.find(
    (opportunity) => opportunity.taskId === task.id,
  );

  return (
    <div className="hover:bg-muted/30 group flex min-h-11 min-w-0 flex-wrap items-center gap-2 rounded px-2 py-1">
      <CheckSquare className="text-muted-foreground h-3.5 w-3.5 shrink-0" />
      <button
        type="button"
        onClick={() => onOpenDetail({ level: "task", item: task })}
        aria-label={`View task details: ${task.title}`}
        className="focus-visible:ring-primary flex min-h-11 min-w-0 flex-1 basis-40 items-center gap-1 rounded py-2 text-left text-sm focus-visible:ring-2 focus-visible:outline-none"
      >
        <span className="truncate">{task.title}</span>
        <Eye className="h-3.5 w-3.5 shrink-0 opacity-60" aria-hidden="true" />
      </button>
      {task.priority && (
        <Badge variant={priorityBadgeVariant(task.priority)} size="sm">
          {task.priority}
        </Badge>
      )}
      {task.status && (
        <Badge variant={statusBadgeVariant(task.status)} size="sm">
          {formatStatus(task.status)}
        </Badge>
      )}
      {task.estimatedHours != null && (
        <Badge variant="outline" size="sm">
          {task.estimatedHours}h estimate
        </Badge>
      )}
      {linkedOpp && (
        <Badge
          variant={statusBadgeVariant(linkedOpp.status)}
          size="sm"
          className="text-[9px]"
        >
          Opp: {formatStatus(linkedOpp.status)}
        </Badge>
      )}
      {isOwner && (
        <Button
          size="sm"
          variant="ghost"
          className="text-destructive hover:text-destructive h-11 w-11 shrink-0 p-0"
          onClick={(event) => {
            event.stopPropagation();
            if (
              window.confirm(
                `Delete task "${task.title}"? This action cannot be undone.`,
              )
            ) {
              deleteMutation.mutate({ id: task.id });
            }
          }}
          aria-label={`Delete task: ${task.title}`}
          disabled={deleteMutation.isPending}
        >
          {deleteMutation.isPending ? (
            <Loader2 className="h-3 w-3 animate-spin" />
          ) : (
            <Trash2 className="h-3 w-3" />
          )}
        </Button>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// PBI level
// ---------------------------------------------------------------------------

function PbiSection({
  featureId,
  isOwner,
  isExpanded,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  featureId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const {
    data: pbis,
    isLoading,
    error,
    refetch,
  } = api.backlog.getPbisByFeatureId.useQuery(
    { featureId },
    { enabled: isExpanded },
  );

  if (!isExpanded) return null;
  if (isLoading) {
    return <LoadingState label="backlog items" className="my-2 ml-8 w-4" />;
  }
  if (error) {
    return (
      <QueryError
        label="backlog items"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  return (
    <div className="border-border ml-2 min-w-0 space-y-1 border-l pl-2 sm:ml-6 sm:pl-3">
      {(pbis ?? []).map((pbi) => (
        <PbiNode
          key={pbi.id}
          pbi={pbi}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          onOpenDetail={onOpenDetail}
        />
      ))}
      {isOwner && (
        <AddFormButton
          level="pbi"
          parentId={featureId}
          addForm={addForm}
          setAddForm={setAddForm}
        />
      )}
      {addForm?.level === "pbi" && addForm.parentId === featureId && (
        <PbiAddForm
          projectId={projectId}
          featureId={featureId}
          onClose={() => setAddForm(null)}
        />
      )}
    </div>
  );
}

function PbiNode({
  pbi,
  isOwner,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  pbi: Pbi;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [expanded, setExpanded] = useState(false);
  const utils = api.useUtils();

  const deleteMutation = api.backlog.deletePbi.useMutation({
    onSuccess: () => {
      void utils.backlog.getPbisByFeatureId.invalidate();
      toast.success("PBI deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  // Fetch tasks for this PBI when expanded
  const {
    data: linkedTasks = [],
    isLoading: tasksLoading,
    error: tasksError,
    refetch: refetchTasks,
  } = api.task.getByPbiId.useQuery({ pbiId: pbi.id }, { enabled: expanded });

  return (
    <div>
      <TreeRow
        level="pbi"
        title={pbi.title}
        status={pbi.status}
        priority={pbi.priority}
        childCount={linkedTasks.length}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() => deleteMutation.mutate({ id: pbi.id })}
        isDeleting={deleteMutation.isPending}
        onDetail={() => onOpenDetail({ level: "pbi", item: pbi })}
        extra={
          pbi.type ? (
            <Badge variant="outline" size="sm">
              {pbi.type}
            </Badge>
          ) : null
        }
      />
      {expanded && (
        <div className="border-border ml-2 min-w-0 space-y-0.5 border-l pl-2 sm:ml-6 sm:pl-3">
          {tasksLoading && <LoadingState label="tasks" className="my-3 w-4" />}
          {tasksError && (
            <QueryError
              label="tasks"
              message={tasksError.message}
              onRetry={() => void refetchTasks()}
            />
          )}
          {!tasksLoading &&
            !tasksError &&
            linkedTasks.map((task) => (
              <TaskRow
                key={task.id}
                task={task}
                isOwner={isOwner}
                opportunities={opportunities}
                onOpenDetail={onOpenDetail}
              />
            ))}
          {isOwner && (
            <AddFormButton
              level="task"
              parentId={pbi.id}
              addForm={addForm}
              setAddForm={setAddForm}
            />
          )}
          {addForm?.level === "task" && addForm.parentId === pbi.id && (
            <TaskAddForm
              projectId={projectId}
              pbiId={pbi.id}
              onClose={() => setAddForm(null)}
            />
          )}
        </div>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Feature level
// ---------------------------------------------------------------------------

function FeatureSection({
  sprintId,
  isOwner,
  isExpanded,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  sprintId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const {
    data: features,
    isLoading,
    error,
    refetch,
  } = api.feature.getBySprintId.useQuery({ sprintId }, { enabled: isExpanded });

  if (!isExpanded) return null;
  if (isLoading) {
    return <LoadingState label="features" className="my-2 ml-8 w-4" />;
  }
  if (error) {
    return (
      <QueryError
        label="features"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  return (
    <div className="border-border ml-2 min-w-0 space-y-1 border-l pl-2 sm:ml-6 sm:pl-3">
      {(features ?? []).map((feature) => (
        <FeatureNode
          key={feature.id}
          feature={feature}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          onOpenDetail={onOpenDetail}
        />
      ))}
      {isOwner && (
        <AddFormButton
          level="feature"
          parentId={sprintId}
          addForm={addForm}
          setAddForm={setAddForm}
        />
      )}
      {addForm?.level === "feature" && addForm.parentId === sprintId && (
        <FeatureAddForm
          projectId={projectId}
          sprintId={sprintId}
          onClose={() => setAddForm(null)}
        />
      )}
    </div>
  );
}

function FeatureNode({
  feature,
  isOwner,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  feature: Feature;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [expanded, setExpanded] = useState(false);
  const utils = api.useUtils();

  const deleteMutation = api.feature.delete.useMutation({
    onSuccess: () => {
      void utils.feature.getBySprintId.invalidate();
      toast.success("Feature deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  return (
    <div>
      <TreeRow
        level="feature"
        title={feature.title}
        status={feature.status}
        priority={feature.priority}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() => deleteMutation.mutate({ id: feature.id })}
        isDeleting={deleteMutation.isPending}
        onDetail={() => onOpenDetail({ level: "feature", item: feature })}
      />
      <PbiSection
        featureId={feature.id}
        isOwner={isOwner}
        isExpanded={expanded}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        onOpenDetail={onOpenDetail}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Sprint level
// ---------------------------------------------------------------------------

function SprintSection({
  epicId,
  isOwner,
  isExpanded,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  epicId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const {
    data: sprints,
    isLoading,
    error,
    refetch,
  } = api.sprint.getByEpicId.useQuery({ epicId }, { enabled: isExpanded });

  if (!isExpanded) return null;
  if (isLoading) {
    return <LoadingState label="sprints" className="my-2 ml-8 w-4" />;
  }
  if (error) {
    return (
      <QueryError
        label="sprints"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  return (
    <div className="border-border ml-2 min-w-0 space-y-1 border-l pl-2 sm:ml-6 sm:pl-3">
      {(sprints ?? []).map((sprint) => (
        <SprintNode
          key={sprint.id}
          sprint={sprint}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          onOpenDetail={onOpenDetail}
        />
      ))}
      {isOwner && (
        <AddFormButton
          level="sprint"
          parentId={epicId}
          addForm={addForm}
          setAddForm={setAddForm}
        />
      )}
      {addForm?.level === "sprint" && addForm.parentId === epicId && (
        <SprintAddForm
          projectId={projectId}
          epicId={epicId}
          onClose={() => setAddForm(null)}
        />
      )}
    </div>
  );
}

function SprintNode({
  sprint,
  isOwner,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  sprint: Sprint;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [expanded, setExpanded] = useState(false);
  const utils = api.useUtils();

  const deleteMutation = api.sprint.delete.useMutation({
    onSuccess: () => {
      void utils.sprint.getByEpicId.invalidate();
      toast.success("Sprint deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  const dateLabel =
    sprint.startDate && sprint.endDate
      ? `${new Date(sprint.startDate).toLocaleDateString()} - ${new Date(sprint.endDate).toLocaleDateString()}`
      : undefined;

  return (
    <div>
      <TreeRow
        level="sprint"
        title={sprint.name ?? "Untitled sprint"}
        status={sprint.status}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() => deleteMutation.mutate({ id: sprint.id })}
        isDeleting={deleteMutation.isPending}
        onDetail={() => onOpenDetail({ level: "sprint", item: sprint })}
        extra={
          dateLabel ? (
            <span className="text-muted-foreground text-[10px]">
              {dateLabel}
            </span>
          ) : null
        }
      />
      <FeatureSection
        sprintId={sprint.id}
        isOwner={isOwner}
        isExpanded={expanded}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        onOpenDetail={onOpenDetail}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Epic level
// ---------------------------------------------------------------------------

function EpicSection({
  milestoneId,
  isOwner,
  isExpanded,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  milestoneId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const {
    data: epics,
    isLoading,
    error,
    refetch,
  } = api.epic.getByMilestoneId.useQuery(
    { milestoneId },
    { enabled: isExpanded },
  );

  if (!isExpanded) return null;
  if (isLoading) {
    return <LoadingState label="epics" className="my-2 ml-8 w-4" />;
  }
  if (error) {
    return (
      <QueryError
        label="epics"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  return (
    <div className="border-border ml-2 min-w-0 space-y-1 border-l pl-2 sm:ml-6 sm:pl-3">
      {(epics ?? []).map((epic) => (
        <EpicNode
          key={epic.id}
          epic={epic}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          onOpenDetail={onOpenDetail}
        />
      ))}
      {isOwner && (
        <AddFormButton
          level="epic"
          parentId={milestoneId}
          addForm={addForm}
          setAddForm={setAddForm}
        />
      )}
      {addForm?.level === "epic" && addForm.parentId === milestoneId && (
        <EpicAddForm
          projectId={projectId}
          milestoneId={milestoneId}
          onClose={() => setAddForm(null)}
        />
      )}
    </div>
  );
}

function EpicNode({
  epic,
  isOwner,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  epic: Epic;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [expanded, setExpanded] = useState(false);
  const utils = api.useUtils();

  const deleteMutation = api.epic.delete.useMutation({
    onSuccess: () => {
      void utils.epic.getByMilestoneId.invalidate();
      toast.success("Epic deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  return (
    <div>
      <TreeRow
        level="epic"
        title={epic.title}
        status={epic.status}
        priority={epic.priority}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() => deleteMutation.mutate({ id: epic.id })}
        isDeleting={deleteMutation.isPending}
        onDetail={() => onOpenDetail({ level: "epic", item: epic })}
      />
      <SprintSection
        epicId={epic.id}
        isOwner={isOwner}
        isExpanded={expanded}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        onOpenDetail={onOpenDetail}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Milestone node
// ---------------------------------------------------------------------------

function MilestoneNode({
  milestone,
  isOwner,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  onOpenDetail,
}: {
  milestone: ProjectMilestone;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [expanded, setExpanded] = useState(false);
  const utils = api.useUtils();

  const deleteMutation = api.project.deleteMilestone.useMutation({
    onSuccess: () => {
      void utils.project.getMilestones.invalidate({ projectId });
      toast.success("Milestone deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  return (
    <div>
      <TreeRow
        level="milestone"
        title={milestone.title}
        status={milestone.status}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() =>
          deleteMutation.mutate({ projectId, milestoneId: milestone.id })
        }
        isDeleting={deleteMutation.isPending}
        onDetail={() => onOpenDetail({ level: "milestone", item: milestone })}
        extra={
          milestone.targetDate ? (
            <span className="text-muted-foreground text-[10px]">
              Target: {new Date(milestone.targetDate).toLocaleDateString()}
            </span>
          ) : null
        }
      />
      <EpicSection
        milestoneId={milestone.id}
        isOwner={isOwner}
        isExpanded={expanded}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        onOpenDetail={onOpenDetail}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Generic tree row
// ---------------------------------------------------------------------------

function TreeRow({
  level,
  title,
  status,
  priority,
  childCount,
  isExpanded,
  onToggle,
  isOwner,
  onDelete,
  isDeleting,
  onDetail,
  extra,
}: {
  level: HierarchyLevel;
  title: string;
  status?: string;
  priority?: string;
  childCount?: number;
  isExpanded: boolean;
  onToggle: () => void;
  isOwner: boolean;
  onDelete: () => void;
  isDeleting: boolean;
  onDetail: () => void;
  extra?: React.ReactNode;
}) {
  const colorClass = LEVEL_COLORS[level];

  return (
    <div className="hover:bg-muted/30 group flex min-h-11 min-w-0 flex-wrap items-center gap-1 rounded px-1 py-0.5 sm:flex-nowrap">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={isExpanded}
        aria-label={`${isExpanded ? "Collapse" : "Expand"} ${title}`}
        className="hover:bg-muted focus-visible:ring-primary flex h-11 w-11 shrink-0 items-center justify-center rounded focus-visible:ring-2 focus-visible:outline-none"
      >
        {isExpanded ? (
          <ChevronDown className="text-muted-foreground h-3.5 w-3.5" />
        ) : (
          <ChevronRight className="text-muted-foreground h-3.5 w-3.5" />
        )}
      </button>
      <HierarchyIcon
        level={level}
        className={cn("h-3.5 w-3.5 shrink-0", colorClass)}
      />
      <button
        type="button"
        className="focus-visible:ring-primary min-h-11 min-w-0 flex-1 basis-28 truncate rounded px-1 text-left text-sm font-medium hover:underline focus-visible:ring-2 focus-visible:outline-none"
        onClick={onDetail}
        aria-label={`View ${level} details: ${title}`}
      >
        {title}
      </button>
      {childCount !== undefined && childCount > 0 && (
        <span className="text-muted-foreground text-[10px]">
          ({childCount})
        </span>
      )}
      {extra}
      {status && (
        <Badge variant={statusBadgeVariant(status)} size="sm">
          {formatStatus(status)}
        </Badge>
      )}
      {priority && (
        <Badge variant={priorityBadgeVariant(priority)} size="sm">
          {priority}
        </Badge>
      )}
      {isOwner && (
        <Button
          type="button"
          size="sm"
          variant="ghost"
          className="text-destructive hover:text-destructive h-11 w-11 shrink-0 p-0"
          onClick={(event) => {
            event.stopPropagation();
            if (
              window.confirm(
                `Delete ${level} "${title}"? This action cannot be undone.`,
              )
            ) {
              onDelete();
            }
          }}
          disabled={isDeleting}
          aria-label={`Delete ${level}: ${title}`}
        >
          {isDeleting ? (
            <Loader2 className="h-3 w-3 animate-spin" />
          ) : (
            <Trash2 className="h-3 w-3" />
          )}
        </Button>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Add button (small inline "+")
// ---------------------------------------------------------------------------

function AddFormButton({
  level,
  parentId,
  addForm,
  setAddForm,
}: {
  level: HierarchyLevel;
  parentId: string;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
}) {
  const triggerId = useId();
  const isActive = addForm?.level === level && addForm.parentId === parentId;
  if (isActive) return null;

  const labels: Record<HierarchyLevel, string> = {
    milestone: "Milestone",
    epic: "Epic",
    sprint: "Sprint",
    feature: "Feature",
    pbi: "PBI",
    task: "Task",
  };

  return (
    <button
      id={triggerId}
      type="button"
      onClick={() => setAddForm({ level, parentId, returnFocusId: triggerId })}
      aria-label={`Add ${labels[level]}`}
      className="text-muted-foreground hover:text-foreground hover:bg-muted/40 focus-visible:ring-primary flex min-h-11 items-center gap-1 rounded px-2 py-1 text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
    >
      <Plus className="h-3 w-3" />
      Add {labels[level]}
    </button>
  );
}

// ---------------------------------------------------------------------------
// ADD FORMS
// ---------------------------------------------------------------------------

function MilestoneAddForm({
  projectId,
  onClose,
}: {
  projectId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [targetDate, setTargetDate] = useState("");
  const utils = api.useUtils();

  const mutation = api.project.addMilestone.useMutation({
    onSuccess: () => {
      void utils.project.getMilestones.invalidate({ projectId });
      toast.success("Milestone created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !targetDate) return;
    mutation.mutate({
      projectId,
      title: title.trim(),
      description: description || undefined,
      targetDate: new Date(targetDate).toISOString(),
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="Milestone"
    >
      <FormField label="Title *">
        <input
          className={inputCn}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="Milestone title"
        />
      </FormField>
      <FormField label="Description">
        <input
          className={inputCn}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Optional description"
        />
      </FormField>
      <FormField label="Target Date *">
        <input
          type="date"
          className={inputCn}
          value={targetDate}
          onChange={(e) => setTargetDate(e.target.value)}
          required
        />
      </FormField>
    </InlineForm>
  );
}

function EpicAddForm({
  projectId,
  milestoneId,
  onClose,
}: {
  projectId: string;
  milestoneId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<EpicPriority>("MEDIUM");
  const utils = api.useUtils();

  const mutation = api.epic.create.useMutation({
    onSuccess: () => {
      void utils.epic.getByMilestoneId.invalidate({ milestoneId });
      toast.success("Epic created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      projectId,
      milestoneId,
      title: title.trim(),
      description: description || undefined,
      priority,
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="Epic"
    >
      <FormField label="Title *">
        <input
          className={inputCn}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="Epic title"
        />
      </FormField>
      <FormField label="Description">
        <input
          className={inputCn}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Optional description"
        />
      </FormField>
      <FormField label="Priority">
        <select
          className={selectCn}
          value={priority}
          onChange={(e) => {
            const value = e.target.value;
            if (isOption(HIERARCHY_PRIORITIES, value)) setPriority(value);
          }}
        >
          <option value="LOW">Low</option>
          <option value="MEDIUM">Medium</option>
          <option value="HIGH">High</option>
          <option value="CRITICAL">Critical</option>
        </select>
      </FormField>
    </InlineForm>
  );
}

function SprintAddForm({
  projectId,
  epicId,
  onClose,
}: {
  projectId: string;
  epicId: string;
  onClose: () => void;
}) {
  const [name, setName] = useState("");
  const [goal, setGoal] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const utils = api.useUtils();

  const mutation = api.sprint.create.useMutation({
    onSuccess: () => {
      void utils.sprint.getByEpicId.invalidate({ epicId });
      toast.success("Sprint created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !startDate || !endDate) return;
    if (endDate < startDate) {
      toast.error("End date must be on or after the start date");
      return;
    }
    mutation.mutate({
      projectId,
      epicId,
      name: name.trim(),
      goal: goal || undefined,
      startDate: new Date(startDate).toISOString(),
      endDate: new Date(endDate).toISOString(),
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="Sprint"
    >
      <FormField label="Name *">
        <input
          className={inputCn}
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          placeholder="Sprint name"
        />
      </FormField>
      <FormField label="Goal">
        <input
          className={inputCn}
          value={goal}
          onChange={(e) => setGoal(e.target.value)}
          placeholder="Sprint goal"
        />
      </FormField>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <FormField label="Start Date *">
          <input
            type="date"
            className={inputCn}
            value={startDate}
            max={endDate || undefined}
            onChange={(e) => setStartDate(e.target.value)}
            required
          />
        </FormField>
        <FormField label="End Date *">
          <input
            type="date"
            className={inputCn}
            value={endDate}
            min={startDate || undefined}
            onChange={(e) => setEndDate(e.target.value)}
            required
          />
        </FormField>
      </div>
    </InlineForm>
  );
}

function FeatureAddForm({
  projectId,
  sprintId,
  onClose,
}: {
  projectId: string;
  sprintId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<FeaturePriority>("MEDIUM");
  const utils = api.useUtils();

  const mutation = api.feature.create.useMutation({
    onSuccess: () => {
      void utils.feature.getBySprintId.invalidate({ sprintId });
      toast.success("Feature created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      projectId,
      sprintId,
      title: title.trim(),
      description: description || undefined,
      priority,
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="Feature"
    >
      <FormField label="Title *">
        <input
          className={inputCn}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="Feature title"
        />
      </FormField>
      <FormField label="Description">
        <input
          className={inputCn}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Optional description"
        />
      </FormField>
      <FormField label="Priority">
        <select
          className={selectCn}
          value={priority}
          onChange={(e) => {
            const value = e.target.value;
            if (isOption(HIERARCHY_PRIORITIES, value)) setPriority(value);
          }}
        >
          <option value="LOW">Low</option>
          <option value="MEDIUM">Medium</option>
          <option value="HIGH">High</option>
          <option value="CRITICAL">Critical</option>
        </select>
      </FormField>
    </InlineForm>
  );
}

function PbiAddForm({
  projectId,
  featureId,
  sprintId,
  epicId,
  milestoneId,
  onClose,
}: {
  projectId: string;
  featureId?: string;
  sprintId?: string;
  epicId?: string;
  milestoneId?: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState<PbiType>("FEATURE");
  const [priority, setPriority] = useState<PbiPriority>("MEDIUM");
  const utils = api.useUtils();

  const mutation = api.backlog.createPbi.useMutation({
    onSuccess: () => {
      if (featureId)
        void utils.backlog.getPbisByFeatureId.invalidate({ featureId });
      void utils.backlog.getPbisByProjectId.invalidate({ projectId });
      toast.success("PBI created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      projectId,
      title: title.trim(),
      description: description || undefined,
      type,
      priority,
      featureId,
      sprintId,
      epicId,
      milestoneId,
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="PBI"
    >
      <FormField label="Title *">
        <input
          className={inputCn}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="PBI title"
        />
      </FormField>
      <FormField label="Description">
        <input
          className={inputCn}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Optional description"
        />
      </FormField>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        <FormField label="Type">
          <select
            className={selectCn}
            value={type}
            onChange={(e) => {
              const value = e.target.value;
              if (isOption(PBI_TYPES, value)) setType(value);
            }}
          >
            <option value="FEATURE">Feature</option>
            <option value="ENHANCEMENT">Enhancement</option>
            <option value="BUG">Bug</option>
            <option value="TECHNICAL_DEBT">Technical Debt</option>
            <option value="SPIKE">Spike</option>
          </select>
        </FormField>
        <FormField label="Priority">
          <select
            className={selectCn}
            value={priority}
            onChange={(e) => {
              const value = e.target.value;
              if (isOption(HIERARCHY_PRIORITIES, value)) setPriority(value);
            }}
          >
            <option value="LOW">Low</option>
            <option value="MEDIUM">Medium</option>
            <option value="HIGH">High</option>
            <option value="CRITICAL">Critical</option>
          </select>
        </FormField>
      </div>
    </InlineForm>
  );
}

function TaskAddForm({
  projectId,
  pbiId,
  onClose,
}: {
  projectId: string;
  pbiId?: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState<TaskType>("FEATURE");
  const [priority, setPriority] = useState<TaskPriority>("MEDIUM");
  const [estimatedHours, setEstimatedHours] = useState<TaskEstimatedHours>(8);
  const utils = api.useUtils();

  const mutation = api.task.create.useMutation({
    onSuccess: () => {
      if (pbiId) void utils.task.getByPbiId.invalidate({ pbiId });
      void utils.task.getAll.invalidate();
      void utils.opportunity.getByProjectId.invalidate();
      toast.success("Task created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const taskDescription = description.trim();
    if (!title.trim() || taskDescription.length < 10) {
      toast.error("Task description must be at least 10 characters");
      return;
    }
    mutation.mutate({
      projectId,
      pbiId: pbiId || undefined,
      title: title.trim(),
      description: taskDescription,
      type,
      priority,
      estimatedHours,
    });
  };

  return (
    <InlineForm
      onSubmit={handleSubmit}
      onClose={onClose}
      isPending={mutation.isPending}
      label="Task"
    >
      <FormField label="Title *">
        <input
          className={inputCn}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          placeholder="Task title"
        />
      </FormField>
      <FormField label="Description * (min 10 chars)">
        <input
          className={inputCn}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          required
          placeholder="Task description"
          minLength={10}
        />
      </FormField>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
        <FormField label="Type">
          <select
            className={selectCn}
            value={type}
            onChange={(e) => {
              const value = e.target.value;
              if (isOption(TASK_TYPES, value)) setType(value);
            }}
          >
            <option value="FEATURE">Feature</option>
            <option value="BUG">Bug</option>
            <option value="ENHANCEMENT">Enhancement</option>
            <option value="DOCUMENTATION">Documentation</option>
            <option value="RESEARCH">Research</option>
            <option value="DESIGN">Design</option>
            <option value="TESTING">Testing</option>
            <option value="REVIEW">Review</option>
            <option value="MAINTENANCE">Maintenance</option>
            <option value="OTHER">Other</option>
          </select>
        </FormField>
        <FormField label="Priority">
          <select
            className={selectCn}
            value={priority}
            onChange={(e) => {
              const value = e.target.value;
              if (isOption(TASK_PRIORITIES, value)) setPriority(value);
            }}
          >
            <option value="LOW">Low</option>
            <option value="MEDIUM">Medium</option>
            <option value="HIGH">High</option>
            <option value="URGENT">Urgent</option>
          </select>
        </FormField>
        <FormField label="Estimate">
          <select
            className={selectCn}
            value={estimatedHours}
            onChange={(e) => {
              const hours = parseTaskEstimatedHours(e.target.value);
              if (hours !== null) setEstimatedHours(hours);
            }}
          >
            {TASK_HOUR_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </FormField>
      </div>
    </InlineForm>
  );
}

// ---------------------------------------------------------------------------
// Generic inline form wrapper
// ---------------------------------------------------------------------------

function InlineForm({
  onSubmit,
  onClose,
  isPending,
  label,
  children,
}: {
  onSubmit: (e: React.FormEvent) => void;
  onClose: () => void;
  isPending: boolean;
  label: string;
  children: React.ReactNode;
}) {
  const formRef = useRef<HTMLFormElement>(null);

  useEffect(() => {
    formRef.current
      ?.querySelector<HTMLElement>("input, textarea, select")
      ?.focus();
  }, []);

  return (
    <form
      ref={formRef}
      onSubmit={onSubmit}
      className="border-border bg-card my-1 space-y-2 rounded-md border p-3"
    >
      <div className="text-muted-foreground mb-1 text-xs font-semibold">
        New {label}
      </div>
      {children}
      <div className="flex gap-2 pt-1">
        <Button
          type="submit"
          size="sm"
          className="min-h-11"
          disabled={isPending}
        >
          {isPending && <Loader2 className="mr-1.5 h-3 w-3 animate-spin" />}
          Create
        </Button>
        <Button
          type="button"
          size="sm"
          variant="ghost"
          className="min-h-11"
          onClick={onClose}
        >
          Cancel
        </Button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Team Positions view
// ---------------------------------------------------------------------------

function TeamPositionsView({
  projectId,
  projectSlug,
  canManage,
}: {
  projectId: string;
  projectSlug: string;
  canManage: boolean;
}) {
  const {
    data: teamPositions = [],
    isLoading,
    error,
    refetch,
  } = api.opportunity.getByProjectId.useQuery({
    projectId,
  });
  const positions = teamPositions.filter(
    (opportunity) =>
      opportunity.origin === "TEAM_POSITION" &&
      Boolean(opportunity.projectRole),
  );

  if (isLoading) {
    return <LoadingState label="team positions" className="py-8" />;
  }

  if (error) {
    return (
      <QueryError
        label="team positions"
        message={error.message}
        onRetry={() => void refetch()}
      />
    );
  }

  if (positions.length === 0) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <Users className="text-muted-foreground/50 mx-auto mb-3 h-10 w-10" />
          <h3 className="mb-1 text-base font-medium">No team positions</h3>
          <p className="text-muted-foreground mb-3 text-sm">
            {canManage
              ? "Create team position opportunities from the Opportunities page."
              : "No team positions have been posted yet."}
          </p>
          {canManage && (
            <Button asChild variant="outline" size="sm" className="min-h-11">
              <Link
                href={`/opportunities/create?entityType=project&entityId=${projectId}&entitySlug=${projectSlug}`}
              >
                <Plus className="mr-1.5 h-3.5 w-3.5" />
                Create Position
              </Link>
            </Button>
          )}
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-3">
      {positions.map((opp) => (
        <Card key={opp.id}>
          <CardContent className="py-4">
            <div className="flex min-w-0 flex-wrap items-center justify-between gap-2">
              <div className="flex min-w-0 items-center gap-2">
                <Briefcase className="text-muted-foreground h-4 w-4" />
                <Link
                  href={`/opportunities/${opp.slug || opp.id}`}
                  className="hover:text-primary focus-visible:ring-primary flex min-h-11 min-w-0 items-center truncate rounded text-sm font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
                >
                  {opp.title}
                </Link>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                {opp.projectRole && (
                  <Badge
                    variant={getRoleBadgeVariant(opp.projectRole)}
                    size="sm"
                  >
                    {formatProjectRoleName(opp.projectRole)}
                  </Badge>
                )}
                {opp.type && (
                  <Badge variant="neon-purple" size="sm">
                    {opp.type}
                  </Badge>
                )}
                <Badge variant={statusBadgeVariant(opp.status)} size="sm">
                  {formatStatus(opp.status)}
                </Badge>
              </div>
            </div>
            {opp.description && (
              <p className="text-muted-foreground mt-1.5 ml-6 line-clamp-2 text-xs">
                {opp.description}
              </p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Flat Tasks view
// ---------------------------------------------------------------------------

function FlatTasksView({
  projectId,
  isOwner,
  opportunities,
  onOpenDetail,
}: {
  projectId: string;
  isOwner: boolean;
  opportunities: ProjectOpportunity[];
  onOpenDetail: OpenWorkItemDetail;
}) {
  const [showAddTask, setShowAddTask] = useState(false);
  const [showAddPbi, setShowAddPbi] = useState(false);
  const {
    data: tasksResult,
    isLoading: tasksLoading,
    error: tasksError,
    refetch: refetchTasks,
  } = api.task.getAll.useQuery({
    projectId,
    limit: 100,
  });
  const {
    data: pbis = [],
    isLoading: pbisLoading,
    error: pbisError,
    refetch: refetchPbis,
  } = api.backlog.getPbisByProjectId.useQuery({
    projectId,
  });

  const allTasks = tasksResult?.items ?? [];

  if (tasksLoading || pbisLoading) {
    return <LoadingState label="project work" className="py-8" />;
  }

  if (tasksError || pbisError) {
    return (
      <div className="space-y-2">
        {tasksError && (
          <QueryError
            label="tasks"
            message={tasksError.message}
            onRetry={() => void refetchTasks()}
          />
        )}
        {pbisError && (
          <QueryError
            label="backlog items"
            message={pbisError.message}
            onRetry={() => void refetchPbis()}
          />
        )}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* PBIs section */}
      {pbis.length > 0 && (
        <Card>
          <CardContent className="min-w-0 overflow-hidden px-3 py-3">
            <div className="text-muted-foreground mb-2 flex items-center gap-1.5 text-xs font-semibold">
              <Layers className="text-neon-yellow h-3.5 w-3.5" />
              Product Backlog Items ({pbis.length})
            </div>
            <div className="space-y-0.5">
              {pbis.map((pbi) => (
                <div
                  key={pbi.id}
                  className="hover:bg-muted/30 flex min-h-11 min-w-0 flex-wrap items-center gap-2 rounded px-2 py-1 sm:flex-nowrap"
                >
                  <Layers className="text-neon-yellow h-3.5 w-3.5 shrink-0" />
                  <button
                    type="button"
                    onClick={() => onOpenDetail({ level: "pbi", item: pbi })}
                    aria-label={`View PBI details: ${pbi.title}`}
                    className="focus-visible:ring-primary flex min-h-11 min-w-0 flex-1 items-center gap-1 rounded text-left text-sm focus-visible:ring-2 focus-visible:outline-none"
                  >
                    <span className="truncate">{pbi.title}</span>
                    <Eye
                      className="h-3.5 w-3.5 shrink-0 opacity-60"
                      aria-hidden="true"
                    />
                  </button>
                  {pbi.type && (
                    <Badge variant="outline" size="sm">
                      {pbi.type}
                    </Badge>
                  )}
                  {pbi.priority && (
                    <Badge
                      variant={priorityBadgeVariant(pbi.priority)}
                      size="sm"
                    >
                      {pbi.priority}
                    </Badge>
                  )}
                  <Badge variant={statusBadgeVariant(pbi.status)} size="sm">
                    {formatStatus(pbi.status)}
                  </Badge>
                  {pbi.guildId && (
                    <Badge
                      variant="neon-purple"
                      size="sm"
                      className="text-[9px]"
                    >
                      Guild
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Tasks section */}
      <Card>
        <CardContent className="min-w-0 overflow-hidden px-3 py-3">
          <div className="text-muted-foreground mb-2 flex items-center gap-1.5 text-xs font-semibold">
            <CheckSquare className="h-3.5 w-3.5" />
            All Tasks ({allTasks.length})
          </div>
          {allTasks.length === 0 && !isOwner && (
            <p className="text-muted-foreground py-4 text-center text-sm">
              No tasks yet.
            </p>
          )}
          <div className="space-y-0.5">
            {allTasks.map((task) => (
              <TaskRow
                key={task.id}
                task={task}
                isOwner={isOwner}
                opportunities={opportunities}
                onOpenDetail={onOpenDetail}
              />
            ))}
          </div>
          {isOwner && (
            <div className="mt-2 flex gap-2">
              <button
                type="button"
                onClick={() => {
                  setShowAddTask(true);
                  setShowAddPbi(false);
                }}
                aria-label="Add task"
                className="text-muted-foreground hover:text-foreground hover:bg-muted/40 focus-visible:ring-primary flex min-h-11 items-center gap-1 rounded px-2 py-1 text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
              >
                <Plus className="h-3 w-3" /> Add Task
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowAddPbi(true);
                  setShowAddTask(false);
                }}
                aria-label="Add product backlog item"
                className="text-muted-foreground hover:text-foreground hover:bg-muted/40 focus-visible:ring-primary flex min-h-11 items-center gap-1 rounded px-2 py-1 text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
              >
                <Plus className="h-3 w-3" /> Add PBI
              </button>
            </div>
          )}
          {showAddTask && (
            <TaskAddForm
              projectId={projectId}
              onClose={() => setShowAddTask(false)}
            />
          )}
          {showAddPbi && (
            <PbiAddForm
              projectId={projectId}
              onClose={() => setShowAddPbi(false)}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ---------------------------------------------------------------------------
// MAIN COMPONENT
// ---------------------------------------------------------------------------

export default function OpportunitiesTab({
  projectId,
  projectSlug,
  isOwner,
  canComment,
  userRole,
}: OpportunitiesTabProps) {
  const [viewMode, setViewMode] = useState<ViewMode>("hierarchy");
  const [addForm, setAddFormState] = useState<AddFormState | null>(null);
  // Detail modal state
  const [detailModal, setDetailModal] =
    useState<WorkItemDetailSelection | null>(null);
  const detailTriggerRef = useRef<HTMLElement | null>(null);
  // Create modal state
  const [createModal, setCreateModal] = useState<{
    level: CreateLevel;
    parentId?: string;
  } | null>(null);

  const canManage =
    isOwner ||
    userRole === "FOUNDER" ||
    userRole === "LEADER" ||
    userRole === "CORE_CONTRIBUTOR";
  const setAddForm = (next: AddFormState | null) => {
    const returnFocusId = addForm?.returnFocusId;
    setAddFormState(next);
    if (next === null && returnFocusId) {
      window.requestAnimationFrame(() =>
        document.getElementById(returnFocusId)?.focus(),
      );
    }
  };
  const utils = api.useUtils();
  const openWorkItemDetail: OpenWorkItemDetail = (selection) => {
    detailTriggerRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    setDetailModal(selection);
  };
  const closeWorkItemDetail = () => {
    setDetailModal(null);
    window.requestAnimationFrame(() => detailTriggerRef.current?.focus());
  };

  // Top-level data: milestones, opportunities (always fetched)
  const {
    data: milestones,
    isLoading: milestonesLoading,
    error: milestonesError,
    refetch: refetchMilestones,
  } = api.project.getMilestones.useQuery({ projectId });

  const {
    data: projectOpportunities = [],
    error: projectOpportunitiesError,
    refetch: refetchProjectOpportunities,
  } = api.opportunity.getByProjectId.useQuery({
    projectId,
  });

  return (
    <div className="space-y-4">
      {/* View Toggle */}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="border-border flex w-full max-w-full items-center gap-1 overflow-x-auto rounded-md border p-0.5 sm:w-auto">
          <button
            type="button"
            className={cn(
              "min-h-11 shrink-0 rounded-sm px-3 py-1.5 text-sm transition-colors",
              viewMode === "hierarchy"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:text-foreground",
            )}
            onClick={() => setViewMode("hierarchy")}
            aria-pressed={viewMode === "hierarchy"}
          >
            <ListTodo className="-mt-0.5 mr-1.5 inline h-3.5 w-3.5" />
            Hierarchy
          </button>
          <button
            type="button"
            className={cn(
              "min-h-11 shrink-0 rounded-sm px-3 py-1.5 text-sm transition-colors",
              viewMode === "flat-tasks"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:text-foreground",
            )}
            onClick={() => setViewMode("flat-tasks")}
            aria-pressed={viewMode === "flat-tasks"}
          >
            <CheckSquare className="-mt-0.5 mr-1.5 inline h-3.5 w-3.5" />
            All Tasks
          </button>
          <button
            type="button"
            className={cn(
              "min-h-11 shrink-0 rounded-sm px-3 py-1.5 text-sm transition-colors",
              viewMode === "team-positions"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:text-foreground",
            )}
            onClick={() => setViewMode("team-positions")}
            aria-pressed={viewMode === "team-positions"}
          >
            <Users className="-mt-0.5 mr-1.5 inline h-3.5 w-3.5" />
            Team Positions
          </button>
        </div>

        {viewMode === "hierarchy" && canManage && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setCreateModal({ level: "milestone" })}
            className="min-h-11 w-full sm:w-auto"
          >
            <Plus className="mr-1 h-3.5 w-3.5" />
            Add Milestone
          </Button>
        )}
      </div>

      {viewMode === "hierarchy" && milestonesError && (
        <QueryError
          label="milestones"
          message={milestonesError.message}
          onRetry={() => void refetchMilestones()}
        />
      )}
      {viewMode !== "team-positions" && projectOpportunitiesError && (
        <QueryError
          label="linked opportunities"
          message={projectOpportunitiesError.message}
          onRetry={() => void refetchProjectOpportunities()}
        />
      )}

      {/* Views */}
      {viewMode === "team-positions" ? (
        <TeamPositionsView
          projectId={projectId}
          projectSlug={projectSlug}
          canManage={canManage}
        />
      ) : viewMode === "flat-tasks" ? (
        <FlatTasksView
          projectId={projectId}
          isOwner={canManage}
          opportunities={projectOpportunities}
          onOpenDetail={openWorkItemDetail}
        />
      ) : (
        <div className="space-y-2">
          {/* Milestone add form */}
          {addForm?.level === "milestone" && addForm.parentId === projectId && (
            <MilestoneAddForm
              projectId={projectId}
              onClose={() => setAddForm(null)}
            />
          )}

          {/* Loading state */}
          {milestonesLoading && (
            <LoadingState label="milestones" className="py-12" />
          )}

          {/* Empty state */}
          {!milestonesLoading &&
            !milestonesError &&
            (!milestones || milestones.length === 0) && (
              <Card>
                <CardContent className="py-10 text-center">
                  <Flag className="text-muted-foreground/50 mx-auto mb-3 h-10 w-10" />
                  <h3 className="mb-1 text-base font-medium">
                    No work items yet
                  </h3>
                  <p className="text-muted-foreground mb-3 text-sm">
                    {canManage
                      ? "Start by creating a milestone, or switch to 'All Tasks' for a flat list."
                      : "No work items have been created for this project yet."}
                  </p>
                  {canManage && (
                    <div className="flex justify-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        className="min-h-11"
                        onClick={() => setCreateModal({ level: "milestone" })}
                      >
                        <Plus className="mr-1.5 h-3.5 w-3.5" />
                        Create Milestone
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="min-h-11"
                        onClick={() => setViewMode("flat-tasks")}
                      >
                        <CheckSquare className="mr-1.5 h-3.5 w-3.5" />
                        Quick Task List
                      </Button>
                    </div>
                  )}
                </CardContent>
              </Card>
            )}

          {/* Hierarchy tree */}
          {!milestonesError && milestones && milestones.length > 0 && (
            <Card>
              <CardContent className="min-w-0 overflow-hidden px-3 py-3">
                <div className="space-y-0.5">
                  {milestones.map((milestone) => (
                    <MilestoneNode
                      key={milestone.id}
                      milestone={milestone}
                      isOwner={canManage}
                      addForm={addForm}
                      setAddForm={setAddForm}
                      projectId={projectId}
                      opportunities={projectOpportunities}
                      onOpenDetail={openWorkItemDetail}
                    />
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Legend */}
          {!milestonesError && milestones && milestones.length > 0 && (
            <div className="text-muted-foreground flex flex-wrap gap-3 px-1 text-[10px]">
              {HIERARCHY_LEVELS.map((level) => (
                <span key={level} className="flex items-center gap-1">
                  <HierarchyIcon
                    level={level}
                    className={cn("h-3 w-3", LEVEL_COLORS[level])}
                  />
                  {level.charAt(0).toUpperCase() + level.slice(1)}
                </span>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Detail Modal */}
      {detailModal && (
        <WorkItemDetailModal
          open={!!detailModal}
          onClose={closeWorkItemDetail}
          selection={detailModal}
          projectId={projectId}
          canComment={canComment}
          signInHref={`/auth/signin?callbackUrl=${encodeURIComponent(`/projects/${projectSlug}`)}`}
        />
      )}

      {/* Create Modal */}
      {createModal && (
        <CreateWorkItemModal
          open={!!createModal}
          onClose={() => setCreateModal(null)}
          level={createModal.level}
          projectId={projectId}
          parentId={createModal.parentId}
          onCreated={() => {
            void utils.project.getMilestones.invalidate({ projectId });
            void utils.epic.getByMilestoneId.invalidate();
            void utils.sprint.getByEpicId.invalidate();
            void utils.feature.getBySprintId.invalidate();
            void utils.backlog.getPbisByFeatureId.invalidate();
            void utils.backlog.getPbisByProjectId.invalidate();
            void utils.task.getByPbiId.invalidate();
            void utils.task.getAll.invalidate();
          }}
        />
      )}
    </div>
  );
}
