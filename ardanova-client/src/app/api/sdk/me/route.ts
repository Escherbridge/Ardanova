import { NextResponse } from "next/server";
import { getSessionOrError } from "../_lib/session";
import { apiClient } from "~/lib/api";
import { runWithActorAssertion } from "~/server/actor-assertion";

/**
 * GET /api/sdk/me
 *
 * The signed-in user's own record. Signed as that user so the .NET directory privacy filter
 * returns their private fields (email, phone, role, trust score) instead of the public view.
 */
export async function GET() {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const response = await runWithActorAssertion(
    { subject: session.user.id, role: session.user.role },
    () => apiClient.users.getById(session.user.id),
  );

  if (response.error || !response.data) {
    return NextResponse.json(
      { error: response.error ?? "User not found" },
      { status: 404 },
    );
  }

  return NextResponse.json(response.data);
}
