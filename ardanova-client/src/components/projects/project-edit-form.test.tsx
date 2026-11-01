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

import {
  buildEditFormData,
  ProjectEditForm,
  type ProjectEditFormData,
} from "./project-edit-form";
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

function projectFromApi(overrides: Partial<ProjectDto> = {}): ProjectDto {
  return {
    id: "project-1",
    title: "Reopen the corner cafe as a co-op",
    slug: "corner-cafe-co-op",
    description: "Members buy, staff and run the cafe together.",
    problemStatement: "The block lost its only place to meet.",
    solution: "A member-owned cafe with a shared kitchen.",
    categories: [],
    hashtags: ["construction", "barn-raising"],
    status: "DRAFT",
    fundingGoal: null,
    fundingGoalUnit: "USD",
    currentFunding: 0,
    supportersCount: 0,
    votesCount: 0,
    viewsCount: 0,
    featured: false,
    tags: null,
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
    publishedAt: null,
    fundedAt: null,
    completedAt: null,
    commerceEnabled: false,
    storefrontDescription: null,
    createdById: "user-1",
    createdBy: null,
    assignedGuildId: null,
    ...overrides,
  } as ProjectDto;
}

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

beforeEach(() => {
  vi.stubGlobal("fetch", dotNetApi);
  dotNetApi.mockImplementation((url, init) => {
    const method = init?.method ?? "GET";
    if (url.includes("/api/enums/ProjectDuration")) {
      return jsonResponse(200, ["ONE_TWO_WEEKS", "ONGOING"]);
    }
    if (url.includes("/api/taxonomy")) {
      return jsonResponse(200, [
        {
          slug: "construction",
          kind: "SECTOR",
          label: "Construction",
          description: null,
          parentSlug: null,
          sortOrder: 0,
        },
      ]);
    }
    if (url.includes("/api/projects/project-1") && method === "GET") {
      return jsonResponse(200, projectFromApi());
    }
    if (url.includes("/api/projects/project-1") && method === "PUT") {
      return jsonResponse(200, projectFromApi({ hashtags: ["roofing"] }));
    }
    return jsonResponse(404, { error: "not found" });
  });
});
afterEach(() => {
  vi.unstubAllGlobals();
  dotNetApi.mockReset();
});
afterAll(() => vi.unstubAllEnvs());

describe("buildEditFormData (SR-03 Phase 3 Lane I prefill)", () => {
  it("prefills the hashtags field from ProjectDto.Hashtags", () => {
    const data: ProjectEditFormData = buildEditFormData(
      projectFromApi({ hashtags: ["carpentry", "mutual-aid"] }),
    );
    expect(data.hashtags).toEqual(["carpentry", "mutual-aid"]);
  });

  it("prefills an empty array for a project with no hashtags yet", () => {
    const data = buildEditFormData(projectFromApi({ hashtags: [] }));
    expect(data.hashtags).toEqual([]);
  });
});

describe("ProjectEditForm (SR-03 Phase 3 Lane I)", () => {
  it("renders the project's hashtags as chips and caps the title input at 200 chars", async () => {
    renderSignedIn(
      <ProjectEditForm
        project={projectFromApi({ hashtags: ["construction", "barn-raising"] })}
      />,
    );

    expect(await screen.findByText("#construction")).not.toBeNull();
    expect(screen.getByText("#barn-raising")).not.toBeNull();

    const titleInput = screen.getByDisplayValue(
      "Reopen the corner cafe as a co-op",
    );
    expect((titleInput as HTMLInputElement).maxLength).toBe(200);
  });

  it("submitting sends only hashtags, never categories, projectType or tags", async () => {
    renderSignedIn(
      <ProjectEditForm
        project={projectFromApi({ hashtags: ["construction"] })}
      />,
    );

    await screen.findByText("#construction");

    fireEvent.click(screen.getByRole("button", { name: /save changes/i }));

    await waitFor(() => {
      const putCall = dotNetApi.mock.calls.find(
        ([url, init]) =>
          url.includes("/api/projects/project-1") && init?.method === "PUT",
      );
      expect(putCall).toBeDefined();
    });

    const putCall = dotNetApi.mock.calls.find(
      ([url, init]) =>
        url.includes("/api/projects/project-1") && init?.method === "PUT",
    )!;
    const body = JSON.parse(putCall[1]!.body as string) as Record<
      string,
      unknown
    >;
    expect(body.hashtags).toEqual(["construction"]);
    expect(body.categories).toBeUndefined();
    expect(body.projectType).toBeUndefined();
    expect(body.tags).toBeUndefined();
  });
});
