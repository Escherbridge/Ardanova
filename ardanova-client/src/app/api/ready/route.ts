import { NextResponse } from "next/server";

import {
  checkBackendReadiness,
  validateReadinessConfiguration,
} from "~/lib/health/readiness";

export const dynamic = "force-dynamic";

export async function GET() {
  const configuration = validateReadinessConfiguration(process.env);
  const backend = await checkBackendReadiness(process.env.API_URL);
  const ready = configuration.ready && backend.ready;

  return NextResponse.json(
    {
      status: ready ? "ready" : "not_ready",
      timestamp: new Date().toISOString(),
      service: "ardanova-client",
      checks: {
        configuration: { ready: configuration.ready },
        backend,
      },
    },
    {
      status: ready ? 200 : 503,
      headers: { "Cache-Control": "no-store" },
    },
  );
}
