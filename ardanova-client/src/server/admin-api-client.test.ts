import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
vi.mock("~/env", () => ({
  env: {
    API_URL: "https://api.example.test",
    API_KEY: "test-service-key",
    ADMIN_API_KEY: "test-admin-key",
  },
}));

import { createAdminApiClient } from "./admin-api-client";

describe("admin API client", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("sends the service and distinct administrative credentials only from the server client", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response("[]", { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const client = createAdminApiClient("https://api.example.test", "service-key", "admin-key");
    await client.payouts.getPendingPayouts();

    const [, options] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = new Headers(options.headers);
    expect(headers.get("X-Api-Key")).toBe("service-key");
    expect(headers.get("X-Admin-Api-Key")).toBe("admin-key");
  });

  it("fails closed when no separate administrative credential is configured", () => {
    expect(() => createAdminApiClient("https://api.example.test", "service-key", "")).toThrow(
      "ADMIN_API_KEY is required",
    );
  });
});
