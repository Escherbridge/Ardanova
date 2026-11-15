import { afterAll, afterEach, describe, expect, it, vi } from "vitest";
import {
  browserOverTheBff,
  dotNetAnswer,
  fakeDotNetApi,
  refusalOf,
} from "~/server/api/dotnet-test-edge";
vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-ada", role: "INDIVIDUAL" },
  }),
}));
const browser = await browserOverTheBff();
afterEach(() => vi.unstubAllGlobals());
afterAll(() => vi.unstubAllEnvs());
const handoff = {
  requestId: "request-one",
  targetUserId: "private-node-subject",
  ownership: "custodial",
  status: "pending",
  expiresAt: "2026-11-02T00:00:00Z",
  delivery: "operator_stub",
  claimToken: "SECRET",
  returnUrl: "https://wrong.example",
};

describe("money transport reaches the real actor-signed API boundary", () => {
  it("requests only safe handoff metadata with server-derived recipient and return URL", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, handoff));
    const answer = await browser.money.createHandoff.mutate({
      expectedSubject: "member-ada",
    });
    expect(answer).toEqual({
      requestId: "request-one",
      ownership: "custodial",
      status: "pending",
      expiresAt: handoff.expiresAt,
      delivery: "operator_stub",
    });
    expect(requests).toEqual([
      expect.objectContaining({
        method: "POST",
        body: {},
        signedActor: "member-ada",
      }),
    ]);
    expect(requests[0]?.url).toMatch(
      /\/api\/azoa\/custodial-account\/handoff$/u,
    );
  });
  it("resends the existing request without recipient, token, scope or return URL input", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, handoff));
    await browser.money.resendHandoff.mutate({
      id: "request-one",
      expectedSubject: "member-ada",
    });
    expect(requests[0]).toMatchObject({
      method: "POST",
      body: {},
      signedActor: "member-ada",
    });
    expect(requests[0]?.url).toMatch(/handoff\/request-one\/resend$/u);
  });
  it("forwards the exact saved withdrawal key and amount", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, {
        id: "cash-one",
        status: "Requested",
        statusMessage: "Confirming.",
        amountMinorUnits: 2500,
        currencyCode: "USD",
        requestedAt: "2026-11-01T12:00:00Z",
        paidAt: null,
        reversedAt: null,
      }),
    );
    await browser.money.startCashOut.mutate({
      requestKey: "same-key",
      expectedSubject: "member-ada",
      amountMinorUnits: 2500,
    });
    expect(requests[0]).toMatchObject({
      body: { requestKey: "same-key", amountMinorUnits: 2500 },
      signedActor: "member-ada",
    });
  });
  it.each([200, 204])(
    "an empty successful money response (%s) is unconfirmed",
    async (status) => {
      fakeDotNetApi(() => dotNetAnswer(status));
      const failure = await refusalOf(
        browser.money.startCashOut.mutate({
          requestKey: "same-key",
          expectedSubject: "member-ada",
          amountMinorUnits: 2500,
        }),
      );
      expect(failure.data?.moneyCode).toBe("unconfirmed");
      expect(failure.message).not.toMatch(
        /nothing was sent|nothing was moved/u,
      );
    },
  );
  it("forwards sale key and durable quote only, with no browser price or actor", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, {
        id: "sale-one",
        status: "PENDING",
        sourceTokenAmount: 3,
        quantityBaseUnits: "3",
        ardaMinorUnits: "1234567",
        currencyCode: "ARDA",
        currencyDecimals: 2,
        requestedAt: "2026-11-01T12:00:00Z",
        failureReason: null,
      }),
    );
    await browser.money.sell.mutate({
      requestKey: "sale-key",
      expectedSubject: "member-ada",
      quoteId: "quote-one",
    });
    expect(requests[0]).toMatchObject({
      body: { requestKey: "sale-key", quoteId: "quote-one" },
      signedActor: "member-ada",
    });
  });
  it("rejects extra money input before forwarding to the privileged API", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, handoff));
    await refusalOf(
      browser.money.sell.mutate({
        requestKey: "sale-key",
        expectedSubject: "member-ada",
        quoteId: "quote-one",
        userId: "someone-else",
        proceedsMinorUnits: 999,
      } as never),
    );
    expect(requests).toEqual([]);
  });
});

describe("subject binding and money void forwarding", () => {
  it("rejects a queued request from an account that is no longer signed in", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, handoff));
    const failure = await refusalOf(
      browser.money.startCashOut.mutate({
        expectedSubject: "former-account",
        requestKey: "former-key",
        amountMinorUnits: 2500,
      }),
    );
    expect(failure.data?.moneyCode).toBe("unauthenticated");
    expect(requests).toEqual([]);
  });
  it("forwards a quote-bound sale void without manufacturing an outcome", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, { outcome: "voided" }),
    );
    expect(
      await browser.money.voidSale.mutate({
        expectedSubject: "member-ada",
        requestKey: "sale-key",
        quoteId: "quote-one",
      }),
    ).toEqual({ outcome: "voided" });
    expect(requests[0]).toMatchObject({
      method: "POST",
      body: { quoteId: "quote-one" },
      signedActor: "member-ada",
    });
    expect(requests[0]?.url).toMatch(/sale\/requests\/sale-key\/void$/u);
  });
  it("forwards cash-out void to the actor-bound backend", async () => {
    const requests = fakeDotNetApi(() =>
      dotNetAnswer(200, { outcome: "voided" }),
    );
    expect(
      await browser.money.voidCashOut.mutate({
        expectedSubject: "member-ada",
        requestKey: "cash-key",
      }),
    ).toEqual({ outcome: "voided" });
    expect(requests[0]).toMatchObject({
      method: "POST",
      body: {},
      signedActor: "member-ada",
    });
    expect(requests[0]?.url).toMatch(/withdraw\/requests\/cash-key\/void$/u);
  });
});
