import { describe, expect, it } from "vitest";

import type { WorkflowRunStatus } from "./types";
import { isAzoaRunPending } from "./queries";

describe("isAzoaRunPending", () => {
  it.each([
    "Pending",
    "Running",
    "Suspended",
    "AwaitingSignal",
    "AwaitingTimer",
  ] satisfies WorkflowRunStatus[])("treats %s as pending", (status) => {
    expect(isAzoaRunPending(status)).toBe(true);
  });

  it.each([
    "Succeeded",
    "Failed",
    "Forked",
    "Cancelled",
  ] satisfies WorkflowRunStatus[])("treats %s as terminal", (status) => {
    expect(isAzoaRunPending(status)).toBe(false);
  });
});
