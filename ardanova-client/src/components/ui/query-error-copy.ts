/** User-facing copy and retry policy for failed tRPC queries, plus the cash-out gate copy. See `src/components/ui/AGENTS.md` §query-error-state. */

/** tRPC error codes that a retry cannot fix: the answer will be the same until the user acts. */
const TERMINAL_CODES = new Set([
  "UNAUTHORIZED",
  "FORBIDDEN",
  "NOT_FOUND",
  "BAD_REQUEST",
  "PARSE_ERROR",
  "UNPROCESSABLE_CONTENT",
]);

/** Maximum automatic retries for transient failures (network, 5xx, timeouts). */
export const MAX_TRANSIENT_RETRIES = 3;

export type QueryErrorKind =
  | "signed-out"
  | "forbidden"
  | "not-found"
  | "invalid"
  | "rate-limited"
  | "service";

export interface QueryErrorCopy {
  kind: QueryErrorKind;
  /** Short sentence explaining the cause; never contains a raw error code. */
  explanation: string;
  /** Whether a retry button can plausibly help. */
  retryable: boolean;
}

/** Reads the tRPC error code from a TRPCClientError-like value, if present. */
export function readErrorCode(error: unknown): string | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const data = (error as { data?: unknown }).data;
  if (typeof data !== "object" || data === null) return undefined;
  const code = (data as { code?: unknown }).code;
  return typeof code === "string" ? code : undefined;
}

/** Maps a query failure to plain-language copy that distinguishes "failed" from "empty". */
export function describeQueryError(error: unknown): QueryErrorCopy {
  switch (readErrorCode(error)) {
    case "UNAUTHORIZED":
      return {
        kind: "signed-out",
        explanation:
          "Your session has ended or was never started. Sign in again to load this.",
        retryable: false,
      };
    case "FORBIDDEN":
      return {
        kind: "forbidden",
        explanation:
          "Your account does not have permission to see this. Ask a project admin if you expected access.",
        retryable: false,
      };
    case "NOT_FOUND":
      return {
        kind: "not-found",
        explanation: "This record does not exist or has been removed.",
        retryable: false,
      };
    case "BAD_REQUEST":
    case "PARSE_ERROR":
    case "UNPROCESSABLE_CONTENT":
      return {
        kind: "invalid",
        explanation:
          "The request was rejected as invalid. Reload the page; if it keeps happening, report it.",
        retryable: false,
      };
    case "TOO_MANY_REQUESTS":
      return {
        kind: "rate-limited",
        explanation:
          "Too many requests in a short time. Wait a moment, then retry.",
        retryable: true,
      };
    default:
      return {
        kind: "service",
        explanation:
          "This is a connection or service error, not an empty result. Your data has not changed.",
        retryable: true,
      };
  }
}

/** Field names from the tRPC errorFormatter's flattened ZodError, if the failure was input validation. */
export function readInvalidFields(error: unknown): string[] {
  if (typeof error !== "object" || error === null) return [];
  const data = (error as { data?: unknown }).data;
  if (typeof data !== "object" || data === null) return [];
  const zodError = (data as { zodError?: unknown }).zodError;
  if (typeof zodError !== "object" || zodError === null) return [];
  const fieldErrors = (zodError as { fieldErrors?: unknown }).fieldErrors;
  if (typeof fieldErrors !== "object" || fieldErrors === null) return [];
  return Object.entries(fieldErrors as Record<string, unknown>)
    .filter(([, messages]) => Array.isArray(messages) && messages.length > 0)
    .map(([field]) => field);
}

/** "assigneeId" -> "assignee", "storyPoints" -> "story points". */
function humanizeField(field: string): string {
  return field
    .replace(/Id$/, "")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_.]+/g, " ")
    .toLowerCase()
    .trim();
}

function joinList(items: string[]): string {
  if (items.length <= 1) return items.join("");
  return `${items.slice(0, -1).join(", ")} and ${items.at(-1)}`;
}

/** Deployment/configuration or vendor wording written for operators, never for end users. */
const OPERATOR_MESSAGE =
  /\b(?:not configured|misconfigured|configuration|tenant|custodial|azoa|api key|webhook|environment variable)\b/i;

/** True when the server message is operator/config wording that must never be shown verbatim. */
export function isOperatorMessage(error: unknown): boolean {
  if (typeof error !== "object" || error === null) return false;
  const message = (error as { message?: unknown }).message;
  return typeof message === "string" && OPERATOR_MESSAGE.test(message);
}

