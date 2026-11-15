import { describe, expect, it } from "vitest";

import { IDAHO_GOVERNING_LAW } from "./governing-law";

describe("terms of use", () => {
  it("keeps the Idaho governing-law sentence exact", () => {
    expect(IDAHO_GOVERNING_LAW).toBe(
      "These terms are governed by the laws of the State of Idaho, and any dispute will be resolved in the state or federal courts located in Ada County, Idaho.",
    );
  });
});
