import { NextResponse } from "next/server";
import { auth } from "~/server/auth";

/**
 * Get the authenticated session for SDK route handlers.
 * Returns the session if valid, or a 401 NextResponse if not.
 */
export async function getSessionOrError() {
  const session = await auth();

  if (!session?.user?.id) {
    return {
      session: null,
      error: NextResponse.json(
        { error: "Unauthorized — valid session required" },
        { status: 401 },
      ),
    };
  }

  return { session, error: null };
}
