import { NextResponse } from "next/server";

export const dynamic = "force-dynamic";

export function GET() {
  return NextResponse.json(
    {
      status: "alive",
      timestamp: new Date().toISOString(),
      service: "ardanova-client",
      uptimeSeconds: Math.floor(process.uptime()),
    },
    { status: 200 },
  );
}
