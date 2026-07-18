"use client";

import { useEffect, useRef, useCallback, useState } from "react";
import { useSession } from "next-auth/react";
import { RealtimeClient } from "~/lib/websocket/realtime-client";
import type {
  ArdaNovaEvent,
  ConnectionState,
  EventCallback,
} from "~/lib/websocket/types";

/**
 * Main hook for real-time functionality.
 * Automatically connects when the user is authenticated.
 */
export function useRealtime() {
  const { status } = useSession();
  const clientRef = useRef<RealtimeClient | null>(null);
  const [connectionState, setConnectionState] =
    useState<ConnectionState>("disconnected");

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
      callback: EventCallback<T>,
    ) => {
      return clientRef.current?.on(eventType, callback) ?? (() => undefined);
    },
    [],
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
   * Subscribe to guild events.
   */
  const subscribeToGuild = useCallback(async (guildId: string) => {
    await clientRef.current?.subscribeToGuild(guildId);
  }, []);

  /**
   * Unsubscribe from guild events.
   */
  const unsubscribeFromGuild = useCallback(async (guildId: string) => {
    await clientRef.current?.unsubscribeFromGuild(guildId);
  }, []);

  /**
   * Subscribe to conversation events.
   */
  const subscribeToConversation = useCallback(
    async (conversationId: string) => {
      await clientRef.current?.subscribeToConversation(conversationId);
    },
    [],
  );

  /**
   * Unsubscribe from conversation events.
   */
  const unsubscribeFromConversation = useCallback(
    async (conversationId: string) => {
      await clientRef.current?.unsubscribeFromConversation(conversationId);
    },
    [],
  );

  return {
    isConnected: connectionState === "connected",
    isConnecting: connectionState === "connecting",
    isReconnecting: connectionState === "reconnecting",
    connectionState,
    subscribe,
    subscribeToProject,
    unsubscribeFromProject,
    subscribeToGuild,
    unsubscribeFromGuild,
    subscribeToConversation,
    unsubscribeFromConversation,
    client: clientRef.current,
  };
}
