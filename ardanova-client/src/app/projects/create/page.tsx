"use client";

import { useEffect, useRef, useState, type ChangeEvent } from "react";
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
  ImagePlus,
  Paperclip,
  Wallet,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import {
  describeIdentityStep,
  reviewContactHref,
} from "~/components/azoa/custodial-account-card";
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
  PROJECT_FUNDING_UNIT,
  PROJECT_MEDIA_ACCEPT_ATTRIBUTE,
  PROJECT_MEDIA_MAX_FILES,
  PROJECT_MEDIA_MAX_FILE_BYTES,
  PROJECT_SUMMARY_MAX_LENGTH,
  classifyProjectMediaKind,
  createEmptyProjectFormData,
  describeFundingGoalInPlatformUnits,
  evaluateValueActionAccount,
  formatUsdAmount,
  isProjectCreateCompensationModel,
  isProjectRoleValueBearing,
  parseFundingGoalDollars,
  parseProjectCreateDraft,
  readArdaUsdRate,
  serializeProjectCreateDraft,
  serializeProjectMediaItems,
  toProjectRoleOpportunityInput,
  toOptionalIsoDate,
  validateProjectSummary,
  type ProjectCreateCompensationModel,
  type ProjectCreateFormData as WizardFormData,
  type ProjectCreateMilestone as Milestone,
  type ProjectCreateRecovery,
  type ProjectCreateResource as Resource,
  type ProjectCreateRole as Role,
  type ProjectMediaItem,
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
  FIXED_SHARES: "Fixed project units",
  HOURLY_SHARES: "Hourly project units",
  EQUITY_PERCENT: "Project-unit allocation percentage",
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
  { id: 4, name: "Funding" },
  { id: 5, name: "Review" },
];

const FUNDING_STEP = 4;

/** What the owner declares on the funding step; whole units, no fractions to reason about. */
interface FundingDeclarationDraft {
  enabled: boolean;
  stakeName: string;
  unitLabel: string;
  totalUnits: string;
  sponsorUnits: string;
  workerUnits: string;
  disclosure: string;
}

const emptyFundingDeclaration: FundingDeclarationDraft = {
  enabled: true,
  stakeName: "",
  unitLabel: "",
  totalUnits: "1000",
  sponsorUnits: "600",
  workerUnits: "300",
  disclosure: "",
};

/** A short label derived from the project's own name, so nobody is asked to invent a ticker. */
function suggestUnitLabel(title: string): string {
  const initials = title
    .split(/\s+/u)
    .map((word) => word.replace(/[^A-Za-z0-9]/gu, "").charAt(0))
    .join("")
    .toUpperCase();
  const letters = title.replace(/[^A-Za-z0-9]/gu, "").toUpperCase();
  const candidate = initials.length >= 2 ? initials : letters;
  return candidate.length >= 2 ? candidate.slice(0, 8) : "UNIT";
}

