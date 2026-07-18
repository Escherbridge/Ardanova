"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
  Plus,
  X,
  Info,
  Briefcase,
  DollarSign,
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
import EntitySelector from "~/components/opportunities/entity-selector";

// Label overrides for opportunity types
const jobTypeLabels: Record<string, string> = {
  GUILD_POSITION: "Guild Position",
  PROJECT_ROLE: "Project Role",
  TASK_BOUNTY: "Task Bounty",
};

// Label overrides for compensation types
const compensationLabels: Record<string, string> = {
  FIXED_SHARES: "Fixed Shares",
  EQUITY_PERCENT: "Project-token allocation percentage",
};

// Label overrides for experience levels
const experienceLabels: Record<string, string> = {
  ENTRY: "Entry Level",
};

// Hidden compensation models
const HIDDEN_COMPENSATION_MODELS = ["HOURLY_SHARES"];

interface OpportunityData {
  id: string;
  title: string;
  slug: string;
  description: string;
  type: string;
  status: string;
  experienceLevel: string;
  skills?: string;
  compensation?: number;
  compensationDetails?: string;
  location?: string;
  isRemote: boolean;
  deadline?: string;
  projectId?: string;
  guildId?: string;
  projectRole?: string;
}

interface OpportunityFormProps {
  mode: "create" | "edit";
  opportunity?: OpportunityData;
  entityType?: "project" | "guild";
  entityId?: string;
  entitySlug?: string;
  defaultProjectRole?: string;
}

interface OpportunityFormState {
  title: string;
  description: string;
  type: string;
  skills: string[];
  compensationModel: string;
  compensationAmount: string;
  location: string;
  experienceLevel: string;
  deadline: string;
  projectRole: string;
}

