import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import { httpLink } from "@trpc/client";
import type { AnyTRPCRouter } from "@trpc/server";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import { RouterContext } from "next/dist/shared/lib/router-context.shared-runtime";
import type { NextRouter } from "next/router";
import superjson from "superjson";

import { api } from "~/trpc/react";

/**
 * Test-only. The shell's flow tests run the real tRPC routers and API client in-process; the .NET HTTP edge
 * (global `fetch`) is the only fake, answering with the API's own JSON. See ./AGENTS.md §Tests.
 */

/** The loopback API the in-process routers call; `IN_PROCESS_SERVER_ENV` points them at it. */
export const DOTNET_API_ORIGIN = "http://127.0.0.1:5147";

/** Server variables the routers read on import. jsdom defines `window`, so t3-env validation is skipped. */
export const IN_PROCESS_SERVER_ENV: Readonly<Record<string, string>> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: DOTNET_API_ORIGIN,
  API_KEY: "shell-flow-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "shell-flow-test-actor-key-is-at-least-32-bytes",
};

export interface DotNetCall {
  method: string;
  path: string;
  search: string;
  body: unknown;
}

export interface DotNetAnswer {
  status?: number;
  body?: unknown;
}

/** Keyed by "METHOD /path" (no query string). An unmodelled route answers 503, as an unready API would. */
export type DotNetRoutes = Record<
  string,
  (call: DotNetCall) => DotNetAnswer | Promise<DotNetAnswer>
>;

/** A fake .NET API for `vi.stubGlobal("fetch", dotNet.fetch)`; `calls` records every request in order. */
export function fakeDotNetApi(routes: DotNetRoutes) {
  const calls: DotNetCall[] = [];
  const fetch = async (
    input: RequestInfo | URL,
    init: RequestInit = {},
  ): Promise<Response> => {
    const url = new URL(input instanceof Request ? input.url : String(input));
    const method = (init.method ?? "GET").toUpperCase();
    const body: unknown =
      typeof init.body === "string" && init.body !== ""
        ? JSON.parse(init.body)
        : undefined;
    const call = { method, path: url.pathname, search: url.search, body };
    calls.push(call);
    const route = routes[`${method} ${url.pathname}`];
    const answer: DotNetAnswer = route
      ? await route(call)
      : {
          status: 503,
          body: { error: `${method} ${url.pathname} unavailable` },
        };
    return new Response(
      answer.body === undefined ? null : JSON.stringify(answer.body),
      {
        status: answer.status ?? 200,
        headers: { "Content-Type": "application/json" },
      },
    );
  };
  return { fetch, calls };
}

/**
 * Renders `ui` under a real tRPC client whose every request `router` answers in-process. Link clicks reach a
 * recording router (the browser's navigation is the other process edge); `navigations` lists each href.
 */
export function renderOverRouter(
  ui: ReactNode,
  router: AnyTRPCRouter,
  createContext: (opts: { headers: Headers }) => Promise<object>,
) {
  const navigations: string[] = [];
  const recordNavigation = async (href: string) => {
    navigations.push(href);
    return true;
  };
  const browser = {
    push: recordNavigation,
    replace: recordNavigation,
    prefetch: async () => undefined,
  } as unknown as NextRouter;
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const client = api.createClient({
    links: [
      httpLink({
        url: "http://localhost:3000/api/trpc",
        transformer: superjson,
        fetch: (input, init) =>
          fetchRequestHandler({
            endpoint: "/api/trpc",
            // jsdom's AbortSignal is not Node's; the in-process request needs no abort.
            req: new Request(input as RequestInfo, {
              ...(init as RequestInit),
              signal: undefined,
            }),
            router,
            createContext: ({ req }) => createContext({ headers: req.headers }),
          }),
      }),
    ],
  });
  const view = render(
    <RouterContext.Provider value={browser}>
      <QueryClientProvider client={queryClient}>
        <api.Provider client={client} queryClient={queryClient}>
          {ui}
        </api.Provider>
      </QueryClientProvider>
    </RouterContext.Provider>,
  );
  return { ...view, navigations };
}
