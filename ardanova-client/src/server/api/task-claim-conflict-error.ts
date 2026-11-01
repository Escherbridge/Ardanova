import { TRPCError } from "@trpc/server";

import {
  parseTaskClaimRefusalCode,
  type TaskClaimRefusalCode,
} from "~/lib/tasks/claim-refusal";

/**
 * Rides on a `TRPCError`'s `cause` so `errorFormatter` (`./trpc.ts`) can expose a refused claim's code as
 * `error.data.taskClaimCode`. Mirrors `HelpWantedConflictCause` and `MoneyRefusalCause`.
 */
export class TaskClaimRefusalCause extends Error {
  constructor(readonly claimCode: TaskClaimRefusalCode) {
    super(`Task claim refused: ${claimCode}`);
    this.name = "TaskClaimRefusalCause";
  }
}

/** A claim's 409 as CONFLICT, carrying the API's `code` when it is one this app knows (and none otherwise). */
export function taskClaimConflictError(
  code: string | undefined,
  message: string,
): TRPCError {
  const claimCode = parseTaskClaimRefusalCode(code);
  return new TRPCError({
    code: "CONFLICT",
    message,
    ...(claimCode ? { cause: new TaskClaimRefusalCause(claimCode) } : {}),
  });
}

/** The claim code a procedure attached to its error, or null. */
export function readTaskClaimCode(
  error: TRPCError,
): TaskClaimRefusalCode | null {
  return error.cause instanceof TaskClaimRefusalCause
    ? error.cause.claimCode
    : null;
}
