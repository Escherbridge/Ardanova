"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Plus, X, Info } from "lucide-react";

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

export default function CreateProjectPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    title: "",
    problemStatement: "",
    solution: "",
    category: "",
    targetAudience: "",
    expectedImpact: "",
    timeline: "",
    tags: [] as string[],
    fundingGoal: "",
  });
  const [newTag, setNewTag] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = api.project.create.useMutation({
    onSuccess: (data) => {
      router.push(`/projects/${data.slug}`);
    },
    onError: (error) => {
      setErrors({ submit: error.message });
    },
  });

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
    if (!formData.problemStatement.trim())
      newErrors.problemStatement = "Problem statement is required";
    if (formData.problemStatement.length < 10)
      newErrors.problemStatement = "Must be at least 10 characters";
    if (!formData.solution.trim()) newErrors.solution = "Solution is required";
    if (formData.solution.length < 10)
      newErrors.solution = "Must be at least 10 characters";
    if (!formData.category) newErrors.category = "Category is required";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    createMutation.mutate({
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
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/projects">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Projects
            </Link>
          </Button>
          <h1 className="text-3xl font-bold">Create New Project</h1>
          <p className="text-muted-foreground mt-2">
            Define your problem and solution to launch your project on ArdaNova
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Project Title */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                Project Overview
              </CardTitle>
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
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p className="text-sm text-destructive mt-1">{errors.title}</p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Problem Definition */}
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
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.problemStatement
                      ? "border-destructive"
                      : "border-border"
                  }`}
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
                  onChange={(e) => handleChange("targetAudience", e.target.value)}
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
                  onChange={(e) => handleChange("expectedImpact", e.target.value)}
                  placeholder="What impact do you expect to achieve?"
                  rows={3}
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                />
              </div>
            </CardContent>
          </Card>

          {/* Solution Definition */}
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
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.solution ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.solution && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.solution}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Project Details */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Project Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
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

          {/* Tags */}
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
              disabled={createMutation.isPending}
            >
              {createMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Creating Project...
                </>
              ) : (
                "Create Project"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/projects">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
            <Info className="h-5 w-5 text-neon mt-0.5" />
            <div className="text-sm text-muted-foreground">
              <p>
                Your project will be created as a draft. You can publish it once
                you're ready to share with the community.
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
