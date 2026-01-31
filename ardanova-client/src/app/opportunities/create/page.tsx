"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Plus, X, Info, Briefcase, DollarSign } from "lucide-react";

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

const jobTypes = [
  { id: "Bounty", label: "Bounty" },
  { id: "Freelance", label: "Freelance" },
  { id: "Contract", label: "Contract" },
  { id: "Part-time", label: "Part-time" },
  { id: "Full-time", label: "Full-time" },
];

const compensationTypes = [
  { id: "fixed", label: "Fixed Price" },
  { id: "hourly", label: "Hourly Rate" },
  { id: "negotiable", label: "Negotiable" },
];

const experienceLevels = [
  { id: "entry", label: "Entry Level" },
  { id: "intermediate", label: "Intermediate" },
  { id: "senior", label: "Senior" },
  { id: "expert", label: "Expert" },
];

export default function CreateOpportunityPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    type: "",
    skills: [] as string[],
    compensationType: "",
    compensationAmount: "",
    location: "",
    experienceLevel: "",
    deadline: "",
  });
  const [newSkill, setNewSkill] = useState("");
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
    // TODO: Implement opportunity creation API
    setTimeout(() => {
      router.push("/marketplace");
    }, 1000);
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/marketplace">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Opportunities
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon-pink/20 rounded-lg flex items-center justify-center">
              <Briefcase className="h-5 w-5 text-neon-pink" />
            </div>
            Post a Job
          </h1>
          <p className="text-muted-foreground mt-2">
            Create an opportunity to find talented contributors for your project
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Job Info */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Job Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Job Title <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Build Mobile App for EcoWaste Platform"
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
                  placeholder="Describe the job requirements, responsibilities, and deliverables..."
                  rows={5}
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

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Job Type <span className="text-neon">*</span>
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
                      {jobTypes.map((type) => (
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

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Experience Level
                  </label>
                  <Select
                    value={formData.experienceLevel}
                    onValueChange={(value) =>
                      handleChange("experienceLevel", value)
                    }
                  >
                    <SelectTrigger>
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
            </CardContent>
          </Card>

          {/* Skills */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Required Skills</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <input
                  type="text"
                  value={newSkill}
                  onChange={(e) => setNewSkill(e.target.value)}
                  placeholder="e.g., React, TypeScript, Node.js"
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
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              {formData.skills.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {formData.skills.map((skill) => (
                    <Badge
                      key={skill}
                      variant="secondary"
                      className="gap-1 cursor-pointer hover:bg-destructive/20"
                      onClick={() => removeSkill(skill)}
                    >
                      {skill}
                      <X className="h-3 w-3" />
                    </Badge>
                  ))}
                </div>
              )}
              {errors.skills && (
                <p className="text-sm text-destructive">{errors.skills}</p>
              )}
            </CardContent>
          </Card>

          {/* Compensation */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <DollarSign className="h-5 w-5 text-neon-green" />
                Compensation
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Payment Type
                  </label>
                  <Select
                    value={formData.compensationType}
                    onValueChange={(value) =>
                      handleChange("compensationType", value)
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select type" />
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
                    Amount ($)
                  </label>
                  <input
                    type="number"
                    value={formData.compensationAmount}
                    onChange={(e) =>
                      handleChange("compensationAmount", e.target.value)
                    }
                    placeholder="0"
                    min="0"
                    className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                  />
                </div>
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Location</label>
                <input
                  type="text"
                  value={formData.location}
                  onChange={(e) => handleChange("location", e.target.value)}
                  placeholder="e.g., Remote, New York, NY"
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50"
                />
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
            </CardContent>
          </Card>

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
                  Posting Job...
                </>
              ) : (
                "Post Job"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/marketplace">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
            <Info className="h-5 w-5 text-neon mt-0.5" />
            <div className="text-sm text-muted-foreground">
              <p>
                Your job posting will be visible to all community members. You'll
                receive applications that you can review and respond to.
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
