import { TRPCError } from "@trpc/server";

/**
 * Rides on a `TRPCError`'s `cause` so `errorFormatter` (`./trpc.ts`) can expose the existing
 * HELP_WANTED post's id as `error.data.helpWantedExistingPostId`. The composer reads this to offer
 * "Edit your ask" instead of a plain conflict error. Mirrors `money-refusal-error.ts`'s
 * `MoneyRefusalCause`. See SR-03 Phase 3 plan §2 HELP_WANTED rules: `Post.taskId` is `[unique]`, so a
 * second ask on the same task is a 23505 the API maps to 409 carrying the first post's id.
 */
export class HelpWantedConflictCause extends Error {
  constructor(readonly existingPostId: string) {
    super(`A HELP_WANTED post already exists for this task: ${existingPostId}`);
    this.name = "HelpWantedConflictCause";
  }
}

/** A 409 from creating a HELP_WANTED post, carrying the post id the unique constraint named (if any). */
export function helpWantedConflictError(
  existingPostId: string | undefined,
  message: string,
): TRPCError {
  return new TRPCError({
    code: "CONFLICT",
    message,
    ...(existingPostId
      ? { cause: new HelpWantedConflictCause(existingPostId) }
      : {}),
  });
}

/** The existing post id a HELP_WANTED conflict named, or null. */
export function readHelpWantedExistingPostId(error: TRPCError): string | null {
  return error.cause instanceof HelpWantedConflictCause
    ? error.cause.existingPostId
    : null;
}
