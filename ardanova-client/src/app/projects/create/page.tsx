"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  Loader2,
  Plus,
  X,
  Info,
  Check,
  Pencil,
  Trash2,
  Calendar,
  Users,
  Target,
  Flag,
  RefreshCw,
  ShieldCheck,
  TriangleAlert,
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
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { useEnumOptions } from "~/hooks/use-enum";
import { toast } from "sonner";
import {
  isProjectDuration,
  isProjectRole,
  isProjectType,
  type ProjectDuration,
  type ProjectType,
} from "~/lib/contracts/project-contract";
import {
  PROJECT_CREATE_DRAFT_KEY,
  createEmptyProjectFormData,
  isProjectCreateCompensationModel,
  isProjectCreationCustodyReady,
  parseProjectCreateDraft,
  serializeProjectCreateDraft,
  toProjectRoleOpportunityInput,
  toOptionalIsoDate,
  type ProjectCreateCompensationModel,
  type ProjectCreateFormData as WizardFormData,
  type ProjectCreateMilestone as Milestone,
  type ProjectCreateRecovery,
  type ProjectCreateResource as Resource,
  type ProjectCreateRole as Role,
} from "~/lib/contracts/project-create-contract";

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

const compensationLabels: Record<ProjectCreateCompensationModel, string> = {
  FIXED_SHARES: "Fixed Token",
  HOURLY_SHARES: "Hourly Token",
  EQUITY_PERCENT: "Project-token allocation percentage",
  HYBRID: "Hybrid",
  BOUNTY: "Bounty",
  MILESTONE: "Milestone",
};

const RECURRING_PRESETS = [
  { label: "Biweekly", days: 14 },
  { label: "Monthly", days: 30 },
  { label: "Yearly", days: 365 },
] as const;

const PRESET_DAYS = RECURRING_PRESETS.map((p) => p.days);

const durationToMonths = (durationId: ProjectDuration | ""): number | null => {
  switch (durationId) {
    case "ONE_TWO_WEEKS":
      return 0.5;
    case "ONE_THREE_MONTHS":
      return 2;
    case "THREE_SIX_MONTHS":
      return 4.5;
    case "SIX_TWELVE_MONTHS":
      return 9;
    case "ONE_TWO_YEARS":
      return 18;
    case "TWO_PLUS_YEARS":
      return 30;
    case "ONGOING":
      return 12;
    default:
      return null;
  }
};

const toMonthlyRate = (cost: number, intervalDays: number): number =>
  (cost / intervalDays) * 30;

interface CreatedProjectReference {
  id: string;
  slug: string;
}

function isCreatedProjectReference(
  value: unknown,
): value is CreatedProjectReference {
  if (typeof value !== "object" || value === null) return false;
  const record = value as Record<string, unknown>;
  return typeof record.id === "string" && typeof record.slug === "string";
}

const steps: { id: number; name: string }[] = [
  { id: 0, name: "Basic Info" },
  { id: 1, name: "Resources" },
  { id: 2, name: "Team Roles" },
  { id: 3, name: "Milestones" },
  { id: 4, name: "Review" },
];

