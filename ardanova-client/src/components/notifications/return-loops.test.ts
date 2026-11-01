import { describe, expect, it } from "vitest";

import type { Notification } from "~/lib/api/ardanova/endpoints/notifications";

import { returnLoopsOf } from "./return-loops";

const reply = (actionUrl: string | null): Notification => ({
  id: "n-1",
  userId: "user-bea",
  type: "COMMENT_REPLY",
  title: "New reply",
  message: "Cy Carpenter: “Count me in.”",
  data: null,
  isRead: false,
  readAt: null,
  actionUrl,
  createdAt: "2026-11-01T09:00:00Z",
});

describe("returnLoopsOf deep links", () => {
  // actionUrl is written server-side from a project slug; the bell still only ever links inside the app.
  it.each([
    ["/tasks/task-t", "/tasks/task-t"],
    ["/projects/barn-raising?tab=feed", "/projects/barn-raising?tab=feed"],
    // A reply to a post with no project carries no link: Home, where the post is.
    [null, "/dashboard"],
    ["//evil.example/phish", "/dashboard"],
    ["https://evil.example/phish", "/dashboard"],
    ["/\\evil.example", "/dashboard"],
  ])("actionUrl %s links to %s", (actionUrl, href) => {
    expect(returnLoopsOf([reply(actionUrl)]).map((item) => item.href)).toEqual([
      href,
    ]);
  });
});
