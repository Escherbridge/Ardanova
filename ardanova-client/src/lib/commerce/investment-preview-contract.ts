import { z } from "zod";

import type { ConversionPreviewDto } from "~/lib/api/ardanova/endpoints/token-balances";

export const conversionPreviewDtoSchema: z.ZodType<ConversionPreviewDto> =
  z.object({
    projectTokenValueUsd: z.number().finite().nonnegative(),
    ardaValueUsd: z.number().finite().nonnegative(),
    sourceTokenAmount: z.number().int().positive(),
    usdValue: z.number().finite().nonnegative(),
    ardaAmount: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER),
  });

export type ConversionPreviewContract = z.infer<
  typeof conversionPreviewDtoSchema
>;

export const fundingProjectTokenConfigSchema = z.object({
  id: z.string().min(1),
  assetName: z.string().min(1),
  unitName: z.string().min(1),
  gateStatus: z.enum(["FUNDING", "ACTIVE", "SUCCEEDED", "FAILED"]),
  totalSupply: z.number().int().nonnegative(),
});

export type FundingProjectTokenConfig = z.infer<
  typeof fundingProjectTokenConfigSchema
>;

const positiveUsdInputPattern = /^(?:0|[1-9]\d*)(?:\.\d{1,2})?$/;

export interface FundingAmount {
  apiAmount: string;
  value: number;
}

/** Mirrors the funding-intent route's positive USD input contract. */
export function parseFundingAmount(input: string): FundingAmount | null {
  const apiAmount = input.trim();

  if (!positiveUsdInputPattern.test(apiAmount)) return null;

  const value = Number(apiAmount);
  if (
    !Number.isFinite(value) ||
    value > Number.MAX_SAFE_INTEGER / 100 ||
    value <= 0
  ) {
    return null;
  }

  return { apiAmount, value };
}
