import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, users } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  users: {
    getAll: vi.fn(),
    search: vi.fn(),
    getById: vi.fn(),
    getFollowing: vi.fn(),
  },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { users } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { userRouter } from "./user";

const steward = {
  id: "user-steward",
  email: "steward@example.org",
  emailVerified: "2026-01-01T00:00:00Z",
  name: "Steward",
  phone: "+1 555 0100",
  role: "ADMIN",
  userType: "VOLUNTEER",
  isVerified: true,
  totalXP: 10,
  level: 2,
  tier: "BRONZE",
  trustScore: 87,
  verificationLevel: "VERIFIED",
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

const privateFields = ["email", "emailVerified", "phone", "role", "trustScore"];

async function callerAs(userId: string | null) {
  currentSession.mockResolvedValue(
    userId ? { expires: "2099-01-01T00:00:00Z", user: { id: userId } } : null,
  );
  return createCallerFactory(userRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("user directory privacy", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    const page = { items: [steward], totalCount: 1, hasNextPage: false };
    users.getAll.mockResolvedValue({ data: page });
    users.search.mockResolvedValue({ data: page });
    users.getById.mockResolvedValue({ data: steward });
    users.getFollowing.mockResolvedValue({
      data: [{ followingId: steward.id }],
    });
  });

  it.each([
    ["an anonymous browse", null, (c: Awaited<ReturnType<typeof callerAs>>) => c.getAll({}).then((r) => r.items[0])],
    ["an anonymous search", null, (c: Awaited<ReturnType<typeof callerAs>>) => c.search({ query: "stew" }).then((r) => r.items[0])],
    ["an anonymous profile read", null, (c: Awaited<ReturnType<typeof callerAs>>) => c.getById({ id: steward.id })],
    ["another member's profile read", "user-visitor", (c: Awaited<ReturnType<typeof callerAs>>) => c.getById({ id: steward.id })],
    ["a following list", "user-visitor", (c: Awaited<ReturnType<typeof callerAs>>) => c.getFollowingWithUsers({ userId: "user-visitor" }).then((r) => r[0])],
  ])("%s never reveals contact or authority fields", async (_flow, viewer, read) => {
    const user = await read(await callerAs(viewer));

    expect(user).toMatchObject({ id: steward.id, name: "Steward", verificationLevel: "VERIFIED" });
    for (const field of privateFields) expect(user).not.toHaveProperty(field);
  });

  it("returns the full record when a member reads their own profile", async () => {
    const caller = await callerAs(steward.id);

    await expect(caller.getById({ id: steward.id })).resolves.toMatchObject({
      email: "steward@example.org",
      phone: "+1 555 0100",
    });
  });
});

describe("user ids reaching the API path", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    users.getById.mockResolvedValue({
      data: { items: [steward], totalCount: 1, hasNextPage: false },
    });
  });

  it.each([
    ["a list route", "paged?page=1&pageSize=3"],
    ["a parent-directory hop", "../users/paged?pageSize=1&x="],
    ["an email lookup", "email/steward@example.org"],
    ["an encoded slash", "user%2Fpaged"],
  ])("rejects %s before any backend call", async (_shape, id) => {
    const caller = await callerAs(null);

    await expect(caller.getById({ id })).rejects.toMatchObject({ code: "BAD_REQUEST" });
    await expect(caller.getFollowCounts({ userId: id })).rejects.toMatchObject({
      code: "BAD_REQUEST",
    });
    expect(users.getById).not.toHaveBeenCalled();
  });

  it("refuses a backend answer that is not the user that was asked for", async () => {
    const caller = await callerAs(null);

    await expect(caller.getById({ id: steward.id })).rejects.toMatchObject({
      code: "NOT_FOUND",
    });
  });
});
