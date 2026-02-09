import { NextResponse, type NextRequest } from "next/server";
import { getSessionOrError } from "../_lib/session";

/**
 * POST /api/sdk/actions
 *
 * Report an in-game action to earn equity/XP rewards.
 * The platform determines the reward based on configured task allocations.
 * Games never directly mint or transfer tokens — they only report actions.
 *
 * Body: { actionType: string, taskId: string, metadata?: object }
 * Returns: { awarded: boolean, tokensEarned: number, newBalance: number, message: string }
 *
 * This will be fully implemented when the tokenomics backend (Track 09) is complete.
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

    // TODO: Wire up to game action processing backend once Track 09 is implemented
    // const response = await apiClient.gameActions.report({
    //   userId: session!.user.id,
    //   actionType: body.actionType,
    //   taskId: body.taskId,
    //   metadata: body.metadata,
    // });

    return NextResponse.json({
      awarded: false,
      tokensEarned: 0,
      newBalance: 0,
      message: "Game action recorded — tokenomics backend not yet active",
    });
  } catch {
    return NextResponse.json(
      { error: "Invalid request body" },
      { status: 400 },
    );
  }
}
