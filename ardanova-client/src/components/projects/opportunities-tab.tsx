"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { api } from "~/trpc/react";
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
} from "lucide-react";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface OpportunitiesTabProps {
  projectId: string;
  projectSlug: string;
  isOwner: boolean;
  userRole?: string;
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
}

type ViewMode = "hierarchy" | "team-positions";

// ---------------------------------------------------------------------------
// Badge helpers
// ---------------------------------------------------------------------------

function statusBadgeVariant(
  status: string
): "secondary" | "neon" | "neon-green" | "destructive" | "info" {
  const s = status.toUpperCase();
  if (
    ["PLANNED", "DRAFT", "NEW", "BACKLOG", "PLANNING", "TODO"].includes(s)
  )
    return "secondary";
  if (["IN_PROGRESS", "ACTIVE", "READY", "IN_REVIEW"].includes(s))
    return "neon";
  if (["COMPLETED", "DONE"].includes(s)) return "neon-green";
  if (["CANCELLED", "REMOVED"].includes(s)) return "destructive";
  return "secondary";
}

function priorityBadgeVariant(
  priority: string
): "destructive" | "warning" | "secondary" | "outline" {
  const p = priority.toUpperCase();
  if (p === "CRITICAL") return "destructive";
  if (p === "HIGH") return "warning";
  if (p === "MEDIUM") return "secondary";
  return "outline";
}

function formatStatus(s: string) {
  return s.replace(/_/g, " ");
}

// ---------------------------------------------------------------------------
// Level metadata
// ---------------------------------------------------------------------------

const LEVEL_ICONS: Record<HierarchyLevel, React.ElementType> = {
  milestone: Flag,
  epic: Target,
  sprint: Zap,
  feature: Box,
  pbi: Layers,
  task: CheckSquare,
};

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
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-xs font-medium text-muted-foreground mb-1">
        {label}
      </label>
      {children}
    </div>
  );
}

const inputCn =
  "w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary";
const selectCn =
  "w-full rounded-md border border-input bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary";

// ---------------------------------------------------------------------------
// Sub-components for each hierarchy level
// ---------------------------------------------------------------------------

