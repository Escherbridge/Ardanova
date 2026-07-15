import "server-only";

import { AsyncLocalStorage } from "node:async_hooks";
import { createHash, createHmac, randomUUID } from "node:crypto";

const actorStorage = new AsyncLocalStorage<Actor>();
const assertionLifetimeSeconds = 90;

export type Actor = {
  subject: string;
  role?: string;
};

export type ActorAssertionRequest = {
  method: string;
  url: string;
  body?: Uint8Array;
  contentType?: string | null;
  idempotencyKey?: string | null;
};

type ActorAssertionPayload = {
  version: 2;
  issuer: "ardanova-next-bff";
  audience: "ardanova-api";
  subject: string;
  role?: string;
  method: string;
  requestTarget: string;
  contentType: string;
  bodySha256: string;
  idempotencyKey?: string;
  jti: string;
  issuedAt: number;
  expiresAt: number;
};

/** Runs a BFF procedure with the actor eligible for backend assertion signing. */
export function runWithActorAssertion<T>(actor: Actor, callback: () => T): T {
  return actorStorage.run(actor, callback);
}

/** Returns whether the current BFF execution must use a signed actor assertion. */
export function isActorAssertionActive(): boolean {
  return actorStorage.getStore() !== undefined;
}

/** Normalizes the single Content-Type representation included in a signed request. */
export function normalizeActorAssertionContentType(value: string | null | undefined): string {
  if (!value) return "";
  return value
    .split(";")
    .map((segment) => segment.trim().toLowerCase())
    .join(";");
}

/** Produces a single-use assertion bound to exact BFF request bytes and metadata. */
export function getActorAssertion(request: ActorAssertionRequest): string | undefined {
  const actor = actorStorage.getStore();
  if (!actor) return undefined;

  const signingKey = process.env.ACTOR_ASSERTION_HMAC_KEY;
  if (!signingKey || Buffer.byteLength(signingKey, "utf8") < 32) {
    throw new Error("ACTOR_ASSERTION_HMAC_KEY must contain at least 32 bytes.");
  }

  const url = new URL(request.url, "http://actor-assertion.invalid");
  const body = request.body ?? new Uint8Array();
  const now = Math.floor(Date.now() / 1000);
  const payload: ActorAssertionPayload = {
    version: 2,
    issuer: "ardanova-next-bff",
    audience: "ardanova-api",
    subject: actor.subject,
    ...(actor.role ? { role: actor.role } : {}),
    method: request.method.toUpperCase(),
    requestTarget: `${url.pathname}${url.search}`,
    contentType: normalizeActorAssertionContentType(request.contentType),
    bodySha256: createHash("sha256").update(body).digest("hex"),
    ...(request.idempotencyKey ? { idempotencyKey: request.idempotencyKey } : {}),
    jti: randomUUID(),
    issuedAt: now,
    expiresAt: now + assertionLifetimeSeconds,
  };
  const encodedPayload = Buffer.from(JSON.stringify(payload)).toString("base64url");
  const signature = createHmac("sha256", signingKey).update(encodedPayload, "ascii").digest("base64url");
  return `${encodedPayload}.${signature}`;
}
