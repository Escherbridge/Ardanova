import { NextResponse } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { getSessionOrError } = vi.hoisted(() => ({ getSessionOrError: vi.fn() }));
vi.mock("../_lib/session", () => ({ getSessionOrError }));
vi.mock("~/lib/api", async () => {
  const { ArdaNovaApiClient } = await vi.importActual<typeof import("~/lib/api/ardanova")>(
    "~/lib/api/ardanova",
  );
  return { apiClient: new ArdaNovaApiClient({ baseUrl: "http://api.test" }) };
});
vi.mock("~/env", () => ({ env: {} }));

import { GET } from "./route";

let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.stubEnv("ACTOR_ASSERTION_HMAC_KEY", "k".repeat(64));
  fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ id: "user-1", email: "me@example.test" }), { status: 200 }),
  );
  vi.stubGlobal("fetch", fetchMock);
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe("GET /api/sdk/me", () => {
  it("reads the caller's own record as the caller, so the backend keeps their private fields", async () => {
    getSessionOrError.mockResolvedValue({
      session: { user: { id: "user-1", role: "INDIVIDUAL" } },
      error: null,
    });

    const response = await GET();

    expect(response.status).toBe(200);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("http://api.test/api/users/user-1");
    const assertion = new Headers(init.headers).get("X-Ardanova-Actor");
    expect(assertion).toBeTruthy();
    const payload = JSON.parse(
      Buffer.from(assertion!.split(".")[0]!, "base64url").toString("utf8"),
    ) as { subject: string };
    expect(payload.subject).toBe("user-1");
  });

  it("refuses without a session before calling the API", async () => {
    getSessionOrError.mockResolvedValue({
      session: null,
      error: NextResponse.json({ error: "Unauthorized" }, { status: 401 }),
    });

    const response = await GET();

    expect(response.status).toBe(401);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
