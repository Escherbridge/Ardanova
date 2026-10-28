import { z } from "zod";

/**
 * Idempotency-key lifecycle for first-party money requests: a key persisted BEFORE it leaves the
 * browser, together with the exact request bytes it covers, so a lost response or a reload resumes
 * the SAME admission instead of minting a second one. Rules and rationale: `./AGENTS.md`
 * §Persisted request keys (ported from oasis-sleek `lib/persisted-request-key.ts`).
 */

const SAVED_REQUEST_VERSION = 2;
const STORAGE_PREFIX = `ardanova:commerce:request:v${SAVED_REQUEST_VERSION}:`;

/** One money flow's storage slot and the exact request body its key covers. */
export interface RequestFlow<TWire> {
  scope: string;
  /** The wire body, validated on read so a saved request is always one the router would accept. */
  wire: z.ZodType<TWire>;
  /** Whether a fresh draft is the SAME request the server binds this key to. */
  isSameRequest: (saved: TWire, draft: TWire) => boolean;
}

/** A caller-stable idempotency key saved before the request leaves, with the bytes it covers. */
export interface SavedRequest<TWire> {
  requestKey: string;
  /** The exact body sent under this key. A retry resends THESE bytes; it never recomputes them. */
  wire: TWire;
  /** What the person named, in cents. Display only, never sent. */
  displayMinorUnits: number;
  /** The server's id for what this key admitted, once it answered (an add-funds payment id). */
  admittedId?: string;
}

/** `ready`: send `request.wire` under its key. `pending`: an unresolved key for a different request. */
export type RequestClaim<TWire> =
  | { kind: "ready"; request: SavedRequest<TWire> }
  | { kind: "pending"; pending: SavedRequest<TWire> };

const savedEnvelopeSchema = z.object({
  version: z.literal(SAVED_REQUEST_VERSION),
  requestKey: z.string().min(1).max(128),
  wire: z.unknown(),
  displayMinorUnits: z.number().int().nonnegative(),
  admittedId: z.string().min(1).max(128).optional(),
});

/** Non-cryptographic FNV-1a over the subject id: keeps a raw user id out of the storage key. */
function subjectFingerprint(subjectId: string): string {
  let hash = 0x811c9dc5;
  for (let index = 0; index < subjectId.length; index += 1) {
    hash ^= subjectId.charCodeAt(index);
    hash = Math.imul(hash, 0x01000193);
  }
  return (hash >>> 0).toString(16).padStart(8, "0");
}

function subjectPrefix(subjectId: string): string {
  return `${STORAGE_PREFIX}${subjectFingerprint(subjectId)}:`;
}

/** The subject is IN the key, so two people's entries on one device coexist and never collide. */
function storageKey(scope: string, subjectId: string): string {
  return `${subjectPrefix(subjectId)}${scope}`;
}

/** This subject's saved request for this flow; null when there is none or it is unreadable. */
export function readSavedRequest<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
): SavedRequest<TWire> | null {
  try {
    const raw = storage.getItem(storageKey(flow.scope, subjectId));
    if (!raw) return null;

    const envelope = savedEnvelopeSchema.safeParse(JSON.parse(raw));
    if (!envelope.success) return null;
    const wire = flow.wire.safeParse(envelope.data.wire);
    if (!wire.success) return null;

    return {
      requestKey: envelope.data.requestKey,
      wire: wire.data,
      displayMinorUnits: envelope.data.displayMinorUnits,
      ...(envelope.data.admittedId ? { admittedId: envelope.data.admittedId } : {}),
    };
  } catch {
    return null;
  }
}

function writeSavedRequest<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
  request: SavedRequest<TWire>,
): void {
  try {
    storage.setItem(
      storageKey(flow.scope, subjectId),
      JSON.stringify({ version: SAVED_REQUEST_VERSION, ...request }),
    );
  } catch {
    // Private windows and a full quota both throw on write. The key still covers this one
    // request; it just will not survive a reload - degrading beats crashing.
  }
}

/**
 * Reuses the saved key - and its SAVED bytes - for the same request; never silently replaces an
 * unresolved key for a different one. Persists a fresh key BEFORE returning it, so the request
 * leaves only after the key that covers it is durable.
 */
export function claimRequestKey<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
  draft: TWire,
  displayMinorUnits: number,
): RequestClaim<TWire> {
  const saved = readSavedRequest(storage, flow, subjectId);
  if (saved) {
    return flow.isSameRequest(saved.wire, draft)
      ? { kind: "ready", request: saved }
      : { kind: "pending", pending: saved };
  }

  const fresh: SavedRequest<TWire> = {
    requestKey: crypto.randomUUID(),
    wire: draft,
    displayMinorUnits,
  };
  writeSavedRequest(storage, flow, subjectId, fresh);
  return { kind: "ready", request: fresh };
}

/** Notes what the server admitted under `requestKey`, if that key is still the saved one. */
export function recordAdmittedId<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
  requestKey: string,
  admittedId: string,
): SavedRequest<TWire> | null {
  const saved = readSavedRequest(storage, flow, subjectId);
  if (saved?.requestKey !== requestKey) return null;

  const updated = { ...saved, admittedId };
  writeSavedRequest(storage, flow, subjectId, updated);
  return updated;
}

/**
 * Forgets `requestKey` once the server has given a final answer for it. Keyed, so an answer that
 * arrives late for an OLD key can never erase a newer one saved since.
 */
export function clearSavedRequest<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
  requestKey: string,
): void {
  try {
    if (readSavedRequest(storage, flow, subjectId)?.requestKey !== requestKey) return;
    storage.removeItem(storageKey(flow.scope, subjectId));
  } catch {
    // Storage that cannot be written cannot be cleared either; reusing the same key again next
    // time is always safe, so there is nothing more to do here.
  }
}

/** Erases every saved money request belonging to `subjectId` on this device. Run at sign-out. */
export function clearSavedRequestsForSubject(
  storage: Storage,
  subjectId: string,
): void {
  try {
    const prefix = subjectPrefix(subjectId);
    const owned: string[] = [];
    for (let index = 0; index < storage.length; index += 1) {
      const key = storage.key(index);
      if (key?.startsWith(prefix)) owned.push(key);
    }
    for (const key of owned) storage.removeItem(key);
  } catch {
    // Blocked storage never held anything to erase.
  }
}

/**
 * The ONLY answer that ends a saved key: the BFF's `definite` money code, set by the server and
 * only where nothing was, or ever will be, admitted under the key. Anything else - a missing or
 * unknown code, a network drop, a 4xx with no code - keeps it.
 */
export function releasesSavedRequest(moneyCode: unknown): boolean {
  return moneyCode === "definite";
}

/** After a failed send: forgets the key on `definite`, keeps it on anything else. */
export function settleSavedRequestAfterFailure<TWire>(
  storage: Storage,
  flow: RequestFlow<TWire>,
  subjectId: string,
  requestKey: string,
  moneyCode: unknown,
): "released" | "kept" {
  if (!releasesSavedRequest(moneyCode)) return "kept";
  clearSavedRequest(storage, flow, subjectId, requestKey);
  return "released";
}
