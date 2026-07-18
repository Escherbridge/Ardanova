import { describe, it, expect, vi, beforeEach } from "vitest";
import type { Prisma } from "@prisma/client";

/**
 * Tests for the NextAuth configuration callbacks.
 *
 * We mock external dependencies (next-auth, db, env) so we can test
 * the callback logic in isolation without a running Next.js server.
 */

// ---------------------------------------------------------------------------
// Mocks -- vi.hoisted ensures these are available when vi.mock factories run
// ---------------------------------------------------------------------------

const { mockFindUnique, mockCreate, mockAccountFindUnique } = vi.hoisted(
  () => ({
    mockFindUnique:
      vi.fn<(args: Prisma.UserFindUniqueArgs) => Promise<unknown>>(),
    mockCreate: vi.fn<(args: Prisma.UserCreateArgs) => Promise<unknown>>(),
    mockAccountFindUnique:
      vi.fn<(args: Prisma.AccountFindUniqueArgs) => Promise<unknown>>(),
  }),
);

vi.mock("~/server/db", () => ({
  db: {
    user: {
      findUnique: mockFindUnique,
      create: mockCreate,
    },
    account: {
      findUnique: mockAccountFindUnique,
    },
  },
}));

vi.mock("~/env", () => ({
  env: {
    GOOGLE_CLIENT_ID: "test-google-client-id",
    GOOGLE_CLIENT_SECRET: "test-google-client-secret",
  },
}));

// Mock next-auth so the module can be imported without a Next.js runtime
vi.mock("next-auth", () => ({
  default: vi.fn(),
}));

// Mock GoogleProvider -- return a simple object with id
vi.mock("next-auth/providers/google", () => ({
  default: vi.fn((opts: Record<string, unknown>) => ({
    id: "google",
    name: "Google",
    type: "oidc",
    ...opts,
  })),
}));

import { authConfig } from "./config";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** A mock DB user with all the fields the callbacks should read. */
const mockDbUser = {
  id: "user-123",
  email: "test@example.com",
  name: "Test User",
  image: "https://example.com/avatar.jpg",
  role: "INDIVIDUAL",
  userType: "VOLUNTEER",
  isVerified: false,
  verificationLevel: "ANONYMOUS",
};

type JwtCallbackParams = Parameters<typeof authConfig.callbacks.jwt>[0];
type SessionCallbackParams = Parameters<typeof authConfig.callbacks.session>[0];
type SignInCallbackParams = Parameters<typeof authConfig.callbacks.signIn>[0];

const jwtCallback = (params: JwtCallbackParams) =>
  authConfig.callbacks.jwt(params);
const sessionCallback = (params: SessionCallbackParams) =>
  authConfig.callbacks.session(params);
const signInCallback = (params: SignInCallbackParams) =>
  authConfig.callbacks.signIn(params);

const googleAccount = {
  provider: "google",
  providerAccountId: "google-user-123",
  type: "oidc",
} satisfies NonNullable<JwtCallbackParams["account"]>;

const credentialsAccount = {
  provider: "credentials",
  providerAccountId: "credentials-user-123",
  type: "credentials",
} satisfies NonNullable<SignInCallbackParams["account"]>;

function googleProfile(
  email: string,
  overrides: Partial<NonNullable<JwtCallbackParams["profile"]>> = {},
): NonNullable<JwtCallbackParams["profile"]> {
  return {
    email,
    email_verified: true,
    sub: googleAccount.providerAccountId,
    ...overrides,
  };
}

function jwtParams(
  overrides: Omit<Partial<JwtCallbackParams>, "token"> & {
    token?: JwtCallbackParams["token"];
  },
): JwtCallbackParams {
  return { ...overrides, token: overrides.token ?? {} };
}

