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

import { ProjectEditForm } from "./project-edit-form";
import type { ProjectDto } from "~/lib/contracts/project-contract";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-1", role: "INDIVIDUAL" },
  }),
}));
const pushRoute = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ push: pushRoute }) }));

const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "edit-form-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "edit-form-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const { projectRouter } = await import("~/server/api/routers/project");
const { taxonomyRouter } = await import("~/server/api/routers/taxonomy");
const { enumRouter } = await import("~/server/api/routers/enum");
const router = createTRPCRouter({
  project: projectRouter,
  taxonomy: taxonomyRouter,
  enum: enumRouter,
});

/** The .NET API: the only faked collaborator. Everything from the form to this request is real. */
const dotNetApi =
  vi.fn<(url: string, init?: RequestInit) => Promise<Response>>();

function jsonResponse(status: number, body: unknown) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

/**
 * .NET ProjectDto for a project created before hashtags: the sector CONSTRUCTION plus a custom "Other" category
 * ("Community Garden") in `categories`, the free tag in `tags`, and `hashtags` derived by ProjectHashtagDisplay
 * (SECTOR + PROJECT_TYPE as "#slug"; the custom category fails Hashtags.Normalize, so it is not there).
 */
const legacyProject = {
  id: "project-1",
  title: "Reopen the corner cafe as a co-op",
  slug: "corner-cafe-co-op",
  description: "Members buy, staff and run the cafe together.",
  summary: null,
  problemStatement: "The block lost its only place to meet.",
  solution: "A member-owned cafe with a shared kitchen.",
  categories: ["CONSTRUCTION", "Community Garden"],
  hashtags: ["#construction", "#temporary"],
  status: "PUBLISHED",
  fundingGoal: null,
  fundingGoalUnit: "USD",
  currentFunding: 0,
  supportersCount: 0,
  votesCount: 0,
  viewsCount: 0,
  featured: false,
  tags: "barn-raising",
  images: null,
  videos: null,
  documents: null,
  mediaPaths: "[]",
  targetAudience: null,
  expectedImpact: null,
  timeline: null,
  projectType: "TEMPORARY",
  duration: null,
  createdAt: "2026-10-26T12:00:00Z",
  updatedAt: "2026-10-26T12:00:00Z",
  publishedAt: "2026-10-26T12:00:00Z",
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-1",
  createdBy: { id: "user-1", name: "Ada", image: null },
  assignedGuildId: null,
} satisfies ProjectDto;

/** .NET TaxonomyTermDto rows: slugs are the stored codes (uppercase), exactly as the taxonomy migration seeds them. */
const taxonomy = [
  ["CONSTRUCTION", "SECTOR", "Construction"],
  ["HOSPITALITY", "SECTOR", "Hospitality"],
  ["TEMPORARY", "PROJECT_TYPE", "Temporary"],
].map(([slug, kind, label], sortOrder) => ({
  slug,
  kind,
  label,
  description: null,
  parentSlug: null,
  sortOrder,
}));

/** Renders `ui` under a real tRPC client whose HTTP requests the real routers answer in-process. */
function renderSignedIn(ui: ReactNode) {
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

/** The body of the one PUT the form sent, read the way .NET reads it. */
async function savedBody() {
  await waitFor(() => expect(pushRoute).toHaveBeenCalled());
  const puts = dotNetApi.mock.calls.filter(
    ([url, init]) =>
      url.includes("/api/projects/project-1") && init?.method === "PUT",
  );
  expect(puts).toHaveLength(1);
  return JSON.parse(puts[0]![1]!.body as string) as Record<string, unknown>;
}

/** Every chip under the hashtag field, in order, as a reader sees it. */
const chipTexts = () =>
  Array.from(screen.getByText("#construction").parentElement!.children).map(
    (chip) => chip.textContent,
  );

beforeEach(() => {
  pushRoute.mockReset();
  vi.stubGlobal("fetch", dotNetApi);
  dotNetApi.mockImplementation((url, init) => {
    const method = init?.method ?? "GET";
    if (url.includes("/api/enums/ProjectDuration")) {
      return jsonResponse(200, ["ONE_TWO_WEEKS", "ONGOING"]);
    }
    if (url.includes("/api/taxonomy")) return jsonResponse(200, taxonomy);
    // The BFF's manager check reads the project first (Ada created it).
    if (url.endsWith("/api/projects/project-1") && method === "GET") {
      return jsonResponse(200, legacyProject);
    }
    if (url.includes("/api/projects/project-1") && method === "PUT") {
      return jsonResponse(200, legacyProject);
    }
    return jsonResponse(404, { error: "not found" });
  });
});
afterEach(() => {
  vi.unstubAllGlobals();
  dotNetApi.mockReset();
});
afterAll(() => vi.unstubAllEnvs());

describe("ProjectEditForm: the hashtag field shows everything a hashtags write replaces", () => {
  it("shows derived and free tags once each as #tag, and the custom category as a fixed chip", async () => {
    renderSignedIn(<ProjectEditForm project={legacyProject} />);

    await screen.findByText("#construction");
    expect(chipTexts()).toEqual([
      "#construction",
      "#temporary",
      "#barn-raising",
      "Community Garden",
    ]);
    expect(screen.queryByText(/##/)).toBeNull();
    // Editable chips carry a remove button; the custom category, which no hashtag can spell, does not.
    expect(
      screen.getByRole("button", { name: "Remove #barn-raising hashtag" }),
    ).not.toBeNull();
    expect(
      screen.queryByRole("button", { name: /Remove .*Community Garden/ }),
    ).toBeNull();
    expect(
      (
        screen.getByDisplayValue(
          "Reopen the corner cafe as a co-op",
        ) as HTMLInputElement
      ).maxLength,
    ).toBe(200);
  });

  it("editing only the title saves without hashtags, so the free tag and the custom category are untouched", async () => {
    renderSignedIn(<ProjectEditForm project={legacyProject} />);
    await screen.findByText("#barn-raising");

    fireEvent.change(screen.getByLabelText(/Project Title/), {
      target: { value: "Reopen the corner cafe" },
    });
    fireEvent.click(screen.getByRole("button", { name: /save changes/i }));

    const body = await savedBody();
    expect(body.title).toBe("Reopen the corner cafe");
    // Absent hashtags is the API's "leave categories, projectType and tags alone".
    expect(body).not.toHaveProperty("hashtags");
    expect(body).not.toHaveProperty("categories");
    expect(body).not.toHaveProperty("projectType");
    expect(body).not.toHaveProperty("tags");
  });

  it("an edited field sends the full canonical set, never a duplicate, and leaves the custom category to the API", async () => {
    renderSignedIn(<ProjectEditForm project={legacyProject} />);
    await screen.findByText("#temporary");

    fireEvent.click(
      screen.getByRole("button", { name: "Remove #temporary hashtag" }),
    );
    const input = screen.getByLabelText("Add hashtags");
    // Already there, typed the way the API stores it: not added twice.
    fireEvent.change(input, { target: { value: "#Construction" } });
    fireEvent.keyDown(input, { key: "Enter" });
    // A taxonomy suggestion is offered and added in the same canonical form.
    fireEvent.change(input, { target: { value: "hosp" } });
    fireEvent.click(
      await screen.findByRole("button", { name: /#hospitality/ }),
    );
    fireEvent.click(screen.getByRole("button", { name: /save changes/i }));

    const body = await savedBody();
    expect(body.hashtags).toEqual([
      "construction",
      "barn-raising",
      "hospitality",
    ]);
    expect(body).not.toHaveProperty("categories");
  });
});
