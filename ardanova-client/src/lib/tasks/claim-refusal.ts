/**
 * Why a "Pick up task" claim was refused (.NET `TaskClaimCodes` on the claim's 409), carried to the browser as
 * `error.data.taskClaimCode`. See `src/lib/api/ardanova/endpoints/AGENTS.md` §Task claim.
 */
export const TASK_CLAIM_REFUSAL_CODES = [
  /** Someone else holds the task. */
  "TASK_TAKEN",
  /** The task is COMPLETED. */
  "TASK_DONE",
  /** An accepted bid's agreement, or escrow, owns who does the work. */
  "TASK_UNDER_AGREEMENT",
] as const;

export type TaskClaimRefusalCode = (typeof TASK_CLAIM_REFUSAL_CODES)[number];

const KNOWN_CODES: ReadonlySet<string> = new Set(TASK_CLAIM_REFUSAL_CODES);

/** The server-set claim code, or null when it is missing or not one this app knows (read the message then). */
export function parseTaskClaimRefusalCode(
  value: unknown,
): TaskClaimRefusalCode | null {
  return typeof value === "string" && KNOWN_CODES.has(value)
    ? (value as TaskClaimRefusalCode)
    : null;
}

/** The claim code on a thrown tRPC client error (`error.data.taskClaimCode`), or null for anything else. */
export function taskClaimRefusalOf(error: unknown): TaskClaimRefusalCode | null {
  if (typeof error !== "object" || error === null) return null;
  const data = (error as { data?: unknown }).data;
  return typeof data === "object" && data !== null
    ? parseTaskClaimRefusalCode((data as Record<string, unknown>).taskClaimCode)
    : null;
}