export default function CreateProjectPage() {
  const router = useRouter();
  const { data: session } = useSession();
  const custodyStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: Boolean(session?.user?.id),
    retry: false,
    staleTime: 0,
  });

  // API-driven enum options
  const { options: categories } = useEnumOptions("ProjectCategory");
  const { options: projectTypeOptions } = useEnumOptions("ProjectType");
  const projectTypes = projectTypeOptions.filter(
    (option): option is { id: ProjectType; label: string } =>
      isProjectType(option.id),
  );
  const { options: durationOptions } = useEnumOptions("ProjectDuration");
  const durations = durationOptions
    .filter((option): option is { id: ProjectDuration; label: string } =>
      isProjectDuration(option.id),
    )
    .map((option) => ({ ...option, label: durationLabels[option.id] }));
  const { options: compensationModelOptions } = useEnumOptions(
    "CompensationModel",
    compensationLabels,
  );
  const compensationModels = compensationModelOptions.filter(
    (option): option is { id: ProjectCreateCompensationModel; label: string } =>
      isProjectCreateCompensationModel(option.id),
  );

  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<WizardFormData>(
    createEmptyProjectFormData,
  );
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [draftHydrated, setDraftHydrated] = useState(false);
  const [draftPersistenceState, setDraftPersistenceState] = useState<
    "loading" | "saved" | "unavailable"
  >("loading");
  const [recovery, setRecovery] = useState<ProjectCreateRecovery | null>(null);
  const draftCompletedRef = useRef(false);

  useEffect(() => {
    try {
      const serialized = window.sessionStorage.getItem(
        PROJECT_CREATE_DRAFT_KEY,
      );
      const draft = parseProjectCreateDraft(serialized);

      if (draft) {
        setCurrentStep(draft.currentStep);
        setFormData(draft.formData);
        setRecovery(draft.recovery ?? null);
      } else if (serialized) {
        window.sessionStorage.removeItem(PROJECT_CREATE_DRAFT_KEY);
      }
      setDraftPersistenceState("saved");
    } catch {
      setDraftPersistenceState("unavailable");
    } finally {
      setDraftHydrated(true);
    }
  }, []);

  useEffect(() => {
    if (!draftHydrated || draftCompletedRef.current) return;

    try {
      window.sessionStorage.setItem(
        PROJECT_CREATE_DRAFT_KEY,
        serializeProjectCreateDraft({
          currentStep,
          formData,
          recovery: recovery ?? undefined,
        }),
      );
      setDraftPersistenceState("saved");
    } catch {
      setDraftPersistenceState("unavailable");
    }
  }, [currentStep, draftHydrated, formData, recovery]);

  // Resource state
  const [isAddingResource, setIsAddingResource] = useState(false);
  const [editingResourceId, setEditingResourceId] = useState<string | null>(
    null,
  );
  const [resourceForm, setResourceForm] = useState<Omit<Resource, "id">>({
    name: "",
    description: "",
    quantity: 1,
    estimatedCost: "",
    recurringCost: "",
    recurringIntervalDays: null,
    isRequired: true,
  });

  // Role state
  const [isAddingRole, setIsAddingRole] = useState(false);
  const [editingRoleId, setEditingRoleId] = useState<string | null>(null);
  const [roleForm, setRoleForm] = useState<Omit<Role, "id">>({
    title: "",
    description: "",
    compensationModel: "",
    shareAmount: "",
    equityPercent: "",
    isOpenForApplications: true,
    projectRole: "",
  });

  // Milestone state
  const [isAddingMilestone, setIsAddingMilestone] = useState(false);
  const [editingMilestoneId, setEditingMilestoneId] = useState<string | null>(
    null,
  );
  const [milestoneForm, setMilestoneForm] = useState<Omit<Milestone, "id">>({
    title: "",
    description: "",
    targetDate: "",
  });

  const createMutation = api.project.create.useMutation();
  const addResourceMutation = api.project.addResource.useMutation();
  const addMilestoneMutation = api.project.addMilestone.useMutation();
  const addMemberMutation = api.project.addMember.useMutation();
  const createOpportunityMutation = api.opportunity.create.useMutation();
  const deleteProjectMutation = api.project.delete.useMutation();
  const custodyReady = isProjectCreationCustodyReady(custodyStatus.data);
  const custodyCheckPending =
    !session?.user?.id || (!custodyStatus.data && custodyStatus.isPending);

  const handleChange = <Key extends keyof WizardFormData>(
    field: Key,
    value: WizardFormData[Key],
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

  const validateStep = (step: number): boolean => {
    const newErrors: Record<string, string> = {};

    if (step === 0) {
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
    }

    // Step 1: Resources - validate costs are numeric if provided
    if (step === 1) {
      for (const resource of formData.resources) {
        if (resource.estimatedCost && isNaN(Number(resource.estimatedCost))) {
          newErrors.resources = "Estimated cost must be a number";
        }
        if (resource.recurringCost && isNaN(Number(resource.recurringCost))) {
          newErrors.resources = "Recurring cost must be a number";
        }
      }
    }

    // Step 3: Milestones - validate dates are valid if provided
    if (step === 3) {
      for (const milestone of formData.milestones) {
        if (milestone.targetDate) {
          const date = new Date(milestone.targetDate);
          if (isNaN(date.getTime())) {
            newErrors.milestones = "Invalid date format";
          }
        }
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const goToNextStep = () => {
    if (validateStep(currentStep)) {
      setCurrentStep((prev) => Math.min(prev + 1, steps.length - 1));
    }
  };

  const goToPreviousStep = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0));
  };

  const goToStep = (step: number) => {
    if (step < currentStep || validateStep(currentStep)) {
      setCurrentStep(step);
    }
  };

  // Resource functions
  const addResource = () => {
    if (!resourceForm.name.trim()) {
      setErrors((previous) => ({
        ...previous,
        resourceName: "Resource name is required",
      }));
      return;
    }

    if (editingResourceId) {
      setFormData((prev) => ({
        ...prev,
        resources: prev.resources.map((r) =>
          r.id === editingResourceId ? { ...resourceForm, id: r.id } : r,
        ),
      }));
      setEditingResourceId(null);
    } else {
      setFormData((prev) => ({
        ...prev,
        resources: [
          ...prev.resources,
          { ...resourceForm, id: `resource-${Date.now()}` },
        ],
      }));
    }

    setResourceForm({
      name: "",
      description: "",
      quantity: 1,
      estimatedCost: "",
      recurringCost: "",
      recurringIntervalDays: null,
      isRequired: true,
    });
    setIsAddingResource(false);
  };

  const editResource = (resource: Resource) => {
    setResourceForm({
      name: resource.name,
      description: resource.description,
      quantity: resource.quantity,
      estimatedCost: resource.estimatedCost,
      recurringCost: resource.recurringCost,
      recurringIntervalDays: resource.recurringIntervalDays,
      isRequired: resource.isRequired,
    });
    setEditingResourceId(resource.id);
    setIsAddingResource(true);
  };

  const removeResource = (id: string) => {
    setFormData((prev) => ({
      ...prev,
      resources: prev.resources.filter((r) => r.id !== id),
    }));
  };

  const cancelResourceEdit = () => {
    setIsAddingResource(false);
    setEditingResourceId(null);
    setResourceForm({
      name: "",
      description: "",
      quantity: 1,
      estimatedCost: "",
      recurringCost: "",
      recurringIntervalDays: null,
      isRequired: true,
    });
  };

  // Role functions
  const addRole = () => {
    if (!roleForm.title.trim()) {
      setErrors((previous) => ({
        ...previous,
        roleTitle: "Role title is required",
      }));
      return;
    }

    if (editingRoleId) {
      setFormData((prev) => ({
        ...prev,
        roles: prev.roles.map((r) =>
          r.id === editingRoleId ? { ...roleForm, id: r.id } : r,
        ),
      }));
      setEditingRoleId(null);
    } else {
      setFormData((prev) => ({
        ...prev,
        roles: [...prev.roles, { ...roleForm, id: `role-${Date.now()}` }],
      }));
    }

    setRoleForm({
      title: "",
      description: "",
      compensationModel: "",
      shareAmount: "",
      equityPercent: "",
      isOpenForApplications: true,
      projectRole: "",
    });
    setIsAddingRole(false);
  };

  const editRole = (role: Role) => {
    setRoleForm({
      title: role.title,
      description: role.description,
      compensationModel: role.compensationModel,
      shareAmount: role.shareAmount,
      equityPercent: role.equityPercent,
      isOpenForApplications: role.isOpenForApplications,
      projectRole: role.projectRole,
    });
    setEditingRoleId(role.id);
    setIsAddingRole(true);
  };

  const removeRole = (id: string) => {
    setFormData((prev) => ({
      ...prev,
      roles: prev.roles.filter((r) => r.id !== id),
    }));
  };

  const cancelRoleEdit = () => {
    setIsAddingRole(false);
    setEditingRoleId(null);
    setRoleForm({
      title: "",
      description: "",
      compensationModel: "",
      shareAmount: "",
      equityPercent: "",
      isOpenForApplications: true,
      projectRole: "",
    });
  };

  // Milestone functions
  const addMilestone = () => {
    if (!milestoneForm.title.trim()) {
      setErrors((previous) => ({
        ...previous,
        milestoneTitle: "Milestone title is required",
      }));
      return;
    }

    if (editingMilestoneId) {
      setFormData((prev) => ({
        ...prev,
        milestones: prev.milestones.map((m) =>
          m.id === editingMilestoneId ? { ...milestoneForm, id: m.id } : m,
        ),
      }));
      setEditingMilestoneId(null);
    } else {
      setFormData((prev) => ({
        ...prev,
        milestones: [
          ...prev.milestones,
          { ...milestoneForm, id: `milestone-${Date.now()}` },
        ],
      }));
    }

    setMilestoneForm({
      title: "",
      description: "",
      targetDate: "",
    });
    setIsAddingMilestone(false);
  };

  const editMilestone = (milestone: Milestone) => {
    setMilestoneForm({
      title: milestone.title,
      description: milestone.description,
      targetDate: milestone.targetDate,
    });
    setEditingMilestoneId(milestone.id);
    setIsAddingMilestone(true);
  };

  const removeMilestone = (id: string) => {
    setFormData((prev) => ({
      ...prev,
      milestones: prev.milestones.filter((m) => m.id !== id),
    }));
  };

  const cancelMilestoneEdit = () => {
    setIsAddingMilestone(false);
    setEditingMilestoneId(null);
    setMilestoneForm({
      title: "",
      description: "",
      targetDate: "",
    });
  };

  const handleSubmit = async () => {
    if (!custodyReady) {
      setErrors((previous) => ({
        ...previous,
        submit:
          "Azoa must confirm your action account, identity verification, and managed wallet before this project can be created.",
      }));
      return;
    }

    const partialWarnings: string[] = [];
    const failedResourceIds: string[] = [];
    const failedMilestoneIds: string[] = [];
    const failedRoleIds: string[] = [];
    let founderMembershipPending = false;
    let createdProjectId: string | null = null;

    setIsSubmitting(true);
    try {
      let project: { id: string; slug: string };
      if (recovery) {
        project = { id: recovery.projectId, slug: recovery.projectSlug };
      } else {
        const resolvedCategories = formData.categories.map((category) =>
          category === "OTHER" ? formData.otherCategory.trim() : category,
        );
        const projectResult: unknown = await createMutation.mutateAsync({
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

        if (!isCreatedProjectReference(projectResult)) {
          throw new Error(
            "The project service returned an invalid project response.",
          );
        }
        project = projectResult;
        createdProjectId = project.id;
      }

      const resourcesToSave = recovery
        ? formData.resources.filter((resource) =>
            recovery.resourceIds.includes(resource.id),
          )
        : formData.resources;
      const milestonesToSave = recovery
        ? formData.milestones.filter((milestone) =>
            recovery.milestoneIds.includes(milestone.id),
          )
        : formData.milestones;
      const rolesToSave = recovery
        ? formData.roles.filter((role) => recovery.roleIds.includes(role.id))
        : formData.roles;

      // 2. Save resources
      for (const resource of resourcesToSave) {
        try {
          await addResourceMutation.mutateAsync({
            projectId: project.id,
            name: resource.name,
            description: resource.description || undefined,
            quantity: resource.quantity,
            estimatedCost:
              resource.estimatedCost && !isNaN(Number(resource.estimatedCost))
                ? Number(resource.estimatedCost)
                : undefined,
            recurringCost:
              resource.recurringCost && !isNaN(Number(resource.recurringCost))
                ? Number(resource.recurringCost)
                : undefined,
            recurringIntervalDays: resource.recurringIntervalDays ?? undefined,
            isRequired: resource.isRequired,
          });
        } catch (e) {
          failedResourceIds.push(resource.id);
          partialWarnings.push(
            `Resource "${resource.name}": ${e instanceof Error ? e.message : "could not be saved"}`,
          );
        }
      }

      // 3. Save milestones
      for (const milestone of milestonesToSave) {
        try {
          await addMilestoneMutation.mutateAsync({
            projectId: project.id,
            title: milestone.title,
            description: milestone.description || undefined,
            targetDate: toOptionalIsoDate(milestone.targetDate),
          });
        } catch (e) {
          failedMilestoneIds.push(milestone.id);
          partialWarnings.push(
            `Milestone "${milestone.title}": ${e instanceof Error ? e.message : "could not be saved"}`,
          );
        }
      }

      // 4. Auto-add creator as FOUNDER member
      if (
        session?.user?.id &&
        (!recovery || recovery.founderMembershipPending)
      ) {
        try {
          await addMemberMutation.mutateAsync({
            projectId: project.id,
            userId: session.user.id,
            role: "FOUNDER",
          });
        } catch (e) {
          founderMembershipPending = true;
          partialWarnings.push(
            `Founder membership: ${e instanceof Error ? e.message : "could not be saved"}`,
          );
        }
      }

      // 5. Save team roles as project opportunities
      for (const role of rolesToSave) {
        try {
          await createOpportunityMutation.mutateAsync(
            toProjectRoleOpportunityInput({
              projectId: project.id,
              projectTitle: formData.title,
              tags: formData.tags,
              role,
            }),
          );
        } catch (e) {
          failedRoleIds.push(role.id);
          partialWarnings.push(
            `Role "${role.title}": ${e instanceof Error ? e.message : "opportunity could not be created"}`,
          );
        }
      }

      if (partialWarnings.length > 0) {
        const nextRecovery: ProjectCreateRecovery = {
          projectId: project.id,
          projectSlug: project.slug,
          resourceIds: failedResourceIds,
          milestoneIds: failedMilestoneIds,
          roleIds: failedRoleIds,
          founderMembershipPending,
          warnings: partialWarnings,
        };
        setRecovery(nextRecovery);

        try {
          window.sessionStorage.setItem(
            PROJECT_CREATE_DRAFT_KEY,
            serializeProjectCreateDraft({
              currentStep,
              formData,
              recovery: nextRecovery,
            }),
          );
          setDraftPersistenceState("saved");
        } catch {
          setDraftPersistenceState("unavailable");
          setErrors({
            submit:
              "The project exists, but this browser could not save the remaining setup. Keep this tab open and retry the unfinished items.",
          });
          return;
        }

        toast.warning("Project created, but with some issues.", {
          description: `${partialWarnings.slice(0, 5).join(". ")}${partialWarnings.length > 5 ? `. ${partialWarnings.length - 5} more items remain.` : ""}`,
          duration: 8000,
        });
      } else {
        draftCompletedRef.current = true;
        setRecovery(null);
        try {
          window.sessionStorage.removeItem(PROJECT_CREATE_DRAFT_KEY);
        } catch {
          // The server result remains authoritative when tab storage is unavailable.
        }
      }
      router.push(`/projects/${project.slug}`);
    } catch (error) {
      // Rollback: delete the project if it was created but later steps failed
      if (createdProjectId) {
        const rollbackProjectId = createdProjectId;
        try {
          await new Promise<void>((resolve, reject) => {
            deleteProjectMutation.mutate(
              { id: rollbackProjectId },
              {
                onSuccess: () => resolve(),
                onError: (mutationError) =>
                  reject(
                    mutationError instanceof Error
                      ? mutationError
                      : new Error("Project rollback failed"),
                  ),
              },
            );
          });
        } catch {
          // Best effort rollback - can't do much if this fails
        }
      }
      setErrors({
        submit:
          error instanceof Error ? error.message : "Failed to create project",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="bg-background min-h-screen">
      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Header */}
        <div className="mb-8 sm:flex sm:items-end sm:justify-between sm:gap-6">
          <div>
            <Button variant="ghost" asChild className="mb-4 -ml-2">
              <Link href="/projects">
                <ArrowLeft className="mr-2 size-4" />
                Back to Projects
              </Link>
            </Button>
            <h1 className="text-3xl font-bold">Create New Project</h1>
            <p className="text-muted-foreground mt-2">
              Define a problem, shape a solution, then make the work actionable.
            </p>
          </div>
          <p
            role="status"
            className="text-muted-foreground mt-4 font-mono text-xs uppercase sm:mt-0 sm:text-right"
          >
            {draftPersistenceState === "loading"
              ? "Restoring this tab's draft..."
              : draftPersistenceState === "saved"
                ? "Draft saved in this tab"
                : "This browser blocked draft storage"}
          </p>
        </div>

        {recovery && (
          <section
            className="border-foreground bg-system/10 mb-8 border-2 p-4 sm:p-5"
            aria-labelledby="project-recovery-title"
          >
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <Badge variant="outline">Project already created</Badge>
                <h2
                  id="project-recovery-title"
                  className="mt-2 font-mono text-lg font-black uppercase"
                >
                  Resume unfinished setup
                </h2>
                <p className="text-muted-foreground mt-1 max-w-2xl text-sm leading-relaxed">
                  This validated recovery draft will retry only the resources,
                  milestones, membership, and roles that did not save. It will
                  not create a duplicate project.
                </p>
              </div>
              <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row">
                <Button variant="outline" className="min-h-11" asChild>
                  <Link href={`/projects/${recovery.projectSlug}`}>
                    View project
                  </Link>
                </Button>
                <Button
                  type="button"
                  className="min-h-11"
                  onClick={handleSubmit}
                  disabled={isSubmitting || !custodyReady}
                >
                  {isSubmitting ? "Retrying..." : "Retry remaining setup"}
                </Button>
              </div>
            </div>
          </section>
        )}

        <section
          className="border-foreground bg-card mb-8 border-2"
          aria-labelledby="project-readiness-title"
          aria-live="polite"
        >
          <div className="flex flex-col gap-4 p-4 sm:flex-row sm:items-start sm:justify-between sm:p-5">
            <div className="flex items-start gap-3">
              <span className="border-foreground bg-system/10 text-system flex size-10 shrink-0 items-center justify-center border-2">
                {custodyCheckPending ? (
                  <Loader2 className="size-5 animate-spin" aria-hidden="true" />
                ) : custodyReady ? (
                  <ShieldCheck className="size-5" aria-hidden="true" />
                ) : (
                  <TriangleAlert className="size-5" aria-hidden="true" />
                )}
              </span>
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h2
                    id="project-readiness-title"
                    className="font-mono text-base font-black uppercase"
                  >
                    Azoa action readiness
                  </h2>
                  <Badge variant={custodyReady ? "success" : "outline"}>
                    {custodyCheckPending
                      ? "Checking"
                      : custodyReady
                        ? "Confirmed"
                        : "Setup needed"}
                  </Badge>
                </div>
                <p
                  id="create-project-readiness"
                  className="text-muted-foreground mt-2 max-w-2xl text-sm leading-relaxed"
                >
                  {custodyCheckPending
                    ? "Checking the tenant-bound account used for project actions."
                    : custodyStatus.error
                      ? custodyStatus.error.message
                      : custodyReady
                        ? "Azoa confirms your action account, identity check, and managed wallet are ready. The project service verifies them again when you create."
                        : (custodyStatus.data?.unavailableReason ??
                          "Complete your Azoa action account and identity verification before creating the project record.")}
                </p>
              </div>
            </div>

            {!custodyCheckPending && !custodyReady && (
              <div className="flex w-full flex-col gap-2 sm:w-auto">
                <Button className="min-h-11 w-full sm:w-auto" asChild>
                  <Link href="/settings/verification?returnTo=%2Fprojects%2Fcreate">
                    <ShieldCheck className="size-4" aria-hidden="true" />
                    Review secure setup
                  </Link>
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11 w-full sm:w-auto"
                  disabled={custodyStatus.isFetching}
                  onClick={() => void custodyStatus.refetch()}
                >
                  <RefreshCw
                    className={cn(
                      "size-4",
                      custodyStatus.isFetching && "animate-spin",
                    )}
                    aria-hidden="true"
                  />
                  Check again
                </Button>
              </div>
            )}
          </div>

          <div className="border-foreground grid border-t-2 sm:grid-cols-3">
            <div className="border-foreground flex items-center justify-between gap-3 border-b-2 px-4 py-3 sm:border-r-2 sm:border-b-0">
              <span className="text-sm font-medium">Action account</span>
              <span className="font-mono text-xs uppercase">
                {custodyStatus.data?.identityReady ? "Ready" : "Not confirmed"}
              </span>
            </div>
            <div className="border-foreground flex items-center justify-between gap-3 border-b-2 px-4 py-3 sm:border-r-2 sm:border-b-0">
              <span className="text-sm font-medium">Identity check</span>
              <span className="font-mono text-xs uppercase">
                {custodyStatus.data?.kycStatus ?? "Unknown"}
              </span>
            </div>
            <div className="flex items-center justify-between gap-3 px-4 py-3">
              <span className="text-sm font-medium">Managed wallet</span>
              <span className="font-mono text-xs uppercase">
                {custodyStatus.data?.walletReady ? "Ready" : "Not confirmed"}
              </span>
            </div>
          </div>

          {!custodyReady && (
            <p className="border-foreground text-muted-foreground border-t-2 px-4 py-3 text-xs leading-relaxed sm:px-5">
              Keep shaping the draft here if you like. Moving to verification
              will not erase it in this tab; only the final create action stays
              locked.
            </p>
          )}
        </section>

        {/* Progress Stepper */}
        <nav
          className="mb-8 overflow-x-auto pb-2"
          aria-label="Project creation progress"
        >
          <div className="flex min-w-max items-start gap-2 sm:w-full sm:justify-between sm:gap-0">
            {steps.map((step, index) => (
              <div key={step.id} className="flex items-center">
                <button
                  type="button"
                  onClick={() => goToStep(index)}
                  aria-current={currentStep === index ? "step" : undefined}
                  aria-label={`Step ${index + 1} of ${steps.length}: ${step.name}${currentStep > index ? ", completed" : ""}`}
                  className={cn(
                    "flex min-h-11 min-w-20 flex-col items-center gap-2 px-1 transition-opacity",
                    currentStep < index && "opacity-50",
                  )}
                >
                  <div
                    className={cn(
                      "flex h-10 w-10 items-center justify-center border-2 font-medium transition-colors",
                      currentStep > index
                        ? "bg-system text-system-foreground"
                        : currentStep === index
                          ? "bg-system/10 text-system border-system border-2"
                          : "bg-muted text-muted-foreground",
                    )}
                  >
                    {currentStep > index ? (
                      <Check className="size-5" />
                    ) : (
                      index + 1
                    )}
                  </div>
                  <span className="text-xs font-medium">{step.name}</span>
                </button>
                {index < steps.length - 1 && (
                  <div
                    className={cn(
                      "mx-1 mt-5 h-0.5 w-5 transition-colors sm:mx-2 sm:w-12",
                      currentStep > index ? "bg-system" : "bg-border",
                    )}
                  />
                )}
              </div>
            ))}
          </div>
        </nav>

        {/* Step Content */}
        <div className="space-y-6">
          {/* Step 1: Basic Info */}
          {currentStep === 0 && (
            <>
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
                        errors.problemStatement
                          ? "project-problem-error"
                          : undefined
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
                      onChange={(e) =>
                        handleChange("targetAudience", e.target.value)
                      }
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
                      onChange={(e) =>
                        handleChange("expectedImpact", e.target.value)
                      }
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
                        errors.solution
                          ? "border-destructive"
                          : "border-border",
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
                        errors.categories
                          ? "project-categories-error"
                          : undefined
                      }
                      className={cn(
                        "flex flex-wrap gap-2 rounded-lg border-2 p-3",
                        errors.categories
                          ? "border-destructive"
                          : "border-border",
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
                                  const newErrors = { ...prev };
                                  delete newErrors.categories;
                                  return newErrors;
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
                                  const newErrors = { ...prev };
                                  delete newErrors.otherCategory;
                                  return newErrors;
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
                            {formData.otherCategory.length}/
                            {OTHER_CATEGORY_MAX_LENGTH}
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
                            <X className="size-3" aria-hidden="true" />
                          </button>
                        </Badge>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </>
          )}

          {/* Step 2: Resources */}
          {currentStep === 1 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2 text-lg">
                      <Target className="text-system size-5" />
                      Project Resources
                    </CardTitle>
                    <p className="text-muted-foreground mt-1 text-sm">
                      Define resources needed for your project
                    </p>
                  </div>
                  {!isAddingResource && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingResource(true)}
                    >
                      <Plus className="mr-2 size-4" />
                      Add Resource
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {errors.resources && (
                  <p role="alert" className="text-destructive text-sm">
                    {errors.resources}
                  </p>
                )}
                {/* Add/Edit Resource Form */}
                {isAddingResource && (
                  <Card className="bg-muted/30 border-border border-2 border-dashed">
                    <CardContent className="space-y-4 p-4">
                      <div>
                        <label
                          htmlFor="project-resource-name"
                          className="mb-2 block text-sm font-medium"
                        >
                          Resource Name <span className="text-system">*</span>
                        </label>
                        <input
                          type="text"
                          id="project-resource-name"
                          required
                          aria-invalid={Boolean(errors.resourceName)}
                          aria-describedby={
                            errors.resourceName
                              ? "project-resource-name-error"
                              : undefined
                          }
                          value={resourceForm.name}
                          onChange={(e) => {
                            setResourceForm((prev) => ({
                              ...prev,
                              name: e.target.value,
                            }));
                            setErrors((previous) => {
                              const next = { ...previous };
                              delete next.resourceName;
                              return next;
                            });
                          }}
                          placeholder="e.g., Cloud Server"
                          className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                        {errors.resourceName && (
                          <p
                            id="project-resource-name-error"
                            role="alert"
                            className="text-destructive mt-1 text-sm"
                          >
                            {errors.resourceName}
                          </p>
                        )}
                      </div>
                      <div>
                        <label
                          htmlFor="project-resource-description"
                          className="mb-2 block text-sm font-medium"
                        >
                          Description
                        </label>
                        <textarea
                          id="project-resource-description"
                          value={resourceForm.description}
                          onChange={(e) =>
                            setResourceForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe the resource..."
                          rows={2}
                          className="bg-background border-border focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                      </div>
                      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <div>
                          <label
                            htmlFor="project-resource-quantity"
                            className="mb-2 block text-sm font-medium"
                          >
                            Quantity
                          </label>
                          <input
                            type="number"
                            id="project-resource-quantity"
                            value={resourceForm.quantity}
                            onChange={(e) =>
                              setResourceForm((prev) => ({
                                ...prev,
                                quantity: parseInt(e.target.value) || 1,
                              }))
                            }
                            min="1"
                            className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label
                            htmlFor="project-resource-estimated-cost"
                            className="mb-2 block text-sm font-medium"
                          >
                            Estimated Cost ($)
                          </label>
                          <input
                            type="number"
                            id="project-resource-estimated-cost"
                            value={resourceForm.estimatedCost}
                            onChange={(e) =>
                              setResourceForm((prev) => ({
                                ...prev,
                                estimatedCost: e.target.value,
                              }))
                            }
                            placeholder="0"
                            min="0"
                            className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                          />
                        </div>
                      </div>

                      {/* Recurring Cost */}
                      <div className="border-border/50 space-y-3 border-t pt-2">
                        <p className="block text-sm font-medium">
                          Recurring Cost
                        </p>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                          <div>
                            <label
                              htmlFor="project-resource-recurring-cost"
                              className="text-muted-foreground mb-1 block text-xs"
                            >
                              Amount ($) per period
                            </label>
                            <input
                              type="number"
                              id="project-resource-recurring-cost"
                              value={resourceForm.recurringCost}
                              onChange={(e) =>
                                setResourceForm((prev) => ({
                                  ...prev,
                                  recurringCost: e.target.value,
                                }))
                              }
                              placeholder="0"
                              min="0"
                              className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                            />
                          </div>
                          <div>
                            <span className="text-muted-foreground mb-1 block text-xs">
                              Billing interval
                            </span>
                            <div
                              className="flex flex-wrap gap-2"
                              role="group"
                              aria-label="Recurring billing interval"
                            >
                              {RECURRING_PRESETS.map((preset) => (
                                <button
                                  key={preset.label}
                                  type="button"
                                  aria-pressed={
                                    resourceForm.recurringIntervalDays ===
                                    preset.days
                                  }
                                  onClick={() =>
                                    setResourceForm((prev) => ({
                                      ...prev,
                                      recurringIntervalDays: preset.days,
                                    }))
                                  }
                                  className={cn(
                                    "min-h-11 rounded-md border px-3 py-1.5 text-xs transition-colors",
                                    resourceForm.recurringIntervalDays ===
                                      preset.days
                                      ? "bg-system/10 border-system text-system"
                                      : "bg-muted/50 border-border text-muted-foreground hover:bg-muted",
                                  )}
                                >
                                  {preset.label}
                                </button>
                              ))}
                              <button
                                type="button"
                                aria-pressed={
                                  resourceForm.recurringIntervalDays !== null &&
                                  !PRESET_DAYS.includes(
                                    resourceForm.recurringIntervalDays as
                                      | 14
                                      | 30
                                      | 365,
                                  )
                                }
                                onClick={() =>
                                  setResourceForm((prev) => ({
                                    ...prev,
                                    recurringIntervalDays:
                                      prev.recurringIntervalDays !== null &&
                                      !PRESET_DAYS.includes(
                                        prev.recurringIntervalDays as
                                          | 14
                                          | 30
                                          | 365,
                                      )
                                        ? prev.recurringIntervalDays
                                        : 7,
                                  }))
                                }
                                className={cn(
                                  "min-h-11 rounded-md border px-3 py-1.5 text-xs transition-colors",
                                  resourceForm.recurringIntervalDays !== null &&
                                    !PRESET_DAYS.includes(
                                      resourceForm.recurringIntervalDays as
                                        | 14
                                        | 30
                                        | 365,
                                    )
                                    ? "bg-system/10 border-system text-system"
                                    : "bg-muted/50 border-border text-muted-foreground hover:bg-muted",
                                )}
                              >
                                Custom
                              </button>
                            </div>
                          </div>
                        </div>

                        {/* Custom interval input */}
                        {resourceForm.recurringIntervalDays !== null &&
                          !PRESET_DAYS.includes(
                            resourceForm.recurringIntervalDays as 14 | 30 | 365,
                          ) && (
                            <div>
                              <label
                                htmlFor="project-resource-custom-interval"
                                className="text-muted-foreground mb-1 block text-xs"
                              >
                                Custom interval (days, max 365)
                              </label>
                              <input
                                type="number"
                                id="project-resource-custom-interval"
                                value={resourceForm.recurringIntervalDays}
                                onChange={(e) => {
                                  const val = Math.min(
                                    365,
                                    Math.max(1, parseInt(e.target.value) || 1),
                                  );
                                  setResourceForm((prev) => ({
                                    ...prev,
                                    recurringIntervalDays: val,
                                  }));
                                }}
                                min="1"
                                max="365"
                                className="bg-background border-border focus-visible:ring-ring/50 w-32 rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                              />
                            </div>
                          )}

                        {/* Computed monthly rate preview */}
                        {resourceForm.recurringCost &&
                          Number(resourceForm.recurringCost) > 0 &&
                          resourceForm.recurringIntervalDays &&
                          resourceForm.recurringIntervalDays > 0 && (
                            <p className="text-muted-foreground text-xs">
                              Equivalent to{" "}
                              <span className="text-foreground font-medium">
                                $
                                {toMonthlyRate(
                                  Number(resourceForm.recurringCost),
                                  resourceForm.recurringIntervalDays,
                                ).toFixed(2)}
                                /month
                              </span>
                            </p>
                          )}

                        {/* Clear recurring cost button */}
                        {resourceForm.recurringIntervalDays !== null && (
                          <button
                            type="button"
                            onClick={() =>
                              setResourceForm((prev) => ({
                                ...prev,
                                recurringCost: "",
                                recurringIntervalDays: null,
                              }))
                            }
                            className="text-muted-foreground hover:text-foreground min-h-11 text-xs underline"
                          >
                            Remove recurring cost
                          </button>
                        )}
                      </div>

                      <div className="flex min-h-11 items-center gap-2">
                        <input
                          type="checkbox"
                          id="resourceRequired"
                          checked={resourceForm.isRequired}
                          onChange={(e) =>
                            setResourceForm((prev) => ({
                              ...prev,
                              isRequired: e.target.checked,
                            }))
                          }
                          className="border-border size-5 rounded"
                        />
                        <label
                          htmlFor="resourceRequired"
                          className="text-sm font-medium"
                        >
                          This resource is required
                        </label>
                      </div>
                      <div className="flex justify-end gap-2 pt-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={cancelResourceEdit}
                        >
                          Cancel
                        </Button>
                        <Button
                          variant="default"
                          size="sm"
                          onClick={addResource}
                        >
                          {editingResourceId ? "Update" : "Add"} Resource
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Resource List */}
                {formData.resources.length === 0 && !isAddingResource && (
                  <div className="text-muted-foreground py-8 text-center">
                    <Target className="mx-auto mb-2 size-12 opacity-50" />
                    <p>No resources added yet</p>
                    <p className="text-sm">
                      Click &ldquo;Add Resource&rdquo; to define project
                      resources
                    </p>
                  </div>
                )}

                {formData.resources.map((resource) => (
                  <Card key={resource.id} className="bg-card border-border">
                    <CardContent className="p-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <h4 className="font-medium">{resource.name}</h4>
                            {resource.isRequired && (
                              <Badge variant="secondary" className="text-xs">
                                Required
                              </Badge>
                            )}
                          </div>
                          {resource.description && (
                            <p className="text-muted-foreground mt-1 text-sm">
                              {resource.description}
                            </p>
                          )}
                          <div className="text-muted-foreground mt-2 flex flex-wrap gap-4 text-sm">
                            <span>Qty: {resource.quantity}</span>
                            {resource.estimatedCost && (
                              <span>One-time: ${resource.estimatedCost}</span>
                            )}
                            {resource.recurringCost &&
                              resource.recurringIntervalDays && (
                                <span>
                                  Recurring: ${resource.recurringCost}/
                                  {resource.recurringIntervalDays}d ($
                                  {toMonthlyRate(
                                    Number(resource.recurringCost),
                                    resource.recurringIntervalDays,
                                  ).toFixed(2)}
                                  /mo)
                                </span>
                              )}
                          </div>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => editResource(resource)}
                          >
                            <Pencil className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => removeResource(resource.id)}
                          >
                            <Trash2 className="text-destructive size-4" />
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </CardContent>
            </Card>
          )}

          {/* Step 3: Team Roles */}
          {currentStep === 2 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2 text-lg">
                      <Users className="text-system size-5" />
                      Team Roles
                    </CardTitle>
                    <p className="text-muted-foreground mt-1 text-sm">
                      Define roles and compensation for your team
                    </p>
                  </div>
                  {!isAddingRole && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingRole(true)}
                    >
                      <Plus className="mr-2 size-4" />
                      Add Role
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Add/Edit Role Form */}
                {isAddingRole && (
                  <Card className="bg-muted/30 border-border border-2 border-dashed">
                    <CardContent className="space-y-4 p-4">
                      <div>
                        <label
                          htmlFor="project-role-title"
                          className="mb-2 block text-sm font-medium"
                        >
                          Role Title <span className="text-system">*</span>
                        </label>
                        <input
                          type="text"
                          id="project-role-title"
                          required
                          aria-invalid={Boolean(errors.roleTitle)}
                          aria-describedby={
                            errors.roleTitle
                              ? "project-role-title-error"
                              : undefined
                          }
                          value={roleForm.title}
                          onChange={(e) => {
                            setRoleForm((prev) => ({
                              ...prev,
                              title: e.target.value,
                            }));
                            setErrors((previous) => {
                              const next = { ...previous };
                              delete next.roleTitle;
                              return next;
                            });
                          }}
                          placeholder="e.g., Lead Developer"
                          className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                        {errors.roleTitle && (
                          <p
                            id="project-role-title-error"
                            role="alert"
                            className="text-destructive mt-1 text-sm"
                          >
                            {errors.roleTitle}
                          </p>
                        )}
                      </div>
                      <div>
                        <label
                          htmlFor="project-role-description"
                          className="mb-2 block text-sm font-medium"
                        >
                          Description
                        </label>
                        <textarea
                          id="project-role-description"
                          value={roleForm.description}
                          onChange={(e) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe the role responsibilities..."
                          rows={2}
                          className="bg-background border-border focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                      </div>
                      <div>
                        <label
                          htmlFor="project-role-compensation"
                          className="mb-2 block text-sm font-medium"
                        >
                          Compensation Model
                        </label>
                        <Select
                          value={roleForm.compensationModel}
                          onValueChange={(value) => {
                            if (!isProjectCreateCompensationModel(value)) return;
                            setRoleForm((prev) => ({
                              ...prev,
                              compensationModel: value,
                            }));
                          }}
                        >
                          <SelectTrigger id="project-role-compensation">
                            <SelectValue placeholder="Select model" />
                          </SelectTrigger>
                          <SelectContent>
                            {compensationModels.map((model) => (
                              <SelectItem key={model.id} value={model.id}>
                                {model.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <div>
                        <label
                          htmlFor="project-role-base"
                          className="mb-2 block text-sm font-medium"
                        >
                          Base Role
                        </label>
                        <Select
                          value={roleForm.projectRole}
                          onValueChange={(value) => {
                            if (isProjectRole(value)) {
                              setRoleForm((prev) => ({
                                ...prev,
                                projectRole: value,
                              }));
                            }
                          }}
                        >
                          <SelectTrigger
                            id="project-role-base"
                            aria-describedby="project-role-base-help"
                          >
                            <SelectValue placeholder="Select base role" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="FOUNDER">Founder</SelectItem>
                            <SelectItem value="LEADER">Leader</SelectItem>
                            <SelectItem value="CORE_CONTRIBUTOR">
                              Core Contributor
                            </SelectItem>
                            <SelectItem value="CONTRIBUTOR">
                              Contributor
                            </SelectItem>
                            <SelectItem value="OBSERVER">Observer</SelectItem>
                          </SelectContent>
                        </Select>
                        <p
                          id="project-role-base-help"
                          className="text-muted-foreground mt-1 text-xs"
                        >
                          The organizational role granted when this position is
                          filled
                        </p>
                      </div>
                      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                        <div>
                          <label
                            htmlFor="project-role-share"
                            className="mb-2 block text-sm font-medium"
                          >
                            Share Amount
                          </label>
                          <input
                            type="number"
                            id="project-role-share"
                            value={roleForm.shareAmount}
                            onChange={(e) =>
                              setRoleForm((prev) => ({
                                ...prev,
                                shareAmount: e.target.value,
                              }))
                            }
                            placeholder="0"
                            min="0"
                            className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                          />
                        </div>
                        <div>
                          <label
                            htmlFor="project-role-equity"
                            className="mb-2 block text-sm font-medium"
                          >
                            Allocation percentage
                          </label>
                          <input
                            type="number"
                            id="project-role-equity"
                            value={roleForm.equityPercent}
                            onChange={(e) =>
                              setRoleForm((prev) => ({
                                ...prev,
                                equityPercent: e.target.value,
                              }))
                            }
                            placeholder="0"
                            min="0"
                            max="100"
                            className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                          />
                        </div>
                      </div>
                      <div className="flex min-h-11 items-center gap-2">
                        <input
                          type="checkbox"
                          id="roleOpen"
                          checked={roleForm.isOpenForApplications}
                          onChange={(e) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              isOpenForApplications: e.target.checked,
                            }))
                          }
                          className="border-border size-5 rounded"
                        />
                        <label
                          htmlFor="roleOpen"
                          className="text-sm font-medium"
                        >
                          Open for applications
                        </label>
                      </div>
                      <div className="flex justify-end gap-2 pt-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={cancelRoleEdit}
                        >
                          Cancel
                        </Button>
                        <Button variant="default" size="sm" onClick={addRole}>
                          {editingRoleId ? "Update" : "Add"} Role
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Role List */}
                {formData.roles.length === 0 && !isAddingRole && (
                  <div className="text-muted-foreground py-8 text-center">
                    <Users className="mx-auto mb-2 size-12 opacity-50" />
                    <p>No roles defined yet</p>
                    <p className="text-sm">
                      Click &ldquo;Add Role&rdquo; to define team roles
                    </p>
                  </div>
                )}

                {formData.roles.map((role) => (
                  <Card key={role.id} className="bg-card border-border">
                    <CardContent className="p-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <h4 className="font-medium">{role.title}</h4>
                            {role.isOpenForApplications && (
                              <Badge variant="secondary" className="text-xs">
                                Open
                              </Badge>
                            )}
                          </div>
                          {role.description && (
                            <p className="text-muted-foreground mt-1 text-sm">
                              {role.description}
                            </p>
                          )}
                          <div className="text-muted-foreground mt-2 flex flex-wrap gap-3 text-sm">
                            {role.compensationModel && (
                              <span>
                                Model:{" "}
                                {compensationModels.find(
                                  (m) => m.id === role.compensationModel,
                                )?.label || role.compensationModel}
                              </span>
                            )}
                            {role.shareAmount && (
                              <span>Shares: {role.shareAmount}</span>
                            )}
                            {role.equityPercent && (
                              <span>Allocation: {role.equityPercent}%</span>
                            )}
                          </div>
                        </div>
                        <div className="flex gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => editRole(role)}
                          >
                            <Pencil className="size-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => removeRole(role.id)}
                          >
                            <Trash2 className="text-destructive size-4" />
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </CardContent>
            </Card>
          )}

          {/* Step 4: Milestones */}
          {currentStep === 3 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="flex items-center gap-2 text-lg">
                      <Flag className="text-system size-5" />
                      Project Milestones
                    </CardTitle>
                    <p className="text-muted-foreground mt-1 text-sm">
                      Define key milestones for your project timeline
                    </p>
                  </div>
                  {!isAddingMilestone && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingMilestone(true)}
                    >
                      <Plus className="mr-2 size-4" />
                      Add Milestone
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {errors.milestones && (
                  <p role="alert" className="text-destructive text-sm">
                    {errors.milestones}
                  </p>
                )}
                {/* Add/Edit Milestone Form */}
                {isAddingMilestone && (
                  <Card className="bg-muted/30 border-border border-2 border-dashed">
                    <CardContent className="space-y-4 p-4">
                      <div>
                        <label
                          htmlFor="project-milestone-title"
                          className="mb-2 block text-sm font-medium"
                        >
                          Milestone Title <span className="text-system">*</span>
                        </label>
                        <input
                          type="text"
                          id="project-milestone-title"
                          required
                          aria-invalid={Boolean(errors.milestoneTitle)}
                          aria-describedby={
                            errors.milestoneTitle
                              ? "project-milestone-title-error"
                              : undefined
                          }
                          value={milestoneForm.title}
                          onChange={(e) => {
                            setMilestoneForm((prev) => ({
                              ...prev,
                              title: e.target.value,
                            }));
                            setErrors((previous) => {
                              const next = { ...previous };
                              delete next.milestoneTitle;
                              return next;
                            });
                          }}
                          placeholder="e.g., MVP Launch"
                          className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                        {errors.milestoneTitle && (
                          <p
                            id="project-milestone-title-error"
                            role="alert"
                            className="text-destructive mt-1 text-sm"
                          >
                            {errors.milestoneTitle}
                          </p>
                        )}
                      </div>
                      <div>
                        <label
                          htmlFor="project-milestone-description"
                          className="mb-2 block text-sm font-medium"
                        >
                          Description
                        </label>
                        <textarea
                          id="project-milestone-description"
                          value={milestoneForm.description}
                          onChange={(e) =>
                            setMilestoneForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe this milestone..."
                          rows={2}
                          className="bg-background border-border focus-visible:ring-ring/50 w-full resize-none rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                      </div>
                      <div>
                        <label
                          htmlFor="project-milestone-date"
                          className="mb-2 block text-sm font-medium"
                        >
                          Target Date
                        </label>
                        <input
                          type="date"
                          id="project-milestone-date"
                          value={milestoneForm.targetDate}
                          onChange={(e) =>
                            setMilestoneForm((prev) => ({
                              ...prev,
                              targetDate: e.target.value,
                            }))
                          }
                          className="bg-background border-border focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-2 focus:ring-2 focus:outline-none"
                        />
                      </div>
                      <div className="flex justify-end gap-2 pt-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={cancelMilestoneEdit}
                        >
                          Cancel
                        </Button>
                        <Button
                          variant="default"
                          size="sm"
                          onClick={addMilestone}
                        >
                          {editingMilestoneId ? "Update" : "Add"} Milestone
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Milestone List */}
                {formData.milestones.length === 0 && !isAddingMilestone && (
                  <div className="text-muted-foreground py-8 text-center">
                    <Flag className="mx-auto mb-2 size-12 opacity-50" />
                    <p>No milestones defined yet</p>
                    <p className="text-sm">
                      Click &ldquo;Add Milestone&rdquo; to define project
                      milestones
                    </p>
                  </div>
                )}

                <div className="space-y-3">
                  {formData.milestones.map((milestone, index) => (
                    <div key={milestone.id} className="flex gap-3">
                      <div className="flex flex-col items-center">
                        <div className="border-system bg-system/10 text-system flex h-8 w-8 items-center justify-center border-2 text-sm font-medium">
                          {index + 1}
                        </div>
                        {index < formData.milestones.length - 1 && (
                          <div className="bg-border my-1 w-0.5 flex-1" />
                        )}
                      </div>
                      <Card className="bg-card border-border flex-1">
                        <CardContent className="p-4">
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <h4 className="font-medium">{milestone.title}</h4>
                              {milestone.description && (
                                <p className="text-muted-foreground mt-1 text-sm">
                                  {milestone.description}
                                </p>
                              )}
                              {milestone.targetDate && (
                                <div className="text-muted-foreground mt-2 flex items-center gap-2 text-sm">
                                  <Calendar className="size-4" />
                                  {new Date(
                                    milestone.targetDate,
                                  ).toLocaleDateString()}
                                </div>
                              )}
                            </div>
                            <div className="flex gap-2">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => editMilestone(milestone)}
                              >
                                <Pencil className="size-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => removeMilestone(milestone.id)}
                              >
                                <Trash2 className="text-destructive size-4" />
                              </Button>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Step 5: Review */}
          {currentStep === 4 && (
            <div className="space-y-6">
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg">Basic Information</CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(0)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="mr-2 size-4" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div>
                    <h3 className="text-xl font-semibold">{formData.title}</h3>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm font-medium">
                      Problem Statement
                    </p>
                    <p className="mt-1 text-sm">{formData.problemStatement}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground text-sm font-medium">
                      Solution
                    </p>
                    <p className="mt-1 text-sm">{formData.solution}</p>
                  </div>
                  <div className="grid grid-cols-1 gap-4 pt-2 sm:grid-cols-2">
                    {formData.projectType && (
                      <div>
                        <p className="text-muted-foreground text-sm font-medium">
                          Project Type
                        </p>
                        <p className="mt-1 text-sm">
                          {projectTypes.find(
                            (pt) => pt.id === formData.projectType,
                          )?.label || formData.projectType}
                        </p>
                      </div>
                    )}
                    {formData.duration && (
                      <div>
                        <p className="text-muted-foreground text-sm font-medium">
                          Duration
                        </p>
                        <p className="mt-1 text-sm">
                          {durations.find((d) => d.id === formData.duration)
                            ?.label || formData.duration}
                        </p>
                      </div>
                    )}
                  </div>
                  <div className="grid grid-cols-1 gap-4 pt-2 sm:grid-cols-2">
                    <div>
                      <p className="text-muted-foreground text-sm font-medium">
                        Categories
                      </p>
                      <div className="mt-1 flex flex-wrap gap-1.5">
                        {formData.categories.map((catId) => {
                          const label =
                            catId === "OTHER"
                              ? formData.otherCategory || "Other"
                              : categories.find((c) => c.id === catId)?.label ||
                                catId.replace("_", " ");
                          return (
                            <Badge
                              key={catId}
                              variant="secondary"
                              className="text-xs"
                            >
                              {label}
                            </Badge>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                  {formData.targetAudience && (
                    <div>
                      <p className="text-muted-foreground text-sm font-medium">
                        Target Audience
                      </p>
                      <p className="mt-1 text-sm">{formData.targetAudience}</p>
                    </div>
                  )}
                  {formData.expectedImpact && (
                    <div>
                      <p className="text-muted-foreground text-sm font-medium">
                        Expected Impact
                      </p>
                      <p className="mt-1 text-sm">{formData.expectedImpact}</p>
                    </div>
                  )}
                  {formData.tags.length > 0 && (
                    <div>
                      <p className="text-muted-foreground mb-2 text-sm font-medium">
                        Tags
                      </p>
                      <div className="flex flex-wrap gap-2">
                        {formData.tags.map((tag) => (
                          <Badge key={tag} variant="secondary">
                            {tag}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Target className="text-system size-5" />
                    Resources ({formData.resources.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(1)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="mr-2 size-4" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.resources.length === 0 ? (
                    <p className="text-muted-foreground text-sm">
                      No resources defined
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {formData.resources.map((resource) => (
                        <div
                          key={resource.id}
                          className="bg-muted/30 flex items-center justify-between rounded-lg p-3"
                        >
                          <div>
                            <p className="text-sm font-medium">
                              {resource.name}
                            </p>
                            <p className="text-muted-foreground text-xs">
                              Qty: {resource.quantity}
                              {resource.estimatedCost &&
                                ` • One-time: $${resource.estimatedCost}`}
                              {resource.recurringCost &&
                                resource.recurringIntervalDays &&
                                ` • Recurring: $${resource.recurringCost}/${resource.recurringIntervalDays}d ($${toMonthlyRate(Number(resource.recurringCost), resource.recurringIntervalDays).toFixed(2)}/mo)`}
                            </p>
                          </div>
                          {resource.isRequired && (
                            <Badge variant="secondary" className="text-xs">
                              Required
                            </Badge>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Financial Projections */}
              {formData.resources.length > 0 &&
                (() => {
                  const totalOneTime = formData.resources.reduce(
                    (sum, r) =>
                      sum + (Number(r.estimatedCost) || 0) * r.quantity,
                    0,
                  );
                  const totalMonthlyRecurring = formData.resources.reduce(
                    (sum, r) =>
                      sum +
                      (r.recurringCost && r.recurringIntervalDays
                        ? toMonthlyRate(
                            Number(r.recurringCost),
                            r.recurringIntervalDays,
                          )
                        : 0),
                    0,
                  );
                  const projectMonths = durationToMonths(formData.duration);
                  const totalProjectedRecurring = projectMonths
                    ? totalMonthlyRecurring * projectMonths
                    : null;
                  const totalProjectedCost =
                    totalProjectedRecurring !== null
                      ? totalOneTime + totalProjectedRecurring
                      : null;

                  if (totalOneTime === 0 && totalMonthlyRecurring === 0)
                    return null;

                  return (
                    <Card className="bg-card border-border">
                      <CardHeader className="pb-3">
                        <CardTitle className="flex items-center gap-2 text-lg">
                          <Target className="text-system size-5" />
                          Financial Projections
                        </CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                          <div className="bg-muted/30 rounded-lg p-3">
                            <p className="text-muted-foreground text-xs">
                              Total One-Time Costs
                            </p>
                            <p className="text-lg font-semibold">
                              ${totalOneTime.toFixed(2)}
                            </p>
                          </div>
                          {totalMonthlyRecurring > 0 && (
                            <div className="bg-muted/30 rounded-lg p-3">
                              <p className="text-muted-foreground text-xs">
                                Total Monthly Recurring
                              </p>
                              <p className="text-lg font-semibold">
                                ${totalMonthlyRecurring.toFixed(2)}/mo
                              </p>
                            </div>
                          )}
                          {totalProjectedCost !== null &&
                            totalProjectedRecurring !== null &&
                            totalMonthlyRecurring > 0 && (
                              <>
                                <div className="bg-muted/30 rounded-lg p-3">
                                  <p className="text-muted-foreground text-xs">
                                    Projected Recurring (
                                    {
                                      durations.find(
                                        (d) => d.id === formData.duration,
                                      )?.label
                                    }
                                    )
                                  </p>
                                  <p className="text-lg font-semibold">
                                    ${totalProjectedRecurring.toFixed(2)}
                                  </p>
                                </div>
                                <div className="bg-system/10 border-system/30 rounded-lg border p-3">
                                  <p className="text-muted-foreground text-xs">
                                    Total Projected Cost
                                  </p>
                                  <p className="text-system text-lg font-semibold">
                                    ${totalProjectedCost.toFixed(2)}
                                  </p>
                                </div>
                              </>
                            )}
                          {totalProjectedCost === null &&
                            totalMonthlyRecurring > 0 && (
                              <div className="bg-muted/30 col-span-2 rounded-lg p-3 text-center">
                                <p className="text-muted-foreground text-xs">
                                  Set a project duration to see total projected
                                  costs
                                </p>
                              </div>
                            )}
                        </div>
                      </CardContent>
                    </Card>
                  );
                })()}

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Users className="text-system size-5" />
                    Team Roles ({formData.roles.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(2)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="mr-2 size-4" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.roles.length === 0 ? (
                    <p className="text-muted-foreground text-sm">
                      No roles defined
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {formData.roles.map((role) => (
                        <div
                          key={role.id}
                          className="bg-muted/30 flex items-center justify-between rounded-lg p-3"
                        >
                          <div>
                            <p className="text-sm font-medium">{role.title}</p>
                            <p className="text-muted-foreground text-xs">
                              {role.compensationModel &&
                                compensationModels.find(
                                  (m) => m.id === role.compensationModel,
                                )?.label}
                              {role.shareAmount &&
                                ` • ${role.shareAmount} shares`}
                              {role.equityPercent &&
                                ` • ${role.equityPercent}% project-token allocation`}
                            </p>
                          </div>
                          {role.isOpenForApplications && (
                            <Badge variant="secondary" className="text-xs">
                              Open
                            </Badge>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Flag className="text-system size-5" />
                    Milestones ({formData.milestones.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(3)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="mr-2 size-4" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.milestones.length === 0 ? (
                    <p className="text-muted-foreground text-sm">
                      No milestones defined
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {formData.milestones.map((milestone, index) => (
                        <div
                          key={milestone.id}
                          className="bg-muted/30 flex items-start gap-3 rounded-lg p-3"
                        >
                          <div className="border-system bg-system/10 text-system flex h-6 w-6 flex-shrink-0 items-center justify-center border-2 text-xs font-medium">
                            {index + 1}
                          </div>
                          <div className="flex-1">
                            <p className="text-sm font-medium">
                              {milestone.title}
                            </p>
                            {milestone.targetDate && (
                              <p className="text-muted-foreground text-xs">
                                {new Date(
                                  milestone.targetDate,
                                ).toLocaleDateString()}
                              </p>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>

              {errors.submit && (
                <Card className="bg-destructive/10 border-destructive">
                  <CardContent className="py-4">
                    <p role="alert" className="text-destructive text-sm">
                      {errors.submit}
                    </p>
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </div>

        {/* Navigation Buttons */}
        <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <Button
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            onClick={goToPreviousStep}
            disabled={currentStep === 0}
          >
            <ArrowLeft className="mr-2 size-4" />
            Previous
          </Button>

          {currentStep < steps.length - 1 ? (
            <Button
              variant="default"
              className="min-h-11 w-full sm:w-auto"
              onClick={goToNextStep}
            >
              Next
              <ArrowRight className="ml-2 size-4" />
            </Button>
          ) : (
            <div className="flex w-full flex-col-reverse gap-2 sm:w-auto sm:flex-row">
              {!custodyCheckPending && !custodyReady && (
                <Button
                  variant="outline"
                  className="min-h-11 w-full sm:w-auto"
                  asChild
                >
                  <Link href="/settings/verification?returnTo=%2Fprojects%2Fcreate">
                    Review setup
                  </Link>
                </Button>
              )}
              <Button
                variant="default"
                className="bg-system text-system-foreground hover:bg-system/90 min-h-11 w-full font-semibold sm:w-auto"
                onClick={handleSubmit}
                disabled={isSubmitting || custodyCheckPending || !custodyReady}
                aria-describedby="create-project-readiness"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 size-4 animate-spin" />
                    {recovery ? "Retrying..." : "Creating..."}
                  </>
                ) : custodyReady ? (
                  recovery ? (
                    "Retry remaining setup"
                  ) : (
                    "Create Project"
                  )
                ) : (
                  "Secure setup required"
                )}
              </Button>
            </div>
          )}
        </div>

        {/* Info Note */}
        {currentStep === 0 && (
          <div className="bg-muted/30 border-border mt-6 flex items-start gap-3 rounded-lg border p-4">
            <Info className="text-system mt-0.5 size-5 flex-shrink-0" />
            <div className="text-muted-foreground text-sm">
              <p>
                This wizard will guide you through creating a comprehensive
                project. You can skip optional steps and come back to edit them
                later.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
