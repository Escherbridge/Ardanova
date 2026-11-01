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

import { feedCardSchema, type FeedCard } from "~/lib/contracts/feed-contract";
import { api } from "~/trpc/react";

import { FieldNoteComposer } from "./field-note-composer";
import { ProjectFieldNotes } from "./project-field-notes";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-b", role: "INDIVIDUAL" },
  }),
}));

// The real routers and API client run in-process and read `~/env` on import; jsdom defines `window`, so validation is
// skipped and the values are loopback fixtures (same as like-toggle.test.tsx).
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "composer-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "composer-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const { postRouter } = await import("~/server/api/routers/post");
const { taskRouter } = await import("~/server/api/routers/task");
const { taxonomyRouter } = await import("~/server/api/routers/taxonomy");
const { projectRouter } = await import("~/server/api/routers/project");
const { guildRouter } = await import("~/server/api/routers/guild");
const router = createTRPCRouter({
  post: postRouter,
  task: taskRouter,
  taxonomy: taxonomyRouter,
  project: projectRouter,
  guild: guildRouter,
});

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
const author = { id: "user-b", name: "Bea Builder", image: null };

/** .NET ProjectDto. */
const projectDto = {
  id: "project-p",
  title: "Barn raising",
  slug: "barn-raising",
  description: "A shared barn for the commons.",
  summary: "A barn for the commons.",
  problemStatement: "No covered space for winter work.",
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
  duration: null,
  createdAt: NOW,
  updatedAt: NOW,
  publishedAt: NOW,
  fundedAt: null,
  completedAt: null,
  commerceEnabled: false,
  storefrontDescription: null,
  createdById: "user-b",
  createdBy: { id: "user-b", name: "Bea Builder", image: null },
  assignedGuildId: null,
};

/** .NET GuildDto, as `GET /api/guilds/mine` lists it. */
const guildDto = {
  id: "guild-g",
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
  ownerId: "user-b",
};

/** .NET TaskDto. */
function taskDto(overrides: Record<string, unknown>) {
  return {
    id: "task-1",
    projectId: "project-p",
    pbiId: null,
    featureId: null,
    sprintId: null,
    epicId: null,
    milestoneId: null,
    guildId: null,
    opportunityId: null,
    title: "Frame the east wall",
    description: null,
    status: "TODO",
    priority: "MEDIUM",
    taskType: "FEATURE",
    estimatedHours: null,
    actualHours: null,
    equityReward: null,
    escrowStatus: "NONE",
    dueDate: null,
    completedAt: null,
    createdAt: NOW,
    updatedAt: NOW,
    assignedToId: null,
    assignedTo: null,
    project: { id: "project-p", title: "Barn raising", slug: "barn-raising" },
    hasAgreement: false,
    helpWanted: null,
    ...overrides,
  };
}

/** .NET PagedResult<T>. */
const paged = (items: unknown[]) => ({
  items,
  page: 1,
  pageSize: 100,
  totalCount: items.length,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
});

/** .NET PostDto. */
function postDto(overrides: Record<string, unknown>) {
  return {
    id: "post-new",
    authorId: "user-b",
    projectId: null,
    guildId: null,
    type: "POST",
    visibility: "PUBLIC",
    title: null,
    content: "",
    metadata: null,
    hashtags: [],
    taskId: null,
    likesCount: 0,
    commentsCount: 0,
    sharesCount: 0,
    viewsCount: 0,
    isPinned: false,
    createdAt: NOW,
    updatedAt: NOW,
    ...overrides,
  };
}

/** .NET TaxonomyTermDto list. */
const taxonomy = [
  ["CONSTRUCTION", "SECTOR", "Construction", 3, null],
  ["CARE", "SECTOR", "Care", 1, null],
  ["CARPENTRY", "SKILL", "Carpentry", 0, "CONSTRUCTION"],
].map(([slug, kind, label, sortOrder, parentSlug]) => ({
  slug,
  kind,
  label,
  description: null,
  parentSlug,
  sortOrder,
}));

/** `POST /api/posts` as PostService.CreateAsync answers: the stored post, author taken from the actor. */
const createdFromRequest: DotNetRoute = ({ body }) => ({
  status: 201,
  body: postDto({
    content: body?.content,
    type: body?.type ?? "POST",
    visibility: body?.visibility ?? "PUBLIC",
    projectId: body?.projectId ?? null,
    guildId: body?.guildId ?? null,
    taskId: body?.taskId ?? null,
  }),
});

