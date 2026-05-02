import { NextResponse, type NextRequest } from "next/server";
import { apiClient } from "~/lib/api";
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

  const response = await apiClient.tokenBalances.getPortfolio(session!.user.id);
  if (response.error) {
    return NextResponse.json({ error: response.error }, { status: response.status });
  }

  const row = response.data?.balances?.find((b) => b.projectTokenConfigId === projectId);
  return NextResponse.json({
    userId: session!.user.id,
    projectTokenConfigId: projectId,
    balance: row?.balance ?? 0,
  });
}
