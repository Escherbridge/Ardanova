/** Turns an enum value such as `PROJECT_FOUNDER` into "Project founder". */
export function formatEnumLabel(value: string | null | undefined): string | null {
  if (!value) return null;
  const words = value.trim().toLowerCase().split(/[_\s]+/).filter(Boolean);
  if (words.length === 0) return null;
  const sentence = words.join(" ");
  return sentence.charAt(0).toUpperCase() + sentence.slice(1);
}

/** Badge labels for a member's declared type and identity-verification level. */
export function profileIdentityLabels(identity: {
  userType?: string | null;
  verificationLevel?: string | null;
}): { userType: string; verification: string | null } {
  const verification =
    identity.verificationLevel && identity.verificationLevel !== "ANONYMOUS"
      ? formatEnumLabel(identity.verificationLevel)
      : null;
  return {
    userType: formatEnumLabel(identity.userType) ?? "Member",
    verification,
  };
}
