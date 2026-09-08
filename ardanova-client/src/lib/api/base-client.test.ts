import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
vi.mock("~/server/actor-assertion", () => ({
  getActorAssertion: () => null,
  isActorAssertionActive: () => false,
}));

import { BaseApiClient } from "./base-client";

describe("BaseApiClient error messages", () => {
  it("uses the safe error field from a JSON action response", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ error: "Complete your account setup before funding a project." }),
      { status: 403, headers: { "Content-Type": "application/json" } },
    )));

    const response = await new BaseApiClient({ baseUrl: "https://api.example.test" })
      .post("/funding-intents/checkout", {});

    expect(response).toMatchObject({
      status: 403,
      error: "Complete your account setup before funding a project.",
    });
  });

  it("uses Problem Details detail before falling back to raw JSON", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ title: "Unavailable", detail: "Settlement is not available yet." }),
      { status: 503, headers: { "Content-Type": "application/problem+json" } },
    )));

    const response = await new BaseApiClient({ baseUrl: "https://api.example.test" })
      .post("/swaps", {});

    expect(response).toMatchObject({
      status: 503,
      error: "Settlement is not available yet.",
    });
  });
});
