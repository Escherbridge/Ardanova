import { describe, expect, it } from "vitest";

import {
  projectLedeOf,
  projectTabFrom,
  projectTagsOf,
} from "./project-page-model";

describe("projectTabFrom: which tab a link opens", () => {
  it.each([
    ["", null],
    ["tab=comments", "comments"],
    // The feed kit's DISCUSSION Reply and governance's proposal link.
    ["tab=proposals&proposalId=pr-1", "proposals"],
    ["proposalId=pr-1", "proposals"],
    ["tab=team&proposalId=pr-1", "team"],
    // The retired Updates tab was folded into the Feed.
    ["tab=updates", "feed"],
    ["tab=nonsense", null],
    // Not an inherited object key.
    ["tab=constructor", null],
  ])("?%s -> %s", (search, tab) => {
    expect(projectTabFrom(new URLSearchParams(search))).toBe(tab);
  });
});

describe("projectTagsOf: the header's tag chips", () => {
  it.each([
    {
      flow: "derived #slugs first, then free tags, each once",
      project: {
        hashtags: ["#construction", "#community"],
        tags: "timber-framing,construction",
      },
      linked: ["construction", "community", "timber-framing"],
      plain: [],
    },
    {
      flow: "legacy free text that is not a tag stays plain; a tag-shaped one is canonical",
      project: { tags: "community garden, Solar" },
      linked: ["solar"],
      plain: ["community garden"],
    },
    {
      flow: "an older API with neither",
      project: { tags: null },
      linked: [],
      plain: [],
    },
  ])("$flow", ({ project, linked, plain }) => {
    expect(projectTagsOf(project)).toEqual({ linked, plain });
  });
});

describe("projectLedeOf: the header's one-line ask", () => {
  it.each([
    [{ summary: "A barn.", description: "Long text." }, "A barn."],
    [{ summary: "  ", description: "Long text." }, "Long text."],
    // A pre-summary project's description is a copy of its solution: shown once, in Overview.
    [{ description: "Raise it.", solution: "Raise it." }, undefined],
  ])("%o -> %s", (project, lede) => {
    expect(projectLedeOf(project)).toBe(lede);
  });
});
