import { describe, expect, it } from "vitest";

import { subscriptionActionSchema } from "./types";

describe("realtime subscription action contract", () => {
  it("accepts a strict project subscription", () => {
    expect(
      subscriptionActionSchema.safeParse({
        action: "subscribeToProject",
        payload: { projectId: "project-1" },
      }).success,
    ).toBe(true);
  });

  it("rejects the removed global subscription", () => {
    expect(
      subscriptionActionSchema.safeParse({
        action: "subscribeToAll",
        payload: {},
      }).success,
    ).toBe(false);
  });

  it("rejects payload fields from a different action", () => {
    expect(
      subscriptionActionSchema.safeParse({
        action: "subscribeToGuild",
        payload: { agencyId: "legacy-agency" },
      }).success,
    ).toBe(false);
  });

  it("rejects unknown fields rather than stripping them", () => {
    expect(
      subscriptionActionSchema.safeParse({
        action: "subscribeToConversation",
        payload: { conversationId: "conversation-1", userId: "user-2" },
      }).success,
    ).toBe(false);
  });
});
