import { describe, expect, it, vi } from "vitest";

import {
  checkBackendReadiness,
  validateReadinessConfiguration,
  type ReadinessEnvironment,
} from "./readiness";

const validEnvironment: ReadinessEnvironment = {
  AUTH_SECRET: "auth-secret-012345678901234567890123456789",
  AUTH_URL: "https://app.example.test",
  GOOGLE_CLIENT_ID: "google-client",
  GOOGLE_CLIENT_SECRET: "google-secret",
  DATABASE_URL: "postgresql://user:password@db.example.test:5432/ardanova",
  API_URL: "https://api.example.test",
  API_KEY: "service-key-012345678901234567890123456789",
  ADMIN_API_KEY: "admin-key-012345678901234567890123456789",
  ACTOR_ASSERTION_HMAC_KEY: "actor-key-012345678901234567890123456789",
};

describe("readiness configuration", () => {
  it("accepts the complete production runtime contract", () => {
    expect(validateReadinessConfiguration(validEnvironment)).toEqual({
      ready: true,
      invalid: [],
    });
  });

  it("names a missing actor assertion key without exposing values", () => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        ACTOR_ASSERTION_HMAC_KEY: "short",
      }),
    ).toEqual({
      ready: false,
      invalid: ["ACTOR_ASSERTION_HMAC_KEY"],
    });
  });

  it("rejects a missing or weak administrative API key", () => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        ADMIN_API_KEY: "short",
      }),
    ).toEqual({
      ready: false,
      invalid: ["ADMIN_API_KEY"],
    });
  });

  it("rejects a weak primary service API key", () => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        API_KEY: "short",
      }),
    ).toEqual({
      ready: false,
      invalid: ["API_KEY"],
    });
  });

  it.each([
    ["AUTH_SECRET", "your-auth-secret-min-32-chars-here"],
    ["API_KEY", "replace-with-a-random-32-byte-minimum-service-secret"],
    ["ADMIN_API_KEY", "replace-with-a-distinct-random-32-byte-minimum-secret"],
    [
      "ACTOR_ASSERTION_HMAC_KEY",
      "placeholder-secret-012345678901234567890123456789",
    ],
  ] as const)("rejects a documented or sentinel %s", (name, value) => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        [name]: value,
      }),
    ).toEqual({ ready: false, invalid: [name] });
  });

  it("rejects local auth and TLS escape hatches in a hosted runtime", () => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        AUTH_URL: "http://app.example.test",
        RAILWAY_ENVIRONMENT: "production",
        DEV_AUTH_BYPASS: "false",
        NODE_TLS_REJECT_UNAUTHORIZED: "0",
      }),
    ).toEqual({
      ready: false,
      invalid: ["AUTH_URL", "DEV_AUTH_BYPASS", "NODE_TLS_REJECT_UNAUTHORIZED"],
    });
  });

  it("permits explicit local development URLs outside hosted runtimes", () => {
    expect(
      validateReadinessConfiguration({
        ...validEnvironment,
        AUTH_URL: "http://localhost:3000",
        API_URL: "http://localhost:5147",
        DEV_AUTH_BYPASS: "false",
        NODE_TLS_REJECT_UNAUTHORIZED: "0",
      }),
    ).toEqual({ ready: true, invalid: [] });
  });
});

describe("backend readiness", () => {
  it("checks the backend readiness path", async () => {
    const fetchImpl = vi.fn(async () => new Response(null, { status: 200 }));

    await expect(
      checkBackendReadiness("https://api.example.test/base", { fetchImpl }),
    ).resolves.toEqual({ ready: true, status: "ready", httpStatus: 200 });
    expect(fetchImpl).toHaveBeenCalledWith(
      new URL("https://api.example.test/ready"),
      expect.objectContaining({ method: "GET", cache: "no-store" }),
    );
  });

  it("fails readiness on a non-success backend response", async () => {
    const fetchImpl = vi.fn(async () => new Response(null, { status: 503 }));

    await expect(
      checkBackendReadiness("https://api.example.test", { fetchImpl }),
    ).resolves.toEqual({
      ready: false,
      status: "unreachable",
      httpStatus: 503,
    });
  });
});
