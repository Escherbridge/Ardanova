/**
 * The existing ask a HELP_WANTED 409 names (`error.data.helpWantedExistingPostId`, set by the BFF's errorFormatter
 * from the API's `conflictId`), or null for any other failure. See ./AGENTS.md §Edit your ask.
 */
export function helpWantedExistingPostIdOf(error: unknown): string | null {
  if (typeof error !== "object" || error === null) return null;
  const data = (error as { data?: unknown }).data;
  if (typeof data !== "object" || data === null) return null;
  const postId = (data as Record<string, unknown>).helpWantedExistingPostId;
  return typeof postId === "string" && postId.length > 0 ? postId : null;
}
