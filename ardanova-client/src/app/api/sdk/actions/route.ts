import { NextResponse } from "next/server";
import { getSessionOrError } from "../_lib/session";

/**
 * POST /api/sdk/actions
 *
 * Client-reported actions cannot award XP until the backend can verify and
 * idempotently consume a platform event.
 */
export async function POST() {
  const { error } = await getSessionOrError();
  if (error) return error;

  return NextResponse.json(
    {
      awarded: false,
      tokensEarned: 0,
      newBalance: 0,
      message:
        "Client-reported XP is disabled until actions can be verified by the platform.",
    },
    {
      status: 501,
      headers: { "Cache-Control": "no-store" },
    },
  );
}
