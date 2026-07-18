import { describe, expect, it } from "vitest";

import {
  buildSignInHref,
  normalizeInternalCallbackUrl,
  normalizeInternalReturnTo,
} from "./auth-navigation";

describe("auth navigation", () => {
  it.each([
    [undefined, "/dashboard"],
    ["https://attacker.example/steal", "/dashboard"],
    ["//attacker.example/steal", "/dashboard"],
    ["/\\attacker.example/steal", "/dashboard"],
    [
      "/projects/neighborhood?tab=tasks#active",
      "/projects/neighborhood?tab=tasks#active",
    ],
  ])("normalizes %j to %s", (input, expected) => {
    expect(normalizeInternalCallbackUrl(input)).toBe(expected);
  });

  it("builds a discoverable signup entry while preserving destination", () => {
    expect(buildSignInHref("/projects/create", { mode: "signup" })).toBe(
      "/auth/signin?mode=signup&callbackUrl=%2Fprojects%2Fcreate",
    );
  });

  it("accepts only a safe non-looping verification return destination", () => {
    expect(normalizeInternalReturnTo("/projects/create?step=4")).toBe(
      "/projects/create?step=4",
    );
    expect(
      normalizeInternalReturnTo("https://attacker.example/collect"),
    ).toBeNull();
    expect(
      normalizeInternalReturnTo("/settings/verification?returnTo=%2Fprojects"),
    ).toBeNull();
  });
});