function serve(overrides: Record<string, DotNetRoute> = {}) {
  routes = {
    "GET /api/projects/mine": () => ok([projectDto]),
    "GET /api/guilds/mine": () => ok([guildDto]),
    "GET /api/projects/project-p": () => ok(projectDto),
    "GET /api/taxonomy": () => ok(taxonomy),
    "POST /api/posts": createdFromRequest,
    ...overrides,
  };
}

/** Renders `ui` under a real tRPC client whose requests the real routers answer in-process. */
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
        {ui}
      </api.Provider>
    </QueryClientProvider>,
  );
}

const textbox = (name: string) =>
  screen.getByRole<HTMLTextAreaElement>("textbox", { name });
const button = (name: string) =>
  screen.getByRole<HTMLButtonElement>("button", { name });
const write = (field: HTMLTextAreaElement, text: string) =>
  fireEvent.change(field, { target: { value: text } });
const pick = (label: string, value: string) =>
  fireEvent.change(screen.getByLabelText(label), { target: { value } });

beforeEach(() => {
  calls.length = 0;
  unexpected.length = 0;
  serve();
  vi.stubGlobal("fetch", dotNetApi);
});
afterEach(() => {
  expect(unexpected).toEqual([]);
  vi.unstubAllGlobals();
  dotNetApi.mockClear();
});
afterAll(() => vi.unstubAllEnvs());

describe("FieldNoteComposer: each prompt writes its post type (plan §1-§2)", () => {
  it.each([
    {
      flow: "What moved? with nothing attached is a POST anyone can read",
      prompt: "What moved?",
      attach: "",
      audience: "PUBLIC",
      sent: { type: "POST", visibility: "PUBLIC" },
      subject: { project: null, guild: null },
    },
    {
      flow: "What's stuck? on a project you're on is a team-only PROJECT_UPDATE",
      prompt: "What's stuck?",
      attach: "project:project-p",
      audience: "PROJECT_MEMBERS",
      sent: {
        type: "PROJECT_UPDATE",
        visibility: "PROJECT_MEMBERS",
        projectId: "project-p",
      },
      subject: {
        project: {
          id: "project-p",
          title: "Barn raising",
          slug: "barn-raising",
          summary: "A barn for the commons.",
          sectors: ["CONSTRUCTION"],
        },
        guild: null,
      },
    },
    {
      flow: "a note in your guild is a GUILD_ACTIVITY",
      prompt: "What moved?",
      attach: "guild:guild-g",
      audience: "PUBLIC",
      sent: {
        type: "GUILD_ACTIVITY",
        visibility: "PUBLIC",
        guildId: "guild-g",
      },
      subject: {
        project: null,
        guild: {
          id: "guild-g",
          name: "Timber framers",
          slug: "timber-framers",
        },
      },
    },
  ])(
    "$flow; onPublished gets the created post as a leading card",
    async ({ prompt, attach, audience, sent, subject }) => {
      const published = vi.fn<(card: FeedCard) => void>();
      renderSignedIn(
        <FieldNoteComposer author={author} onPublished={published} />,
      );

      fireEvent.click(screen.getByRole("radio", { name: prompt }));
      await screen.findByRole("option", { name: "Timber framers" });
      if (attach) pick("Attach", attach);
      pick("Who can read", audience);
      write(textbox(prompt), "Walls are up on the east side.");
      fireEvent.click(button("Post update"));

      await waitFor(() => expect(published).toHaveBeenCalledTimes(1));
      const [create] = callsTo("POST", "/api/posts");
      // Exactly these fields: no taskId on a note (the API refuses one), no stray attachment.
      expect(create?.body).toEqual({
        content: "Walls are up on the east side.",
        ...sent,
      });

      const card = published.mock.calls[0][0];
      expect(feedCardSchema.parse(card)).toEqual(card);
      expect(card).toMatchObject({
        id: "post-new",
        postType: sent.type,
        body: "Walls are up on the east side.",
        bodyIsUserWritten: true,
        author,
        task: null,
        ...subject,
      });
      expect(textbox(prompt).value).toBe("");
    },
  );
});

