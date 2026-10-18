import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, nova } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  nova: { status: vi.fn(), refineProject: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { nova } }));

import { TRPCError } from "@trpc/server";
import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { NOVA_STATE_MAX_CHARS } from "~/lib/nova-state-limits";
import { novaRouter } from "./nova";

const start = {
  idea: "A tool library run by a neighbourhood guild, lending power tools.",
  turns: [],
};

async function caller() {
  return createCallerFactory(novaRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("nova router", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    currentSession.mockResolvedValue({
      expires: "2099-01-01T00:00:00Z",
      user: { id: "member-1" },
    });
  });

  it.each([
    [429, "nova_daily_limit", "TOO_MANY_REQUESTS"],
    [503, "nova_capacity", "SERVICE_UNAVAILABLE"],
    [503, "nova_unavailable", "INTERNAL_SERVER_ERROR"],
    [404, "nova_session_not_found", "NOT_FOUND"],
    [408, "Request timeout", "TIMEOUT"],
  ])(
    "a .NET %i %s reaches the panel as %s, so its copy never blames the wrong party",
    async (status, error, code) => {
      nova.refineProject.mockResolvedValue({ status, error });

      const failure = await (await caller())
        .refineProject(start)
        .catch((cause: unknown) => cause);

      expect(failure).toBeInstanceOf(TRPCError);
      expect((failure as TRPCError).code).toBe(code);
    },
  );

  it.each([
    [NOVA_STATE_MAX_CHARS, true],
    [NOVA_STATE_MAX_CHARS + 1, false],
  ])(
    "a continuation state of %i characters (ticket envelope + upstream max is the cap) reaches .NET: %s",
    async (length, forwarded) => {
      nova.refineProject.mockResolvedValue({
        status: 200,
        data: { type: "question" },
      });
      const continuation = {
        ...start,
        turns: [
          {
            question: "Who has this problem?",
            answer: "Renters on our street.",
          },
        ],
        state: `nv1.1792000000.${"A".repeat(64)}.`.padEnd(length, "s"),
        sessionId: "session-1",
      };

      const outcome = await (await caller())
        .refineProject(continuation)
        .catch((cause: unknown) => cause);

      expect(nova.refineProject).toHaveBeenCalledTimes(forwarded ? 1 : 0);
      if (!forwarded) expect((outcome as TRPCError).code).toBe("BAD_REQUEST");
    },
  );

  it("reports availability as unknown, not off, when .NET is unreachable", async () => {
    nova.status.mockResolvedValue({ status: 500, error: "HTTP 500" });

    expect(await (await caller()).status()).toBeNull();
  });
});
