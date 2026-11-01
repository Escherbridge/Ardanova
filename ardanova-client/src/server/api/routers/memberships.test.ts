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

/** .NET ProjectMembershipDto for Cy's barn, which Ada joined but did not create. */
const cysBarn = {
  id: "project-barn",
  title: "Barn raising",
  slug: "barn-raising",
  summary: null,
  status: "PUBLISHED",
  categories: ["CONSTRUCTION"],
};

/** .NET GuildMembershipDto for a guild Ada is a member of, not its owner. */
const framers = {
  id: "guild-framers",
  name: "Timber framers",
  slug: "timber-framers",
};

/**
 * The composer's attach lists (SR-03 P3 final join b). Which rows count as membership is .NET's rule, proven on real
 * Postgres (MembershipListFlowTests); this side proves the BFF asks as the signed actor and never names a user.
 */
describe("membership reads for the composer's attach picker", () => {
  it("project.getMyMemberships reads /api/projects/mine as the session user and keeps a project she joined", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, [cysBarn]));

    const projects = await browser.project.getMyMemberships.query();

    // The slim row passes the contract intact; a drifted shape would be a BAD_GATEWAY, not a half-read list.
    expect(projects).toEqual([cysBarn]);
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

    expect(guilds).toEqual([framers]);
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
