import { z } from "zod";

/**
 * Idempotency-key lifecycle for first-party money requests: a key persisted BEFORE it leaves the
 * browser, so a lost response or a reload resumes the SAME admission instead of minting a second
 * one. Ported from oasis-sleek's `lib/persisted-request-key.ts` (rules: that repo's
 * `src/app/(dashboard)/AGENTS.md` §first-party-money); storage access follows this repo's own
 * `components/ai/nova-refine-storage.ts` convention — an explicit `Storage` parameter, ownership
 * checked on read, a mismatched or unreadable entry is dropped rather than shown.
 *
 * One slot per flow (`scope`), never per subject in the key name: the owner lives inside the
 * stored value as a non-cryptographic fingerprint, exactly like `nova-refine-storage`'s `owner`
 * field, so a shared device never resumes a different signed-in person's request — it erases it
 * on sight instead, the same "never shown to the next member, and is erased" behaviour that file
 * already proved for Nova's refinement draft.
 */

const SAVED_REQUEST_VERSION = 1;

const savedRequestSchema = z.object({
  version: z.literal(SAVED_REQUEST_VERSION),
  ownerFingerprint: z.string().min(1).max(64),
  requestKey: z.string().min(1).max(128),
  amountMinorUnits: z.number().int().nonnegative(),
});

/** A caller-stable idempotency key saved before the request leaves, for the exact amount it covers. */
export interface SavedRequest {
  requestKey: string;
  amountMinorUnits: number;
}

/** `ready`: send with this key. `pending`: an unresolved key for a different amount must be resolved first. */
export type RequestClaim =
  | { kind: "ready"; request: SavedRequest }
  | { kind: "pending"; pending: SavedRequest };

function storageKey(scope: string): string {
  return `ardanova:commerce:request:v${SAVED_REQUEST_VERSION}:${scope}`;
}

/**
 * Non-cryptographic FNV-1a over the signed-in subject id. This is not a security boundary — it
 * only keeps a raw user id out of the stored payload, mirroring `owner` in `nova-refine-storage`
 * closely enough to reuse that file's proven "drop it, never show it" rule on mismatch.
 */
function subjectFingerprint(subjectId: string): string {
  let hash = 0x811c9dc5;
  for (let index = 0; index < subjectId.length; index += 1) {
    hash ^= subjectId.charCodeAt(index);
    hash = Math.imul(hash, 0x01000193);
  }
  return (hash >>> 0).toString(16).padStart(8, "0");
}

/**
 * This subject's saved request for this flow. Null when there is none, it is unreadable, or it
 * belongs to a different signed-in subject on this device — and a mismatch is erased rather than
 * left behind, so the next claim for this scope starts clean.
 */
export function readSavedRequest(
  storage: Storage,
  scope: string,
  subjectId: string,
): SavedRequest | null {
  try {
    const raw = storage.getItem(storageKey(scope));
    if (!raw) return null;

    const parsed = savedRequestSchema.safeParse(JSON.parse(raw));
    if (!parsed.success || parsed.data.ownerFingerprint !== subjectFingerprint(subjectId)) {
      storage.removeItem(storageKey(scope));
      return null;
    }

    return {
      requestKey: parsed.data.requestKey,
      amountMinorUnits: parsed.data.amountMinorUnits,
    };
  } catch {
    return null;
  }
}

function writeSavedRequest(
  storage: Storage,
  scope: string,
  subjectId: string,
  request: SavedRequest,
): void {
  try {
    storage.setItem(
      storageKey(scope),
      JSON.stringify({
        version: SAVED_REQUEST_VERSION,
        ownerFingerprint: subjectFingerprint(subjectId),
        ...request,
      }),
    );
  } catch {
    // Private windows and a full quota both throw on write. The freshly minted key below still
    // works for this one request; it just will not survive a reload — degrading beats crashing.
  }
}

/**
 * Reuses the saved key for the same amount; never silently replaces an unresolved key for a
 * different amount, because an overwrite there is how one lost request becomes two admissions.
 * Persists the fresh key BEFORE returning it, so the caller's request leaves only after the key
 * that covers it is already durable.
 */
export function claimRequestKey(
  storage: Storage,
  scope: string,
  subjectId: string,
  amountMinorUnits: number,
): RequestClaim {
  const saved = readSavedRequest(storage, scope, subjectId);
  if (saved) {
    return saved.amountMinorUnits === amountMinorUnits
      ? { kind: "ready", request: saved }
      : { kind: "pending", pending: saved };
  }

  const fresh: SavedRequest = { requestKey: crypto.randomUUID(), amountMinorUnits };
  writeSavedRequest(storage, scope, subjectId, fresh);
  return { kind: "ready", request: fresh };
}

/** Forgets the saved key once the server has given a definite answer for it. */
export function clearSavedRequest(storage: Storage, scope: string): void {
  try {
    storage.removeItem(storageKey(scope));
  } catch {
    // Storage that cannot be written cannot be cleared either; reusing the same key again next
    // time is always safe, so there is nothing more to do here.
  }
}

/**
 * A refusal that is a definite answer for the key, in this app's own tRPC error-code vocabulary
 * (see `~/lib/commerce/funding-intent-status.ts` `CODE_FALLBACKS` and
 * `~/server/api/routers/add-funds.ts` `toAddFundsError`, which both already draw this line for
 * the same two routers). Network failures, `UNAUTHORIZED`, `CONFLICT`/`PRECONDITION_FAILED` (the
 * request may still be in progress), `TOO_MANY_REQUESTS` and `*_SERVER_ERROR`/`BAD_GATEWAY` keep
 * the key, because the request may still have been admitted upstream.
 */
export function isDefiniteRefusal(code: string | null | undefined): boolean {
  return (
    code === "BAD_REQUEST" ||
    code === "FORBIDDEN" ||
    code === "NOT_FOUND" ||
    code === "UNPROCESSABLE_CONTENT"
  );
}
