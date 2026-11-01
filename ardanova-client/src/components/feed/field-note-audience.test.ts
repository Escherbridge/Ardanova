import { describe, expect, it } from "vitest";

import {
  audienceDescription,
  audienceOptionsFor,
  audienceWithin,
  type AudienceContext,
  type FieldNoteAudience,
} from "./field-note-audience";

/** .NET PostVisibilityPolicy.HasCoherentAudience: a members-only audience needs the project or guild it names. */
function apiAccepts(
  audience: FieldNoteAudience,
  context: AudienceContext,
): boolean {
  // An ask carries the task's project server-side, so it counts as on a project.
  const onProject = context === "project" || context === "ask";
  if (audience === "PROJECT_MEMBERS") return onProject;
  if (audience === "GUILD_MEMBERS") return context === "guild";
  return true;
}

describe("audienceOptionsFor: the picker never offers an audience the API refuses with a 400", () => {
  it.each<[AudienceContext, FieldNoteAudience[]]>([
    [null, ["PUBLIC", "PRIVATE"]],
    ["project", ["PUBLIC", "PROJECT_MEMBERS", "PRIVATE"]],
    ["guild", ["PUBLIC", "GUILD_MEMBERS", "PRIVATE"]],
    // A private ask reaches nobody who could help.
    ["ask", ["PUBLIC", "PROJECT_MEMBERS"]],
  ])("%s", (context, expected) => {
    const offered = audienceOptionsFor(context);
    expect(offered).toEqual(expected);
    expect(offered.every((audience) => apiAccepts(audience, context))).toBe(
      true,
    );
  });
});

describe("audienceWithin: a changed attachment narrows the chosen audience, never widens it", () => {
  it.each<[FieldNoteAudience, AudienceContext, FieldNoteAudience]>([
    ["PROJECT_MEMBERS", "project", "PROJECT_MEMBERS"],
    // Detached: the team is gone, so only the writer is left.
    ["PROJECT_MEMBERS", null, "PRIVATE"],
    // Moved to a guild: its members are the same rung as a project's team.
    ["PROJECT_MEMBERS", "guild", "GUILD_MEMBERS"],
    ["GUILD_MEMBERS", "project", "PROJECT_MEMBERS"],
    ["GUILD_MEMBERS", null, "PRIVATE"],
    // An ask cannot be private; the narrowest it allows is the team.
    ["PRIVATE", "ask", "PROJECT_MEMBERS"],
    ["PUBLIC", "ask", "PUBLIC"],
  ])("%s on %s -> %s", (chosen, context, expected) => {
    expect(audienceWithin(chosen, audienceOptionsFor(context))).toBe(expected);
  });
});

describe("audienceDescription: true wherever the note shows", () => {
  it.each([
    ["PUBLIC", false, "Anyone can read it."],
    // A DRAFT project's cards are hidden from outsiders until publish.
    ["PUBLIC", true, "Anyone can read it once the project is published."],
    ["PROJECT_MEMBERS", true, "Only people on the project team can read it."],
  ])("%s (draft: %s)", (audience, projectIsDraft, line) => {
    expect(audienceDescription(audience, { projectIsDraft })).toBe(line);
  });
});
