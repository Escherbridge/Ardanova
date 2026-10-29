import { createTRPCClient, httpLink, TRPCClientError } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const session = vi.hoisted(() => ({
  expires: "2099-01-01T00:00:00Z",
  user: { id: "member-1", role: "INDIVIDUAL" },
}));
vi.mock("~/server/auth", () => ({ auth: async () => session }));
vi.mock("~/server/admin-api-client", () => ({ getAdminApiClient: vi.fn() }));

// The real endpoints over the real base client: only the BFF's HTTP answer (`fetch`) is faked.
vi.mock("~/lib/api", async () => {
  const { BaseApiClient } = await import("~/lib/api/base-client");
  const { FundingIntentsEndpoint } = await import(
    "~/lib/api/ardanova/endpoints/funding-intents"
  );
  const { AddFundsEndpoint } = await import(
    "~/lib/api/ardanova/endpoints/add-funds"
  );
  const backend = new BaseApiClient({ baseUrl: "https://api.example.test" });
  return {
    apiClient: {
      fundingIntents: new FundingIntentsEndpoint(backend),
      addFunds: new AddFundsEndpoint(backend),
      get: (endpoint: string) => backend.get(endpoint),
    },
  };
});

const { createTRPCContext, createTRPCRouter } = await import("~/server/api/trpc");
const { fundingIntentRouter } = await import("./funding-intent");
const { addFundsRouter } = await import("./add-funds");
const { payoutsRouter } = await import("./payouts");
const {
  ADD_FUNDS_REQUEST_FLOW,
  savedPaymentLocksContinue,
  setAsideSavedPayment,
  settleAddFundsFailure,
} = await import("~/lib/commerce/add-funds-request");
const { moneyCodeOf, moneyIntentIdOf } = await import("~/lib/commerce/money-refusal");
const { claimRequestKey, readSavedRequest, recordAdmittedId } = await import(
  "~/lib/commerce/persisted-request-key"
);
const { settleSponsorshipFailure, sponsorshipRequestFlow } = await import(
  "~/lib/commerce/sponsorship-request"
);
const { TabStorage } = await import("~/lib/commerce/testing/tab-storage");
const { CASH_OUT_NOT_AVAILABLE, describeCashOutStep, readCashOutState } =
  await import("~/components/ui/query-error-copy");

const router = createTRPCRouter({
  fundingIntent: fundingIntentRouter,
  addFunds: addFundsRouter,
  payouts: payoutsRouter,
});

/** A browser-side tRPC client wired straight into the handler: the error it throws is the page's. */
const browser = createTRPCClient<typeof router>({
  links: [
    httpLink({
      url: "http://localhost:3000/api/trpc",
      transformer: superjson,
      fetch: (input, init) =>
        fetchRequestHandler({
          endpoint: "/api/trpc",
          req: new Request(input as RequestInfo, init as RequestInit),
          router,
          createContext: ({ req }) => createTRPCContext({ headers: req.headers }),
        }),
    }),
  ],
});

type BffCall = { method: string; path: string; idempotencyKey: string | null; body: unknown };
type BffAnswer = { status: number; body: unknown };

/** Fakes the .NET BFF by route: `answer` picks the reply for each call; every call is recorded. */
function bff(answer: (call: BffCall) => BffAnswer): BffCall[] {
  const calls: BffCall[] = [];
  vi.stubGlobal(
    "fetch",
    vi.fn(async (url: string, init: RequestInit) => {
      const call: BffCall = {
        method: init.method ?? "GET",
        path: new URL(url).pathname,
        idempotencyKey: new Headers(init.headers).get("X-Idempotency-Key"),
        body: init.body ? JSON.parse(String(init.body)) : null,
      };
      calls.push(call);
      const { status, body } = answer(call);
      return new Response(JSON.stringify(body), {
        status,
        headers: { "Content-Type": "application/json" },
      });
    }),
  );
  return calls;
}

