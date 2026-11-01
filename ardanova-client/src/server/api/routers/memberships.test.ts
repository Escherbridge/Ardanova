import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  browserOverTheBff,
  dotNetAnswer,
  dotNetBaseUrl,
  fakeDotNetApi,
} from "~/server/api/dotnet-test-edge";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-ada", role: "INDIVIDUAL" },
  }),
}));

const browser = await browserOverTheBff();

afterEach(() => {
  vi.unstubAllGlobals();
});

afterAll(() => {
  vi.unstubAllEnvs();
});

const NOW = "2026-11-01T09:00:00Z";

/** .NET ProjectDto for Cy's barn, which Ada joined but did not create. */
const cysBarn = {
  id: "project-barn",
  title: "Barn raising",
  slug: "barn-raising",
  description: "A shared barn for the commons.",
  summary: null,
  problemStatement: "No covered space for winter work.",
  solution: "Raise a timber barn together.",
  categories: ["CONSTRUCTION"],
  hashtags: ["#construction"],
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
  projectType: "COMMUNITY",
  duration: null,
  createdAt: NOW,
  updatedAt: NOW,
  publishedAt: NOW,
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-cy",
  createdBy: { id: "user-cy", name: "Cy Carpenter", image: null },
  assignedGuildId: null,
};

/** .NET GuildDto for a guild Ada is a member of, not its owner. */
const framers = {
  id: "guild-framers",
  name: "Timber framers",
  slug: "timber-framers",
  description: "Joinery and frames.",
  website: null,
  email: "framers@example.org",
  phone: null,
  address: null,
  logo: null,
  portfolio: null,
  specialties: null,
  isVerified: false,
  rating: null,
  reviewsCount: 0,
  projectsCount: 1,
  createdAt: NOW,
  updatedAt: NOW,
  ownerId: "user-cy",
};

/**
 * The composer's attach lists (SR-03 P3 final join b). Which rows count as membership is .NET's rule, proven on real
 * Postgres (MembershipListFlowTests); this side proves the BFF asks as the signed actor and never names a user.
 */
describe("membership reads for the composer's attach picker", () => {
  it("project.getMyMemberships reads /api/projects/mine as the session user and keeps a project she joined", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, [cysBarn]));

    const projects = await browser.project.getMyMemberships.query();

    expect(projects.map((project) => [project.id, project.createdById])).toEqual(
      [["project-barn", "user-cy"]],
    );
    expect(requests).toEqual([
      {
        method: "GET",
        url: `${dotNetBaseUrl}/api/projects/mine`,
        body: undefined,
        signedActor: "member-ada",
      },
    ]);
  });

  it("guild.getMyMemberships reads /api/guilds/mine as the session user and keeps a guild she joined", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, [framers]));

    const guilds = await browser.guild.getMyMemberships.query();

    expect(guilds.map((guild) => [guild.id, guild.ownerId])).toEqual([
      ["guild-framers", "user-cy"],
    ]);
    expect(requests).toEqual([
      {
        method: "GET",
        url: `${dotNetBaseUrl}/api/guilds/mine`,
        body: undefined,
        signedActor: "member-ada",
      },
    ]);
  });
});
