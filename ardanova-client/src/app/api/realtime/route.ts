import { NextRequest, NextResponse } from "next/server";
import { auth } from "~/server/auth";
import { connectionManager } from "~/server/websocket";
import type { ArdaNovaEvent, SubscriptionAction } from "~/lib/websocket/types";

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
        const connectMessage = `event: connected\ndata: ${JSON.stringify({ userId, timestamp: new Date().toISOString() })}\n\n`;
        controller.enqueue(encoder.encode(connectMessage));

        // Subscribe to all events and forward to SSE stream
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
        request.signal.addEventListener("abort", async () => {
          isActive = false;
          clearInterval(heartbeatInterval);
          unsubscribe?.();
          await connectionManager.releaseConnection(userId);
          controller.close();
          console.log(`[SSE] Client disconnected: ${userId}`);
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
      "Connection": "keep-alive",
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

  try {
    const body = await request.json() as SubscriptionAction;
    const client = await connectionManager.getConnection(userId);

    switch (body.action) {
      case "subscribeToProject":
        await client.subscribeToProject(body.payload.projectId);
        break;

      case "unsubscribeFromProject":
        await client.unsubscribeFromProject(body.payload.projectId);
        break;

      case "subscribeToAgency":
        await client.subscribeToAgency(body.payload.agencyId);
        break;

      case "unsubscribeFromAgency":
        await client.unsubscribeFromAgency(body.payload.agencyId);
        break;

      case "subscribeToUser":
        await client.subscribeToUser(body.payload.userId);
        break;

      case "unsubscribeFromUser":
        await client.unsubscribeFromUser(body.payload.userId);
        break;

      case "subscribeToAll":
        await client.subscribeToAll();
        break;

      case "unsubscribeFromAll":
        await client.unsubscribeFromAll();
        break;

      default:
        return NextResponse.json(
          { error: "Unknown action" },
          { status: 400 }
        );
    }

    // Release the connection ref since POST is a one-shot
    await connectionManager.releaseConnection(userId);

    return NextResponse.json({ success: true });
  } catch (error) {
    console.error("[SSE POST] Error:", error);
    return NextResponse.json(
      { error: error instanceof Error ? error.message : "Unknown error" },
      { status: 500 }
    );
  }
}
