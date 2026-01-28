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
  Briefcase,
  FileText,
  DollarSign,
  MapPin,
  Calendar,
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

const opportunityTypes = [
  { id: "Bounty", label: "Bounty" },
  { id: "Freelance", label: "Freelance" },
  { id: "Contract", label: "Contract" },
  { id: "Part-time", label: "Part-time" },
  { id: "Full-time", label: "Full-time" },
];

const experienceLevels = [
  { id: "entry", label: "Entry Level" },
  { id: "intermediate", label: "Intermediate" },
  { id: "senior", label: "Senior" },
  { id: "expert", label: "Expert" },
];

const compensationTypes = [
  { id: "fixed", label: "Fixed Price" },
  { id: "hourly", label: "Hourly Rate" },
  { id: "negotiable", label: "Negotiable" },
];

interface WizardFormData {
  // Step 1: Basic Info
  title: string;
  description: string;
  type: string;

  // Step 2: Requirements
  requiredSkills: string[];
  experienceLevel: string;
  requirements: string;

  // Step 3: Compensation
  compensationType: string;
  compensation: string;
  benefits: string;

  // Step 4: Details
  location: string;
  isRemote: boolean;
  deadline: string;
  maxApplications: string;
}

const steps = [
  { id: 0, name: "Basic Info", icon: Info },
  { id: 1, name: "Requirements", icon: FileText },
  { id: 2, name: "Compensation", icon: DollarSign },
  { id: 3, name: "Details", icon: MapPin },
  { id: 4, name: "Review", icon: Check },
];

