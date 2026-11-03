// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from "vitest";
import {
  createKeyStore,
  MONEY_SCOPES,
  claimRequestKey,
  readSavedRequest,
} from "azoa-sdk/money";
import {
  recordAdmittedId,
  recordHeldForReview,
  claimRequestKey as claimLegacy,
  readSavedRequest as readLegacy,
} from "~/lib/commerce/persisted-request-key";
import { ADD_FUNDS_REQUEST_FLOW } from "~/lib/commerce/add-funds-request";
import { durableMoneyStore } from "./storage";

beforeEach(() => localStorage.clear());
describe("durable subject money storage", () => {
  it("reloads the same withdrawal amount and key and keeps another subject separate", () => {
    const first = durableMoneyStore("ada", localStorage);
    const saved = claimRequestKey(first, MONEY_SCOPES.cashOut, 2500);
    expect(saved.kind).toBe("ready");
    const reload = durableMoneyStore("ada", localStorage);
    expect(readSavedRequest(reload, MONEY_SCOPES.cashOut)).toEqual(
      saved.kind === "ready" ? saved.request : null,
    );
    expect(claimRequestKey(reload, MONEY_SCOPES.cashOut, 5000).kind).toBe(
      "pending",
    );
    expect(
      readSavedRequest(
        durableMoneyStore("ben", localStorage),
        MONEY_SCOPES.cashOut,
      ),
    ).toBeNull();
  });
  it("transfers an unresolved legacy checkout before the new SDK can send anything", () => {
    const old = claimLegacy(
      localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      "ada",
      { amountMinorUnits: 2500 },
      2500,
    );
    const store = durableMoneyStore("ada", localStorage);
    expect(readSavedRequest(store, MONEY_SCOPES.checkout)).toEqual(
      old.kind === "ready"
        ? { requestKey: old.request.requestKey, amountMinorUnits: 2500 }
        : null,
    );
    expect(readLegacy(localStorage, ADD_FUNDS_REQUEST_FLOW, "ada")).toBeNull();
  });
  it("does not overwrite two different unresolved checkout keys", () => {
    claimRequestKey(
      durableMoneyStore("ada", localStorage),
      MONEY_SCOPES.checkout,
      5000,
    );
    claimLegacy(
      localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      "ada",
      { amountMinorUnits: 2500 },
      2500,
    );
    expect(() => durableMoneyStore("ada", localStorage)).toThrow(
      /Two payment requests/,
    );
    expect(
      readLegacy(localStorage, ADD_FUNDS_REQUEST_FLOW, "ada"),
    ).not.toBeNull();
  });
});

describe("unreadable checkout migration", () => {
  it("does not overwrite a corrupt SDK slot with a valid legacy request", () => {
    const store = durableMoneyStore("ada", localStorage);
    store.write(MONEY_SCOPES.checkout, "corrupt SDK data");
    claimLegacy(
      localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      "ada",
      { amountMinorUnits: 2500 },
      2500,
    );
    expect(() => durableMoneyStore("ada", localStorage)).toThrow();
    expect(store.read(MONEY_SCOPES.checkout)).toBe("corrupt SDK data");
    expect(
      readLegacy(localStorage, ADD_FUNDS_REQUEST_FLOW, "ada"),
    ).not.toBeNull();
  });
  it("blocks a malformed legacy request instead of treating it as no payment", () => {
    claimLegacy(
      localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      "ada",
      { amountMinorUnits: 2500 },
      2500,
    );
    const legacySlot = Object.keys(localStorage).find((key) =>
      key.startsWith("ardanova:commerce:request:"),
    )!;
    localStorage.setItem(legacySlot, "corrupt legacy data");
    expect(() => durableMoneyStore("ada", localStorage)).toThrow();
    expect(localStorage.getItem(legacySlot)).toBe("corrupt legacy data");
  });
});

describe("exact migration evidence", () => {
  it.each(["different amount", "missing admitted id", "missing review hold"])(
    "preserves both records when the same key has %s",
    (kind) => {
      const claim = claimLegacy(
        localStorage,
        ADD_FUNDS_REQUEST_FLOW,
        "ada",
        { amountMinorUnits: 5000 },
        5000,
      );
      if (claim.kind !== "ready") throw new Error("Expected legacy request.");
      if (kind === "missing admitted id")
        recordAdmittedId(
          localStorage,
          ADD_FUNDS_REQUEST_FLOW,
          "ada",
          claim.request.requestKey,
          "payment-one",
        );
      if (kind === "missing review hold")
        recordHeldForReview(
          localStorage,
          ADD_FUNDS_REQUEST_FLOW,
          "ada",
          claim.request.requestKey,
          null,
        );
      const store = createKeyStore({ subject: "ada", storage: localStorage });
      const raw = JSON.stringify({
        requestKey: claim.request.requestKey,
        amountMinorUnits: kind === "different amount" ? 1000 : 5000,
      });
      store.write(MONEY_SCOPES.checkout, raw);
      expect(() => durableMoneyStore("ada", localStorage)).toThrow(
        /Two payment requests/,
      );
      expect(store.read(MONEY_SCOPES.checkout)).toBe(raw);
      expect(
        readLegacy(localStorage, ADD_FUNDS_REQUEST_FLOW, "ada"),
      ).not.toBeNull();
    },
  );
});

describe("migration readback proof", () => {
  it("retains legacy data when a same-key write does not retain its exact amount", () => {
    claimLegacy(
      localStorage,
      ADD_FUNDS_REQUEST_FLOW,
      "ada",
      { amountMinorUnits: 5000 },
      5000,
    );
    const storage: Storage = {
      get length() {
        return localStorage.length;
      },
      clear: () => localStorage.clear(),
      getItem: (key) => localStorage.getItem(key),
      key: (index) => localStorage.key(index),
      removeItem: (key) => localStorage.removeItem(key),
      setItem(key, raw) {
        if (key.startsWith("azoa-sdk:money:checkout:")) {
          const value = JSON.parse(raw) as Record<string, unknown>;
          localStorage.setItem(
            key,
            JSON.stringify({ ...value, amountMinorUnits: 1000 }),
          );
        } else localStorage.setItem(key, raw);
      },
    };
    expect(() => durableMoneyStore("ada", storage)).toThrow(
      /transferred safely/,
    );
    expect(
      readLegacy(localStorage, ADD_FUNDS_REQUEST_FLOW, "ada"),
    ).toMatchObject({ wire: { amountMinorUnits: 5000 } });
  });
});
