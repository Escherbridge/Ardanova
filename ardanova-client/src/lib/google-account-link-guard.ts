export const LEGACY_GOOGLE_LINK_OPT_IN = "ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK";

export type LegacyGoogleLinkEnvironment = {
  ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK?: string;
  LEGACY_LINK_GOOGLE_SUB?: string;
  LEGACY_LINK_USER_ID?: string;
};

export type LegacyGoogleLinkTarget = {
  providerAccountId: string;
  userId: string;
};

const SAFE_IDENTIFIER = /^[A-Za-z0-9_-]{6,255}$/;

/** Validates explicit identifiers without accepting email as identity evidence. */
export function assertLegacyGoogleLinkAllowed(
  environment: LegacyGoogleLinkEnvironment,
): LegacyGoogleLinkTarget {
  if (
    environment.ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK?.trim().toLowerCase() !==
    "true"
  ) {
    throw new Error(
      `${LEGACY_GOOGLE_LINK_OPT_IN}=true is required for this one-time account link.`,
    );
  }

  const userId = environment.LEGACY_LINK_USER_ID?.trim() ?? "";
  const providerAccountId = environment.LEGACY_LINK_GOOGLE_SUB?.trim() ?? "";
  if (!SAFE_IDENTIFIER.test(userId)) {
    throw new Error("LEGACY_LINK_USER_ID must be a valid persisted user ID.");
  }
  if (!SAFE_IDENTIFIER.test(providerAccountId)) {
    throw new Error(
      "LEGACY_LINK_GOOGLE_SUB must be a verified Google provider subject.",
    );
  }

  return { userId, providerAccountId };
}
