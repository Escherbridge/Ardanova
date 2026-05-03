"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Plus, X, Info, CheckSquare, Calendar, Tag } from "lucide-react";

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

const effortLabels: Record<string, string> = {
  XS: "XS (~1 hour)",
  S: "S (~2-4 hours)",
  M: "M (~1 day)",
  L: "L (~2-3 days)",
  XL: "XL (~1 week+)",
};

export default function CreateTaskPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialType = searchParams.get("type") === "pbi" ? "pbi" : "task";
  const [itemType, setItemType] = useState<"task" | "pbi">(initialType);
  const { data: projectsData } = api.project.getMyProjects.useQuery({ limit: 50, page: 1 });
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
  const { options: effortEstimates } = useEnumOptions("EffortEstimate", effortLabels);
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    projectId: "",
    type: "",
    priority: "MEDIUM",
    effort: "",
    dueDate: "",
    assignee: "",
    tags: [] as string[],
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (field: string, value: string) => {
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
    if (!formData.type) newErrors.type = `${itemType === "pbi" ? "PBI" : "Task"} type is required`;
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
          type: (formData.type || "FEATURE") as "FEATURE" | "ENHANCEMENT" | "BUG" | "TECHNICAL_DEBT" | "SPIKE",
          priority: (formData.priority || "MEDIUM") as "CRITICAL" | "HIGH" | "MEDIUM" | "LOW",
        });
      } else {
        await createTask.mutateAsync({
          projectId: formData.projectId,
          title: formData.title.trim(),
          description: formData.description.trim(),
          type: formData.type as "FEATURE" | "BUG" | "ENHANCEMENT" | "DOCUMENTATION" | "RESEARCH" | "DESIGN" | "TESTING" | "REVIEW" | "MAINTENANCE" | "OTHER",
          priority: formData.priority as "LOW" | "MEDIUM" | "HIGH" | "URGENT",
          effortEstimate: formData.effort ? formData.effort as "XS" | "S" | "M" | "L" | "XL" : undefined,
          dueDate: formData.dueDate || undefined,
        });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/tasks">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Tasks
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon/20 rounded-lg flex items-center justify-center">
              <CheckSquare className="h-5 w-5 text-neon" />
            </div>
            Create New {itemType === "pbi" ? "PBI" : "Task"}
          </h1>
          <p className="text-muted-foreground mt-2">
            Add a new work item to track progress
          </p>
          <div className="flex items-center gap-2 mt-3">
            <button
              type="button"
              onClick={() => { setItemType("task"); handleChange("type", ""); }}
              className={`px-4 py-2 rounded-lg border-2 text-sm font-medium transition-all ${
                itemType === "task"
                  ? "border-neon bg-neon/10 text-neon"
                  : "border-border hover:border-neon/50"
              }`}
            >
              <CheckSquare className="inline h-4 w-4 mr-1.5 -mt-0.5" />
              Task
            </button>
            <button
              type="button"
              onClick={() => { setItemType("pbi"); handleChange("type", ""); }}
              className={`px-4 py-2 rounded-lg border-2 text-sm font-medium transition-all ${
                itemType === "pbi"
                  ? "border-neon-purple bg-neon-purple/10 text-neon-purple"
                  : "border-border hover:border-neon-purple/50"
              }`}
            >
              <Tag className="inline h-4 w-4 mr-1.5 -mt-0.5" />
              PBI
            </button>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Task Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">{itemType === "pbi" ? "PBI" : "Task"} Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Title <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Implement user authentication flow"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p className="text-sm text-destructive mt-1">{errors.title}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Description <span className="text-neon">*</span>
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe the task in detail. Include acceptance criteria if applicable..."
                  rows={4}
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Project <span className="text-neon">*</span>
                </label>
                <Select
                  value={formData.projectId}
                  onValueChange={(value) => handleChange("projectId", value)}
                >
                  <SelectTrigger className={errors.projectId ? "border-destructive" : "border-border"}>
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
                  <p className="text-sm text-destructive mt-1">{errors.projectId}</p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Type <span className="text-neon">*</span>
                  </label>
                  <Select
                    value={formData.type}
                    onValueChange={(value) => handleChange("type", value)}
                  >
                    <SelectTrigger
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
                    <p className="text-sm text-destructive mt-1">{errors.type}</p>
                  )}
                </div>

                {itemType === "task" && (
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Effort Estimate
                    </label>
                    <Select
                      value={formData.effort}
                      onValueChange={(value) => handleChange("effort", value)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select effort" />
                      </SelectTrigger>
                      <SelectContent>
                        {effortEstimates.map((est) => (
                          <SelectItem key={est.id} value={est.id}>
                            {est.label}
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
              <CardTitle className="text-lg flex items-center gap-2">
                <Calendar className="h-5 w-5 text-neon-purple" />
                Priority & Scheduling
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Priority</label>
                <div className="grid grid-cols-4 gap-2">
                  {priorityLevels.map((level) => (
                    <button
                      key={level.id}
                      type="button"
                      onClick={() => handleChange("priority", level.id)}
                      className={`px-4 py-3 rounded-lg border-2 text-sm font-medium transition-all ${
                        formData.priority === level.id
                          ? "border-neon bg-neon/10 text-neon"
                          : "border-border hover:border-neon/50"
                      }`}
                    >
                      <div
                        className={`w-2 h-2 rounded-full ${priorityColors[level.id] ?? "bg-gray-500"} inline-block mr-2`}
                      />
                      {level.label}
                    </button>
                  ))}
                </div>
              </div>

              {itemType === "task" && (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Due Date
                    </label>
                    <input
                      type="date"
                      value={formData.dueDate}
                      onChange={(e) => handleChange("dueDate", e.target.value)}
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Assignee
                    </label>
                    <input
                      type="text"
                      value={formData.assignee}
                      onChange={(e) => handleChange("assignee", e.target.value)}
                      placeholder="@username or email"
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
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
                <CardTitle className="text-lg flex items-center gap-2">
                  <Tag className="h-5 w-5 text-neon-pink" />
                  Tags
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex gap-2">
                  <input
                    type="text"
                    value={newTag}
                    onChange={(e) => setNewTag(e.target.value)}
                    placeholder="Add a tag..."
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        e.preventDefault();
                        addTag();
                      }
                    }}
                    className="flex-1 px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    onClick={addTag}
                    className="px-4"
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
                        className="gap-1 cursor-pointer hover:bg-destructive/20"
                        onClick={() => removeTag(tag)}
                      >
                        {tag}
                        <X className="h-3 w-3" />
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
                <p className="text-sm text-destructive">{errors.submit}</p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex gap-4">
            <Button
              type="submit"
              className="flex-1 bg-neon hover:bg-neon/90 text-black font-semibold py-6"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Creating {itemType === "pbi" ? "PBI" : "Task"}...
                </>
              ) : (
                itemType === "pbi" ? "Create PBI" : "Create Task"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/tasks">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
            <Info className="h-5 w-5 text-neon mt-0.5" />
            <div className="text-sm text-muted-foreground">
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
