"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect } from "react";
import type { ArdaNovaEvent, ArdaNovaEventType } from "~/lib/websocket/types";
import { useRealtimeContext } from "~/providers/realtime-provider";

/**
 * Hook for automatically invalidating React Query cache when real-time events arrive.
 *
 * @example
 * // Invalidate projects query when any project event occurs
 * useRealtimeInvalidation("project.created", [["projects"]]);
 */
export function useRealtimeInvalidation(
  eventType: ArdaNovaEventType | "*",
  queryKeys: unknown[][]
) {
  const queryClient = useQueryClient();
  const { subscribe } = useRealtimeContext();

  useEffect(() => {
    const unsubscribe = subscribe(eventType, () => {
      queryKeys.forEach((key) => {
        void queryClient.invalidateQueries({ queryKey: key });
      });
    });

    return unsubscribe;
  }, [eventType, queryClient, queryKeys, subscribe]);
}

/**
 * Hook for optimistically updating React Query cache when real-time events arrive.
 *
 * @example
 * // Update a specific project in cache when it's updated
 * useRealtimeUpdate<Project[], ProjectUpdatedEvent>(
 *   "project.updated",
 *   ["projects"],
 *   (oldData, event) => {
 *     if (!oldData) return oldData;
 *     return oldData.map(p =>
 *       p.id === event.projectId ? { ...p, ...event } : p
 *     );
 *   }
 * );
 */
export function useRealtimeUpdate<TData, TEvent extends ArdaNovaEvent = ArdaNovaEvent>(
  eventType: ArdaNovaEventType | "*",
  queryKey: unknown[],
  updater: (oldData: TData | undefined, event: TEvent) => TData | undefined
) {
  const queryClient = useQueryClient();
  const { subscribe } = useRealtimeContext();

  const stableUpdater = useCallback(updater, [updater]);

  useEffect(() => {
    const unsubscribe = subscribe(eventType, (event) => {
      queryClient.setQueryData<TData>(queryKey, (oldData) =>
        stableUpdater(oldData, event as TEvent)
      );
    });

    return unsubscribe;
  }, [eventType, queryClient, queryKey, stableUpdater, subscribe]);
}

/**
 * Hook for adding new items to a list in React Query cache.
 *
 * @example
 * // Add new projects to the list when created
 * useRealtimeAppend<Project[], ProjectCreatedEvent>(
 *   "project.created",
 *   ["projects"],
 *   (event) => ({
 *     id: event.projectId,
 *     title: event.title,
 *     // ... other fields
 *   })
 * );
 */
export function useRealtimeAppend<TData extends unknown[], TEvent extends ArdaNovaEvent>(
  eventType: ArdaNovaEventType,
  queryKey: unknown[],
  transformer: (event: TEvent) => TData[number],
  options?: { prepend?: boolean }
) {
  const queryClient = useQueryClient();
  const { subscribe } = useRealtimeContext();

  useEffect(() => {
    const unsubscribe = subscribe(eventType, (event) => {
      queryClient.setQueryData<TData>(queryKey, (oldData) => {
        if (!oldData) return oldData;

        const newItem = transformer(event as TEvent);

        if (options?.prepend) {
          return [newItem, ...oldData] as TData;
        }
        return [...oldData, newItem] as TData;
      });
    });

    return unsubscribe;
  }, [eventType, options?.prepend, queryClient, queryKey, subscribe, transformer]);
}

/**
 * Hook for removing items from a list in React Query cache.
 *
 * @example
 * // Remove deleted projects from the list
 * useRealtimeRemove<Project[], ProjectDeletedEvent>(
 *   "project.deleted",
 *   ["projects"],
 *   (event) => (project) => project.id !== event.projectId
 * );
 */
export function useRealtimeRemove<TData extends unknown[], TEvent extends ArdaNovaEvent>(
  eventType: ArdaNovaEventType,
  queryKey: unknown[],
  filterFn: (event: TEvent) => (item: TData[number]) => boolean
) {
  const queryClient = useQueryClient();
  const { subscribe } = useRealtimeContext();

  useEffect(() => {
    const unsubscribe = subscribe(eventType, (event) => {
      queryClient.setQueryData<TData>(queryKey, (oldData) => {
        if (!oldData) return oldData;
        return oldData.filter(filterFn(event as TEvent)) as TData;
      });
    });

    return unsubscribe;
  }, [eventType, filterFn, queryClient, queryKey, subscribe]);
}
