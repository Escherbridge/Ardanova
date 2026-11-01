import { describe, expect, it } from "vitest";

import {
  audienceOptionsFor,
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
