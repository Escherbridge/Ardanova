// @vitest-environment jsdom
import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { httpLink } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import {
  afterAll,
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";

import { api } from "~/trpc/react";

import OpportunitiesPage from "./page";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({ auth: async () => null }));

const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "opportunities-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "opportunities-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const { opportunityRouter } = await import("~/server/api/routers/opportunity");
const router = createTRPCRouter({ opportunity: opportunityRouter });

/** The .NET API: the only faked collaborator. The page's own filter logic is all real. */
const dotNetApi =
  vi.fn<(url: string, init?: RequestInit) => Promise<Response>>();

function opportunityFromApi(
  overrides: Record<string, unknown> = {},
): Record<string, unknown> {
  return {
    id: `opp-${Math.random()}`,
    posterId: "user-1",
    title: "Frame the east wall",
    description: "Two hands for a Saturday build.",
    type: "TASK_BOUNTY",
    skills: null,
    compensation: null,
    compensationDetails: null,
    isOpenForApplications: true,
    location: null,
    isRemote: true,
    deadline: null,
    status: "OPEN",
    slug: "frame-the-east-wall",
    origin: "TASK_GENERATED",
    applicationsCount: 0,
    poster: { id: "user-1", name: "Bea" },
    createdAt: "2026-10-26T12:00:00Z",
    updatedAt: "2026-10-26T12:00:00Z",
    ...overrides,
  };
}

function renderSignedOut(ui: ReactNode) {
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
  return render(
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        {ui}
      </api.Provider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.stubGlobal("fetch", dotNetApi);
  dotNetApi.mockImplementation((url) => {
    if (url.includes("/api/opportunities/search")) {
      return Promise.resolve(
        new Response(
          JSON.stringify({
            items: [
              opportunityFromApi({ id: "opp-bounty", type: "TASK_BOUNTY" }),
              opportunityFromApi({
                id: "opp-freelance",
                type: "FREELANCE",
                origin: "TEAM_POSITION",
                title: "Design the newsletter template",
                slug: "design-newsletter",
              }),
            ],
            page: 1,
            pageSize: 100,
            totalCount: 2,
            totalPages: 1,
            hasNextPage: false,
            hasPreviousPage: false,
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      );
    }
    return Promise.resolve(
      new Response(JSON.stringify({ error: "not found" }), { status: 404 }),
    );
  });
});
afterEach(() => {
  vi.unstubAllGlobals();
  dotNetApi.mockReset();
});
afterAll(() => vi.unstubAllEnvs());

// Both cards' h3 title doubles as the sidebar's "Most active" list entry (a <p>), so
// queries scope to the heading role to stay unambiguous.
const bountyCardHeading = () =>
  screen.findByRole("heading", { name: "Frame the east wall" });
const freelanceCardHeading = () =>
  screen.queryByRole("heading", { name: "Design the newsletter template" });

describe("Opportunities page type filter and bounty tab (P8-16)", () => {
  it("the Job Type filter's selectable values equal OpportunityType enum members, not the old labels", async () => {
    renderSignedOut(<OpportunitiesPage />);
    await bountyCardHeading();

    fireEvent.click(screen.getByRole("button", { name: /filters/i }));
    const typeSelect = screen.getByLabelText(/job type/i);
    const values = Array.from((typeSelect as HTMLSelectElement).options).map(
      (o) => o.value,
    );

    expect(values).toContain("TASK_BOUNTY");
    expect(values).toContain("FREELANCE");
    // The pre-fix dropdown offered these display strings, which never equalled
    // an OpportunityType enum member, so selecting them always filtered to zero.
    expect(values).not.toContain("Bounty");
    expect(values).not.toContain("Contract");
    expect(values).not.toContain("Part-time");
  });

  it("selecting Task Bounty in the type filter narrows the list to TASK_BOUNTY items", async () => {
    renderSignedOut(<OpportunitiesPage />);
    await bountyCardHeading();
    expect(freelanceCardHeading()).not.toBeNull();

    fireEvent.click(screen.getByRole("button", { name: /filters/i }));
    const typeSelect = screen.getByLabelText(/job type/i);
    fireEvent.change(typeSelect, { target: { value: "TASK_BOUNTY" } });

    await waitFor(() => {
      expect(freelanceCardHeading()).toBeNull();
    });
    expect(await bountyCardHeading()).not.toBeNull();
  });

  it("the Bounties tab shows only TASK_BOUNTY items", async () => {
    renderSignedOut(<OpportunitiesPage />);
    await bountyCardHeading();

    fireEvent.click(screen.getByRole("tab", { name: "Bounties" }));

    await waitFor(() => {
      expect(freelanceCardHeading()).toBeNull();
    });
    expect(await bountyCardHeading()).not.toBeNull();
  });
});
