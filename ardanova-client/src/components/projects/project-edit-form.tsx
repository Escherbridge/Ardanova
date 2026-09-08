"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Loader2, Plus, X } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { useEnumOptions, type EnumOption } from "~/hooks/use-enum";
import {
  isProjectDuration,
  isProjectType,
  type ProjectDto,
  type ProjectDuration,
  type ProjectType,
} from "~/lib/contracts/project-contract";

const OTHER_CATEGORY_MAX_LENGTH = 50;

const projectTypeDescriptions: Record<ProjectType, string> = {
  TEMPORARY: "Short-term project with a defined end date",
  LONG_TERM: "Ongoing project without a strict end date",
  FOUNDATION: "Non-profit organization or foundation",
  BUSINESS: "Business venture or startup",
  PRODUCT: "A product people can help develop",
  OPEN_SOURCE: "Open source project or tool",
  COMMUNITY: "Community-driven initiative",
};

const durationLabels: Record<ProjectDuration, string> = {
  ONE_TWO_WEEKS: "1-2 weeks",
  ONE_THREE_MONTHS: "1-3 months",
  THREE_SIX_MONTHS: "3-6 months",
  SIX_TWELVE_MONTHS: "6-12 months",
  ONE_TWO_YEARS: "1-2 years",
  TWO_PLUS_YEARS: "2+ years",
  ONGOING: "Ongoing",
};

/**
 * The fields the create wizard's "Basic Info" step (currentStep === 0 in
 * ../../app/projects/create/page.tsx) collects, re-scoped for editing a
 * published project. Deliberately excludes resources/roles/milestones and the
 * sessionStorage-draft machinery - those belong to the create-only wizard.
 */
interface ProjectEditFormData {
  title: string;
  problemStatement: string;
  solution: string;
  categories: string[];
  otherCategory: string;
  projectType: ProjectType | "";
  duration: ProjectDuration | "";
  targetAudience: string;
  expectedImpact: string;
  timeline: string;
  tags: string[];
}

/**
 * Reverses the wizard's OTHER-category resolution performed on create
 * (handleSubmit in create/page.tsx maps a selected "OTHER" category to its
 * free-text value before the array reaches the API, so a stored project never
 * contains a literal "OTHER" entry). Any stored category that isn't a known
 * enum id is treated as that free text again here.
 */
function buildEditFormData(
  project: ProjectDto,
  knownCategoryIds: ReadonlySet<string>,
): ProjectEditFormData {
  const known = project.categories.filter((category) =>
    knownCategoryIds.has(category),
  );
  const unknown = project.categories.filter(
    (category) => !knownCategoryIds.has(category),
  );

  return {
    title: project.title,
    problemStatement: project.problemStatement,
    solution: project.solution,
    categories: unknown.length > 0 ? [...known, "OTHER"] : known,
    otherCategory: unknown.join(", ").slice(0, OTHER_CATEGORY_MAX_LENGTH),
    projectType: project.projectType,
    duration: project.duration ?? "",
    targetAudience: project.targetAudience ?? "",
    expectedImpact: project.expectedImpact ?? "",
    timeline: project.timeline ?? "",
    tags: project.tags
      ? project.tags
          .split(",")
          .map((tag) => tag.trim())
          .filter(Boolean)
      : [],
  };
}

interface ProjectEditFormProps {
  project: ProjectDto;
}

/** Loads the enum option lists the fields below need, then renders once they're ready. */
export function ProjectEditForm({ project }: ProjectEditFormProps) {
  const { options: categoryOptions, isLoading: categoriesLoading } =
    useEnumOptions("ProjectCategory");
  const { options: projectTypeOptions, isLoading: projectTypesLoading } =
    useEnumOptions("ProjectType");
  const { options: durationOptions, isLoading: durationsLoading } =
    useEnumOptions("ProjectDuration");

  if (categoriesLoading || projectTypesLoading || durationsLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center p-6">
        <div className="flex items-center gap-3" role="status">
          <Loader2 className="text-primary size-5 animate-spin" />
          <span className="font-mono text-sm">Loading project fields…</span>
        </div>
      </div>
    );
  }

  const projectTypes = projectTypeOptions.filter(
    (option): option is { id: ProjectType; label: string } =>
      isProjectType(option.id),
  );
  const durations = durationOptions
    .filter((option): option is { id: ProjectDuration; label: string } =>
      isProjectDuration(option.id),
    )
    .map((option) => ({ ...option, label: durationLabels[option.id] }));

  return (
    <ProjectEditFormFields
      project={project}
      categories={categoryOptions}
      projectTypes={projectTypes}
      durations={durations}
    />
  );
}

