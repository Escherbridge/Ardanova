import { describe, expect, it } from "vitest";

import { assertLegacyGoogleLinkAllowed } from "./google-account-link-guard";

const validEnvironment = {
  ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK: "true",
  LEGACY_LINK_USER_ID: "user-123",
  LEGACY_LINK_GOOGLE_SUB: "108234567890123456789",
};

describe("legacy Google account link guard", () => {
  it("requires an explicit per-run opt in", () => {
    expect(() =>
      assertLegacyGoogleLinkAllowed({
        ...validEnvironment,
        ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK: undefined,
      }),
    ).toThrow("ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK=true");
  });

  it("does not accept an email address as either identity anchor", () => {
    expect(() =>
      assertLegacyGoogleLinkAllowed({
        ...validEnvironment,
        LEGACY_LINK_USER_ID: "person@example.com",
      }),
    ).toThrow("persisted user ID");
    expect(() =>
      assertLegacyGoogleLinkAllowed({
        ...validEnvironment,
        LEGACY_LINK_GOOGLE_SUB: "person@example.com",
      }),
    ).toThrow("Google provider subject");
  });

  it("returns only the explicit persisted ID and verified provider subject", () => {
    expect(assertLegacyGoogleLinkAllowed(validEnvironment)).toEqual({
      userId: "user-123",
      providerAccountId: "108234567890123456789",
    });
  });
});
