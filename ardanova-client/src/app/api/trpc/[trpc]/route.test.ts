import { createTRPCClient, httpLink, TRPCClientError } from "@trpc/client";
import { NextRequest } from "next/server";
import superjson from "superjson";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import type { AppRouter } from "~/server/api/root";

// Regression test for the request-body half of the unbounded-array DoS fix: see
// SECURITY-DEPENDENCIES.md "unbounded-array DoS" entry. The route handler must reject an
// oversized body before tRPC/zod ever read it, whether the client is honest about
// Content-Length or not, while a normal request still reaches tRPC's own validation.

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-1", role: "INDIVIDUAL" },
  }),
}));

// The real `~/env` schema runs on import; these are the release gate's non-secret loopback fixtures
// (mirrors src/server/api/root.test.ts).
const releaseFixtures: Record<string, string> = {
  AUTH_SECRET: "release-check-auth-secret-is-not-for-runtime-use",
  AUTH_URL: "https://ardanova.example",
  GOOGLE_CLIENT_ID: "release-check-google-client",
  GOOGLE_CLIENT_SECRET: "release-check-google-secret",
  DATABASE_URL:
    "postgresql://release_check:release_check@127.0.0.1:5433/release_check",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "release-check-api-key-is-at-least-32-bytes",
  ADMIN_API_KEY: "release-check-admin-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY:
    "release-check-actor-assertion-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(releaseFixtures))
  vi.stubEnv(name, value);

const { GET, POST } = await import("./route");

afterEach(() => {
  vi.unstubAllGlobals();
});
afterAll(() => {
  vi.unstubAllEnvs();
});

const ENDPOINT = "http://localhost:3000/api/trpc/governance.create";
const ONE_MIB = 1024 * 1024;

describe("tRPC route handler: request body cap", () => {
  it("rejects a body whose honest Content-Length declares more than the cap", async () => {
    // The declared size alone is over the cap; the actual bytes sent are tiny, so this proves
    // the header check short-circuits before the body is ever read.
    const request = new NextRequest(ENDPOINT, {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "content-length": String(2 * ONE_MIB),
      },
      body: "{}",
    });

    const response = await POST(request);

    expect(response.status).toBe(413);
  });

  it("rejects a chunked body over the cap with no Content-Length, without buffering all of it", async () => {
    const totalBytes = 2 * ONE_MIB;
    const chunkSize = 64 * 1024;
    const payload = new TextEncoder().encode("a".repeat(totalBytes));
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        for (let offset = 0; offset < payload.length; offset += chunkSize) {
          controller.enqueue(payload.subarray(offset, offset + chunkSize));
        }
        controller.close();
      },
    });
    const request = new NextRequest(ENDPOINT, {
      method: "POST",
      headers: { "content-type": "application/json" }, // no content-length: chunked transfer
      body: stream,
      duplex: "half",
    } as ConstructorParameters<typeof NextRequest>[1]);

    expect(request.headers.get("content-length")).toBeNull();

    const response = await POST(request);

    expect(response.status).toBe(413);
  });

  it("passes a normal-sized request through to tRPC's own input validation", async () => {
    const browser = createTRPCClient<AppRouter>({
      links: [
        httpLink({
          url: "http://localhost:3000/api/trpc",
          transformer: superjson,
          fetch: (input, init) => {
            const request = new NextRequest(
              input as string,
              init as ConstructorParameters<typeof NextRequest>[1],
            );
            return request.method === "GET" ? GET(request) : POST(request);
          },
        }),
      ],
    });

    // A deliberately invalid payload (title too short) proves the request reached tRPC/zod
    // rather than being rejected at the body-cap edge: a 413 would surface as a network-shaped
    // failure, not a well-formed BAD_REQUEST carrying a field error.
    const failure = await browser.governance.create
      .mutate({
        title: "",
        description: "x".repeat(60),
        type: "TREASURY",
        projectId: "project-1",
        options: ["Yes", "No"],
      })
      .then(
        () => null,
        (error: unknown) => error,
      );

    expect(failure).toBeInstanceOf(TRPCClientError);
    expect((failure as TRPCClientError<AppRouter>).data?.code).toBe(
      "BAD_REQUEST",
    );
  });
});

describe("tRPC mutation origin guard", () => {
  it.each([
    { origin: "https://another.example" },
    { origin: "null" },
    { "sec-fetch-site": "cross-site" },
    { origin: "http://localhost:3000", "sec-fetch-site": "cross-site" },
  ])(
    "rejects cross-origin metadata before consuming the body: %j",
    async (headers: Record<string, string | undefined>) => {
      const requestHeaders = new Headers({
        "content-type": "application/json",
      });
      for (const [name, value] of Object.entries(headers))
        if (value) requestHeaders.set(name, value);
      const request = new NextRequest(ENDPOINT, {
        method: "POST",
        headers: requestHeaders,
        body: "{}",
      });
      const response = await POST(request);
      expect(response.status).toBe(403);
      expect(request.bodyUsed).toBe(false);
    },
  );
  it("lets a same-origin mutation reach input validation", async () => {
    const request = new NextRequest(ENDPOINT, {
      method: "POST",
      headers: {
        "content-type": "application/json",
        origin: "https://ardanova.example",
        "sec-fetch-site": "same-origin",
      },
      body: "{}",
    });
    expect((await POST(request)).status).not.toBe(403);
  });
});

describe("mutation origins behind a trusted reverse proxy", () => {
  it("uses configured AUTH_URL rather than the internal NextRequest host", async () => {
    const request = new NextRequest(
      "http://0.0.0.0:8080/api/trpc/governance.create",
      {
        method: "POST",
        headers: {
          "content-type": "application/json",
          origin: "https://ardanova.example",
          "sec-fetch-site": "same-origin",
        },
        body: "{}",
      },
    );
    expect((await POST(request)).status).not.toBe(403);
  });
  it.each([
    "https://evil.example",
    "https://ardanova.example/path",
    "https://user@ardanova.example",
    "null",
  ])(
    "rejects untrusted or malformed Origin %s before reading the body",
    async (origin) => {
      const request = new NextRequest(
        "http://0.0.0.0:8080/api/trpc/governance.create",
        {
          method: "POST",
          headers: {
            "content-type": "application/json",
            origin,
            "x-forwarded-host": "evil.example",
          },
          body: "{}",
        },
      );
      expect((await POST(request)).status).toBe(403);
      expect(request.bodyUsed).toBe(false);
    },
  );
});
