import { NextResponse, type NextRequest } from "next/server";
import { apiClient } from "~/lib/api";
import { getSessionOrError } from "../_lib/session";

/**
 * POST /api/sdk/actions
 *
 * Report an in-game action. Awards XP via the platform XP API (`source: GAME_SDK`).
 *
 * Body: { actionType: string, taskId: string, metadata?: object }
 * Returns: { awarded: boolean, tokensEarned: number, newBalance: number, message: string }
 */
export async function POST(request: NextRequest) {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  try {
    const body = (await request.json()) as {
      actionType?: string;
      taskId?: string;
      metadata?: Record<string, unknown>;
    };

    if (!body.actionType || !body.taskId) {
      return NextResponse.json(
        { error: "actionType and taskId are required" },
        { status: 400 },
      );
    }

    const award = await apiClient.xpEvents.award({
      userId: session!.user.id,
      eventType: body.actionType,
      amount: 1,
      source: "GAME_SDK",
      sourceId: body.taskId,
      metadata: body.metadata ? JSON.stringify(body.metadata) : undefined,
    });

    if (award.error || !award.data) {
      return NextResponse.json({
        awarded: false,
        tokensEarned: 0,
        newBalance: 0,
        message: award.error ?? "Could not record action",
      });
    }

    return NextResponse.json({
      awarded: true,
      tokensEarned: 0,
      newBalance: 0,
      message: "Action recorded and XP awarded",
    });
  } catch {
    return NextResponse.json(
      { error: "Invalid request body" },
      { status: 400 },
    );
  }
}
