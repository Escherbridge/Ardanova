// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  act,
  configure,
  fireEvent,
  render,
  screen,
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

// tRPC's dev timing middleware (src/server/api/trpc.ts) delays each call 100-500 ms; a send chains four calls.
configure({ asyncUtilTimeout: 5000 });

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-a", role: "INDIVIDUAL" },
  }),
}));
vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: { expires: "2099-01-01T00:00:00Z", user: { id: "user-a" } },
    status: "authenticated",
  }),
}));
// The page as `/chats?with=user-b`, the link a help card's "Message" opens.
vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams("with=user-b"),
}));
// SignalR is the other process edge: no live events in these flows.
vi.mock("~/providers/realtime-provider", () => ({
  useRealtimeContext: () => ({ subscribe: () => () => undefined }),
}));

// The real routers and API client run in-process and read `~/env` on import; jsdom defines `window`, so validation is
// skipped and the values are loopback fixtures (same as like-toggle.test.tsx).
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "chats-page-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "chats-page-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const router = createTRPCRouter({
  chat: (await import("~/server/api/routers/chat")).chatRouter,
  user: (await import("~/server/api/routers/user")).userRouter,
});
const { default: ChatsPage } = await import("./page");

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
const writes = () => calls.filter((call) => call.method !== "GET");

const NOW = "2026-11-01T09:30:00Z";

/** .NET UserDto for the person `?with=` names. */
const bea = {
  id: "user-b",
  email: "bea@example.org",
  emailVerified: null,
  name: "Bea Builder",
  image: null,
  bio: null,
  location: null,
  phone: null,
  website: null,
  linkedIn: null,
  twitter: null,
  role: "INDIVIDUAL",
  userType: "INNOVATOR",
  isVerified: false,
  totalXP: 0,
  level: 1,
  tier: "BRONZE",
  trustScore: 0,
  verificationLevel: "ANONYMOUS",
  createdAt: NOW,
  updatedAt: NOW,
};

/** .NET ConversationMemberDto. */
const member = (userId: string, userName: string) => ({
  id: `member-${userId}`,
  userId,
  userName,
  userImage: null,
  role: "MEMBER",
  lastReadAt: null,
  lastActiveAt: null,
  joinedAt: NOW,
  isOnline: false,
});

/** .NET ChatMessageDto. */
const messageDto = (
  id: string,
  from: string,
  fromName: string,
  text: string,
) => ({
  id,
  conversationId: "conv-ab",
  userFromId: from,
  userFromName: fromName,
  userFromImage: null,
  message: text,
  status: "SENT",
  replyToId: null,
  replyTo: null,
  sentAt: NOW,
  deliveredAt: null,
  seenAt: null,
  editedAt: null,
  isDeleted: false,
});

/** .NET ConversationDto: Ada and Bea's direct conversation. */
const directConversation = (lastMessage: unknown) => ({
  id: "conv-ab",
  type: "DIRECT",
  name: null,
  avatarUrl: null,
  createdAt: NOW,
  lastMessageAt: lastMessage ? NOW : null,
  members: [member("user-a", "Ada Joiner"), member("user-b", "Bea Builder")],
  lastMessage,
  unreadCount: 0,
});

/** The conversations the API holds for Ada; the first send can add one. */
let stored: {
  conversation: ReturnType<typeof directConversation> | null;
  messages: ReturnType<typeof messageDto>[];
};

function serve(overrides: Record<string, DotNetRoute> = {}) {
  routes = {
    "GET /api/chat/conversations": () =>
      ok({
        items: stored.conversation ? [stored.conversation] : [],
        totalCount: stored.conversation ? 1 : 0,
        hasMore: false,
      }),
    "GET /api/users/user-b": () => ok(bea),
    // ChatService.GetOrCreateDirectConversationAsync: the existing DIRECT conversation, else a new one.
    "POST /api/chat/conversations/direct": () => {
      stored.conversation ??= directConversation(null);
      return ok(stored.conversation);
    },
    "POST /api/chat/messages": ({ body }) => {
      const sent = messageDto(
        `message-${stored.messages.length + 1}`,
        "user-a",
        "Ada Joiner",
        String(body?.message),
      );
      stored.messages.push(sent);
      return ok(sent);
    },
    "GET /api/chat/conversations/conv-ab": () =>
      stored.conversation
        ? ok(stored.conversation)
        : { status: 404, body: { error: "Conversation not found" } },
    "GET /api/chat/conversations/conv-ab/messages": () =>
      ok({
        items: [...stored.messages].reverse(),
        totalCount: stored.messages.length,
        hasMore: false,
        nextCursor: null,
      }),
    "POST /api/chat/messages/read": () => ok({ success: true }),
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
        <ChatsPage />
      </api.Provider>
    </QueryClientProvider>,
  );
}

