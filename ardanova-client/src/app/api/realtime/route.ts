import { NextResponse, type NextRequest } from "next/server";
import { auth } from "~/server/auth";
import { connectionManager } from "~/server/websocket";
import {
  subscriptionActionSchema,
  type ArdaNovaEvent,
} from "~/lib/websocket/types";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

/**
 * GET /api/realtime
 * Server-Sent Events endpoint for real-time updates.
 * Requires authentication via NextAuth session.
 */
export async function GET(request: NextRequest) {
  // Validate session
  const session = await auth();
  if (!session?.user?.id) {
    return new NextResponse("Unauthorized", { status: 401 });
  }

  const userId = session.user.id;
  const encoder = new TextEncoder();

  // Track if the stream is still active
  let isActive = true;
  let unsubscribe: (() => void) | null = null;

  const stream = new ReadableStream({
    async start(controller) {
      try {
        // Get or create backend connection for this user
        const client = await connectionManager.getConnection(userId);

        // Send initial connection message
        const connectMessage = `event: connected\ndata: ${JSON.stringify({ timestamp: new Date().toISOString() })}\n\n`;
        controller.enqueue(encoder.encode(connectMessage));

        // Forward only events delivered through the actor's authorized groups.
        unsubscribe = client.on("*", (event: ArdaNovaEvent) => {
          if (!isActive) return;

          try {
            const sseMessage = `event: ${event.eventType}\ndata: ${JSON.stringify(event)}\n\n`;
            controller.enqueue(encoder.encode(sseMessage));
          } catch (error) {
            console.error("[SSE] Error sending event:", error);
          }
        });

        // Send periodic heartbeat to keep connection alive
        const heartbeatInterval = setInterval(() => {
          if (!isActive) {
            clearInterval(heartbeatInterval);
            return;
          }

          try {
            const heartbeat = `:heartbeat ${Date.now()}\n\n`;
            controller.enqueue(encoder.encode(heartbeat));
          } catch {
            clearInterval(heartbeatInterval);
          }
        }, 30000); // Every 30 seconds

        // Handle client disconnect
        request.signal.addEventListener("abort", () => {
          isActive = false;
          clearInterval(heartbeatInterval);
          unsubscribe?.();
          void connectionManager.releaseConnection(userId).catch(() => {
            console.error("[SSE] Failed to release realtime connection");
          });
          try {
            controller.close();
          } catch {
            // Stream already closed
          }
        });
      } catch (error) {
        console.error("[SSE] Error starting stream:", error);
        const errorMessage = `event: error\ndata: ${JSON.stringify({ error: "Failed to connect" })}\n\n`;
        controller.enqueue(encoder.encode(errorMessage));
        controller.close();
      }
    },
  });

  return new NextResponse(stream, {
    headers: {
      "Content-Type": "text/event-stream",
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
      "X-Accel-Buffering": "no", // Disable nginx buffering
    },
  });
}

/**
 * POST /api/realtime
 * Handle subscription commands from the client.
 */
export async function POST(request: NextRequest) {
  // Validate session
  const session = await auth();
  if (!session?.user?.id) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  const userId = session.user.id;

  const json: unknown = await request.json().catch(() => null);
  const parsed = subscriptionActionSchema.safeParse(json);
  if (!parsed.success) {
    return NextResponse.json(
      { error: "Invalid subscription action" },
      { status: 400 },
    );
  }

  let connectionAcquired = false;
  try {
    const client = await connectionManager.getConnection(userId);
    connectionAcquired = true;

    switch (parsed.data.action) {
      case "subscribeToProject":
        await client.subscribeToProject(parsed.data.payload.projectId);
        break;

      case "unsubscribeFromProject":
        await client.unsubscribeFromProject(parsed.data.payload.projectId);
        break;

      case "subscribeToGuild":
        await client.subscribeToGuild(parsed.data.payload.guildId);
        break;

      case "unsubscribeFromGuild":
        await client.unsubscribeFromGuild(parsed.data.payload.guildId);
        break;

      case "subscribeToConversation":
        await client.subscribeToConversation(
          parsed.data.payload.conversationId,
        );
        break;

      case "unsubscribeFromConversation":
        await client.unsubscribeFromConversation(
          parsed.data.payload.conversationId,
        );
        break;
    }

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error("[SSE POST] Subscription command failed", {
      action: parsed.data.action,
      error: error instanceof Error ? error.name : "UnknownError",
    });
    return NextResponse.json(
      { error: "Realtime subscription unavailable" },
      { status: 502 },
    );
  } finally {
    if (connectionAcquired) {
      try {
        await connectionManager.releaseConnection(userId);
      } catch {
        console.error("[SSE POST] Failed to release realtime connection");
      }
    }
  }
}
