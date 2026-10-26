import { describe, expect, it } from "vitest";

import { linkableSourceUrl, refineDisclosure } from "./nova-refine-copy";

describe("Refine header and footer say what this deployment does with the idea", () => {
  it.each([
    ["live", undefined, true, "go to Nova", true],
    ["local-preview", undefined, true, "nothing is sent", false],
    ["off", undefined, true, "not available", false],
    // A preview reply outranks a status that has not loaded yet.
    [undefined, "local-preview", true, "nothing is sent", false],
    // Unknown status (API unreachable) keeps the live wording but names no provider it cannot vouch for.
    [null, undefined, true, "go to Nova", false],
  ] as const)(
    "status %s, reply %s: subtitle says %s; provider credit %s",
    (availability, replyMode, saved, phrase, credit) => {
      const disclosure = refineDisclosure(availability, replyMode, saved);

      expect(disclosure.subtitle).toContain(phrase);
      expect(disclosure.showsProviderCredit).toBe(credit);
    },
  );

  it("never claims data goes anywhere outside live mode", () => {
    for (const mode of ["local-preview", "off"] as const) {
      const { subtitle, description } = refineDisclosure(mode, undefined, true);
      expect(`${subtitle} ${description}`).not.toMatch(
        /go to Nova|go through/,
      );
    }
  });

  it("warns when tab storage is blocked", () => {
    expect(refineDisclosure("live", undefined, false).subtitle).toContain(
      "a reload loses it",
    );
  });
});

describe("verdict sources", () => {
  it.each([
    ["https://example.org/survey", "https://example.org/survey"],
    ["http://example.org/", "http://example.org/"],
    ["javascript:alert(1)", null],
    ["data:text/html,hi", null],
    ["/admin", null],
  ])("%s links as %s", (url, expected) => {
    expect(linkableSourceUrl(url)).toBe(expected);
  });
});
