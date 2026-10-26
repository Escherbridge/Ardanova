import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TRPCError } from "@trpc/server";
import type { NextRequest } from "next/server";

vi.mock("server-only", () => ({}));

const { providerSession, findPreviewUser, providerGet, requestCookie } =
  vi.hoisted(() => ({
    providerSession: vi.fn<() => Promise<unknown>>(),
    findPreviewUser: vi.fn<() => Promise<unknown>>(),
    providerGet: vi.fn<() => Promise<Response>>(),
    requestCookie: vi.fn<() => string | undefined>(),
  }));

vi.mock("next/headers", () => ({
  cookies: async () => ({
    get: (name: string) =>
      name === "ardanova-dev-persona" && requestCookie() !== undefined
        ? { name, value: requestCookie()! }
        : undefined,
  }),
}));

vi.mock("next-auth", () => ({
  default: () => ({
    auth: providerSession,
    handlers: { GET: providerGet, POST: vi.fn() },
    signIn: vi.fn(),
    signOut: vi.fn(),
  }),
}));
vi.mock("./config", () => ({ authConfig: {} }));
vi.mock("~/server/db", () => ({
  db: { user: { findUnique: findPreviewUser } },
}));

const seededPreviewRow = {
  id: "local-preview-user",
  email: "preview@local.ardanova.test",
  name: "ArdaNova Preview Steward",
  image: null,
  role: "INDIVIDUAL",
  userType: "VOLUNTEER",
  isVerified: true,
  verificationLevel: "VERIFIED",
};

/** Builds a caller over the real tRPC context and protectedProcedure with fresh env guards. */
async function loadWorkspace() {
  vi.resetModules();
  const trpc = await import("~/server/api/trpc");
  const { isActorAssertionActive } = await import("~/server/actor-assertion");
  const auth = await import("./index");
  const router = trpc.createTRPCRouter({
    whoAmI: trpc.protectedProcedure.query(({ ctx }) => ({
      id: ctx.session.user.id,
      name: ctx.session.user.name,
      verificationLevel: ctx.session.user.verificationLevel,
      signsActorAssertions: isActorAssertionActive(),
    })),
  });
  const caller = trpc.createCallerFactory(router)(
    await trpc.createTRPCContext({ headers: new Headers() }),
  );
  return { caller, auth };
}

function sessionRequest(): NextRequest {
  const url = new URL("http://localhost:3000/api/auth/session");
  return { nextUrl: url, url: url.toString() } as unknown as NextRequest;
}

describe("development preview identity", () => {
  beforeEach(() => {
    vi.stubEnv("NODE_ENV", "development");
    vi.stubEnv("DEV_AUTH_BYPASS", "true");
    vi.stubEnv("RAILWAY_ENVIRONMENT_ID", "");
    vi.stubEnv("RAILWAY_PROJECT_ID", "");
    vi.stubEnv("VERCEL_ENV", "");
    providerSession.mockResolvedValue(null);
    findPreviewUser.mockResolvedValue(seededPreviewRow);
    providerGet.mockResolvedValue(Response.json(null));
    requestCookie.mockReturnValue(undefined);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it("lets a protected procedure act as the seeded preview user with its database claims", async () => {
    const { caller } = await loadWorkspace();

    await expect(caller.whoAmI()).resolves.toEqual({
      id: "local-preview-user",
      name: "ArdaNova Preview Steward",
      verificationLevel: "VERIFIED",
      signsActorAssertions: true,
    });
  });

  it("reports the same preview identity from the client session endpoint", async () => {
    const { auth } = await loadWorkspace();

    const response = await auth.handlers.GET(sessionRequest());

    await expect(response.json()).resolves.toMatchObject({
      user: { id: "local-preview-user", verificationLevel: "VERIFIED" },
    });
  });

  it("prefers a real provider session over the preview", async () => {
    providerSession.mockResolvedValue({
      expires: "2099-01-01T00:00:00.000Z",
      user: { id: "google-user", name: "Real Person" },
    });
    const { caller } = await loadWorkspace();

    await expect(caller.whoAmI()).resolves.toMatchObject({ id: "google-user" });
  });

  it("acts as a seeded demo persona when the persona cookie selects one", async () => {
    requestCookie.mockReturnValue("demo-sponsor");
    findPreviewUser.mockResolvedValue({
      ...seededPreviewRow,
      id: "demo-sponsor",
      email: "demo-sponsor@local.ardanova.test",
      name: "Demo Sponsor",
    });
    const { caller } = await loadWorkspace();

    await expect(caller.whoAmI()).resolves.toMatchObject({
      id: "demo-sponsor",
      name: "Demo Sponsor",
    });
  });

  it.each([
    ["the database has no seeded preview row", () => findPreviewUser.mockResolvedValue(null)],
    [
      "the persona cookie names a row outside the seed email namespace",
      () => {
        requestCookie.mockReturnValue("some-real-user");
        findPreviewUser.mockResolvedValue({
          ...seededPreviewRow,
          id: "some-real-user",
          email: "person@gmail.com",
        });
      },
    ],
    [
      "the persona cookie is malformed",
      () => requestCookie.mockReturnValue("nope nope/../etc"),
    ],
    [
      "the preview id belongs to a different email",
      () =>
        findPreviewUser.mockResolvedValue({
          ...seededPreviewRow,
          email: "someone@example.com",
        }),
    ],
    ["the runtime is production", () => vi.stubEnv("NODE_ENV", "production")],
    ["the runtime is hosted on Railway", () => vi.stubEnv("RAILWAY_PROJECT_ID", "prj")],
    ["the bypass flag is off", () => vi.stubEnv("DEV_AUTH_BYPASS", "false")],
  ])("denies data access when %s", async (_case, arrange) => {
    arrange();
    const { caller } = await loadWorkspace();

    await expect(caller.whoAmI()).rejects.toMatchObject(
      new TRPCError({ code: "UNAUTHORIZED" }),
    );
  });

  it("still renders an unseeded page shell without granting data access", async () => {
    findPreviewUser.mockResolvedValue(null);
    const { auth } = await loadWorkspace();

    await expect(auth.auth()).resolves.toBeNull();
    await expect(auth.authForPage()).resolves.toMatchObject({
      user: { id: "local-preview-user", verificationLevel: "ANONYMOUS" },
    });
  });

  it("keeps the sign-in page reachable during the preview", async () => {
    const { auth } = await loadWorkspace();

    await expect(auth.providerAuth()).resolves.toBeNull();
  });
});