describe("FieldNoteComposer: Who can help? (HELP_WANTED)", () => {
  it("lists only open, unassigned tasks, refuses to publish without one, and sends the task", async () => {
    serve({
      "GET /api/tasks/search": () =>
        ok(
          paged([
            taskDto({
              id: "task-open",
              title: "Frame the east wall",
              equityReward: 40,
            }),
            taskDto({
              id: "task-taken",
              title: "Order lumber",
              status: "IN_PROGRESS",
              assignedToId: "user-c",
            }),
            taskDto({
              id: "task-done",
              title: "Pour footings",
              status: "COMPLETED",
            }),
            taskDto({
              id: "task-blocked",
              title: "File the permit",
              status: "BLOCKED",
            }),
          ]),
        ),
    });
    const published = vi.fn<(card: FeedCard) => void>();
    renderSignedIn(
      <FieldNoteComposer author={author} onPublished={published} />,
    );

    fireEvent.click(screen.getByRole("radio", { name: "Who can help?" }));
    const words = textbox("Who can help?");
    write(words, "Need two hands Saturday #carpentry");
    expect(button("Ask for help").disabled).toBe(true);

    await screen.findByRole("option", { name: "Barn raising" });
    pick("Project", "project:project-p");
    await screen.findByLabelText("Task");
    expect(callsTo("GET", "/api/tasks/search")[0]?.query.get("projectId")).toBe(
      "project-p",
    );
    const offered = screen
      .getAllByRole("option")
      .map((option) => option.textContent);
    expect(offered).toEqual(
      expect.arrayContaining(["Frame the east wall", "File the permit"]),
    );
    expect(offered).not.toContain("Order lumber");
    expect(offered).not.toContain("Pour footings");

    // No task yet: the button is off and a keyboard submit sends nothing either.
    expect(button("Ask for help").disabled).toBe(true);
    fireEvent.keyDown(words, { key: "Enter", ctrlKey: true });
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(callsTo("POST", "/api/posts")).toHaveLength(0);

    pick("Task", "task-open");
    fireEvent.click(button("Ask for help"));

    await waitFor(() => expect(published).toHaveBeenCalledTimes(1));
    expect(callsTo("POST", "/api/posts")[0]?.body).toEqual({
      content: "Need two hands Saturday #carpentry",
      type: "HELP_WANTED",
      visibility: "PUBLIC",
      projectId: "project-p",
      taskId: "task-open",
    });
    const card = published.mock.calls[0][0];
    expect(feedCardSchema.parse(card)).toEqual(card);
    expect(card).toMatchObject({
      postType: "HELP_WANTED",
      task: { id: "task-open", state: "OPEN", assigneeName: null, paid: true },
      // Never claimable locally: the next feed read carries the server's TaskClaimRule answer.
      viewer: { isMember: true, canClaim: false },
    });
  });

  describe("a second ask on the same task (409 carrying the existing post id)", () => {
    const existingAsk = (authorId: string) =>
      postDto({
        id: "post-existing",
        authorId,
        type: "HELP_WANTED",
        projectId: "project-p",
        taskId: "task-1",
        content: "Framing the east wall Saturday, need two hands #carpentry",
        hashtags: ["carpentry"],
      });

    /** The work-item modal's mount: project and task fixed, opening in help mode. */
    async function askAgain(existingAuthorId: string) {
      serve({
        "GET /api/tasks/search": () => ok(paged([taskDto({})])),
        // PostsController's Conflict arm, after ErrorEnvelopeResultFilter adds statusCode and traceId.
        "POST /api/posts": () => ({
          status: 409,
          body: {
            error: "This task already has an ask.",
            code: "HELP_WANTED_EXISTS",
            conflictId: "post-existing",
            statusCode: 409,
            traceId: "00-composer-test-00",
          },
        }),
        "GET /api/posts/post-existing": () => ok(existingAsk(existingAuthorId)),
        "PUT /api/posts/post-existing": ({ body }) =>
          ok({ ...existingAsk(existingAuthorId), content: body?.content }),
      });
      const published = vi.fn<(card: FeedCard) => void>();
      renderSignedIn(
        <FieldNoteComposer
          projectId="project-p"
          taskId="task-1"
          author={author}
          onPublished={published}
        />,
      );
      expect(
        screen
          .getByRole("radio", { name: "Who can help?" })
          .getAttribute("aria-checked"),
      ).toBe("true");
      await screen.findByText("Frame the east wall");

      write(textbox("Who can help?"), "Anyone free Saturday?");
      fireEvent.click(button("Ask for help"));
      const edit = await screen.findByRole("button", { name: "Edit your ask" });
      return { edit, published };
    }

    it("offers Edit your ask instead of an error, opens the existing ask in place, and saves it", async () => {
      const { edit, published } = await askAgain("user-b");
      expect(edit.getAttribute("data-existing-post-id")).toBe("post-existing");
      expect(screen.queryByRole("alert")).toBeNull();

      fireEvent.click(edit);
      const words = await screen.findByRole<HTMLTextAreaElement>("textbox", {
        name: "Your ask",
      });
      expect(words.value).toBe(
        "Framing the east wall Saturday, need two hands #carpentry",
      );
      expect(callsTo("GET", "/api/posts/post-existing")).toHaveLength(1);
      expect(document.activeElement).toBe(words);

      write(
        words,
        "Framing the east wall Saturday, need three hands #carpentry",
      );
      fireEvent.click(button("Save ask"));

      await waitFor(() => expect(published).toHaveBeenCalledTimes(1));
      expect(callsTo("PUT", "/api/posts/post-existing")[0]?.body).toEqual({
        content: "Framing the east wall Saturday, need three hands #carpentry",
      });
      // Same id as the stream's copy, so mergeFeedPages shows the edit once, at the top.
      expect(published.mock.calls[0][0]).toMatchObject({
        id: "post-existing",
        body: "Framing the east wall Saturday, need three hands #carpentry",
        project: { id: "project-p", slug: "barn-raising" },
        task: { id: "task-1", state: "OPEN" },
      });
      expect(callsTo("POST", "/api/posts")).toHaveLength(1);
    });

    it("when the existing ask is someone else's, points at the task instead of an edit that would be refused", async () => {
      const { edit } = await askAgain("user-c");

      fireEvent.click(edit);

      expect(await screen.findByText("Asked by someone else.")).toBeTruthy();
      expect(
        screen.getByRole("link", { name: "Open task" }).getAttribute("href"),
      ).toBe("/tasks/task-1");
      expect(screen.queryByRole("textbox", { name: "Your ask" })).toBeNull();
      expect(callsTo("PUT", "/api/posts/post-existing")).toHaveLength(0);
    });
  });
});

