import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, projects } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  projects: {
    create: vi.fn(),
    update: vi.fn(),
    getById: vi.fn(),
    getMembers: vi.fn(),
  },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { projects } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { projectRouter } from "./project";

/** A .NET ProjectDto as the API serializes it, with the SR-03 Phase 3 Hashtags field. */
function projectFromApi(overrides: Record<string, unknown> = {}) {
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
  };
}

async function signedInCaller(userId = "user-1") {
  currentSession.mockResolvedValue({
    expires: "2099-01-01T00:00:00Z",
    user: { id: userId, role: "INDIVIDUAL" },
  });
  return createCallerFactory(projectRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("project hashtags (SR-03 Phase 3 Lane I)", () => {
  beforeEach(() => vi.clearAllMocks());

  it("create sends hashtags and no categories for a tags-only project", async () => {
    projects.create.mockResolvedValue({ status: 201, data: projectFromApi() });

    const caller = await signedInCaller();
    await caller.create({
      title: "Reopen the corner cafe as a co-op",
      description: "Members buy, staff and run the cafe together.",
      problemStatement: "The block lost its only place to meet.",
      solution: "A member-owned cafe with a shared kitchen.",
      hashtags: ["construction", "barn-raising"],
    });

    expect(projects.create).toHaveBeenCalledTimes(1);
    const sent = projects.create.mock.calls[0][0] as Record<string, unknown>;
    expect(sent.hashtags).toEqual(["construction", "barn-raising"]);
    expect(sent.categories).toBeUndefined();
    expect(sent.projectType).toBeUndefined();
  });

  it("update replaces tags: a hashtags-only edit reaches the API with no categories", async () => {
    projects.getById.mockResolvedValue({
      status: 200,
      data: projectFromApi(),
    });
    projects.update.mockResolvedValue({
      status: 200,
      data: projectFromApi({ hashtags: ["roofing"] }),
    });

    const caller = await signedInCaller("user-1");
    await caller.update({
      id: "project-1",
      hashtags: ["roofing"],
    });

    expect(projects.update).toHaveBeenCalledTimes(1);
    const [, sent] = projects.update.mock.calls[0] as [
      string,
      Record<string, unknown>,
    ];
    expect(sent.hashtags).toEqual(["roofing"]);
    expect(sent.categories).toBeUndefined();
  });

  it("rejects more than 10 hashtags before any API call (DoS-fix style bound)", async () => {
    const caller = await signedInCaller();
    const tooMany = Array.from({ length: 11 }, (_, i) => `tag-${i}`);

    await expect(
      caller.create({
        title: "Title",
        description: "Members buy, staff and run the cafe together.",
        problemStatement: "The block lost its only place to meet.",
        solution: "A member-owned cafe with a shared kitchen.",
        hashtags: tooMany,
      }),
    ).rejects.toThrow();
    expect(projects.create).not.toHaveBeenCalled();
  });
});
