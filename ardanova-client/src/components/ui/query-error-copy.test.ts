import { readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";
import { z } from "zod";

import {
  CASH_OUT_NOT_AVAILABLE,
  describeCashOutStep,
  describeListLoadFailure,
  describeMutationError,
  describeQueryError,
  describeWithdrawBlocker,
  readCashOutState,
  readFigure,
  shouldRetryQuery,
} from "./query-error-copy";

const trpcError = (code: string) => ({ message: code, data: { code } });

/** Shapes a client error exactly as `server/api/trpc.ts` errorFormatter does for a Zod input failure. */
function zodInputError(schema: z.ZodTypeAny, input: unknown) {
  const parsed = schema.safeParse(input);
  if (parsed.success) throw new Error("fixture input unexpectedly valid");
  return {
    message: JSON.stringify(parsed.error.issues, null, 2),
    data: { code: "BAD_REQUEST", zodError: parsed.error.flatten() },
  };
}

describe("describeMutationError", () => {
  const pbiInput = z.object({
    title: z.string().min(1),
    priority: z.enum(["CRITICAL", "HIGH", "MEDIUM", "LOW"]),
    assigneeId: z.string().optional(),
  });

  it("turns the PBI 'URGENT' rejection into a field hint instead of the Zod JSON dump", () => {
    const error = zodInputError(pbiInput, { title: "Fix", priority: "URGENT" });
    const sentence = describeMutationError(error, "create the PBI");
    expect(sentence).toBe(
      "We couldn't create the PBI: check the priority field. Nothing was saved.",
    );
    expect(sentence).not.toMatch(/invalid_enum_value|\[|\{/);
  });

  it("names every invalid field in plain words", () => {
    const error = zodInputError(pbiInput, {
      title: "",
      priority: "URGENT",
      assigneeId: 7,
    });
    expect(describeMutationError(error, "create the PBI")).toContain(
      "check the title, priority and assignee fields",
    );
  });

  it.each([
    [
      {
        message: "Only project members can vote.",
        data: { code: "FORBIDDEN" },
      },
      "We couldn't cast your vote: Only project members can vote. Nothing was saved.",
    ],
    [
      {
        message: '{"title":"Bad Request","status":400}',
        data: { code: "BAD_REQUEST" },
      },
      "We couldn't cast your vote: some of the details were rejected. Check the form and try again. Nothing was saved.",
    ],
    [
      {
        message:
          "AZOA tenant-bound custodial account onboarding is not configured.",
        data: { code: "CONFLICT" },
      },
      "We couldn't cast your vote: this is not switched on for this deployment yet. Nothing was saved.",
    ],
    [
      {
        message: "Request failed with status code 500",
        data: { code: "INTERNAL_SERVER_ERROR" },
      },
      "We couldn't cast your vote: this is a connection or service error, not a problem with what you entered. Nothing was saved. Try again.",
    ],
    [
      new TypeError("Failed to fetch"),
      "We couldn't cast your vote: this is a connection or service error, not a problem with what you entered. Nothing was saved. Try again.",
    ],
  ])(
    "shows author copy but never a serialized or internal message (%o)",
    (error, expected) => {
      expect(describeMutationError(error, "cast your vote")).toBe(expected);
    },
  );
});

describe("describeQueryError", () => {
  it.each([
    [trpcError("UNAUTHORIZED"), "signed-out", false],
    [trpcError("FORBIDDEN"), "forbidden", false],
    [trpcError("NOT_FOUND"), "not-found", false],
    [trpcError("BAD_REQUEST"), "invalid", false],
    [trpcError("TOO_MANY_REQUESTS"), "rate-limited", true],
    [trpcError("INTERNAL_SERVER_ERROR"), "service", true],
    [new TypeError("Failed to fetch"), "service", true],
    [null, "service", true],
  ])("classifies %o as %s", (error, kind, retryable) => {
    const copy = describeQueryError(error);
    expect(copy.kind).toBe(kind);
    expect(copy.retryable).toBe(retryable);
  });

  it("never echoes the raw error code to the user", () => {
    for (const code of ["UNAUTHORIZED", "FORBIDDEN", "INTERNAL_SERVER_ERROR"]) {
      expect(describeQueryError(trpcError(code)).explanation).not.toContain(
        code,
      );
    }
  });
});

/** The exact body the BFF passes through from `ExceptionHandlingMiddleware` on a backend 500. */
const backend500 = {
  message: "An error occurred processing your request.",
  data: { code: "INTERNAL_SERVER_ERROR" },
};
/** `PayoutsController` 503 when this deployment has no bank off-ramp. */
const cashOutGate = {
  message:
    "Sending money to a bank account is not switched on for this deployment.",
  data: { code: "SERVICE_UNAVAILABLE" },
};
const settled = { isLoading: false, error: null };

describe("describeListLoadFailure", () => {
  it("replaces the raw 500 on withdraw 'Sales still going through' with plain, retryable copy", () => {
    const failure = describeListLoadFailure(
      backend500,
      "your pending sales",
      "there are none",
    );
    expect(failure).toEqual({
      sentence:
        "We couldn't load your pending sales just now. This does not mean there are none, and nothing has changed.",
      retryable: true,
    });
  });

  it("sends a signed-out reader to sign in rather than offering a retry that fails again", () => {
    const failure = describeListLoadFailure(
      { message: "UNAUTHORIZED", data: { code: "UNAUTHORIZED" } },
      "what you hold",
      "you hold nothing",
    );
    expect(failure.retryable).toBe(false);
    expect(failure.sentence).toContain("Sign in again");
  });
});

describe("readCashOutState", () => {
  it.each([
    [
      "the availability read refuses with the 503 gate",
      { ...settled, error: cashOutGate, available: undefined },
      undefined,
      "unavailable",
    ],
    [
      "availability answers available: false",
      { ...settled, available: false },
      undefined,
      "unavailable",
    ],
    [
      "destination answers available: false",
      { ...settled, available: true },
      { ...settled, available: false },
      "unavailable",
    ],
    [
      "the gate wins over a still-loading destination",
      { ...settled, error: cashOutGate, available: undefined },
      { isLoading: true, error: null, available: undefined },
      "unavailable",
    ],
    [
      "a read is still loading",
      { isLoading: true, error: null, available: undefined },
      undefined,
      "checking",
    ],
    [
      "availability fails for another reason",
      { ...settled, error: backend500, available: undefined },
      undefined,
      "unknown",
    ],
    [
      "both reads say available",
      { ...settled, available: true },
      { ...settled, available: true },
      "available",
    ],
  ] as const)("%s -> %s", (_label, availability, destination, expected) => {
    expect(readCashOutState(availability, destination)).toBe(expected);
  });
});

describe("cash-out copy", () => {
  it("never promises a bank send unless the availability read said yes", () => {
    for (const state of ["unavailable", "checking", "unknown"] as const) {
      expect(describeCashOutStep(state)).not.toMatch(
        /can be sent|whenever you like/i,
      );
    }
    expect(describeCashOutStep("unavailable")).toContain(
      CASH_OUT_NOT_AVAILABLE,
    );
    expect(describeCashOutStep("available")).toMatch(/can be sent/);
  });

  it("names the real blocker in the withdraw amount hint", () => {
    const notReady = { unreadable: false, ready: false };
    expect(describeWithdrawBlocker("unavailable", notReady)).toMatch(
      /isn't available yet/,
    );
    expect(
      describeWithdrawBlocker("available", { unreadable: true, ready: false }),
    ).toMatch(/couldn't read your bank details/);
    expect(describeWithdrawBlocker("available", notReady)).toMatch(
      /Tell us where to send your money/,
    );
    expect(
      describeWithdrawBlocker("available", { unreadable: false, ready: true }),
    ).toMatch(/Enter an amount/);
  });
});

describe("readFigure", () => {
  it.each([
    [{ isLoading: true, error: null }, "—"],
    [{ isLoading: false, error: backend500 }, "Not available"],
    [{ isLoading: false, error: null }, "$0.00"],
  ])("a figure read as %o shows %s, never a fake zero", (query, expected) => {
    expect(readFigure(query, () => "$0.00")).toBe(expected);
  });
});

describe("money pages wire the honest copy", () => {
  const appRoot = join(__dirname, "..", "..", "app");
  const source = (relative: string) =>
    readFileSync(join(appRoot, relative), "utf8");

  it.each([
    [
      "portfolio/withdraw/page.tsx",
      /\{(?:sales|withdrawals|destination|availability)\.error\.message\}/,
    ],
    ["swap/page.tsx", /\{holdings\.error\.message\}|whenever you\s+like/],
    [
      "projects/page.tsx",
      /value=\{(?:String\(currentViewStats|formatUSD\(currentViewStats)/,
    ],
  ])(
    "%s no longer renders the raw message, false promise or bare zero",
    (file, leak) => {
      expect(source(file)).not.toMatch(leak);
    },
  );
});

describe("shouldRetryQuery", () => {
  it.each([
    ["UNAUTHORIZED", 0, false],
    ["FORBIDDEN", 0, false],
    ["NOT_FOUND", 1, false],
    ["INTERNAL_SERVER_ERROR", 0, true],
    ["INTERNAL_SERVER_ERROR", 2, true],
    ["INTERNAL_SERVER_ERROR", 3, false],
  ])("code %s after %i failures -> %s", (code, failures, expected) => {
    expect(shouldRetryQuery(failures, trpcError(code))).toBe(expected);
  });

  it("retries a plain network failure with no tRPC code", () => {
    expect(shouldRetryQuery(0, new TypeError("Failed to fetch"))).toBe(true);
  });
});
