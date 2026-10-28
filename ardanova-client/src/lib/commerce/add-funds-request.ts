import { z } from "zod";

import {
  getOnRampPresentation,
  type OnRampReport,
} from "~/lib/commerce/on-ramp-status";
import type {
  RequestFlow,
  SavedRequest,
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

/**
 * Whether a person's explicit "start a new payment instead" may set the saved payment aside: any
 * outcome the node actually stated. An open, unpaid page charged nothing and is safe to leave;
 * a paid one is already accounted for. Only "unconfirmed" keeps the key.
 */
export function canSetAsideSavedPayment(payment: AddFundsPaymentReport): boolean {
  return getOnRampPresentation(payment).outcome !== "unconfirmed";
}
