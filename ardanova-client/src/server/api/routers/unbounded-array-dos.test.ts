import { createTRPCClient, httpLink, TRPCClientError } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import type { AppRouter } from "../root";

// Regression test for the unbounded-array DoS: see SECURITY-DEPENDENCIES.md "unbounded-array DoS"
// entry. Each router here had an array input with no `.max()`, so an attacker-sized array turned
// into one zod issue per element. These assert the fixed `.max()` holds at the boundary, and that
// a 100,000-element array is rejected as a single cheap issue, not parsed element-by-element.

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

// Importing the whole tree is the point: a schema that throws at definition time 500s every call.
const { appRouter } = await import("../root");
const { createTRPCContext } = await import("../trpc");

afterEach(() => {
  vi.unstubAllGlobals();
});
afterAll(() => {
  vi.unstubAllEnvs();
});

/** Byte length of each response the handler actually sent, in call order. */
const responseByteSizes: number[] = [];

const browser = createTRPCClient<AppRouter>({
  links: [
    httpLink({
      url: "http://localhost:3000/api/trpc",
      transformer: superjson,
      fetch: async (input, init) => {
        const response = await fetchRequestHandler({
          endpoint: "/api/trpc",
          req: new Request(input as RequestInfo, init as RequestInit),
          router: appRouter,
          createContext: ({ req }) =>
            createTRPCContext({ headers: req.headers }),
        });
        const bytes = new TextEncoder().encode(
          await response.clone().text(),
        ).length;
        responseByteSizes.push(bytes);
        return response;
      },
    }),
  ],
});

async function failureOf(call: Promise<unknown>) {
  const failure = await call.then(
    () => null,
    (error: unknown) => error,
  );
  expect(failure).toBeInstanceOf(TRPCClientError);
  return failure as TRPCClientError<AppRouter>;
}

/** Total zod issue messages actually echoed back across every field. */
function issueCount(failure: TRPCClientError<AppRouter>): number {
  const zodError = (
    failure.data as { zodError?: { fieldErrors?: Record<string, string[]> } }
  )?.zodError;
  const fieldErrors = zodError?.fieldErrors ?? {};
  return Object.values(fieldErrors).reduce(
    (total, messages) => total + (messages?.length ?? 0),
    0,
  );
}

// "A few KB": a genuine per-element blow-up is multiple MB (the finding measured ~179 MB).
const MAX_RESPONSE_BYTES = 5_000;
// Generous but discriminating: the finding measured ~3.5s of event-loop block on a 2 MB body alone.
const MAX_ELAPSED_MS = 2_000;

describe.each([
  {
    label: "governance.create options",
    limit: 20,
    call: (options: string[]) =>
      browser.governance.create.mutate({
        title: "A proposal",
        description: "x".repeat(60),
        type: "TREASURY",
        projectId: "project-1",
        options,
      }),
  },
  {
    label: "opportunity.create skills",
    limit: 20,
    call: (skills: string[]) =>
      browser.opportunity.create.mutate({
        title: "A role",
        description: "x".repeat(25),
        type: "FULL_TIME",
        skills,
        projectId: "project-1",
      }),
  },
  {
    label: "project.create categories",
    limit: 20,
    call: (categories: string[]) =>
      browser.project.create.mutate({
        title: "A project",
        description: "x".repeat(15),
        problemStatement: "x".repeat(15),
        solution: "x".repeat(15),
        categories,
      }),
  },
])("$label: unbounded-array DoS stays closed", ({ limit, call }) => {
  it(`rejects ${limit + 1} elements as BAD_REQUEST with a small, fast response`, async () => {
    const oversized = Array.from({ length: limit + 1 }, (_, i) => `v${i}`);

    const started = Date.now();
    const failure = await failureOf(call(oversized));
    const elapsed = Date.now() - started;

    expect(failure.data?.code).toBe("BAD_REQUEST");
    expect(responseByteSizes.at(-1)).toBeLessThan(MAX_RESPONSE_BYTES);
    expect(elapsed).toBeLessThan(MAX_ELAPSED_MS);
  });

  it("rejects 100,000 elements without producing one issue per element", async () => {
    const huge = Array.from({ length: 100_000 }, (_, i) => `v${i}`);

    const started = Date.now();
    const failure = await failureOf(call(huge));
    const elapsed = Date.now() - started;

    expect(failure.data?.code).toBe("BAD_REQUEST");
    // zod 4 stops at the array's own `.max()` check without visiting any element; a per-element
    // blow-up would put ~100,000 messages under this field instead.
    expect(issueCount(failure)).toBeLessThanOrEqual(10);
    expect(responseByteSizes.at(-1)).toBeLessThan(MAX_RESPONSE_BYTES);
    expect(elapsed).toBeLessThan(MAX_ELAPSED_MS);
  });
});
