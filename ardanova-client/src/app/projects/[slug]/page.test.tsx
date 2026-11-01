// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  configure,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
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

/** Who is signed in and what the URL says; the browser session and the BFF's session always agree. */
const browser = vi.hoisted(() => ({
  viewer: null as { id: string; name: string } | null,
  search: new URLSearchParams(),
}));

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () =>
    browser.viewer
      ? {
          expires: "2099-01-01T00:00:00Z",
          user: { id: browser.viewer.id, role: "INDIVIDUAL" },
        }
      : null,
}));
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: browser.viewer
      ? {
          expires: "2099-01-01T00:00:00Z",
          user: {
            id: browser.viewer.id,
            name: browser.viewer.name,
            image: null,
          },
        }
      : null,
    status: browser.viewer ? "authenticated" : "unauthenticated",
  }),
}));
vi.mock("next/navigation", () => ({
  useParams: () => ({ slug: "barn-raising" }),
  useSearchParams: () => browser.search,
  usePathname: () => "/projects/barn-raising",
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    refresh: vi.fn(),
    back: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

// The real routers and API client run in-process and read `~/env` on import; jsdom defines `window`, so validation is
// skipped and the values are loopback fixtures (same as like-toggle.test.tsx).
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "project-page-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "project-page-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const router = createTRPCRouter({
  post: (await import("~/server/api/routers/post")).postRouter,
  feed: (await import("~/server/api/routers/feed")).feedRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  guild: (await import("~/server/api/routers/guild")).guildRouter,
  task: (await import("~/server/api/routers/task")).taskRouter,
  taxonomy: (await import("~/server/api/routers/taxonomy")).taxonomyRouter,
  membershipCredential: (
    await import("~/server/api/routers/membership-credential")
  ).membershipCredentialRouter,
});
const { default: ProjectDetailPage } = await import("./page");

// Every wait crosses the real routers in-process, and the header Follow sits behind four sequential reads
// (project by id, by slug, members, follow state); the 1 s default is too tight once a full suite runs alongside.
configure({ asyncUtilTimeout: 5000 });

// ---------------------------------------------------------------------------------------------------------------------
// The .NET API: the only faked collaborator, answering with its real JSON (camelCase, string enums, error envelope).
// ---------------------------------------------------------------------------------------------------------------------

interface DotNetCall {
  method: string;
  path: string;
  query: URLSearchParams;
  body: Record<string, unknown> | undefined;
}
type DotNetAnswer = { status: number; body?: unknown };
type DotNetRoute = (call: DotNetCall) => DotNetAnswer;

const calls: DotNetCall[] = [];
const unexpected: string[] = [];
let routes: Record<string, DotNetRoute> = {};

const dotNetApi = vi.fn(async (url: string, init: RequestInit) => {
  const parsed = new URL(url);
  const call: DotNetCall = {
    method: init.method ?? "GET",
    path: parsed.pathname,
    query: parsed.searchParams,
    body:
      typeof init.body === "string" && init.body
        ? (JSON.parse(init.body) as Record<string, unknown>)
        : undefined,
  };
  calls.push(call);
  const route = routes[`${call.method} ${call.path}`];
  if (!route) unexpected.push(`${call.method} ${call.path}`);
  const answer = route
    ? route(call)
    : { status: 404, body: { error: "Not found", statusCode: 404 } };
  return new Response(
    answer.body === undefined ? null : JSON.stringify(answer.body),
    { status: answer.status, headers: { "Content-Type": "application/json" } },
  );
});

const ok = (body: unknown): DotNetAnswer => ({ status: 200, body });
const callsTo = (method: string, path: string) =>
  calls.filter((call) => call.method === method && call.path === path);

const NOW = "2026-11-01T09:30:00Z";
const EARLIER = "2026-10-30T16:00:00Z";
const people = {
  "user-a": "Ada Apprentice",
  "user-b": "Bea Builder",
  "user-c": "Cal Creator",
} as const;
type Person = keyof typeof people;

/** .NET ProjectDto; Cal created it, and Lane L derives `hashtags` from its sector and type. */
const projectDto = {
  id: "project-p",
  title: "Barn raising",
  slug: "barn-raising",
  description: "A shared barn for the commons.",
  summary: "A barn for the commons.",
  problemStatement: "No covered space for winter work.",
  solution: "Raise a timber barn together.",
  categories: ["CONSTRUCTION"],
  hashtags: ["#construction", "#community"],
  status: "PUBLISHED",
  fundingGoal: null,
  fundingGoalUnit: "USD",
  currentFunding: 0,
  supportersCount: 0,
  votesCount: 0,
  viewsCount: 0,
  featured: false,
  tags: "timber-framing",
  images: null,
  videos: null,
  documents: null,
  mediaPaths: "[]",
  targetAudience: null,
  expectedImpact: null,
  timeline: null,
  projectType: "COMMUNITY",
  duration: null,
  createdAt: EARLIER,
  updatedAt: EARLIER,
  publishedAt: EARLIER,
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-c",
  createdBy: { id: "user-c", name: people["user-c"], image: null },
  assignedGuildId: null,
};

/** .NET ProjectMemberDto. */
const memberDto = (userId: Person, role: string) => ({
  id: `member-${userId}`,
  projectId: "project-p",
  userId,
  role,
  shareBalance: 0,
  votingPower: 0,
  joinedAt: EARLIER,
  invitedById: null,
  user: { id: userId, name: people[userId], email: null, image: null },
});

/** What differs between the stream's cards; the rest of each FeedCardDto is Bea's post on Barn raising. */
interface StreamCard {
  id: string;
  postType: string;
  sourceType: string | null;
  body: string;
  occurredAt: string;
}

/** .NET FeedCardDto on the `project:project-p` stream; `isMember`/`isFollowing` are the reader's, as the API computes them. */
function feedCardDto(
  card: StreamCard,
  reader: { isMember: boolean; isFollowing: boolean },
) {
  return {
    ...card,
    author: { id: "user-b", name: people["user-b"], image: null },
    title: null,
    bodyIsUserWritten: true,
    hashtags: [],
    project: {
      id: "project-p",
      title: "Barn raising",
      slug: "barn-raising",
      summary: "A barn for the commons.",
      sectors: ["CONSTRUCTION"],
    },
    guild: null,
    opportunity: null,
    event: null,
    funding: null,
    thread: null,
    counters: { likes: 0, replies: 0, shares: 0 },
    viewer: { ...reader, canClaim: false },
    isDiscovery: false,
    reasons: [],
    task: null,
  };
}

/** A row the old Updates tab wrote: the projector already carries it into the feed as PROJECT_UPDATE. */
const legacyUpdate: StreamCard = {
  id: "card-legacy-update",
  postType: "PROJECT_UPDATE",
  sourceType: "PROJECT_UPDATE",
  body: "Foundation poured, curing for a week.",
  occurredAt: EARLIER,
};
const benchNote: StreamCard = {
  id: "card-bench-note",
  postType: "POST",
  sourceType: null,
  body: "Sawhorses are in the shed.",
  occurredAt: EARLIER,
};
const WRITTEN = "Raised the east wall today #carpentry";

/** .NET PostDto for the post `POST /api/posts` stored. */
function storedPost(body: Record<string, unknown> | undefined) {
  return {
    id: "post-east-wall",
    authorId: browser.viewer?.id,
    projectId: body?.projectId ?? null,
    guildId: null,
    type: body?.type ?? "POST",
    visibility: body?.visibility ?? "PUBLIC",
    title: null,
    content: body?.content,
    metadata: null,
    hashtags: ["carpentry"],
    taskId: null,
    likesCount: 0,
    commentsCount: 0,
    sharesCount: 0,
    viewsCount: 0,
    isPinned: false,
    createdAt: NOW,
    updatedAt: NOW,
  };
}

/** The API's state: the project's stream (newest first) and whether the signed-in reader follows the project. */
const server = {
  stream: [] as StreamCard[],
  members: [] as ReturnType<typeof memberDto>[],
  following: false,
};

function serve() {
  const isMember = () =>
    browser.viewer?.id === projectDto.createdById ||
    server.members.some((member) => member.userId === browser.viewer?.id);
  routes = {
    // getById tries the id first; a slug 404s there and resolves on the slug route.
    "GET /api/projects/barn-raising": () => ({
      status: 404,
      body: { error: "Project barn-raising not found", statusCode: 404 },
    }),
    "GET /api/projects/slug/barn-raising": () => ok(projectDto),
    "GET /api/projects/project-p": () => ok(projectDto),
    "GET /api/projects/project-p/members": () => ok(server.members),
    "GET /api/membershipcredentials/project/project-p/active": () => ok([]),
    "GET /api/feed": () => {
      const reader = { isMember: isMember(), isFollowing: server.following };
      return ok({
        items: server.stream.map((card) => feedCardDto(card, reader)),
        nextCursor: null,
      });
    },
    "GET /api/projects/project-p/follow": () =>
      ok({ following: server.following }),
    "POST /api/projects/project-p/follow": () => {
      server.following = true;
      return ok({
        id: "follow-1",
        projectId: "project-p",
        userId: browser.viewer?.id,
        createdAt: NOW,
      });
    },
    "DELETE /api/projects/project-p/follow": () => {
      server.following = false;
      return ok({ following: false });
    },
    "POST /api/posts": ({ body }) => ({ status: 201, body: storedPost(body) }),
    "GET /api/taxonomy": () => ok([]),
  };
}

/** Renders the page under a real tRPC client whose requests the real routers answer in-process; a fresh cache each time. */
function openProjectPage() {
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
            // jsdom's AbortSignal is not Node's; the in-process request needs no abort.
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
        <ProjectDetailPage />
      </api.Provider>
    </QueryClientProvider>,
  );
}

const articles = () =>
  within(screen.getByRole("feed", { name: "Project feed" })).getAllByRole(
    "article",
  );
const button = (name: string) =>
  screen.getByRole<HTMLButtonElement>("button", { name });
const isEnabled = (name: string) => () =>
  expect(button(name).disabled).toBe(false);

beforeEach(() => {
  calls.length = 0;
  unexpected.length = 0;
  browser.search = new URLSearchParams();
  window.history.replaceState(null, "", "/projects/barn-raising");
  server.stream = [legacyUpdate];
  server.members = [];
  server.following = false;
  serve();
  vi.stubGlobal("fetch", dotNetApi);
});
afterEach(() => {
  expect(unexpected).toEqual([]);
  vi.unstubAllGlobals();
  dotNetApi.mockClear();
});
afterAll(() => vi.unstubAllEnvs());

describe("Project page: the feed is the default tab (SR-03 P3 §1, Lane H)", () => {
  it("a member publishes from the Feed tab: the post is first at once, named, and still first after a reload", async () => {
    browser.viewer = { id: "user-b", name: people["user-b"] };
    server.members = [memberDto("user-b", "CONTRIBUTOR")];
    const firstVisit = openProjectPage();

    const feedTab = await screen.findByRole("tab", { name: "Feed" });
    expect(feedTab.getAttribute("aria-selected")).toBe("true");
    // The light header: title, lede, and the API's derived tags plus the free one, each a /tag link.
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      "Barn raising",
    );
    const tags = within(screen.getByRole("list", { name: "Tags" }));
    expect(
      tags.getAllByRole("link").map((link) => link.getAttribute("href")),
    ).toEqual(["/tag/construction", "/tag/community", "/tag/timber-framing"]);
    // The old Updates rows are cards in the stream now.
    await waitFor(() => expect(articles()).toHaveLength(1));
    expect(articles()[0]?.textContent).toContain(legacyUpdate.body);

    const words = await screen.findByRole<HTMLTextAreaElement>("textbox", {
      name: "What moved?",
    });
    fireEvent.change(words, { target: { value: WRITTEN } });
    fireEvent.click(button("Post update"));

    await waitFor(() => expect(articles()).toHaveLength(2));
    const [published] = articles();
    expect(published?.textContent).toContain(WRITTEN);
    expect(published?.textContent).toContain(people["user-b"]);
    expect(callsTo("POST", "/api/posts")[0]?.body).toMatchObject({
      projectId: "project-p",
      content: WRITTEN,
    });
    // At once: the card came from the publish, not from reading the stream again. The scope names the id, not the slug.
    expect(
      callsTo("GET", "/api/feed").map((call) => call.query.get("scope")),
    ).toEqual(["project:project-p"]);
    // The composer is the one writer, and a member never reads or sees Follow.
    expect(
      calls.filter((call) =>
        call.path.startsWith("/api/projects/project-p/updates"),
      ),
    ).toEqual([]);
    expect(callsTo("GET", "/api/projects/project-p/follow")).toEqual([]);
    expect(screen.queryByRole("button", { name: "Follow" })).toBeNull();

    firstVisit.unmount();
    server.stream = [
      {
        id: "post-east-wall",
        postType: "PROJECT_UPDATE",
        sourceType: null,
        body: WRITTEN,
        occurredAt: NOW,
      },
      legacyUpdate,
    ];
    openProjectPage();

    await waitFor(() => expect(articles()).toHaveLength(2));
    expect(articles()[0]?.textContent).toContain(WRITTEN);
    expect(articles()[1]?.textContent).toContain(legacyUpdate.body);
  });

  it("a reader off the team follows from the header; the header and every card chip stay in step", async () => {
    browser.viewer = { id: "user-a", name: people["user-a"] };
    server.members = [memberDto("user-b", "CONTRIBUTOR")];
    server.stream = [benchNote, legacyUpdate];
    openProjectPage();

    await waitFor(isEnabled("Follow"));
    expect(screen.queryByRole("textbox", { name: "What moved?" })).toBeNull();
    await waitFor(() => expect(articles()).toHaveLength(2));
    const chips = (label: string) =>
      screen.queryAllByRole("button", { name: `${label} Barn raising` });
    expect(chips("Follow")).toHaveLength(2);

    // Header -> API -> every chip.
    fireEvent.click(button("Follow"));
    expect(screen.queryByRole("button", { name: "Following" })).not.toBeNull();
    await waitFor(() => expect(chips("Following")).toHaveLength(2));
    await waitFor(isEnabled("Following"));
    expect(callsTo("POST", "/api/projects/project-p/follow")).toHaveLength(1);
    expect(server.following).toBe(true);

    // A chip -> API -> the header.
    fireEvent.click(chips("Following")[0]);
    await waitFor(isEnabled("Follow"));
    expect(callsTo("DELETE", "/api/projects/project-p/follow")).toHaveLength(1);
    expect(chips("Follow")).toHaveLength(2);

    // The header again, after a chip held its own state: the chips still follow the header.
    fireEvent.click(button("Follow"));
    await waitFor(isEnabled("Following"));
    expect(callsTo("POST", "/api/projects/project-p/follow")).toHaveLength(2);
    await waitFor(() => expect(chips("Following")).toHaveLength(2));
    expect(server.following).toBe(true);
  });

  it("an old ?tab=updates link opens the Feed; Updates is gone, and picking a tab writes ?tab=", async () => {
    browser.viewer = { id: "user-c", name: people["user-c"] };
    browser.search = new URLSearchParams("tab=updates");
    openProjectPage();

    const feedTab = await screen.findByRole("tab", { name: "Feed" });
    expect(feedTab.getAttribute("aria-selected")).toBe("true");
    expect(screen.queryByRole("tab", { name: "Updates" })).toBeNull();
    expect(screen.queryByRole("button", { name: /new update/i })).toBeNull();
    // The creator writes through the composer, and the creator never sees Follow either.
    await screen.findByRole("textbox", { name: "What moved?" });
    expect(screen.queryByRole("button", { name: "Follow" })).toBeNull();

    fireEvent.click(screen.getByRole("tab", { name: "Overview" }));
    expect(
      screen
        .getByRole("tab", { name: "Overview" })
        .getAttribute("aria-selected"),
    ).toBe("true");
    expect(new URL(window.location.href).searchParams.get("tab")).toBe(
      "overview",
    );
    expect(screen.queryByText(projectDto.problemStatement)).not.toBeNull();
  });
});
