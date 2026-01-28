"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import { Card, CardContent } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { cn } from "~/lib/utils";
import {
  Loader2,
  Flag,
  Plus,
  Check,
  Calendar,
  Trash2,
  Edit,
} from "lucide-react";

interface MilestonesTabProps {
  projectId: string;
  isOwner: boolean;
}

export default function MilestonesTab({
  projectId,
  isOwner,
}: MilestonesTabProps) {
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    targetDate: "",
  });

  const utils = api.useUtils();

  // Queries
  const {
    data: milestones,
    isLoading,
    error,
  } = api.project.getMilestones.useQuery({ projectId });

  // Mutations
  const addMutation = api.project.addMilestone.useMutation({
    onMutate: async (newMilestone) => {
      await utils.project.getMilestones.cancel({ projectId });
      const previous = utils.project.getMilestones.getData({ projectId });

      const optimisticMilestone = {
        id: `temp-${Date.now()}`,
        projectId,
        title: newMilestone.title,
        description: newMilestone.description ?? null,
        targetDate: newMilestone.targetDate,
        completed: false,
        completedAt: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      utils.project.getMilestones.setData({ projectId }, (old) => [
        ...(old ?? []),
        optimisticMilestone,
      ]);

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getMilestones.setData({ projectId }, context.previous);
      }
    },
    onSuccess: () => {
      setShowAddForm(false);
      setFormData({ title: "", description: "", targetDate: "" });
    },
    onSettled: () => {
      void utils.project.getMilestones.invalidate({ projectId });
    },
  });

  const updateMutation = api.project.updateMilestone.useMutation({
    onMutate: async (updatedMilestone) => {
      await utils.project.getMilestones.cancel({ projectId });
      const previous = utils.project.getMilestones.getData({ projectId });

      utils.project.getMilestones.setData({ projectId }, (old) =>
        old?.map((m) =>
          m.id === updatedMilestone.milestoneId
            ? {
                ...m,
                title: updatedMilestone.title ?? m.title,
                description: updatedMilestone.description ?? m.description,
                targetDate: updatedMilestone.targetDate ?? m.targetDate,
                updatedAt: new Date().toISOString(),
              }
            : m
        )
      );

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getMilestones.setData({ projectId }, context.previous);
      }
    },
    onSuccess: () => {
      setEditingId(null);
      setFormData({ title: "", description: "", targetDate: "" });
    },
    onSettled: () => {
      void utils.project.getMilestones.invalidate({ projectId });
    },
  });

  const deleteMutation = api.project.deleteMilestone.useMutation({
    onMutate: async ({ milestoneId }) => {
      await utils.project.getMilestones.cancel({ projectId });
      const previous = utils.project.getMilestones.getData({ projectId });

      utils.project.getMilestones.setData({ projectId }, (old) =>
        old?.filter((m) => m.id !== milestoneId)
      );

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getMilestones.setData({ projectId }, context.previous);
      }
    },
    onSettled: () => {
      void utils.project.getMilestones.invalidate({ projectId });
    },
  });

  const completeMutation = api.project.completeMilestone.useMutation({
    onMutate: async ({ milestoneId }) => {
      await utils.project.getMilestones.cancel({ projectId });
      const previous = utils.project.getMilestones.getData({ projectId });

      utils.project.getMilestones.setData({ projectId }, (old) =>
        old?.map((m) =>
          m.id === milestoneId
            ? {
                ...m,
                completed: true,
                completedAt: new Date().toISOString(),
              }
            : m
        )
      );

      return { previous };
    },
    onError: (err, vars, context) => {
      if (context?.previous) {
        utils.project.getMilestones.setData({ projectId }, context.previous);
      }
    },
    onSettled: () => {
      void utils.project.getMilestones.invalidate({ projectId });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.title.trim() || !formData.targetDate) return;

    if (editingId) {
      updateMutation.mutate({
        milestoneId: editingId,
        title: formData.title,
        description: formData.description || undefined,
        targetDate: formData.targetDate,
      });
    } else {
      addMutation.mutate({
        projectId,
        title: formData.title,
        description: formData.description || undefined,
        targetDate: formData.targetDate,
      });
    }
  };

  const handleEdit = (milestone: {
    id: string;
    title: string;
    description: string | null;
    targetDate: string;
  }) => {
    setEditingId(milestone.id);
    setFormData({
      title: milestone.title,
      description: milestone.description ?? "",
      targetDate: milestone.targetDate,
    });
    setShowAddForm(true);
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setFormData({ title: "", description: "", targetDate: "" });
    setShowAddForm(false);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <p className="text-sm text-destructive">
            Error loading milestones: {error.message}
          </p>
        </CardContent>
      </Card>
    );
  }

  const sortedMilestones = [...(milestones ?? [])].sort(
    (a, b) =>
      new Date(a.targetDate).getTime() - new Date(b.targetDate).getTime()
  );

  return (
    <div className="space-y-6">
      {/* Add Milestone Button */}
      {isOwner && !showAddForm && (
        <Button onClick={() => setShowAddForm(true)} className="w-full">
          <Plus className="mr-2 h-4 w-4" />
          Add Milestone
        </Button>
      )}

      {/* Add/Edit Form */}
      {isOwner && showAddForm && (
        <Card>
          <CardContent className="pt-6">
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label
                  htmlFor="title"
                  className="block text-sm font-medium mb-2"
                >
                  Title
                </label>
                <input
                  id="title"
                  type="text"
                  value={formData.title}
                  onChange={(e) =>
                    setFormData({ ...formData, title: e.target.value })
                  }
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  placeholder="Milestone title"
                  required
                />
              </div>

              <div>
                <label
                  htmlFor="description"
                  className="block text-sm font-medium mb-2"
                >
                  Description (optional)
                </label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  placeholder="Milestone description"
                  rows={3}
                />
              </div>

              <div>
                <label
                  htmlFor="targetDate"
                  className="block text-sm font-medium mb-2"
                >
                  Target Date
                </label>
                <input
                  id="targetDate"
                  type="date"
                  value={formData.targetDate}
                  onChange={(e) =>
                    setFormData({ ...formData, targetDate: e.target.value })
                  }
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  required
                />
              </div>

              <div className="flex gap-2">
                <Button
                  type="submit"
                  disabled={
                    addMutation.isPending ||
                    updateMutation.isPending ||
                    !formData.title.trim() ||
                    !formData.targetDate
                  }
                >
                  {(addMutation.isPending || updateMutation.isPending) && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {editingId ? "Update" : "Add"} Milestone
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleCancelEdit}
                >
                  Cancel
                </Button>
              </div>
              {(addMutation.error || updateMutation.error) && (
                <div className="p-3 bg-destructive/10 border border-destructive/30 rounded text-sm text-destructive">
                  Error: {addMutation.error?.message || updateMutation.error?.message}
                </div>
              )}
            </form>
          </CardContent>
        </Card>
      )}

      {/* Empty State */}
      {sortedMilestones.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Flag className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-sm text-muted-foreground text-center">
              No milestones yet.
              {isOwner && " Add your first milestone to track progress!"}
            </p>
          </CardContent>
        </Card>
      )}

      {/* Milestones Timeline */}
      {sortedMilestones.length > 0 && (
        <div className="relative space-y-8">
          {sortedMilestones.map((milestone, index) => (
            <div key={milestone.id} className="relative flex gap-4">
              {/* Timeline Line */}
              {index < sortedMilestones.length - 1 && (
                <div className="absolute left-4 top-10 bottom-0 w-0.5 bg-border -translate-x-1/2" />
              )}

              {/* Timeline Marker */}
              <div className="relative flex-shrink-0">
                <div
                  className={cn(
                    "flex h-8 w-8 items-center justify-center rounded-full border-2 bg-background",
                    milestone.completed
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-muted-foreground"
                  )}
                >
                  {milestone.completed ? (
                    <Check className="h-4 w-4" />
                  ) : (
                    <span className="text-xs font-semibold">{index + 1}</span>
                  )}
                </div>
              </div>

              {/* Milestone Card */}
              <Card className="flex-1">
                <CardContent className="pt-6">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 space-y-2">
                      <div className="flex items-center gap-2">
                        <h3 className="font-semibold">{milestone.title}</h3>
                        {milestone.completed && (
                          <Badge variant="secondary" className="text-xs">
                            Completed
                          </Badge>
                        )}
                      </div>

                      {milestone.description && (
                        <p className="text-sm text-muted-foreground">
                          {milestone.description}
                        </p>
                      )}

                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Calendar className="h-4 w-4" />
                          <span>Target: {formatDate(milestone.targetDate)}</span>
                        </div>
                        {milestone.completed && milestone.completedAt && (
                          <div className="flex items-center gap-1">
                            <Check className="h-4 w-4" />
                            <span>
                              Completed: {formatDate(milestone.completedAt)}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Actions */}
                    {isOwner && (
                      <div className="flex gap-2">
                        {!milestone.completed && (
                          <>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => handleEdit(milestone)}
                              disabled={
                                updateMutation.isPending ||
                                deleteMutation.isPending ||
                                completeMutation.isPending
                              }
                            >
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() =>
                                completeMutation.mutate({
                                  milestoneId: milestone.id,
                                })
                              }
                              disabled={
                                updateMutation.isPending ||
                                deleteMutation.isPending ||
                                completeMutation.isPending
                              }
                            >
                              {completeMutation.isPending &&
                              completeMutation.variables?.milestoneId ===
                                milestone.id ? (
                                <Loader2 className="h-4 w-4 animate-spin" />
                              ) : (
                                <Check className="h-4 w-4" />
                              )}
                            </Button>
                          </>
                        )}
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() =>
                            deleteMutation.mutate({ milestoneId: milestone.id })
                          }
                          disabled={
                            updateMutation.isPending ||
                            deleteMutation.isPending ||
                            completeMutation.isPending
                          }
                        >
                          {deleteMutation.isPending &&
                          deleteMutation.variables?.milestoneId ===
                            milestone.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
