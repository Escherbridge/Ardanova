import { NextResponse, type NextRequest } from "next/server";
import { getSessionOrError } from "../../../_lib/session";

/**
 * GET /api/sdk/me/token-balances/[projectId]
 *
 * Get token balance for a specific project for the current user.
 * This will be implemented when the tokenomics backend (Track 09) is complete.
 * For now returns a zero balance placeholder.
 */
export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ projectId: string }> },
) {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const { projectId } = await params;

  // TODO: Wire up to token balance backend once Track 09 is implemented
  // const response = await apiClient.tokenBalances.getByProjectAndUser(projectId, session!.user.id);
  return NextResponse.json({
    userId: session!.user.id,
    projectId,
    balance: 0,
  });
}
