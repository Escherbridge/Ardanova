/**
 * The contract for a project's money page — the wire shape ProjectLedgerService returns.
 *
 * It lives here, beside the other contracts and under test, because a zod boundary that
 * silently drops a field is not a type error: `summary`, `entriesVisible` and
 * `entriesHiddenNote` were absent from this object once, so `z.object` stripped all three,
 * the money tab saw an empty entry list and told sponsors a FUNDED project had raised
 * nothing. `project-money-contract.test.ts` is what keeps that from coming back.
 */

import { z } from "zod";
/** Which way value moved, from the project's side. */
export const PROJECT_MONEY_DIRECTIONS = ["In", "Out"] as const;

/** What an entry is counted in: spendable funds, or a share of the project. */
export const PROJECT_MONEY_DENOMINATIONS = ["Funds", "Stake"] as const;

export const projectMoneyEntrySchema = z.object({
  id: z.string(),
  occurredAt: z.string(),
  direction: z.enum(PROJECT_MONEY_DIRECTIONS),
  denomination: z.enum(PROJECT_MONEY_DENOMINATIONS),
  headline: z.string(),
  description: z.string(),
  /** The exact figure as text. Always display this, never `amountBaseUnits`. */
  amountText: z.string(),
  amountBaseUnits: z.number(),
  amountScale: z.number().int(),
  unitLabel: z.string(),
  exchangedForText: z.string().nullish(),
  /** The D16 currency reference AT THE TIME, in exact cents. */
  usdReferenceMinorUnits: z.number().nullish(),
  usdReferenceText: z.string().nullish(),
  isSettled: z.boolean(),
  pendingReason: z.string().nullish(),
  counterpartyName: z.string().nullish(),
  /** The node operation this entry attests to; shown only behind "verify". */
  verifyReference: z.string().nullish(),
});

/**
 * The project's balance of record. `fundsText` is null when the funds account could
 * not be read — a null is NOT a zero, and `unavailableReason` says why.
 */
export const projectMoneyBalanceSchema = z.object({
  isAvailable: z.boolean(),
  fundsBaseUnits: z.number().nullish(),
  fundsText: z.string().nullish(),
  fundsScale: z.number().int(),
  fundsUnitLabel: z.string(),
  observedAt: z.string().nullish(),
  unavailableReason: z.string().nullish(),
});

/**
 * Aggregate support, and the WHOLE money payload a non-member is sent. It carries a
 * headcount and one project-level total; never a per-person figure.
 */
export const projectMoneySummarySchema = z.object({
  sponsorCount: z.number().int(),
  contributorCount: z.number().int(),
  sponsorshipTotalUsdMinorUnits: z.number().int(),
  sponsorshipTotalUsdText: z.string(),
});

export const projectMoneyViewSchema = z.object({
  projectId: z.string(),
  projectUnitLabel: z.string().nullish(),
  balance: projectMoneyBalanceSchema,
  balanceProvenance: z.string(),
  /**
   * ALWAYS present. `entries` being empty is ambiguous on its own — it means either "nothing
   * has happened" or "you are not on this project" — and a plain z.object() silently stripped
   * these three fields, so the money tab told sponsors a funded project had raised nothing.
   * The summary is what disambiguates it, and a project publicly asking for sponsorship has
   * to be able to show that it has support.
   */
  summary: projectMoneySummarySchema,
  entries: z.array(projectMoneyEntrySchema),
  /** False when the reader is not on the project, so no entry was built for them at all. */
  entriesVisible: z.boolean(),
  /** Why there are no entries, when the reader may not see them. Server wording, not the UI's. */
  entriesHiddenNote: z.string().nullish(),
  counterpartyNamesVisible: z.boolean(),
  /** Why names are missing, when they are. The server owns this wording, not the UI. */
  counterpartyNamesNote: z.string().nullish(),
});

export type ProjectMoneyEntry = z.infer<typeof projectMoneyEntrySchema>;
export type ProjectMoneyView = z.infer<typeof projectMoneyViewSchema>;
