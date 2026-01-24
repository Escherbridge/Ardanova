import "server-only";

import * as signalR from "@microsoft/signalr";
import { env } from "~/env";
import type { ArdaNovaEvent, EventCallback } from "~/lib/websocket/types";

/**
 * SignalR client for connecting to the ArdaNova backend hub.
 * This runs on the Next.js server and connects with API key authentication.
 */
export class SignalRBackendClient {
  private connection: signalR.HubConnection | null = null;
  private eventCallbacks: Map<string, Set<EventCallback>> = new Map();
  private wildcardCallbacks: Set<EventCallback> = new Set();
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;

  constructor(private readonly userId: string) {}

  /**
   * Connects to the backend SignalR hub.
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const hubUrl = `${env.API_URL}/hubs/ardanova`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        headers: {
          "X-Api-Key": env.API_KEY,
          "X-User-Id": this.userId,
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop reconnecting
          }
          // Exponential backoff: 1s, 2s, 4s, 8s, ... up to 30s
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register generic event handler
    this.connection.on("ReceiveEvent", (eventType: string, payload: unknown) => {
      this.emitEvent(eventType, payload);
    });

    // Register typed event handlers
    this.connection.on("UserCreated", (data) => this.emitEvent("user.created", data));
    this.connection.on("UserUpdated", (data) => this.emitEvent("user.updated", data));
    this.connection.on("ProjectCreated", (data) => this.emitEvent("project.created", data));
    this.connection.on("ProjectUpdated", (data) => this.emitEvent("project.updated", data));
    this.connection.on("ProjectStatusChanged", (data) => this.emitEvent("project.status_changed", data));
    this.connection.on("TaskCompleted", (data) => this.emitEvent("project.task_completed", data));
    this.connection.on("NotificationReceived", (data) => this.emitEvent("notification.created", data));
    this.connection.on("ActivityLogged", (data) => this.emitEvent("activity.logged", data));

    // Connection state handlers
    this.connection.onreconnecting(() => {
      console.log(`[SignalR] Reconnecting for user ${this.userId}...`);
      this.isConnected = false;
    });

    this.connection.onreconnected(() => {
      console.log(`[SignalR] Reconnected for user ${this.userId}`);
      this.isConnected = true;
      this.reconnectAttempts = 0;
    });

    this.connection.onclose((error) => {
      console.log(`[SignalR] Connection closed for user ${this.userId}`, error?.message);
      this.isConnected = false;
    });

    try {
      await this.connection.start();
      this.isConnected = true;
      console.log(`[SignalR] Connected for user ${this.userId}`);
    } catch (error) {
      console.error(`[SignalR] Failed to connect for user ${this.userId}:`, error);
      throw error;
    }
  }

  /**
   * Disconnects from the SignalR hub.
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.isConnected = false;
      console.log(`[SignalR] Disconnected for user ${this.userId}`);
    }
  }

  /**
   * Checks if connected to the hub.
   */
  getIsConnected(): boolean {
    return this.isConnected;
  }

  /**
   * Subscribes to a specific event type.
   * @returns Unsubscribe function
   */
  on(eventType: string, callback: EventCallback): () => void {
    if (eventType === "*") {
      this.wildcardCallbacks.add(callback);
      return () => this.wildcardCallbacks.delete(callback);
    }

    if (!this.eventCallbacks.has(eventType)) {
      this.eventCallbacks.set(eventType, new Set());
    }
    this.eventCallbacks.get(eventType)!.add(callback);

    return () => {
      this.eventCallbacks.get(eventType)?.delete(callback);
    };
  }

  /**
   * Hub method invocations
   */
  async subscribeToProject(projectId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("SubscribeToProject", projectId);
    console.log(`[SignalR] Subscribed to project ${projectId}`);
  }

  async unsubscribeFromProject(projectId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("UnsubscribeFromProject", projectId);
    console.log(`[SignalR] Unsubscribed from project ${projectId}`);
  }

  async subscribeToAgency(agencyId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("SubscribeToAgency", agencyId);
    console.log(`[SignalR] Subscribed to agency ${agencyId}`);
  }

  async unsubscribeFromAgency(agencyId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("UnsubscribeFromAgency", agencyId);
    console.log(`[SignalR] Unsubscribed from agency ${agencyId}`);
  }

  async subscribeToUser(targetUserId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("SubscribeToUser", targetUserId);
    console.log(`[SignalR] Subscribed to user ${targetUserId}`);
  }

  async unsubscribeFromUser(targetUserId: string): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("UnsubscribeFromUser", targetUserId);
    console.log(`[SignalR] Unsubscribed from user ${targetUserId}`);
  }

  async subscribeToAll(): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("SubscribeToAll");
    console.log("[SignalR] Subscribed to all events");
  }

  async unsubscribeFromAll(): Promise<void> {
    if (!this.connection) throw new Error("Not connected");
    await this.connection.invoke("UnsubscribeFromAll");
    console.log("[SignalR] Unsubscribed from all events");
  }

  /**
   * Emits an event to all registered callbacks.
   */
  private emitEvent(eventType: string, payload: unknown): void {
    const event = {
      eventType,
      ...payload,
    } as ArdaNovaEvent;

    // Notify type-specific listeners
    const callbacks = this.eventCallbacks.get(eventType);
    callbacks?.forEach((callback) => {
      try {
        callback(event);
      } catch (error) {
        console.error(`[SignalR] Error in callback for ${eventType}:`, error);
      }
    });

    // Notify wildcard listeners
    this.wildcardCallbacks.forEach((callback) => {
      try {
        callback(event);
      } catch (error) {
        console.error(`[SignalR] Error in wildcard callback:`, error);
      }
    });
  }
}
