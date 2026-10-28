import { beforeEach, describe, expect, it, vi } from "vitest";
import { TRPCError } from "@trpc/server";

vi.mock("server-only", () => ({}));

const { currentSession, taxonomy } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  taxonomy: { getAll: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { taxonomy } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { taxonomyRouter } from "./taxonomy";

/** A .NET TaxonomyTermDto as the API serializes it. */
function term(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    slug: "AGRICULTURE",
    kind: "SECTOR",
    label: "Agriculture",
    description: null,
    parentSlug: null,
    sortOrder: 0,
    ...overrides,
  };
}

async function signedOutCaller() {
  currentSession.mockResolvedValue(null);
  return createCallerFactory(taxonomyRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("taxonomy reads", () => {
  beforeEach(() => vi.clearAllMocks());

  it("parses a normal response and keeps the API's order", async () => {
    taxonomy.getAll.mockResolvedValue({
      status: 200,
      data: [
        term({ slug: "AGRICULTURE", kind: "SECTOR", sortOrder: 0 }),
        term({ slug: "CARPENTRY", kind: "SKILL", label: "Carpentry", parentSlug: "CONSTRUCTION", sortOrder: 0 }),
        term({ slug: "VENUE", kind: "PROJECT_TYPE", label: "Venue", sortOrder: 0 }),
      ],
    });

    const result = await (await signedOutCaller()).getAll();

    expect(result.map((t) => t.slug)).toEqual([
      "AGRICULTURE",
      "CARPENTRY",
      "VENUE",
    ]);
  });

  /**
   * The API owns the TaxonomyKind vocabulary and can add a value before this client is
   * redeployed. A read must still succeed then, and must not drop unrecognized fields
   * the server already stopped sending but a stale deploy might still include.
   */
  it("tolerates an unknown kind and extra fields without failing the parse", async () => {
    taxonomy.getAll.mockResolvedValue({
      status: 200,
      data: [
        term({
          slug: "MOBILE_KITCHEN",
          kind: "MOBILE_UNIT",
          label: "Mobile kitchen",
          futureField: "should be stripped, not fatal",
        }),
      ],
    });

    const result = await (await signedOutCaller()).getAll();

    expect(result).toEqual([
      {
        slug: "MOBILE_KITCHEN",
        kind: "MOBILE_UNIT",
        label: "Mobile kitchen",
        description: null,
        parentSlug: null,
        sortOrder: 0,
      },
    ]);
  });

  it("surfaces an API error as a tRPC error instead of an unhandled throw", async () => {
    taxonomy.getAll.mockResolvedValue({
      status: 500,
      error: "An unexpected error occurred.",
    });

    const failure = await (await signedOutCaller())
      .getAll()
      .then(() => null, (error: unknown) => error);

    expect(failure).toBeInstanceOf(TRPCError);
    expect((failure as TRPCError).code).toBe("INTERNAL_SERVER_ERROR");
  });

  it("rejects a response that does not match the taxonomy contract with BAD_GATEWAY", async () => {
    taxonomy.getAll.mockResolvedValue({
      status: 200,
      data: [{ slug: "AGRICULTURE" /* missing kind, label, sortOrder */ }],
    });

    const failure = await (await signedOutCaller())
      .getAll()
      .then(() => null, (error: unknown) => error);

    expect(failure).toBeInstanceOf(TRPCError);
    expect((failure as TRPCError).code).toBe("BAD_GATEWAY");
  });
});
