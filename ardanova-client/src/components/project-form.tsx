"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "~/components/ui/select";
import { Badge } from "~/components/ui/badge";
import { Plus, X, FileText, Video, Image, Presentation, Loader2 } from "lucide-react";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import type { RouterOutputs } from "~/trpc/react";

const OTHER_CATEGORY_MAX_LENGTH = 50;

type Project = RouterOutputs["project"]["getById"];

interface ProjectFormProps {
  mode?: "create" | "edit";
  project?: Project;
}

interface ProjectFormData {
  title: string;
  problem: {
    description: string;
    impact: string;
    targetAudience: string;
  };
  solution: {
    description: string;
    approach: string;
    expectedOutcome: string;
  };
  categories: string[];
  otherCategory: string;
  difficulty: string;
  estimatedDuration: string;
  resources: string[];
  skills: string[];
  attachments: {
    presentations: File[];
    videos: File[];
    images: File[];
    documents: File[];
  };
}

export function ProjectForm({ mode = "create", project }: ProjectFormProps) {
  const [formData, setFormData] = useState<ProjectFormData>({
    title: "",
    problem: {
      description: "",
      impact: "",
      targetAudience: "",
    },
    solution: {
      description: "",
      approach: "",
      expectedOutcome: "",
    },
    categories: [],
    otherCategory: "",
    difficulty: "",
    estimatedDuration: "",
    resources: [],
    skills: [],
    attachments: {
      presentations: [],
      videos: [],
      images: [],
      documents: [],
    },
  });

  // Pre-fill form data when editing
  useEffect(() => {
    if (mode === "edit" && project) {
      setFormData({
        title: project.title,
        problem: {
          description: project.problemStatement,
          impact: project.expectedImpact || "",
          targetAudience: project.targetAudience || "",
        },
        solution: {
          description: project.solution,
          approach: project.solution, // Using solution as approach for now
          expectedOutcome: project.expectedImpact || "",
        },
        categories: project.categories ? project.categories : project.category ? [project.category] : [],
        otherCategory: "",
        difficulty: "Intermediate", // Default value
        estimatedDuration: project.timeline || "",
        resources: project.tags ? project.tags.split(",").filter(tag => tag.trim()) : [],
        skills: [], // Skills not in current schema
        attachments: {
          presentations: [],
          videos: project.videos ? project.videos.split(",").filter(v => v.trim()).map(v => new File([], v.trim())) : [],
          images: project.images ? project.images.split(",").filter(i => i.trim()).map(i => new File([], i.trim())) : [],
          documents: project.documents ? project.documents.split(",").filter(d => d.trim()).map(d => new File([], d.trim())) : [],
        },
      });
    }
  }, [mode, project]);

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [successMessage, setSuccessMessage] = useState("");
  const [newResource, setNewResource] = useState("");
  const [newSkill, setNewSkill] = useState("");

  const categoryOptions = [
    { id: "TECHNOLOGY", label: "Technology" },
    { id: "HEALTHCARE", label: "Healthcare" },
    { id: "EDUCATION", label: "Education" },
    { id: "ENVIRONMENT", label: "Environment" },
    { id: "SOCIAL_IMPACT", label: "Social Impact" },
    { id: "BUSINESS", label: "Business" },
    { id: "ARTS_CULTURE", label: "Arts & Culture" },
    { id: "AGRICULTURE", label: "Agriculture" },
    { id: "FINANCE", label: "Finance" },
  ];

  const difficulties = ["Beginner", "Intermediate", "Advanced", "Expert"];
  const durations = ["1-2 weeks", "1-3 months", "3-6 months", "6+ months"];

  const handleInputChange = (field: string, value: string) => {
    if (field.includes(".")) {
      const [section, key] = field.split(".");
      if (section === "problem") {
        setFormData(prev => ({
          ...prev,
          problem: {
            ...prev.problem,
            [key as keyof typeof prev.problem]: value,
          },
        }));
      } else if (section === "solution") {
        setFormData(prev => ({
          ...prev,
          solution: {
            ...prev.solution,
            [key as keyof typeof prev.solution]: value,
          },
        }));
      }
    } else {
      setFormData(prev => ({ ...prev, [field]: value }));
    }
  };

  const addResource = () => {
    if (newResource.trim()) {
      setFormData(prev => ({
        ...prev,
        resources: [...prev.resources, newResource.trim()],
      }));
      setNewResource("");
    }
  };

  const removeResource = (index: number) => {
    setFormData(prev => ({
      ...prev,
      resources: prev.resources.filter((_, i) => i !== index),
    }));
  };

  const addSkill = () => {
    if (newSkill.trim()) {
      setFormData(prev => ({
        ...prev,
        skills: [...prev.skills, newSkill.trim()],
      }));
      setNewSkill("");
    }
  };

  const removeSkill = (index: number) => {
    setFormData(prev => ({
      ...prev,
      skills: prev.skills.filter((_, i) => i !== index),
    }));
  };

  const handleFileUpload = (type: keyof ProjectFormData["attachments"], files: FileList | null) => {
    if (files) {
      setFormData(prev => ({
        ...prev,
        attachments: {
          ...prev.attachments,
          [type]: [...prev.attachments[type], ...Array.from(files)],
        },
      }));
    }
  };

  const removeFile = (type: keyof ProjectFormData["attachments"], index: number) => {
    setFormData(prev => ({
      ...prev,
      attachments: {
        ...prev.attachments,
        [type]: prev.attachments[type].filter((_, i) => i !== index),
      },
    }));
  };

  const router = useRouter();
  const createProject = api.project.create.useMutation({
    onSuccess: (project) => {
      console.log("Project created successfully:", project);
      setSuccessMessage("Project created successfully! Redirecting...");
      // Redirect to the project detail page
      setTimeout(() => {
        router.push(`/projects/${project.slug}`);
      }, 1500);
    },
    onError: (error) => {
      console.error("Error creating project:", error);
      setErrors({ submit: "Failed to create project. Please try again." });
    },
  });

  const updateProject = api.project.update.useMutation({
    onSuccess: (project) => {
      console.log("Project updated successfully:", project);
      setSuccessMessage("Project updated successfully! Redirecting...");
      // Redirect to the project detail page
      setTimeout(() => {
        router.push(`/projects/${project.slug}`);
      }, 1500);
    },
    onError: (error) => {
      console.error("Error updating project:", error);
      setErrors({ submit: "Failed to update project. Please try again." });
    },
  });

    const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    // Required field validations
    if (!formData.title.trim()) {
      newErrors.title = "Project title is required";
    }

    if (!formData.problem.description.trim()) {
      newErrors.problemDescription = "Problem description is required";
    }

    if (!formData.solution.description.trim()) {
      newErrors.solutionDescription = "Solution description is required";
    }

    if (formData.categories.length === 0) {
      newErrors.categories = "Select at least one category";
    }
    if (formData.categories.includes("OTHER") && !formData.otherCategory.trim()) {
      newErrors.otherCategory = "Please specify your category";
    }
    if (formData.otherCategory.length > OTHER_CATEGORY_MAX_LENGTH) {
      newErrors.otherCategory = `Max ${OTHER_CATEGORY_MAX_LENGTH} characters`;
    }

    // Set errors
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // Clear previous errors
    setErrors({});

    // Validate form
    if (!validateForm()) {
      return;
    }

    // Build categories list, replacing "OTHER" with the custom value
    const resolvedCategories = formData.categories.map((c) =>
      c === "OTHER" ? formData.otherCategory.trim() : c
    );

    // Prepare data for API
    const projectData = {
      title: formData.title,
      description: formData.solution.description,
      problemStatement: formData.problem.description,
      solution: formData.solution.description,
      categories: resolvedCategories,
      targetAudience: formData.problem.targetAudience,
      expectedImpact: formData.problem.impact,
      timeline: formData.estimatedDuration,
      tags: [...formData.resources, ...formData.skills].join(", "),
      images: formData.attachments.images.map(f => f.name).join(", "),
      videos: formData.attachments.videos.map(f => f.name).join(", "),
      documents: formData.attachments.documents.map(f => f.name).join(", "),
    };

    if (mode === "edit" && project) {
      updateProject.mutate({
        id: project.id,
        ...projectData,
      });
    } else {
      createProject.mutate(projectData);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-4 sm:p-6 space-y-6 sm:space-y-8">
      <div className="text-center">
        <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
          {mode === "edit" ? "Edit Project" : "Create New Project"}
        </h1>
        <p className="text-sm sm:text-base text-gray-600 mt-2">
          {mode === "edit" 
            ? "Update your project details and make changes as needed"
            : "Define your problem and solution to get started on your ArdaNova project"
          }
        </p>
      </div>

                    {/* Success/Error Messages */}
              {successMessage && (
                <div className="p-4 bg-green-50 border border-green-200 rounded-lg">
                  <p className="text-green-800 text-sm flex items-center gap-2">
                    <span className="w-2 h-2 bg-green-600 rounded-full"></span>
                    {successMessage}
                  </p>
                </div>
              )}
              
              {errors.submit && (
                <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-red-800 text-sm flex items-center gap-2">
                    <span className="w-2 h-2 bg-red-600 rounded-full"></span>
                    {errors.submit}
                  </p>
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-6 sm:space-y-8">
        {/* Project Title */}
        <Card>
          <CardHeader>
            <CardTitle>Project Overview</CardTitle>
            <CardDescription>
              Start with a clear, compelling title for your project
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
                                    <div>
                        <Label htmlFor="title">Project Title *</Label>
                        <Input
                          id="title"
                          value={formData.title}
                          onChange={(e) => handleInputChange("title", e.target.value)}
                          placeholder="e.g., Sustainable Water Filtration System for Rural Communities"
                          className={errors.title ? "border-red-500 focus:border-red-500" : ""}
                        />
                        {errors.title && (
                          <p className="text-sm text-red-600 mt-1 flex items-center gap-1">
                            <span className="w-1 h-1 bg-red-600 rounded-full"></span>
                            {errors.title}
                          </p>
                        )}
                      </div>
            </div>
          </CardContent>
        </Card>

        {/* Problem Definition */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <div className="w-8 h-8 bg-red-100 rounded-full flex items-center justify-center">
                <span className="text-red-600 font-bold">?</span>
              </div>
              Problem Definition
            </CardTitle>
            <CardDescription>
              Clearly describe the problem you're trying to solve
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
                                <div>
                      <Label htmlFor="problem-description">Problem Description *</Label>
                      <Textarea
                        id="problem-description"
                        value={formData.problem.description}
                        onChange={(e) => handleInputChange("problem.description", e.target.value)}
                        placeholder="Describe the problem in detail. What is the issue? Who does it affect? Why is it important?"
                        rows={4}
                        className={errors.problemDescription ? "border-red-500 focus:border-red-500" : ""}
                      />
                      {errors.problemDescription && (
                        <p className="text-sm text-red-600 mt-1 flex items-center gap-1">
                          <span className="w-1 h-1 bg-red-600 rounded-full"></span>
                          {errors.problemDescription}
                        </p>
                      )}
                    </div>
            <div>
              <Label htmlFor="problem-impact">Impact & Urgency</Label>
              <Textarea
                id="problem-impact"
                value={formData.problem.impact}
                onChange={(e) => handleInputChange("problem.impact", e.target.value)}
                placeholder="What is the scale of this problem? How many people are affected? What are the consequences if not solved?"
                rows={3}
              />
            </div>
            <div>
              <Label htmlFor="problem-audience">Target Audience</Label>
              <Input
                id="problem-audience"
                value={formData.problem.targetAudience}
                onChange={(e) => handleInputChange("problem.targetAudience", e.target.value)}
                placeholder="Who are the primary beneficiaries of solving this problem?"
              />
            </div>
          </CardContent>
        </Card>

        {/* Solution Definition */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                <span className="text-green-600 font-bold">💡</span>
              </div>
              Solution Definition
            </CardTitle>
            <CardDescription>
              Outline your proposed solution and approach
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
                                <div>
                      <Label htmlFor="solution-description">Solution Description *</Label>
                      <Textarea
                        id="solution-description"
                        value={formData.solution.description}
                        onChange={(e) => handleInputChange("solution.description", e.target.value)}
                        placeholder="Describe your proposed solution. How does it address the problem? What makes it innovative?"
                        rows={4}
                        className={errors.solutionDescription ? "border-red-500 focus:border-red-500" : ""}
                      />
                      {errors.solutionDescription && (
                        <p className="text-sm text-red-600 mt-1 flex items-center gap-1">
                          <span className="w-1 h-1 bg-red-600 rounded-full"></span>
                          {errors.solutionDescription}
                        </p>
                      )}
                    </div>
            <div>
              <Label htmlFor="solution-approach">Implementation Approach</Label>
              <Textarea
                id="solution-approach"
                value={formData.solution.approach}
                onChange={(e) => handleInputChange("solution.approach", e.target.value)}
                placeholder="How do you plan to implement this solution? What are the key steps?"
                rows={3}
              />
            </div>
            <div>
              <Label htmlFor="solution-outcome">Expected Outcome</Label>
              <Textarea
                id="solution-outcome"
                value={formData.solution.expectedOutcome}
                onChange={(e) => handleInputChange("solution.expectedOutcome", e.target.value)}
                placeholder="What do you expect to achieve? What are the measurable results?"
                rows={3}
              />
            </div>
          </CardContent>
        </Card>

        {/* Project Details */}
        <Card>
          <CardHeader>
            <CardTitle>Project Details</CardTitle>
            <CardDescription>
              Set project parameters and requirements
            </CardDescription>
          </CardHeader>
                            <CardContent className="px-4 sm:px-6 space-y-4">
                    <div className="sm:col-span-2 lg:col-span-3">
                      <Label>Categories *</Label>
                      <div className={cn(
                        "flex flex-wrap gap-2 mt-2 p-3 rounded-md border",
                        errors.categories ? "border-red-500" : "border-input"
                      )}>
                        {categoryOptions.map((cat) => {
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
                                  ? "bg-primary text-primary-foreground border-primary"
                                  : "bg-muted text-muted-foreground border-input hover:border-primary/50 hover:text-foreground"
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
                              ? "bg-primary text-primary-foreground border-primary"
                              : "bg-muted text-muted-foreground border-input hover:border-primary/50 hover:text-foreground"
                          )}
                        >
                          Other
                        </button>
                      </div>
                      {errors.categories && (
                        <p className="text-sm text-red-600 mt-1 flex items-center gap-1">
                          <span className="w-1 h-1 bg-red-600 rounded-full"></span>
                          {errors.categories}
                        </p>
                      )}
                      {formData.categories.includes("OTHER") && (
                        <div className="mt-3">
                          <Label>Specify other category</Label>
                          <div className="relative mt-1">
                            <Input
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
                              className={errors.otherCategory ? "border-red-500 focus:border-red-500 pr-16" : "pr-16"}
                            />
                            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">
                              {formData.otherCategory.length}/{OTHER_CATEGORY_MAX_LENGTH}
                            </span>
                          </div>
                          {errors.otherCategory && (
                            <p className="text-sm text-red-600 mt-1 flex items-center gap-1">
                              <span className="w-1 h-1 bg-red-600 rounded-full"></span>
                              {errors.otherCategory}
                            </p>
                          )}
                        </div>
                      )}
                    </div>
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4">
                      <div>
                        <Label htmlFor="difficulty">Difficulty Level</Label>
                        <Select value={formData.difficulty} onValueChange={(value) => handleInputChange("difficulty", value)}>
                          <SelectTrigger>
                            <SelectValue placeholder="Select difficulty" />
                          </SelectTrigger>
                          <SelectContent>
                            {difficulties.map((difficulty) => (
                              <SelectItem key={difficulty} value={difficulty}>
                                {difficulty}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                      <div>
                        <Label htmlFor="duration">Estimated Duration</Label>
                        <Select value={formData.estimatedDuration} onValueChange={(value) => handleInputChange("estimatedDuration", value)}>
                          <SelectTrigger>
                            <SelectValue placeholder="Select duration" />
                          </SelectTrigger>
                          <SelectContent>
                            {durations.map((duration) => (
                              <SelectItem key={duration} value={duration}>
                                {duration}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                  </CardContent>
        </Card>

        {/* Resources & Skills */}
        <Card>
          <CardHeader>
            <CardTitle>Resources & Skills</CardTitle>
            <CardDescription>
              What resources and skills are needed for this project?
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Resources */}
            <div>
              <Label>Required Resources</Label>
              <div className="flex gap-2 mt-2">
                <Input
                  value={newResource}
                  onChange={(e) => setNewResource(e.target.value)}
                  placeholder="e.g., Funding, Equipment, Materials"
                  onKeyPress={(e) => e.key === "Enter" && (e.preventDefault(), addResource())}
                />
                <Button type="button" onClick={addResource} size="sm">
                  <Plus className="w-4 h-4" />
                </Button>
              </div>
              {formData.resources.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-3">
                  {formData.resources.map((resource, index) => (
                    <Badge key={index} variant="secondary" className="flex items-center gap-1">
                      {resource}
                      <button
                        type="button"
                        onClick={() => removeResource(index)}
                        className="ml-1 hover:text-red-600"
                      >
                        <X className="w-3 h-3" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
            </div>

            {/* Skills */}
            <div>
              <Label>Required Skills</Label>
              <div className="flex gap-2 mt-2">
                <Input
                  value={newSkill}
                  onChange={(e) => setNewSkill(e.target.value)}
                  placeholder="e.g., Programming, Design, Marketing"
                  onKeyPress={(e) => e.key === "Enter" && (e.preventDefault(), addSkill())}
                />
                <Button type="button" onClick={addSkill} size="sm">
                  <Plus className="w-4 h-4" />
                </Button>
              </div>
              {formData.skills.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-3">
                  {formData.skills.map((skill, index) => (
                    <Badge key={index} variant="secondary" className="flex items-center gap-1">
                      {skill}
                      <button
                        type="button"
                        onClick={() => removeSkill(index)}
                        className="ml-1 hover:text-red-600"
                      >
                        <X className="w-3 h-3" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Attachments */}
        <Card>
          <CardHeader>
            <CardTitle>Project Attachments</CardTitle>
            <CardDescription>
              Upload supporting materials for your project
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Presentations */}
            <div>
              <Label className="flex items-center gap-2">
                <Presentation className="w-4 h-4" />
                Presentations
              </Label>
              <div className="mt-2">
                <Input
                  type="file"
                  accept=".ppt,.pptx,.pdf"
                  multiple
                  onChange={(e) => handleFileUpload("presentations", e.target.files)}
                  className="cursor-pointer"
                />
                {formData.attachments.presentations.length > 0 && (
                  <div className="mt-2 space-y-2">
                    {formData.attachments.presentations.map((file, index) => (
                      <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                        <span className="text-sm">{file.name}</span>
                        <button
                          type="button"
                          onClick={() => removeFile("presentations", index)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Videos */}
            <div>
              <Label className="flex items-center gap-2">
                <Video className="w-4 h-4" />
                Videos
              </Label>
              <div className="mt-2">
                <Input
                  type="file"
                  accept="video/*"
                  multiple
                  onChange={(e) => handleFileUpload("videos", e.target.files)}
                  className="cursor-pointer"
                />
                {formData.attachments.videos.length > 0 && (
                  <div className="mt-2 space-y-2">
                    {formData.attachments.videos.map((file, index) => (
                      <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                        <span className="text-sm">{file.name}</span>
                        <button
                          type="button"
                          onClick={() => removeFile("videos", index)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Images */}
            <div>
              <Label className="flex items-center gap-2">
                <Image className="w-4 h-4" />
                Images
              </Label>
              <div className="mt-2">
                <Input
                  type="file"
                  accept="image/*"
                  multiple
                  onChange={(e) => handleFileUpload("images", e.target.files)}
                  className="cursor-pointer"
                />
                {formData.attachments.images.length > 0 && (
                  <div className="mt-2 space-y-2">
                    {formData.attachments.images.map((file, index) => (
                      <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                        <span className="text-sm">{file.name}</span>
                        <button
                          type="button"
                          onClick={() => removeFile("images", index)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Documents */}
            <div>
              <Label className="flex items-center gap-2">
                <FileText className="w-4 h-4" />
                Documents
              </Label>
              <div className="mt-2">
                <Input
                  type="file"
                  accept=".pdf,.doc,.docx,.txt"
                  multiple
                  onChange={(e) => handleFileUpload("documents", e.target.files)}
                  className="cursor-pointer"
                />
                {formData.attachments.documents.length > 0 && (
                  <div className="mt-2 space-y-2">
                    {formData.attachments.documents.map((file, index) => (
                      <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                        <span className="text-sm">{file.name}</span>
                        <button
                          type="button"
                          onClick={() => removeFile("documents", index)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

                        {/* Submit Button */}
                <div className="flex justify-center">
                  <Button
                    type="submit"
                    size="lg"
                    className="px-6 sm:px-8 w-full sm:w-auto"
                    disabled={createProject.isPending || updateProject.isPending}
                  >
                    {(createProject.isPending || updateProject.isPending) ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                        {mode === "edit" ? "Updating Project..." : "Creating Project..."}
                      </>
                    ) : (
                      mode === "edit" ? "Update Project" : "Create Project"
                    )}
                  </Button>
                </div>
      </form>
    </div>
  );
}
