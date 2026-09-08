import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import type { BaseApiClient } from "../../base-client";
import { ChatEndpoint } from "./chat";
import { NotificationsEndpoint } from "./notifications";

function fakeClient() {
  const get = vi.fn().mockResolvedValue({ status: 200 });
  const post = vi.fn().mockResolvedValue({ status: 200 });
  const put = vi.fn().mockResolvedValue({ status: 200 });
  const deleteRequest = vi.fn().mockResolvedValue({ status: 204 });

  return {
    client: {
      get,
      post,
      put,
      delete: deleteRequest,
    } as unknown as BaseApiClient,
    get,
    post,
    put,
    deleteRequest,
  };
}

function requestPaths(
  ...requests: Array<{ mock: { calls: unknown[][] } }>
): string[] {
  return requests.flatMap((request) =>
    request.mock.calls.map(([path]) => String(path)),
  );
}

describe("collaboration endpoint identity boundaries", () => {
  it("uses actor-scoped chat paths without sending a caller-selected user id", async () => {
    const { client, get, post } = fakeClient();
    const chat = new ChatEndpoint(client);

    await chat.getConversations({ page: 2, pageSize: 25 });
    await chat.getConversation("conversation-1");
    await chat.sendMessage({
      conversationId: "conversation-1",
      message: "Hello",
    });
    await chat.sendTypingIndicator({
      conversationId: "conversation-1",
      isTyping: true,
    });

    const paths = requestPaths(get, post);
    expect(paths).toContain("/api/chat/conversations?page=2&pageSize=25");
    expect(paths).toContain("/api/chat/conversations/conversation-1");
    expect(paths).toContain("/api/chat/messages");
    expect(paths).toContain("/api/chat/typing");
    expect(paths.join("\n")).not.toContain("userId");
  });

  it("uses /me notification routes and exposes no public notification creation method", async () => {
    const { client, get, post, deleteRequest } = fakeClient();
    const notifications = new NotificationsEndpoint(client);

    await notifications.getPaged(2, 25);
    await notifications.getUnread();
    await notifications.getSummary();
    await notifications.markAllAsRead();
    await notifications.deleteAll();

    const paths = requestPaths(get, post, deleteRequest);
    expect(paths).toContain("/api/notifications/me/paged?page=2&pageSize=25");
    expect(paths).toContain("/api/notifications/me/unread");
    expect(paths).toContain("/api/notifications/me/summary");
    expect(paths).toContain("/api/notifications/me/read-all");
    expect(paths).toContain("/api/notifications/me");
    expect(paths.join("\n")).not.toContain("/user/");
    expect("create" in notifications).toBe(false);
  });
});
