"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
  Plus,
  X,
  Info,
  CheckSquare,
  Calendar,
  Tag,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { useEnumOptions } from "~/hooks/use-enum";
import { api } from "~/trpc/react";
import { toast } from "sonner";

// Priority color indicators (matches DBML TaskPriority enum)
const priorityColors: Record<string, string> = {
  LOW: "bg-blue-500",
  MEDIUM: "bg-yellow-500",
  HIGH: "bg-orange-500",
  URGENT: "bg-red-500",
};

// Label overrides
const taskTypeLabels: Record<string, string> = {
  BUG: "Bug Fix",
  ENHANCEMENT: "Enhancement",
  DOCUMENTATION: "Documentation",
  TESTING: "Testing",
  REVIEW: "Code Review",
  MAINTENANCE: "Maintenance",
};

const pbiTypeLabels: Record<string, string> = {
  FEATURE: "Feature",
  ENHANCEMENT: "Enhancement",
  BUG: "Bug",
  TECHNICAL_DEBT: "Technical Debt",
  SPIKE: "Spike",
};

const estimatedHourOptions = [1, 3, 8, 20, 40] as const;

interface WorkItemFormData {
  title: string;
  description: string;
  projectId: string;
  type: string;
  priority: string;
  estimatedHours: string;
  dueDate: string;
  assignee: string;
  tags: string[];
}

