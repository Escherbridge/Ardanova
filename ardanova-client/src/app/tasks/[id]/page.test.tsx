// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  act,
  configure,
  fireEvent,
  render,
  screen,
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

const viewer = vi.hoisted(() => ({ id: "user-a" }));

// tRPC's dev timing middleware (src/server/api/trpc.ts) delays each call 100-500 ms; the page chains up to three reads.
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
    data: { expires: "2099-01-01T00:00:00Z", user: { id: viewer.id } },
    status: "authenticated",
  }),
}));
vi.mock("next/navigation", () => ({ useParams: () => ({ id: "task-1" }) }));

// The real routers and API client run in-process and read `~/env` on import; jsdom defines `window`, so validation is
// skipped and the values are loopback fixtures (same as like-toggle.test.tsx).
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "task-page-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "task-page-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const router = createTRPCRouter({
  task: (await import("~/server/api/routers/task")).taskRouter,
  project: (await import("~/server/api/routers/project")).projectRouter,
  taskEscrow: (await import("~/server/api/routers/task-escrow"))
    .taskEscrowRouter,
  azoaCustodialAccount: (
    await import("~/server/api/routers/azoa-custodial-account")
  ).azoaCustodialAccountRouter,
  post: (await import("~/server/api/routers/post")).postRouter,
});
const { default: TaskDetailPage } = await import("./page");

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

/** .NET TaskDto, open and unpaid unless overridden. */
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

/** .NET ProjectDto; Bea created the project. */
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

/** .NET ProjectMemberDto[]: Ada is a contributor; Cy only follows the project, so is not listed. */
const membersDto = [
  {
    id: "member-a",
    projectId: "project-p",
    userId: "user-a",
    role: "CONTRIBUTOR",
    shareBalance: 0,
    votingPower: 0,
    joinedAt: NOW,
    invitedById: "user-b",
    user: { id: "user-a", name: "Ada Joiner", email: null, image: null },
  },
];

/** .NET AzoaCustodialAccountStatusDto. */
const accountStatus = {
  ready: {
    avatarId: "avatar-a",
    walletId: "wallet-a",
    walletAddress: "0x7a11e7",
    kycStatus: "Approved",
    identityReady: true,
    kycReady: true,
    walletReady: true,
    ready: true,
    unavailableReason: null,
    reviewDueAt: null,
  },
  setupLeft: {
    avatarId: "avatar-a",
    walletId: null,
    walletAddress: null,
    kycStatus: "Pending",
    identityReady: true,
    kycReady: false,
    walletReady: false,
    ready: false,
    unavailableReason: null,
    reviewDueAt: null,
  },
};

const askPost = {
  id: "post-ask",
  authorId: "user-b",
  projectId: "project-p",
  guildId: null,
  type: "HELP_WANTED",
  visibility: "PUBLIC",
  title: null,
  content: "Framing the east wall Saturday, need two hands #carpentry",
  metadata: null,
  hashtags: ["carpentry"],
  taskId: "task-1",
  likesCount: 0,
  commentsCount: 0,
  sharesCount: 0,
  viewsCount: 0,
  isPinned: false,
  createdAt: NOW,
  updatedAt: NOW,
};

/** The task as the API currently stores it; a claim's answer can move it. */
let storedTask = taskDto();

function serve(overrides: Record<string, DotNetRoute> = {}) {
  routes = {
    "GET /api/tasks/task-1": () => ok(storedTask),
    "GET /api/projects/project-p": () => ok(projectDto),
    "GET /api/projects/project-p/members": () => ok(membersDto),
    "GET /api/taskescrows/task/task-1": () => ({
      status: 404,
      body: { error: "Escrow for task task-1 not found" },
    }),
    "GET /api/posts/post-ask": () => ok(askPost),
    ...overrides,
  };
}

function renderPage() {
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
  return render(
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        <TaskDetailPage />
      </api.Provider>
    </QueryClientProvider>,
  );
}

/** tRPC requests the page has started and not yet had answered (dependent reads start one render later). */
let inFlight = 0;

/** Waits until no request has been in flight for three ticks, so an absent action is absent for a reason, not a race. */
async function quiet() {
  for (let quietTicks = 0, ticks = 0; quietTicks < 3 && ticks < 400; ticks++) {
    await act(() => new Promise<void>((resolve) => setTimeout(resolve, 25)));
    quietTicks = inFlight === 0 ? quietTicks + 1 : 0;
  }
}