describe("FieldNoteComposer: inline # suggestions", () => {
  it("loads canonical tags on the first #, picks one from the keyboard, and publishes it as typed", async () => {
    renderSignedIn(<FieldNoteComposer author={author} />);
    const words = textbox("What moved?");

    write(words, "Framing the east wall");
    expect(callsTo("GET", "/api/taxonomy")).toHaveLength(0);
    write(words, "Framing the east wall #ca");

    const list = await screen.findByRole("listbox", { name: "Tags" });
    const options = Array.from(list.querySelectorAll('[role="option"]'));
    expect(options.map((option) => option.textContent)).toEqual([
      "#careCare",
      "#carpentryCarpentry",
    ]);
    expect(words.getAttribute("aria-activedescendant")).toBe(options[0].id);

    fireEvent.keyDown(words, { key: "ArrowDown" });
    expect(words.getAttribute("aria-activedescendant")).toBe(options[1].id);
    fireEvent.keyDown(words, { key: "Enter" });

    expect(words.value).toBe("Framing the east wall #carpentry ");
    expect(screen.queryByRole("listbox", { name: "Tags" })).toBeNull();

    fireEvent.click(button("Post update"));
    await waitFor(() => expect(callsTo("POST", "/api/posts")).toHaveLength(1));
    expect(callsTo("POST", "/api/posts")[0]?.body?.content).toBe(
      "Framing the east wall #carpentry",
    );
    expect(callsTo("GET", "/api/taxonomy")).toHaveLength(1);
  });
});

describe("existing usage: the project page's field notes (projectId + onPosted)", () => {
  it("still posts a PROJECT_UPDATE and the stream shows it, without reading the attach lists", async () => {
    let notes: unknown[] = [];
    serve({
      "GET /api/projects/project-p/field-notes": () =>
        ok({ ...paged(notes), pageSize: 20 }),
      "POST /api/posts": (call) => {
        const created = createdFromRequest(call);
        notes = [created.body];
        return created;
      },
    });
    renderSignedIn(
      <ProjectFieldNotes projectId="project-p" canPost viewerId="user-b" />,
    );

    await screen.findByText("No field notes yet.");
    write(textbox("What moved?"), "Walls are up.");
    fireEvent.click(button("Post update"));

    // The stream's copy, not the draft: React mirrors a textarea's value into its text content.
    expect(
      await screen.findByText("Walls are up.", {
        ignore: "textarea, script, style",
      }),
    ).toBeTruthy();
    expect(textbox("What moved?").value).toBe("");
    expect(callsTo("POST", "/api/posts")[0]?.body).toEqual({
      content: "Walls are up.",
      type: "PROJECT_UPDATE",
      visibility: "PUBLIC",
      projectId: "project-p",
    });
    // A fixed project needs no picker, so Home's attach-list reads never happen on the project page.
    expect(
      calls.filter(
        (call) =>
          call.path === "/api/projects/mine" ||
          call.path === "/api/guilds/mine",
      ),
    ).toEqual([]);
  });
});
