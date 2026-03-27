/**
 * tRPC-compatible RBAC middleware factories.
 *
 * These factory functions return middleware-shaped async functions that can be
 * used both standalone (in tests, via direct invocation) and composed into
 * tRPC procedure builders via `.use()`.
 *
 * They read `ctx.session.user` and throw `TRPCError` with code `FORBIDDEN`
 * when the user does not satisfy the requirement.
 */

import { TRPCError } from "@trpc/server";
import {
  type UserRole,
  type UserType,
  type VerificationLevel,
  checkRole,
  checkUserType,
  checkVerificationLevel,
} from "./rbac";

// ---------------------------------------------------------------------------
// Shared context shape expected by the middleware
// ---------------------------------------------------------------------------

interface RBACMiddlewareContext {
  ctx: {
    session: {
      user: {
        id: string;
        role?: string;
        userType?: string;
        verificationLevel?: string;
      };
    };
  };
  next: () => Promise<unknown>;
}

// ---------------------------------------------------------------------------
// Middleware factories
// ---------------------------------------------------------------------------

/**
 * Create middleware that verifies the user has one of the specified roles.
 *
 * @example
 *   const requireAdminOrGuild = createRoleMiddleware(["ADMIN", "GUILD"]);
 */
export function createRoleMiddleware(allowedRoles: readonly UserRole[]) {
  return async ({ ctx, next }: RBACMiddlewareContext) => {
    const result = checkRole(ctx.session.user.role, allowedRoles);
    if (!result.allowed) {
      throw new TRPCError({
        code: "FORBIDDEN",
        message: result.reason ?? "Insufficient role",
      });
    }
    return next();
  };
}

/**
 * Create middleware that verifies the user has one of the specified user types.
 *
 * @example
 *   const requireFreelancer = createUserTypeMiddleware(["FREELANCER"]);
 */
export function createUserTypeMiddleware(allowedTypes: readonly UserType[]) {
  return async ({ ctx, next }: RBACMiddlewareContext) => {
    const result = checkUserType(ctx.session.user.userType, allowedTypes);
    if (!result.allowed) {
      throw new TRPCError({
        code: "FORBIDDEN",
        message: result.reason ?? "Insufficient user type",
      });
    }
    return next();
  };
}

/**
 * Create middleware that verifies the user meets a minimum verification level.
 *
 * @example
 *   const requireVerified = createVerificationLevelMiddleware("VERIFIED");
 */
export function createVerificationLevelMiddleware(
  minimumLevel: VerificationLevel,
) {
  return async ({ ctx, next }: RBACMiddlewareContext) => {
    const result = checkVerificationLevel(
      ctx.session.user.verificationLevel,
      minimumLevel,
    );
    if (!result.allowed) {
      throw new TRPCError({
        code: "FORBIDDEN",
        message: result.reason ?? "Insufficient verification level",
      });
    }
    return next();
  };
}

/**
 * Create middleware that restricts access to ADMIN role only.
 *
 * @example
 *   const adminOnly = createAdminMiddleware();
 */
export function createAdminMiddleware() {
  return createRoleMiddleware(["ADMIN"]);
}
