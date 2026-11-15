import { describe, expect, it, vi } from "vitest";
import { createArdaMoneyTransport, type MoneyHost } from "./transport";

function host(overrides: Partial<MoneyHost>): MoneyHost {
  const unavailable = async () => {
    throw new Error("No answer.");
  };
  return {
    voidCashOut: unavailable,
    voidSale: unavailable,
    accountCapabilities: unavailable,
    accountStatus: unavailable,
    ensureAccount: unavailable,
    beginKyc: unavailable,
    revokeConsent: unavailable,
    checkoutCapabilities: unavailable,
    beginCheckout: unavailable,
    getCheckout: unavailable,
    voidCheckout: unavailable,
    cashOutAvailability: unavailable,
    cashOutDestination: unavailable,
    startCashOut: unavailable,
    cashOutHistory: unavailable,
    sellable: unavailable,
    quoteSale: unavailable,
    sell: unavailable,
    ...overrides,
  };
}
const payment = {
  paymentId: "payment-one",
  status: "complete",
  settlementState: "payment_received",
  resumed: true,
  amountPaidMinorUnits: 2500,
  currencyCode: "USD",
  paymentUrl: null,
  expiresAt: null,
  admissionKey: "SECRET",
  tenantId: "tenant",
};
const withdrawal = {
  id: "cash-one",
  status: "Requested",
  statusMessage: "Your request is being confirmed.",
  amountMinorUnits: 2500,
  currencyCode: "USD",
  requestedAt: "2026-11-01T12:00:00Z",
  paidAt: null,
  reversedAt: null,
};

describe("Arda money host transport", () => {
  it("maps the real add-funds JSON names and excludes all authority fields", async () => {
    const transport = createArdaMoneyTransport(
      host({ getCheckout: async () => payment }),
    );
    const answer = await transport.checkout.get("payment-one");
    expect(answer).toEqual({
      ok: true,
      value: {
        intentId: "payment-one",
        status: "complete",
        settlementState: "payment_received",
        resumed: true,
        amountPaidMinorUnits: 2500,
        currency: "USD",
        paymentUrl: null,
        expiresAt: null,
      },
    });
  });
  it("forwards required void and only accepts a literal authoritative outcome", async () => {
    const voidCheckout = vi.fn(async () => ({ outcome: "revealed", payment }));
    const transport = createArdaMoneyTransport(host({ voidCheckout }));
    expect(await transport.checkout.void("saved-key")).toMatchObject({
      ok: true,
      value: { outcome: "revealed", intent: { intentId: "payment-one" } },
    });
    expect(voidCheckout).toHaveBeenCalledWith("saved-key");
    const unknown = createArdaMoneyTransport(
      host({
        voidCheckout: async () => ({ outcome: "void", paymentId: null }),
      }),
    );
    expect(await unknown.checkout.void("saved-key")).toMatchObject({
      ok: false,
      refusal: { code: "unconfirmed" },
    });
  });
  it.each([
    { status: "unknown_status" },
    { settlementState: "unknown_settlement" },
  ])("keeps an unknown checkout lifecycle unconfirmed: %j", async (change) => {
    const transport = createArdaMoneyTransport(
      host({ getCheckout: async () => ({ ...payment, ...change }) }),
    );
    expect(await transport.checkout.get("payment-one")).toMatchObject({
      ok: false,
      refusal: { code: "unconfirmed" },
    });
  });
  it("acknowledges only a dated revocation receipt without inventing a time", async () => {
    const dated = createArdaMoneyTransport(
      host({
        revokeConsent: async () => ({
          revoked: true,
          revokedAt: "2026-11-01T12:00:00Z",
        }),
      }),
    );
    expect(await dated.account.revokeConsent()).toEqual({
      ok: true,
      value: { revokedAt: "2026-11-01T12:00:00Z" },
    });
    const alreadyAbsent = createArdaMoneyTransport(
      host({ revokeConsent: async () => ({ revoked: true, revokedAt: null }) }),
    );
    expect(await alreadyAbsent.account.revokeConsent()).toMatchObject({
      ok: false,
      refusal: { code: "unconfirmed" },
    });
  });
  it.each([
    undefined,
    null,
    {},
    { ...withdrawal, amountMinorUnits: 9007199254740992 },
  ])(
    "an ambiguous successful cash-out payload keeps the outcome unconfirmed: %j",
    async (payload) => {
      const transport = createArdaMoneyTransport(
        host({ startCashOut: async () => payload }),
      );
      expect(
        await transport.cashOut.start({
          requestKey: "same-key",
          amountMinorUnits: 2500,
        }),
      ).toMatchObject({ ok: false, refusal: { code: "unconfirmed" } });
    },
  );
  it("replays exactly the caller's saved withdrawal bytes and strips provider identifiers", async () => {
    const startCashOut = vi.fn(async () => ({
      ...withdrawal,
      connectedAccountId: "secret-provider-id",
    }));
    const transport = createArdaMoneyTransport(host({ startCashOut }));
    const request = { requestKey: "same-key", amountMinorUnits: 2500 };
    expect(await transport.cashOut.start(request)).toEqual({
      ok: true,
      value: withdrawal,
    });
    expect(startCashOut).toHaveBeenCalledWith(request);
  });
  it("never calls tenant payout-setup and never exposes its returned setup URL", async () => {
    const cashOutDestination = vi.fn(async () => ({
      available: true,
      opened: true,
      readyToReceive: false,
      detailsSubmitted: false,
      outstandingRequirementCount: 1,
      setupUrl: "https://connect.stripe.com/secret",
      setupUrlExpiresAt: null,
      statusMessage: "Continue on the secure account page.",
    }));
    const transport = createArdaMoneyTransport(host({ cashOutDestination }));
    expect(await transport.cashOut.beginDestinationSetup()).toMatchObject({
      ok: false,
      refusal: { code: "account_holder_required" },
    });
    expect(cashOutDestination).not.toHaveBeenCalled();
    expect(await transport.cashOut.destination()).toMatchObject({
      ok: true,
      value: { setupUrl: null },
    });
  });
  it("keeps a thrown access refusal separate from proof of a money outcome", async () => {
    const transport = createArdaMoneyTransport(
      host({
        startCashOut: async () => {
          throw Object.assign(new Error("Permission withdrawn."), {
            data: { moneyCode: "consent_revoked" },
          });
        },
      }),
    );
    expect(
      await transport.cashOut.start({
        requestKey: "old-key",
        amountMinorUnits: 2500,
      }),
    ).toMatchObject({ ok: false, refusal: { code: "consent_revoked" } });
  });
  it("sends the backend's durable quote id and key without recomputing quantity or price", async () => {
    const sell = vi.fn(async () => ({
      id: "sale-one",
      status: "PENDING",
      sourceTokenAmount: 3,
      quantityBaseUnits: "3",
      ardaMinorUnits: "1234567",
      currencyCode: "ARDA",
      currencyDecimals: 2,
      requestedAt: "2026-11-01T12:00:00Z",
      failureReason: null,
    }));
    const transport = createArdaMoneyTransport(host({ sell }), {
      projectTokenConfigId: "project-config",
      holderClass: "CONTRIBUTOR",
    });
    const answer = await transport.sale!.sell({
      requestKey: "sale-key",
      quoteId: "durable-quote",
    });
    expect(sell).toHaveBeenCalledWith({
      requestKey: "sale-key",
      quoteId: "durable-quote",
    });
    expect(answer).toMatchObject({
      ok: true,
      value: {
        quantityBaseUnits: "3",
        proceedsMinorUnits: 1234567,
        currencyCode: "ARDA",
      },
    });
  });
});
