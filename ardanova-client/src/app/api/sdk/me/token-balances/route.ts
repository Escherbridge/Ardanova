import { NextResponse } from "next/server";
import { apiClient } from "~/lib/api";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { getSessionOrError } from "../../_lib/session";

/**
 * GET /api/sdk/me/token-balances
 *
 * Get all token balances for the current authenticated user (portfolio rows).
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const response = await runWithActorAssertion(
    { subject: session!.user.id, role: session!.user.role },
    () => apiClient.tokenBalances.getPortfolio(),
  );
  if (response.error) {
    return NextResponse.json({ error: response.error }, { status: response.status });
  }
  return NextResponse.json(response.data?.holdings ?? []);
}
