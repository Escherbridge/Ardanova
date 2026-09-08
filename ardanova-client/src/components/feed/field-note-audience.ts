/** Plain-language names for who can read a field note. */

export type FieldNoteAudience =
  | "PUBLIC"
  | "PROJECT_MEMBERS"
  | "GUILD_MEMBERS"
  | "PRIVATE";

const audienceLabels: Record<FieldNoteAudience, string> = {
  PUBLIC: "Everyone",
  PROJECT_MEMBERS: "Project team",
  GUILD_MEMBERS: "Group members",
  PRIVATE: "Only you",
};

const audienceDescriptions: Record<FieldNoteAudience, string> = {
  PUBLIC: "Anyone visiting this project can read it.",
  PROJECT_MEMBERS: "Only people on the project team can read it.",
  GUILD_MEMBERS: "Only members of the group can read it.",
  PRIVATE: "Kept to yourself until you change it.",
};

/** The audiences a note written inside a project may choose between. */
export const projectAudienceOptions: readonly FieldNoteAudience[] = [
  "PUBLIC",
  "PROJECT_MEMBERS",
  "PRIVATE",
];

export function audienceLabel(audience: string): string {
  return audienceLabels[audience as FieldNoteAudience] ?? "Everyone";
}

export function audienceDescription(audience: string): string {
  return (
    audienceDescriptions[audience as FieldNoteAudience] ??
    audienceDescriptions.PUBLIC
  );
}

/** Short, calendar-free age of a note, e.g. "4 min ago". */
export function formatNoteAge(isoTimestamp: string): string {
  const written = new Date(isoTimestamp).getTime();
  if (Number.isNaN(written)) return "";

  const elapsedSeconds = Math.max(0, Math.round((Date.now() - written) / 1000));
  if (elapsedSeconds < 60) return "just now";

  const elapsedMinutes = Math.round(elapsedSeconds / 60);
  if (elapsedMinutes < 60) return `${elapsedMinutes} min ago`;

  const elapsedHours = Math.round(elapsedMinutes / 60);
  if (elapsedHours < 24) return `${elapsedHours} h ago`;

  const elapsedDays = Math.round(elapsedHours / 24);
  if (elapsedDays < 30) return `${elapsedDays} d ago`;

  return new Date(written).toLocaleDateString();
}
