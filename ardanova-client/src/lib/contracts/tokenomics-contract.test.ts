import { describe, expect, it } from "vitest";

import { projectTokenMetadataBatchDtoSchema } from "./tokenomics-contract";

describe("project-token metadata batch contract", () => {
  it("accepts only the minimal metadata and explicit missing IDs", () => {
    expect(
      projectTokenMetadataBatchDtoSchema.parse({
        items: [
          { id: "config-1", assetName: "Heat Commons", unitName: "HEAT" },
        ],
        missingIds: ["config-missing"],
      }),
    ).toEqual({
      items: [{ id: "config-1", assetName: "Heat Commons", unitName: "HEAT" }],
      missingIds: ["config-missing"],
    });
  });

  it("rejects broader or malformed backend records", () => {
    expect(
      projectTokenMetadataBatchDtoSchema.safeParse({
        items: [
          {
            id: "config-1",
            assetName: "Heat Commons",
            unitName: "HEAT",
            fundingRaised: 500,
          },
        ],
        missingIds: [],
      }).success,
    ).toBe(false);
    expect(
      projectTokenMetadataBatchDtoSchema.safeParse({
        items: [{ id: "config-1", unitName: "HEAT" }],
        missingIds: [],
      }).success,
    ).toBe(false);
  });
});
