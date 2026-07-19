import { describe, expect, it } from "vitest";

import { cn } from "~/lib/utils";

import { authErrorSecondaryButtonClass } from "./styles";

describe("authentication error page", () => {
  it("keeps the secondary public-site action legible on the dark panel", () => {
    const classes = cn("bg-background", authErrorSecondaryButtonClass);

    expect(classes).toContain("bg-transparent");
    expect(classes).toContain("text-[#f6f0eb]");
    expect(classes).not.toContain("bg-background");
  });
});
