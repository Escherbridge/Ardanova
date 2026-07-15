import { NextResponse, type NextRequest } from "next/server";
import { apiClient } from "~/lib/api";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { getSessionOrError } from "../../../_lib/session";

/**
 * GET /api/sdk/me/token-balances/[projectId]
 *
 * `projectId` is the project **token config** id (`projectTokenConfigId`) used by the token balance API.
 */
export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ projectId: string }> },
) {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const { projectId } = await params;

  const response = await runWithActorAssertion(
    { subject: session!.user.id, role: session!.user.role },
    () => apiClient.tokenBalances.getPortfolio(),
  );
  if (response.error) {
    return NextResponse.json({ error: response.error }, { status: response.status });
  }

  const row = response.data?.holdings?.find((b) => b.projectTokenConfigId === projectId);
  return NextResponse.json({
    userId: session!.user.id,
    projectTokenConfigId: projectId,
    balance: row?.balance ?? 0,
  });
}
