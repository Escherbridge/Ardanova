import { describe, it, expect } from "vitest";
import {
  UserRole,
  UserType,
  VerificationLevel,
  VERIFICATION_LEVEL_ORDER,
  checkRole,
  checkUserType,
  checkVerificationLevel,
  meetsMinimumVerificationLevel,
} from "../rbac";

// ===========================================================================
// Constants
// ===========================================================================

describe("VERIFICATION_LEVEL_ORDER", () => {
  it("should define the correct hierarchy: ANONYMOUS < VERIFIED < PRO < EXPERT", () => {
    expect(VERIFICATION_LEVEL_ORDER.ANONYMOUS).toBeLessThan(
      VERIFICATION_LEVEL_ORDER.VERIFIED,
    );
    expect(VERIFICATION_LEVEL_ORDER.VERIFIED).toBeLessThan(
      VERIFICATION_LEVEL_ORDER.PRO,
    );
    expect(VERIFICATION_LEVEL_ORDER.PRO).toBeLessThan(
      VERIFICATION_LEVEL_ORDER.EXPERT,
    );
  });

  it("should include all four verification levels", () => {
    expect(Object.keys(VERIFICATION_LEVEL_ORDER)).toHaveLength(4);
    expect(VERIFICATION_LEVEL_ORDER).toHaveProperty("ANONYMOUS");
    expect(VERIFICATION_LEVEL_ORDER).toHaveProperty("VERIFIED");
    expect(VERIFICATION_LEVEL_ORDER).toHaveProperty("PRO");
    expect(VERIFICATION_LEVEL_ORDER).toHaveProperty("EXPERT");
  });
});

// ===========================================================================
// Enums
// ===========================================================================

describe("UserRole enum", () => {
  it("should have INDIVIDUAL, GUILD, and ADMIN values", () => {
    expect(UserRole.INDIVIDUAL).toBe("INDIVIDUAL");
    expect(UserRole.GUILD).toBe("GUILD");
    expect(UserRole.ADMIN).toBe("ADMIN");
  });
});

describe("UserType enum", () => {
  it("should have all six user type values", () => {
    expect(UserType.INNOVATOR).toBe("INNOVATOR");
    expect(UserType.SUPPORTER).toBe("SUPPORTER");
    expect(UserType.VOLUNTEER).toBe("VOLUNTEER");
    expect(UserType.FREELANCER).toBe("FREELANCER");
    expect(UserType.SME_OWNER).toBe("SME_OWNER");
    expect(UserType.GUILD_MEMBER).toBe("GUILD_MEMBER");
  });
});

describe("VerificationLevel enum", () => {
  it("should have all four verification level values", () => {
    expect(VerificationLevel.ANONYMOUS).toBe("ANONYMOUS");
    expect(VerificationLevel.VERIFIED).toBe("VERIFIED");
    expect(VerificationLevel.PRO).toBe("PRO");
    expect(VerificationLevel.EXPERT).toBe("EXPERT");
  });
});

// ===========================================================================
// checkRole
// ===========================================================================

describe("checkRole", () => {
  it("should return allowed: true when user role is in allowed list", () => {
    const result = checkRole("ADMIN", ["ADMIN", "GUILD"]);
    expect(result.allowed).toBe(true);
    expect(result.reason).toBeUndefined();
  });

  it("should return allowed: true for single role match", () => {
    const result = checkRole("INDIVIDUAL", ["INDIVIDUAL"]);
    expect(result.allowed).toBe(true);
  });

  it("should return allowed: false when user role is not in allowed list", () => {
    const result = checkRole("INDIVIDUAL", ["ADMIN"]);
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
    expect(result.reason).toContain("ADMIN");
  });

  it("should return allowed: false when user role is undefined", () => {
    const result = checkRole(undefined, ["ADMIN"]);
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
  });

  it("should handle multiple allowed roles", () => {
    const result = checkRole("GUILD", ["ADMIN", "GUILD", "INDIVIDUAL"]);
    expect(result.allowed).toBe(true);
  });

  it("should return allowed: false for empty allowed roles array", () => {
    const result = checkRole("ADMIN", []);
    expect(result.allowed).toBe(false);
  });
});

// ===========================================================================
// checkUserType
// ===========================================================================

