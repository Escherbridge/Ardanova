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
  private listeners: Map<string, Set<EventCallback>> = new Map();
  private wildcardListeners: Set<EventCallback> = new Set();
  private connectionState: ConnectionState = "disconnected";
  private reconnectTimeout: NodeJS.Timeout | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private baseReconnectDelay = 1000;

  private stateChangeCallbacks: Set<(state: ConnectionState) => void> = new Set();

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
      console.log("[RealtimeClient] Connected");
    };

    this.eventSource.onerror = (error) => {
      console.error("[RealtimeClient] Error:", error);

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
        const data = JSON.parse(event.data) as ArdaNovaEvent;
        this.emitEvent(data.eventType, data);
      } catch (error) {
        console.error("[RealtimeClient] Failed to parse message:", error);
      }
    };

    // Handle the connected event
    this.eventSource.addEventListener("connected", (event) => {
      const messageEvent = event as MessageEvent;
      console.log("[RealtimeClient] Received connected event:", messageEvent.data);
    });

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
          const messageEvent = event as MessageEvent;
          const data = JSON.parse(messageEvent.data) as ArdaNovaEvent;
          this.emitEvent(eventType, data);
        } catch (error) {
          console.error(`[RealtimeClient] Failed to parse ${eventType} event:`, error);
        }
      });
    });

    // Handle error event from server
    this.eventSource.addEventListener("error", (event) => {
      const messageEvent = event as MessageEvent;
      if (messageEvent.data) {
        try {
          const errorData = JSON.parse(messageEvent.data);
          console.error("[RealtimeClient] Server error:", errorData);
        } catch {
          // Ignore parse errors for SSE error events
        }
      }
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
    console.log("[RealtimeClient] Disconnected");
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
    callback: EventCallback<T>
  ): () => void {
    if (eventType === "*") {
      this.wildcardListeners.add(callback as EventCallback);
      return () => this.wildcardListeners.delete(callback as EventCallback);
    }

    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, new Set());
    }
    this.listeners.get(eventType)!.add(callback as EventCallback);

    return () => {
      this.listeners.get(eventType)?.delete(callback as EventCallback);
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
      const error = await response.json();
      throw new Error(error.error ?? "Failed to send command");
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
   * Subscribe to agency events.
   */
  async subscribeToAgency(agencyId: string): Promise<void> {
    await this.sendCommand({
      action: "subscribeToAgency",
      payload: { agencyId },
    });
  }

  /**
   * Unsubscribe from agency events.
   */
  async unsubscribeFromAgency(agencyId: string): Promise<void> {
    await this.sendCommand({
      action: "unsubscribeFromAgency",
      payload: { agencyId },
    });
  }

  /**
   * Subscribe to all events.
   */
  async subscribeToAll(): Promise<void> {
    await this.sendCommand({
      action: "subscribeToAll",
      payload: {},
    });
  }

  /**
   * Unsubscribe from all events.
   */
  async unsubscribeFromAll(): Promise<void> {
    await this.sendCommand({
      action: "unsubscribeFromAll",
      payload: {},
    });
  }

  private emitEvent(eventType: string, event: ArdaNovaEvent): void {
    // Notify type-specific listeners
    const callbacks = this.listeners.get(eventType);
    callbacks?.forEach((callback) => {
      try {
        callback(event);
      } catch (error) {
        console.error(`[RealtimeClient] Error in callback for ${eventType}:`, error);
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
        console.error("[RealtimeClient] Error in state change callback:", error);
      }
    });
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log("[RealtimeClient] Max reconnect attempts reached");
      return;
    }

    const delay = Math.min(
      this.baseReconnectDelay * Math.pow(2, this.reconnectAttempts),
      30000
    );

    console.log(`[RealtimeClient] Scheduling reconnect in ${delay}ms (attempt ${this.reconnectAttempts + 1})`);

    this.connectionState = "reconnecting";
    this.notifyStateChange();

    this.reconnectTimeout = setTimeout(() => {
      this.reconnectAttempts++;
      this.connect();
    }, delay);
  }
}
