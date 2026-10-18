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

const { createTRPCContext, createTRPCRouter, protectedProcedure } =
  await import("./trpc");

const router = createTRPCRouter({
  whoAmI: protectedProcedure.query(({ ctx }) => ({ id: ctx.session.user.id })),
});

async function callWhoAmI() {
  const response = await fetchRequestHandler({
    endpoint: "/api/trpc",
    req: new Request("http://localhost:3000/api/trpc/whoAmI"),
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
});
