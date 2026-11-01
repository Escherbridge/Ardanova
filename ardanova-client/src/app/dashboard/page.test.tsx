// @vitest-environment jsdom
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import {
  afterAll,
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";

import {
  IN_PROCESS_SERVER_ENV,
  fakeDotNetApi,
  renderOverRouter,
  type DotNetCall,
  type DotNetRoutes,
} from "~/components/notifications/dotnet-test-edge";
import { feedCardFixture } from "~/lib/feed/card-fixture";
import type { FeedKitCard } from "~/lib/feed/types";

import HomePage from "./page";

vi.mock("server-only", () => ({}));
// Ada: signed in, follows nothing. The session is the auth edge; everything past it is real.
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-ada", role: "INDIVIDUAL" },
  }),
}));
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    status: "authenticated",
    data: {
      expires: "2099-01-01T00:00:00Z",
      user: { id: "user-ada", name: "Ada Lovelace", image: null },
    },
  }),
}));

for (const [name, value] of Object.entries(IN_PROCESS_SERVER_ENV)) {
  vi.stubEnv(name, value);
}
const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const routers = {
  feed: (await import("~/server/api/routers/feed")).feedRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  guild: (await import("~/server/api/routers/guild")).guildRouter,
  task: (await import("~/server/api/routers/task")).taskRouter,
  post: (await import("~/server/api/routers/post")).postRouter,
  azoaCustodialAccount: (
    await import("~/server/api/routers/azoa-custodial-account")
  ).azoaCustodialAccountRouter,
};
const router = createTRPCRouter(routers);

const barnRaising = {
  id: "project-barn",
  title: "Barn raising",
  slug: "barn-raising",
  summary: "A barn for the commons.",
  sectors: ["CONSTRUCTION"],
};
const seedLibrary = {
  id: "project-seeds",
  title: "Seed library",
  slug: "seed-library",
  summary: null,
  sectors: ["FOOD_BEVERAGE"],
};

/** Cy's barn, which Ada joined, as .NET `GET /api/projects/mine` lists it (ProjectDto). */
const adaProjectDto = {
  id: "project-barn",
  title: "Barn raising",
  slug: "barn-raising",
  description: "A barn for the commons.",
  problemStatement: "The commons has no dry storage.",
  solution: "Raise a timber barn together.",
  categories: ["CONSTRUCTION"],
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
  duration: "ONGOING",
  createdAt: "2026-10-01T12:00:00Z",
  updatedAt: "2026-10-01T12:00:00Z",
  publishedAt: "2026-10-01T12:00:00Z",
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-cy",
  createdBy: { id: "user-cy", name: "Cy Carpenter", image: null },
  assignedGuildId: null,
};

/** A guild Ada is a member of, as .NET `GET /api/guilds/mine` lists it (GuildDto). */
const framersGuildDto = {
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
  projectsCount: 0,
  createdAt: "2026-10-01T12:00:00Z",
  updatedAt: "2026-10-01T12:00:00Z",
  ownerId: "user-cy",
};

/** .NET FeedCardDto JSON for a card Ada sees through discovery: not a member, not following. */
const discovered = (overrides: Partial<FeedKitCard>): FeedKitCard =>
  feedCardFixture({
    isDiscovery: true,
    reasons: [
      { kind: "Discovery", text: "Discovery", about: null, aboutId: null },
    ],
    ...overrides,
  });

const framing = discovered({
  id: "post-framing",
  body: "Framing the east wall Saturday, need two hands",
  project: barnRaising,
});
const seeds = discovered({
  id: "post-seeds",
  author: { id: "user-cy", name: "Cy Carpenter", image: null },
  body: "Saving seeds this spring",
  project: seedLibrary,
});
const roofVote = discovered({
  id: "post-roof-vote",
  postType: "PROPOSAL",
  body: "Proposal opened: roof material",
  bodyIsUserWritten: false,
  project: barnRaising,
});

/** The .NET `GET /api/feed`: pages by the opaque cursor it handed out. */
function feedPages(
  pages: Record<string, { items: FeedKitCard[]; nextCursor: string | null }>,
) {
  return (call: DotNetCall) => ({
    body: pages[new URLSearchParams(call.search).get("cursor") ?? ""],
  });
}

/** Every route Home reads for a person on no team and with no tasks; custody status is left unmodelled (503). */
function zeroFollowApi(overrides: DotNetRoutes): DotNetRoutes {
  return {
    "GET /api/projects/mine": () => ({ body: [] }),
    "GET /api/guilds/mine": () => ({ body: [] }),
    "GET /api/tasks/me": () => ({ body: [] }),
    ...overrides,
  };
}

const renderedBodies = () =>
  screen
    .getAllByRole("article")
    .map(
      (article) =>
        document.getElementById(article.getAttribute("aria-describedby") ?? "")
          ?.textContent,
    );

beforeEach(() => vi.stubGlobal("IntersectionObserver", undefined));
afterEach(() => vi.unstubAllGlobals());
afterAll(() => vi.unstubAllEnvs());

