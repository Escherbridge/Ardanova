"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
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
  DollarSign,
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

const categories = [
  { id: "TECHNOLOGY", label: "Technology" },
  { id: "HEALTHCARE", label: "Healthcare" },
  { id: "EDUCATION", label: "Education" },
  { id: "ENVIRONMENT", label: "Environment" },
  { id: "SOCIAL_IMPACT", label: "Social Impact" },
  { id: "BUSINESS", label: "Business" },
  { id: "ARTS_CULTURE", label: "Arts & Culture" },
  { id: "AGRICULTURE", label: "Agriculture" },
  { id: "FINANCE", label: "Finance" },
  { id: "OTHER", label: "Other" },
];

const durations = [
  { id: "1-2 weeks", label: "1-2 weeks" },
  { id: "1-3 months", label: "1-3 months" },
  { id: "3-6 months", label: "3-6 months" },
  { id: "6+ months", label: "6+ months" },
];

const compensationModels = [
  { id: "FIXED_TOKEN", label: "Fixed Token" },
  { id: "HOURLY_TOKEN", label: "Hourly Token" },
  { id: "EQUITY_PERCENT", label: "Equity Percent" },
  { id: "HYBRID", label: "Hybrid" },
  { id: "BOUNTY", label: "Bounty" },
  { id: "MILESTONE", label: "Milestone" },
];

interface Resource {
  id: string;
  name: string;
  description: string;
  quantity: number;
  estimatedCost: string;
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
  category: string;
  targetAudience: string;
  expectedImpact: string;
  timeline: string;
  tags: string[];
  fundingGoal: string;

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
  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<WizardFormData>({
    title: "",
    problemStatement: "",
    solution: "",
    category: "",
    targetAudience: "",
    expectedImpact: "",
    timeline: "",
    tags: [],
    fundingGoal: "",
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
      if (!formData.category) newErrors.category = "Category is required";
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
      const project = await createMutation.mutateAsync({
        title: formData.title,
        description: formData.solution,
        problemStatement: formData.problemStatement,
        solution: formData.solution,
        category: formData.category as any,
        targetAudience: formData.targetAudience || undefined,
        expectedImpact: formData.expectedImpact || undefined,
        timeline: formData.timeline || undefined,
        tags: formData.tags.join(", ") || undefined,
        fundingGoal: formData.fundingGoal ? Number(formData.fundingGoal) : undefined,
      });

      // Note: Resource, milestone, and role APIs would be called here when available
      // For now, we'll navigate to the created project

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
                  <CardTitle className="text-lg">Project Details</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="text-sm font-medium mb-2 block">
                        Category <span className="text-neon">*</span>
                      </label>
                      <Select
                        value={formData.category}
                        onValueChange={(value) => handleChange("category", value)}
                      >
                        <SelectTrigger
                          className={
                            errors.category ? "border-destructive" : "border-border"
                          }
                        >
                          <SelectValue placeholder="Select category" />
                        </SelectTrigger>
                        <SelectContent>
                          {categories.map((cat) => (
                            <SelectItem key={cat.id} value={cat.id}>
                              {cat.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {errors.category && (
                        <p className="text-sm text-destructive mt-1">
                          {errors.category}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="text-sm font-medium mb-2 block">
                        Timeline
                      </label>
                      <Select
                        value={formData.timeline}
                        onValueChange={(value) => handleChange("timeline", value)}
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
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Funding Goal ($)
                    </label>
                    <input
                      type="number"
                      value={formData.fundingGoal}
                      onChange={(e) => handleChange("fundingGoal", e.target.value)}
                      placeholder="0"
                      min="0"
                      className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
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
                          <div className="flex gap-4 mt-2 text-sm text-muted-foreground">
                            <span>Qty: {resource.quantity}</span>
                            {resource.estimatedCost && (
                              <span>Cost: ${resource.estimatedCost}</span>
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
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Category
                      </p>
                      <p className="text-sm mt-1">
                        {categories.find((c) => c.id === formData.category)
                          ?.label || formData.category}
                      </p>
                    </div>
                    {formData.timeline && (
                      <div>
                        <p className="text-sm font-medium text-muted-foreground">
                          Timeline
                        </p>
                        <p className="text-sm mt-1">{formData.timeline}</p>
                      </div>
                    )}
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
                  {formData.fundingGoal && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Funding Goal
                      </p>
                      <p className="text-sm mt-1">${formData.fundingGoal}</p>
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
                                ` • Cost: $${resource.estimatedCost}`}
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
