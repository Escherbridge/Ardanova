import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, users } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  users: { search: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { users } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { chatRouter } from "./chat";

const unnamedByEmail = {
  id: "user-target",
  email: "target@example.org",
  emailVerified: "2026-01-01T00:00:00Z",
  phone: "+1 555 0101",
  role: "INDIVIDUAL",
  trustScore: 40,
  userType: "VOLUNTEER",
  verificationLevel: "VERIFIED",
};

describe("chat.searchUsers directory privacy", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    currentSession.mockResolvedValue({
      expires: "2099-01-01T00:00:00Z",
      user: { id: "user-searcher" },
    });
    users.search.mockResolvedValue({
      data: { items: [unnamedByEmail], totalCount: 1, hasNextPage: false },
    });
  });

  it("an exact-email match never echoes the address or authority fields", async () => {
    const caller = createCallerFactory(chatRouter)(
      await createTRPCContext({ headers: new Headers() }),
    );

    const [hit] = await caller.searchUsers({ query: "target@example.org" });

    expect(hit).toEqual({
      id: "user-target",
      name: "Unnamed member",
      image: undefined,
      isSelf: false,
    });
    expect(JSON.stringify(hit)).not.toContain("target@example.org");
  });
});