/** Whole non-negative counts only; anything else is a declaration error, not a rounded number. */
function parseWholeUnits(value: string): number | undefined {
  const trimmed = value.trim();
  if (!/^\d+$/u.test(trimmed)) return undefined;
  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : undefined;
}

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
  // The declaration is deliberately NOT part of the saved draft: its shape belongs to the
  // project-create contract, which this step does not own. A reload sends the owner to the
  // project's Funding tab, which offers the same declaration and the same server job.
  const [funding, setFunding] = useState<FundingDeclarationDraft>(
    emptyFundingDeclaration,
  );
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

  // Media state
  const mediaInputRef = useRef<HTMLInputElement | null>(null);
  const [isUploadingMedia, setIsUploadingMedia] = useState(false);
  const getUploadUrlsMutation = api.attachment.getUploadUrls.useMutation();

  // The live platform-unit rate behind the goal's derived caption. The ask itself is in
  // dollars and needs no rate; a failure here only softens the caption to the launch
  // reference, which it says out loud.
  const treasuryStatus = api.treasury.getStatus.useQuery(undefined, {
    enabled: Boolean(session?.user?.id),
    retry: false,
    staleTime: 5 * 60_000,
  });
  const ardaUsdRate = readArdaUsdRate(treasuryStatus.data);

  const setMediaError = (message: string | null) =>
    setErrors((previous) => {
      const next = { ...previous };
      if (message) next.media = message;
      else delete next.media;
      return next;
    });

  const handleMediaSelected = async (
    event: ChangeEvent<HTMLInputElement>,
  ) => {
    const chosen = Array.from(event.target.files ?? []);
    // Reset immediately so re-picking the same file still fires a change event.
    event.target.value = "";
    if (chosen.length === 0) return;

    const remainingSlots = PROJECT_MEDIA_MAX_FILES - formData.media.length;
    if (remainingSlots <= 0) {
      setMediaError(`You can attach up to ${PROJECT_MEDIA_MAX_FILES} files.`);
      return;
    }

    const problems: string[] = [];
    if (chosen.length > remainingSlots) {
      problems.push(`Only ${remainingSlots} more file(s) fit on this project.`);
    }

    const maxMegabytes = Math.floor(PROJECT_MEDIA_MAX_FILE_BYTES / 1_000_000);
    const accepted = chosen.slice(0, remainingSlots).filter((file) => {
      if (!classifyProjectMediaKind(file.type)) {
        problems.push(`${file.name}: that file type is not supported.`);
        return false;
      }
      if (file.size > PROJECT_MEDIA_MAX_FILE_BYTES) {
        problems.push(`${file.name}: larger than ${maxMegabytes}MB.`);
        return false;
      }
      return true;
    });

    if (accepted.length === 0) {
      setMediaError(problems.join(" "));
      return;
    }

    setIsUploadingMedia(true);
    const stored: ProjectMediaItem[] = [];
    try {
      const issued = await getUploadUrlsMutation.mutateAsync({
        files: accepted.map((file) => ({
          fileName: file.name,
          contentType: file.type,
          fileSize: file.size,
          folder: "projects",
        })),
      });

      const uploads = issued.uploads ?? [];
      if (uploads.length !== accepted.length) {
        throw new Error(
          "Storage issued a different number of upload links than files selected.",
        );
      }

      for (let index = 0; index < accepted.length; index += 1) {
        const file = accepted[index]!;
        const target = uploads[index]!;
        const response = await fetch(target.uploadUrl, {
          method: "PUT",
          headers: { "Content-Type": file.type, ...(target.headers ?? {}) },
          body: file,
        });

        if (!response.ok) {
          problems.push(
            `${file.name}: upload failed (${response.status}). If this environment stores files locally, presigned uploads are not available.`,
          );
          continue;
        }

        stored.push({
          bucketPath: target.bucketPath,
          url: target.publicUrl ?? "",
          fileName: file.name,
          contentType: file.type,
          kind: classifyProjectMediaKind(file.type) ?? "document",
        });
      }
    } catch (error) {
      problems.push(
        error instanceof Error
          ? error.message
          : "The files could not be uploaded.",
      );
    } finally {
      setIsUploadingMedia(false);
    }

    if (stored.length > 0) {
      setFormData((previous) => ({
        ...previous,
        media: [...previous.media, ...stored],
      }));
    }
    setMediaError(problems.length > 0 ? problems.join(" ") : null);
  };

  const removeMedia = (bucketPath: string) => {
    setFormData((previous) => ({
      ...previous,
      media: previous.media.filter((item) => item.bucketPath !== bucketPath),
    }));
    setMediaError(null);
  };

  const fundingGoalDollars = (() => {
    try {
      return parseFundingGoalDollars(formData.fundingGoal);
    } catch {
      return undefined;
    }
  })();

  const resolvedStakeName = (
    funding.stakeName.trim() || `${formData.title.trim()} Stake`
  ).slice(0, 32);
  const resolvedUnitLabel = (
    funding.unitLabel.trim() || suggestUnitLabel(formData.title)
  )
    .toUpperCase()
    .slice(0, 8);

  const createMutation = api.project.create.useMutation();
  const addResourceMutation = api.project.addResource.useMutation();
  const addMilestoneMutation = api.project.addMilestone.useMutation();
  const addMemberMutation = api.project.addMember.useMutation();
  const createOpportunityMutation = api.opportunity.create.useMutation();
  const deleteProjectMutation = api.project.delete.useMutation();
  const provisionFundingMutation =
    api.projectTokens.provisionFunding.useMutation();
  const valueAccountCheckPending =
    Boolean(session?.user?.id) &&
    !custodyStatus.data &&
    custodyStatus.isPending;
  // Three-way, never a boolean: an account held for a human decision has no setup left to
  // finish, so it must never read the "complete account setup" instruction. Only "ready"
  // opens a value-bearing action; the other states decide only the words.
  const accountEvaluation = evaluateValueActionAccount(custodyStatus.data, {
    loadFailed: Boolean(custodyStatus.error),
  });
  const valueAccountReady = accountEvaluation.state === "ready";
  const valueAccountUnderReview =
    !valueAccountCheckPending && accountEvaluation.state === "under-review";
  const reviewContact = valueAccountUnderReview ? reviewContactHref() : null;

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

      const summaryProblem = validateProjectSummary(formData.summary);
      if (summaryProblem) newErrors.summary = summaryProblem;

      try {
        parseFundingGoalDollars(formData.fundingGoal);
      } catch (error) {
        newErrors.fundingGoal =
          error instanceof Error ? error.message : "Enter a valid amount";
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

    // Step 4: Funding - the declaration the server re-checks before it creates anything
    if (step === FUNDING_STEP && funding.enabled) {
      const stakeName = resolvedStakeName;
      if (stakeName.length < 3 || stakeName.length > 32) {
        newErrors.fundingStakeName =
          "Give the stake a name between 3 and 32 characters";
      }

      const unitLabel = resolvedUnitLabel;
      if (!/^[A-Za-z0-9]{2,8}$/u.test(unitLabel)) {
        newErrors.fundingUnitLabel = "Use 2 to 8 letters or digits";
      }

      const total = parseWholeUnits(funding.totalUnits);
      const forSponsors = parseWholeUnits(funding.sponsorUnits);
      const forWorkers = parseWholeUnits(funding.workerUnits);

      if (total === undefined || total < 1) {
        newErrors.fundingTotalUnits = "Enter a whole number of units, at least 1";
      }
      if (forSponsors === undefined) {
        newErrors.fundingSponsorUnits = "Enter a whole number of units";
      }
      if (forWorkers === undefined) {
        newErrors.fundingWorkerUnits = "Enter a whole number of units";
      }
      if (
        total !== undefined &&
        forSponsors !== undefined &&
        forWorkers !== undefined
      ) {
        if (forSponsors < 1) {
          // What the project is asking for is spread across the SPONSOR units alone, so an ask
          // with none set aside is an ask nobody can answer: there is nothing to sell, no
          // sponsorship can settle, and the workers' stake never becomes theirs. The server
          // refuses the same shape at provisioning.
          newErrors.fundingSponsorUnits =
            "Set aside at least one unit for sponsors. What you are asking for is spread across these units, so with none there is nothing for a sponsor to take";
        } else if (forSponsors + forWorkers > total) {
          newErrors.fundingSponsorUnits = `Sponsor and worker shares add up to ${forSponsors + forWorkers} units, more than the ${total} that exist`;
        }
      }

      const disclosure = funding.disclosure.trim();
      if (disclosure.length < 20 || disclosure.length > 2000) {
        newErrors.fundingDisclosure =
          "Write 20 to 2000 characters on what a stake means and when it becomes theirs";
      }

      if (fundingGoalDollars === undefined || fundingGoalDollars <= 0) {
        newErrors.fundingGoal =
          "Set a funding goal above zero on the Basic Info step";
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
        const summary = formData.summary.trim();
        // Media is written twice on purpose. `mediaPaths` is the typed manifest every
        // new reader uses; the three comma-separated columns stay populated so readers
        // written before the manifest existed keep rendering the same files.
        const mediaLocations = (kind: ProjectMediaItem["kind"]) =>
          formData.media
            .filter((item) => item.kind === kind && item.url)
            .map((item) => item.url)
            .join(",") || undefined;
        const projectResult: unknown = await createMutation.mutateAsync({
          title: formData.title,
          // The public one-liner. It carries the summary rather than a second copy of
          // the solution, which is what a card, a search result and a share preview
          // actually need.
          description: summary || formData.solution,
          summary: summary || undefined,
          problemStatement: formData.problemStatement,
          solution: formData.solution,
          categories: resolvedCategories,
          projectType: formData.projectType || undefined,
          duration: formData.duration || undefined,
          fundingGoal: parseFundingGoalDollars(formData.fundingGoal),
          images: mediaLocations("image"),
          videos: mediaLocations("video"),
          documents: mediaLocations("document"),
          mediaPaths: formData.media.length
            ? serializeProjectMediaItems(formData.media)
            : undefined,
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
      const paidRolesAwaitingSetup = valueAccountReady
        ? []
        : rolesToSave.filter(isProjectRoleValueBearing);
      const rolesEligibleToSave = valueAccountReady
        ? rolesToSave
        : rolesToSave.filter((role) => !isProjectRoleValueBearing(role));

      if (paidRolesAwaitingSetup.length > 0) {
        failedRoleIds.push(
          ...paidRolesAwaitingSetup.map((role) => role.id),
        );
        partialWarnings.push(
          valueAccountUnderReview
            ? "Paid role listings remain saved in this recovery draft until the review of your account finishes."
            : "Paid role listings remain saved in this recovery draft until account setup is complete.",
        );
      }

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

      // 5. Save only non-paid roles until account setup can authorize paid work.
      for (const role of rolesEligibleToSave) {
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

      // 6. Funding setup. Runs on the server: it creates the project's stake, resolves the
      // account funds land in, and registers the sponsor and worker allowances. Idempotent,
      // so a retry resumes rather than repeating. A failure here never rolls the project
      // back — the project is real either way, and the Funding tab offers the same job.
      if (funding.enabled && valueAccountReady) {
        const totalUnits = parseWholeUnits(funding.totalUnits);
        const sponsorUnits = parseWholeUnits(funding.sponsorUnits);
        const workerUnits = parseWholeUnits(funding.workerUnits);

        if (
          totalUnits === undefined ||
          sponsorUnits === undefined ||
          workerUnits === undefined ||
          fundingGoalDollars === undefined
        ) {
          partialWarnings.push(
            "Funding setup was skipped: the declaration was incomplete. Finish it on the project's Funding tab.",
          );
        } else {
          try {
            const provisioned = await provisionFundingMutation.mutateAsync({
              projectId: project.id,
              assetName: resolvedStakeName,
              unitName: resolvedUnitLabel,
              totalSupply: totalUnits,
              investorSupply: sponsorUnits,
              contributorSupply: workerUnits,
              fundingGoal: fundingGoalDollars,
              fundingGoalUnit: PROJECT_FUNDING_UNIT,
              disclosure: funding.disclosure.trim(),
              reservedPercentage: 0,
            });

            if (!provisioned.isFundable) {
              partialWarnings.push(
                `Funding setup: ${provisioned.setupMessage ?? "did not finish"}. Retry it on the project's Funding tab.`,
              );
            }
          } catch (e) {
            partialWarnings.push(
              `Funding setup: ${e instanceof Error ? e.message : "could not be completed"}. Retry it on the project's Funding tab.`,
            );
          }
        }
      } else if (funding.enabled && !valueAccountReady) {
        partialWarnings.push(
          valueAccountUnderReview
            ? "Funding setup waits for the review of your account to finish. The project is saved."
            : "Funding setup waits for your account setup to finish. The project is saved.",
        );
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

        toast.warning(
          paidRolesAwaitingSetup.length > 0
            ? "Project draft created; paid roles are waiting for account setup."
            : "Project created, but with some issues.",
          {
            description: `${partialWarnings.slice(0, 5).join(". ")}${partialWarnings.length > 5 ? `. ${partialWarnings.length - 5} more items remain.` : ""}`,
            duration: 8000,
          },
        );

        if (paidRolesAwaitingSetup.length > 0) {
          return;
        }
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
                  not create a duplicate project. Paid role listings stay on
                  hold until account setup is complete.
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
                  disabled={isSubmitting}
                >
                  {isSubmitting
                    ? "Saving..."
                    : valueAccountReady
                      ? "Retry remaining setup"
                      : "Save remaining non-paid setup"}
                </Button>
              </div>
            </div>
          </section>
        )}

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

                  <div>
                    <label
                      htmlFor="project-summary"
                      className="mb-2 block text-sm font-medium"
                    >
                      One-line summary
                    </label>
                    <input
                      type="text"
                      id="project-summary"
                      maxLength={PROJECT_SUMMARY_MAX_LENGTH}
                      aria-invalid={Boolean(errors.summary)}
                      aria-describedby={
                        errors.summary
                          ? "project-summary-error"
                          : "project-summary-help"
                      }
                      value={formData.summary}
                      onChange={(e) => handleChange("summary", e.target.value)}
                      placeholder="The one sentence someone reads before deciding to care"
                      className={cn(
                        "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                        errors.summary ? "border-destructive" : "border-border",
                      )}
                    />
                    <div className="mt-1 flex items-start justify-between gap-3">
                      {errors.summary ? (
                        <p
                          id="project-summary-error"
                          role="alert"
                          className="text-destructive text-sm"
                        >
                          {errors.summary}
                        </p>
                      ) : (
                        <p
                          id="project-summary-help"
                          className="text-muted-foreground text-sm"
                        >
                          Shown on cards and in search results. Optional, but it
                          is the first thing anyone reads.
                        </p>
                      )}
                      <span className="text-muted-foreground shrink-0 text-xs">
                        {formData.summary.length}/{PROJECT_SUMMARY_MAX_LENGTH}
                      </span>
                    </div>
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
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <ImagePlus className="text-system size-5" />
                    Photos, video and documents
                  </CardTitle>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Show the problem. Up to {PROJECT_MEDIA_MAX_FILES} files,{" "}
                    {Math.floor(PROJECT_MEDIA_MAX_FILE_BYTES / 1_000_000)}MB
                    each.
                  </p>
                </CardHeader>
                <CardContent className="space-y-4">
                  <input
                    ref={mediaInputRef}
                    id="project-media-input"
                    type="file"
                    multiple
                    accept={PROJECT_MEDIA_ACCEPT_ATTRIBUTE}
                    className="sr-only"
                    onChange={(event) => void handleMediaSelected(event)}
                  />
                  <div className="flex flex-wrap items-center gap-3">
                    <Button
                      type="button"
                      variant="outline"
                      className="min-h-11"
                      disabled={
                        isUploadingMedia ||
                        formData.media.length >= PROJECT_MEDIA_MAX_FILES
                      }
                      onClick={() => mediaInputRef.current?.click()}
                    >
                      {isUploadingMedia ? (
                        <Loader2
                          className="mr-2 size-4 animate-spin"
                          aria-hidden="true"
                        />
                      ) : (
                        <Plus className="mr-2 size-4" aria-hidden="true" />
                      )}
                      {isUploadingMedia ? "Uploading..." : "Add files"}
                    </Button>
                    <span
                      className="text-muted-foreground text-xs"
                      aria-live="polite"
                    >
                      {formData.media.length} of {PROJECT_MEDIA_MAX_FILES}{" "}
                      attached
                    </span>
                  </div>

                  {errors.media && (
                    <p role="alert" className="text-destructive text-sm">
                      {errors.media}
                    </p>
                  )}

                  {formData.media.length > 0 && (
                    <ul className="grid list-none grid-cols-2 gap-3 sm:grid-cols-4">
                      {formData.media.map((item) => (
                        <li
                          key={item.bucketPath}
                          className="border-border relative rounded-lg border-2 p-2"
                        >
                          {item.kind === "image" && item.url ? (
                            // next/image is skipped: bucket hosts are deployment-specific
                            // and are not in an images.remotePatterns allow-list.
                            // eslint-disable-next-line @next/next/no-img-element
                            <img
                              src={item.url}
                              alt={item.fileName}
                              className="h-24 w-full rounded object-cover"
                            />
                          ) : (
                            <div className="bg-muted/40 flex h-24 w-full flex-col items-center justify-center gap-1 rounded p-2 text-center">
                              <Paperclip
                                className="text-muted-foreground size-4"
                                aria-hidden="true"
                              />
                              <span className="text-xs break-all">
                                {item.kind}
                              </span>
                            </div>
                          )}
                          <p className="text-muted-foreground mt-1 truncate text-xs">
                            {item.fileName}
                          </p>
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            className="mt-1 min-h-11 w-full"
                            onClick={() => removeMedia(item.bucketPath)}
                          >
                            <Trash2 className="mr-2 size-4" aria-hidden="true" />
                            Remove
                          </Button>
                        </li>
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Wallet className="text-system size-5" />
                    The ask
                  </CardTitle>
                  <p className="text-muted-foreground mt-1 text-sm">
                    How much do you need to do this? Optional - you can set it
                    later.
                  </p>
                </CardHeader>
                <CardContent className="space-y-2">
                  <label
                    htmlFor="project-funding-goal"
                    className="mb-2 block text-sm font-medium"
                  >
                    Funding goal
                  </label>
                  <div className="flex items-stretch gap-2">
                    <input
                      type="text"
                      inputMode="decimal"
                      id="project-funding-goal"
                      aria-invalid={Boolean(errors.fundingGoal)}
                      aria-describedby={
                        errors.fundingGoal
                          ? "project-funding-goal-error"
                          : "project-funding-goal-help"
                      }
                      value={formData.fundingGoal}
                      onChange={(e) =>
                        handleChange("fundingGoal", e.target.value)
                      }
                      placeholder="0.00"
                      className={cn(
                        "bg-muted/50 focus-visible:ring-ring/50 flex-1 rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                        errors.fundingGoal
                          ? "border-destructive"
                          : "border-border",
                      )}
                    />
                    <span className="border-border bg-muted/30 text-muted-foreground flex items-center rounded-lg border-2 px-4 font-mono text-sm">
                      USD
                    </span>
                  </div>
                  {errors.fundingGoal ? (
                    <p
                      id="project-funding-goal-error"
                      role="alert"
                      className="text-destructive text-sm"
                    >
                      {errors.fundingGoal}
                    </p>
                  ) : (
                    <p
                      id="project-funding-goal-help"
                      className="text-muted-foreground text-sm"
                      aria-live="polite"
                    >
                      {fundingGoalDollars === undefined
                        ? "Enter the amount you are asking for, in dollars."
                        : `Sponsors see ${formatUsdAmount(fundingGoalDollars)} — ${describeFundingGoalInPlatformUnits(fundingGoalDollars, ardaUsdRate)}.`}
                    </p>
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
            <div className="space-y-6">
              {/* Account setup is asked for here, at the step where a role first carries
                  money - not before the creator has written anything. */}
            <section
              className="border-foreground bg-card border-2"
              aria-labelledby="project-readiness-title"
              aria-live="polite"
            >
              <div className="flex flex-col gap-4 p-4 sm:flex-row sm:items-start sm:justify-between sm:p-5">
                <div className="flex items-start gap-3">
                  <span className="border-foreground bg-system/10 text-system flex size-10 shrink-0 items-center justify-center border-2">
                    {valueAccountCheckPending ? (
                      <Loader2 className="size-5 animate-spin" aria-hidden="true" />
                    ) : valueAccountReady ? (
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
                        {valueAccountUnderReview
                          ? "Account review status"
                          : "Account setup status"}
                      </h2>
                      <Badge variant={valueAccountReady ? "success" : "outline"}>
                        {valueAccountCheckPending
                          ? "Checking"
                          : valueAccountReady
                            ? "Complete"
                            : valueAccountUnderReview
                              ? "In review"
                              : "Setup needed"}
                      </Badge>
                    </div>
                    <p
                      id="team-roles-readiness"
                      className="text-muted-foreground mt-2 max-w-2xl text-sm leading-relaxed"
                    >
                      {valueAccountCheckPending
                        ? "Checking the account setup needed for funding and paid work."
                        : valueAccountReady
                          ? "Account setup is complete. You can publish paid role listings when the project is ready; the server checks every value-bearing action again."
                          : valueAccountUnderReview
                            ? // The shared contract's own words, matching the server's refusal
                              // text. The node's `unavailableReason` is a provider diagnostic
                              // and is never printed at a creator.
                              accountEvaluation.message
                            : accountEvaluation.state === "service-unavailable"
                              ? "We could not confirm account setup. You can still create a project draft; funding and paid role listings remain unavailable until status is confirmed."
                              : "You can create a project draft and collaborate now. Complete account setup before funding a project, publishing paid roles, or receiving money or rewards."}
                    </p>
                  </div>
                </div>

                {!valueAccountCheckPending && !valueAccountReady && (
                  <div className="flex w-full flex-col gap-2 sm:w-auto">
                    {valueAccountUnderReview ? (
                      reviewContact && (
                        <Button className="min-h-11 w-full sm:w-auto" asChild>
                          <a href={reviewContact}>
                            <ShieldCheck className="size-4" aria-hidden="true" />
                            Contact the review team
                          </a>
                        </Button>
                      )
                    ) : (
                      <Button className="min-h-11 w-full sm:w-auto" asChild>
                        <Link href="/settings/verification?returnTo=%2Fprojects%2Fcreate">
                          <ShieldCheck className="size-4" aria-hidden="true" />
                          Review account setup
                        </Link>
                      </Button>
                    )}
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
                  <span className="text-sm font-medium">Account setup</span>
                  <span className="font-mono text-xs uppercase">
                    {custodyStatus.data?.identityReady ? "Ready" : "Not confirmed"}
                  </span>
                </div>
                <div className="border-foreground flex items-center justify-between gap-3 border-b-2 px-4 py-3 sm:border-r-2 sm:border-b-0">
                  <span className="text-sm font-medium">Identity check</span>
                  <span className="font-mono text-xs uppercase">
                    {describeIdentityStep(custodyStatus.data?.kycStatus)}
                  </span>
                </div>
                <div className="flex items-center justify-between gap-3 px-4 py-3">
                  <span className="text-sm font-medium">Managed payment account</span>
                  <span className="font-mono text-xs uppercase">
                    {custodyStatus.data?.walletReady ? "Ready" : "Not confirmed"}
                  </span>
                </div>
              </div>

              {!valueAccountReady && (
                <p className="border-foreground text-muted-foreground border-t-2 px-4 py-3 text-xs leading-relaxed sm:px-5">
                  {valueAccountUnderReview
                    ? "Keep shaping and saving this project draft. Only funding and paid role listings wait for the review decision."
                    : "Keep shaping and saving this project draft. Visiting account setup will not erase it in this tab; only funding and paid role listings wait for completion."}
                </p>
              )}
            </section>

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
            </div>
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

          {/* Step 5: Funding */}
          {currentStep === FUNDING_STEP && (
            <div className="space-y-6">
              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Wallet className="text-system size-5" />
                    Stake in this project
                  </CardTitle>
                  <p className="text-muted-foreground mt-1 text-sm">
                    A stake is a share of this project. Sponsors receive stake
                    when they put funds in; workers receive stake for the work
                    they do. You decide how much exists and how it is divided.
                  </p>
                </CardHeader>
                <CardContent className="space-y-4">
                  <label className="flex items-start gap-3 text-sm">
                    <input
                      type="checkbox"
                      checked={funding.enabled}
                      onChange={(e) =>
                        setFunding((prev) => ({
                          ...prev,
                          enabled: e.target.checked,
                        }))
                      }
                      className="mt-1 size-4"
                    />
                    <span>
                      Let people sponsor this project and earn stake for working
                      on it.
                      <span className="text-muted-foreground block text-xs">
                        Leave this off to publish without funding. You can set it
                        up later from the project&rsquo;s Funding tab.
                      </span>
                    </span>
                  </label>

                  {funding.enabled && (
                    <>
                      <div className="grid gap-4 sm:grid-cols-2">
                        <div>
                          <label
                            htmlFor="funding-stake-name"
                            className="mb-2 block text-sm font-medium"
                          >
                            Name of the stake
                          </label>
                          <input
                            type="text"
                            id="funding-stake-name"
                            maxLength={32}
                            aria-invalid={Boolean(errors.fundingStakeName)}
                            value={funding.stakeName}
                            onChange={(e) =>
                              setFunding((prev) => ({
                                ...prev,
                                stakeName: e.target.value,
                              }))
                            }
                            placeholder={resolvedStakeName}
                            className={cn(
                              "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                              errors.fundingStakeName
                                ? "border-destructive"
                                : "border-border",
                            )}
                          />
                          {errors.fundingStakeName ? (
                            <p role="alert" className="text-destructive mt-1 text-sm">
                              {errors.fundingStakeName}
                            </p>
                          ) : (
                            <p className="text-muted-foreground mt-1 text-sm">
                              Leave blank to use &ldquo;{resolvedStakeName}
                              &rdquo;.
                            </p>
                          )}
                        </div>

                        <div>
                          <label
                            htmlFor="funding-unit-label"
                            className="mb-2 block text-sm font-medium"
                          >
                            Short label
                          </label>
                          <input
                            type="text"
                            id="funding-unit-label"
                            maxLength={8}
                            aria-invalid={Boolean(errors.fundingUnitLabel)}
                            value={funding.unitLabel}
                            onChange={(e) =>
                              setFunding((prev) => ({
                                ...prev,
                                unitLabel: e.target.value,
                              }))
                            }
                            placeholder={resolvedUnitLabel}
                            className={cn(
                              "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 font-mono uppercase focus:ring-2 focus:outline-none",
                              errors.fundingUnitLabel
                                ? "border-destructive"
                                : "border-border",
                            )}
                          />
                          {errors.fundingUnitLabel ? (
                            <p role="alert" className="text-destructive mt-1 text-sm">
                              {errors.fundingUnitLabel}
                            </p>
                          ) : (
                            <p className="text-muted-foreground mt-1 text-sm">
                              Shown next to a quantity, e.g. &ldquo;12{" "}
                              {resolvedUnitLabel}&rdquo;.
                            </p>
                          )}
                        </div>
                      </div>

                      <div className="grid gap-4 sm:grid-cols-3">
                        <div>
                          <label
                            htmlFor="funding-total-units"
                            className="mb-2 block text-sm font-medium"
                          >
                            Units in total
                          </label>
                          <input
                            type="text"
                            inputMode="numeric"
                            id="funding-total-units"
                            aria-invalid={Boolean(errors.fundingTotalUnits)}
                            value={funding.totalUnits}
                            onChange={(e) =>
                              setFunding((prev) => ({
                                ...prev,
                                totalUnits: e.target.value,
                              }))
                            }
                            className={cn(
                              "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                              errors.fundingTotalUnits
                                ? "border-destructive"
                                : "border-border",
                            )}
                          />
                          {errors.fundingTotalUnits && (
                            <p role="alert" className="text-destructive mt-1 text-sm">
                              {errors.fundingTotalUnits}
                            </p>
                          )}
                        </div>

                        <div>
                          <label
                            htmlFor="funding-sponsor-units"
                            className="mb-2 block text-sm font-medium"
                          >
                            For sponsors
                          </label>
                          <input
                            type="text"
                            inputMode="numeric"
                            id="funding-sponsor-units"
                            aria-invalid={Boolean(errors.fundingSponsorUnits)}
                            value={funding.sponsorUnits}
                            onChange={(e) =>
                              setFunding((prev) => ({
                                ...prev,
                                sponsorUnits: e.target.value,
                              }))
                            }
                            className={cn(
                              "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                              errors.fundingSponsorUnits
                                ? "border-destructive"
                                : "border-border",
                            )}
                          />
                          {errors.fundingSponsorUnits && (
                            <p role="alert" className="text-destructive mt-1 text-sm">
                              {errors.fundingSponsorUnits}
                            </p>
                          )}
                        </div>

                        <div>
                          <label
                            htmlFor="funding-worker-units"
                            className="mb-2 block text-sm font-medium"
                          >
                            For workers
                          </label>
                          <input
                            type="text"
                            inputMode="numeric"
                            id="funding-worker-units"
                            aria-invalid={Boolean(errors.fundingWorkerUnits)}
                            value={funding.workerUnits}
                            onChange={(e) =>
                              setFunding((prev) => ({
                                ...prev,
                                workerUnits: e.target.value,
                              }))
                            }
                            className={cn(
                              "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                              errors.fundingWorkerUnits
                                ? "border-destructive"
                                : "border-border",
                            )}
                          />
                          {errors.fundingWorkerUnits && (
                            <p role="alert" className="text-destructive mt-1 text-sm">
                              {errors.fundingWorkerUnits}
                            </p>
                          )}
                        </div>
                      </div>

                      <div>
                        <label
                          htmlFor="funding-disclosure"
                          className="mb-2 block text-sm font-medium"
                        >
                          What a stake in this project means
                        </label>
                        <textarea
                          id="funding-disclosure"
                          rows={4}
                          maxLength={2000}
                          aria-invalid={Boolean(errors.fundingDisclosure)}
                          value={funding.disclosure}
                          onChange={(e) =>
                            setFunding((prev) => ({
                              ...prev,
                              disclosure: e.target.value,
                            }))
                          }
                          placeholder="Say what a supporter is getting, when their stake becomes fully theirs, and what happens if the project does not finish."
                          className={cn(
                            "bg-muted/50 focus-visible:ring-ring/50 w-full rounded-lg border-2 px-4 py-3 focus:ring-2 focus:outline-none",
                            errors.fundingDisclosure
                              ? "border-destructive"
                              : "border-border",
                          )}
                        />
                        {errors.fundingDisclosure ? (
                          <p role="alert" className="text-destructive mt-1 text-sm">
                            {errors.fundingDisclosure}
                          </p>
                        ) : (
                          <p className="text-muted-foreground mt-1 text-sm">
                            Everyone sees this before they commit funds.
                          </p>
                        )}
                      </div>

                      <div className="bg-muted/30 border-border flex items-start gap-3 rounded-lg border p-4">
                        <ShieldCheck
                          className="text-system mt-0.5 size-5 flex-shrink-0"
                          aria-hidden="true"
                        />
                        <div className="text-muted-foreground text-sm">
                          <p>
                            Asking for{" "}
                            <span className="text-foreground font-medium">
                              {fundingGoalDollars === undefined
                                ? "no amount yet"
                                : formatUsdAmount(fundingGoalDollars)}
                            </span>
                            {fundingGoalDollars === undefined
                              ? " — set the goal on the Basic Info step."
                              : ` (${describeFundingGoalInPlatformUnits(fundingGoalDollars, ardaUsdRate)}).`}
                          </p>
                          {!valueAccountReady && (
                            <p className="mt-2">
                              {valueAccountUnderReview
                                ? "Funding setup runs once the review of your account finishes. The project itself still saves."
                                : "Funding setup runs once your account setup is complete. The project itself still saves."}
                            </p>
                          )}
                        </div>
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            </div>
          )}

          {/* Step 6: Review */}
          {currentStep === 5 && (
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
                    {formData.summary.trim() && (
                      <p className="text-muted-foreground mt-1 text-sm">
                        {formData.summary.trim()}
                      </p>
                    )}
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
                  {fundingGoalDollars !== undefined && (
                    <div>
                      <p className="text-muted-foreground text-sm font-medium">
                        Funding goal
                      </p>
                      <p className="mt-1 text-sm">
                        {formatUsdAmount(fundingGoalDollars)} -{" "}
                        {describeFundingGoalInPlatformUnits(
                          fundingGoalDollars,
                          ardaUsdRate,
                        )}
                      </p>
                    </div>
                  )}
                  {formData.media.length > 0 && (
                    <div>
                      <p className="text-muted-foreground mb-2 text-sm font-medium">
                        Media ({formData.media.length})
                      </p>
                      <ul className="flex list-none flex-wrap gap-2">
                        {formData.media.map((item) => (
                          <li key={item.bucketPath}>
                            <Badge variant="secondary" className="text-xs">
                              {item.fileName}
                            </Badge>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
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
                                ` - ${role.equityPercent}% project-unit allocation`}
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
              {/* The submit button points here, so the readiness sentence travels with
                  the action even though the full panel lives on the Team Roles step. */}
              <p
                id="create-project-readiness"
                className="text-muted-foreground max-w-md text-xs leading-relaxed sm:self-center"
              >
                {valueAccountCheckPending
                  ? "Checking the account setup needed for funding and paid work."
                  : valueAccountReady
                    ? "Account setup is complete; every value-bearing action is checked again on the server."
                    : valueAccountUnderReview
                      ? "You can save this project as a draft now. Funding and paid role listings wait for the review decision - there is nothing for you to redo."
                      : "You can save this project as a draft now. Funding and paid role listings wait for account setup."}
              </p>
              {!valueAccountCheckPending &&
                !valueAccountReady &&
                (valueAccountUnderReview ? (
                  reviewContact && (
                    <Button
                      variant="outline"
                      className="min-h-11 w-full sm:w-auto"
                      asChild
                    >
                      <a href={reviewContact}>Contact the review team</a>
                    </Button>
                  )
                ) : (
                  <Button
                    variant="outline"
                    className="min-h-11 w-full sm:w-auto"
                    asChild
                  >
                    <Link href="/settings/verification?returnTo=%2Fprojects%2Fcreate">
                      Review setup
                    </Link>
                  </Button>
                ))}
              <Button
                variant="default"
                className="bg-system text-system-foreground hover:bg-system/90 min-h-11 w-full font-semibold sm:w-auto"
                onClick={handleSubmit}
                disabled={isSubmitting}
                aria-describedby="create-project-readiness"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 size-4 animate-spin" />
                    {recovery ? "Retrying..." : "Creating..."}
                  </>
                ) : valueAccountReady ? (
                  recovery ? (
                    "Retry remaining setup"
                  ) : (
                    "Create Project"
                  )
                ) : (
                  recovery ? "Save remaining non-paid setup" : "Create project draft"
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
