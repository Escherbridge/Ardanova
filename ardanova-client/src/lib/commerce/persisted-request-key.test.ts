import { describe, expect, it } from "vitest";

import {
  ADD_FUNDS_REQUEST_FLOW,
  endsSavedPayment,
  savedPaymentLocksContinue,
} from "./add-funds-request";
import {
  claimRequestKey,
  clearResolvedRequestsForSubject,
  clearSavedRequest,
  readSavedRequest,
  recordAdmittedId,
  voidSavedRequest,
} from "./persisted-request-key";
import {
  draftSponsorshipRequest,
  sponsorshipRequestFlow,
} from "./sponsorship-request";
import { TabStorage } from "./testing/tab-storage";

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const addFunds = ADD_FUNDS_REQUEST_FLOW;

function claimAddFunds(tab: Storage, subject: string, cents: number) {
  return claimRequestKey(
    tab,
    addFunds,
    subject,
    { amountMinorUnits: cents },
    cents,
  );
}

function readyRequest(claim: ReturnType<typeof claimAddFunds>) {
  if (claim.kind !== "ready") throw new Error("expected a ready claim");
  return claim.request;
}

describe("claimRequestKey persistence", () => {
  it("persists a freshly minted key and its exact bytes BEFORE returning it", () => {
    const tab = new TabStorage();
    const request = readyRequest(claimAddFunds(tab, "user-1", 2_500));

    // Read back independently of the function under test, proving the write already landed.
    expect(readSavedRequest(tab, addFunds, "user-1")).toEqual(request);
    expect(request.wire).toEqual({ amountMinorUnits: 2_500 });
  });

  it("returns the same key across a remount over the same storage (the reload a ref lost)", () => {
    const tab = new TabStorage();
    const first = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    const second = readyRequest(claimAddFunds(tab, "user-1", 2_500));

    expect(second.requestKey).toBe(first.requestKey);
  });

  it("keeps two people's saved requests side by side on one device, and never hands one to the other", () => {
    const tab = new TabStorage();
    const first = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    const second = readyRequest(claimAddFunds(tab, "user-2", 2_500));

    expect(second.requestKey).not.toBe(first.requestKey);
    // Reading user-2's slot erased nothing: user-1 still resumes their own key.
    expect(readSavedRequest(tab, addFunds, "user-1")?.requestKey).toBe(
      first.requestKey,
    );
  });

  it("signing out keeps every unresolved saved request and drops only unreadable leftovers (M-6)", () => {
    const tab = new TabStorage();
    const payment = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    const sponsorship = claimRequestKey(
      tab,
      sponsorshipRequestFlow("config-1"),
      "user-1",
      {
        projectTokenConfigId: "config-1",
        amount: "100.00",
        disclosureVersion: "d1",
        displayNameConsent: false,
      },
      100,
    );
    const other = readyRequest(claimAddFunds(tab, "user-2", 2_500));
    // A corrupt slot under the signing-out person's prefix: it protects nothing and is never read.
    const storedKeys = Array.from(
      { length: tab.length },
      (_, index) => tab.key(index)!,
    );
    const user1Slot = storedKeys.find((key) =>
      tab.getItem(key)!.includes(payment.requestKey),
    )!;
    const corruptKey = user1Slot.replace(
      /:add-funds$/u,
      ":invest:config-corrupt",
    );
    tab.setItem(corruptKey, "{not json");

    clearResolvedRequestsForSubject(tab, "user-1");

    // A payment that may still clear is never forgotten at sign-out; the next person cannot see it.
    expect(readSavedRequest(tab, addFunds, "user-1")?.requestKey).toBe(
      payment.requestKey,
    );
    expect(
      readSavedRequest(tab, sponsorshipRequestFlow("config-1"), "user-1"),
    ).toEqual(sponsorship.kind === "ready" ? sponsorship.request : null);
    expect(readSavedRequest(tab, addFunds, "user-2")?.requestKey).toBe(
      other.requestKey,
    );
    expect(tab.getItem(corruptKey)).toBeNull();
  });

  it("a changed naming choice is a different request, so it never borrows the saved key (L-3)", () => {
    const tab = new TabStorage();
    const flow = sponsorshipRequestFlow("config-1");
    const wire = {
      projectTokenConfigId: "config-1",
      amount: "100.00",
      disclosureVersion: "d1",
      displayNameConsent: false,
    };
    const first = claimRequestKey(tab, flow, "user-1", wire, 100);

    const named = claimRequestKey(
      tab,
      flow,
      "user-1",
      { ...wire, displayNameConsent: true },
      100,
    );

    expect(named).toEqual({
      kind: "pending",
      pending: first.kind === "ready" ? first.request : null,
    });
  });

  it("returns pending for a different request, and leaves the saved key untouched", () => {
    const tab = new TabStorage();
    const first = readyRequest(claimAddFunds(tab, "user-1", 10_000));
    const second = claimAddFunds(tab, "user-1", 25_000);

    expect(second).toEqual({ kind: "pending", pending: first });
    expect(readSavedRequest(tab, addFunds, "user-1")).toEqual(first);
  });

  it("a late answer for an OLD key never erases the key saved since", () => {
    const tab = new TabStorage();
    const old = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    clearSavedRequest(tab, addFunds, "user-1", old.requestKey);
    const fresh = readyRequest(claimAddFunds(tab, "user-1", 2_500));

    clearSavedRequest(tab, addFunds, "user-1", old.requestKey);

    expect(fresh.requestKey).not.toBe(old.requestKey);
    expect(readSavedRequest(tab, addFunds, "user-1")?.requestKey).toBe(
      fresh.requestKey,
    );
  });

  it("degrades to a usable, unpersisted key when storage throws on every call", () => {
    const blocked = new TabStorage(true);

    const request = readyRequest(claimAddFunds(blocked, "user-1", 2_500));
    expect(UUID_PATTERN.test(request.requestKey)).toBe(true);
    expect(() => readSavedRequest(blocked, addFunds, "user-1")).not.toThrow();
    expect(() =>
      clearResolvedRequestsForSubject(blocked, "user-1"),
    ).not.toThrow();
  });
});

