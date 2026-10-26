/**
 * Turns the node's add-funds vocabulary into the three things a person is allowed to be told:
 * the money is in, it is on its way and we will write, or it did not work.
 *
 * Nothing here says wallet, chain, token, provider or gas. The node's own words
 * (`awaiting_payment`, `funds_applied`, ...) never reach a screen; they are matched here and
 * replaced. A word this file does not recognise is treated as "we cannot confirm it", never as
 * success - a browser redirect is not a receipt.
 *
 * This module also owns the two things the add-funds page is allowed to decide for itself: which
 * of the three readiness states a person is in, and whether a sentence the server sent is fit to
 * put in front of a sponsor.
 */

import {
  describeValueActionUnderReview,
  evaluateValueActionAccount,
  type ValueActionAccountStatus,
} from "~/lib/contracts/project-create-contract";

/** The only outcomes an add-funds return page may show. */
export type OnRampOutcome =
  | "unfinished"
  | "added"
  | "on-its-way"
  | "went-wrong"
  | "confirming"
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

/**
 * The answer is still being fetched, or the request for it failed. This is deliberately NOT
 * `unconfirmed`: nothing has been read yet, so "something went wrong" would be a guess in the
 * other direction. It keeps polling, and it never says the money arrived.
 */
const confirming: OnRampPresentation = {
  outcome: "confirming",
  heading: "We're confirming this",
  detail:
    "We're checking what happened to this payment. This page updates itself, so you can leave it open, and nothing changes on your account while we check.",
  poll: true,
};

/** What to show while the server has not yet answered for a payment we were sent back from. */
export function getOnRampConfirmingPresentation(): OnRampPresentation {
  return confirming;
}

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

/** What a person is told when this deployment cannot take a payment at all. No internals. */
export const ADD_FUNDS_UNAVAILABLE_MESSAGE =
  "Adding funds isn't switched on for this site yet. Nothing you do on this page can charge you.";

// ---------------------------------------------------------------------------
// Who may add funds
// ---------------------------------------------------------------------------

/**
 * The three verdicts the add-funds page may reach about an account - `ready`, `needs-setup`,
 * `under-review` - plus the two non-verdicts: the answer is still in flight (`checking`), or the
 * account could not be read at all (`unavailable`). The last two are not "needs setup", which is
 * the whole point of not collapsing this to a boolean: an unread account and a held account are
 * both refusals, and neither of them is this person's to fix.
 */
export type AddFundsReadinessState =
  | "ready"
  | "needs-setup"
  | "under-review"
  | "checking"
  | "not-open"
  | "unavailable";

export type AddFundsReadiness = {
  state: AddFundsReadinessState;
  /** Whether the Continue button may be pressed. Only "ready" is true; there is no fourth way in. */
  canContinue: boolean;
  /** The banner heading, or null when there is nothing to say. */
  heading: string | null;
  /** The banner body, or null when there is nothing to say. */
  detail: string | null;
  /** The button to offer alongside the banner. Never offered to someone under review. */
  action: { label: string; href: string } | null;
  /** Whether asking the server again could change the answer. Only a transient read failure. */
  canRetry: boolean;
};

/**
 * The tRPC code the account-status read refuses with when this deployment cannot open accounts
 * at all (backend 409, mapped in `routers/azoa-custodial-account.ts`). Retrying cannot change it.
 */
const NOT_CONFIGURED_ERROR_CODE = "PRECONDITION_FAILED";

/** Where somebody who genuinely has setup left to do is sent. */
const ACCOUNT_SETUP_HREF =
  "/settings/verification?returnTo=/portfolio/add-funds";

/**
 * Classifies an account for the add-funds page. Three verdicts, never a boolean - plus the two
 * honest non-answers above.
 *
 * The classification itself is `evaluateValueActionAccount`, which is the same rule the server's
 * own gate applies and orders the cases the same way; only the wording is add-funds specific. A
 * separate predicate here would be a second rule that can drift from the one that actually
 * refuses, and "under review" would collapse back into "complete account setup" - an instruction
 * a person who has finished every step cannot act on.
 *
 * `pending` is the request still being in flight. It is its own answer, so a person who is fine
 * never sees "finish your setup" flash at them before the first response lands.
 */
export function getAddFundsReadiness(
  status: ValueActionAccountStatus | null | undefined,
  options: {
    pending?: boolean;
    loadFailed?: boolean;
    /** The tRPC error code of the failed read, so a deterministic refusal is not called transient. */
    loadErrorCode?: string | null;
  } = {},
): AddFundsReadiness {
  if (options.pending === true && options.loadFailed !== true) {
    return {
      state: "checking",
      canContinue: false,
      heading: null,
      detail: null,
      action: null,
      canRetry: false,
    };
  }

  // Not a blip: the site cannot open accounts yet, so "try again shortly" would be a false promise.
  if (
    options.loadFailed === true &&
    options.loadErrorCode === NOT_CONFIGURED_ERROR_CODE
  ) {
    return {
      state: "not-open",
      canContinue: false,
      heading: "Adding money isn't open yet",
      detail:
        "Adding money isn't open on ArdaNova yet. Nothing was charged, and there is nothing you need to do - this isn't a problem with your account.",
      action: null,
      canRetry: false,
    };
  }

  const evaluation = evaluateValueActionAccount(status, {
    loadFailed: options.loadFailed,
  });

  switch (evaluation.state) {
    case "ready":
      return {
        state: "ready",
        canContinue: true,
        heading: null,
        detail: null,
        action: null,
        canRetry: false,
      };

    case "under-review":
      // No call to action: there is nothing left for this person to do, and pointing them at the
      // setup flow they already finished is the exact instruction they cannot act on.
      return {
        state: "under-review",
        canContinue: false,
        heading: "We're finishing one last check",
        detail: describeValueActionUnderReview(status?.reviewDueAt),
        action: null,
        canRetry: false,
      };

    case "service-unavailable":
      // The account was never read. Saying "finish your setup" would blame this person for the
      // site being unable to answer, so the words say what is actually true.
      return {
        state: "unavailable",
        canContinue: false,
        heading: "We couldn't check your account",
        detail:
          "We couldn't confirm your account just now, so we haven't started anything. This isn't a problem with your account - please try again.",
        action: null,
        canRetry: true,
      };

    default:
      return {
        state: "needs-setup",
        canContinue: false,
        heading: "Finish setting up your account first",
        detail:
          "Adding money needs your account set up and your identity confirmed. It takes a few minutes and you only do it once.",
        action: { label: "Set up my account", href: ACCOUNT_SETUP_HREF },
        canRetry: false,
      };
  }
}