export function OpportunityForm({
  mode,
  opportunity,
  entityType,
  entityId,
  entitySlug,
  defaultProjectRole,
}: OpportunityFormProps) {
  const router = useRouter();
  const { options: jobTypes } = useEnumOptions(
    "OpportunityType",
    jobTypeLabels,
  );
  const { options: compensationTypes } = useEnumOptions(
    "CompensationModel",
    compensationLabels,
  );
  const { options: experienceLevels } = useEnumOptions(
    "ExperienceLevel",
    experienceLabels,
  );

  // Filter out hidden compensation models
  const filteredCompensationTypes = compensationTypes.filter(
    (type) => !HIDDEN_COMPENSATION_MODELS.includes(type.id),
  );

  const [entitySelection, setEntitySelection] = useState<{
    type: "project" | "guild";
    id: string;
    slug: string;
  } | null>(null);

  // Derive entity context from explicit route context, selection, or edit data.
  const resolvedEntityType =
    entityType ??
    entitySelection?.type ??
    (opportunity?.projectId
      ? "project"
      : opportunity?.guildId
        ? "guild"
        : undefined);
  const resolvedEntityId =
    entityId ??
    entitySelection?.id ??
    opportunity?.projectId ??
    opportunity?.guildId;
  const resolvedEntitySlug = entitySlug ?? entitySelection?.slug;

  const [formData, setFormData] = useState<OpportunityFormState>({
    title: opportunity?.title ?? "",
    description: opportunity?.description ?? "",
    type: opportunity?.type ?? "",
    skills: opportunity?.skills
      ? opportunity.skills
          .split(",")
          .map((s) => s.trim())
          .filter(Boolean)
      : [],
    compensationModel: opportunity?.compensationDetails ?? "",
    compensationAmount: opportunity?.compensation
      ? String(opportunity.compensation)
      : "",
    location: opportunity?.location ?? "",
    experienceLevel: opportunity?.experienceLevel ?? "",
    deadline: opportunity?.deadline ? opportunity.deadline.split("T")[0] : "",
    projectRole: opportunity?.projectRole ?? defaultProjectRole ?? "",
  });
  const [newSkill, setNewSkill] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const createOpportunity = api.opportunity.create.useMutation({
    onSuccess: () => {
      if (resolvedEntitySlug && resolvedEntityType === "project") {
        router.push(`/projects/${resolvedEntitySlug}`);
      } else {
        router.push("/opportunities");
      }
    },
    onError: (error) => {
      setErrors({ submit: error.message });
      setIsSubmitting(false);
    },
  });

  const updateOpportunity = api.opportunity.update.useMutation({
    onSuccess: () => {
      if (opportunity?.slug) {
        router.push(`/opportunities/${opportunity.slug}`);
      } else {
        router.push("/opportunities");
      }
    },
    onError: (error) => {
      setErrors({ submit: error.message });
      setIsSubmitting(false);
    },
  });

  const handleChange = <Key extends keyof OpportunityFormState>(
    field: Key,
    value: OpportunityFormState[Key],
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

  const addSkill = () => {
    if (newSkill.trim() && !formData.skills.includes(newSkill.trim())) {
      setFormData((prev) => ({
        ...prev,
        skills: [...prev.skills, newSkill.trim()],
      }));
      setNewSkill("");
    }
  };

  const removeSkill = (skill: string) => {
    setFormData((prev) => ({
      ...prev,
      skills: prev.skills.filter((s) => s !== skill),
    }));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.title.trim()) newErrors.title = "Title is required";
    if (!formData.description.trim())
      newErrors.description = "Description is required";
    if (formData.description.length < 20)
      newErrors.description = "Must be at least 20 characters";
    if (!formData.type) newErrors.type = "Job type is required";
    if (formData.skills.length === 0)
      newErrors.skills = "At least one skill is required";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);

    if (mode === "edit" && opportunity) {
      updateOpportunity.mutate({
        id: opportunity.id,
        title: formData.title,
        description: formData.description,
        type: formData.type,
        skills: formData.skills,
        experienceLevel: formData.experienceLevel || undefined,
        compensationModel: formData.compensationModel || undefined,
        compensationAmount: formData.compensationAmount
          ? parseFloat(formData.compensationAmount)
          : undefined,
        location: formData.location || undefined,
        isRemote:
          formData.location.toLowerCase().includes("remote") ||
          !formData.location,
        deadline: formData.deadline || undefined,
        projectRole: formData.projectRole
          ? (formData.projectRole as
              | "FOUNDER"
              | "LEADER"
              | "CORE_CONTRIBUTOR"
              | "CONTRIBUTOR"
              | "OBSERVER")
          : undefined,
      });
    } else {
      // Create mode
      if (!resolvedEntityId || !resolvedEntityType) {
        setErrors({
          submit: "An opportunity must be associated with a project or guild",
        });
        setIsSubmitting(false);
        return;
      }

      createOpportunity.mutate({
        title: formData.title,
        description: formData.description,
        type: formData.type,
        skills: formData.skills,
        experienceLevel: formData.experienceLevel || undefined,
        compensationModel: formData.compensationModel || undefined,
        compensationAmount: formData.compensationAmount
          ? parseFloat(formData.compensationAmount)
          : undefined,
        location: formData.location || undefined,
        isRemote:
          formData.location.toLowerCase().includes("remote") ||
          !formData.location,
        deadline: formData.deadline || undefined,
        projectId:
          resolvedEntityType === "project"
            ? resolvedEntityId || undefined
            : undefined,
        guildId:
          resolvedEntityType === "guild"
            ? resolvedEntityId || undefined
            : undefined,
        projectRole: formData.projectRole
          ? (formData.projectRole as
              | "FOUNDER"
              | "LEADER"
              | "CORE_CONTRIBUTOR"
              | "CONTRIBUTOR"
              | "OBSERVER")
          : undefined,
      });
    }
  };

  const isEditMode = mode === "edit";
  const backHref =
    isEditMode && opportunity?.slug
      ? `/opportunities/${opportunity.slug}`
      : "/opportunities";

  if (!isEditMode && (!resolvedEntityId || !resolvedEntityType)) {
    return (
      <div className="bg-background min-h-screen">
        <div className="mx-auto max-w-2xl px-4 py-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href={backHref}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Opportunities
            </Link>
          </Button>
          <h1 className="mb-2 text-3xl font-bold">Choose who is posting</h1>
          <p className="text-muted-foreground mb-8">
            Every opportunity belongs to a project or guild you can manage.
          </p>
          <EntitySelector
            onSelect={(type, id, slug) =>
              setEntitySelection({ type, id, slug })
            }
          />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-2xl px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href={backHref}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              {isEditMode ? "Back to Position" : "Back to Opportunities"}
            </Link>
          </Button>
          <h1 className="flex items-center gap-3 text-3xl font-bold">
            <div className="bg-neon-pink/20 flex h-10 w-10 items-center justify-center rounded-lg">
              <Briefcase className="text-neon-pink h-5 w-5" />
            </div>
            {isEditMode ? "Edit Position" : "Post a Job"}
          </h1>
          <p className="text-muted-foreground mt-2">
            {isEditMode
              ? "Update the details for this position"
              : "Create an opportunity to find talented contributors for your project"}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Purpose Info Card */}
          {!isEditMode &&
            resolvedEntityType === "project" &&
            resolvedEntitySlug && (
              <Card className="bg-neon/5 border-neon/20">
                <CardContent className="py-4">
                  <p className="text-sm">
                    Creating a team position for project:{" "}
                    <span className="font-medium">{resolvedEntitySlug}</span>
                  </p>
                </CardContent>
              </Card>
            )}

          {/* Job Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Job Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label
                  htmlFor="opportunity-title"
                  className="mb-2 block text-sm font-medium"
                >
                  Job Title <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  id="opportunity-title"
                  required
                  aria-invalid={Boolean(errors.title)}
                  aria-describedby={
                    errors.title ? "opportunity-title-error" : undefined
                  }
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Build Mobile App for EcoWaste Platform"
                  className={`bg-muted/50 focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p
                    id="opportunity-title-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.title}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="opportunity-description"
                  className="mb-2 block text-sm font-medium"
                >
                  Description <span className="text-neon">*</span>
                </label>
                <textarea
                  id="opportunity-description"
                  required
                  aria-invalid={Boolean(errors.description)}
                  aria-describedby={
                    errors.description
                      ? "opportunity-description-error"
                      : undefined
                  }
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Describe the job requirements, responsibilities, and deliverables..."
                  rows={5}
                  className={`bg-muted/50 focus:ring-neon/50 w-full resize-none rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p
                    id="opportunity-description-error"
                    role="alert"
                    className="text-destructive mt-1 text-sm"
                  >
                    {errors.description}
                  </p>
                )}
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="opportunity-type"
                    className="mb-2 block text-sm font-medium"
                  >
                    Job Type <span className="text-neon">*</span>
                  </label>
                  <Select
                    value={formData.type}
                    onValueChange={(value) => handleChange("type", value)}
                  >
                    <SelectTrigger
                      id="opportunity-type"
                      aria-required="true"
                      aria-invalid={Boolean(errors.type)}
                      aria-describedby={
                        errors.type ? "opportunity-type-error" : undefined
                      }
                      className={
                        errors.type ? "border-destructive" : "border-border"
                      }
                    >
                      <SelectValue placeholder="Select type" />
                    </SelectTrigger>
                    <SelectContent>
                      {jobTypes.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {errors.type && (
                    <p
                      id="opportunity-type-error"
                      role="alert"
                      className="text-destructive mt-1 text-sm"
                    >
                      {errors.type}
                    </p>
                  )}
                </div>

                <div>
                  <label
                    htmlFor="opportunity-experience"
                    className="mb-2 block text-sm font-medium"
                  >
                    Experience Level
                  </label>
                  <Select
                    value={formData.experienceLevel}
                    onValueChange={(value) =>
                      handleChange("experienceLevel", value)
                    }
                  >
                    <SelectTrigger id="opportunity-experience">
                      <SelectValue placeholder="Select level" />
                    </SelectTrigger>
                    <SelectContent>
                      {experienceLevels.map((level) => (
                        <SelectItem key={level.id} value={level.id}>
                          {level.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* Project Role - only for project-associated opportunities */}
              {resolvedEntityType === "project" && (
                <div>
                  <label
                    htmlFor="opportunity-role"
                    className="mb-2 block text-sm font-medium"
                  >
                    Team Role
                  </label>
                  <Select
                    value={formData.projectRole}
                    onValueChange={(value) =>
                      handleChange("projectRole", value)
                    }
                  >
                    <SelectTrigger
                      id="opportunity-role"
                      aria-describedby="opportunity-role-help"
                    >
                      <SelectValue placeholder="Assign to a role (optional)" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="FOUNDER">Founder</SelectItem>
                      <SelectItem value="LEADER">Leader</SelectItem>
                      <SelectItem value="CORE_CONTRIBUTOR">
                        Core Contributor
                      </SelectItem>
                      <SelectItem value="CONTRIBUTOR">Contributor</SelectItem>
                      <SelectItem value="OBSERVER">Observer</SelectItem>
                    </SelectContent>
                  </Select>
                  <p
                    id="opportunity-role-help"
                    className="text-muted-foreground mt-1 text-xs"
                  >
                    Assign this position to a team role so it appears under that
                    role in the team view
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Skills */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Required Skills</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <label htmlFor="opportunity-skill" className="sr-only">
                Add a required skill
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  id="opportunity-skill"
                  aria-invalid={Boolean(errors.skills)}
                  aria-describedby={
                    errors.skills ? "opportunity-skills-error" : undefined
                  }
                  value={newSkill}
                  onChange={(e) => setNewSkill(e.target.value)}
                  placeholder="e.g., React, TypeScript, Node.js"
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      addSkill();
                    }
                  }}
                  className="bg-muted/50 border-border focus:ring-neon/50 flex-1 rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
                <Button
                  type="button"
                  variant="outline"
                  onClick={addSkill}
                  className="min-h-11 min-w-11 px-4"
                  aria-label="Add skill"
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              {formData.skills.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {formData.skills.map((skill) => (
                    <Badge
                      key={skill}
                      variant="secondary"
                      className="gap-1 pr-0 pl-3"
                    >
                      {skill}
                      <button
                        type="button"
                        onClick={() => removeSkill(skill)}
                        aria-label={`Remove ${skill} skill`}
                        className="focus-visible:ring-ring hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
                      >
                        <X className="h-3 w-3" aria-hidden="true" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
              {errors.skills && (
                <p
                  id="opportunity-skills-error"
                  role="alert"
                  className="text-destructive text-sm"
                >
                  {errors.skills}
                </p>
              )}
            </CardContent>
          </Card>

          {/* Compensation */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-lg">
                <DollarSign className="text-neon-green h-5 w-5" />
                Compensation
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="opportunity-compensation-model"
                    className="mb-2 block text-sm font-medium"
                  >
                    Compensation Model
                  </label>
                  <Select
                    value={formData.compensationModel}
                    onValueChange={(value) =>
                      handleChange("compensationModel", value)
                    }
                  >
                    <SelectTrigger id="opportunity-compensation-model">
                      <SelectValue placeholder="Select model" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredCompensationTypes.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div>
                  <label
                    htmlFor="opportunity-compensation-amount"
                    className="mb-2 block text-sm font-medium"
                  >
                    Amount
                  </label>
                  <input
                    type="number"
                    id="opportunity-compensation-amount"
                    value={formData.compensationAmount}
                    onChange={(e) =>
                      handleChange("compensationAmount", e.target.value)
                    }
                    placeholder="0"
                    min="0"
                    className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label
                  htmlFor="opportunity-location"
                  className="mb-2 block text-sm font-medium"
                >
                  Location
                </label>
                <input
                  type="text"
                  id="opportunity-location"
                  value={formData.location}
                  onChange={(e) => handleChange("location", e.target.value)}
                  placeholder="e.g., Remote, New York, NY"
                  className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
              </div>

              <div>
                <label
                  htmlFor="opportunity-deadline"
                  className="mb-2 block text-sm font-medium"
                >
                  Application Deadline
                </label>
                <input
                  type="date"
                  id="opportunity-deadline"
                  value={formData.deadline}
                  onChange={(e) => handleChange("deadline", e.target.value)}
                  className="bg-muted/50 border-border focus:ring-neon/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none"
                />
              </div>
            </CardContent>
          </Card>

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
              className="bg-neon hover:bg-neon/90 flex-1 py-6 font-semibold text-black"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {isEditMode ? "Saving..." : "Posting Job..."}
                </>
              ) : isEditMode ? (
                "Save Changes"
              ) : (
                "Post Job"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href={backHref}>Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          {!isEditMode && (
            <div className="bg-muted/30 border-border flex items-start gap-3 rounded-lg border p-4">
              <Info className="text-neon mt-0.5 h-5 w-5" />
              <div className="text-muted-foreground text-sm">
                <p>
                  Your job posting will be visible to all community members.
                  You&apos;ll receive applications that you can review and
                  respond to.
                </p>
              </div>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