/** Every call answered alike. */
const bffAnswers = (status: number, body: unknown) => bff(() => ({ status, body }));

async function failureOf(call: Promise<unknown>) {
  const failure = await call.then(
    () => null,
    (error: unknown) => error,
  );
  expect(failure).toBeInstanceOf(TRPCClientError);
  return failure as TRPCClientError<typeof router>;
}

const SUBJECT = session.user.id;
const flow = sponsorshipRequestFlow("config-1");
const PAYMENT_ID = "0f8fad5bd9cb469fa16570867728950e";
const isVoid = (call: BffCall) => call.path.endsWith("/void");

const sponsorVoid = (idempotencyKey: string) =>
  browser.fundingIntent.voidRequest.mutate({ idempotencyKey });

/** The page's own server calls for the add-funds key lifecycle, through the real routers. */
const addFundsServer = {
  readPayment: (paymentId: string) => browser.addFunds.getPayment.query({ paymentId }),
  voidRequest: (requestKey: string) => browser.addFunds.voidRequest.mutate({ requestKey }),
};

function claimSponsorship(tab: Storage) {
  const claim = claimRequestKey(
    tab,
    flow,
    SUBJECT,
    {
      projectTokenConfigId: "config-1",
      amount: "10000.00",
      disclosureVersion: "funding-disclosure-v1",
      displayNameConsent: false,
    },
    10_000,
  );
  if (claim.kind !== "ready") throw new Error("expected a ready claim");
  return claim.request;
}

function claimAddFunds(tab: Storage, admittedId?: string) {
  const claim = claimRequestKey(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT, { amountMinorUnits: 2_500 }, 2_500);
  if (claim.kind !== "ready") throw new Error("expected a ready claim");
  return admittedId
    ? recordAdmittedId(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT, claim.request.requestKey, admittedId)!
    : claim.request;
}

function payment(report: { status: string; settlementState: string }) {
  return { paymentId: PAYMENT_ID, resumed: true, paymentUrl: null, ...report };
}

