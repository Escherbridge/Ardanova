"use client";

import { api } from "~/trpc/react";
import { toast } from "sonner";

/**
 * Custom hook for opportunity mutations with toast notifications
 * Provides create, update, delete, close, and submitApplication operations
 */
export function useOpportunityMutations() {
  const utils = api.useUtils();

  // Create opportunity mutation
  const createOpportunity = api.opportunity.create.useMutation({
    onSuccess: (data) => {
      toast.success("Opportunity created successfully!");
      utils.opportunity.getAll.invalidate();
      utils.opportunity.getMyOpportunities.invalidate();
    },
    onError: (error) => {
      const message = error.data?.code === "FORBIDDEN"
        ? "You don't have permission to create this opportunity"
        : error.message || "Failed to create opportunity";
      toast.error(message);
    },
  });

  // Update opportunity mutation
  const updateOpportunity = api.opportunity.update.useMutation({
    onSuccess: (data) => {
      if (!data) return;
      toast.success("Opportunity updated successfully!");
      utils.opportunity.getById.invalidate({ id: data.id });
      utils.opportunity.getAll.invalidate();
      utils.opportunity.getMyOpportunities.invalidate();
    },
    onError: (error) => {
      const message = error.data?.code === "FORBIDDEN"
        ? "You don't have permission to update this opportunity"
        : error.message || "Failed to update opportunity";
      toast.error(message);
    },
  });

  // Delete opportunity mutation
  const deleteOpportunity = api.opportunity.delete.useMutation({
    onSuccess: () => {
      toast.success("Opportunity deleted successfully!");
      utils.opportunity.getAll.invalidate();
      utils.opportunity.getMyOpportunities.invalidate();
    },
    onError: (error) => {
      const message = error.data?.code === "FORBIDDEN"
        ? "You don't have permission to delete this opportunity"
        : error.message || "Failed to delete opportunity";
      toast.error(message);
    },
  });

  // Close opportunity mutation
  const closeOpportunity = api.opportunity.close.useMutation({
    onSuccess: (_data, variables: { id?: string }) => {
      toast.success("Opportunity closed!");
      utils.opportunity.getById.invalidate({ id: variables.id ?? "" });
      utils.opportunity.getAll.invalidate();
      utils.opportunity.getMyOpportunities.invalidate();
    },
    onError: (error) => {
      const message = error.data?.code === "FORBIDDEN"
        ? "You don't have permission to close this opportunity"
        : error.message || "Failed to close opportunity";
      toast.error(message);
    },
  });

  // Submit application mutation
  const submitApplication = api.opportunity.submitApplication.useMutation({
    onSuccess: (_data, variables) => {
      if (!variables) return;
      toast.success("Application submitted successfully!");
      utils.opportunity.getById.invalidate({ id: variables.opportunityId });
      utils.opportunity.getAll.invalidate();
    },
    onError: (error) => {
      const message = error.data?.code === "FORBIDDEN"
        ? "You don't have permission to perform this action"
        : error.message || "Failed to submit application";
      toast.error(message);
    },
  });

  return {
    createOpportunity,
    updateOpportunity,
    deleteOpportunity,
    closeOpportunity,
    submitApplication,
    isCreating: createOpportunity.isPending,
    isUpdating: updateOpportunity.isPending,
    isDeleting: deleteOpportunity.isPending,
    isClosing: closeOpportunity.isPending,
    isSubmitting: submitApplication.isPending,
  };
}

// Export types for use in components
export type OpportunityMutations = ReturnType<typeof useOpportunityMutations>;