/** Author-written TRPCError copy is shown; serialized payloads, stack-ish or multi-line text never are. */
function readableServerMessage(error: unknown): string | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const message = (error as { message?: unknown }).message;
  if (typeof message !== "string") return undefined;
  const trimmed = message.trim();
  if (trimmed.length === 0 || trimmed.length > 160) return undefined;
  if (/^[[{]/.test(trimmed) || trimmed.includes("\n")) return undefined;
  if (
    /exception|status code|stack|ECONN|fetch failed|undefined|null/i.test(
      trimmed,
    )
  ) {
    return undefined;
  }
  return /[.!?]$/.test(trimmed) ? trimmed : `${trimmed}.`;
}

const NOTHING_SAVED = "Nothing was saved.";

/** Plain-language sentence for a failed mutation, e.g. action = "create the PBI". See `src/components/ui/AGENTS.md` §mutation-error. */
export function describeMutationError(error: unknown, action: string): string {
  const lead = `We couldn't ${action}`;
  if (isOperatorMessage(error)) {
    return `${lead}: this is not switched on for this deployment yet. ${NOTHING_SAVED}`;
  }
  const fields = readInvalidFields(error).map(humanizeField);
  if (fields.length > 0) {
    const noun = fields.length === 1 ? "field" : "fields";
    return `${lead}: check the ${joinList(fields)} ${noun}. ${NOTHING_SAVED}`;
  }

  switch (readErrorCode(error)) {
    case "UNAUTHORIZED":
      return `${lead}: your session has ended. Sign in again, then retry. ${NOTHING_SAVED}`;
    case "FORBIDDEN":
      return `${lead}: ${readableServerMessage(error) ?? "your account does not have permission to do this."} ${NOTHING_SAVED}`;
    case "NOT_FOUND":
      return `${lead}: the record no longer exists or has been removed. ${NOTHING_SAVED}`;
    case "CONFLICT":
      return `${lead}: ${readableServerMessage(error) ?? "this changed while you were editing. Reload, then try again."} ${NOTHING_SAVED}`;
    case "TOO_MANY_REQUESTS":
      return `${lead}: too many requests in a short time. Wait a moment, then retry. ${NOTHING_SAVED}`;
    case "BAD_REQUEST":
    case "UNPROCESSABLE_CONTENT":
      return `${lead}: ${readableServerMessage(error) ?? "some of the details were rejected. Check the form and try again."} ${NOTHING_SAVED}`;
    default:
      return `${lead}: this is a connection or service error, not a problem with what you entered. ${NOTHING_SAVED} Try again.`;
  }
}

/** Plain sentence for a list that failed to load, never the server's text. See `src/components/ui/AGENTS.md` §list-load-failure. */
export function describeListLoadFailure(
  error: unknown,
  subject: string,
  emptyClaim: string,
): { sentence: string; retryable: boolean } {
  const copy = describeQueryError(error);
  if (copy.kind === "signed-out") {
    return {
      sentence: `Your session has ended. Sign in again to see ${subject}.`,
      retryable: false,
    };
  }
  return {
    sentence: `We couldn't load ${subject} just now. This does not mean ${emptyClaim}, and nothing has changed.`,
    retryable: copy.retryable,
  };
}

/** A figure that must never read as zero while its source is loading or failed. */
export function readFigure(
  query: { isLoading: boolean; error: unknown },
  format: () => string,
): string {
  if (query.isLoading) return "—";
  if (query.error) return "Not available";
  return format();
}

/** The one cash-out sentence, shared with add-funds ("You cannot cash out yet"). See `src/components/ui/AGENTS.md` §cash-out-gate. */
export const CASH_OUT_NOT_AVAILABLE =
  "Sending money to your bank isn't available on ArdaNova yet.";

export type CashOutState = "checking" | "unavailable" | "available" | "unknown";

interface CashOutRead {
  isLoading: boolean;
  error: unknown;
  /** The read's own `available` flag, when it answered. */
  available: boolean | undefined;
}

/** True when a cash-out read failed because this deployment has no way to send money to a bank. */
export function isCashOutGateError(error: unknown): boolean {
  if (readErrorCode(error) === "SERVICE_UNAVAILABLE") return true;
  if (isOperatorMessage(error)) return true;
  const message = (error as { message?: unknown } | null)?.message;
  return typeof message === "string" && /not switched on/i.test(message);
}

/** Folds the withdraw availability (and optionally destination) reads into one cash-out state. */
export function readCashOutState(
  availability: CashOutRead,
  destination?: CashOutRead,
): CashOutState {
  const reads = destination ? [availability, destination] : [availability];
  if (reads.some((read) => isCashOutGateError(read.error)))
    return "unavailable";
  if (reads.some((read) => read.available === false)) return "unavailable";
  if (reads.some((read) => read.isLoading)) return "checking";
  if (availability.error) return "unknown";
  return availability.available === true ? "available" : "unknown";
}

/** How the money pages describe the bank step; never promises a send the read has not confirmed. */
export function describeCashOutStep(state: CashOutState): string {
  switch (state) {
    case "unavailable":
      return `${CASH_OUT_NOT_AVAILABLE} Until it is, money in your balance stays here for backing projects.`;
    case "available":
      return "Money in your balance can be sent to your own bank account. You only set up where it goes once.";
    case "checking":
      return "Checking whether you can send money to your bank yet…";
    case "unknown":
      return "We couldn't check whether sending money to your bank is available yet. The withdraw page shows the latest.";
  }
}

/** The withdraw amount hint: names the real blocker instead of a setup step that cannot be taken. */
export function describeWithdrawBlocker(
  state: CashOutState,
  destination: { unreadable: boolean; ready: boolean },
): string {
  if (state === "unavailable") {
    return "Sending isn't available yet, so there is nothing to enter.";
  }
  if (state === "checking") return "Checking whether you can send money…";
  if (state === "unknown") {
    return "We couldn't check what you can send, so sending is paused for now.";
  }
  if (destination.unreadable) {
    return "We couldn't read your bank details, so sending is paused. Try again above.";
  }
  if (!destination.ready) {
    return "Tell us where to send your money first, using the button above.";
  }
  return "Enter an amount up to what is ready to send.";
}

/** React Query `retry` predicate: retry transient failures only, never auth/permission/validation ones. */
export function shouldRetryQuery(
  failureCount: number,
  error: unknown,
): boolean {
  const code = readErrorCode(error);
  if (code !== undefined && TERMINAL_CODES.has(code)) return false;
  return failureCount < MAX_TRANSIENT_RETRIES;
}