beforeEach(() => {
  viewer.id = "user-a";
  storedTask = taskDto();
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
  "Task page: Pick up task (SR-03 Phase 3 §2)",
  { timeout: 20_000 },
  () => {
    it("a member picks up an open, unpaid task: one claim as themself, then the button reads Picked up", async () => {
      serve({
        "PUT /api/tasks/task-1/assign": ({ body }) => {
          storedTask = taskDto({
            assignedToId: body?.userId,
            assignedTo: { id: "user-a", name: "Ada Joiner", image: null },
          });
          return ok(storedTask);
        },
      });
      renderPage();

      fireEvent.click(
        await screen.findByRole("button", { name: "Pick up task" }),
      );

      const pickedUp = await screen.findByRole("button", { name: "Picked up" });
      expect(pickedUp).toHaveProperty("disabled", true);
      expect(screen.queryByRole("button", { name: "Pick up task" })).toBeNull();
      const claims = callsTo("PUT", "/api/tasks/task-1/assign");
      expect(claims.map((claim) => claim.body)).toEqual([{ userId: "user-a" }]);
      await screen.findByText("Ada Joiner");
      // Unpaid: the account is never read.
      expect(callsTo("GET", "/api/azoa/custodial-account/status")).toHaveLength(
        0,
      );
    });

    it.each([
      {
        code: "TASK_TAKEN",
        apiMessage: "Already picked up.",
        shown: "Already taken",
        after: {},
      },
      {
        code: "TASK_DONE",
        apiMessage: "This task is done.",
        shown: "Done",
        after: { status: "COMPLETED" },
      },
      {
        code: "TASK_UNDER_AGREEMENT",
        apiMessage:
          "This task's worker was agreed through an accepted bid, so it can no longer be reassigned here.",
        shown: "Under agreement",
        after: { hasAgreement: true },
      },
      // A code this client doesn't know yet: the API's own words.
      {
        code: "TASK_ARCHIVED",
        apiMessage: "This task was archived.",
        shown: /This task was archived\./,
        after: {},
      },
    ])(
      "someone got there first: a 409 $code reads $shown, and no claim is offered again",
      async ({ code, apiMessage, shown, after }) => {
        serve({
          "PUT /api/tasks/task-1/assign": () => {
            storedTask = taskDto({
              assignedToId: "user-c",
              assignedTo: { id: "user-c", name: "Cy Quick", image: null },
              ...after,
            });
            return { status: 409, body: { error: apiMessage, code } };
          },
        });
        renderPage();

        fireEvent.click(
          await screen.findByRole("button", { name: "Pick up task" }),
        );

        await screen.findByText(shown);
        await screen.findByText("Cy Quick");
        expect(
          screen.queryByRole("button", { name: "Pick up task" }),
        ).toBeNull();
        expect(callsTo("PUT", "/api/tasks/task-1/assign")).toHaveLength(1);
      },
    );

    it.each([
      {
        account: "setup left to do",
        status: accountStatus.setupLeft,
        offered: "Finish setup",
      },
      {
        account: "a ready account",
        status: accountStatus.ready,
        offered: "Pick up task",
      },
    ])(
      "a paid task reads the account once; $account is offered $offered",
      async ({ status, offered }) => {
        storedTask = taskDto({ equityReward: 40 });
        serve({ "GET /api/azoa/custodial-account/status": () => ok(status) });
        renderPage();

        if (offered === "Finish setup") {
          const setup = await screen.findByRole("link", {
            name: "Finish setup",
          });
          const href = new URL(
            setup.getAttribute("href") ?? "",
            "http://localhost",
          );
          expect(href.pathname).toBe("/settings/verification");
          expect(href.searchParams.get("returnTo")).toBe("/tasks/task-1");
          expect(
            screen.queryByRole("button", { name: "Pick up task" }),
          ).toBeNull();
        } else {
          await screen.findByRole("button", { name: "Pick up task" });
          expect(
            screen.queryByRole("link", { name: "Finish setup" }),
          ).toBeNull();
        }
        await quiet();
        expect(
          callsTo("GET", "/api/azoa/custodial-account/status"),
        ).toHaveLength(1);
        expect(callsTo("PUT", "/api/tasks/task-1/assign")).toHaveLength(0);
      },
    );

    it("a non-member sees the ask and Message the asker, a draft link, but no claim", async () => {
      viewer.id = "user-c";
      storedTask = taskDto({
        helpWanted: {
          postId: "post-ask",
          askerId: "user-b",
          askerName: "Bea Builder",
        },
      });
      renderPage();

      await screen.findByText(/Framing the east wall Saturday/);
      const message = await screen.findByRole("link", {
        name: "Message Bea Builder",
      });
      expect(message.getAttribute("href")).toBe("/chats?with=user-b");

      await quiet();
      expect(callsTo("GET", "/api/projects/project-p/members")).toHaveLength(1);
      expect(screen.queryByRole("button", { name: "Pick up task" })).toBeNull();
      expect(screen.queryByRole("link", { name: "Finish setup" })).toBeNull();
      expect(callsTo("PUT", "/api/tasks/task-1/assign")).toHaveLength(0);
    });
  },
);
