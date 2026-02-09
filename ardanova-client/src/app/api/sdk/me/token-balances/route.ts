import { NextResponse } from "next/server";
import { getSessionOrError } from "../../_lib/session";

/**
 * GET /api/sdk/me/token-balances
 *
 * Get all token balances for the current authenticated user.
 * This will be implemented when the tokenomics backend (Track 09) is complete.
 * For now returns an empty array as a placeholder.
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  // TODO: Wire up to token balance backend once Track 09 is implemented
  // const response = await apiClient.tokenBalances.getByUserId(session!.user.id);
  return NextResponse.json([]);
}
