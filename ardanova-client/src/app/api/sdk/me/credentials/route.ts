import { NextResponse } from "next/server";
import { getSessionOrError } from "../../_lib/session";
import { apiClient } from "~/lib/api";

/**
 * GET /api/sdk/me/credentials
 *
 * Get all membership credentials for the current authenticated user.
 * Returns only the user's own credentials — never other users' data.
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const response = await apiClient.membershipCredentials.getByUserId(session!.user.id);

  if (response.error) {
    return NextResponse.json(
      { error: response.error },
      { status: 500 },
    );
  }

  return NextResponse.json(response.data ?? []);
}
