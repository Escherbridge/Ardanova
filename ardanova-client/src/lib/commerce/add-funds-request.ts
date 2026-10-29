import { z } from "zod";

import { looksFinal, type MoneyRefusalCode } from "~/lib/commerce/money-refusal";
import {
  getOnRampPresentation,
  type OnRampReport,
} from "~/lib/commerce/on-ramp-status";
import {
  readSavedRequest,
  recordAdmittedId,
  voidSavedRequest,
  type RequestFlow,
  type SavedRequest,
  type VoidAnswer,
} from "~/lib/commerce/persisted-request-key";

/** `addFunds.begin`'s body minus the key. */
export const addFundsWireSchema = z.object({
  amountMinorUnits: z.number().int().positive(),
});

export type AddFundsWire = z.infer<typeof addFundsWireSchema>;

/** One add-funds attempt at a time per person; the same amount is the same request. */
export const ADD_FUNDS_REQUEST_FLOW: RequestFlow<AddFundsWire> = {
  scope: "add-funds",
  wire: addFundsWireSchema,
  isSameRequest: (saved, draft) =>
    saved.amountMinorUnits === draft.amountMinorUnits,
};

/** A payment as the server reports it, id included. */
export type AddFundsPaymentReport = OnRampReport & { paymentId?: string | null };

/** Payment ids arrive compact (`?intent=`, 32 hex) or dashed; both name the same payment. */
export function isSamePaymentId(
  left: string | null | undefined,
  right: string | null | undefined,
): boolean {
  const normalize = (value: string | null | undefined) =>
    (value ?? "").trim().toLowerCase().replace(/-/gu, "");
  const normalizedLeft = normalize(left);
  return normalizedLeft.length > 0 && normalizedLeft === normalize(right);
}

/**
 * Whether a read of a payment ends the saved key: ONLY when it is that key's own payment and the
 * node says it is finished either way (`added` / `went-wrong`). "Unfinished" can still be resumed;
 * "unconfirmed" is not an answer. A return for some other payment never touches the key.
 */
export function endsSavedPayment(
  saved: SavedRequest<AddFundsWire>,
  payment: AddFundsPaymentReport,
): boolean {
  if (!isSamePaymentId(saved.admittedId, payment.paymentId)) return false;
  const outcome = getOnRampPresentation(payment).outcome;
  return outcome === "added" || outcome === "went-wrong";
}

/** A payment as the server returns it: the id is always present. */
export type AddFundsPaymentRead = OnRampReport & { paymentId: string };

/** `addFunds.voidRequest`'s answer, as the router validated it. */
export type AddFundsVoidAnswer =
  | { outcome: "voided" }
  | { outcome: "revealed"; payment: AddFundsPaymentRead };

/** The two server calls the add-funds key lifecycle needs; the page passes its tRPC calls. */
export interface AddFundsServer {
  readPayment: (paymentId: string) => Promise<AddFundsPaymentRead>;
  voidRequest: (requestKey: string) => Promise<AddFundsVoidAnswer>;
}

/** What the page shows after a key was settled. */
export type AddFundsKeySettlement =
  /** The server voided the key: nothing was or can be paid under it. */
  | { kind: "released" }
  /** The key stays, following this payment (or one not read yet). */
  | {
      kind: "tracking";
      saved: SavedRequest<AddFundsWire> | null;
      payment: AddFundsPaymentRead | null;
    }
  /** The key stays and nothing more is known; `error` is the failed server call, if any. */
  | { kind: "kept"; saved: SavedRequest<AddFundsWire> | null; error: unknown };

function asVoidAnswer(answer: AddFundsVoidAnswer): VoidAnswer<AddFundsPaymentRead> {
  return answer.outcome === "voided"
    ? { outcome: "voided" }
    : { outcome: "revealed", revealed: answer.payment };
}

/** Asks the server to void the saved key; on a reveal, the payment it names becomes the tracked one. */
async function voidOrReveal(
  storage: Storage,
  subjectId: string,
  requestKey: string,
  server: AddFundsServer,
): Promise<AddFundsKeySettlement> {
  const settled = await voidSavedRequest(
    storage,
    ADD_FUNDS_REQUEST_FLOW,
    subjectId,
    requestKey,
    async (key) => asVoidAnswer(await server.voidRequest(key)),
  );
  if (settled.kind !== "revealed") return settled;

  const payment = settled.revealed;
  const saved =
    recordAdmittedId(storage, ADD_FUNDS_REQUEST_FLOW, subjectId, requestKey, payment.paymentId) ??
    settled.saved;
  return { kind: "tracking", saved, payment };
}

/**
 * After Continue (or any resend) failed. Once the key names a payment, a code never decides it:
 * that payment is READ and the page follows its state (C-1). Without one, a final-looking code is
 * settled by void-or-reveal; anything else keeps the key. See `./AGENTS.md` §Persisted request keys.
 */
export async function settleAddFundsFailure(input: {
  storage: Storage;
  subjectId: string;
  requestKey: string;
  moneyCode: MoneyRefusalCode | null;
  server: AddFundsServer;
}): Promise<AddFundsKeySettlement> {
  const { storage, subjectId, requestKey, moneyCode, server } = input;
  const saved = readSavedRequest(storage, ADD_FUNDS_REQUEST_FLOW, subjectId);
  if (saved && saved.requestKey !== requestKey)
    return { kind: "kept", saved, error: null };

  if (saved?.admittedId) {
    try {
      return { kind: "tracking", saved, payment: await server.readPayment(saved.admittedId) };
    } catch {
      return { kind: "tracking", saved, payment: null };
    }
  }

  if (!looksFinal(moneyCode)) return { kind: "kept", saved, error: null };
  return voidOrReveal(storage, subjectId, requestKey, server);
}

/**
 * "Start a new payment instead" (M-1). Re-reads storage, then lets the server decide: `voided`
 * (an open page is expired first) releases the key; `revealed` keeps it and shows that payment.
 */
export async function setAsideSavedPayment(input: {
  storage: Storage;
  subjectId: string;
  server: AddFundsServer;
}): Promise<AddFundsKeySettlement | { kind: "none" }> {
  const saved = readSavedRequest(input.storage, ADD_FUNDS_REQUEST_FLOW, input.subjectId);
  if (!saved) return { kind: "none" };
  return voidOrReveal(input.storage, input.subjectId, saved.requestKey, input.server);
}

/**
 * Continue stays LOCKED while the saved key names a payment this page has no settled view of: not
 * read yet, unreadable, unconfirmed, or paid and still landing. An unfinished page is the one
 * exception - Continue resumes that same page under the same key.
 */
export function savedPaymentLocksContinue(
  saved: SavedRequest<AddFundsWire> | null,
  tracked: AddFundsPaymentReport | null | undefined,
): boolean {
  if (!saved?.admittedId) return false;
  if (!tracked || !isSamePaymentId(saved.admittedId, tracked.paymentId)) return true;
  return getOnRampPresentation(tracked).outcome !== "unfinished";
}
