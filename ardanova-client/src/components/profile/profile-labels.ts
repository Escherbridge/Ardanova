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

/** "Mar 2024" for a stored experience date, read as a UTC calendar date so the viewer's zone cannot shift the month. */
export function formatExperienceMonth(
  value: string | Date | null | undefined,
): string {
  if (!value) return "";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? ""
    : date.toLocaleDateString("en-US", {
        month: "short",
        year: "numeric",
        timeZone: "UTC",
      });
}
