/**
 * The sponsor types DOLLARS. This module is the only place that turns what they typed into the
 * platform-unit figure the funding-intent route still takes.
 *
 * D52 settles the direction: a person thinks in dollars, and the platform unit is at most a
 * derived caption. The founder's side of that inversion landed first (the create wizard's ask is
 * a `$` field with the unit figure demoted underneath), which left the two halves of the money
 * story disagreeing: the founder asked in dollars while the sponsor still spent in ARDA, on the
 * primary spend surface, with an `ARDA` affix chip of exactly the shape the founder's field had
 * just lost. This closes that half.
 *
 * The wire contract is deliberately UNCHANGED: `fundingIntent.createCheckout` still takes a
 * platform-unit amount, because that is the unit the exchange leg settles in. Only the surface a
 * person reads and types is dollars. Inverting the wire too would be a value-path change; this
 * is not one.
 */

/** Dollars and the platform unit both carry two decimal places. */
export const SPONSOR_AMOUNT_DECIMALS = 2;

/** The smallest sponsorship this surface accepts, in dollars. */
export const SPONSOR_MINIMUM_DOLLARS = 1;

/**
 * The largest sponsorship this surface accepts, in dollars. Well under the point at which the
 * derived unit figure stops being exactly representable, so a typed amount always round-trips.
 */
export const SPONSOR_MAXIMUM_DOLLARS = 1_000_000;

/** Accepts `1234`, `1234.5`, `1234.56`, `$1,234.56`; rejects a third decimal place. */
const DOLLAR_INPUT_PATTERN = /^(?:0|[1-9]\d*)(?:\.\d{1,2})?$/;

export interface SponsorDollars {
  /** The dollar figure the person typed, as a number. */
  value: number;
  /** The same figure normalized for display and for comparing one input against another. */
  normalized: string;
}

/**
 * Parses a typed dollar amount, or returns null. A leading `$` and thousands separators are
 * accepted because the field shows a `$` affix and a person may well retype it.
 */
export function parseSponsorDollars(input: string): SponsorDollars | null {
  const normalized = input.trim().replace(/^\$/, "").replace(/,/g, "").trim();
  if (!DOLLAR_INPUT_PATTERN.test(normalized)) return null;

  const value = Number(normalized);
  if (
    !Number.isFinite(value) ||
    value < SPONSOR_MINIMUM_DOLLARS ||
    value > SPONSOR_MAXIMUM_DOLLARS
  ) {
    return null;
  }

  return { value, normalized };
}

/**
 * Dollars to the platform units the funding-intent route takes, at the node's live rate.
 *
 * Rounds DOWN to the unit's own decimal places: the person named a dollar ceiling, so spending
 * a rounded-up unit figure would spend more than they said. Returns null when there is no usable
 * rate — the caller then says nothing rather than quoting against an invented one.
 */
export function dollarsToPlatformUnits(
  dollars: number,
  usdPerUnit: number | undefined,
): number | null {
  if (!Number.isFinite(dollars) || dollars <= 0) return null;
  if (usdPerUnit === undefined || !Number.isFinite(usdPerUnit) || usdPerUnit <= 0) {
    return null;
  }

  const scale = 10 ** SPONSOR_AMOUNT_DECIMALS;
  const units = Math.floor((dollars / usdPerUnit) * scale) / scale;
  return units > 0 ? units : null;
}

/**
 * Platform units back to dollars, for displaying what will ACTUALLY be spent rather than what
 * was typed. The two differ by the rounding above, and showing the typed figure beside a
 * different charge is how a person ends up not recognising their own receipt.
 */
export function platformUnitsToDollars(
  units: number,
  usdPerUnit: number | undefined,
): number | null {
  if (!Number.isFinite(units) || units < 0) return null;
  if (usdPerUnit === undefined || !Number.isFinite(usdPerUnit) || usdPerUnit <= 0) {
    return null;
  }

  const scale = 10 ** SPONSOR_AMOUNT_DECIMALS;
  return Math.round(units * usdPerUnit * scale) / scale;
}

/** The amount as the route wants it: a plain decimal string in platform units. */
export function toPlatformUnitApiAmount(units: number): string {
  return units.toFixed(SPONSOR_AMOUNT_DECIMALS);
}

export function formatSponsorDollars(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

/**
 * The refusal a person reads when their amount is not usable. Says dollars, never a ticker —
 * the sentence "Enter a positive ARDA amount" was the last place the platform asked a sponsor to
 * think in a unit they never chose.
 */
export const SPONSOR_AMOUNT_HELP =
  `Enter an amount between ${formatSponsorDollars(SPONSOR_MINIMUM_DOLLARS)} and ` +
  `${formatSponsorDollars(SPONSOR_MAXIMUM_DOLLARS)}, to the cent.`;
