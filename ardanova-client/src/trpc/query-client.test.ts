import { describe, expect, it } from "vitest";

import { shouldRetryQuery } from "~/components/ui/query-error-copy";

import { createQueryClient } from "./query-client";

const trpcError = (code: string) => ({ message: code, data: { code } });

describe("createQueryClient retry wiring", () => {
  it("wires the query default retry to shouldRetryQuery, not a reimplementation", () => {
    const retry = createQueryClient().getDefaultOptions().queries?.retry;
    expect(retry).toBe(shouldRetryQuery);
  });

  it("does not retry a 404-coded (terminal) query failure", () => {
    const retry = createQueryClient().getDefaultOptions().queries?.retry as (
      failureCount: number,
      error: unknown,
    ) => boolean;

    expect(retry(0, trpcError("NOT_FOUND"))).toBe(false);
  });

  it("retries a plain network failure with no tRPC code", () => {
    const retry = createQueryClient().getDefaultOptions().queries?.retry as (
      failureCount: number,
      error: unknown,
    ) => boolean;

    expect(retry(0, new TypeError("Failed to fetch"))).toBe(true);
  });
});
