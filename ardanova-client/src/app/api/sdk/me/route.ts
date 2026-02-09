import { NextResponse } from "next/server";
import { getSessionOrError } from "../_lib/session";
import { apiClient } from "~/lib/api";

/**
 * GET /api/sdk/me
 *
 * Get the current authenticated user's profile.
 * Requires a valid NextAuth session.
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const response = await apiClient.users.getById(session!.user.id);

  if (response.error || !response.data) {
    return NextResponse.json(
      { error: response.error ?? "User not found" },
      { status: 404 },
    );
  }

  return NextResponse.json(response.data);
}
