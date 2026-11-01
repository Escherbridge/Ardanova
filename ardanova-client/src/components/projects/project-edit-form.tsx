"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import { useState, type KeyboardEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Loader2, X } from "lucide-react";
import { toast } from "sonner";

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
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { useEnumOptions } from "~/hooks/use-enum";
import { useTaxonomy } from "~/hooks/use-taxonomy";
import {
  canonicalHashtag,
  hashtagsIn,
  MAX_HASHTAGS_PER_POST,
} from "~/components/hashtags/hashtag-tokens";
import {
  isProjectDuration,
  type ProjectDto,
  type ProjectDuration,
} from "~/lib/contracts/project-contract";

import { projectHashtagFieldOf } from "./project-page-model";

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
export interface ProjectEditFormData {
  title: string;
  problemStatement: string;
  solution: string;
  /** Canonical tags without '#': everything a `hashtags` write replaces. See ./AGENTS.md §Edit form. */
  hashtags: string[];
  duration: ProjectDuration | "";
  targetAudience: string;
  expectedImpact: string;
  timeline: string;
}

export function buildEditFormData(project: ProjectDto): ProjectEditFormData {
  return {
    title: project.title,
    problemStatement: project.problemStatement,
    solution: project.solution,
    hashtags: projectHashtagFieldOf(project).editable,
    duration: project.duration ?? "",
    targetAudience: project.targetAudience ?? "",
    expectedImpact: project.expectedImpact ?? "",
    timeline: project.timeline ?? "",
  };
}

function sameSequence(left: readonly string[], right: readonly string[]) {
  return (
    left.length === right.length &&
    left.every((value, index) => value === right[index])
  );
}

interface ProjectEditFormProps {
  project: ProjectDto;
}

/** Loads the enum option lists the fields below need, then renders once they're ready. */
export function ProjectEditForm({ project }: ProjectEditFormProps) {
  const { options: durationOptions, isLoading: durationsLoading } =
    useEnumOptions("ProjectDuration");

  if (durationsLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center p-6">
        <div className="flex items-center gap-3" role="status">
          <Loader2 className="text-primary size-5 animate-spin" />
          <span className="font-mono text-sm">Loading project fields…</span>
        </div>
      </div>
    );
  }

  const durations = durationOptions
    .filter((option): option is { id: ProjectDuration; label: string } =>
      isProjectDuration(option.id),
    )
    .map((option) => ({ ...option, label: durationLabels[option.id] }));

  return <ProjectEditFormFields project={project} durations={durations} />;
}

interface ProjectEditFormFieldsProps {
  project: ProjectDto;
  durations: { id: ProjectDuration; label: string }[];
}

