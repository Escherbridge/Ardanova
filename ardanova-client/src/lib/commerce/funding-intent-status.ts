/**
 * Turns the node's sponsorship vocabulary into the three things a person is allowed to be told:
 * it's confirmed, we're still working on it, or it did not go through. Mirrors the shape of
 * `on-ramp-status.ts` on purpose — an add-funds payment and a sponsorship are the same kind of
 * "durable server state behind a browser redirect" problem.
 *
 * `FundingIntentStatus` (ArdaNova.Domain.Models.Enums) is DRAFT, SETTLEMENT_PENDING, SETTLED,
 * HOLDING, REJECTED, CANCELLED, FAILED. There is no card state any more — see
 * `FundingIntentService.cs` AGENTS note "sponsorship".
 */

/** The only outcomes a sponsorship success page may show. */
export type FundingIntentOutcome = "confirmed" | "waiting" | "went-wrong";

export type FundingIntentPresentation = {
  outcome: FundingIntentOutcome;
  heading: string;
  detail: string;
  /** Whether the page should keep asking the server for a newer answer. */
  poll: boolean;
};

/** What the server reports about one sponsorship. Both fields are the node's own words. */
export type FundingIntentFacts = {
  status?: string | null;
  /** Consumer-safe reason text the server already wrote (`FundingIntent.holdingReasonDetail`). */
  reasonDetail?: string | null;
  /**
   * ISO timestamp the held stake becomes the sponsor's to move (`FundingIntent.releaseNotBefore`),
   * carried by `FundingIntentStatusDto`. Absent means the server has not set a date, and the
   * copy says so rather than naming one.
   */
  releaseNotBefore?: string | null;
};

function daysFromNow(iso: string): number | null {
  const targetMs = new Date(iso).getTime();
  if (!Number.isFinite(targetMs)) return null;
  const wholeDays = Math.ceil((targetMs - Date.now()) / (24 * 60 * 60 * 1000));
  return Math.max(0, wholeDays);
}

function releaseLabel(facts: FundingIntentFacts): string {
  // No invented window: when the server names no date, the copy stays vague rather than
  // promising a day nothing in the system committed to.
  const days = facts.releaseNotBefore
    ? daysFromNow(facts.releaseNotBefore)
    : null;
  if (days === null) return "after a short hold";
  if (days <= 0) return "today";
  return `in ${days} day${days === 1 ? "" : "s"}`;
}

function confirmed(facts: FundingIntentFacts): FundingIntentPresentation {
  return {
    outcome: "confirmed",
    heading: `Confirmed — yours ${releaseLabel(facts)}`,
    detail:
      "The node moved your ARDA and recorded your stake. It sits in a short hold, then it shows as available in your portfolio.",
    poll: false,
  };
}

const waiting: FundingIntentPresentation = {
  outcome: "waiting",
  heading: "We'll email you",
  detail:
    "We're still confirming this with the node. It usually finishes in a couple of minutes; we'll email you the result. You can close this page.",
  poll: true,
};

function wentWrong(detail?: string | null): FundingIntentPresentation {
  return {
    outcome: "went-wrong",
    heading: "Something went wrong",
    detail:
      detail?.trim() ||
      "This sponsorship could not be completed. Nothing was taken.",
    poll: false,
  };
}

const unavailable = wentWrong(
  "We can't confirm this sponsorship right now. A browser redirect is not a settlement receipt — if your balance looks wrong, contact support and quote this page.",
);

/** Maps only server-owned sponsorship facts to conservative return-page copy. */
export function getFundingIntentPresentation(
  facts?: FundingIntentFacts | null,
): FundingIntentPresentation {
  if (!facts?.status) return unavailable;

  switch (facts.status) {
    case "SETTLED":
      return confirmed(facts);
    case "DRAFT":
    case "SETTLEMENT_PENDING":
      return waiting;
    case "HOLDING":
    case "REJECTED":
    case "CANCELLED":
    case "FAILED":
      return wentWrong(facts.reasonDetail);
    default:
      return unavailable;
  }
}

// ---------------------------------------------------------------------------
// Server error codes -> human copy
// ---------------------------------------------------------------------------

/** The subset of a tRPC client error this module needs. Structural, so any tRPC error fits it. */
export type FundingRequestErrorLike = {
  message?: string | null;
  data?: { code?: string | null } | null;
} | null;

/**
 * `Result<T>.Conflict/.ValidationError/.NotFound` on the .NET side already write full sentences
 * ("This project has no price yet, so nothing was taken.") into `.Error`, which lands on
 * `error.message` here — so the server's own words are used whenever present. This table is only
 * the fallback for the codes a message-less failure (a network drop, an unauthenticated session)
 * can still carry.
 */
const CODE_FALLBACKS: Record<string, string> = {
  UNAUTHORIZED: "Please sign in again, then try supporting this project.",
  FORBIDDEN: "You don't have access to complete this sponsorship.",
  NOT_FOUND:
    "We couldn't find this project's funding setup. It may have changed since you loaded this page.",
  BAD_REQUEST: "That amount wasn't valid. Adjust it and try again.",
  CONFLICT: "This sponsorship couldn't go through right now.",
  TIMEOUT: "That took too long to answer. Nothing was taken — try again.",
  TOO_MANY_REQUESTS: "Too many attempts in a row. Wait a moment and try again.",
  BAD_GATEWAY: "We couldn't reach the pricing service. Nothing was taken.",
  INTERNAL_SERVER_ERROR: "Something went wrong on our end. Nothing was taken.",
};

const GENERIC_FUNDING_ERROR =
  "Something went wrong, and nothing was taken. Try again in a moment.";

/** Server error codes, mapped to copy a sponsor can act on. Prefers the server's own sentence. */
export function describeFundingError(error?: FundingRequestErrorLike): string {
  const message = error?.message?.trim();
  if (message) return message;

  const code = error?.data?.code;
  if (code && code in CODE_FALLBACKS) return CODE_FALLBACKS[code];

  return GENERIC_FUNDING_ERROR;
}
