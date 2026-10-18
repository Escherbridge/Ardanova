import { describe, expect, it } from "vitest";

import { describeQueryError, shouldRetryQuery } from "./query-error-copy";

const trpcError = (code: string) => ({ message: code, data: { code } });

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
      expect(describeQueryError(trpcError(code)).explanation).not.toContain(code);
    }
  });
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
