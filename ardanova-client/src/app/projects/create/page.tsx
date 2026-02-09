"use client";

import { useState } from "react";
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
import { useEnumOptions, formatEnumLabel } from "~/hooks/use-enum";
import { toast } from "sonner";

const OTHER_CATEGORY_MAX_LENGTH = 50;

const projectTypeDescriptions: Record<string, string> = {
  TEMPORARY: "Short-term project with a defined end date",
  LONG_TERM: "Ongoing project without a strict end date",
  FOUNDATION: "Non-profit organization or foundation",
  BUSINESS: "Business venture or startup",
  PRODUCT: "A product people can help develop",
  OPEN_SOURCE: "Open source project or tool",
  COMMUNITY: "Community-driven initiative",
};

const durationLabels: Record<string, string> = {
  ONE_TWO_WEEKS: "1-2 weeks",
  ONE_THREE_MONTHS: "1-3 months",
  THREE_SIX_MONTHS: "3-6 months",
  SIX_TWELVE_MONTHS: "6-12 months",
  ONE_TWO_YEARS: "1-2 years",
  TWO_PLUS_YEARS: "2+ years",
  ONGOING: "Ongoing",
};

const compensationLabels: Record<string, string> = {
  FIXED_SHARES: "Fixed Token",
  HOURLY_SHARES: "Hourly Token",
  EQUITY_PERCENT: "Equity Percent",
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

const durationToMonths = (durationId: string): number | null => {
  switch (durationId) {
    case "ONE_TWO_WEEKS": return 0.5;
    case "ONE_THREE_MONTHS": return 2;
    case "THREE_SIX_MONTHS": return 4.5;
    case "SIX_TWELVE_MONTHS": return 9;
    case "ONE_TWO_YEARS": return 18;
    case "TWO_PLUS_YEARS": return 30;
    case "ONGOING": return 12;
    default: return null;
  }
};

const toMonthlyRate = (cost: number, intervalDays: number): number =>
  (cost / intervalDays) * 30;

interface Resource {
  id: string;
  name: string;
  description: string;
  quantity: number;
  estimatedCost: string;
  recurringCost: string;
  recurringIntervalDays: number | null;
  isRequired: boolean;
}

interface Role {
  id: string;
  title: string;
  description: string;
  compensationModel: string;
  shareAmount: string;
  equityPercent: string;
  isOpenForApplications: boolean;
  projectRole: string;
}

interface Milestone {
  id: string;
  title: string;
  description: string;
  targetDate: string;
}

interface WizardFormData {
  // Step 1: Basic Info
  title: string;
  problemStatement: string;
  solution: string;
  categories: string[];
  otherCategory: string;
  projectType: string;
  duration: string;
  targetAudience: string;
  expectedImpact: string;
  timeline: string;
  tags: string[];

  // Step 2: Resources
  resources: Resource[];

  // Step 3: Roles
  roles: Role[];

  // Step 4: Milestones
  milestones: Milestone[];
}

const steps = [
  { id: 0, name: "Basic Info", icon: Info },
  { id: 1, name: "Resources", icon: Target },
  { id: 2, name: "Team Roles", icon: Users },
  { id: 3, name: "Milestones", icon: Flag },
  { id: 4, name: "Review", icon: Check },
];

export default function CreateProjectPage() {
  const router = useRouter();
  const { data: session } = useSession();

  // API-driven enum options
  const { options: categories } = useEnumOptions("ProjectCategory");
  const { options: projectTypes } = useEnumOptions("ProjectType");
  const { options: durations } = useEnumOptions("ProjectDuration", durationLabels);
  const { options: compensationModels } = useEnumOptions("CompensationModel", compensationLabels);

  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<WizardFormData>({
    title: "",
    problemStatement: "",
    solution: "",
    categories: [],
    otherCategory: "",
    projectType: "",
    duration: "",
    targetAudience: "",
    expectedImpact: "",
    timeline: "",
    tags: [],
    resources: [],
    roles: [],
    milestones: [],
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Resource state
  const [isAddingResource, setIsAddingResource] = useState(false);
  const [editingResourceId, setEditingResourceId] = useState<string | null>(null);
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
  const [editingMilestoneId, setEditingMilestoneId] = useState<string | null>(null);
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

  const validateStep = (step: number): boolean => {
    const newErrors: Record<string, string> = {};

    if (step === 0) {
      if (!formData.title.trim()) newErrors.title = "Title is required";
      if (!formData.problemStatement.trim())
        newErrors.problemStatement = "Required";
      if (formData.problemStatement.length < 10)
        newErrors.problemStatement = "Min 10 characters";
      if (!formData.solution.trim()) newErrors.solution = "Required";
      if (formData.solution.length < 10) newErrors.solution = "Min 10 characters";
      if (formData.categories.length === 0) newErrors.categories = "Select at least one category";
      if (formData.categories.includes("OTHER") && !formData.otherCategory.trim()) {
        newErrors.otherCategory = "Please specify your category";
      }
      if (formData.otherCategory.length > OTHER_CATEGORY_MAX_LENGTH) {
        newErrors.otherCategory = `Max ${OTHER_CATEGORY_MAX_LENGTH} characters`;
      }
    }

    // Steps 1-3: No required fields, just informational

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
    if (!resourceForm.name.trim()) return;

    if (editingResourceId) {
      setFormData((prev) => ({
        ...prev,
        resources: prev.resources.map((r) =>
          r.id === editingResourceId ? { ...resourceForm, id: r.id } : r
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
    if (!roleForm.title.trim()) return;

    if (editingRoleId) {
      setFormData((prev) => ({
        ...prev,
        roles: prev.roles.map((r) =>
          r.id === editingRoleId ? { ...roleForm, id: r.id } : r
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
    if (!milestoneForm.title.trim()) return;

    if (editingMilestoneId) {
      setFormData((prev) => ({
        ...prev,
        milestones: prev.milestones.map((m) =>
          m.id === editingMilestoneId ? { ...milestoneForm, id: m.id } : m
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

  const handleSubmit = async (publish: boolean) => {
    try {
      // 1. Create the project first
      // Build categories list, replacing "OTHER" with the custom value
      const resolvedCategories = formData.categories.map((c) =>
        c === "OTHER" ? formData.otherCategory.trim() : c
      );

      const project = await createMutation.mutateAsync({
        title: formData.title,
        description: formData.solution,
        problemStatement: formData.problemStatement,
        solution: formData.solution,
        categories: resolvedCategories,
        projectType: (formData.projectType as any) || undefined,
        duration: (formData.duration as any) || undefined,
        targetAudience: formData.targetAudience || undefined,
        expectedImpact: formData.expectedImpact || undefined,
        timeline: formData.duration
          ? durations.find((d) => d.id === formData.duration)?.label
          : formData.timeline || undefined,
        tags: formData.tags.join(", ") || undefined,
      });

      // 2. Save resources
      for (const resource of formData.resources) {
        await addResourceMutation.mutateAsync({
          projectId: project.id,
          name: resource.name,
          description: resource.description || undefined,
          quantity: resource.quantity,
          estimatedCost: resource.estimatedCost ? Number(resource.estimatedCost) : undefined,
          recurringCost: resource.recurringCost ? Number(resource.recurringCost) : undefined,
          recurringIntervalDays: resource.recurringIntervalDays ?? undefined,
          isRequired: resource.isRequired,
        });
      }

      // 3. Save milestones
      for (const milestone of formData.milestones) {
        await addMilestoneMutation.mutateAsync({
          projectId: project.id,
          title: milestone.title,
          description: milestone.description || undefined,
          targetDate: milestone.targetDate,
        });
      }

      // 4. Auto-add creator as FOUNDER member
      if (session?.user?.id) {
        try {
          await addMemberMutation.mutateAsync({
            projectId: project.id,
            userId: session.user.id,
            role: "FOUNDER",
          });
        } catch {
          // Non-critical: creator might already be added by backend
        }
      }

      // 5. Save team roles as project opportunities
      for (const role of formData.roles) {
        const compensationMap: Record<string, "fixed" | "hourly" | "negotiable"> = {
          FIXED_TOKEN: "fixed",
          HOURLY_TOKEN: "hourly",
          EQUITY_PERCENT: "negotiable",
          HYBRID: "negotiable",
          BOUNTY: "fixed",
          MILESTONE: "fixed",
        };

        const description = role.description.length >= 20
          ? role.description
          : `${role.description} — Role for ${formData.title}`.slice(0, 200);

        try {
          await createOpportunityMutation.mutateAsync({
            projectId: project.id,
            title: role.title,
            description,
            type: "PROJECT_ROLE",
            skills: formData.tags.length > 0 ? formData.tags : ["General"],
            compensationModel: compensationMap[role.compensationModel] ?? "negotiable",
            compensationAmount: role.shareAmount && Number(role.shareAmount) > 0 ? Number(role.shareAmount) : undefined,
            isRemote: true,
            projectRole: (role.projectRole || undefined) as "FOUNDER" | "LEADER" | "CORE_CONTRIBUTOR" | "CONTRIBUTOR" | "OBSERVER" | undefined,
          });
        } catch (err) {
          console.error(`Failed to create role "${role.title}":`, err);
          toast.error(`Failed to save role "${role.title}"`);
        }
      }

      router.push(`/projects/${project.slug}`);
    } catch (error) {
      setErrors({
        submit: error instanceof Error ? error.message : "Failed to create project",
      });
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/projects">
              <ArrowLeft className="size-4 mr-2" />
              Back to Projects
            </Link>
          </Button>
          <h1 className="text-3xl font-bold">Create New Project</h1>
          <p className="text-muted-foreground mt-2">
            Complete the wizard to launch your project on ArdaNova
          </p>
        </div>

        {/* Progress Stepper */}
        <div className="mb-8 overflow-x-auto">
          <div className="flex items-center justify-between min-w-[600px]">
            {steps.map((step, index) => (
              <div key={step.id} className="flex items-center">
                <button
                  onClick={() => goToStep(index)}
                  className={cn(
                    "flex flex-col items-center gap-2 transition-opacity",
                    currentStep < index && "opacity-50"
                  )}
                >
                  <div
                    className={cn(
                      "w-10 h-10 rounded-full flex items-center justify-center font-medium transition-colors",
                      currentStep > index
                        ? "bg-neon text-black"
                        : currentStep === index
                          ? "bg-neon/20 text-neon border-2 border-neon"
                          : "bg-muted text-muted-foreground"
                    )}
                  >
                    {currentStep > index ? (
                      <Check className="size-5" />
                    ) : (
                      index + 1
                    )}
                  </div>
                  <span className="text-xs font-medium hidden sm:block">
                    {step.name}
                  </span>
                </button>
                {index < steps.length - 1 && (
                  <div
                    className={cn(
                      "h-0.5 w-12 mx-2 transition-colors",
                      currentStep > index ? "bg-neon" : "bg-border"
                    )}
                  />
                )}
              </div>
            ))}
          </div>
        </div>

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
                    <label className="text-sm font-medium mb-2 block">
                      Project Title <span className="text-neon">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.title}
                      onChange={(e) => handleChange("title", e.target.value)}
                      placeholder="e.g., Sustainable Water Filtration System"
                      className={cn(
                        "w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50",
                        errors.title ? "border-destructive" : "border-border"
                      )}
                    />
                    {errors.title && (
                      <p className="text-sm text-destructive mt-1">
                        {errors.title}
                      </p>
                    )}
                  </div>
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center gap-2">
                    <div className="w-8 h-8 bg-neon-pink/20 rounded-lg flex items-center justify-center">
                      <span className="text-neon-pink font-bold">?</span>
                    </div>
                    Problem Definition
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Problem Statement <span className="text-neon">*</span>
                    </label>
                    <textarea
                      value={formData.problemStatement}
                      onChange={(e) =>
                        handleChange("problemStatement", e.target.value)
                      }
                      placeholder="Describe the problem you're solving. What is the issue? Who does it affect?"
                      rows={4}
                      className={cn(
                        "w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none",
                        errors.problemStatement
                          ? "border-destructive"
                          : "border-border"
                      )}
                    />
                    {errors.problemStatement && (
                      <p className="text-sm text-destructive mt-1">
                        {errors.problemStatement}
                      </p>
                    )}
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Target Audience
                    </label>
                    <input
                      type="text"
                      value={formData.targetAudience}
                      onChange={(e) =>
                        handleChange("targetAudience", e.target.value)
                      }
                      placeholder="Who are the primary beneficiaries?"
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Expected Impact
                    </label>
                    <textarea
                      value={formData.expectedImpact}
                      onChange={(e) =>
                        handleChange("expectedImpact", e.target.value)
                      }
                      placeholder="What impact do you expect to achieve?"
                      rows={3}
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                    />
                  </div>
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg flex items-center gap-2">
                    <div className="w-8 h-8 bg-neon-green/20 rounded-lg flex items-center justify-center">
                      <span className="text-neon-green font-bold">!</span>
                    </div>
                    Solution Definition
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Proposed Solution <span className="text-neon">*</span>
                    </label>
                    <textarea
                      value={formData.solution}
                      onChange={(e) => handleChange("solution", e.target.value)}
                      placeholder="Describe your proposed solution. How does it address the problem?"
                      rows={4}
                      className={cn(
                        "w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none",
                        errors.solution ? "border-destructive" : "border-border"
                      )}
                    />
                    {errors.solution && (
                      <p className="text-sm text-destructive mt-1">
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
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      What kind of project is this?
                    </label>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                      {projectTypes.map((pt) => {
                        const isSelected = formData.projectType === pt.id;
                        return (
                          <button
                            key={pt.id}
                            type="button"
                            onClick={() => handleChange("projectType", pt.id)}
                            className={cn(
                              "flex flex-col items-start p-3 rounded-lg border-2 text-left transition-colors",
                              isSelected
                                ? "border-neon bg-neon/10"
                                : "border-border bg-muted/30 hover:border-neon/50"
                            )}
                          >
                            <span className={cn(
                              "text-sm font-medium",
                              isSelected ? "text-neon" : "text-foreground"
                            )}>
                              {pt.label}
                            </span>
                            <span className="text-xs text-muted-foreground mt-0.5">
                              {projectTypeDescriptions[pt.id] ?? ""}
                            </span>
                          </button>
                        );
                      })}
                    </div>
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Expected Duration
                    </label>
                    <Select
                      value={formData.duration}
                      onValueChange={(value) => handleChange("duration", value)}
                    >
                      <SelectTrigger>
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
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Categories <span className="text-neon">*</span>
                    </label>
                    <div className={cn(
                      "flex flex-wrap gap-2 p-3 rounded-lg border-2",
                      errors.categories ? "border-destructive" : "border-border"
                    )}>
                      {categories.map((cat) => {
                        const isSelected = formData.categories.includes(cat.id);
                        return (
                          <button
                            key={cat.id}
                            type="button"
                            onClick={() => {
                              setFormData((prev) => ({
                                ...prev,
                                categories: isSelected
                                  ? prev.categories.filter((c) => c !== cat.id)
                                  : [...prev.categories, cat.id],
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
                              "px-3 py-1.5 rounded-full text-sm font-medium transition-colors border",
                              isSelected
                                ? "bg-neon text-black border-neon"
                                : "bg-muted/50 text-muted-foreground border-border hover:border-neon/50 hover:text-foreground"
                            )}
                          >
                            {cat.label}
                          </button>
                        );
                      })}
                      <button
                        type="button"
                        onClick={() => {
                          const isSelected = formData.categories.includes("OTHER");
                          setFormData((prev) => ({
                            ...prev,
                            categories: isSelected
                              ? prev.categories.filter((c) => c !== "OTHER")
                              : [...prev.categories, "OTHER"],
                            otherCategory: isSelected ? "" : prev.otherCategory,
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
                          "px-3 py-1.5 rounded-full text-sm font-medium transition-colors border",
                          formData.categories.includes("OTHER")
                            ? "bg-neon text-black border-neon"
                            : "bg-muted/50 text-muted-foreground border-border hover:border-neon/50 hover:text-foreground"
                        )}
                      >
                        Other
                      </button>
                    </div>
                    {errors.categories && (
                      <p className="text-sm text-destructive mt-1">
                        {errors.categories}
                      </p>
                    )}
                    {formData.categories.includes("OTHER") && (
                      <div className="mt-3">
                        <label className="text-sm font-medium mb-1 block">
                          Specify other category
                        </label>
                        <div className="relative">
                          <input
                            type="text"
                            value={formData.otherCategory}
                            onChange={(e) => {
                              const value = e.target.value.slice(0, OTHER_CATEGORY_MAX_LENGTH);
                              setFormData((prev) => ({ ...prev, otherCategory: value }));
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
                              "w-full px-4 py-2 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50",
                              errors.otherCategory ? "border-destructive" : "border-border"
                            )}
                          />
                          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">
                            {formData.otherCategory.length}/{OTHER_CATEGORY_MAX_LENGTH}
                          </span>
                        </div>
                        {errors.otherCategory && (
                          <p className="text-sm text-destructive mt-1">
                            {errors.otherCategory}
                          </p>
                        )}
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg">Tags</CardTitle>
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
                      <Plus className="size-4" />
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
                          <X className="size-3" />
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
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Target className="size-5 text-neon" />
                      Project Resources
                    </CardTitle>
                    <p className="text-sm text-muted-foreground mt-1">
                      Define resources needed for your project
                    </p>
                  </div>
                  {!isAddingResource && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingResource(true)}
                    >
                      <Plus className="size-4 mr-2" />
                      Add Resource
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Add/Edit Resource Form */}
                {isAddingResource && (
                  <Card className="bg-muted/30 border-dashed border-2 border-border">
                    <CardContent className="p-4 space-y-4">
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Resource Name <span className="text-neon">*</span>
                        </label>
                        <input
                          type="text"
                          value={resourceForm.name}
                          onChange={(e) =>
                            setResourceForm((prev) => ({
                              ...prev,
                              name: e.target.value,
                            }))
                          }
                          placeholder="e.g., Cloud Server"
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                        />
                      </div>
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Description
                        </label>
                        <textarea
                          value={resourceForm.description}
                          onChange={(e) =>
                            setResourceForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe the resource..."
                          rows={2}
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                        />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-sm font-medium mb-2 block">
                            Quantity
                          </label>
                          <input
                            type="number"
                            value={resourceForm.quantity}
                            onChange={(e) =>
                              setResourceForm((prev) => ({
                                ...prev,
                                quantity: parseInt(e.target.value) || 1,
                              }))
                            }
                            min="1"
                            className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                          />
                        </div>
                        <div>
                          <label className="text-sm font-medium mb-2 block">
                            Estimated Cost ($)
                          </label>
                          <input
                            type="number"
                            value={resourceForm.estimatedCost}
                            onChange={(e) =>
                              setResourceForm((prev) => ({
                                ...prev,
                                estimatedCost: e.target.value,
                              }))
                            }
                            placeholder="0"
                            min="0"
                            className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                          />
                        </div>
                      </div>

                      {/* Recurring Cost */}
                      <div className="space-y-3 pt-2 border-t border-border/50">
                        <label className="text-sm font-medium block">
                          Recurring Cost
                        </label>
                        <div className="grid grid-cols-2 gap-4">
                          <div>
                            <label className="text-xs text-muted-foreground mb-1 block">
                              Amount ($) per period
                            </label>
                            <input
                              type="number"
                              value={resourceForm.recurringCost}
                              onChange={(e) =>
                                setResourceForm((prev) => ({
                                  ...prev,
                                  recurringCost: e.target.value,
                                }))
                              }
                              placeholder="0"
                              min="0"
                              className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                            />
                          </div>
                          <div>
                            <label className="text-xs text-muted-foreground mb-1 block">
                              Billing interval
                            </label>
                            <div className="flex flex-wrap gap-2">
                              {RECURRING_PRESETS.map((preset) => (
                                <button
                                  key={preset.label}
                                  type="button"
                                  onClick={() =>
                                    setResourceForm((prev) => ({
                                      ...prev,
                                      recurringIntervalDays: preset.days,
                                    }))
                                  }
                                  className={cn(
                                    "px-3 py-1.5 text-xs rounded-md border transition-colors",
                                    resourceForm.recurringIntervalDays === preset.days
                                      ? "bg-neon/20 border-neon text-neon"
                                      : "bg-muted/50 border-border text-muted-foreground hover:bg-muted"
                                  )}
                                >
                                  {preset.label}
                                </button>
                              ))}
                              <button
                                type="button"
                                onClick={() =>
                                  setResourceForm((prev) => ({
                                    ...prev,
                                    recurringIntervalDays:
                                      prev.recurringIntervalDays !== null &&
                                      !PRESET_DAYS.includes(prev.recurringIntervalDays as 14 | 30 | 365)
                                        ? prev.recurringIntervalDays
                                        : 7,
                                  }))
                                }
                                className={cn(
                                  "px-3 py-1.5 text-xs rounded-md border transition-colors",
                                  resourceForm.recurringIntervalDays !== null &&
                                    !PRESET_DAYS.includes(resourceForm.recurringIntervalDays as 14 | 30 | 365)
                                    ? "bg-neon/20 border-neon text-neon"
                                    : "bg-muted/50 border-border text-muted-foreground hover:bg-muted"
                                )}
                              >
                                Custom
                              </button>
                            </div>
                          </div>
                        </div>

                        {/* Custom interval input */}
                        {resourceForm.recurringIntervalDays !== null &&
                          !PRESET_DAYS.includes(resourceForm.recurringIntervalDays as 14 | 30 | 365) && (
                          <div>
                            <label className="text-xs text-muted-foreground mb-1 block">
                              Custom interval (days, max 365)
                            </label>
                            <input
                              type="number"
                              value={resourceForm.recurringIntervalDays}
                              onChange={(e) => {
                                const val = Math.min(365, Math.max(1, parseInt(e.target.value) || 1));
                                setResourceForm((prev) => ({
                                  ...prev,
                                  recurringIntervalDays: val,
                                }));
                              }}
                              min="1"
                              max="365"
                              className="w-32 px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                            />
                          </div>
                        )}

                        {/* Computed monthly rate preview */}
                        {resourceForm.recurringCost &&
                          Number(resourceForm.recurringCost) > 0 &&
                          resourceForm.recurringIntervalDays &&
                          resourceForm.recurringIntervalDays > 0 && (
                          <p className="text-xs text-muted-foreground">
                            Equivalent to{" "}
                            <span className="font-medium text-foreground">
                              ${toMonthlyRate(Number(resourceForm.recurringCost), resourceForm.recurringIntervalDays).toFixed(2)}/month
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
                            className="text-xs text-muted-foreground hover:text-foreground underline"
                          >
                            Remove recurring cost
                          </button>
                        )}
                      </div>

                      <div className="flex items-center gap-2">
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
                          className="w-4 h-4 rounded border-border"
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
                        <Button variant="default" size="sm" onClick={addResource}>
                          {editingResourceId ? "Update" : "Add"} Resource
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Resource List */}
                {formData.resources.length === 0 && !isAddingResource && (
                  <div className="text-center py-8 text-muted-foreground">
                    <Target className="size-12 mx-auto mb-2 opacity-50" />
                    <p>No resources added yet</p>
                    <p className="text-sm">
                      Click "Add Resource" to define project resources
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
                            <p className="text-sm text-muted-foreground mt-1">
                              {resource.description}
                            </p>
                          )}
                          <div className="flex flex-wrap gap-4 mt-2 text-sm text-muted-foreground">
                            <span>Qty: {resource.quantity}</span>
                            {resource.estimatedCost && (
                              <span>One-time: ${resource.estimatedCost}</span>
                            )}
                            {resource.recurringCost && resource.recurringIntervalDays && (
                              <span>
                                Recurring: ${resource.recurringCost}/{resource.recurringIntervalDays}d
                                {" "}(${toMonthlyRate(Number(resource.recurringCost), resource.recurringIntervalDays).toFixed(2)}/mo)
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
                            <Trash2 className="size-4 text-destructive" />
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
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Users className="size-5 text-neon" />
                      Team Roles
                    </CardTitle>
                    <p className="text-sm text-muted-foreground mt-1">
                      Define roles and compensation for your team
                    </p>
                  </div>
                  {!isAddingRole && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingRole(true)}
                    >
                      <Plus className="size-4 mr-2" />
                      Add Role
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Add/Edit Role Form */}
                {isAddingRole && (
                  <Card className="bg-muted/30 border-dashed border-2 border-border">
                    <CardContent className="p-4 space-y-4">
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Role Title <span className="text-neon">*</span>
                        </label>
                        <input
                          type="text"
                          value={roleForm.title}
                          onChange={(e) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              title: e.target.value,
                            }))
                          }
                          placeholder="e.g., Lead Developer"
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                        />
                      </div>
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Description
                        </label>
                        <textarea
                          value={roleForm.description}
                          onChange={(e) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe the role responsibilities..."
                          rows={2}
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                        />
                      </div>
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Compensation Model
                        </label>
                        <Select
                          value={roleForm.compensationModel}
                          onValueChange={(value) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              compensationModel: value,
                            }))
                          }
                        >
                          <SelectTrigger>
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
                        <label className="text-sm font-medium mb-2 block">
                          Base Role
                        </label>
                        <Select
                          value={roleForm.projectRole}
                          onValueChange={(value) =>
                            setRoleForm((prev) => ({
                              ...prev,
                              projectRole: value,
                            }))
                          }
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select base role" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="FOUNDER">Founder</SelectItem>
                            <SelectItem value="LEADER">Leader</SelectItem>
                            <SelectItem value="CORE_CONTRIBUTOR">Core Contributor</SelectItem>
                            <SelectItem value="CONTRIBUTOR">Contributor</SelectItem>
                            <SelectItem value="OBSERVER">Observer</SelectItem>
                          </SelectContent>
                        </Select>
                        <p className="text-xs text-muted-foreground mt-1">
                          The organizational role granted when this position is filled
                        </p>
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <label className="text-sm font-medium mb-2 block">
                            Share Amount
                          </label>
                          <input
                            type="number"
                            value={roleForm.shareAmount}
                            onChange={(e) =>
                              setRoleForm((prev) => ({
                                ...prev,
                                shareAmount: e.target.value,
                              }))
                            }
                            placeholder="0"
                            min="0"
                            className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                          />
                        </div>
                        <div>
                          <label className="text-sm font-medium mb-2 block">
                            Equity Percent
                          </label>
                          <input
                            type="number"
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
                            className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                          />
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
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
                          className="w-4 h-4 rounded border-border"
                        />
                        <label htmlFor="roleOpen" className="text-sm font-medium">
                          Open for applications
                        </label>
                      </div>
                      <div className="flex justify-end gap-2 pt-2">
                        <Button variant="ghost" size="sm" onClick={cancelRoleEdit}>
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
                  <div className="text-center py-8 text-muted-foreground">
                    <Users className="size-12 mx-auto mb-2 opacity-50" />
                    <p>No roles defined yet</p>
                    <p className="text-sm">Click "Add Role" to define team roles</p>
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
                            <p className="text-sm text-muted-foreground mt-1">
                              {role.description}
                            </p>
                          )}
                          <div className="flex flex-wrap gap-3 mt-2 text-sm text-muted-foreground">
                            {role.compensationModel && (
                              <span>
                                Model:{" "}
                                {compensationModels.find(
                                  (m) => m.id === role.compensationModel
                                )?.label || role.compensationModel}
                              </span>
                            )}
                            {role.shareAmount && (
                              <span>Shares: {role.shareAmount}</span>
                            )}
                            {role.equityPercent && (
                              <span>Equity: {role.equityPercent}%</span>
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
                            <Trash2 className="size-4 text-destructive" />
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
                    <CardTitle className="text-lg flex items-center gap-2">
                      <Flag className="size-5 text-neon" />
                      Project Milestones
                    </CardTitle>
                    <p className="text-sm text-muted-foreground mt-1">
                      Define key milestones for your project timeline
                    </p>
                  </div>
                  {!isAddingMilestone && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsAddingMilestone(true)}
                    >
                      <Plus className="size-4 mr-2" />
                      Add Milestone
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Add/Edit Milestone Form */}
                {isAddingMilestone && (
                  <Card className="bg-muted/30 border-dashed border-2 border-border">
                    <CardContent className="p-4 space-y-4">
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Milestone Title <span className="text-neon">*</span>
                        </label>
                        <input
                          type="text"
                          value={milestoneForm.title}
                          onChange={(e) =>
                            setMilestoneForm((prev) => ({
                              ...prev,
                              title: e.target.value,
                            }))
                          }
                          placeholder="e.g., MVP Launch"
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                        />
                      </div>
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Description
                        </label>
                        <textarea
                          value={milestoneForm.description}
                          onChange={(e) =>
                            setMilestoneForm((prev) => ({
                              ...prev,
                              description: e.target.value,
                            }))
                          }
                          placeholder="Describe this milestone..."
                          rows={2}
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                        />
                      </div>
                      <div>
                        <label className="text-sm font-medium mb-2 block">
                          Target Date
                        </label>
                        <input
                          type="date"
                          value={milestoneForm.targetDate}
                          onChange={(e) =>
                            setMilestoneForm((prev) => ({
                              ...prev,
                              targetDate: e.target.value,
                            }))
                          }
                          className="w-full px-4 py-2 bg-background border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
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
                        <Button variant="default" size="sm" onClick={addMilestone}>
                          {editingMilestoneId ? "Update" : "Add"} Milestone
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Milestone List */}
                {formData.milestones.length === 0 && !isAddingMilestone && (
                  <div className="text-center py-8 text-muted-foreground">
                    <Flag className="size-12 mx-auto mb-2 opacity-50" />
                    <p>No milestones defined yet</p>
                    <p className="text-sm">
                      Click "Add Milestone" to define project milestones
                    </p>
                  </div>
                )}

                <div className="space-y-3">
                  {formData.milestones.map((milestone, index) => (
                    <div key={milestone.id} className="flex gap-3">
                      <div className="flex flex-col items-center">
                        <div className="w-8 h-8 rounded-full bg-neon/20 border-2 border-neon flex items-center justify-center text-sm font-medium">
                          {index + 1}
                        </div>
                        {index < formData.milestones.length - 1 && (
                          <div className="w-0.5 flex-1 bg-border my-1" />
                        )}
                      </div>
                      <Card className="flex-1 bg-card border-border">
                        <CardContent className="p-4">
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <h4 className="font-medium">{milestone.title}</h4>
                              {milestone.description && (
                                <p className="text-sm text-muted-foreground mt-1">
                                  {milestone.description}
                                </p>
                              )}
                              {milestone.targetDate && (
                                <div className="flex items-center gap-2 mt-2 text-sm text-muted-foreground">
                                  <Calendar className="size-4" />
                                  {new Date(milestone.targetDate).toLocaleDateString()}
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
                                <Trash2 className="size-4 text-destructive" />
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
                    <Pencil className="size-4 mr-2" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div>
                    <h3 className="text-xl font-semibold">{formData.title}</h3>
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      Problem Statement
                    </p>
                    <p className="text-sm mt-1">{formData.problemStatement}</p>
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      Solution
                    </p>
                    <p className="text-sm mt-1">{formData.solution}</p>
                  </div>
                  <div className="grid grid-cols-2 gap-4 pt-2">
                    {formData.projectType && (
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">
                          Project Type
                        </p>
                        <p className="text-sm mt-1">
                          {projectTypes.find((pt) => pt.id === formData.projectType)?.label || formData.projectType}
                        </p>
                      </div>
                    )}
                    {formData.duration && (
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">
                          Duration
                        </p>
                        <p className="text-sm mt-1">
                          {durations.find((d) => d.id === formData.duration)?.label || formData.duration}
                        </p>
                      </div>
                    )}
                  </div>
                  <div className="grid grid-cols-2 gap-4 pt-2">
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Categories
                      </p>
                      <div className="flex flex-wrap gap-1.5 mt-1">
                        {formData.categories.map((catId) => {
                          const label = catId === "OTHER"
                            ? formData.otherCategory || "Other"
                            : categories.find((c) => c.id === catId)?.label || catId.replace("_", " ");
                          return (
                            <Badge key={catId} variant="secondary" className="text-xs">
                              {label}
                            </Badge>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                  {formData.targetAudience && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Target Audience
                      </p>
                      <p className="text-sm mt-1">{formData.targetAudience}</p>
                    </div>
                  )}
                  {formData.expectedImpact && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Expected Impact
                      </p>
                      <p className="text-sm mt-1">{formData.expectedImpact}</p>
                    </div>
                  )}
                  {formData.tags.length > 0 && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">
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
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Target className="size-5 text-neon" />
                    Resources ({formData.resources.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(1)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="size-4 mr-2" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.resources.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No resources defined
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {formData.resources.map((resource) => (
                        <div
                          key={resource.id}
                          className="flex items-center justify-between p-3 bg-muted/30 rounded-lg"
                        >
                          <div>
                            <p className="text-sm font-medium">{resource.name}</p>
                            <p className="text-xs text-muted-foreground">
                              Qty: {resource.quantity}
                              {resource.estimatedCost &&
                                ` • One-time: $${resource.estimatedCost}`}
                              {resource.recurringCost && resource.recurringIntervalDays &&
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
              {formData.resources.length > 0 && (() => {
                const totalOneTime = formData.resources.reduce(
                  (sum, r) => sum + (Number(r.estimatedCost) || 0) * r.quantity,
                  0
                );
                const totalMonthlyRecurring = formData.resources.reduce(
                  (sum, r) =>
                    sum +
                    (r.recurringCost && r.recurringIntervalDays
                      ? toMonthlyRate(Number(r.recurringCost), r.recurringIntervalDays)
                      : 0),
                  0
                );
                const projectMonths = durationToMonths(formData.duration);
                const totalProjectedRecurring = projectMonths
                  ? totalMonthlyRecurring * projectMonths
                  : null;
                const totalProjectedCost = totalProjectedRecurring !== null
                  ? totalOneTime + totalProjectedRecurring
                  : null;

                if (totalOneTime === 0 && totalMonthlyRecurring === 0) return null;

                return (
                  <Card className="bg-card border-border">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-lg flex items-center gap-2">
                        <Target className="size-5 text-neon" />
                        Financial Projections
                      </CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-2 gap-4">
                        <div className="p-3 bg-muted/30 rounded-lg">
                          <p className="text-xs text-muted-foreground">Total One-Time Costs</p>
                          <p className="text-lg font-semibold">${totalOneTime.toFixed(2)}</p>
                        </div>
                        {totalMonthlyRecurring > 0 && (
                          <div className="p-3 bg-muted/30 rounded-lg">
                            <p className="text-xs text-muted-foreground">Total Monthly Recurring</p>
                            <p className="text-lg font-semibold">${totalMonthlyRecurring.toFixed(2)}/mo</p>
                          </div>
                        )}
                        {totalProjectedCost !== null && totalMonthlyRecurring > 0 && (
                          <>
                            <div className="p-3 bg-muted/30 rounded-lg">
                              <p className="text-xs text-muted-foreground">
                                Projected Recurring ({durations.find((d) => d.id === formData.duration)?.label})
                              </p>
                              <p className="text-lg font-semibold">
                                ${totalProjectedRecurring!.toFixed(2)}
                              </p>
                            </div>
                            <div className="p-3 bg-neon/10 rounded-lg border border-neon/30">
                              <p className="text-xs text-muted-foreground">
                                Total Projected Cost
                              </p>
                              <p className="text-lg font-semibold text-neon">
                                ${totalProjectedCost.toFixed(2)}
                              </p>
                            </div>
                          </>
                        )}
                        {totalProjectedCost === null && totalMonthlyRecurring > 0 && (
                          <div className="col-span-2 p-3 bg-muted/30 rounded-lg text-center">
                            <p className="text-xs text-muted-foreground">
                              Set a project duration to see total projected costs
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
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Users className="size-5 text-neon" />
                    Team Roles ({formData.roles.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(2)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="size-4 mr-2" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.roles.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No roles defined</p>
                  ) : (
                    <div className="space-y-2">
                      {formData.roles.map((role) => (
                        <div
                          key={role.id}
                          className="flex items-center justify-between p-3 bg-muted/30 rounded-lg"
                        >
                          <div>
                            <p className="text-sm font-medium">{role.title}</p>
                            <p className="text-xs text-muted-foreground">
                              {role.compensationModel &&
                                compensationModels.find(
                                  (m) => m.id === role.compensationModel
                                )?.label}
                              {role.shareAmount && ` • ${role.shareAmount} shares`}
                              {role.equityPercent && ` • ${role.equityPercent}% equity`}
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
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Flag className="size-5 text-neon" />
                    Milestones ({formData.milestones.length})
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(3)}
                    className="absolute top-4 right-4"
                  >
                    <Pencil className="size-4 mr-2" />
                    Edit
                  </Button>
                </CardHeader>
                <CardContent>
                  {formData.milestones.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No milestones defined
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {formData.milestones.map((milestone, index) => (
                        <div
                          key={milestone.id}
                          className="flex items-start gap-3 p-3 bg-muted/30 rounded-lg"
                        >
                          <div className="w-6 h-6 rounded-full bg-neon/20 border-2 border-neon flex items-center justify-center text-xs font-medium flex-shrink-0">
                            {index + 1}
                          </div>
                          <div className="flex-1">
                            <p className="text-sm font-medium">{milestone.title}</p>
                            {milestone.targetDate && (
                              <p className="text-xs text-muted-foreground">
                                {new Date(milestone.targetDate).toLocaleDateString()}
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
                    <p className="text-sm text-destructive">{errors.submit}</p>
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </div>

        {/* Navigation Buttons */}
        <div className="flex justify-between mt-8">
          <Button
            variant="outline"
            onClick={goToPreviousStep}
            disabled={currentStep === 0}
          >
            <ArrowLeft className="size-4 mr-2" />
            Previous
          </Button>

          {currentStep < steps.length - 1 ? (
            <Button variant="default" onClick={goToNextStep}>
              Next
              <ArrowRight className="size-4 ml-2" />
            </Button>
          ) : (
            <div className="flex gap-3">
              <Button
                variant="outline"
                onClick={() => handleSubmit(false)}
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  "Save as Draft"
                )}
              </Button>
              <Button
                variant="default"
                className="bg-neon hover:bg-neon/90 text-black font-semibold"
                onClick={() => handleSubmit(true)}
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? (
                  <>
                    <Loader2 className="size-4 mr-2 animate-spin" />
                    Creating...
                  </>
                ) : (
                  "Create & Publish"
                )}
              </Button>
            </div>
          )}
        </div>

        {/* Info Note */}
        {currentStep === 0 && (
          <div className="flex items-start gap-3 p-4 mt-6 bg-muted/30 rounded-lg border border-border">
            <Info className="size-5 text-neon mt-0.5 flex-shrink-0" />
            <div className="text-sm text-muted-foreground">
              <p>
                This wizard will guide you through creating a comprehensive project.
                You can skip optional steps and come back to edit them later.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
