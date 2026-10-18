import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, users } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  users: {
    getById: vi.fn(),
    getSkills: vi.fn(),
    getExperience: vi.fn(),
    update: vi.fn(),
    addSkill: vi.fn(),
    addExperience: vi.fn(),
  },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { users } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { profileRouter } from "./profile";

const victim = {
  id: "qa-auth-victim",
  email: "victim@example.org",
  emailVerified: "2026-01-01T00:00:00Z",
  name: "Victim",
  bio: "Builds things.",
  phone: "+1 555 0199",
  role: "INDIVIDUAL",
  userType: "VOLUNTEER",
  isVerified: false,
  totalXP: 0,
  level: 1,
  tier: "BRONZE",
  trustScore: 42,
  verificationLevel: "ANONYMOUS",
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

const privateFields = ["email", "emailVerified", "phone", "role", "trustScore"];

async function callerAs(userId: string | null) {
  currentSession.mockResolvedValue(
    userId ? { expires: "2099-01-01T00:00:00Z", user: { id: userId } } : null,
  );
  return createCallerFactory(profileRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("public profile reads", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    users.getById.mockResolvedValue({ data: victim });
    users.getSkills.mockResolvedValue({
      data: [{ id: "skill-1", userId: victim.id, skill: "Rust", level: 5 }],
    });
    users.getExperience.mockResolvedValue({ data: [] });
  });

  it.each([
    ["an anonymous visitor", null],
    ["another member", "user-visitor"],
  ])(
    "shows %s the profile without contact or authority fields",
    async (_viewer, viewer) => {
      const profile = await (
        await callerAs(viewer)
      ).getProfileById({ userId: victim.id });

      expect(profile).toMatchObject({
        id: victim.id,
        bio: "Builds things.",
        skills: [{ skill: "Rust", level: 5 }],
      });
      for (const field of privateFields)
        expect(profile).not.toHaveProperty(field);
    },
  );

  it("shows the owner their own contact fields", async () => {
    const profile = await (
      await callerAs(victim.id)
    ).getProfileById({ userId: victim.id });

    expect(profile).toMatchObject({
      email: "victim@example.org",
      phone: "+1 555 0199",
    });
  });

  it.each(["email/victim@example.org", "paged", "Search"])(
    "refuses %s, an id that would reach a different API route",
    async (userId) => {
      const caller = await callerAs(null);

      await expect(caller.getProfileById({ userId })).rejects.toMatchObject({
        code: "BAD_REQUEST",
      });
    },
  );
});

describe("profile edits", () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    [
      400,
      "You already list Rust as a skill.",
      "BAD_REQUEST",
      "You already list Rust as a skill.",
    ],
    [
      403,
      "Only the profile owner can change skills.",
      "FORBIDDEN",
      "Only the profile owner can change skills.",
    ],
    [
      500,
      "System.NullReferenceException at UserSkillService",
      "INTERNAL_SERVER_ERROR",
      "Failed to add skill",
    ],
  ])(
    "turns a backend %i into a tRPC error that keeps only safe reasons",
    async (status, backendError, code, message) => {
      users.addSkill.mockResolvedValue({ status, error: backendError });

      await expect(
        (await callerAs(victim.id)).addSkill({ skill: "Rust", level: 7 }),
      ).rejects.toMatchObject({ code, message });
    },
  );

  it.each([
    { website: "javascript:alert(document.cookie)" },
    { website: "data:text/html,<script>alert(1)</script>" },
    { linkedIn: "javascript:alert(1)" },
    { twitter: "  vbscript:msgbox(1)" },
  ])("refuses a non-http profile link %o", async (input) => {
    await expect(
      (await callerAs(victim.id)).updateProfile(input),
    ).rejects.toMatchObject({ code: "BAD_REQUEST" });
  });

  it("saves https links and bare handles", async () => {
    users.update.mockResolvedValue({
      status: 200,
      data: {
        ...victim,
        website: "https://victim.example",
        twitter: "@victim",
      },
    });

    const saved = await (
      await callerAs(victim.id)
    ).updateProfile({
      website: "https://victim.example",
      linkedIn: "victim-builds",
      twitter: "@victim",
    });

    expect(saved).toMatchObject({
      website: "https://victim.example",
      twitter: "@victim",
    });
  });

  it("refuses experience that ends before it starts", async () => {
    await expect(
      (await callerAs(victim.id)).addExperience({
        title: "Engineer",
        company: "Acme",
        startDate: new Date("2024-05-01"),
        endDate: new Date("2024-01-01"),
      }),
    ).rejects.toMatchObject({ code: "BAD_REQUEST" });
  });
});
