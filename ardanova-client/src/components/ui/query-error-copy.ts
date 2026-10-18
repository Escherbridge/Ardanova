/** User-facing copy and retry policy for failed tRPC queries. See `src/components/ui/AGENTS.md` §query-error-state. */

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

/** React Query `retry` predicate: retry transient failures only, never auth/permission/validation ones. */
export function shouldRetryQuery(
  failureCount: number,
  error: unknown,
): boolean {
  const code = readErrorCode(error);
  if (code !== undefined && TERMINAL_CODES.has(code)) return false;
  return failureCount < MAX_TRANSIENT_RETRIES;
}
