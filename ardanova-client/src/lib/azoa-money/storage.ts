import {
  createKeyStore,
  MONEY_SCOPES,
  readSavedRequest as readSdkRequest,
} from "azoa-sdk/money";
import type { KeyStore } from "azoa-sdk/money";
import { ADD_FUNDS_REQUEST_FLOW } from "~/lib/commerce/add-funds-request";
import {
  clearSavedRequest,
  readSavedRequest,
} from "~/lib/commerce/persisted-request-key";

/** Creates durable, subject-bound storage and transfers any unresolved legacy checkout. */
export function durableMoneyStore(subject: string, storage: Storage): KeyStore {
  const store = createKeyStore({ subject, storage });
  const current = readSdkRequest(store, MONEY_SCOPES.checkout, true);
  const legacy = readSavedRequest(
    storage,
    ADD_FUNDS_REQUEST_FLOW,
    subject,
    true,
  );
  if (legacy) {
    if (
      current &&
      (current.requestKey !== legacy.requestKey ||
        current.amountMinorUnits !== legacy.wire.amountMinorUnits ||
        (legacy.admittedId !== undefined &&
          current.serverId !== legacy.admittedId) ||
        (legacy.heldForReview && current.needsReview !== true))
    )
      throw new Error(
        "Two payment requests need review before another can start.",
      );
    if (!current)
      store.write(
        MONEY_SCOPES.checkout,
        JSON.stringify({
          requestKey: legacy.requestKey,
          amountMinorUnits: legacy.wire.amountMinorUnits,
          ...(legacy.admittedId ? { serverId: legacy.admittedId } : {}),
          ...(legacy.heldForReview ? { needsReview: true } : {}),
        }),
      );
    const restored = readSdkRequest(store, MONEY_SCOPES.checkout, true);
    if (
      !restored ||
      restored.requestKey !== legacy.requestKey ||
      restored.amountMinorUnits !== legacy.wire.amountMinorUnits ||
      (legacy.admittedId !== undefined &&
        restored.serverId !== legacy.admittedId) ||
      (legacy.heldForReview && restored.needsReview !== true)
    )
      throw new Error("The saved payment could not be transferred safely.");
    clearSavedRequest(
      storage,
      ADD_FUNDS_REQUEST_FLOW,
      subject,
      legacy.requestKey,
    );
  }
  return store;
}
