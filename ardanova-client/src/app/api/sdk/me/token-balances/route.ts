import { NextResponse } from "next/server";
import { apiClient } from "~/lib/api";
import { getSessionOrError } from "../../_lib/session";

/**
 * GET /api/sdk/me/token-balances
 *
 * Get all token balances for the current authenticated user (portfolio rows).
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const response = await apiClient.tokenBalances.getPortfolio(session!.user.id);
  if (response.error) {
    return NextResponse.json({ error: response.error }, { status: response.status });
  }
  return NextResponse.json(response.data?.balances ?? []);
}
