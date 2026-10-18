import { describe, expect, it } from "vitest";
import { z } from "zod";

import {
  describeMutationError,
  describeQueryError,
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
