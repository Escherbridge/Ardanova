import { describe, expect, it } from "vitest";

import {
  conversionPreviewDtoSchema,
  fundingProjectTokenConfigSchema,
  parseFundingAmount,
} from "./investment-preview-contract";

describe("investment preview contracts", () => {
  it("accepts the conversion DTO returned by the .NET API", () => {
    expect(
      conversionPreviewDtoSchema.parse({
        projectTokenValueUsd: 2.5,
        ardaValueUsd: 1.25,
        sourceTokenAmount: 40,
        usdValue: 100,
        ardaAmount: 80,
      }),
    ).toEqual({
      projectTokenValueUsd: 2.5,
      ardaValueUsd: 1.25,
      sourceTokenAmount: 40,
      usdValue: 100,
      ardaAmount: 80,
    });
  });

  it("rejects the invented legacy conversion shape", () => {
    expect(
      conversionPreviewDtoSchema.safeParse({
        projectTokens: 40,
        usdAmount: 100,
        tokenRate: 2.5,
        ardaRate: 1.25,
        ardaAmount: 80,
      }).success,
    ).toBe(false);
  });

  it("narrows the project config fields needed by funding UI", () => {
    const result = fundingProjectTokenConfigSchema.safeParse({
      id: "config-1",
      assetName: "Community Solar Credits",
      unitName: "SOLAR",
      gateStatus: "FUNDING",
      totalSupply: 10_000,
      ignoredBackendField: true,
    });

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data).not.toHaveProperty("ignoredBackendField");
    }
  });

  it.each(["", "0", "-1", "1.001", "ten", ".5", "01"])(
    "rejects invalid funding amount %s",
    (value) => {
      expect(parseFundingAmount(value)).toBeNull();
    },
  );

  it("preserves the validated decimal string sent to the server", () => {
    expect(parseFundingAmount(" 125.50 ")).toEqual({
      apiAmount: "125.50",
      value: 125.5,
    });
  });
});
