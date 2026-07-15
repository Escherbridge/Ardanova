import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import { getActorAssertion, runWithActorAssertion } from "./actor-assertion";

const previousSigningKey = process.env.ACTOR_ASSERTION_HMAC_KEY;

afterEach(() => {
  if (previousSigningKey === undefined) delete process.env.ACTOR_ASSERTION_HMAC_KEY;
  else process.env.ACTOR_ASSERTION_HMAC_KEY = previousSigningKey;
});

describe("actor assertion v2", () => {
  it("binds the authenticated actor to exact query, JSON bytes, normalized type, and idempotency key", () => {
    process.env.ACTOR_ASSERTION_HMAC_KEY = "actor-key-012345678901234567890123456789";
    const body = new TextEncoder().encode('{"amount":"1.00"}');

    const assertion = runWithActorAssertion(
      { subject: "user-123", role: "INDIVIDUAL" },
      () => getActorAssertion({
        method: "post",
        url: "/api/Swaps?project=project-1&asset=ARDA",
        body,
        contentType: "Application/Json; Charset=UTF-8",
        idempotencyKey: "payment-1",
      }),
    );

    expect(assertion).toBeDefined();
    const [payload] = assertion!.split(".");
    expect(JSON.parse(Buffer.from(payload!, "base64url").toString("utf8"))).toMatchObject({
      version: 2,
      subject: "user-123",
      role: "INDIVIDUAL",
      method: "POST",
      requestTarget: "/api/Swaps?project=project-1&asset=ARDA",
      contentType: "application/json;charset=utf-8",
      bodySha256: "ecd4beb07d489cdc999ba7c40e5d86d2e433e3c5248ab08cf368a85c93fb8040",
      idempotencyKey: "payment-1",
    });
    expect(JSON.parse(Buffer.from(payload!, "base64url").toString("utf8")).jti)
      .toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
  });

  it("rejects an active BFF scope without at least 32 bytes of signing key material", () => {
    process.env.ACTOR_ASSERTION_HMAC_KEY = "too-short";

    expect(() => runWithActorAssertion(
      { subject: "user-123" },
      () => getActorAssertion({ method: "GET", url: "https://api.example.test/api/Swaps" }),
    )).toThrow("ACTOR_ASSERTION_HMAC_KEY must contain at least 32 bytes.");
  });

  it("does not manufacture an assertion outside a protected BFF scope", () => {
    process.env.ACTOR_ASSERTION_HMAC_KEY = "actor-key-012345678901234567890123456789";

    expect(getActorAssertion({ method: "GET", url: "https://api.example.test/api/Swaps" })).toBeUndefined();
  });
});