describe("Home is the feed (SR-03 plan §1)", () => {
  it("a zero-follow persona reads to the end, follows a project from its card, and the sector chip leads to its tag", async () => {
    const dotNet = fakeDotNetApi(
      zeroFollowApi({
        "GET /api/feed": feedPages({
          "": { items: [framing, seeds], nextCursor: "after-seeds" },
          "after-seeds": { items: [roofVote], nextCursor: null },
        }),
        "POST /api/projects/project-barn/follow": () => ({
          body: {
            id: "follow-1",
            projectId: "project-barn",
            userId: "user-ada",
            createdAt: "2026-11-01T10:00:00Z",
          },
        }),
      }),
    );
    vi.stubGlobal("fetch", dotNet.fetch);
    renderOverRouter(<HomePage />, router, createTRPCContext);

    await screen.findByText("Saving seeds this spring");
    expect(renderedBodies()).toEqual([
      "Framing the east wall Saturday, need two hands",
      "Saving seeds this spring",
    ]);
    expect(screen.queryByRole("navigation", { name: "Sectors" })).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Load more" }));
    await screen.findByText("Proposal opened: roof material");

    const sectors = screen.getByRole("navigation", { name: "Sectors" });
    expect(
      within(sectors)
        .getAllByRole("link")
        .map((chip) => chip.getAttribute("href")),
    ).toEqual(["/tag/construction", "/tag/food-beverage"]);
    expect(
      screen
        .getByRole("link", { name: "Start a project" })
        .getAttribute("href"),
    ).toBe("/projects/create");
    // A proposal is on screen, so its record note is too (verbatim guard copy).
    screen.getByText(
      "Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as attendance or consent.",
    );

    fireEvent.click(
      screen.getAllByRole("button", { name: "Follow Barn raising" })[0],
    );

    // Every Barn raising card flips together; Seed library is untouched.
    await waitFor(() =>
      expect(
        screen.getAllByRole("button", { name: "Following Barn raising" }),
      ).toHaveLength(2),
    );
    screen.getByRole("button", { name: "Follow Seed library" });
    await waitFor(() =>
      expect(
        dotNet.calls
          .filter((call) => call.method === "POST")
          .map((call) => call.path),
      ).toEqual(["/api/projects/project-barn/follow"]),
    );
    expect(
      dotNet.calls
        .filter((call) => call.path === "/api/feed")
        .map((call) => new URLSearchParams(call.search).get("scope")),
    ).toEqual(["home", "home"]);
  });

  it.each([
    {
      subject: "a project she joined",
      attach: "project:project-barn",
      name: "Barn raising",
      sent: { type: "PROJECT_UPDATE", projectId: "project-barn" },
    },
    {
      subject: "a guild she is in",
      attach: "guild:guild-framers",
      name: "Timber framers",
      sent: { type: "GUILD_ACTIVITY", guildId: "guild-framers" },
    },
  ])(
    "posts to $subject from the bare composer, and the update goes first, naming it",
    async ({ attach, name, sent }) => {
      const dotNet = fakeDotNetApi(
        zeroFollowApi({
          "GET /api/projects/mine": () => ({ body: [adaProjectDto] }),
          "GET /api/guilds/mine": () => ({ body: [framersGuildDto] }),
          "GET /api/feed": feedPages({
            "": { items: [seeds], nextCursor: null },
          }),
          "POST /api/posts": (call) => {
            const request = call.body as Record<string, string | undefined>;
            return {
              status: 201,
              body: {
                id: "post-trusses",
                authorId: "user-ada",
                projectId: request.projectId ?? null,
                guildId: request.guildId ?? null,
                type: request.type,
                visibility: request.visibility,
                title: null,
                content: request.content,
                metadata: null,
                hashtags: [],
                taskId: null,
                likesCount: 0,
                commentsCount: 0,
                sharesCount: 0,
                viewsCount: 0,
                isPinned: false,
                createdAt: "2026-11-01T10:00:00Z",
                updatedAt: "2026-11-01T10:00:00Z",
              },
            };
          },
        }),
      );
      vi.stubGlobal("fetch", dotNet.fetch);
      renderOverRouter(<HomePage />, router, createTRPCContext);

      await screen.findByText("Saving seeds this spring");
      const composer = await screen.findByRole("region", {
        name: "Post an update",
      });
      // The composer's own picker: no Home-side "Post to".
      expect(composer.textContent).not.toContain("Post to");
      await within(composer).findByRole("option", { name });
      fireEvent.change(within(composer).getByLabelText("Attach"), {
        target: { value: attach },
      });
      const words = composer.querySelector("textarea")!;
      fireEvent.change(words, { target: { value: "Roof trusses are up." } });
      fireEvent.submit(words.closest("form")!);

      // The textarea echoes the draft as text, so wait on the stream itself.
      await waitFor(() =>
        expect(renderedBodies()).toEqual([
          "Roof trusses are up.",
          "Saving seeds this spring",
        ]),
      );
      const published = dotNet.calls.find((call) => call.path === "/api/posts");
      expect(published?.body).toEqual({
        content: "Roof trusses are up.",
        visibility: "PUBLIC",
        ...sent,
      });
      // Her own team's card: the subject chip names it, and there is no Follow toggle.
      const first = screen.getAllByRole("article")[0];
      within(first).getByRole("link", { name });
      expect(
        within(first).queryByRole("button", { name: /^Follow/ }),
      ).toBeNull();
    },
  );
});
