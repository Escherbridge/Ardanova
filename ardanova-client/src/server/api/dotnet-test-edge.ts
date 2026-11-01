import { createTRPCClient, httpLink, TRPCClientError } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import { vi } from "vitest";

import type { AppRouter } from "./root";

/**
 * Test-only: the BFF exactly as a browser reaches it. The real app router answers over in-process HTTP and the
 * real API client builds every .NET request; only the .NET API (global `fetch`) is fake. A test file still
 * declares its own `vi.mock("server-only")` and `vi.mock("~/server/auth")`, because vitest scopes mocks per file.
 */

/** The release gate's non-secret loopback fixtures: the real `~/env` schema validates them on import. */
const serverEnv = {
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

/** Where the API client sends .NET requests under these fixtures. */
export const dotNetBaseUrl = serverEnv.API_URL;

/** Stubs the server env, then imports the whole app router (a schema that throws at definition time 500s every call). */
export async function browserOverTheBff() {
  for (const [name, value] of Object.entries(serverEnv))
    vi.stubEnv(name, value);
  const { appRouter } = await import("./root");
  const { createTRPCContext } = await import("./trpc");

  return createTRPCClient<AppRouter>({
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
}

/** One request the BFF sent to .NET, read the way .NET reads it. */
export interface DotNetRequest {
  method: string;
  url: string;
  body: unknown;
  /** The `subject` of the signed `X-Ardanova-Actor` assertion: the only identity .NET trusts. */
  signedActor: string | undefined;
}

/** Replaces the .NET API with `answer` and records every request the BFF sends it. */
export function fakeDotNetApi(
  answer: (request: DotNetRequest) => Response,
): DotNetRequest[] {
  const requests: DotNetRequest[] = [];
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const request = readDotNetRequest(input, init);
      requests.push(request);
      return answer(request);
    }),
  );
  return requests;
}

/** A .NET answer: JSON when there is a body, an empty response (204 and friends) otherwise. */
export function dotNetAnswer(status: number, body?: unknown): Response {
  return body === undefined
    ? new Response(null, { status })
    : new Response(JSON.stringify(body), {
        status,
        headers: { "Content-Type": "application/json" },
      });
}

/** The error a browser call ended in; fails the test when the call succeeded or broke some other way. */
export async function refusalOf(
  call: Promise<unknown>,
): Promise<TRPCClientError<AppRouter>> {
  const outcome = await call.then(
    () => undefined,
    (error: unknown) => error,
  );
  if (!(outcome instanceof TRPCClientError))
    throw new Error(
      "Expected the BFF to refuse this call with a tRPC error, but it did not.",
    );
  return outcome as TRPCClientError<AppRouter>;
}

function readDotNetRequest(
  input: RequestInfo | URL,
  init: RequestInit | undefined,
): DotNetRequest {
  const rawBody = typeof init?.body === "string" ? init.body : "";
  return {
    method: init?.method ?? "GET",
    url: input instanceof Request ? input.url : String(input),
    body: rawBody ? (JSON.parse(rawBody) as unknown) : undefined,
    signedActor: signedSubjectOf(
      new Headers(init?.headers).get("X-Ardanova-Actor"),
    ),
  };
}

function signedSubjectOf(assertion: string | null): string | undefined {
  const payload = assertion?.split(".")[0];
  if (!payload) return undefined;
  const claims = JSON.parse(
    Buffer.from(payload, "base64url").toString("utf8"),
  ) as { subject?: string };
  return claims.subject;
}
