"use client";

import { useEffect, useRef, useCallback, useState } from "react";
import { useSession } from "next-auth/react";
import { RealtimeClient } from "~/lib/websocket/realtime-client";
import type { ArdaNovaEvent, ConnectionState, EventCallback } from "~/lib/websocket/types";

/**
 * Main hook for real-time functionality.
 * Automatically connects when the user is authenticated.
 */
export function useRealtime() {
  const { status } = useSession();
  const clientRef = useRef<RealtimeClient | null>(null);
  const [connectionState, setConnectionState] = useState<ConnectionState>("disconnected");

  // Initialize client on mount
  useEffect(() => {
    if (status !== "authenticated") {
      return;
    }

    const client = new RealtimeClient();
    clientRef.current = client;

    // Subscribe to state changes
    const unsubscribeState = client.onStateChange(setConnectionState);

    // Connect
    client.connect();

    return () => {
      unsubscribeState();
      client.disconnect();
      clientRef.current = null;
    };
  }, [status]);

  /**
   * Subscribe to a specific event type.
   */
  const subscribe = useCallback(
    <T extends ArdaNovaEvent = ArdaNovaEvent>(
      eventType: string,
      callback: EventCallback<T>
    ) => {
      return clientRef.current?.on(eventType, callback) ?? (() => {});
    },
    []
  );

  /**
   * Subscribe to project events.
   */
  const subscribeToProject = useCallback(async (projectId: string) => {
    await clientRef.current?.subscribeToProject(projectId);
  }, []);

  /**
   * Unsubscribe from project events.
   */
  const unsubscribeFromProject = useCallback(async (projectId: string) => {
    await clientRef.current?.unsubscribeFromProject(projectId);
  }, []);

  /**
   * Subscribe to agency events.
   */
  const subscribeToAgency = useCallback(async (agencyId: string) => {
    await clientRef.current?.subscribeToAgency(agencyId);
  }, []);

  /**
   * Unsubscribe from agency events.
   */
  const unsubscribeFromAgency = useCallback(async (agencyId: string) => {
    await clientRef.current?.unsubscribeFromAgency(agencyId);
  }, []);

  /**
   * Subscribe to conversation events.
   */
  const subscribeToConversation = useCallback(async (conversationId: string) => {
    await clientRef.current?.subscribeToConversation(conversationId);
  }, []);

  /**
   * Unsubscribe from conversation events.
   */
  const unsubscribeFromConversation = useCallback(async (conversationId: string) => {
    await clientRef.current?.unsubscribeFromConversation(conversationId);
  }, []);

  /**
   * Subscribe to all events.
   */
  const subscribeToAll = useCallback(async () => {
    await clientRef.current?.subscribeToAll();
  }, []);

  /**
   * Unsubscribe from all events.
   */
  const unsubscribeFromAll = useCallback(async () => {
    await clientRef.current?.unsubscribeFromAll();
  }, []);

  return {
    isConnected: connectionState === "connected",
    isConnecting: connectionState === "connecting",
    isReconnecting: connectionState === "reconnecting",
    connectionState,
    subscribe,
    subscribeToProject,
    unsubscribeFromProject,
    subscribeToAgency,
    unsubscribeFromAgency,
    subscribeToConversation,
    unsubscribeFromConversation,
    subscribeToAll,
    unsubscribeFromAll,
    client: clientRef.current,
  };
}
