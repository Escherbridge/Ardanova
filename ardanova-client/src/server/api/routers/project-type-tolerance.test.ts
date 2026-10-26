import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, projects } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  projects: { getById: vi.fn(), getBySlug: vi.fn(), search: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { projects } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { projectRouter } from "./project";

/** A .NET ProjectDto as the API serializes it. */
function projectFromApi(projectType: string) {
  return {
    id: "project-1",
    title: "Reopen the corner cafe as a co-op",
    slug: "corner-cafe-co-op",
    description: "Members buy, staff and run the cafe together.",
    problemStatement: "The block lost its only place to meet.",
    solution: "A member-owned cafe with a shared kitchen.",
    categories: ["HOSPITALITY", "FOOD_BEVERAGE"],
    status: "PUBLISHED",
    fundingGoal: null,
    currentFunding: 0,
    supportersCount: 0,
    votesCount: 0,
    viewsCount: 0,
    featured: false,
    tags: null,
    images: null,
    videos: null,
    documents: null,
    targetAudience: null,
    expectedImpact: null,
    timeline: null,
    projectType,
    duration: null,
    createdAt: "2026-10-26T12:00:00Z",
    updatedAt: "2026-10-26T12:00:00Z",
    publishedAt: "2026-10-26T12:00:00Z",
    fundedAt: null,
    completedAt: null,
    commerceEnabled: false,
    storefrontDescription: null,
    createdById: "user-1",
    createdBy: null,
    assignedGuildId: null,
  };
}

async function signedOutCaller() {
  currentSession.mockResolvedValue(null);
  return createCallerFactory(projectRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

/**
 * The API owns the ProjectType vocabulary and can add a value before this client is redeployed.
 * A project read must still succeed then; before SR-03 it failed the whole read as BAD_GATEWAY.
 */
describe("project reads tolerate project types this client does not know yet", () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ["a type added in this release", "VENUE"],
    ["a type added by a newer API", "MOBILE_KITCHEN"],
  ])("opens a project of %s", async (_case, projectType) => {
    projects.getById.mockResolvedValue({
      status: 200,
      data: projectFromApi(projectType),
    });

    const project = await (
      await signedOutCaller()
    ).getById({ id: "project-1" });

    expect(project.projectType).toBe(projectType);
    expect(project.slug).toBe("corner-cafe-co-op");
  });

  it("lists a page that mixes known and unknown types", async () => {
    projects.search.mockResolvedValue({
      status: 200,
      data: {
        items: [
          projectFromApi("COMMUNITY"),
          { ...projectFromApi("MOBILE_KITCHEN"), id: "project-2" },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    });

    const page = await (await signedOutCaller()).getAll({});

    expect(page.items.map((item) => item.projectType)).toEqual([
      "COMMUNITY",
      "MOBILE_KITCHEN",
    ]);
  });
});
