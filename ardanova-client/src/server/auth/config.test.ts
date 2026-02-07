import { describe, it, expect, vi, beforeEach } from "vitest";

/**
 * Tests for the NextAuth configuration callbacks.
 *
 * We mock external dependencies (next-auth, db, env) so we can test
 * the callback logic in isolation without a running Next.js server.
 */

// ---------------------------------------------------------------------------
// Mocks -- vi.hoisted ensures these are available when vi.mock factories run
// ---------------------------------------------------------------------------

const { mockFindUnique, mockCreate } = vi.hoisted(() => ({
  mockFindUnique: vi.fn(),
  mockCreate: vi.fn(),
}));

vi.mock("~/server/db", () => ({
  db: {
    user: {
      findUnique: mockFindUnique,
      create: mockCreate,
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

/** Shorthand for extracting callbacks from the config. */
const { jwt: jwtCallback, session: sessionCallback, signIn: signInCallback } =
  authConfig.callbacks;

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

  // -------------------------------------------------------------------------
  // JWT callback
  // -------------------------------------------------------------------------

  describe("jwt callback", () => {
    it("should populate token with all five required claims from DB user on first sign-in", async () => {
      mockFindUnique.mockResolvedValue(mockDbUser);

      const token = await jwtCallback({
        token: { email: "test@example.com" },
        user: { email: "test@example.com" },
        account: { provider: "google" } as any,
        profile: {
          email: "test@example.com",
          name: "Test User",
          picture: "https://example.com/avatar.jpg",
        } as any,
        trigger: "signIn",
      } as any);

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

      const token = await jwtCallback({
        token: { email: "test@example.com" },
        user: undefined,
        account: undefined,
        profile: undefined,
        trigger: "update",
      } as any);

      expect(token.id).toBe("user-123");
      expect(token.verificationLevel).toBe("VERIFIED");
    });

    it("should handle all VerificationLevel enum values", async () => {
      for (const level of ["ANONYMOUS", "VERIFIED", "PRO", "EXPERT"]) {
        mockFindUnique.mockResolvedValue({
          ...mockDbUser,
          verificationLevel: level,
        });

        const token = await jwtCallback({
          token: { email: "test@example.com" },
          user: { email: "test@example.com" },
          account: { provider: "google" } as any,
          profile: { email: "test@example.com" } as any,
          trigger: "signIn",
        } as any);

        expect(token.verificationLevel).toBe(level);
      }
    });

    it("should query DB by user email on first sign-in", async () => {
      mockFindUnique.mockResolvedValue(mockDbUser);

      await jwtCallback({
        token: { email: "test@example.com" },
        user: { email: "test@example.com" },
        account: { provider: "google" } as any,
        profile: { email: "test@example.com" } as any,
        trigger: "signIn",
      } as any);

      expect(mockFindUnique).toHaveBeenCalledWith({
        where: { email: "test@example.com" },
      });
    });

    it("should query DB by token email when user object is absent", async () => {
      mockFindUnique.mockResolvedValue(mockDbUser);

      await jwtCallback({
        token: { email: "returning@example.com" },
        user: undefined,
        account: undefined,
        profile: undefined,
        trigger: "update",
      } as any);

      expect(mockFindUnique).toHaveBeenCalledWith({
        where: { email: "returning@example.com" },
      });
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

      const mockSession = {
        user: { id: "", email: "", name: "", image: "" },
        expires: new Date().toISOString(),
      };

      const session = await sessionCallback({
        session: mockSession,
        token: mockToken,
      } as any);

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

        const mockSession = {
          user: { id: "", email: "", name: "", image: "" },
          expires: new Date().toISOString(),
        };

        const session = await sessionCallback({
          session: mockSession,
          token: mockToken,
        } as any);

        expect(session.user.verificationLevel).toBe(level);
      }
    });

    it("should not set custom claims when token has no id", async () => {
      const mockToken = {
        email: "test@example.com",
      };

      const mockSession = {
        user: { id: "", email: "", name: "", image: "" },
        expires: new Date().toISOString(),
      };

      const session = await sessionCallback({
        session: mockSession,
        token: mockToken,
      } as any);

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
    it("should return true for existing Google user", async () => {
      mockFindUnique.mockResolvedValue(mockDbUser);

      const result = await signInCallback({
        user: { email: "test@example.com" } as any,
        account: { provider: "google" } as any,
        profile: { email: "test@example.com", name: "Test" } as any,
        email: undefined,
        credentials: undefined,
      } as any);

      expect(result).toBe(true);
    });

    it("should create a new user when none exists and return true", async () => {
      mockFindUnique.mockResolvedValue(null);
      mockCreate.mockResolvedValue(mockDbUser);

      const result = await signInCallback({
        user: { email: "new@example.com" } as any,
        account: { provider: "google" } as any,
        profile: {
          email: "new@example.com",
          name: "New User",
          picture: "https://example.com/new.jpg",
        } as any,
        email: undefined,
        credentials: undefined,
      } as any);

      expect(result).toBe(true);
      expect(mockCreate).toHaveBeenCalledWith({
        data: expect.objectContaining({
          email: "new@example.com",
          name: "New User",
          role: "INDIVIDUAL",
          userType: "VOLUNTEER",
          isVerified: false,
        }),
      });
    });

    it("should return false on database error", async () => {
      mockFindUnique.mockRejectedValue(new Error("DB connection error"));

      const result = await signInCallback({
        user: { email: "test@example.com" } as any,
        account: { provider: "google" } as any,
        profile: { email: "test@example.com", name: "Test" } as any,
        email: undefined,
        credentials: undefined,
      } as any);

      expect(result).toBe(false);
    });

    it("should return true for non-Google providers without DB interaction", async () => {
      const result = await signInCallback({
        user: { email: "test@example.com" } as any,
        account: { provider: "credentials" } as any,
        profile: undefined,
        email: undefined,
        credentials: undefined,
      } as any);

      expect(result).toBe(true);
      expect(mockFindUnique).not.toHaveBeenCalled();
    });
  });
});
