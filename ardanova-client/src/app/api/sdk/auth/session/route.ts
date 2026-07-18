import { NextResponse } from "next/server";

export async function POST() {
  return NextResponse.json(
    {
      error:
        "SDK session exchange is unavailable until single-use authorization codes are supported.",
    },
    {
      status: 501,
      headers: { "Cache-Control": "no-store" },
    },
  );
}
