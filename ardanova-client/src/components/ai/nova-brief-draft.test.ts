import { describe, expect, it } from "vitest";

import type { NovaBrief } from "~/lib/api/ardanova/endpoints/nova";
import { briefAsProjectDraft } from "./nova-brief-draft";

const idea =
  "Volunteers map street heat on our blocks so the council funds shade trees.";

const brief: NovaBrief = {
  workingName: "Neighborhood Heat Commons",
  oneLiner: "Volunteers map street heat so the block can ask for shade.",
  customer: "Residents of the east side blocks.",
  problem: "Summer heat with no data to show the council.",
  offer: "A shared heat map and a monthly report.",
  pricing: "to decide",
  channels: ["Block association"],
  milestones: ["Ten sensors placed"],
  metrics: ["Readings a week"],
};

describe("briefAsProjectDraft", () => {
  it("fills the draft from the brief and the member's chosen category", () => {
    expect(briefAsProjectDraft(brief, idea, "ENVIRONMENT")).toEqual({
      title: "Neighborhood Heat Commons",
      description: brief.oneLiner,
      summary: brief.oneLiner,
      problemStatement: brief.problem,
      solution: brief.offer,
      categories: ["ENVIRONMENT"],
      targetAudience: brief.customer,
    });
  });

  it.each([
    ["too short for project.create", "Shade."],
    ["an upstream placeholder", "To decide with the block"],
  ])(
    "a brief field that is %s falls back to the member's own idea, so create never refuses it",
    (_, weak) => {
      const draft = briefAsProjectDraft(
        {
          ...brief,
          problem: weak,
          offer: weak,
          oneLiner: weak,
          customer: "to decide",
        },
        idea,
        "OTHER",
      );

      expect(draft.problemStatement).toBe(idea);
      expect(draft.solution).toBe(idea);
      expect(draft.description).toBe(idea);
      expect(draft.targetAudience).toBeNull();
    },
  );
});