describe("voidSavedRequest releases a key on the server's `voided` and on nothing else", () => {
  it("never voids, or clears, a key another tab replaced since this page read it", async () => {
    const tab = new TabStorage();
    const stale = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    clearSavedRequest(tab, addFunds, "user-1", stale.requestKey);
    const fresh = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    const voided: string[] = [];

    const settled = await voidSavedRequest(
      tab,
      addFunds,
      "user-1",
      stale.requestKey,
      async (key) => {
        voided.push(key);
        return { outcome: "voided" };
      },
    );

    expect(settled.kind).toBe("kept");
    expect(voided).toEqual([]);
    expect(readSavedRequest(tab, addFunds, "user-1")?.requestKey).toBe(
      fresh.requestKey,
    );
  });
});

describe("sponsorship keys survive a rate move", () => {
  it("resends the identical saved amount after ARDA re-prices, and never the re-priced one under that key", () => {
    const tab = new TabStorage();
    const flow = sponsorshipRequestFlow("config-1");
    const draftAt = (usdPerArda: number) =>
      draftSponsorshipRequest({
        projectTokenConfigId: "config-1",
        dollars: 100,
        usdPerArda,
        displayNameConsent: false,
      })!;

    const sent = claimRequestKey(tab, flow, "user-1", draftAt(0.01), 10_000);
    if (sent.kind !== "ready") throw new Error("expected a ready claim");
    expect(sent.request.wire.amount).toBe("10000.00");

    // The answer is lost and the rate moves before the person presses Retry.
    const repriced = draftAt(0.0125);
    expect(repriced.amount).toBe("8000.00");

    // Retry / Resume read the saved request: the same key with the SAME amount string.
    expect(readSavedRequest(tab, flow, "user-1")).toEqual(sent.request);
    // Supporting again at the new rate is a different request, so it never borrows the key.
    expect(claimRequestKey(tab, flow, "user-1", repriced, 10_000)).toEqual({
      kind: "pending",
      pending: sent.request,
    });
  });
});

describe("add funds clears a saved key only on its own payment's final answer", () => {
  const savedWithPayment = (tab: Storage) => {
    const request = readyRequest(claimAddFunds(tab, "user-1", 2_500));
    return recordAdmittedId(
      tab,
      addFunds,
      "user-1",
      request.requestKey,
      "0f8fad5bd9cb469fa16570867728950e",
    )!;
  };

  it.each([
    [
      "its own payment, funds applied",
      true,
      "0f8fad5b-d9cb-469f-a165-70867728950e",
      { settlementState: "funds_applied" },
    ],
    [
      "its own payment, failed",
      true,
      "0f8fad5bd9cb469fa16570867728950e",
      { status: "failed" },
    ],
    [
      "its own payment, expired unpaid",
      true,
      "0f8fad5bd9cb469fa16570867728950e",
      { status: "expired" },
    ],
    [
      "its own payment, still open",
      false,
      "0f8fad5bd9cb469fa16570867728950e",
      { status: "awaiting_payment" },
    ],
    [
      "its own payment, paid but not applied yet",
      false,
      "0f8fad5bd9cb469fa16570867728950e",
      { status: "complete" },
    ],
    [
      "its own payment, a word nobody recognises",
      false,
      "0f8fad5bd9cb469fa16570867728950e",
      { status: "mystery" },
    ],
    [
      "a DIFFERENT returning payment, funds applied",
      false,
      "7c9e6679742540de944be07fc1f90ae7",
      { settlementState: "funds_applied" },
    ],
  ] as const)("%s -> clears: %s", (_label, clears, paymentId, report) => {
    const tab = new TabStorage();
    const saved = savedWithPayment(tab);

    expect(endsSavedPayment(saved, { paymentId, ...report })).toBe(clears);
  });

  it.each([
    ["not read yet", undefined, true],
    [
      "a read of a DIFFERENT payment",
      {
        paymentId: "7c9e6679742540de944be07fc1f90ae7",
        status: "awaiting_payment",
      },
      true,
    ],
    [
      "paid and still landing",
      { paymentId: "0f8fad5bd9cb469fa16570867728950e", status: "complete" },
      true,
    ],
    [
      "a word nobody recognises",
      { paymentId: "0f8fad5bd9cb469fa16570867728950e", status: "mystery" },
      true,
    ],
    // An open page: Continue resumes that same page under the same key.
    [
      "still open and unpaid",
      {
        paymentId: "0f8fad5b-d9cb-469f-a165-70867728950e",
        status: "awaiting_payment",
      },
      false,
    ],
  ] as const)(
    "a saved payment %s -> Continue locked: %s",
    (_label, tracked, locked) => {
      const tab = new TabStorage();

      expect(savedPaymentLocksContinue(savedWithPayment(tab), tracked)).toBe(
        locked,
      );
    },
  );

  it("a saved request that names no payment yet never locks Continue", () => {
    const tab = new TabStorage();

    expect(
      savedPaymentLocksContinue(
        readyRequest(claimAddFunds(tab, "user-1", 2_500)),
        undefined,
      ),
    ).toBe(false);
  });
});
