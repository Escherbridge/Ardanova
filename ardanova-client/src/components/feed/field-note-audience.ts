/** Plain-language names for who can read a field note. */

export type FieldNoteAudience =
  | "PUBLIC"
  | "PROJECT_MEMBERS"
  | "GUILD_MEMBERS"
  | "PRIVATE";

const audienceLabels: Record<FieldNoteAudience, string> = {
  PUBLIC: "Everyone",
  PROJECT_MEMBERS: "Project team",
  GUILD_MEMBERS: "Guild members",
  PRIVATE: "Only you",
};

// Who can read it, wherever it shows (Home, tag pages, the project or guild): never where it is written.
const audienceDescriptions: Record<FieldNoteAudience, string> = {
  PUBLIC: "Anyone can read it.",
  PROJECT_MEMBERS: "Only people on the project team can read it.",
  GUILD_MEMBERS: "Only members of the guild can read it.",
  PRIVATE: "Only you can read it.",
};

/** The audiences a note written inside a project may choose between. */
export const projectAudienceOptions: readonly FieldNoteAudience[] = [
  "PUBLIC",
  "PROJECT_MEMBERS",
  "PRIVATE",
];

/** Where a note is being written: on a project, in a guild, as an ask on a project's task, or attached to nothing. */
export type AudienceContext = "project" | "guild" | "ask" | null;

const audienceOptionsByContext: Record<
  NonNullable<AudienceContext> | "none",
  readonly FieldNoteAudience[]
> = {
  project: projectAudienceOptions,
  guild: ["PUBLIC", "GUILD_MEMBERS", "PRIVATE"],
  // An ask nobody can read helps nobody, so it is never private.
  ask: ["PUBLIC", "PROJECT_MEMBERS"],
  none: ["PUBLIC", "PRIVATE"],
};

/** The audiences the API accepts here: a members-only note needs the project or guild whose members read it. */
export function audienceOptionsFor(
  context: AudienceContext,
): readonly FieldNoteAudience[] {
  return audienceOptionsByContext[context ?? "none"];
}

/** How far an audience reaches, narrowest first; a project's team and a guild's members are one rung. */
const audienceReach: Record<FieldNoteAudience, number> = {
  PRIVATE: 0,
  PROJECT_MEMBERS: 1,
  GUILD_MEMBERS: 1,
  PUBLIC: 2,
};

/**
 * The audience a note goes to: the chosen one when offered here, else the widest offered one that reaches no further,
 * else (an ask cannot be private) the narrowest offered. Never silently wider than chosen. See ./composer/AGENTS.md §Audience.
 */
export function audienceWithin(
  chosen: FieldNoteAudience,
  offered: readonly FieldNoteAudience[],
): FieldNoteAudience {
  if (offered.includes(chosen)) return chosen;
  const narrowestFirst = [...offered].sort(
    (left, right) => audienceReach[left] - audienceReach[right],
  );
  const noWider = narrowestFirst.filter(
    (audience) => audienceReach[audience] <= audienceReach[chosen],
  );
  return noWider.at(-1) ?? narrowestFirst[0] ?? "PRIVATE";
}

export function audienceLabel(audience: string): string {
  return audienceLabels[audience as FieldNoteAudience] ?? "Everyone";
}

/** Who can read a note: one line per audience, true wherever the note is written. */
export function audienceDescription(
  audience: string,
  { projectIsDraft = false }: { projectIsDraft?: boolean } = {},
): string {
  const known: FieldNoteAudience =
    audience in audienceDescriptions
      ? (audience as FieldNoteAudience)
      : "PUBLIC";
  // A DRAFT project's cards are hidden from everyone off its team until it is published.
  if (projectIsDraft && known === "PUBLIC")
    return "Anyone can read it once the project is published.";
  return audienceDescriptions[known];
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