/** tRPC requests the page has started and not yet had answered (dependent reads start one render later). */
let inFlight = 0;

/** Waits until no request has been in flight for three ticks, so "nothing was written" is a fact, not a race. */
async function quiet() {
  for (let quietTicks = 0, ticks = 0; quietTicks < 3 && ticks < 400; ticks++) {
    await act(() => new Promise<void>((resolve) => setTimeout(resolve, 25)));
    quietTicks = inFlight === 0 ? quietTicks + 1 : 0;
  }
}

beforeEach(() => {
  stored = { conversation: null, messages: [] };
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
  "Chats: ?with= is a draft until the first send (SR-03 Phase 3 §2, §10)",
  { timeout: 20_000 },
  () => {
    it("opening the link creates nothing; the first send creates exactly one conversation and lands in it", async () => {
      renderPage();

      await screen.findByRole("heading", { name: "Bea Builder" });
      screen.getByText("Starts when you send");
      await quiet();
      expect(writes()).toEqual([]);

      fireEvent.change(screen.getByRole("textbox", { name: "Message" }), {
        target: { value: "I can take Saturday morning." },
      });
      await quiet();
      // A draft has no conversation to be typing in.
      expect(writes()).toEqual([]);

      fireEvent.click(screen.getByRole("button", { name: "Send message" }));

      await within(await screen.findByRole("log")).findByText(
        "I can take Saturday morning.",
      );
      expect(writes().map((call) => `${call.method} ${call.path}`)).toEqual([
        "POST /api/chat/conversations/direct",
        "POST /api/chat/messages",
      ]);
      expect(
        callsTo("POST", "/api/chat/conversations/direct")[0]?.body,
      ).toEqual({
        participantUserId: "user-b",
      });
      expect(callsTo("POST", "/api/chat/messages")[0]?.body).toMatchObject({
        conversationId: "conv-ab",
        message: "I can take Saturday morning.",
      });
      expect(screen.queryByText("Starts when you send")).toBeNull();
      expect(
        screen.getByRole<HTMLTextAreaElement>("textbox", { name: "Message" })
          .value,
      ).toBe("");
    });

    it("an existing direct conversation with that person opens instead of a draft", async () => {
      stored.messages = [
        messageDto("message-1", "user-b", "Bea Builder", "Framing Saturday?"),
      ];
      stored.conversation = directConversation(stored.messages[0]);
      renderPage();

      await within(await screen.findByRole("log")).findByText(
        "Framing Saturday?",
      );
      screen.getByText("Direct conversation");
      expect(screen.queryByText("Starts when you send")).toBeNull();
      await quiet();
      expect(callsTo("POST", "/api/chat/conversations/direct")).toHaveLength(0);
    });

    it("when the conversation cannot be started, nothing is sent and the words stay in the draft", async () => {
      serve({
        "POST /api/chat/conversations/direct": () => ({
          status: 503,
          body: { error: "Service unavailable" },
        }),
      });
      renderPage();

      await screen.findByRole("heading", { name: "Bea Builder" });
      fireEvent.change(screen.getByRole("textbox", { name: "Message" }), {
        target: { value: "I can take Saturday morning." },
      });
      fireEvent.click(screen.getByRole("button", { name: "Send message" }));

      await screen.findByText(
        "Your message was not sent. Check your connection and try again.",
      );
      expect(callsTo("POST", "/api/chat/messages")).toHaveLength(0);
      expect(
        screen.getByRole<HTMLTextAreaElement>("textbox", { name: "Message" })
          .value,
      ).toBe("I can take Saturday morning.");
      screen.getByText("Starts when you send");
    });
  },
);
