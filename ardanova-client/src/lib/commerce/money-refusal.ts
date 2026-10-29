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
  /** A final state that may include a payment: never releases, the page asks the void route. */
  "finished",
  /** A payment held for a manual check: never releases, never resent; `error.data.moneyIntentId` names it. */
  "needs_review",
  "unconfirmed",
  "unauthenticated",
] as const;

export type MoneyRefusalCode = (typeof MONEY_REFUSAL_CODES)[number];

/** Codes that LOOK final. Without a server id to read, the page settles them through void-or-reveal. */
export function looksFinal(code: MoneyRefusalCode | null): boolean {
  return code === "definite" || code === "finished";
}

const KNOWN_CODES: ReadonlySet<string> = new Set(MONEY_REFUSAL_CODES);

/** The server-set code in a BFF error body, or null when it is missing or not one this app knows. */
export function parseMoneyRefusalCode(value: unknown): MoneyRefusalCode | null {
  return typeof value === "string" && KNOWN_CODES.has(value)
    ? (value as MoneyRefusalCode)
    : null;
}

/** A payment id as the server writes it: 32 hex characters, or a dashed UUID. */
const PAYMENT_ID_PATTERN =
  /^(?:[0-9a-fA-F]{32}|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$/u;

/** A server-named payment id, or null when it is missing or not shaped like one this app can read back. */
export function parseMoneyIntentId(value: unknown): string | null {
  return typeof value === "string" && PAYMENT_ID_PATTERN.test(value.trim())
    ? value.trim()
    : null;
}

function errorData(error: unknown): Record<string, unknown> | null {
  if (typeof error !== "object" || error === null) return null;
  const data = (error as { data?: unknown }).data;
  return typeof data === "object" && data !== null ? (data as Record<string, unknown>) : null;
}

/** The money code on a thrown tRPC client error (`error.data.moneyCode`), or null for anything else. */
export function moneyCodeOf(error: unknown): MoneyRefusalCode | null {
  return parseMoneyRefusalCode(errorData(error)?.moneyCode);
}

/** The held payment a `needs_review` refusal names (`error.data.moneyIntentId`), or null. */
export function moneyIntentIdOf(error: unknown): string | null {
  return parseMoneyIntentId(errorData(error)?.moneyIntentId);
}
