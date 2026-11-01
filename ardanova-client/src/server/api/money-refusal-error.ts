import { TRPCError } from "@trpc/server";

import {
  parseMoneyIntentId,
  parseMoneyRefusalCode,
  type MoneyRefusalCode,
} from "~/lib/commerce/money-refusal";

/**
 * Rides on a `TRPCError`'s `cause` so `errorFormatter` (`./trpc.ts`) can expose the BFF's money
 * code as `error.data.moneyCode` (and a held payment's id as `moneyIntentId`). See
 * `~/lib/commerce/AGENTS.md` §Persisted request keys.
 */
export class MoneyRefusalCause extends Error {
  constructor(
    readonly moneyCode: MoneyRefusalCode,
    readonly intentId: string | null = null,
  ) {
    super(`Money refusal: ${moneyCode}`);
    this.name = "MoneyRefusalCause";
  }
}

type TrpcCode = TRPCError["code"];

/** The tRPC class for a money route's HTTP status. Presentation only: the money code decides a key. */
const STATUS_CODES: Partial<Record<number, TrpcCode>> = {
  400: "BAD_REQUEST",
  401: "UNAUTHORIZED",
  403: "FORBIDDEN",
  404: "NOT_FOUND",
  409: "CONFLICT",
  429: "TOO_MANY_REQUESTS",
  503: "SERVICE_UNAVAILABLE",
};

/**
 * A failed money call as a tRPC error that still carries the BFF's `code` - and carries NONE when
 * the BFF body had none (e.g. `ExceptionHandlingMiddleware`'s post-commit 400), so the page keeps
 * its key rather than guessing from the status.
 */
export function moneyRefusalError(
  response: { status: number; code?: string; intentId?: string },
  message: string,
  trpcCode: TrpcCode = STATUS_CODES[response.status] ?? "INTERNAL_SERVER_ERROR",
): TRPCError {
  const moneyCode = parseMoneyRefusalCode(response.code);
  // Only a held payment is named to the page: it is the one refusal the page must record by id.
  const intentId =
    moneyCode === "needs_review" ? parseMoneyIntentId(response.intentId) : null;
  return new TRPCError({
    code: trpcCode,
    message,
    ...(moneyCode ? { cause: new MoneyRefusalCause(moneyCode, intentId) } : {}),
  });
}

/** The money code a procedure attached to its error, or null - never inferred from the tRPC code. */
export function readMoneyCode(error: TRPCError): MoneyRefusalCode | null {
  return error.cause instanceof MoneyRefusalCause ? error.cause.moneyCode : null;
}

/** The held payment a `needs_review` refusal named, or null. */
export function readMoneyIntentId(error: TRPCError): string | null {
  return error.cause instanceof MoneyRefusalCause ? error.cause.intentId : null;
}
