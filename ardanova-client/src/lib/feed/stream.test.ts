import { describe, expect, it } from "vitest";

import { mergeFeedPages, withCardPatched } from "./stream";

type Card = { id: string; copy: string };
const leading = (id: string): Card => ({ id, copy: "local" });
const served = (id: string): Card => ({ id, copy: "stream" });

describe("mergeFeedPages: leading cards keep their place, the stream's copy wins", () => {
  it.each([
    {
      flow: "a leading card the stream has not read yet stays as published",
      lead: [leading("new")],
      pages: [[served("a")]],
      merged: [leading("new"), served("a")],
    },
    {
      flow: "once the stream carries it, its copy shows in the leading place, once",
      lead: [leading("new")],
      pages: [[served("a"), served("new")]],
      merged: [served("new"), served("a")],
    },
    {
      flow: "a copy on a later page still replaces the leading one",
      lead: [leading("b"), leading("new")],
      pages: [[served("a")], [served("new")]],
      merged: [leading("b"), served("new"), served("a")],
    },
    {
      flow: "a card repeated across pages shows once, first copy",
      lead: [],
      pages: [[served("a")], [{ id: "a", copy: "repeat" }, served("c")]],
      merged: [served("a"), served("c")],
    },
  ])("$flow", ({ lead, pages, merged }) => {
    expect(
      mergeFeedPages<Card>(
        lead,
        pages.map((items) => ({ items })),
      ),
    ).toEqual(merged);
  });
});

describe("withCardPatched: an ask edit reaches every cached copy", () => {
  it("patches only the matching card, on every page, and keeps the rest of the cache", () => {
    const cache = {
      pageParams: [null, "c1"],
      pages: [
        { items: [served("a"), served("ask")], nextCursor: "c1" },
        { items: [served("ask")], nextCursor: null },
      ],
    };

    const patch = (data: typeof cache | undefined) =>
      withCardPatched<Card, typeof cache>(data, "ask", { copy: "edited" });

    expect(patch(cache)).toEqual({
      pageParams: [null, "c1"],
      pages: [
        {
          items: [served("a"), { id: "ask", copy: "edited" }],
          nextCursor: "c1",
        },
        { items: [{ id: "ask", copy: "edited" }], nextCursor: null },
      ],
    });
    // No cache for a scope yet: nothing to patch, nothing created.
    expect(patch(undefined)).toBeUndefined();
  });
});