beforeEach(() => {
  vi.stubEnv("ACTOR_ASSERTION_HMAC_KEY", "k".repeat(48));
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe("a sponsorship's saved key against what the BFF actually answers", () => {
  it("BFF 400 envelope arrives after commit -> the key is kept without a void, and Try again replays it", async () => {
    const tab = new TabStorage();
    const request = claimSponsorship(tab);

    // ExceptionHandlingMiddleware's answer to a post-commit SQLSTATE 22xxx: the node may already
    // have moved the funds. It is a 400, and it carries no money code.
    const firstCalls = bffAnswers(400, {
      error: "The request contained an invalid value.",
      statusCode: 400,
      traceId: "trace-1",
    });
    const failure = await failureOf(
      browser.fundingIntent.createCheckout.mutate({ ...request.wire, idempotencyKey: request.requestKey }),
    );

    expect(failure.data?.code).toBe("BAD_REQUEST");
    expect(failure.data?.moneyCode).toBeNull();
    const settled = await settleSponsorshipFailure({
      storage: tab,
      flow,
      subjectId: SUBJECT,
      requestKey: request.requestKey,
      moneyCode: moneyCodeOf(failure),
      voidRequest: sponsorVoid,
    });
    expect(settled.kind).toBe("kept");
    expect(firstCalls.some(isVoid)).toBe(false);

    // Try again sends what is saved: the BFF sees the same key and the same bytes, so it replays.
    const saved = readSavedRequest(tab, flow, SUBJECT)!;
    const retryCalls = bffAnswers(200, { intentId: "intent-1", checkoutUrl: "/x" });
    await browser.fundingIntent.createCheckout.mutate({ ...saved.wire, idempotencyKey: saved.requestKey });

    expect(retryCalls[0]).toEqual(firstCalls[0]);
    expect(retryCalls[0]?.idempotencyKey).toBe(request.requestKey);
  });

  it.each([
    // A final-looking code goes to void-or-reveal; only `voided` releases.
    ["a pre-admission refusal (definite), void voided", 400, { error: "Sponsorship is not open for this project", code: "definite" }, { status: 200, body: { outcome: "voided", intentId: null } }, "released"],
    ["a hold whose settlement was cancelled (finished), void voided", 409, { error: "On hold. Nothing was taken.", code: "finished", reason: "finished" }, { status: 200, body: { outcome: "voided", intentId: "intent-9" } }, "released"],
    ["a final state the void reveals as still settling", 409, { error: "This payment is already finished.", code: "finished" }, { status: 200, body: { outcome: "revealed", intentId: "intent-9", intent: { intentId: "intent-9", status: "SETTLEMENT_PENDING" } } }, "revealed"],
    ["a final-looking code whose void could not be checked", 400, { error: "Sponsorship is not open for this project", code: "definite" }, { status: 503, body: { error: "We couldn't check that.", code: "unconfirmed" } }, "kept"],
    // Anything else keeps the key and never asks for a void.
    ["a sponsorship still being completed", 409, { error: "This sponsorship is being completed; check back shortly.", code: "unconfirmed" }, null, "kept"],
    ["a post-commit unique-violation envelope", 409, { error: "The request conflicts with existing data.", statusCode: 409 }, null, "kept"],
    ["a transient transport failure", 503, { error: "AZOA node call timed out.", code: "unconfirmed" }, null, "kept"],
    ["a code this app does not know yet", 400, { error: "Something new.", code: "brand_new_code" }, null, "kept"],
  ] as const)("%s -> %s", async (_label, status, body, voidAnswer, expected) => {
    const tab = new TabStorage();
    const request = claimSponsorship(tab);
    const calls = bff((call) => (isVoid(call) && voidAnswer ? voidAnswer : { status, body }));

    const failure = await failureOf(
      browser.fundingIntent.createCheckout.mutate({ ...request.wire, idempotencyKey: request.requestKey }),
    );
    const settled = await settleSponsorshipFailure({
      storage: tab,
      flow,
      subjectId: SUBJECT,
      requestKey: request.requestKey,
      moneyCode: moneyCodeOf(failure),
      voidRequest: sponsorVoid,
    });

    expect(settled.kind).toBe(expected);
    expect(readSavedRequest(tab, flow, SUBJECT) !== null).toBe(expected !== "released");
    expect(calls.filter(isVoid).map((call) => call.path)).toEqual(
      voidAnswer ? [`/api/funding-intents/requests/${request.requestKey}/void`] : [],
    );
  });
});

describe("add funds: a code never releases a key that names a payment (C-1)", () => {
  it("an admitted payment + INTENT_FINISHED on Continue -> key kept, its payment read, Continue locked", async () => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab, PAYMENT_ID);
    // The node sends INTENT_FINISHED for PAID intents too; the BFF now labels it `finished`.
    const calls = bff((call) =>
      call.method === "GET"
        ? { status: 200, body: payment({ status: "processing", settlementState: "payment_received" }) }
        : { status: 409, body: { error: "This payment is already finished.", code: "finished", reason: "finished" } },
    );

    const failure = await failureOf(
      browser.addFunds.begin.mutate({ amountMinorUnits: 2_500, requestKey: saved.requestKey }),
    );
    const settled = await settleAddFundsFailure({
      storage: tab,
      subjectId: SUBJECT,
      requestKey: saved.requestKey,
      moneyCode: moneyCodeOf(failure),
      server: addFundsServer,
    });

    expect(moneyCodeOf(failure)).toBe("finished");
    expect(settled.kind).toBe("tracking");
    expect(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT)?.requestKey).toBe(saved.requestKey);
    expect(calls.map((call) => `${call.method} ${call.path}`)).toEqual([
      "POST /api/add-funds",
      `GET /api/add-funds/${PAYMENT_ID}`,
    ]);
    // Paid and still landing: a fresh payment may not start until it settles.
    const tracked = settled.kind === "tracking" ? settled.payment : null;
    expect(savedPaymentLocksContinue(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT), tracked)).toBe(true);
  });

  it.each([
    ["the node's INTENT_EXPIRED", { error: "This payment page has expired.", code: "definite", reason: "expired" }],
    ["the node's INTENT_FINISHED", { error: "This payment is already finished.", code: "finished", reason: "finished" }],
  ] as const)("a lost first answer, then %s -> the server's void decides", async (_label, refusal) => {
    for (const [voidBody, expected] of [
      [{ outcome: "voided" }, "released"],
      [{ outcome: "revealed", payment: payment({ status: "complete", settlementState: "payment_received" }) }, "tracking"],
    ] as const) {
      const tab = new TabStorage();
      const saved = claimAddFunds(tab);
      const calls = bff((call) => (isVoid(call) ? { status: 200, body: voidBody } : { status: 409, body: refusal }));

      const failure = await failureOf(
        browser.addFunds.begin.mutate({ amountMinorUnits: 2_500, requestKey: saved.requestKey }),
      );
      const settled = await settleAddFundsFailure({
        storage: tab,
        subjectId: SUBJECT,
        requestKey: saved.requestKey,
        moneyCode: moneyCodeOf(failure),
        server: addFundsServer,
      });

      expect(settled.kind).toBe(expected);
      expect(calls.filter(isVoid).map((call) => call.path)).toEqual([
        `/api/add-funds/requests/${saved.requestKey}/void`,
      ]);
      const after = readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT);
      if (expected === "released") {
        expect(after).toBeNull();
      } else {
        // Revealed: the key stays, now naming the payment the server found under it.
        expect(after?.requestKey).toBe(saved.requestKey);
        expect(after?.admittedId).toBe(PAYMENT_ID);
      }
    }
  });

  it("an unconfirmed Continue failure keeps the key and asks for nothing", async () => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab);
    const calls = bffAnswers(503, { error: "AZOA node call timed out.", code: "unconfirmed" });

    const failure = await failureOf(
      browser.addFunds.begin.mutate({ amountMinorUnits: 2_500, requestKey: saved.requestKey }),
    );
    const settled = await settleAddFundsFailure({
      storage: tab,
      subjectId: SUBJECT,
      requestKey: saved.requestKey,
      moneyCode: moneyCodeOf(failure),
      server: addFundsServer,
    });

    expect(settled.kind).toBe("kept");
    expect(calls).toHaveLength(1);
    // M-4: an unknown outcome is never told "nothing on this page can charge you".
    expect(failure.message).not.toMatch(/can charge you|nothing was charged/i);
    expect(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT)?.requestKey).toBe(saved.requestKey);
  });
});

