import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import type { Session } from "next-auth";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const sessions = vi.hoisted(() => ({
  page: null as Session | null,
  cookie: null as Session | null,
}));

vi.mock("~/server/auth", () => ({
  auth: async () => sessions.cookie,
  authForPage: async () => sessions.page,
}));

const {
  createTRPCContext,
  createTRPCRouter,
  protectedProcedure,
  publicProcedure,
} = await import("./trpc");
const { isActorAssertionActive } = await import("~/server/actor-assertion");
const { apiPath } = await import("~/lib/api/path");
const { BaseApiClient } = await import("~/lib/api/base-client");

const backend = new BaseApiClient({ baseUrl: "https://api.example.test" });

/** The router idiom used across the BFF: rethrow the API's message as a plain Error. */
async function rethrowingRead(fallback?: string) {
  const response = await backend.get<{ id: string }>(
    "/api/Referrals/referral-1",
  );
  if (!response.data) throw new Error(fallback ?? response.error);
  return response.data;
}

const router = createTRPCRouter({
  apiRead: publicProcedure.query(() => rethrowingRead()),
  apiReadWithOwnMessage: publicProcedure.query(() =>
    rethrowingRead("Failed to fetch referral"),
  ),
  whoAmI: protectedProcedure.query(({ ctx }) => ({ id: ctx.session.user.id })),
  publicRead: publicProcedure.query(() => ({
    signedAsActor: isActorAssertionActive(),
  })),
  traversalId: publicProcedure.query(() => apiPath`/api/users/${".."}/skills`),
});

async function callWhoAmI(path = "whoAmI") {
  const response = await fetchRequestHandler({
    endpoint: "/api/trpc",
    req: new Request(`http://localhost:3000/api/trpc/${path}`),
    router,
    createContext: ({ req }) => createTRPCContext({ headers: req.headers }),
  });
  return { status: response.status, body: await response.text() };
}

const previewSession: Session = {
  expires: new Date(Date.now() + 60_000).toISOString(),
  user: {
    id: "local-preview-user",
    name: "Local Preview",
    email: "preview@local.ardanova.test",
    image: null,
    role: "INDIVIDUAL",
    userType: "VOLUNTEER",
    isVerified: false,
    verificationLevel: "ANONYMOUS",
  },
} as Session;

describe("tRPC request context", () => {
  beforeEach(() => {
    sessions.page = null;
    sessions.cookie = null;
    vi.unstubAllGlobals();
  });

  it("serves protected procedures to the seeded preview identity that auth() returns", async () => {
    sessions.cookie = previewSession;
    sessions.page = previewSession;

    const { status, body } = await callWhoAmI();

    expect(status).toBe(200);
    expect(body).toContain("local-preview-user");
  });

  it("refuses the page-only synthetic preview, which never authorizes data", async () => {
    sessions.page = previewSession;

    const { status } = await callWhoAmI();

    expect(status).toBe(401);
  });

  it("rejects without a session and never ships the server stack trace", async () => {
    const { status, body } = await callWhoAmI();

    expect(status).toBe(401);
    expect(body).toContain("UNAUTHORIZED");
    expect(body).not.toContain('"stack"');
    expect(body).not.toContain("trpc.ts");
  });

  it("signs public reads as the signed-in user so .NET can return their own private fields", async () => {
    sessions.cookie = previewSession;

    const { status, body } = await callWhoAmI("publicRead");

    expect(status).toBe(200);
    expect(body).toContain('"signedAsActor":true');
  });

  it("leaves anonymous public reads unsigned", async () => {
    const { status, body } = await callWhoAmI("publicRead");

    expect(status).toBe(200);
    expect(body).toContain('"signedAsActor":false');
  });

  it("never signs public reads for the page-only synthetic preview", async () => {
    sessions.page = previewSession;

    const { status, body } = await callWhoAmI("publicRead");

    expect(status).toBe(200);
    expect(body).toContain('"signedAsActor":false');
  });

  it.each([
    [
      403,
      "You do not have permission to perform this action.",
      403,
      "FORBIDDEN",
    ],
    [404, "The requested resource was not found.", 404, "NOT_FOUND"],
    [429, "Too many requests. Try again shortly.", 429, "TOO_MANY_REQUESTS"],
    [
      500,
      "An error occurred processing your request.",
      500,
      "INTERNAL_SERVER_ERROR",
    ],
  ])(
    "keeps the API's %i when a router rethrows its message",
    async (apiStatus, message, expectedStatus, expectedCode) => {
      vi.stubGlobal(
        "fetch",
        vi.fn().mockResolvedValue(
          new Response(
            JSON.stringify({ error: message, statusCode: apiStatus }),
            {
              status: apiStatus,
            },
          ),
        ),
      );

      const { status, body } = await callWhoAmI("apiRead");

      expect(status).toBe(expectedStatus);
      expect(body).toContain(expectedCode);
    },
  );

  it("does not guess a status for a router that replaced the API's message", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          new Response(
            JSON.stringify({ error: "Not found", statusCode: 404 }),
            { status: 404 },
          ),
        ),
    );

    const { status } = await callWhoAmI("apiReadWithOwnMessage");

    expect(status).toBe(500);
  });

  it("answers an id the API client refuses to path as a 400, not a server fault", async () => {
    const { status, body } = await callWhoAmI("traversalId");

    expect(status).toBe(400);
    expect(body).toContain("BAD_REQUEST");
  });
});
