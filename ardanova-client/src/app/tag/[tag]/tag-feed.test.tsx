// @vitest-environment jsdom
import { screen } from "@testing-library/react";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  IN_PROCESS_SERVER_ENV,
  fakeDotNetApi,
  renderOverRouter,
} from "~/components/notifications/dotnet-test-edge";
import { feedCardFixture } from "~/lib/feed/card-fixture";

import { TagFeed } from "./tag-feed";

vi.mock("server-only", () => ({}));
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
const router = createTRPCRouter({
  feed: (await import("~/server/api/routers/feed")).feedRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  guild: (await import("~/server/api/routers/guild")).guildRouter,
});

afterEach(() => vi.unstubAllGlobals());
afterAll(() => vi.unstubAllEnvs());

describe("a tag page carries the record notes its PROPOSAL and FUNDING cards need", () => {
  it("shows both verbatim notes above the stream", async () => {
    // .NET FeedCardDto JSON: a proposal and a funding milestone, both tagged #carpentry.
    const cards = [
      feedCardFixture({
        id: "post-roof-vote",
        postType: "PROPOSAL",
        body: "Proposal opened: roof material",
        bodyIsUserWritten: false,
        hashtags: ["carpentry"],
      }),
      feedCardFixture({
        id: "post-funding",
        postType: "FUNDING_PROGRESS",
        body: "Reached 50% of its goal",
        bodyIsUserWritten: false,
        funding: { percent: 50 },
        hashtags: ["carpentry"],
      }),
    ];
    vi.stubGlobal("IntersectionObserver", undefined);
    vi.stubGlobal(
      "fetch",
      fakeDotNetApi({
        "GET /api/feed": () => ({ body: { items: cards, nextCursor: null } }),
      }).fetch,
    );

    renderOverRouter(<TagFeed tag="carpentry" />, router, createTRPCContext);

    const feed = await screen.findByRole("feed", { name: "#carpentry" });
    for (const note of [
      "Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as attendance or consent.",
      "Participation is not proof of ownership. Governance credentials, project utility, and economic rights remain separate, reviewable records.",
    ]) {
      expect(
        screen.getByText(note).compareDocumentPosition(feed) &
          Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    }
  });
});
