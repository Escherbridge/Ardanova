import { beforeEach, describe, expect, it, vi } from "vitest";
import { TRPCError } from "@trpc/server";

vi.mock("server-only", () => ({}));

const { currentSession, backend } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  backend: {
    get: vi.fn(),
    payouts: { getMine: vi.fn() },
    tokenBalances: { getArdaBalance: vi.fn(), getPortfolio: vi.fn() },
  },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: backend }));
vi.mock("~/server/admin-api-client", () => ({ getAdminApiClient: vi.fn() }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { payoutsRouter } from "./payouts";
import { swapRouter } from "./swap";
import { tokenBalancesRouter } from "./token-balances";

async function context() {
  currentSession.mockResolvedValue({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-1" },
  });
  return createTRPCContext({ headers: new Headers() });
}

/** Every no-input money read, driven through its real tRPC procedure. */
const reads: Array<[string, () => Promise<unknown>]> = [
  [
    "payouts.getSellableUnits",
    async () =>
      createCallerFactory(payoutsRouter)(await context()).getSellableUnits(),
  ],
  [
    "payouts.getPayoutsByUser",
    async () =>
      createCallerFactory(payoutsRouter)(await context()).getPayoutsByUser(),
  ],
  [
    "tokenBalances.getArdaBalance",
    async () =>
      createCallerFactory(tokenBalancesRouter)(
        await context(),
      ).getArdaBalance(),
  ],
  [
    "tokenBalances.getPortfolio",
    async () =>
      createCallerFactory(tokenBalancesRouter)(await context()).getPortfolio(),
  ],
  [
    "swap.getHistory",
    async () => createCallerFactory(swapRouter)(await context()).getHistory(),
  ],
];

function backendReplies(status: number, error: string) {
  const reply = { status, error };
  backend.get.mockResolvedValue(reply);
  backend.payouts.getMine.mockResolvedValue(reply);
  backend.tokenBalances.getArdaBalance.mockResolvedValue(reply);
  backend.tokenBalances.getPortfolio.mockResolvedValue(reply);
}

async function failureCode(read: () => Promise<unknown>) {
  const failure = await read().then(
    () => null,
    (error: unknown) => error,
  );
  expect(failure).toBeInstanceOf(TRPCError);
  return (failure as TRPCError).code;
}

describe("money reads keep the backend's error class", () => {
  beforeEach(() => vi.clearAllMocks());

  it.each(reads)(
    "%s reports a backend 500 as a server fault, never 'not found' or 'rejected'",
    async (_name, read) => {
      backendReplies(500, "An unexpected error occurred.");
      expect(await failureCode(read)).toBe("INTERNAL_SERVER_ERROR");
    },
  );

  it.each(reads)(
    "%s still reports a backend 404 as NOT_FOUND",
    async (_name, read) => {
      backendReplies(404, "Nothing here.");
      expect(await failureCode(read)).toBe("NOT_FOUND");
    },
  );

  it.each(reads)(
    "%s still reports a backend 400 as BAD_REQUEST",
    async (_name, read) => {
      backendReplies(400, "That request was rejected.");
      expect(await failureCode(read)).toBe("BAD_REQUEST");
    },
  );
});