function sessionParams(
  token: SessionCallbackParams["token"],
): SessionCallbackParams {
  return {
    session: {
      user: { id: "", email: "", name: "", image: "" },
      expires: new Date().toISOString(),
    },
    token,
  };
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("authConfig", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // -------------------------------------------------------------------------
  // Structure
  // -------------------------------------------------------------------------

  describe("providers", () => {
    it("should include Google as the only provider", () => {
      expect(authConfig.providers).toHaveLength(1);
      const provider = authConfig.providers[0] as { id?: string };
      expect(provider.id).toBe("google");
    });
  });

  describe("session strategy", () => {
    it("should use JWT strategy", () => {
      expect(authConfig.session.strategy).toBe("jwt");
    });
  });

  describe("custom pages", () => {
    it("should route sign-in and errors through the branded pages", () => {
      expect(authConfig.pages).toEqual({
        signIn: "/auth/signin",
        error: "/auth/error",
      });
    });
  });

  // -------------------------------------------------------------------------
  // JWT callback
  // -------------------------------------------------------------------------

  describe("jwt callback", () => {
    it("should populate token with all five required claims from DB user on first sign-in", async () => {
      mockAccountFindUnique.mockResolvedValue({ user: mockDbUser });

      const token = await jwtCallback(
        jwtParams({
          token: { email: "test@example.com" },
          user: { email: "test@example.com" },
          account: googleAccount,
          profile: googleProfile("test@example.com", {
            name: "Test User",
            picture: "https://example.com/avatar.jpg",
          }),
          trigger: "signIn",
        }),
      );

      // All five required claims: userId (as id), email, role, userType, verificationLevel
      expect(token.id).toBe("user-123");
      expect(token.email).toBe("test@example.com");
      expect(token.role).toBe("INDIVIDUAL");
      expect(token.userType).toBe("VOLUNTEER");
      expect(token.isVerified).toBe(false);
      expect(token.verificationLevel).toBe("ANONYMOUS");
    });

    it("should populate verificationLevel on subsequent requests (token refresh)", async () => {
      mockFindUnique.mockResolvedValue({
        ...mockDbUser,
        verificationLevel: "VERIFIED",
      });

      const token = await jwtCallback(
        jwtParams({
          token: { id: "user-123", email: "test@example.com" },
          trigger: "update",
        }),
      );

      expect(token.id).toBe("user-123");
      expect(token.verificationLevel).toBe("VERIFIED");
    });

    it("should handle all VerificationLevel enum values", async () => {
      for (const level of ["ANONYMOUS", "VERIFIED", "PRO", "EXPERT"]) {
        mockAccountFindUnique.mockResolvedValue({
          user: { ...mockDbUser, verificationLevel: level },
        });

        const token = await jwtCallback(
          jwtParams({
            token: { email: "test@example.com" },
            user: { email: "test@example.com" },
            account: googleAccount,
            profile: googleProfile("test@example.com"),
            trigger: "signIn",
          }),
        );

        expect(token.verificationLevel).toBe(level);
      }
    });

    it("should query the immutable provider account on first sign-in", async () => {
      mockAccountFindUnique.mockResolvedValue({ user: mockDbUser });

      await jwtCallback(
        jwtParams({
          token: { email: "test@example.com" },
          user: { email: "test@example.com" },
          account: googleAccount,
          profile: googleProfile("test@example.com"),
          trigger: "signIn",
        }),
      );

      expect(mockAccountFindUnique).toHaveBeenCalledWith(
        expect.objectContaining({
          where: {
            provider_providerAccountId: {
              provider: "google",
              providerAccountId: "google-user-123",
            },
          },
        }),
      );
      expect(mockFindUnique).not.toHaveBeenCalled();
    });

    it("should query DB by the authorized token user id on refresh", async () => {
      mockFindUnique.mockResolvedValue(mockDbUser);

      await jwtCallback(
        jwtParams({
          token: { id: "user-123", email: "returning@example.com" },
          trigger: "update",
        }),
      );

      const query = mockFindUnique.mock.calls.at(0)?.[0];
      expect(query?.where).toEqual({ id: "user-123" });
      expect(query?.select).toMatchObject({ id: true, email: true });
    });

    it("should remove stale authorization claims when the user no longer exists", async () => {
      mockFindUnique.mockResolvedValue(null);

      const token = await jwtCallback(
        jwtParams({
          token: {
            email: "removed@example.com",
            id: "removed-user",
            role: "ADMIN",
            userType: "VOLUNTEER",
            isVerified: true,
            verificationLevel: "EXPERT",
          },
          trigger: "update",
        }),
      );

      expect(token.id).toBeUndefined();
      expect(token.role).toBeUndefined();
      expect(token.userType).toBeUndefined();
      expect(token.isVerified).toBeUndefined();
      expect(token.verificationLevel).toBeUndefined();
    });

    it("should remove stale authorization claims when the database is unavailable", async () => {
      mockFindUnique.mockRejectedValue(new Error("DB unavailable"));
      const warn = vi
        .spyOn(console, "warn")
        .mockImplementation(() => undefined);

      const token = await jwtCallback(
        jwtParams({
          token: {
            email: "returning@example.com",
            id: "stale-user",
            role: "ADMIN",
            userType: "VOLUNTEER",
            isVerified: true,
            verificationLevel: "EXPERT",
          },
          trigger: "update",
        }),
      );

      expect(token.id).toBeUndefined();
      expect(token.role).toBeUndefined();
      expect(token.verificationLevel).toBeUndefined();
      warn.mockRestore();
    });
  });

  // -------------------------------------------------------------------------
  // Session callback
  // -------------------------------------------------------------------------

  describe("session callback", () => {
    it("should propagate all required claims from token to session", async () => {
      const mockToken = {
        id: "user-123",
        email: "test@example.com",
        name: "Test User",
        picture: "https://example.com/avatar.jpg",
        role: "INDIVIDUAL",
        userType: "VOLUNTEER",
        isVerified: false,
        verificationLevel: "ANONYMOUS",
      };

      const session = await sessionCallback(sessionParams(mockToken));

      // Verify all required claims are in the session
      expect(session.user.id).toBe("user-123");
      expect(session.user.email).toBe("test@example.com");
      expect(session.user.name).toBe("Test User");
      expect(session.user.image).toBe("https://example.com/avatar.jpg");
      expect(session.user.role).toBe("INDIVIDUAL");
      expect(session.user.userType).toBe("VOLUNTEER");
      expect(session.user.isVerified).toBe(false);
      expect(session.user.verificationLevel).toBe("ANONYMOUS");
    });

    it("should propagate verificationLevel for all enum values", async () => {
      for (const level of ["ANONYMOUS", "VERIFIED", "PRO", "EXPERT"]) {
        const mockToken = {
          id: "user-123",
          email: "test@example.com",
          role: "INDIVIDUAL",
          userType: "VOLUNTEER",
          isVerified: true,
          verificationLevel: level,
        };

        const session = await sessionCallback(sessionParams(mockToken));

        expect(session.user.verificationLevel).toBe(level);
      }
    });

    it("should not set custom claims when token has no id", async () => {
      const mockToken = {
        email: "test@example.com",
      };

      const session = await sessionCallback(sessionParams(mockToken));

      // id should remain empty since token.id was not set
      expect(session.user.id).toBe("");
      // email should still be propagated from token.email
      expect(session.user.email).toBe("test@example.com");
    });
  });

  // -------------------------------------------------------------------------
  // SignIn callback
  // -------------------------------------------------------------------------

  describe("signIn callback", () => {
    it("should return true for an existing linked Google identity", async () => {
      mockAccountFindUnique.mockResolvedValue({ user: mockDbUser });

      const result = await signInCallback({
        user: { email: "test@example.com" },
        account: googleAccount,
        profile: googleProfile("test@example.com", { name: "Test" }),
      });

      expect(result).toBe(true);
      expect(mockFindUnique).not.toHaveBeenCalled();
    });

    it("should atomically create a new user and immutable provider link", async () => {
      mockAccountFindUnique.mockResolvedValue(null);
      mockFindUnique.mockResolvedValue(null);
      mockCreate.mockResolvedValue(mockDbUser);

      const result = await signInCallback({
        user: { email: "new@example.com" },
        account: googleAccount,
        profile: googleProfile("new@example.com", {
          name: "New User",
          picture: "https://example.com/new.jpg",
        }),
      });

      expect(result).toBe(true);
      const createArgs = mockCreate.mock.calls.at(0)?.[0];
      expect(createArgs?.data).toMatchObject({
        email: "new@example.com",
        name: "New User",
        role: "INDIVIDUAL",
        userType: "VOLUNTEER",
        isVerified: false,
        accounts: {
          create: {
            type: "oidc",
            provider: "google",
            providerAccountId: "google-user-123",
          },
        },
      });
      expect(createArgs?.data.emailVerified).toBeInstanceOf(Date);
    });

    it("should deny implicit linking when a local email is already owned", async () => {
      mockAccountFindUnique.mockResolvedValue(null);
      mockFindUnique.mockResolvedValue({ id: "legacy-user" });
      const error = vi
        .spyOn(console, "error")
        .mockImplementation(() => undefined);

      const result = await signInCallback({
        user: { email: "test@example.com" },
        account: googleAccount,
        profile: googleProfile("test@example.com"),
      });

      expect(result).toBe(false);
      expect(mockCreate).not.toHaveBeenCalled();
      error.mockRestore();
    });

    it("should return false on database error", async () => {
      mockAccountFindUnique.mockRejectedValue(new Error("DB connection error"));

      const result = await signInCallback({
        user: { email: "test@example.com" },
        account: googleAccount,
        profile: googleProfile("test@example.com", { name: "Test" }),
      });

      expect(result).toBe(false);
    });

    it("should deny a Google profile without a verified email claim", async () => {
      const error = vi
        .spyOn(console, "error")
        .mockImplementation(() => undefined);

      const result = await signInCallback({
        user: { email: null },
        account: googleAccount,
        profile: googleProfile("test@example.com", { email_verified: false }),
      });

      expect(result).toBe(false);
      expect(mockFindUnique).not.toHaveBeenCalled();
      expect(mockAccountFindUnique).not.toHaveBeenCalled();
      error.mockRestore();
    });

    it("should deny a mismatched Google subject", async () => {
      const error = vi
        .spyOn(console, "error")
        .mockImplementation(() => undefined);

      const result = await signInCallback({
        user: { email: "test@example.com" },
        account: googleAccount,
        profile: googleProfile("test@example.com", { sub: "other-subject" }),
      });

      expect(result).toBe(false);
      expect(mockAccountFindUnique).not.toHaveBeenCalled();
      error.mockRestore();
    });

    it("should deny unexpected providers without DB interaction", async () => {
      const error = vi
        .spyOn(console, "error")
        .mockImplementation(() => undefined);
      const result = await signInCallback({
        user: { email: "test@example.com" },
        account: credentialsAccount,
      });

      expect(result).toBe(false);
      expect(mockFindUnique).not.toHaveBeenCalled();
      expect(mockAccountFindUnique).not.toHaveBeenCalled();
      error.mockRestore();
    });
  });
});
