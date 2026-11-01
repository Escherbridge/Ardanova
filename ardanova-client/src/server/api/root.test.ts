import { createTRPCClient, httpLink, TRPCClientError } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import type { AppRouter } from "./root";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-1", role: "INDIVIDUAL" },
  }),
}));

// The real `~/env` schema runs on import; these are the release gate's non-secret loopback fixtures.
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
const { appRouter } = await import("./root");
const { createTRPCContext } = await import("./trpc");
const { describeMutationError, readInvalidFields } = await import(
  "~/components/ui/query-error-copy"
);
const { taskClaimRefusalOf } = await import("~/lib/tasks/claim-refusal");

const browser = createTRPCClient<AppRouter>({
  links: [
    httpLink({
      url: "http://localhost:3000/api/trpc",
      transformer: superjson,
      fetch: (input, init) =>
        fetchRequestHandler({
          endpoint: "/api/trpc",
          req: new Request(input as RequestInfo, init as RequestInit),
          router: appRouter,
          createContext: ({ req }) =>
            createTRPCContext({ headers: req.headers }),
        }),
    }),
  ],
});

afterEach(() => {
  vi.unstubAllGlobals();
});

afterAll(() => {
  vi.unstubAllEnvs();
});

describe("app router input validation", () => {
  it("refuses a malformed sponsorship key as BAD_REQUEST naming the field, before any BFF call", async () => {
    const backend = vi.fn();
    vi.stubGlobal("fetch", backend);

    const failure = await browser.fundingIntent.createCheckout
      .mutate({
        projectTokenConfigId: "config-1",
        amount: "25.00",
        disclosureVersion: "2026-10",
        idempotencyKey: "not-a-uuid",
      })
      .then(
        () => null,
        (error: unknown) => error,
      );

    expect(failure).toBeInstanceOf(TRPCClientError);
    expect((failure as TRPCClientError<AppRouter>).data?.code).toBe(
      "BAD_REQUEST",
    );
    expect(readInvalidFields(failure)).toEqual(["idempotencyKey"]);
    expect(describeMutationError(failure, "start your sponsorship")).toContain(
      "idempotency key",
    );
    expect(backend).not.toHaveBeenCalled();
  });
});

describe("task.claim refusals reach the browser with the API's code", () => {
  it.each([
    ["TASK_TAKEN", "TASK_TAKEN"],
    ["TASK_DONE", "TASK_DONE"],
    ["TASK_UNDER_AGREEMENT", "TASK_UNDER_AGREEMENT"],
    // A code a newer API added: still CONFLICT with the API's words, just no code to branch on.
    ["TASK_ARCHIVED", null],
  ] as const)(
    "a 409 naming %s is CONFLICT with taskClaimCode %s",
    async (code, expected) => {
      const backend = vi.fn(
        async () =>
          new Response(
            JSON.stringify({ error: "This task was just picked up.", code }),
            { status: 409, headers: { "Content-Type": "application/json" } },
          ),
      );
      vi.stubGlobal("fetch", backend);

      const failure = await browser.task.claim.mutate({ id: "task-1" }).then(
        () => null,
        (error: unknown) => error,
      );

      expect(failure).toBeInstanceOf(TRPCClientError);
      const data = (failure as TRPCClientError<AppRouter>).data;
      expect(data?.code).toBe("CONFLICT");
      expect(taskClaimRefusalOf(failure)).toBe(expected);
      expect(describeMutationError(failure, "pick up this task")).toContain(
        "This task was just picked up.",
      );
    },
  );
});
