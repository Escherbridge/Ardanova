// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  act,
  configure,
  fireEvent,
  render,
  screen,
  waitFor,
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

import type { WorkItemDetailSelection } from "./work-item-detail-modal";

const viewer = vi.hoisted(() => ({ id: "user-b" }));

// tRPC's dev timing middleware (src/server/api/trpc.ts) delays each call 100-500 ms; an ask chains several calls.
configure({ asyncUtilTimeout: 5000 });

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: viewer.id, role: "INDIVIDUAL" },
  }),
}));
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: {
      expires: "2099-01-01T00:00:00Z",
      user: { id: viewer.id, name: "Bea Builder", image: null },
    },
    status: "authenticated",
  }),
}));

// The real routers and API client run in-process and read `~/env` on import; jsdom defines `window`, so validation is
// skipped and the values are loopback fixtures (same as like-toggle.test.tsx).
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "work-item-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "work-item-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const router = createTRPCRouter({
  task: (await import("~/server/api/routers/task")).taskRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  post: (await import("~/server/api/routers/post")).postRouter,
  taxonomy: (await import("~/server/api/routers/taxonomy")).taxonomyRouter,
  comment: (await import("~/server/api/routers/comment")).commentRouter,
});
const { default: WorkItemDetailModal } = await import(
  "./work-item-detail-modal"
);

// ---------------------------------------------------------------------------------------------------------------------
// The .NET API: the only faked collaborator, answering with its real JSON (camelCase, string enums, error envelope).
// ---------------------------------------------------------------------------------------------------------------------

interface DotNetCall {
  method: string;
  path: string;
  body: Record<string, unknown> | undefined;
}
type DotNetAnswer = { status: number; body?: unknown };
type DotNetRoute = (call: DotNetCall) => DotNetAnswer;

const calls: DotNetCall[] = [];
const unexpected: string[] = [];
let routes: Record<string, DotNetRoute> = {};