function ProjectEditFormFields({
  project,
  durations,
}: ProjectEditFormFieldsProps) {
  const router = useRouter();
  const utils = api.useUtils();

  const [initialData] = useState(() => buildEditFormData(project));
  const [formData, setFormData] = useState<ProjectEditFormData>(initialData);
  // Legacy text no hashtag can spell: shown, never sent; the API carries it over on a hashtags write.
  const [keptTags] = useState(() => projectHashtagFieldOf(project).kept);
  const hashtagsChanged = !sameSequence(
    formData.hashtags,
    initialData.hashtags,
  );
  const [hashtagInput, setHashtagInput] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const updateMutation = api.project.update.useMutation({
    onSuccess: () => {
      toast.success("Project updated");
      void utils.project.getById.invalidate();
      router.push(`/projects/${project.slug}`);
    },
    onError: (err) => {
      toast.error(describeMutationError(err, "update the project"));
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

  // Discovery hashtags (SR-03 Phase 3 §3): the one field that replaces the old
  // category checkboxes, "Other" category and project-type picker. Parsing
  // reuses the composer's own tag primitive (components/hashtags/hashtag-tokens.ts)
  // so "#carpentry" here means exactly what it means in a feed post.
  const { terms: taxonomyTerms } = useTaxonomy();
  const hashtagSuggestions = (() => {
    const query = hashtagInput.trim().replace(/^#/, "").toLowerCase();
    if (!query) return [];
    return taxonomyTerms
      .filter((term) => term.kind === "SECTOR" || term.kind === "PROJECT_TYPE")
      .flatMap((term) => {
        const tag = canonicalHashtag(term.slug);
        return tag ? [{ tag, label: term.label }] : [];
      })
      .filter(({ tag }) => !formData.hashtags.includes(tag))
      .filter(
        ({ tag, label }) =>
          tag.includes(query) || label.toLowerCase().includes(query),
      )
      .slice(0, 6);
  })();

  const addHashtagTag = (tag: string) => {
    const canonical = canonicalHashtag(tag);
    if (!canonical) return;
    setFormData((prev) =>
      prev.hashtags.includes(canonical) ||
      prev.hashtags.length >= MAX_HASHTAGS_PER_POST
        ? prev
        : { ...prev, hashtags: [...prev.hashtags, canonical] },
    );
    setHashtagInput("");
  };

  const removeHashtag = (tag: string) => {
    handleChange(
      "hashtags",
      formData.hashtags.filter((t) => t !== tag),
    );
  };

  /** Commits every complete #tag in the box at once, so pasting "#a #b" adds both. */
  const commitHashtagInput = () => {
    const raw = hashtagInput.trim();
    if (!raw) return;
    const extracted = [...hashtagsIn(raw)];
    if (extracted.length === 0) {
      const tag = canonicalHashtag(raw);
      if (tag) extracted.push(tag);
    }
    if (extracted.length === 0) {
      setHashtagInput("");
      return;
    }
    setFormData((prev) => {
      const next = [...prev.hashtags];
      for (const tag of extracted) {
        if (next.length >= MAX_HASHTAGS_PER_POST) break;
        if (!next.includes(tag)) next.push(tag);
      }
      return { ...prev, hashtags: next };
    });
    setHashtagInput("");
  };

  const handleHashtagInputKeyDown = (
    event: KeyboardEvent<HTMLInputElement>,
  ) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      commitHashtagInput();
    }
  };

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) newErrors.title = "Title is required";
    if (!formData.problemStatement.trim())
      newErrors.problemStatement = "Required";
    if (formData.problemStatement.length < 10)
      newErrors.problemStatement = "Min 10 characters";
    if (!formData.solution.trim()) newErrors.solution = "Required";
    if (formData.solution.length < 10) newErrors.solution = "Min 10 characters";
    // An older project can hold more than the cap; the API would drop the extras silently.
    if (hashtagsChanged && formData.hashtags.length > MAX_HASHTAGS_PER_POST)
      newErrors.hashtags = `Keep at most ${MAX_HASHTAGS_PER_POST} hashtags.`;

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!validate()) return;

    updateMutation.mutate({
      id: project.id,
      title: formData.title,
      description: formData.solution,
      problemStatement: formData.problemStatement,
      solution: formData.solution,
      // A full replacement of categories/projectType/tags, so sent only when edited: ./AGENTS.md §Edit form.
      ...(hashtagsChanged ? { hashtags: formData.hashtags } : {}),
      duration: formData.duration || undefined,
      targetAudience: formData.targetAudience || undefined,
      expectedImpact: formData.expectedImpact || undefined,
      timeline: formData.duration
        ? (durations.find((duration) => duration.id === formData.duration)
            ?.label ??
          (formData.timeline.trim() || undefined))
        : formData.timeline.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Overview</CardTitle>
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
              maxLength={200}
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
            Problem
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
              maxLength={20_000}
              aria-invalid={Boolean(errors.problemStatement)}
              aria-describedby={
                errors.problemStatement ? "project-problem-error" : undefined
              }
              value={formData.problemStatement}
              onChange={(e) => handleChange("problemStatement", e.target.value)}
              placeholder="What is wrong, and who does it affect?"
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
              maxLength={20_000}
              value={formData.targetAudience}
              onChange={(e) => handleChange("targetAudience", e.target.value)}
              placeholder="Who benefits?"
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
              maxLength={20_000}
              value={formData.expectedImpact}
              onChange={(e) => handleChange("expectedImpact", e.target.value)}
              placeholder="What changes if this works?"
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
            Solution
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
              maxLength={20_000}
              aria-invalid={Boolean(errors.solution)}
              aria-describedby={
                errors.solution ? "project-solution-error" : undefined
              }
              value={formData.solution}
              onChange={(e) => handleChange("solution", e.target.value)}
              placeholder="How does this solve it?"
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
          <CardTitle className="text-lg">Duration</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
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
          <CardTitle className="text-lg">Hashtags</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <label htmlFor="project-hashtags" className="sr-only">
            Add hashtags
          </label>
          <div className="relative">
            <input
              type="text"
              id="project-hashtags"
              value={hashtagInput}
              onChange={(e) => setHashtagInput(e.target.value)}
              onKeyDown={handleHashtagInputKeyDown}
              disabled={formData.hashtags.length >= MAX_HASHTAGS_PER_POST}
              maxLength={200}
              placeholder="#construction #barn-raising"
              className="bg-muted/50 border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
            />
            {hashtagSuggestions.length > 0 && (
              <ul className="bg-card border-border absolute z-10 mt-1 w-full border-2 shadow-lg">
                {hashtagSuggestions.map((suggestion) => (
                  <li key={suggestion.tag}>
                    <button
                      type="button"
                      onClick={() => addHashtagTag(suggestion.tag)}
                      className="hover:bg-muted/50 flex min-h-11 w-full items-center justify-between px-4 py-2 text-left text-sm"
                    >
                      <span className="font-medium">#{suggestion.tag}</span>
                      <span className="text-muted-foreground text-xs">
                        {suggestion.label}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
          {(formData.hashtags.length > 0 || keptTags.length > 0) && (
            <div className="flex flex-wrap gap-2">
              {formData.hashtags.map((tag) => (
                <Badge
                  key={tag}
                  variant="secondary"
                  className="gap-1 pr-0 pl-3"
                >
                  #{tag}
                  <button
                    type="button"
                    onClick={() => removeHashtag(tag)}
                    aria-label={`Remove #${tag} hashtag`}
                    className="focus-visible:ring-ring hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
                  >
                    <X className="size-3" aria-hidden="true" />
                  </button>
                </Badge>
              ))}
              {keptTags.map((text) => (
                <Badge
                  key={`kept:${text}`}
                  variant="outline"
                  className="min-h-11 px-3"
                >
                  {text}
                </Badge>
              ))}
            </div>
          )}
          {errors.hashtags && (
            <p role="alert" className="text-destructive text-sm">
              {errors.hashtags}
            </p>
          )}
          <p className="text-muted-foreground text-xs">
            {formData.hashtags.length}/{MAX_HASHTAGS_PER_POST} · #construction
            sorts by sector.
            {keptTags.length > 0
              ? " Older labels without a # stay as written."
              : null}
          </p>
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
