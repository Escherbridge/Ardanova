// @vitest-environment jsdom
import { act } from "react";
import { hydrateRoot, type Root } from "react-dom/client";
import { renderToString } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { httpLink } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  IN_PROCESS_SERVER_ENV,
  fakeDotNetApi,
} from "~/components/notifications/dotnet-test-edge";
import { api } from "~/trpc/react";

import HomePage from "./page";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-bea", role: "INDIVIDUAL" },
  }),
}));
// AuthenticatedLayout hands the server's session to SessionProvider, so Home's composer is in the server HTML.
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: {
      expires: "2099-01-01T00:00:00Z",
      user: { id: "user-bea", name: "Bea Builder", image: null },
    },
  }),
}));

for (const [name, value] of Object.entries(IN_PROCESS_SERVER_ENV)) {
  vi.stubEnv(name, value);
}
const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const router = createTRPCRouter({
  feed: (await import("~/server/api/routers/feed")).feedRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  guild: (await import("~/server/api/routers/guild")).guildRouter,
  task: (await import("~/server/api/routers/task")).taskRouter,
  post: (await import("~/server/api/routers/post")).postRouter,
  azoaCustodialAccount: (
    await import("~/server/api/routers/azoa-custodial-account")
  ).azoaCustodialAccountRouter,
});

/** Home under its providers, with a fresh QueryClient per render, as the server and the browser each have. */
function homeTree() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const client = api.createClient({
    links: [
      httpLink({
        url: "http://localhost:3000/api/trpc",
        transformer: superjson,
        fetch: (input, init) =>
          fetchRequestHandler({
            endpoint: "/api/trpc",
            req: new Request(input as RequestInfo, {
              ...(init as RequestInit),
              signal: undefined,
            }),
            router,
            createContext: ({ req }) =>
              createTRPCContext({ headers: req.headers }),
          }),
      }),
    ],
  });
  return (
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        <HomePage />
      </api.Provider>
    </QueryClientProvider>
  );
}

let root: Root | null = null;
afterEach(() => {
  act(() => root?.unmount());
  root = null;
  document.body.innerHTML = "";
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});
afterAll(() => vi.unstubAllEnvs());

describe("Home hydrates without a mismatch (walkthrough W-L3)", () => {
  it("the server's HTML for the composer is exactly the browser's first render, whatever the browser has stored", async () => {
    vi.stubGlobal("IntersectionObserver", undefined);
    vi.stubGlobal(
      "fetch",
      fakeDotNetApi({
        "GET /api/projects/mine": () => ({
          body: [
            {
              id: "project-barn",
              title: "Barn raising",
              slug: "barn-raising",
              summary: null,
              status: "PUBLISHED",
              categories: ["CONSTRUCTION"],
            },
          ],
        }),
        "GET /api/guilds/mine": () => ({ body: [] }),
        "GET /api/tasks/me": () => ({ body: [] }),
        "GET /api/feed": () => ({ body: { items: [], nextCursor: null } }),
      }).fetch,
    );
    // The server has no storage; the browser has something saved under every key (a draft, a dismissal, ...).
    const storedValue = vi
      .spyOn(Storage.prototype, "getItem")
      .mockReturnValue(null);
    const serverHtml = renderToString(homeTree());
    expect(serverHtml).toContain('data-prompt="moved"');
    storedValue.mockReturnValue('{"saved":true}');

    const container = document.createElement("div");
    container.innerHTML = serverHtml;
    document.body.appendChild(container);
    const recovered: unknown[] = [];
    const logged: string[] = [];
    vi.spyOn(console, "error").mockImplementation((...args: unknown[]) => {
      logged.push(args.map(String).join(" "));
    });

    await act(async () => {
      root = hydrateRoot(container, homeTree(), {
        onRecoverableError: (error) => recovered.push(error),
      });
    });

    expect(recovered).toEqual([]);
    expect(logged.filter((line) => /hydrat/i.test(line))).toEqual([]);
    // Hydration kept the server's composer rather than replacing it.
    expect(container.querySelector('form[data-prompt="moved"]')).not.toBeNull();
  });
});