describe("add funds: 'start a new payment instead' asks the server to void first (M-1)", () => {
  it.each([
    // The page is still payable; the node expires it at Stripe and answers voided.
    ["the node expired the open page", { status: 200, body: { outcome: "voided" } }, "released"],
    ["the node found it paid", { status: 200, body: { outcome: "revealed", payment: payment({ status: "processing", settlementState: "payment_received" }) } }, "tracking"],
    ["the node could not read the payment", { status: 503, body: { error: "We couldn't confirm this payment just now.", code: "unconfirmed" } }, "kept"],
  ] as const)("%s -> %s", async (_label, voidAnswer, expected) => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab, PAYMENT_ID);
    const calls = bff(() => voidAnswer);

    const settled = await setAsideSavedPayment({ storage: tab, subjectId: SUBJECT, server: addFundsServer });

    expect(settled.kind).toBe(expected);
    expect(calls).toEqual([
      { method: "POST", path: `/api/add-funds/requests/${saved.requestKey}/void`, idempotencyKey: null, body: null },
    ]);
    expect(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT) === null).toBe(expected === "released");
  });
});

describe("add funds: a payment held for a manual check is recorded, locked and never resent (round 4 #3)", () => {
  const held = {
    error: "This payment needs a manual check before it can continue.",
    code: "needs_review",
    reason: "needs_review",
    intentId: PAYMENT_ID,
  };

  it("Continue meets needs_review -> the key names the held payment, and Continue stays locked across a reload", async () => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab);
    // The held row's own page still reads as unfinished: on that alone Continue would unlock and resend.
    const calls = bff((call) =>
      call.method === "GET"
        ? { status: 200, body: payment({ status: "starting", settlementState: "" }) }
        : { status: 409, body: held },
    );

    const failure = await failureOf(
      browser.addFunds.begin.mutate({ amountMinorUnits: 2_500, requestKey: saved.requestKey }),
    );
    const settled = await settleAddFundsFailure({
      storage: tab,
      subjectId: SUBJECT,
      requestKey: saved.requestKey,
      moneyCode: moneyCodeOf(failure),
      heldPaymentId: moneyIntentIdOf(failure),
      server: addFundsServer,
    });

    expect(moneyCodeOf(failure)).toBe("needs_review");
    expect(settled.kind).toBe("tracking");
    const reloaded = readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT);
    expect(reloaded).toMatchObject({ requestKey: saved.requestKey, admittedId: PAYMENT_ID, heldForReview: true });
    const tracked = settled.kind === "tracking" ? settled.payment : null;
    expect(savedPaymentLocksContinue(reloaded, tracked)).toBe(true);
    // One send, one read of the held payment: no void asked for, nothing resent.
    expect(calls.map((call) => `${call.method} ${call.path}`)).toEqual([
      "POST /api/add-funds",
      `GET /api/add-funds/${PAYMENT_ID}`,
    ]);
  });

  it("'start a new payment instead' on a held payment keeps it held; the node's later voided releases it", async () => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab);

    // An attached held row: the node's void answers needs_review with that payment in `result`.
    bffAnswers(409, held);
    const kept = await setAsideSavedPayment({ storage: tab, subjectId: SUBJECT, server: addFundsServer });

    expect(kept.kind).toBe("tracking");
    expect(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT)).toMatchObject({
      requestKey: saved.requestKey,
      admittedId: PAYMENT_ID,
      heldForReview: true,
    });

    // Once the node can prove nothing was or can be paid (an unattached claim), the void releases.
    bffAnswers(200, { outcome: "voided" });
    const released = await setAsideSavedPayment({ storage: tab, subjectId: SUBJECT, server: addFundsServer });

    expect(released.kind).toBe("released");
    expect(readSavedRequest(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT)).toBeNull();
  });

  it.each([
    ["needs_review naming its payment", { ...held }, PAYMENT_ID],
    ["finished, whatever the body names", { ...held, code: "finished", reason: "finished" }, null],
    ["needs_review naming something that is not a payment id", { ...held, intentId: "../admin" }, null],
  ] as const)("the page learns a held payment's id only from %s", async (_label, body, expected) => {
    const tab = new TabStorage();
    const saved = claimAddFunds(tab);
    bffAnswers(409, body);

    const failure = await failureOf(
      browser.addFunds.begin.mutate({ amountMinorUnits: 2_500, requestKey: saved.requestKey }),
    );

    expect(moneyIntentIdOf(failure)).toBe(expected);
  });
});

describe("cash-out copy keys on the money code, not the 503 (M-3)", () => {
  it.each([
    ["payments_not_configured", "unavailable"],
    ["unconfirmed", "unknown"],
  ] as const)("a 503 carrying %s reads as %s", async (moneyCode, expected) => {
    bffAnswers(503, { error: "We could not check what you can send right now.", code: moneyCode });

    const error = await failureOf(browser.payouts.getWithdrawAvailability.query());
    const state = readCashOutState({ isLoading: false, error, available: undefined });

    expect(state).toBe(expected);
    expect(describeCashOutStep(state).includes(CASH_OUT_NOT_AVAILABLE)).toBe(expected === "unavailable");
  });
});