const dotNetApi = vi.fn(async (url: string, init: RequestInit) => {
  const call: DotNetCall = {
    method: init.method ?? "GET",
    path: new URL(url).pathname,
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
    : { status: 404, body: { error: "Not found" } };
  return new Response(
    answer.body === undefined ? null : JSON.stringify(answer.body),
    { status: answer.status, headers: { "Content-Type": "application/json" } },
  );
});

const ok = (body: unknown): DotNetAnswer => ({ status: 200, body });
const callsTo = (method: string, path: string) =>
  calls.filter((call) => call.method === method && call.path === path);

const NOW = "2026-11-01T09:30:00Z";

/** .NET TaskDto: open, unassigned, nobody has asked about it yet. */
function taskDto(overrides: Record<string, unknown> = {}) {
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

/** .NET ProjectDto; Bea created the project, so she is a member without a member row. */
const projectDto = {
  id: "project-p",
  title: "Barn raising",
  slug: "barn-raising",
  description: "A shared barn for the commons.",
  summary: null,
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

/** .NET TaxonomyTermDto list. */
const taxonomy = [
  {
    slug: "CONSTRUCTION",
    kind: "SECTOR",
    label: "Construction",
    description: null,
    parentSlug: null,
    sortOrder: 0,
  },
  {
    slug: "CARPENTRY",
    kind: "SKILL",
    label: "Carpentry",
    description: null,
    parentSlug: "CONSTRUCTION",
    sortOrder: 1,
  },
];

function serve(overrides: Record<string, DotNetRoute> = {}) {
  routes = {
    "GET /api/projects/project-p": () => ok(projectDto),
    "GET /api/projects/project-p/members": () => ok([]),
    "GET /api/projects/comments/target/TASK/task-1": () => ok([]),
    // The composer's help picker reads the project's tasks to title the fixed one.
    "GET /api/tasks/search": () =>
      ok({
        items: [taskDto()],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      }),
    "GET /api/taxonomy": () => ok(taxonomy),
    // PostService.CreateAsync: the stored post, author taken from the actor.
    "POST /api/posts": ({ body }) => ({
      status: 201,
      body: {
        id: "post-ask",
        authorId: viewer.id,
        projectId: body?.projectId ?? null,
        guildId: null,
        type: body?.type,
        visibility: body?.visibility,
        title: null,
        content: body?.content,
        metadata: null,
        hashtags: [],
        taskId: body?.taskId ?? null,
        likesCount: 0,
        commentsCount: 0,
        sharesCount: 0,
        viewsCount: 0,
        isPinned: false,
        createdAt: NOW,
        updatedAt: NOW,
      },
    }),
    ...overrides,
  };
}

function renderModal(task = taskDto(), onClose = vi.fn()) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const client = api.createClient({
    links: [
      httpLink({
        url: "http://localhost:3000/api/trpc",
        transformer: superjson,
        fetch: async (input, init) => {
          inFlight += 1;
          try {
            return await fetchRequestHandler({
              endpoint: "/api/trpc",
              // jsdom's AbortSignal is not Node's; the in-process request needs no abort.
              req: new Request(input as RequestInfo, {
                ...(init as RequestInit),
                signal: undefined,
              }),
              router,
              createContext: ({ req }) =>
                createTRPCContext({ headers: req.headers }),
            });
          } finally {
            inFlight -= 1;
          }
        },
      }),
    ],
  });
  const selection = {
    level: "task",
    item: task,
  } as WorkItemDetailSelection;
  render(
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        <WorkItemDetailModal
          open
          onClose={onClose}
          selection={selection}
          projectId="project-p"
          canComment
          signInHref="/auth/signin"
        />
      </api.Provider>
    </QueryClientProvider>,
  );
  return { onClose };
}

/** tRPC requests the modal has started and not yet had answered (dependent reads start one render later). */
let inFlight = 0;

/** Waits until no request has been in flight for three ticks, so an absent action is absent for a reason, not a race. */
async function quiet() {
  for (let quietTicks = 0, ticks = 0; quietTicks < 3 && ticks < 400; ticks++) {
    await act(() => new Promise<void>((resolve) => setTimeout(resolve, 25)));
    quietTicks = inFlight === 0 ? quietTicks + 1 : 0;
  }
}

beforeEach(() => {
  viewer.id = "user-b";
  inFlight = 0;
  calls.length = 0;
  unexpected.length = 0;
  serve();
  vi.stubGlobal("fetch", dotNetApi);
});
afterEach(async () => {
  await quiet();
  expect(unexpected).toEqual([]);
  vi.unstubAllGlobals();
  dotNetApi.mockClear();
});
afterAll(() => vi.unstubAllEnvs());

describe(
  "Work-item modal: Ask for help (SR-03 Phase 3 §2)",
  { timeout: 20_000 },
  () => {
    it("opens the composer on this task and publishes one HELP_WANTED naming it", async () => {
      renderModal();

      fireEvent.click(
        await screen.findByRole("button", { name: "Ask for help" }),
      );
      const words = await screen.findByRole<HTMLTextAreaElement>("textbox", {
        name: "Who can help?",
      });
      await waitFor(() => expect(document.activeElement).toBe(words));
      // The fixed task is named on the sheet, not picked.
      await screen.findByText("Frame the east wall", { selector: "span" });

      fireEvent.change(words, {
        target: { value: "Framing the east wall Saturday, need two hands" },
      });
      fireEvent.click(screen.getByRole("button", { name: "Ask for help" }));

      await screen.findByText("Asked.");
      expect(
        screen.getByRole("link", { name: "Open task" }).getAttribute("href"),
      ).toBe("/tasks/task-1");
      const creates = callsTo("POST", "/api/posts");
      expect(creates).toHaveLength(1);
      expect(creates[0]?.body).toMatchObject({
        type: "HELP_WANTED",
        projectId: "project-p",
        taskId: "task-1",
        content: "Framing the east wall Saturday, need two hands",
      });
    });

    it("Escape with the # list open closes only the list; the next Escape closes the dialog", async () => {
      const { onClose } = renderModal();
      fireEvent.click(
        await screen.findByRole("button", { name: "Ask for help" }),
      );
      const words = await screen.findByRole<HTMLTextAreaElement>("textbox", {
        name: "Who can help?",
      });

      fireEvent.change(words, { target: { value: "Need two hands #carp" } });
      await screen.findByRole("listbox", { name: "Tags" });

      fireEvent.keyDown(words, { key: "Escape" });
      await waitFor(() =>
        expect(screen.queryByRole("listbox", { name: "Tags" })).toBeNull(),
      );
      expect(onClose).not.toHaveBeenCalled();
      expect(words.value).toBe("Need two hands #carp");

      fireEvent.keyDown(words, { key: "Escape" });
      expect(onClose).toHaveBeenCalledTimes(1);
    });

    it.each([
      {
        who: "someone who is not on the project",
        viewerId: "user-c",
        task: taskDto(),
        shown: null,
      },
      {
        who: "a member, when Cy already asked about this task",
        viewerId: "user-b",
        task: taskDto({
          helpWanted: {
            postId: "post-cy",
            askerId: "user-c",
            askerName: "Cy Quick",
          },
        }),
        shown: "Asked by Cy Quick",
      },
    ])(
      "$who is not offered Ask for help",
      async ({ viewerId, task, shown }) => {
        viewer.id = viewerId;
        renderModal(task);

        if (shown) await screen.findByText(shown);
        await quiet();
        expect(
          screen.queryByRole("button", { name: "Ask for help" }),
        ).toBeNull();
        expect(callsTo("POST", "/api/posts")).toHaveLength(0);
      },
    );
  },
);
