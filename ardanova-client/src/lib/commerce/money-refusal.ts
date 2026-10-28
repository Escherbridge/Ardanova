/**
 * The BFF's money refusal codes (`ArdaNova.API/Controllers/MoneyRefusal.cs` `MoneyRefusalCodes`),
 * carried to the browser as `error.data.moneyCode`. See `./AGENTS.md` §Persisted request keys.
 */

export const MONEY_REFUSAL_CODES = [
  "setup_required",
  "under_review",
  "consent_revoked",
  "account_holder_required",
  "kyb_required",
  "payments_not_configured",
  "rail_busy",
  "definite",
  "unconfirmed",
  "unauthenticated",
] as const;

export type MoneyRefusalCode = (typeof MONEY_REFUSAL_CODES)[number];

const KNOWN_CODES: ReadonlySet<string> = new Set(MONEY_REFUSAL_CODES);

/** The server-set code in a BFF error body, or null when it is missing or not one this app knows. */
export function parseMoneyRefusalCode(value: unknown): MoneyRefusalCode | null {
  return typeof value === "string" && KNOWN_CODES.has(value)
    ? (value as MoneyRefusalCode)
    : null;
}

/** The money code on a thrown tRPC client error (`error.data.moneyCode`), or null for anything else. */
export function moneyCodeOf(error: unknown): MoneyRefusalCode | null {
  if (typeof error !== "object" || error === null) return null;
  const data = (error as { data?: unknown }).data;
  if (typeof data !== "object" || data === null) return null;
  return parseMoneyRefusalCode((data as { moneyCode?: unknown }).moneyCode);
}
