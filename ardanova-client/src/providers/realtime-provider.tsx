"use client";

import React, { createContext, useContext } from "react";
import { useRealtime } from "~/hooks/use-realtime";

type RealtimeContextValue = ReturnType<typeof useRealtime>;

const RealtimeContext = createContext<RealtimeContextValue | null>(null);

interface RealtimeProviderProps {
  children: React.ReactNode;
}

/**
 * Provider component that enables real-time event streaming throughout the app.
 * Wrap your app with this provider to enable real-time features.
 *
 * @example
 * ```tsx
 * // In your root layout
 * <RealtimeProvider>
 *   {children}
 * </RealtimeProvider>
 * ```
 */
export function RealtimeProvider({ children }: RealtimeProviderProps) {
  const realtime = useRealtime();

  return (
    <RealtimeContext.Provider value={realtime}>
      {children}
    </RealtimeContext.Provider>
  );
}

/**
 * Hook to access the realtime context.
 * Must be used within a RealtimeProvider.
 *
 * @example
 * ```tsx
 * const { isConnected, subscribe, subscribeToProject } = useRealtimeContext();
 *
 * // Subscribe to events
 * useEffect(() => {
 *   const unsubscribe = subscribe("project.created", (event) => {
 *     console.log("New project:", event);
 *   });
 *   return unsubscribe;
 * }, [subscribe]);
 * ```
 */
export function useRealtimeContext(): RealtimeContextValue {
  const context = useContext(RealtimeContext);

  if (!context) {
    throw new Error(
      "useRealtimeContext must be used within a RealtimeProvider. " +
      "Wrap your app with <RealtimeProvider> to enable real-time features."
    );
  }

  return context;
}
