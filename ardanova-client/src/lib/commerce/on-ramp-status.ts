/**
 * Turns the node's add-funds vocabulary into the three things a person is allowed to be told:
 * the money is in, it is on its way and we will write, or it did not work.
 *
 * Nothing here says wallet, chain, token, provider or gas. The node's own words
 * (`awaiting_payment`, `funds_applied`, ...) never reach a screen; they are matched here and
 * replaced. A word this file does not recognise is treated as "we cannot confirm it", never as
 * success - a browser redirect is not a receipt.
 */

/** The only outcomes an add-funds return page may show. */
export type OnRampOutcome =
  | "unfinished"
  | "added"
  | "on-its-way"
  | "went-wrong"
  | "unconfirmed";

export type OnRampPresentation = {
  outcome: OnRampOutcome;
  heading: string;
  detail: string;
  /** Whether the page should keep asking the server for a newer answer. */
  poll: boolean;
};

/** What the server reports about one add-funds payment. Both fields are the node's own words. */
export type OnRampReport = {
  status?: string | null;
  settlementState?: string | null;
};

const added: OnRampPresentation = {
  outcome: "added",
  heading: "Funds added",
  detail:
    "Your payment went through and the funds are in your account. You can back a project with them now.",
  poll: false,
};

const onItsWay: OnRampPresentation = {
  outcome: "on-its-way",
  heading: "We'll email you",
  detail:
    "Your payment went through. The funds usually appear within a few minutes; we'll email you as soon as they land. You can close this page.",
  poll: true,
};

const unfinished: OnRampPresentation = {
  outcome: "unfinished",
  heading: "Payment not finished",
  detail:
    "This payment was started but never completed, so nothing was charged and nothing was added. You can start again whenever you like.",
  poll: true,
};

const wentWrong: OnRampPresentation = {
  outcome: "went-wrong",
  heading: "Something went wrong",
  detail:
    "This payment did not go through, so nothing was added to your account. Nothing was charged. You can try again, or use a different card.",
  poll: false,
};

const unconfirmed: OnRampPresentation = {
  outcome: "unconfirmed",
  heading: "Something went wrong",
  detail:
    "We can't confirm this payment right now, so we won't say either way. If money left your account and nothing appears within an hour, contact support and quote this page.",
  poll: false,
};

/** The node's settlement words, which outrank its payment words when both are present. */
const settlementOutcomes: Record<string, OnRampPresentation> = {
  funds_applied: added,
  payment_received: onItsWay,
  payment_failed: wentWrong,
};

/** The node's payment words, used when settlement has not spoken yet. */
const statusOutcomes: Record<string, OnRampPresentation> = {
  starting: unfinished,
  awaiting_payment: unfinished,
  processing: onItsWay,
  complete: onItsWay,
  failed: wentWrong,
  expired: wentWrong,
};

function normalize(value: string | null | undefined): string {
  return (value ?? "").trim().toLowerCase();
}

/**
 * Maps one server-owned report onto the copy a person sees. `funds_applied` is the ONLY input
 * that produces "added"; everything unrecognised produces "we can't confirm it".
 */
export function getOnRampPresentation(
  report?: OnRampReport | null,
): OnRampPresentation {
  if (!report) return unconfirmed;

  const settlement = settlementOutcomes[normalize(report.settlementState)];
  if (settlement) return settlement;

  return statusOutcomes[normalize(report.status)] ?? unconfirmed;
}

/** Whether an add-funds payment has reached an answer that will not change on its own. */
export function isOnRampSettled(report?: OnRampReport | null): boolean {
  return !getOnRampPresentation(report).poll;
}

// ---------------------------------------------------------------------------
// The one seam this page cannot close on its own
// ---------------------------------------------------------------------------

/**
 * STUB SEAM - not a feature flag, and flipping it changes nothing.
 *
 * W6.1 built the server-side on-ramp gateway (`IFiatRailGateway` /
 * `AzoaFiatRailGateway`), but the HTTP surface that exposes it to this client does not exist
 * yet: there is no add-funds controller in `ArdaNova.API`, no `apiClient.fiatRail` endpoint and
 * no tRPC procedure. Until those land, the add-funds page can validate an amount and read a
 * return, but it cannot start or read a payment, so it says so plainly instead of pretending.
 */
export const ADD_FUNDS_SERVER_ROUTE_PENDING = true as const;

/** Exactly what has to exist before adding funds can work end to end. */
export const ADD_FUNDS_UNBLOCKED_BY =
  "ArdaNova.API needs an add-funds controller over IFiatRailGateway (POST begin, GET status), " +
  "an apiClient endpoint for it, and a tRPC procedure the client can call.";

/** What a person is told while the seam above is open. Plain words; no internals. */
export const ADD_FUNDS_UNAVAILABLE_MESSAGE =
  "Adding funds isn't switched on for this site yet. Nothing you do on this page can charge you.";
