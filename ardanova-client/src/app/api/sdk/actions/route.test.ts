import { NextResponse } from "next/server";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { getSessionOrError } = vi.hoisted(() => ({
  getSessionOrError: vi.fn(),
}));

vi.mock("../_lib/session", () => ({ getSessionOrError }));

import { POST } from "./route";

describe("SDK action reporting", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("preserves the authenticated SDK boundary", async () => {
    getSessionOrError.mockResolvedValue({
      session: null,
      error: NextResponse.json({ error: "Unauthorized" }, { status: 401 }),
    });

    const response = await POST();

    expect(response.status).toBe(401);
  });

  it("fails closed instead of awarding XP from client-controlled actions", async () => {
    getSessionOrError.mockResolvedValue({
      session: { user: { id: "user-123" } },
      error: null,
    });

    const response = await POST();
    const body = (await response.json()) as Record<string, unknown>;

    expect(response.status).toBe(501);
    expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(body).toMatchObject({
      awarded: false,
      tokensEarned: 0,
      newBalance: 0,
    });
    expect(body.message).toContain("verified by the platform");
  });
});
