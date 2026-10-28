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
    },
  };
});

const { createTRPCContext, createTRPCRouter } = await import("~/server/api/trpc");
const { fundingIntentRouter } = await import("./funding-intent");
const { addFundsRouter } = await import("./add-funds");
const { ADD_FUNDS_REQUEST_FLOW } = await import("~/lib/commerce/add-funds-request");
const { moneyCodeOf } = await import("~/lib/commerce/money-refusal");
const {
  claimRequestKey,
  readSavedRequest,
  settleSavedRequestAfterFailure,
} = await import("~/lib/commerce/persisted-request-key");
const { sponsorshipRequestFlow } = await import("~/lib/commerce/sponsorship-request");
const { TabStorage } = await import("~/lib/commerce/testing/tab-storage");

const router = createTRPCRouter({
  fundingIntent: fundingIntentRouter,
  addFunds: addFundsRouter,
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

type BffCall = { idempotencyKey: string | null; body: unknown };

/** Fakes the .NET BFF: answers every call with `status`/`body`, recording what reached it. */
function bffAnswers(status: number, body: unknown): BffCall[] {
  const calls: BffCall[] = [];
  vi.stubGlobal(
    "fetch",
    vi.fn(async (_url: string, init: RequestInit) => {
      calls.push({
        idempotencyKey: new Headers(init.headers).get("X-Idempotency-Key"),
        body: init.body ? JSON.parse(String(init.body)) : null,
      });
      return new Response(JSON.stringify(body), {
        status,
        headers: { "Content-Type": "application/json" },
      });
    }),
  );
  return calls;
}

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

describe("a sponsorship's saved key against what the BFF actually answers", () => {
  beforeEach(() => {
    vi.stubEnv("ACTOR_ASSERTION_HMAC_KEY", "k".repeat(48));
  });
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it("BFF 400 envelope arrives after commit -> the saved key is kept, and Retry replays it", async () => {
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
      browser.fundingIntent.createCheckout.mutate({
        ...request.wire,
        idempotencyKey: request.requestKey,
      }),
    );

    expect(failure.data?.code).toBe("BAD_REQUEST");
    expect(failure.data?.moneyCode).toBeNull();
    expect(
      settleSavedRequestAfterFailure(tab, flow, SUBJECT, request.requestKey, moneyCodeOf(failure)),
    ).toBe("kept");

    // Retry sends what is saved: the BFF sees the same key and the same bytes, so it replays.
    const saved = readSavedRequest(tab, flow, SUBJECT)!;
    const retryCalls = bffAnswers(200, { intentId: "intent-1", checkoutUrl: "/x" });
    await browser.fundingIntent.createCheckout.mutate({
      ...saved.wire,
      idempotencyKey: saved.requestKey,
    });

    expect(retryCalls[0]).toEqual(firstCalls[0]);
    expect(retryCalls[0]?.idempotencyKey).toBe(request.requestKey);
  });

  it.each([
    [
      "a refusal raised before the intent was persisted",
      400,
      "released",
      { error: "Sponsorship is not open for this project", code: "definite" },
    ],
    [
      "a hold whose settlement was cancelled (finished)",
      409,
      "released",
      { error: "On hold. Nothing was taken.", code: "definite", reason: "finished" },
    ],
    [
      "a sponsorship still being completed",
      409,
      "kept",
      { error: "This sponsorship is being completed; check back shortly.", code: "unconfirmed" },
    ],
    [
      "a post-commit unique-violation envelope",
      409,
      "kept",
      { error: "The request conflicts with existing data.", statusCode: 409 },
    ],
    [
      "a transient transport failure",
      503,
      "kept",
      { error: "AZOA node call timed out.", code: "unconfirmed" },
    ],
    [
      "a code this app does not know yet",
      400,
      "kept",
      { error: "Something new.", code: "brand_new_code" },
    ],
  ] as const)("%s (%i) -> %s", async (_label, status, expected, body) => {
    const tab = new TabStorage();
    const request = claimSponsorship(tab);
    bffAnswers(status, body);

    const failure = await failureOf(
      browser.fundingIntent.createCheckout.mutate({
        ...request.wire,
        idempotencyKey: request.requestKey,
      }),
    );

    expect(
      settleSavedRequestAfterFailure(tab, flow, SUBJECT, request.requestKey, moneyCodeOf(failure)),
    ).toBe(expected);
    expect(readSavedRequest(tab, flow, SUBJECT) !== null).toBe(expected === "kept");
  });

  it.each([
    ["the node's INTENT_EXPIRED", "released", { code: "definite", reason: "expired" }],
    ["the node's INTENT_NEEDS_REVIEW", "kept", { code: "unconfirmed", reason: "needs_review" }],
  ] as const)("add funds: a same-key 409 carrying %s -> %s", async (_label, expected, labels) => {
    const tab = new TabStorage();
    const claim = claimRequestKey(tab, ADD_FUNDS_REQUEST_FLOW, SUBJECT, { amountMinorUnits: 2_500 }, 2_500);
    if (claim.kind !== "ready") throw new Error("expected a ready claim");
    bffAnswers(409, { error: "That payment can no longer be resumed.", ...labels });

    const failure = await failureOf(
      browser.addFunds.begin.mutate({
        amountMinorUnits: claim.request.wire.amountMinorUnits,
        requestKey: claim.request.requestKey,
      }),
    );

    expect(
      settleSavedRequestAfterFailure(
        tab,
        ADD_FUNDS_REQUEST_FLOW,
        SUBJECT,
        claim.request.requestKey,
        moneyCodeOf(failure),
      ),
    ).toBe(expected);
  });
});
