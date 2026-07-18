"use client";

import type {
  ArdaNovaEvent,
  ArdaNovaEventType,
  ConnectionState,
  EventCallback,
  SubscriptionAction,
} from "./types";

/**
 * Client-side realtime event client using Server-Sent Events.
 * Connects to /api/realtime for authenticated event streaming.
 */
export class RealtimeClient {
  private eventSource: EventSource | null = null;
  private listeners = new Map<string, Set<EventCallback>>();
  private wildcardListeners = new Set<EventCallback>();
  private connectionState: ConnectionState = "disconnected";
  private reconnectTimeout: NodeJS.Timeout | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private baseReconnectDelay = 1000;

  private stateChangeCallbacks = new Set<(state: ConnectionState) => void>();

  /**
   * Connects to the SSE endpoint.
   */
  connect(): void {
    if (this.eventSource?.readyState === EventSource.OPEN) {
      return;
    }

    this.connectionState = "connecting";
    this.notifyStateChange();

    this.eventSource = new EventSource("/api/realtime");

    this.eventSource.onopen = () => {
      this.connectionState = "connected";
      this.reconnectAttempts = 0;
      this.notifyStateChange();
    };

    this.eventSource.onerror = () => {
      console.error("[RealtimeClient] Connection error");

      if (this.eventSource?.readyState === EventSource.CLOSED) {
        this.connectionState = "disconnected";
        this.notifyStateChange();
        this.scheduleReconnect();
      } else {
        this.connectionState = "error";
        this.notifyStateChange();
      }
    };

    // Handle the generic message event
    this.eventSource.onmessage = (event) => {
      try {
        const data: unknown = JSON.parse(String(event.data));
        if (isArdaNovaEvent(data)) {
          this.emitEvent(data.eventType, data);
        }
      } catch (error) {
        console.error("[RealtimeClient] Failed to parse message:", error);
      }
    };

    // Handle typed events
    const eventTypes: ArdaNovaEventType[] = [
      "user.created",
      "user.updated",
      "user.verified",
      "user.deleted",
      "project.created",
      "project.updated",
      "project.status_changed",
      "project.deleted",
      "project.task_completed",
      "project.member_added",
      "project.member_removed",
      "notification.created",
      "notification.read",
      "notification.all_read",
      "activity.logged",
    ];

    eventTypes.forEach((eventType) => {
      this.eventSource?.addEventListener(eventType, (event) => {
        try {
          const data: unknown = JSON.parse(String(event.data));
          if (isArdaNovaEvent(data)) {
            this.emitEvent(eventType, data);
          }
        } catch (error) {
          console.error(
            `[RealtimeClient] Failed to parse ${eventType} event:`,
            error,
          );
        }
      });
    });

    // Handle error event from server
    this.eventSource.addEventListener("error", () => {
      console.error("[RealtimeClient] Realtime connection reported an error");
    });
  }

  /**
   * Disconnects from the SSE endpoint.
   */
  disconnect(): void {
    if (this.reconnectTimeout) {
      clearTimeout(this.reconnectTimeout);
      this.reconnectTimeout = null;
    }

    this.eventSource?.close();
    this.eventSource = null;
    this.connectionState = "disconnected";
    this.notifyStateChange();
  }

  /**
   * Gets the current connection state.
   */
  getConnectionState(): ConnectionState {
    return this.connectionState;
  }

  /**
   * Subscribes to connection state changes.
   */
  onStateChange(callback: (state: ConnectionState) => void): () => void {
    this.stateChangeCallbacks.add(callback);
    return () => this.stateChangeCallbacks.delete(callback);
  }

  /**
   * Subscribes to a specific event type.
   * @param eventType The event type or "*" for all events
   * @param callback The callback to invoke
   * @returns Unsubscribe function
   */
  on<T extends ArdaNovaEvent = ArdaNovaEvent>(
    eventType: string,
    callback: EventCallback<T>,
  ): () => void {
    if (eventType === "*") {
      this.wildcardListeners.add(callback);
      return () => this.wildcardListeners.delete(callback);
    }

    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, new Set());
    }
    this.listeners.get(eventType)?.add(callback);

    return () => {
      this.listeners.get(eventType)?.delete(callback);
    };
  }

  /**
   * Sends a subscription command to the server.
   */
  async sendCommand(action: SubscriptionAction): Promise<void> {
    const response = await fetch("/api/realtime", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(action),
    });

    if (!response.ok) {
      const error: unknown = await response.json().catch(() => null);
      throw new Error(getErrorMessage(error));
    }
  }

  /**
   * Subscribe to project events.
   */
  async subscribeToProject(projectId: string): Promise<void> {
    await this.sendCommand({
      action: "subscribeToProject",
      payload: { projectId },
    });
  }

  /**
   * Unsubscribe from project events.
   */
  async unsubscribeFromProject(projectId: string): Promise<void> {
    await this.sendCommand({
      action: "unsubscribeFromProject",
      payload: { projectId },
    });
  }

  /**
   * Subscribe to guild events.
   */
  async subscribeToGuild(guildId: string): Promise<void> {
    await this.sendCommand({
      action: "subscribeToGuild",
      payload: { guildId },
    });
  }

  /**
   * Unsubscribe from guild events.
   */
  async unsubscribeFromGuild(guildId: string): Promise<void> {
    await this.sendCommand({
      action: "unsubscribeFromGuild",
      payload: { guildId },
    });
  }

  /**
   * Subscribe to conversation events.
   */
  async subscribeToConversation(conversationId: string): Promise<void> {
    await this.sendCommand({
      action: "subscribeToConversation",
      payload: { conversationId },
    });
  }

  /**
   * Unsubscribe from conversation events.
   */
  async unsubscribeFromConversation(conversationId: string): Promise<void> {
    await this.sendCommand({
      action: "unsubscribeFromConversation",
      payload: { conversationId },
    });
  }

  private emitEvent(eventType: string, event: ArdaNovaEvent): void {
    // Notify type-specific listeners
    const callbacks = this.listeners.get(eventType);
    callbacks?.forEach((callback) => {
      try {
        callback(event);
      } catch (error) {
        console.error(
          `[RealtimeClient] Error in callback for ${eventType}:`,
          error,
        );
      }
    });

    // Notify wildcard listeners
    this.wildcardListeners.forEach((callback) => {
      try {
        callback(event);
      } catch (error) {
        console.error("[RealtimeClient] Error in wildcard callback:", error);
      }
    });
  }

  private notifyStateChange(): void {
    this.stateChangeCallbacks.forEach((callback) => {
      try {
        callback(this.connectionState);
      } catch (error) {
        console.error(
          "[RealtimeClient] Error in state change callback:",
          error,
        );
      }
    });
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      return;
    }

    const delay = Math.min(
      this.baseReconnectDelay * Math.pow(2, this.reconnectAttempts),
      30000,
    );

    this.connectionState = "reconnecting";
    this.notifyStateChange();

    this.reconnectTimeout = setTimeout(() => {
      this.reconnectAttempts++;
      this.connect();
    }, delay);
  }
}

function isArdaNovaEvent(value: unknown): value is ArdaNovaEvent {
  return (
    typeof value === "object" &&
    value !== null &&
    "eventType" in value &&
    typeof value.eventType === "string"
  );
}

function getErrorMessage(value: unknown): string {
  if (typeof value !== "object" || value === null) {
    return "Failed to send command";
  }

  if (!("error" in value) || typeof value.error !== "string") {
    return "Failed to send command";
  }

  return value.error;
}