export default function CreateOpportunityPage() {
  const router = useRouter();
  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<WizardFormData>({
    title: "",
    description: "",
    type: "",
    requiredSkills: [],
    experienceLevel: "",
    requirements: "",
    compensationType: "",
    compensation: "",
    benefits: "",
    location: "",
    isRemote: false,
    deadline: "",
    maxApplications: "",
  });
  const [newSkill, setNewSkill] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = api.opportunity.create.useMutation();

  const handleChange = (field: string, value: string | boolean) => {
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
    if (newSkill.trim() && !formData.requiredSkills.includes(newSkill.trim())) {
      setFormData((prev) => ({
        ...prev,
        requiredSkills: [...prev.requiredSkills, newSkill.trim()],
      }));
      setNewSkill("");
    }
  };

  const removeSkill = (skill: string) => {
    setFormData((prev) => ({
      ...prev,
      requiredSkills: prev.requiredSkills.filter((s) => s !== skill),
    }));
  };

  const validateStep = (step: number): boolean => {
    const newErrors: Record<string, string> = {};

    if (step === 0) {
      if (!formData.title.trim()) newErrors.title = "Title is required";
      if (formData.title.length < 5) newErrors.title = "Min 5 characters";
      if (!formData.description.trim()) newErrors.description = "Required";
      if (formData.description.length < 20) newErrors.description = "Min 20 characters";
      if (!formData.type) newErrors.type = "Type is required";
    }

    // Steps 1-3: Optional fields

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

  const handleSubmit = async (publish: boolean) => {
    try {
      const opportunity = await createMutation.mutateAsync({
        title: formData.title,
        description: formData.description + (formData.requirements ? `\n\nRequirements:\n${formData.requirements}` : "") + (formData.benefits ? `\n\nBenefits:\n${formData.benefits}` : ""),
        type: formData.type as "Bounty" | "Freelance" | "Contract" | "Part-time" | "Full-time",
        skills: formData.requiredSkills.length > 0 ? formData.requiredSkills : ["General"],
        experienceLevel: formData.experienceLevel ? formData.experienceLevel as "entry" | "intermediate" | "senior" | "expert" : undefined,
        compensationType: formData.compensationType ? formData.compensationType as "fixed" | "hourly" | "negotiable" : undefined,
        compensationAmount: formData.compensation ? Number(formData.compensation) : undefined,
        location: formData.location || undefined,
        isRemote: formData.isRemote,
        deadline: formData.deadline || undefined,
        maxApplications: formData.maxApplications ? Number(formData.maxApplications) : undefined,
      });

      router.push(`/opportunities/${opportunity.id}`);
    } catch (error) {
      setErrors({
        submit: error instanceof Error ? error.message : "Failed to create opportunity",
      });
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/opportunities">
              <ArrowLeft className="size-4 mr-2" />
              Back to Opportunities
            </Link>
          </Button>
          <h1 className="text-3xl font-bold">Post New Opportunity</h1>
          <p className="text-muted-foreground mt-2">
            Complete the wizard to post your opportunity on ArdaNova
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
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Briefcase className="size-5 text-neon" />
                    Basic Information
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Opportunity Title <span className="text-neon">*</span>
                    </label>
                    <input
                      type="text"
                      value={formData.title}
                      onChange={(e) => handleChange("title", e.target.value)}
                      placeholder="e.g., Senior Full-Stack Developer"
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

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Description <span className="text-neon">*</span>
                    </label>
                    <textarea
                      value={formData.description}
                      onChange={(e) => handleChange("description", e.target.value)}
                      placeholder="Describe the opportunity, what you're looking for, and what the role entails..."
                      rows={6}
                      className={cn(
                        "w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none",
                        errors.description ? "border-destructive" : "border-border"
                      )}
                    />
                    {errors.description && (
                      <p className="text-sm text-destructive mt-1">
                        {errors.description}
                      </p>
                    )}
                  </div>

                  <div>
                    <label className="text-sm font-medium mb-2 block">
                      Opportunity Type <span className="text-neon">*</span>
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
                        {opportunityTypes.map((type) => (
                          <SelectItem key={type.id} value={type.id}>
                            {type.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {errors.type && (
                      <p className="text-sm text-destructive mt-1">
                        {errors.type}
                      </p>
                    )}
                  </div>
                </CardContent>
              </Card>
            </>
          )}

          {/* Step 2: Requirements */}
          {currentStep === 1 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg flex items-center gap-2">
                  <FileText className="size-5 text-neon" />
                  Requirements
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Required Skills
                  </label>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={newSkill}
                      onChange={(e) => setNewSkill(e.target.value)}
                      placeholder="Add a skill..."
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          addSkill();
                        }
                      }}
                      className="flex-1 px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      onClick={addSkill}
                      className="px-4"
                    >
                      <Plus className="size-4" />
                    </Button>
                  </div>
                  {formData.requiredSkills.length > 0 && (
                    <div className="flex flex-wrap gap-2 mt-3">
                      {formData.requiredSkills.map((skill) => (
                        <Badge
                          key={skill}
                          variant="secondary"
                          className="gap-1 cursor-pointer hover:bg-destructive/20"
                          onClick={() => removeSkill(skill)}
                        >
                          {skill}
                          <X className="size-3" />
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Experience Level
                  </label>
                  <Select
                    value={formData.experienceLevel}
                    onValueChange={(value) => handleChange("experienceLevel", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select experience level" />
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

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Additional Requirements
                  </label>
                  <textarea
                    value={formData.requirements}
                    onChange={(e) => handleChange("requirements", e.target.value)}
                    placeholder="Any other requirements, qualifications, or expectations..."
                    rows={4}
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                  />
                </div>
              </CardContent>
            </Card>
          )}

          {/* Step 3: Compensation */}
          {currentStep === 2 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg flex items-center gap-2">
                  <DollarSign className="size-5 text-neon" />
                  Compensation & Benefits
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Compensation Type
                  </label>
                  <Select
                    value={formData.compensationType}
                    onValueChange={(value) => handleChange("compensationType", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select compensation type" />
                    </SelectTrigger>
                    <SelectContent>
                      {compensationTypes.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Compensation Amount ($)
                  </label>
                  <input
                    type="number"
                    value={formData.compensation}
                    onChange={(e) => handleChange("compensation", e.target.value)}
                    placeholder="0"
                    min="0"
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                  />
                  <p className="text-xs text-muted-foreground mt-1">
                    {formData.compensationType === "hourly"
                      ? "Per hour rate"
                      : formData.compensationType === "fixed"
                      ? "Total project cost"
                      : "Leave blank if negotiable"}
                  </p>
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Benefits & Perks
                  </label>
                  <textarea
                    value={formData.benefits}
                    onChange={(e) => handleChange("benefits", e.target.value)}
                    placeholder="List any benefits, perks, or additional compensation..."
                    rows={4}
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                  />
                </div>
              </CardContent>
            </Card>
          )}

          {/* Step 4: Details */}
          {currentStep === 3 && (
            <Card className="bg-card border-border">
              <CardHeader className="pb-3">
                <CardTitle className="text-lg flex items-center gap-2">
                  <MapPin className="size-5 text-neon" />
                  Location & Timeline
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Location
                  </label>
                  <input
                    type="text"
                    value={formData.location}
                    onChange={(e) => handleChange("location", e.target.value)}
                    placeholder="e.g., San Francisco, CA"
                    disabled={formData.isRemote}
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 disabled:opacity-50"
                  />
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isRemote"
                    checked={formData.isRemote}
                    onChange={(e) => handleChange("isRemote", e.target.checked)}
                    className="w-4 h-4 rounded border-border"
                  />
                  <label htmlFor="isRemote" className="text-sm font-medium">
                    This is a remote opportunity
                  </label>
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Application Deadline
                  </label>
                  <input
                    type="date"
                    value={formData.deadline}
                    onChange={(e) => handleChange("deadline", e.target.value)}
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                  />
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Maximum Applications
                  </label>
                  <input
                    type="number"
                    value={formData.maxApplications}
                    onChange={(e) => handleChange("maxApplications", e.target.value)}
                    placeholder="Leave blank for unlimited"
                    min="1"
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                  />
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
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div>
                    <h3 className="text-xl font-semibold">{formData.title}</h3>
                    <Badge variant="neon" className="mt-2">
                      {formData.type}
                    </Badge>
                  </div>
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      Description
                    </p>
                    <p className="text-sm mt-1">{formData.description}</p>
                  </div>
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg">Requirements</CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(1)}
                    className="absolute top-4 right-4"
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  {formData.requiredSkills.length > 0 && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground mb-2">
                        Required Skills
                      </p>
                      <div className="flex flex-wrap gap-2">
                        {formData.requiredSkills.map((skill) => (
                          <Badge key={skill} variant="secondary">
                            {skill}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}
                  {formData.experienceLevel && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Experience Level
                      </p>
                      <p className="text-sm mt-1">
                        {experienceLevels.find((l) => l.id === formData.experienceLevel)?.label}
                      </p>
                    </div>
                  )}
                  {formData.requirements && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Additional Requirements
                      </p>
                      <p className="text-sm mt-1">{formData.requirements}</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg">Compensation</CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(2)}
                    className="absolute top-4 right-4"
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  {formData.compensationType && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Type
                      </p>
                      <p className="text-sm mt-1">
                        {compensationTypes.find((t) => t.id === formData.compensationType)?.label}
                      </p>
                    </div>
                  )}
                  {formData.compensation && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Amount
                      </p>
                      <p className="text-sm mt-1">${Number(formData.compensation).toLocaleString()}</p>
                    </div>
                  )}
                  {formData.benefits && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Benefits
                      </p>
                      <p className="text-sm mt-1">{formData.benefits}</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              <Card className="bg-card border-border">
                <CardHeader className="pb-3">
                  <CardTitle className="text-lg">Details</CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setCurrentStep(3)}
                    className="absolute top-4 right-4"
                  >
                    Edit
                  </Button>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div>
                    <p className="text-sm font-medium text-muted-foreground">
                      Location
                    </p>
                    <p className="text-sm mt-1">
                      {formData.isRemote ? "Remote" : formData.location || "Not specified"}
                    </p>
                  </div>
                  {formData.deadline && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Deadline
                      </p>
                      <p className="text-sm mt-1">
                        {new Date(formData.deadline).toLocaleDateString()}
                      </p>
                    </div>
                  )}
                  {formData.maxApplications && (
                    <div>
                      <p className="text-sm font-medium text-muted-foreground">
                        Max Applications
                      </p>
                      <p className="text-sm mt-1">{formData.maxApplications}</p>
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
                    Publishing...
                  </>
                ) : (
                  "Publish Opportunity"
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
                This wizard will guide you through posting an opportunity.
                Fill in the required information and optional details to attract the right candidates.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