describe("checkUserType", () => {
  it("should return allowed: true when user type is in allowed list", () => {
    const result = checkUserType("FREELANCER", ["FREELANCER", "INNOVATOR"]);
    expect(result.allowed).toBe(true);
    expect(result.reason).toBeUndefined();
  });

  it("should return allowed: false when user type is not in allowed list", () => {
    const result = checkUserType("VOLUNTEER", ["FREELANCER"]);
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
    expect(result.reason).toContain("FREELANCER");
  });

  it("should return allowed: false when user type is undefined", () => {
    const result = checkUserType(undefined, ["FREELANCER"]);
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
  });

  it("should match any type from the allowed list", () => {
    const allowed: Array<
      "INNOVATOR" | "SUPPORTER" | "VOLUNTEER" | "FREELANCER" | "SME_OWNER" | "GUILD_MEMBER"
    > = ["INNOVATOR", "SUPPORTER", "VOLUNTEER", "FREELANCER", "SME_OWNER", "GUILD_MEMBER"];
    for (const type of allowed) {
      const result = checkUserType(type, allowed);
      expect(result.allowed).toBe(true);
    }
  });
});

// ===========================================================================
// checkVerificationLevel
// ===========================================================================

describe("checkVerificationLevel", () => {
  it("should return allowed: true when user level equals minimum", () => {
    const result = checkVerificationLevel("VERIFIED", "VERIFIED");
    expect(result.allowed).toBe(true);
  });

  it("should return allowed: true when user level exceeds minimum", () => {
    const result = checkVerificationLevel("EXPERT", "VERIFIED");
    expect(result.allowed).toBe(true);
  });

  it("should return allowed: false when user level is below minimum", () => {
    const result = checkVerificationLevel("ANONYMOUS", "VERIFIED");
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
    expect(result.reason).toContain("VERIFIED");
  });

  it("should return allowed: false when user level is undefined", () => {
    const result = checkVerificationLevel(undefined, "VERIFIED");
    expect(result.allowed).toBe(false);
    expect(result.reason).toBeDefined();
  });

  it("should allow ANONYMOUS when minimum is ANONYMOUS", () => {
    const result = checkVerificationLevel("ANONYMOUS", "ANONYMOUS");
    expect(result.allowed).toBe(true);
  });

  it("should allow EXPERT for any minimum level", () => {
    for (const min of ["ANONYMOUS", "VERIFIED", "PRO", "EXPERT"] as const) {
      const result = checkVerificationLevel("EXPERT", min);
      expect(result.allowed).toBe(true);
    }
  });

  it("should deny ANONYMOUS for all levels above ANONYMOUS", () => {
    for (const min of ["VERIFIED", "PRO", "EXPERT"] as const) {
      const result = checkVerificationLevel("ANONYMOUS", min);
      expect(result.allowed).toBe(false);
    }
  });
});

// ===========================================================================
// meetsMinimumVerificationLevel
// ===========================================================================

describe("meetsMinimumVerificationLevel", () => {
  it("should return true when current level equals minimum", () => {
    expect(meetsMinimumVerificationLevel("PRO", "PRO")).toBe(true);
  });

  it("should return true when current level exceeds minimum", () => {
    expect(meetsMinimumVerificationLevel("EXPERT", "ANONYMOUS")).toBe(true);
  });

  it("should return false when current level is below minimum", () => {
    expect(meetsMinimumVerificationLevel("ANONYMOUS", "EXPERT")).toBe(false);
  });

  it("should correctly compare all ordered pairs", () => {
    const levels: Array<"ANONYMOUS" | "VERIFIED" | "PRO" | "EXPERT"> = [
      "ANONYMOUS",
      "VERIFIED",
      "PRO",
      "EXPERT",
    ];
    for (let i = 0; i < levels.length; i++) {
      for (let j = 0; j < levels.length; j++) {
        const current = levels[i] as "ANONYMOUS" | "VERIFIED" | "PRO" | "EXPERT";
        const minimum = levels[j] as "ANONYMOUS" | "VERIFIED" | "PRO" | "EXPERT";
        const result = meetsMinimumVerificationLevel(current, minimum);
        expect(result).toBe(i >= j);
      }
    }
  });
});
