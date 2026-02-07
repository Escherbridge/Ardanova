/**
 * Role-Based Access Control (RBAC) utilities for ArdaNova.
 *
 * Provides enum mirrors, permission checks for UserRole, UserType,
 * and VerificationLevel, plus composable tRPC middleware.
 */

// ---------------------------------------------------------------------------
// Enums (mirroring Prisma-generated enums for use without importing Prisma)
// ---------------------------------------------------------------------------

export const UserRole = {
  INDIVIDUAL: "INDIVIDUAL",
  GUILD: "GUILD",
  ADMIN: "ADMIN",
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const UserType = {
  INNOVATOR: "INNOVATOR",
  SUPPORTER: "SUPPORTER",
  VOLUNTEER: "VOLUNTEER",
  FREELANCER: "FREELANCER",
  SME_OWNER: "SME_OWNER",
  GUILD_MEMBER: "GUILD_MEMBER",
} as const;

export type UserType = (typeof UserType)[keyof typeof UserType];

export const VerificationLevel = {
  ANONYMOUS: "ANONYMOUS",
  VERIFIED: "VERIFIED",
  PRO: "PRO",
  EXPERT: "EXPERT",
} as const;

export type VerificationLevel =
  (typeof VerificationLevel)[keyof typeof VerificationLevel];

// ---------------------------------------------------------------------------
// Verification level ordering (ANONYMOUS = 0, EXPERT = 3)
// ---------------------------------------------------------------------------

export const VERIFICATION_LEVEL_ORDER: Record<VerificationLevel, number> = {
  ANONYMOUS: 0,
  VERIFIED: 1,
  PRO: 2,
  EXPERT: 3,
};

// ---------------------------------------------------------------------------
// Permission result type
// ---------------------------------------------------------------------------

export interface PermissionResult {
  allowed: boolean;
  reason?: string;
}

// ---------------------------------------------------------------------------
// Pure check functions
// ---------------------------------------------------------------------------

/**
 * Check whether a user role is within an allowed set of roles.
 */
export function checkRole(
  userRole: string | undefined | null,
  allowedRoles: readonly UserRole[],
): PermissionResult {
  if (!userRole || !allowedRoles.includes(userRole as UserRole)) {
    return {
      allowed: false,
      reason: `Requires one of roles: ${allowedRoles.join(", ")}`,
    };
  }
  return { allowed: true };
}

/**
 * Check whether a user type is within an allowed set of types.
 */
export function checkUserType(
  userType: string | undefined | null,
  allowedTypes: readonly UserType[],
): PermissionResult {
  if (!userType || !allowedTypes.includes(userType as UserType)) {
    return {
      allowed: false,
      reason: `Requires one of user types: ${allowedTypes.join(", ")}`,
    };
  }
  return { allowed: true };
}

/**
 * Check whether a user's verification level meets or exceeds a minimum.
 */
export function checkVerificationLevel(
  currentLevel: string | undefined | null,
  minimumLevel: VerificationLevel,
): PermissionResult {
  if (!currentLevel) {
    return {
      allowed: false,
      reason: `Requires minimum verification level: ${minimumLevel}`,
    };
  }
  if (!meetsMinimumVerificationLevel(currentLevel as VerificationLevel, minimumLevel)) {
    return {
      allowed: false,
      reason: `Requires minimum verification level: ${minimumLevel}`,
    };
  }
  return { allowed: true };
}

/**
 * Returns true when `current` is at or above `minimum` in the
 * ANONYMOUS -> VERIFIED -> PRO -> EXPERT hierarchy.
 */
export function meetsMinimumVerificationLevel(
  current: VerificationLevel,
  minimum: VerificationLevel,
): boolean {
  return VERIFICATION_LEVEL_ORDER[current] >= VERIFICATION_LEVEL_ORDER[minimum];
}