interface ProjectEditFormFieldsProps {
  project: ProjectDto;
  categories: EnumOption[];
  projectTypes: { id: ProjectType; label: string }[];
  durations: { id: ProjectDuration; label: string }[];
}

function ProjectEditFormFields({
  project,
  categories,
  projectTypes,
  durations,
}: ProjectEditFormFieldsProps) {
  const router = useRouter();
  const utils = api.useUtils();

  const [formData, setFormData] = useState<ProjectEditFormData>(() =>
    buildEditFormData(
      project,
      new Set(categories.map((option) => option.id)),
    ),
  );
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const updateMutation = api.project.update.useMutation({
    onSuccess: () => {
      toast.success("Project updated");
      void utils.project.getById.invalidate();
      router.push(`/projects/${project.slug}`);
    },
    onError: (err) => {
      toast.error(`Failed to update project: ${err.message}`);
    },
  });

  const handleChange = <Key extends keyof ProjectEditFormData>(
    field: Key,
    value: ProjectEditFormData[Key],
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next[field];
        return next;
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

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) newErrors.title = "Title is required";
    if (!formData.problemStatement.trim())
      newErrors.problemStatement = "Required";
    if (formData.problemStatement.length < 10)
      newErrors.problemStatement = "Min 10 characters";
    if (!formData.solution.trim()) newErrors.solution = "Required";
    if (formData.solution.length < 10)
      newErrors.solution = "Min 10 characters";
    if (formData.categories.length === 0)
      newErrors.categories = "Select at least one category";
    if (
      formData.categories.includes("OTHER") &&
      !formData.otherCategory.trim()
    ) {
      newErrors.otherCategory = "Please specify your category";
    }
    if (formData.otherCategory.length > OTHER_CATEGORY_MAX_LENGTH) {
      newErrors.otherCategory = `Max ${OTHER_CATEGORY_MAX_LENGTH} characters`;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!validate()) return;

    const resolvedCategories = formData.categories.map((category) =>
      category === "OTHER" ? formData.otherCategory.trim() : category,
    );

    void updateMutation.mutateAsync({
      id: project.id,
      title: formData.title,
      description: formData.solution,
      problemStatement: formData.problemStatement,
      solution: formData.solution,
      categories: resolvedCategories,
      projectType: formData.projectType || undefined,
      duration: formData.duration || undefined,
      targetAudience: formData.targetAudience || undefined,
      expectedImpact: formData.expectedImpact || undefined,
      timeline: formData.duration
        ? (durations.find((duration) => duration.id === formData.duration)
            ?.label ??
          (formData.timeline.trim() || undefined))
        : formData.timeline.trim() || undefined,
      tags: formData.tags.join(", ") || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Project Overview</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <label
              htmlFor="project-title"
              className="mb-2 block text-sm font-medium"
            >
              Project Title <span className="text-system">*</span>
            </label>
            <input
              type="text"
              id="project-title"
              required
              aria-invalid={Boolean(errors.title)}
              aria-describedby={
                errors.title ? "project-title-error" : undefined
              }
              value={formData.title}
              onChange={(e) => handleChange("title", e.target.value)}
              placeholder="e.g., Sustainable Water Filtration System"
              className={cn(
                "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                errors.title ? "border-destructive" : "border-border",
              )}
            />
            {errors.title && (
              <p
                id="project-title-error"
                role="alert"
                className="text-destructive mt-1 text-sm"
              >
                {errors.title}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-primary/10 flex h-8 w-8 items-center justify-center rounded-lg">
              <span className="text-primary font-bold">?</span>
            </div>
            Problem Definition
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <label
              htmlFor="project-problem"
              className="mb-2 block text-sm font-medium"
            >
              Problem Statement <span className="text-system">*</span>
            </label>
            <textarea
              id="project-problem"
              required
              aria-invalid={Boolean(errors.problemStatement)}
              aria-describedby={
                errors.problemStatement ? "project-problem-error" : undefined
              }
              value={formData.problemStatement}
              onChange={(e) =>
                handleChange("problemStatement", e.target.value)
              }
              placeholder="Describe the problem you're solving. What is the issue? Who does it affect?"
              rows={4}
              className={cn(
                "bg-muted/50 focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                errors.problemStatement
                  ? "border-destructive"
                  : "border-border",
              )}
            />
            {errors.problemStatement && (
              <p
                id="project-problem-error"
                role="alert"
                className="text-destructive mt-1 text-sm"
              >
                {errors.problemStatement}
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="project-audience"
              className="mb-2 block text-sm font-medium"
            >
              Target Audience
            </label>
            <input
              type="text"
              id="project-audience"
              value={formData.targetAudience}
              onChange={(e) => handleChange("targetAudience", e.target.value)}
              placeholder="Who are the primary beneficiaries?"
              className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
            />
          </div>

          <div>
            <label
              htmlFor="project-impact"
              className="mb-2 block text-sm font-medium"
            >
              Expected Impact
            </label>
            <textarea
              id="project-impact"
              value={formData.expectedImpact}
              onChange={(e) => handleChange("expectedImpact", e.target.value)}
              placeholder="What impact do you expect to achieve?"
              rows={3}
              className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
            />
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-lg">
            <div className="bg-success/10 flex h-8 w-8 items-center justify-center rounded-lg">
              <span className="text-success font-bold">!</span>
            </div>
            Solution Definition
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <label
              htmlFor="project-solution"
              className="mb-2 block text-sm font-medium"
            >
              Proposed Solution <span className="text-system">*</span>
            </label>
            <textarea
              id="project-solution"
              required
              aria-invalid={Boolean(errors.solution)}
              aria-describedby={
                errors.solution ? "project-solution-error" : undefined
              }
              value={formData.solution}
              onChange={(e) => handleChange("solution", e.target.value)}
              placeholder="Describe your proposed solution. How does it address the problem?"
              rows={4}
              className={cn(
                "bg-muted/50 focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                errors.solution ? "border-destructive" : "border-border",
              )}
            />
            {errors.solution && (
              <p
                id="project-solution-error"
                role="alert"
                className="text-destructive mt-1 text-sm"
              >
                {errors.solution}
              </p>
            )}
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Project Type</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <fieldset>
            <legend className="mb-2 block text-sm font-medium">
              What kind of project is this?
            </legend>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {projectTypes.map((pt) => {
                const isSelected = formData.projectType === pt.id;
                return (
                  <button
                    key={pt.id}
                    type="button"
                    onClick={() => handleChange("projectType", pt.id)}
                    aria-pressed={isSelected}
                    className={cn(
                      "flex min-h-11 flex-col items-start rounded-lg border-2 p-3 text-left transition-colors",
                      isSelected
                        ? "border-system bg-system/10"
                        : "border-border bg-muted/30 hover:border-system",
                    )}
                  >
                    <span
                      className={cn(
                        "text-sm font-medium",
                        isSelected ? "text-system" : "text-foreground",
                      )}
                    >
                      {pt.label}
                    </span>
                    <span className="text-muted-foreground mt-0.5 text-xs">
                      {projectTypeDescriptions[pt.id] ?? ""}
                    </span>
                  </button>
                );
              })}
            </div>
          </fieldset>

          <div>
            <label
              htmlFor="project-duration"
              className="mb-2 block text-sm font-medium"
            >
              Expected Duration
            </label>
            <Select
              value={formData.duration}
              onValueChange={(value) => {
                if (isProjectDuration(value)) {
                  handleChange("duration", value);
                }
              }}
            >
              <SelectTrigger id="project-duration">
                <SelectValue placeholder="Select duration" />
              </SelectTrigger>
              <SelectContent>
                {durations.map((dur) => (
                  <SelectItem key={dur.id} value={dur.id}>
                    {dur.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Project Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <fieldset>
            <legend className="mb-2 block text-sm font-medium">
              Categories <span className="text-system">*</span>
            </legend>
            <div
              aria-invalid={Boolean(errors.categories)}
              aria-describedby={
                errors.categories ? "project-categories-error" : undefined
              }
              className={cn(
                "flex flex-wrap gap-2 rounded-lg border-2 p-3",
                errors.categories ? "border-destructive" : "border-border",
              )}
            >
              {categories.map((cat) => {
                const isSelected = formData.categories.includes(cat.id);
                return (
                  <button
                    key={cat.id}
                    type="button"
                    aria-pressed={isSelected}
                    onClick={() => {
                      setFormData((prev) => ({
                        ...prev,
                        categories: isSelected
                          ? prev.categories.filter((c) => c !== cat.id)
                          : [...prev.categories, cat.id],
                        otherCategory:
                          cat.id === "OTHER" && isSelected
                            ? ""
                            : prev.otherCategory,
                      }));
                      if (errors.categories) {
                        setErrors((prev) => {
                          const next = { ...prev };
                          delete next.categories;
                          return next;
                        });
                      }
                    }}
                    className={cn(
                      "min-h-11 border px-3 py-1.5 text-sm font-medium transition-colors",
                      isSelected
                        ? "bg-system text-system-foreground border-system"
                        : "bg-muted/50 text-muted-foreground border-border hover:border-system hover:text-foreground",
                    )}
                  >
                    {cat.label}
                  </button>
                );
              })}
            </div>
            {errors.categories && (
              <p
                id="project-categories-error"
                role="alert"
                className="text-destructive mt-1 text-sm"
              >
                {errors.categories}
              </p>
            )}
            {formData.categories.includes("OTHER") && (
              <div className="mt-3">
                <label
                  htmlFor="project-other-category"
                  className="mb-1 block text-sm font-medium"
                >
                  Specify other category
                </label>
                <div className="relative">
                  <input
                    type="text"
                    id="project-other-category"
                    required
                    aria-invalid={Boolean(errors.otherCategory)}
                    aria-describedby={
                      errors.otherCategory
                        ? "project-other-category-error"
                        : "project-other-category-count"
                    }
                    value={formData.otherCategory}
                    onChange={(e) => {
                      const value = e.target.value.slice(
                        0,
                        OTHER_CATEGORY_MAX_LENGTH,
                      );
                      setFormData((prev) => ({
                        ...prev,
                        otherCategory: value,
                      }));
                      if (errors.otherCategory) {
                        setErrors((prev) => {
                          const next = { ...prev };
                          delete next.otherCategory;
                          return next;
                        });
                      }
                    }}
                    maxLength={OTHER_CATEGORY_MAX_LENGTH}
                    placeholder="e.g., Renewable Energy"
                    className={cn(
                      "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none",
                      errors.otherCategory
                        ? "border-destructive"
                        : "border-border",
                    )}
                  />
                  <span
                    id="project-other-category-count"
                    className="text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2 text-xs"
                  >
                    {formData.otherCategory.length}/{OTHER_CATEGORY_MAX_LENGTH}
                  </span>
                </div>
                {errors.otherCategory && (
                  <p
                    id="project-other-category-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.otherCategory}
                  </p>
                )}
              </div>
            )}
          </fieldset>
        </CardContent>
      </Card>

      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Tags</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <label htmlFor="project-tag" className="sr-only">
            Add a project tag
          </label>
          <div className="flex gap-2">
            <input
              type="text"
              id="project-tag"
              value={newTag}
              onChange={(e) => setNewTag(e.target.value)}
              placeholder="Add a tag..."
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addTag();
                }
              }}
              className="bg-muted/50 border-border focus-visible:ring-ring/50 flex-1 rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
            />
            <Button
              type="button"
              variant="outline"
              onClick={addTag}
              className="min-h-11 min-w-11 px-4"
              aria-label="Add tag"
            >
              <Plus className="size-4" />
            </Button>
          </div>
          {formData.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {formData.tags.map((tag) => (
                <Badge key={tag} variant="secondary" className="gap-1 pr-0 pl-3">
                  {tag}
                  <button
                    type="button"
                    onClick={() => removeTag(tag)}
                    aria-label={`Remove ${tag} tag`}
                    className="focus-visible:ring-ring hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
                  >
                    <X className="size-3" aria-hidden="true" />
                  </button>
                </Badge>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="flex flex-col-reverse gap-3 pb-4 sm:flex-row sm:items-center sm:justify-end">
        <Button variant="outline" className="min-h-11 w-full sm:w-auto" asChild>
          <Link href={`/projects/${project.slug}`}>Cancel</Link>
        </Button>
        <Button
          type="submit"
          className="min-h-11 w-full sm:w-auto"
          disabled={updateMutation.isPending}
        >
          {updateMutation.isPending ? (
            <>
              <Loader2 className="size-4 animate-spin" />
              Saving…
            </>
          ) : (
            "Save changes"
          )}
        </Button>
      </div>
    </form>
  );
}
