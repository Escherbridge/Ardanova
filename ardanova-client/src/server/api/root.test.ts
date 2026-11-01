import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  browserOverTheBff,
  dotNetAnswer,
  fakeDotNetApi,
  refusalOf,
} from "./dotnet-test-edge";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-1", role: "INDIVIDUAL" },
  }),
}));

// Importing the whole tree is the point: a schema that throws at definition time 500s every call.
const browser = await browserOverTheBff();
const { describeMutationError, readInvalidFields } = await import(
  "~/components/ui/query-error-copy"
);
const { taskClaimRefusalOf } = await import("~/lib/tasks/claim-refusal");

afterEach(() => {
  vi.unstubAllGlobals();
});

afterAll(() => {
  vi.unstubAllEnvs();
});

describe("app router input validation", () => {
  it("refuses a malformed sponsorship key as BAD_REQUEST naming the field, before any BFF call", async () => {
    const dotNetRequests = fakeDotNetApi(() => dotNetAnswer(500));

    const failure = await refusalOf(
      browser.fundingIntent.createCheckout.mutate({
        projectTokenConfigId: "config-1",
        amount: "25.00",
        disclosureVersion: "2026-10",
        idempotencyKey: "not-a-uuid",
      }),
    );

    expect(failure.data?.code).toBe("BAD_REQUEST");
    expect(readInvalidFields(failure)).toEqual(["idempotencyKey"]);
    expect(describeMutationError(failure, "start your sponsorship")).toContain(
      "idempotency key",
    );
    expect(dotNetRequests).toHaveLength(0);
  });
});

describe("task.claim refusals reach the browser with the API's code", () => {
  it.each([
    ["TASK_TAKEN", "TASK_TAKEN"],
    ["TASK_DONE", "TASK_DONE"],
    ["TASK_UNDER_AGREEMENT", "TASK_UNDER_AGREEMENT"],
    // A code a newer API added: still CONFLICT with the API's words, just no code to branch on.
    ["TASK_ARCHIVED", null],
  ] as const)(
    "a 409 naming %s is CONFLICT with taskClaimCode %s",
    async (code, expected) => {
      fakeDotNetApi(() =>
        dotNetAnswer(409, {
          error: "This task was just picked up.",
          code,
          statusCode: 409,
          traceId: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
        }),
      );

      const failure = await refusalOf(
        browser.task.claim.mutate({ id: "task-1" }),
      );

      expect(failure.data?.code).toBe("CONFLICT");
      expect(taskClaimRefusalOf(failure)).toBe(expected);
      expect(describeMutationError(failure, "pick up this task")).toContain(
        "This task was just picked up.",
      );
    },
  );
});

describe("post.create: a second ask on a task reaches the browser naming the first", () => {
  it("the API's real 409 body becomes CONFLICT with helpWantedExistingPostId, so the composer can offer 'Edit your ask'", async () => {
    // PostsController's PostConflictDto after ErrorEnvelopeResultFilter: { error, code, conflictId } + statusCode, traceId.
    fakeDotNetApi(() =>
      dotNetAnswer(409, {
        error: "This task already has an ask.",
        code: "HELP_WANTED_EXISTS",
        conflictId: "post-ask-first",
        statusCode: 409,
        traceId: "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
      }),
    );

    const failure = await refusalOf(
      browser.post.create.mutate({
        type: "HELP_WANTED",
        taskId: "task-frame",
        content: "Framing the east wall Saturday, need two hands",
      }),
    );

    expect(failure.data?.code).toBe("CONFLICT");
    expect(failure.data?.helpWantedExistingPostId).toBe("post-ask-first");
    expect(failure.message).toBe("This task already has an ask.");
  });
});
