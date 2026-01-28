"use client";

import { api } from "~/trpc/react";
import { toast } from "sonner";

/**
 * Custom hook for shop mutations with optimistic updates
 * Provides create, update, delete, follow, unfollow operations
 */
export function useShopMutations() {
  const utils = api.useUtils();

  // Create shop mutation
  const createShop = api.shop.create.useMutation({
    onSuccess: (data) => {
      toast.success("Shop created successfully!");
      utils.shop.getAll.invalidate();
      utils.shop.getMyShops.invalidate();
    },
    onError: (error) => {
      toast.error(error.message || "Failed to create shop");
    },
  });

  // Update shop mutation
  const updateShop = api.shop.update.useMutation({
    onSuccess: (data) => {
      toast.success("Shop updated successfully!");
      utils.shop.getById.invalidate({ id: data.id });
      utils.shop.getAll.invalidate();
    },
    onError: (error) => {
      toast.error(error.message || "Failed to update shop");
    },
  });

  // Delete shop mutation
  const deleteShop = api.shop.delete.useMutation({
    onSuccess: () => {
      toast.success("Shop deleted successfully!");
      utils.shop.getAll.invalidate();
      utils.shop.getMyShops.invalidate();
    },
    onError: (error) => {
      toast.error(error.message || "Failed to delete shop");
    },
  });

  // Follow shop mutation with optimistic update
  const followShop = api.shop.follow.useMutation({
    onMutate: async ({ shopId }) => {
      // Cancel any outgoing refetches
      await utils.shop.getById.cancel({ id: shopId });

      // Snapshot the previous value
      const previousShop = utils.shop.getById.getData({ id: shopId });

      // Return context with the snapshotted value
      return { previousShop };
    },
    onError: (err, { shopId }, context) => {
      // Rollback to previous value on error
      if (context?.previousShop) {
        utils.shop.getById.setData({ id: shopId }, context.previousShop);
      }
      toast.error(err.message || "Failed to follow shop");
    },
    onSuccess: () => {
      toast.success("Now following this shop!");
    },
    onSettled: (_, __, { shopId }) => {
      // Always refetch after error or success
      utils.shop.getById.invalidate({ id: shopId });
    },
  });

  // Unfollow shop mutation with optimistic update
  const unfollowShop = api.shop.unfollow.useMutation({
    onMutate: async ({ shopId }) => {
      await utils.shop.getById.cancel({ id: shopId });
      const previousShop = utils.shop.getById.getData({ id: shopId });
      return { previousShop };
    },
    onError: (err, { shopId }, context) => {
      if (context?.previousShop) {
        utils.shop.getById.setData({ id: shopId }, context.previousShop);
      }
      toast.error(err.message || "Failed to unfollow shop");
    },
    onSuccess: () => {
      toast.success("Unfollowed shop");
    },
    onSettled: (_, __, { shopId }) => {
      utils.shop.getById.invalidate({ id: shopId });
    },
  });

  return {
    createShop,
    updateShop,
    deleteShop,
    followShop,
    unfollowShop,
    isCreating: createShop.isPending,
    isUpdating: updateShop.isPending,
    isDeleting: deleteShop.isPending,
    isFollowing: followShop.isPending,
    isUnfollowing: unfollowShop.isPending,
  };
}

// Export types for use in components
export type ShopMutations = ReturnType<typeof useShopMutations>;