// ---------------------------------------------------------------------------
// What a typed amount is allowed to be
// ---------------------------------------------------------------------------

export const ADD_FUNDS_MINIMUM_DOLLARS = 1;
export const ADD_FUNDS_MAXIMUM_DOLLARS = 10_000;

/**
 * Reads a typed amount as whole cents. Deliberately strict and locale-free: "1,50" is refused,
 * never read as 150 or 1.50, so a stray character can never become a different charge.
 */
export function parseDollarsToMinorUnits(raw: string): number | null {
  const trimmed = raw.trim().replace(/^\$/u, "");
  if (!/^\d{1,7}(\.\d{1,2})?$/u.test(trimmed)) return null;
  const cents = Math.round(Number(trimmed) * 100);
  return Number.isSafeInteger(cents) ? cents : null;
}

function formatWholeOrCents(dollars: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: dollars % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(dollars);
}

/** Why a typed amount cannot be used, in words that say how to fix it; null when it can. */
export function describeAddFundsAmountProblem(raw: string): string | null {
  const trimmed = raw.trim();
  if (trimmed.length === 0) return "Choose an amount, or type your own.";

  const cents = parseDollarsToMinorUnits(trimmed);
  if (cents === null) {
    const unsigned = trimmed.replace(/^\$/u, "");
    if (/[,\s]/u.test(unsigned)) {
      return "Use a dot for cents, like 40.50, with no spaces or commas.";
    }
    if (/^\d+\.\d{3,}$/u.test(unsigned)) {
      return "Amounts can only go down to whole cents - use at most two digits after the dot, like 40.50.";
    }
    return "Enter an amount in dollars, like 40 or 40.50.";
  }
  if (cents < ADD_FUNDS_MINIMUM_DOLLARS * 100)
    return `The smallest amount you can add is ${formatWholeOrCents(ADD_FUNDS_MINIMUM_DOLLARS)}.`;
  if (cents > ADD_FUNDS_MAXIMUM_DOLLARS * 100)
    return `The most you can add in one go is ${formatWholeOrCents(ADD_FUNDS_MAXIMUM_DOLLARS)}.`;
  return null;
}

// ---------------------------------------------------------------------------
// What a server sentence is allowed to say
// ---------------------------------------------------------------------------

/**
 * Words that must never reach a sponsor, in any sentence, from any layer. A message carrying one
 * of these is a message written for an operator, so it is replaced rather than shown.
 */
const OPERATOR_ONLY_WORDS = [
  "azoa",
  "rail",
  "on-ramp",
  "onramp",
  "node",
  "wallet",
  "chain",
  "blockchain",
  "token",
  "stripe",
  "gas fee",
  "mnemonic",
  "asset id",
  "avatar",
  "custodial",
  "gateway",
  "endpoint",
  "http",
] as const;

/**
 * Returns the server's own sentence when a sponsor may read it, and `fallback` when they may not.
 *
 * The server writes most of the money copy deliberately - amount bounds, the readiness refusals,
 * the "nothing was charged" lines - and replacing all of it wholesale would throw away the only
 * text that knows what actually happened. But the same channel also carries transport-shaped
 * failures written for an operator. This is the seam where that distinction is enforced, because
 * it is the last point before the sentence reaches a screen.
 */
export function toSponsorSafeMessage(
  serverMessage: string | null | undefined,
  fallback: string,
): string {
  const trimmed = (serverMessage ?? "").trim();

  // Two independent tests, because a denylist alone lets anything unanticipated through. A
  // fetch failure surfaces as "fetch failed" and a timeout as "Request timeout" - neither
  // contains a banned word, and neither is a sentence anybody wrote for a person to read.
  if (!looksLikeWrittenSentence(trimmed)) return fallback;

  const lowered = trimmed.toLowerCase();
  return OPERATOR_ONLY_WORDS.some((word) => lowered.includes(word))
    ? fallback
    : trimmed;
}

/** Deliberately strict: anything that is not plainly a sentence is treated as operator text. */
function looksLikeWrittenSentence(value: string): boolean {
  return (
    value.length >= 20 &&
    value.includes(" ") &&
    /[.!?]$/u.test(value) &&
    !/^HTTP \d{3}/u.test(value)
  );
}
