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
        explanation: "Too many requests in a short time. Wait a moment, then retry.",
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

/** React Query `retry` predicate: retry transient failures only, never auth/permission/validation ones. */
export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  const code = readErrorCode(error);
  if (code !== undefined && TERMINAL_CODES.has(code)) return false;
  return failureCount < MAX_TRANSIENT_RETRIES;
}
