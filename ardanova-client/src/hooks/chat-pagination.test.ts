import { describe, expect, it } from "vitest";

import {
  CHAT_CONVERSATION_PAGE_LIMIT,
  getNextConversationCursor,
} from "./chat-pagination";

describe("chat conversation pagination contract", () => {
  it("uses the router's bounded conversation page limit", () => {
    expect(CHAT_CONVERSATION_PAGE_LIMIT).toBe(20);
  });

  it("passes the backend cursor through as the next tRPC page parameter", () => {
    expect(getNextConversationCursor({ nextCursor: "cursor-2" })).toBe(
      "cursor-2",
    );
  });

  it("stops pagination when the backend has no cursor", () => {
    expect(getNextConversationCursor({ nextCursor: null })).toBeUndefined();
    expect(getNextConversationCursor({})).toBeUndefined();
  });
});
