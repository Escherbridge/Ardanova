import { NextResponse } from "next/server";

export const dynamic = "force-dynamic";

export function GET() {
  return NextResponse.json(
    {
      status: "alive",
      timestamp: new Date().toISOString(),
      service: "ardanova-client",
      uptimeSeconds: Math.floor(process.uptime()),
      environment: process.env.RAILWAY_ENVIRONMENT_NAME ?? "local",
      commit: process.env.RAILWAY_GIT_COMMIT_SHA ?? null,
      // Opt-in QA target marker; only a test-designated deployment sets it.
      deploymentKey: process.env.DEPLOYMENT_KEY ?? null,
    },
    { status: 200 },
  );
}
