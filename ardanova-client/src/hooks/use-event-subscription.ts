"use client";

import { useEffect, useCallback, useRef } from "react";
import type { ArdaNovaEvent, ArdaNovaEventType, EventCallback } from "~/lib/websocket/types";
import { useRealtimeContext } from "~/providers/realtime-provider";

/**
 * Hook for subscribing to a specific event type.
 * Automatically handles subscription cleanup.
 */
export function useEventSubscription<T extends ArdaNovaEvent = ArdaNovaEvent>(
  eventType: ArdaNovaEventType | "*",
  callback: EventCallback<T>,
  deps: React.DependencyList = []
) {
  const { subscribe } = useRealtimeContext();
  const callbackRef = useRef(callback);

  // Keep callback ref updated
  useEffect(() => {
    callbackRef.current = callback;
  }, [callback]);

  // Stable callback that uses ref
  const stableCallback = useCallback((event: T) => {
    callbackRef.current(event);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    const unsubscribe = subscribe(eventType, stableCallback);
    return unsubscribe;
  }, [eventType, stableCallback, subscribe]);
}

/**
 * Hook for subscribing to multiple event types at once.
 */
export function useEventSubscriptions(
  handlers: Partial<Record<ArdaNovaEventType | "*", EventCallback>>,
  deps: React.DependencyList = []
) {
  const { subscribe } = useRealtimeContext();

  useEffect(() => {
    const unsubscribes = Object.entries(handlers).map(([eventType, handler]) => {
      if (handler) {
        return subscribe(eventType, handler);
      }
      return () => {};
    });

    return () => {
      unsubscribes.forEach((unsubscribe) => unsubscribe());
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [subscribe, ...deps]);
}