function TaskRow({
  task,
  isOwner,
  projectId,
  opportunities,
}: {
  task: any;
  isOwner: boolean;
  projectId: string;
  opportunities: any[];
}) {
  const utils = api.useUtils();

  const deleteMutation = api.task.delete.useMutation({
    onSuccess: () => {
      void utils.task.getAll.invalidate();
      toast.success("Task deleted");
    },
    onError: (e) => toast.error(e.message),
  });

  // Find the auto-generated opportunity for this task
  const linkedOpp = opportunities.find(
    (o: any) => o.taskId === task.id
  );

  return (
    <div className="flex items-center gap-2 py-1.5 px-2 rounded hover:bg-muted/30 group">
      <CheckSquare className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
      <span className="text-sm flex-1 truncate">{task.title}</span>
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
      {task.effort && (
        <Badge variant="outline" size="sm">
          {task.effort.toUpperCase()}
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
          className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100"
          onClick={() => deleteMutation.mutate({ id: task.id })}
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
  onToggle,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  allTasks,
}: {
  featureId: string;
  isOwner: boolean;
  isExpanded: boolean;
  onToggle: () => void;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
}) {
  const { data: pbis, isLoading } = api.backlog.getPbisByFeatureId.useQuery(
    { featureId },
    { enabled: isExpanded }
  );

  if (!isExpanded) return null;
  if (isLoading) return <Loader2 className="h-4 w-4 animate-spin ml-8 my-2" />;

  return (
    <div className="ml-6 border-l border-border pl-3 space-y-1">
      {(pbis ?? []).map((pbi: any) => (
        <PbiNode
          key={pbi.id}
          pbi={pbi}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          allTasks={allTasks}
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
        <PbiAddForm featureId={featureId} onClose={() => setAddForm(null)} />
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
  allTasks,
}: {
  pbi: any;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
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

  // Tasks linked to this PBI
  const linkedTasks = allTasks.filter((t: any) => t.pbiId === pbi.id);

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
        extra={
          pbi.type ? (
            <Badge variant="outline" size="sm">
              {pbi.type}
            </Badge>
          ) : null
        }
      />
      {expanded && (
        <div className="ml-6 border-l border-border pl-3 space-y-0.5">
          {linkedTasks.map((task: any) => (
            <TaskRow
              key={task.id}
              task={task}
              isOwner={isOwner}
              projectId={projectId}
              opportunities={opportunities}
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
  onToggle,
  addForm,
  setAddForm,
  projectId,
  opportunities,
  allTasks,
}: {
  sprintId: string;
  isOwner: boolean;
  isExpanded: boolean;
  onToggle: () => void;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
}) {
  const { data: features, isLoading } = api.feature.getBySprintId.useQuery(
    { sprintId },
    { enabled: isExpanded }
  );

  if (!isExpanded) return null;
  if (isLoading) return <Loader2 className="h-4 w-4 animate-spin ml-8 my-2" />;

  return (
    <div className="ml-6 border-l border-border pl-3 space-y-1">
      {(features ?? []).map((feature: any) => (
        <FeatureNode
          key={feature.id}
          feature={feature}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          allTasks={allTasks}
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
        <FeatureAddForm sprintId={sprintId} onClose={() => setAddForm(null)} />
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
  allTasks,
}: {
  feature: any;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
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
      />
      <PbiSection
        featureId={feature.id}
        isOwner={isOwner}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        allTasks={allTasks}
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
  allTasks,
}: {
  epicId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
}) {
  const { data: sprints, isLoading } = api.sprint.getByEpicId.useQuery(
    { epicId },
    { enabled: isExpanded }
  );

  if (!isExpanded) return null;
  if (isLoading) return <Loader2 className="h-4 w-4 animate-spin ml-8 my-2" />;

  return (
    <div className="ml-6 border-l border-border pl-3 space-y-1">
      {(sprints ?? []).map((sprint: any) => (
        <SprintNode
          key={sprint.id}
          sprint={sprint}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          allTasks={allTasks}
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
        <SprintAddForm epicId={epicId} onClose={() => setAddForm(null)} />
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
  allTasks,
}: {
  sprint: any;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
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
        title={sprint.name}
        status={sprint.status}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() => deleteMutation.mutate({ id: sprint.id })}
        isDeleting={deleteMutation.isPending}
        extra={
          dateLabel ? (
            <span className="text-[10px] text-muted-foreground">{dateLabel}</span>
          ) : null
        }
      />
      <FeatureSection
        sprintId={sprint.id}
        isOwner={isOwner}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        allTasks={allTasks}
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
  allTasks,
}: {
  milestoneId: string;
  isOwner: boolean;
  isExpanded: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
}) {
  const { data: epics, isLoading } = api.epic.getByMilestoneId.useQuery(
    { milestoneId },
    { enabled: isExpanded }
  );

  if (!isExpanded) return null;
  if (isLoading) return <Loader2 className="h-4 w-4 animate-spin ml-8 my-2" />;

  return (
    <div className="ml-6 border-l border-border pl-3 space-y-1">
      {(epics ?? []).map((epic: any) => (
        <EpicNode
          key={epic.id}
          epic={epic}
          isOwner={isOwner}
          addForm={addForm}
          setAddForm={setAddForm}
          projectId={projectId}
          opportunities={opportunities}
          allTasks={allTasks}
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
        <EpicAddForm milestoneId={milestoneId} onClose={() => setAddForm(null)} />
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
  allTasks,
}: {
  epic: any;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
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
      />
      <SprintSection
        epicId={epic.id}
        isOwner={isOwner}
        isExpanded={expanded}
        addForm={addForm}
        setAddForm={setAddForm}
        projectId={projectId}
        opportunities={opportunities}
        allTasks={allTasks}
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
  allTasks,
}: {
  milestone: any;
  isOwner: boolean;
  addForm: AddFormState | null;
  setAddForm: (f: AddFormState | null) => void;
  projectId: string;
  opportunities: any[];
  allTasks: any[];
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

  const milestoneStatus = milestone.isCompleted ? "COMPLETED" : "PLANNED";

  return (
    <div>
      <TreeRow
        level="milestone"
        title={milestone.title}
        status={milestoneStatus}
        isExpanded={expanded}
        onToggle={() => setExpanded(!expanded)}
        isOwner={isOwner}
        onDelete={() =>
          deleteMutation.mutate({ projectId, milestoneId: milestone.id })
        }
        isDeleting={deleteMutation.isPending}
        extra={
          milestone.targetDate ? (
            <span className="text-[10px] text-muted-foreground">
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
        allTasks={allTasks}
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
  extra?: React.ReactNode;
}) {
  const Icon = LEVEL_ICONS[level];
  const colorClass = LEVEL_COLORS[level];

  return (
    <div className="flex items-center gap-1.5 py-1.5 px-1 rounded hover:bg-muted/30 group cursor-pointer">
      <button
        onClick={onToggle}
        className="shrink-0 p-0.5 hover:bg-muted rounded"
      >
        {isExpanded ? (
          <ChevronDown className="h-3.5 w-3.5 text-muted-foreground" />
        ) : (
          <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />
        )}
      </button>
      <Icon className={cn("h-3.5 w-3.5 shrink-0", colorClass)} />
      <span className="text-sm font-medium truncate flex-1" onClick={onToggle}>
        {title}
      </span>
      {childCount !== undefined && childCount > 0 && (
        <span className="text-[10px] text-muted-foreground">({childCount})</span>
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
          size="sm"
          variant="ghost"
          className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 shrink-0"
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
          disabled={isDeleting}
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
      onClick={() => setAddForm({ level, parentId })}
      className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground py-1 px-2 rounded hover:bg-muted/40 transition-colors"
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
      targetDate,
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="Milestone">
      <FormField label="Title *">
        <input className={inputCn} value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Milestone title" />
      </FormField>
      <FormField label="Description">
        <input className={inputCn} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Optional description" />
      </FormField>
      <FormField label="Target Date *">
        <input type="date" className={inputCn} value={targetDate} onChange={(e) => setTargetDate(e.target.value)} required />
      </FormField>
    </InlineForm>
  );
}

function EpicAddForm({
  milestoneId,
  onClose,
}: {
  milestoneId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("MEDIUM");
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
      milestoneId,
      title: title.trim(),
      description: description || undefined,
      priority: priority as any,
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="Epic">
      <FormField label="Title *">
        <input className={inputCn} value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Epic title" />
      </FormField>
      <FormField label="Description">
        <input className={inputCn} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Optional description" />
      </FormField>
      <FormField label="Priority">
        <select className={selectCn} value={priority} onChange={(e) => setPriority(e.target.value)}>
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
  epicId,
  onClose,
}: {
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
    mutation.mutate({
      epicId,
      name: name.trim(),
      goal: goal || undefined,
      startDate: new Date(startDate).toISOString(),
      endDate: new Date(endDate).toISOString(),
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="Sprint">
      <FormField label="Name *">
        <input className={inputCn} value={name} onChange={(e) => setName(e.target.value)} required placeholder="Sprint name" />
      </FormField>
      <FormField label="Goal">
        <input className={inputCn} value={goal} onChange={(e) => setGoal(e.target.value)} placeholder="Sprint goal" />
      </FormField>
      <div className="grid grid-cols-2 gap-2">
        <FormField label="Start Date *">
          <input type="date" className={inputCn} value={startDate} onChange={(e) => setStartDate(e.target.value)} required />
        </FormField>
        <FormField label="End Date *">
          <input type="date" className={inputCn} value={endDate} onChange={(e) => setEndDate(e.target.value)} required />
        </FormField>
      </div>
    </InlineForm>
  );
}

function FeatureAddForm({
  sprintId,
  onClose,
}: {
  sprintId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("MEDIUM");
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
      sprintId,
      title: title.trim(),
      description: description || undefined,
      priority: priority as any,
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="Feature">
      <FormField label="Title *">
        <input className={inputCn} value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Feature title" />
      </FormField>
      <FormField label="Description">
        <input className={inputCn} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Optional description" />
      </FormField>
      <FormField label="Priority">
        <select className={selectCn} value={priority} onChange={(e) => setPriority(e.target.value)}>
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
  featureId,
  onClose,
}: {
  featureId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState("FEATURE");
  const [priority, setPriority] = useState("MEDIUM");
  const utils = api.useUtils();

  const mutation = api.backlog.createPbi.useMutation({
    onSuccess: () => {
      void utils.backlog.getPbisByFeatureId.invalidate({ featureId });
      toast.success("PBI created");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    mutation.mutate({
      featureId,
      title: title.trim(),
      description: description || undefined,
      type: type as any,
      priority: priority as any,
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="PBI">
      <FormField label="Title *">
        <input className={inputCn} value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="PBI title" />
      </FormField>
      <FormField label="Description">
        <input className={inputCn} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Optional description" />
      </FormField>
      <div className="grid grid-cols-2 gap-2">
        <FormField label="Type">
          <select className={selectCn} value={type} onChange={(e) => setType(e.target.value)}>
            <option value="FEATURE">Feature</option>
            <option value="ENHANCEMENT">Enhancement</option>
            <option value="BUG">Bug</option>
            <option value="TECHNICAL_DEBT">Technical Debt</option>
            <option value="SPIKE">Spike</option>
          </select>
        </FormField>
        <FormField label="Priority">
          <select className={selectCn} value={priority} onChange={(e) => setPriority(e.target.value)}>
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
  pbiId: string;
  onClose: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [type, setType] = useState("feature");
  const [priority, setPriority] = useState("medium");
  const [effort, setEffort] = useState("m");
  const utils = api.useUtils();

  const mutation = api.task.create.useMutation({
    onSuccess: () => {
      void utils.task.getAll.invalidate();
      void utils.opportunity.getAll.invalidate();
      toast.success("Task created (opportunity auto-generated)");
      onClose();
    },
    onError: (e) => toast.error(e.message),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) return;
    mutation.mutate({
      projectId,
      pbiId,
      title: title.trim(),
      description: description.trim(),
      type: type as any,
      priority: priority as any,
      effort: effort as any,
    });
  };

  return (
    <InlineForm onSubmit={handleSubmit} onClose={onClose} isPending={mutation.isPending} label="Task">
      <FormField label="Title *">
        <input className={inputCn} value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Task title" />
      </FormField>
      <FormField label="Description * (min 10 chars)">
        <input className={inputCn} value={description} onChange={(e) => setDescription(e.target.value)} required placeholder="Task description" minLength={10} />
      </FormField>
      <div className="grid grid-cols-3 gap-2">
        <FormField label="Type">
          <select className={selectCn} value={type} onChange={(e) => setType(e.target.value)}>
            <option value="feature">Feature</option>
            <option value="bug">Bug</option>
            <option value="improvement">Improvement</option>
            <option value="documentation">Documentation</option>
            <option value="research">Research</option>
            <option value="design">Design</option>
            <option value="other">Other</option>
          </select>
        </FormField>
        <FormField label="Priority">
          <select className={selectCn} value={priority} onChange={(e) => setPriority(e.target.value)}>
            <option value="low">Low</option>
            <option value="medium">Medium</option>
            <option value="high">High</option>
            <option value="critical">Critical</option>
          </select>
        </FormField>
        <FormField label="Effort">
          <select className={selectCn} value={effort} onChange={(e) => setEffort(e.target.value)}>
            <option value="xs">XS (~1h)</option>
            <option value="s">S (~2h)</option>
            <option value="m">M (~4h)</option>
            <option value="l">L (~8h)</option>
            <option value="xl">XL (~16h)</option>
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
  return (
    <form
      onSubmit={onSubmit}
      className="rounded-md border border-border bg-card p-3 space-y-2 my-1"
    >
      <div className="text-xs font-semibold text-muted-foreground mb-1">
        New {label}
      </div>
      {children}
      <div className="flex gap-2 pt-1">
        <Button type="submit" size="sm" disabled={isPending}>
          {isPending && <Loader2 className="mr-1.5 h-3 w-3 animate-spin" />}
          Create
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={onClose}>
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
  isOwner,
}: {
  projectId: string;
  projectSlug: string;
  isOwner: boolean;
}) {
  const { data: teamPositions = [], isLoading } = api.opportunity.getByProjectId.useQuery({
    projectId,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (teamPositions.length === 0) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <Users className="mx-auto h-10 w-10 text-muted-foreground/50 mb-3" />
          <h3 className="text-base font-medium mb-1">No team positions</h3>
          <p className="text-sm text-muted-foreground mb-3">
            {isOwner
              ? "Create team position opportunities from the Opportunities page."
              : "No team positions have been posted yet."}
          </p>
          {isOwner && (
            <Button asChild variant="outline" size="sm">
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
      {teamPositions.map((opp: any) => (
        <Card key={opp.id}>
          <CardContent className="py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Briefcase className="h-4 w-4 text-muted-foreground" />
                <Link
                  href={`/opportunities/${opp.slug || opp.id}`}
                  className="font-medium text-sm hover:text-primary transition-colors"
                >
                  {opp.title}
                </Link>
              </div>
              <div className="flex items-center gap-2">
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
              <p className="text-xs text-muted-foreground mt-1.5 line-clamp-2 ml-6">
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
// MAIN COMPONENT
// ---------------------------------------------------------------------------

export default function OpportunitiesTab({
  projectId,
  projectSlug,
  isOwner,
  userRole,
}: OpportunitiesTabProps) {
  const [viewMode, setViewMode] = useState<ViewMode>("hierarchy");
  const [addForm, setAddForm] = useState<AddFormState | null>(null);

  const canManage = isOwner || userRole === "LEAD" || userRole === "ADMIN";

  // Top-level data: milestones, tasks, opportunities (always fetched)
  const {
    data: milestones,
    isLoading: milestonesLoading,
  } = api.project.getMilestones.useQuery({ projectId });

  const { data: tasksResult } = api.task.getAll.useQuery({
    projectId,
    limit: 100,
  });
  const allTasks = tasksResult?.items ?? [];

  const { data: opportunitiesResult } = api.opportunity.getAll.useQuery({
    limit: 100,
  });
  const projectOpportunities = useMemo(() => {
    const all = opportunitiesResult?.items ?? [];
    return all.filter((o: any) => o.projectId === projectId);
  }, [opportunitiesResult, projectId]);

  return (
    <div className="space-y-4">
      {/* View Toggle */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-1 rounded-md border border-border p-0.5">
          <button
            className={cn(
              "px-3 py-1.5 text-sm rounded-sm transition-colors",
              viewMode === "hierarchy"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:text-foreground"
            )}
            onClick={() => setViewMode("hierarchy")}
          >
            <ListTodo className="inline h-3.5 w-3.5 mr-1.5 -mt-0.5" />
            Hierarchy
          </button>
          <button
            className={cn(
              "px-3 py-1.5 text-sm rounded-sm transition-colors",
              viewMode === "team-positions"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:text-foreground"
            )}
            onClick={() => setViewMode("team-positions")}
          >
            <Users className="inline h-3.5 w-3.5 mr-1.5 -mt-0.5" />
            Team Positions
          </button>
        </div>

        {viewMode === "hierarchy" && canManage && (
          <AddFormButton
            level="milestone"
            parentId={projectId}
            addForm={addForm}
            setAddForm={setAddForm}
          />
        )}
      </div>

      {/* Views */}
      {viewMode === "team-positions" ? (
        <TeamPositionsView
          projectId={projectId}
          projectSlug={projectSlug}
          isOwner={isOwner}
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
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          )}

          {/* Empty state */}
          {!milestonesLoading && (!milestones || milestones.length === 0) && (
            <Card>
              <CardContent className="py-10 text-center">
                <Flag className="mx-auto h-10 w-10 text-muted-foreground/50 mb-3" />
                <h3 className="text-base font-medium mb-1">
                  No work items yet
                </h3>
                <p className="text-sm text-muted-foreground mb-3">
                  {canManage
                    ? "Start by creating a milestone to organize your project's work."
                    : "No work items have been created for this project yet."}
                </p>
                {canManage && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      setAddForm({ level: "milestone", parentId: projectId })
                    }
                  >
                    <Plus className="mr-1.5 h-3.5 w-3.5" />
                    Create First Milestone
                  </Button>
                )}
              </CardContent>
            </Card>
          )}

          {/* Hierarchy tree */}
          {milestones && milestones.length > 0 && (
            <Card>
              <CardContent className="py-3 px-3">
                <div className="space-y-0.5">
                  {milestones.map((milestone: any) => (
                    <MilestoneNode
                      key={milestone.id}
                      milestone={milestone}
                      isOwner={canManage}
                      addForm={addForm}
                      setAddForm={setAddForm}
                      projectId={projectId}
                      opportunities={projectOpportunities}
                      allTasks={allTasks}
                    />
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Legend */}
          {milestones && milestones.length > 0 && (
            <div className="flex flex-wrap gap-3 px-1 text-[10px] text-muted-foreground">
              {(
                [
                  "milestone",
                  "epic",
                  "sprint",
                  "feature",
                  "pbi",
                  "task",
                ] as HierarchyLevel[]
              ).map((level) => {
                const Icon = LEVEL_ICONS[level];
                return (
                  <span key={level} className="flex items-center gap-1">
                    <Icon className={cn("h-3 w-3", LEVEL_COLORS[level])} />
                    {level.charAt(0).toUpperCase() + level.slice(1)}
                  </span>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
