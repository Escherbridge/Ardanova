import { describe, expect, it } from "vitest";

import { publicCanonical, SITE_ORIGIN } from "./site";

describe("public canonical URLs", () => {
  it("keeps a trailing slash on the homepage only", () => {
    expect(SITE_ORIGIN).toBe("https://ardanova.com");
    expect(publicCanonical("/")).toBe("https://ardanova.com/");
    expect(publicCanonical("/terms")).toBe("https://ardanova.com/terms");
    expect(publicCanonical("/privacy")).toBe("https://ardanova.com/privacy");
    expect(publicCanonical("/notes/a-record-is-not-a-right")).toBe(
      "https://ardanova.com/notes/a-record-is-not-a-right",
    );
  });
});