export default function CreateTaskPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialType = searchParams.get("type") === "pbi" ? "pbi" : "task";
  const [itemType, setItemType] = useState<"task" | "pbi">(initialType);
  const { data: projectsData } = api.project.getMyProjects.useQuery({
    limit: 50,
    page: 1,
  });
  const createTask = api.task.create.useMutation({
    onSuccess: () => {
      toast.success("Task created");
      router.push("/tasks");
    },
    onError: (e) => toast.error(e.message),
  });
  const createPbi = api.backlog.createPbi.useMutation({
    onSuccess: () => {
      toast.success("PBI created");
      router.push("/tasks");
    },
    onError: (e) => toast.error(e.message),
  });
  const { options: priorityLevels } = useEnumOptions("TaskPriority");
  const { options: taskTypes } = useEnumOptions("TaskType", taskTypeLabels);
  const { options: pbiTypes } = useEnumOptions("PBIType", pbiTypeLabels);
  const [formData, setFormData] = useState<WorkItemFormData>({
    title: "",
    description: "",
    projectId: "",
    type: "",
    priority: "MEDIUM",
    estimatedHours: "",
    dueDate: "",
    assignee: "",
    tags: [],
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = <Key extends keyof WorkItemFormData>(
    field: Key,
    value: WorkItemFormData[Key],
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const addTag = () => {
    if (newTag.trim() && !formData.tags.includes(newTag.trim())) {
      setFormData((prev) => ({
        ...prev,
        tags: [...prev.tags, newTag.trim()],
      }));
      setNewTag("");
    }
  };

  const removeTag = (tag: string) => {
    setFormData((prev) => ({
      ...prev,
      tags: prev.tags.filter((t) => t !== tag),
    }));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.title.trim()) newErrors.title = "Title is required";
    const trimmedDesc = formData.description.trim();
    if (!trimmedDesc) {
      newErrors.description = "Description is required";
    } else if (trimmedDesc.length < 10) {
      newErrors.description = "Must be at least 10 characters";
    }
    if (!formData.type)
      newErrors.type = `${itemType === "pbi" ? "PBI" : "Task"} type is required`;
    if (!formData.projectId) newErrors.projectId = "Project is required";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Type options depend on item type
  const typeOptions = itemType === "pbi" ? pbiTypes : taskTypes;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      if (itemType === "pbi") {
        await createPbi.mutateAsync({
          projectId: formData.projectId,
          title: formData.title.trim(),
          description: formData.description.trim(),
          type: (formData.type || "FEATURE") as
            | "FEATURE"
            | "ENHANCEMENT"
            | "BUG"
            | "TECHNICAL_DEBT"
            | "SPIKE",
          priority: (formData.priority || "MEDIUM") as
            | "CRITICAL"
            | "HIGH"
            | "MEDIUM"
            | "LOW",
        });
      } else {
        await createTask.mutateAsync({
          projectId: formData.projectId,
          title: formData.title.trim(),
          description: formData.description.trim(),
          type: formData.type as
            | "FEATURE"
            | "BUG"
            | "ENHANCEMENT"
            | "DOCUMENTATION"
            | "RESEARCH"
            | "DESIGN"
            | "TESTING"
            | "REVIEW"
            | "MAINTENANCE"
            | "OTHER",
          priority: formData.priority as "LOW" | "MEDIUM" | "HIGH" | "URGENT",
          estimatedHours: formData.estimatedHours
            ? Number(formData.estimatedHours)
            : undefined,
          dueDate: formData.dueDate || undefined,
        });
      }
    } catch (error) {
      setErrors({
        submit:
          error instanceof Error
            ? error.message
            : "Work item could not be created",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/tasks">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Tasks
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-system/20 flex h-10 w-10 items-center justify-center rounded-none">
              <CheckSquare className="text-system h-5 w-5" />
            </div>
            Create New {itemType === "pbi" ? "PBI" : "Task"}
          </h1>
          <p className="text-muted-foreground mt-2">
            Add a new work item to track progress
          </p>
          <div
            className="mt-3 flex flex-wrap items-center gap-2"
            role="group"
            aria-label="Work item type"
          >
            <button
              type="button"
              onClick={() => {
                setItemType("task");
                handleChange("type", "");
              }}
              aria-pressed={itemType === "task"}
              className={`min-h-11 rounded-none border-2 px-4 py-2 text-sm font-medium transition-all ${
                itemType === "task"
                  ? "border-system bg-system/10 text-system"
                  : "border-border hover:border-system/50"
              }`}
            >
              <CheckSquare className="-mt-0.5 mr-1.5 inline h-4 w-4" />
              Task
            </button>
            <button
              type="button"
              onClick={() => {
                setItemType("pbi");
                handleChange("type", "");
              }}
              aria-pressed={itemType === "pbi"}
              className={`min-h-11 rounded-none border-2 px-4 py-2 text-sm font-medium transition-all ${
                itemType === "pbi"
                  ? "border-system bg-system/10 text-system"
                  : "border-border hover:border-system-purple/50"
              }`}
            >
              <Tag className="-mt-0.5 mr-1.5 inline h-4 w-4" />
              PBI
            </button>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Task Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">
                {itemType === "pbi" ? "PBI" : "Task"} Details
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="task-title"
                  className="mb-2 block text-sm font-medium"
                >
                  Title <span className="text-system">*</span>
                </label>
                <input
                  type="text"
                  id="task-title"
                  required
                  aria-invalid={Boolean(errors.title)}
                  aria-describedby={
                    errors.title ? "task-title-error" : undefined
                  }
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Implement user authentication flow"
                  className={`bg-muted/50 focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p
                    id="task-title-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.title}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="task-description"
                  className="mb-2 block text-sm font-medium"
                >
                  Description <span className="text-system">*</span>
                </label>
                <textarea
                  id="task-description"
                  required
                  aria-invalid={Boolean(errors.description)}
                  aria-describedby={
                    errors.description ? "task-description-error" : undefined
                  }
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe the task in detail. Include acceptance criteria if applicable..."
                  rows={4}
                  className={`bg-muted/50 focus-visible:ring-ring/50 w-full resize-none rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p
                    id="task-description-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="task-project"
                  className="mb-2 block text-sm font-medium"
                >
                  Project <span className="text-system">*</span>
                </label>
                <Select
                  value={formData.projectId}
                  onValueChange={(value) => handleChange("projectId", value)}
                >
                  <SelectTrigger
                    id="task-project"
                    aria-required="true"
                    aria-invalid={Boolean(errors.projectId)}
                    aria-describedby={
                      errors.projectId ? "task-project-error" : undefined
                    }
                    className={
                      errors.projectId ? "border-destructive" : "border-border"
                    }
                  >
                    <SelectValue placeholder="Select a project" />
                  </SelectTrigger>
                  <SelectContent>
                    {(projectsData?.items ?? []).map((p) => (
                      <SelectItem key={p.id} value={p.id}>
                        {p.title}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.projectId && (
                  <p
                    id="task-project-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.projectId}
                  </p>
                )}
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="task-type"
                    className="mb-2 block text-sm font-medium"
                  >
                    Type <span className="text-system">*</span>
                  </label>
                  <Select
                    value={formData.type}
                    onValueChange={(value) => handleChange("type", value)}
                  >
                    <SelectTrigger
                      id="task-type"
                      aria-required="true"
                      aria-invalid={Boolean(errors.type)}
                      aria-describedby={
                        errors.type ? "task-type-error" : undefined
                      }
                      className={
                        errors.type ? "border-destructive" : "border-border"
                      }
                    >
                      <SelectValue placeholder="Select type" />
                    </SelectTrigger>
                    <SelectContent>
                      {typeOptions.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {errors.type && (
                    <p
                      id="task-type-error"
                      role="alert"
                      className="text-destructive mt-1 text-sm"
                    >
                      {errors.type}
                    </p>
                  )}
                </div>

                {itemType === "task" && (
                  <div>
                    <label
                      htmlFor="task-estimated-hours"
                      className="mb-2 block text-sm font-medium"
                    >
                      Estimated hours
                    </label>
                    <Select
                      value={formData.estimatedHours}
                      onValueChange={(value) =>
                        handleChange("estimatedHours", value)
                      }
                    >
                      <SelectTrigger id="task-estimated-hours">
                        <SelectValue placeholder="Select stored hour estimate" />
                      </SelectTrigger>
                      <SelectContent>
                        {estimatedHourOptions.map((hours) => (
                          <SelectItem key={hours} value={String(hours)}>
                            {hours} {hours === 1 ? "hour" : "hours"}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Priority & Scheduling */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-lg">
                <Calendar className="text-system h-5 w-5" />
                Priority & Scheduling
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <fieldset>
                <legend className="mb-2 block text-sm font-medium">
                  Priority
                </legend>
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  {priorityLevels.map((level) => (
                    <button
                      key={level.id}
                      type="button"
                      onClick={() => handleChange("priority", level.id)}
                      aria-pressed={formData.priority === level.id}
                      className={`min-h-11 rounded-none border-2 px-4 py-3 text-sm font-medium transition-all ${
                        formData.priority === level.id
                          ? "border-system bg-system/10 text-system"
                          : "border-border hover:border-system/50"
                      }`}
                    >
                      <div
                        className={`h-2 w-2 rounded-none ${priorityColors[level.id] ?? "bg-gray-500"} mr-2 inline-block`}
                      />
                      {level.label}
                    </button>
                  ))}
                </div>
              </fieldset>

              {itemType === "task" && (
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div>
                    <label
                      htmlFor="task-due-date"
                      className="mb-2 block text-sm font-medium"
                    >
                      Due Date
                    </label>
                    <input
                      type="date"
                      id="task-due-date"
                      value={formData.dueDate}
                      onChange={(e) => handleChange("dueDate", e.target.value)}
                      className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="task-assignee"
                      className="mb-2 block text-sm font-medium"
                    >
                      Assignee
                    </label>
                    <input
                      type="text"
                      id="task-assignee"
                      value={formData.assignee}
                      onChange={(e) => handleChange("assignee", e.target.value)}
                      placeholder="@username or email"
                      className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                    />
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Tags (tasks only) */}
          {itemType === "task" && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Tag className="text-primary h-5 w-5" />
                  Tags
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <label htmlFor="task-tag" className="sr-only">
                  Add a task tag
                </label>
                <div className="flex gap-2">
                  <input
                    type="text"
                    id="task-tag"
                    value={newTag}
                    onChange={(e) => setNewTag(e.target.value)}
                    placeholder="Add a tag..."
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        e.preventDefault();
                        addTag();
                      }
                    }}
                    className="bg-muted/50 border-border focus-visible:ring-ring/50 flex-1 rounded-none border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    onClick={addTag}
                    className="min-h-11 min-w-11 px-4"
                    aria-label="Add tag"
                  >
                    <Plus className="h-4 w-4" />
                  </Button>
                </div>
                {formData.tags.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {formData.tags.map((tag) => (
                      <Badge
                        key={tag}
                        variant="secondary"
                        className="gap-1 pr-0 pl-3"
                      >
                        {tag}
                        <button
                          type="button"
                          onClick={() => removeTag(tag)}
                          aria-label={`Remove ${tag} tag`}
                          className="focus-visible:ring-ring hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
                        >
                          <X className="h-3 w-3" aria-hidden="true" />
                        </button>
                      </Badge>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Submit Error */}
          {errors.submit && (
            <Card className="bg-destructive/10 border-destructive">
              <CardContent className="py-4">
                <p role="alert" className="text-destructive text-sm">
                  {errors.submit}
                </p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex flex-col gap-3 sm:flex-row sm:gap-4">
            <Button
              type="submit"
              className="bg-system text-system-foreground hover:bg-system/90 flex-1 py-6 font-semibold"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating {itemType === "pbi" ? "PBI" : "Task"}...
                </>
              ) : itemType === "pbi" ? (
                "Create PBI"
              ) : (
                "Create Task"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/tasks">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="bg-muted/30 border-border flex items-start gap-3 rounded-none border p-4">
            <Info className="text-system mt-0.5 h-5 w-5" />
            <div className="text-muted-foreground text-sm">
              <p>
                {itemType === "pbi"
                  ? "PBIs represent user stories or work items in your product backlog. They can be broken down into tasks."
                  : "Tasks will appear in your backlog. You can move them to different stages as you work on them."}
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
