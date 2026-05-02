import { describe, it, expect, vi } from "vitest";
import { TRPCError } from "@trpc/server";
import {
  createRoleMiddleware,
  createUserTypeMiddleware,
  createVerificationLevelMiddleware,
  createAdminMiddleware,
} from "../rbac-middleware";

// ---------------------------------------------------------------------------
// Helpers to simulate tRPC middleware context
// ---------------------------------------------------------------------------

function createMockNext() {
  return vi.fn().mockResolvedValue({ ok: true });
}

interface MockUser {
  id: string;
  role?: string;
  userType?: string;
  verificationLevel?: string;
}

function createMockOpts(user: MockUser) {
  const next = createMockNext();
  return {
    ctx: {
      session: {
        user,
      },
    },
    next,
  };
}

// ===========================================================================
// createRoleMiddleware
// ===========================================================================

describe("createRoleMiddleware", () => {
  it("should call next() when user has an allowed role", async () => {
    const middleware = createRoleMiddleware(["ADMIN", "GUILD"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "ADMIN",
    });

    await middleware({ ctx, next });

    expect(next).toHaveBeenCalledOnce();
  });

  it("should throw FORBIDDEN when user role is not in allowed list", async () => {
    const middleware = createRoleMiddleware(["ADMIN"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "INDIVIDUAL",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
    await expect(middleware({ ctx, next })).rejects.toMatchObject({
      code: "FORBIDDEN",
    });
    expect(next).not.toHaveBeenCalled();
  });

  it("should throw FORBIDDEN when user role is undefined", async () => {
    const middleware = createRoleMiddleware(["ADMIN"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: undefined,
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
    expect(next).not.toHaveBeenCalled();
  });

  it("should include descriptive message in error", async () => {
    const middleware = createRoleMiddleware(["ADMIN", "GUILD"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "INDIVIDUAL",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(/role/i);
  });
});

// ===========================================================================
// createUserTypeMiddleware
// ===========================================================================

describe("createUserTypeMiddleware", () => {
  it("should call next() when user has an allowed type", async () => {
    const middleware = createUserTypeMiddleware(["FREELANCER", "INNOVATOR"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      userType: "FREELANCER",
    });

    await middleware({ ctx, next });

    expect(next).toHaveBeenCalledOnce();
  });

  it("should throw FORBIDDEN when user type is not in allowed list", async () => {
    const middleware = createUserTypeMiddleware(["FREELANCER"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      userType: "VOLUNTEER",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
    await expect(middleware({ ctx, next })).rejects.toMatchObject({
      code: "FORBIDDEN",
    });
  });

  it("should throw FORBIDDEN when user type is undefined", async () => {
    const middleware = createUserTypeMiddleware(["FREELANCER"]);
    const { ctx, next } = createMockOpts({
      id: "user-1",
      userType: undefined,
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
  });
});

// ===========================================================================
// createVerificationLevelMiddleware
// ===========================================================================

describe("createVerificationLevelMiddleware", () => {
  it("should call next() when user meets the minimum level", async () => {
    const middleware = createVerificationLevelMiddleware("VERIFIED");
    const { ctx, next } = createMockOpts({
      id: "user-1",
      verificationLevel: "EXPERT",
    });

    await middleware({ ctx, next });

    expect(next).toHaveBeenCalledOnce();
  });

  it("should call next() when user level equals the minimum", async () => {
    const middleware = createVerificationLevelMiddleware("PRO");
    const { ctx, next } = createMockOpts({
      id: "user-1",
      verificationLevel: "PRO",
    });

    await middleware({ ctx, next });

    expect(next).toHaveBeenCalledOnce();
  });

  it("should throw FORBIDDEN when user level is below minimum", async () => {
    const middleware = createVerificationLevelMiddleware("PRO");
    const { ctx, next } = createMockOpts({
      id: "user-1",
      verificationLevel: "ANONYMOUS",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
    await expect(middleware({ ctx, next })).rejects.toMatchObject({
      code: "FORBIDDEN",
    });
  });

  it("should throw FORBIDDEN when verification level is undefined", async () => {
    const middleware = createVerificationLevelMiddleware("VERIFIED");
    const { ctx, next } = createMockOpts({
      id: "user-1",
      verificationLevel: undefined,
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
  });
});

// ===========================================================================
// createAdminMiddleware
// ===========================================================================

describe("createAdminMiddleware", () => {
  it("should call next() when user is ADMIN", async () => {
    const middleware = createAdminMiddleware();
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "ADMIN",
    });

    await middleware({ ctx, next });

    expect(next).toHaveBeenCalledOnce();
  });

  it("should throw FORBIDDEN when user is INDIVIDUAL", async () => {
    const middleware = createAdminMiddleware();
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "INDIVIDUAL",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
    await expect(middleware({ ctx, next })).rejects.toMatchObject({
      code: "FORBIDDEN",
    });
  });

  it("should throw FORBIDDEN when user is GUILD", async () => {
    const middleware = createAdminMiddleware();
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: "GUILD",
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
  });

  it("should throw FORBIDDEN when role is undefined", async () => {
    const middleware = createAdminMiddleware();
    const { ctx, next } = createMockOpts({
      id: "user-1",
      role: undefined,
    });

    await expect(middleware({ ctx, next })).rejects.toThrow(TRPCError);
  });
});
