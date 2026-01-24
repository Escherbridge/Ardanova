import "server-only";

import { SignalRBackendClient } from "./signalr-backend-client";

/**
 * Manages per-user SignalR connections to the backend.
 * Uses reference counting to share connections across multiple SSE streams for the same user.
 */
class ConnectionManager {
  private connections: Map<string, SignalRBackendClient> = new Map();
  private refCounts: Map<string, number> = new Map();
  private connectPromises: Map<string, Promise<SignalRBackendClient>> = new Map();

  /**
   * Gets or creates a connection for the specified user.
   * Increments the reference count.
   */
  async getConnection(userId: string): Promise<SignalRBackendClient> {
    // If there's an existing connection, increment ref count and return it
    const existingClient = this.connections.get(userId);
    if (existingClient?.getIsConnected()) {
      this.refCounts.set(userId, (this.refCounts.get(userId) ?? 0) + 1);
      return existingClient;
    }

    // If there's a connection in progress, wait for it
    const existingPromise = this.connectPromises.get(userId);
    if (existingPromise) {
      const client = await existingPromise;
      this.refCounts.set(userId, (this.refCounts.get(userId) ?? 0) + 1);
      return client;
    }

    // Create a new connection
    const connectPromise = this.createConnection(userId);
    this.connectPromises.set(userId, connectPromise);

    try {
      const client = await connectPromise;
      this.refCounts.set(userId, 1);
      return client;
    } finally {
      this.connectPromises.delete(userId);
    }
  }

  /**
   * Releases a connection for the specified user.
   * Decrements the reference count and disconnects when it reaches zero.
   */
  async releaseConnection(userId: string): Promise<void> {
    const count = (this.refCounts.get(userId) ?? 1) - 1;
    this.refCounts.set(userId, count);

    if (count <= 0) {
      const client = this.connections.get(userId);
      if (client) {
        await client.disconnect();
        this.connections.delete(userId);
        this.refCounts.delete(userId);
        console.log(`[ConnectionManager] Released connection for user ${userId}`);
      }
    } else {
      console.log(`[ConnectionManager] Decremented ref count for user ${userId} to ${count}`);
    }
  }

  /**
   * Creates a new SignalR connection for the user.
   */
  private async createConnection(userId: string): Promise<SignalRBackendClient> {
    const client = new SignalRBackendClient(userId);

    try {
      await client.connect();
      this.connections.set(userId, client);
      console.log(`[ConnectionManager] Created connection for user ${userId}`);
      return client;
    } catch (error) {
      console.error(`[ConnectionManager] Failed to connect for user ${userId}:`, error);
      throw error;
    }
  }

  /**
   * Gets the current connection count for debugging.
   */
  getConnectionCount(): number {
    return this.connections.size;
  }

  /**
   * Gets connection stats for debugging.
   */
  getStats(): { userId: string; refCount: number; connected: boolean }[] {
    return Array.from(this.connections.entries()).map(([userId, client]) => ({
      userId,
      refCount: this.refCounts.get(userId) ?? 0,
      connected: client.getIsConnected(),
    }));
  }
}

// Singleton instance for the Next.js server
export const connectionManager = new ConnectionManager();
