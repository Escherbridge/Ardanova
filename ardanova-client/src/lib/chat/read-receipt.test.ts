import { describe, expect, it } from "vitest";

import {
  createReadReceiptQueue,
  intersectsVertically,
  isChatViewportNearBottom,
} from "./read-receipt";

describe("chat read receipt visibility", () => {
  it("only treats a viewport near its newest content as current", () => {
    expect(
      isChatViewportNearBottom({
        scrollHeight: 1_000,
        scrollTop: 480,
        clientHeight: 400,
      }),
    ).toBe(false);
    expect(
      isChatViewportNearBottom({
        scrollHeight: 1_000,
        scrollTop: 500,
        clientHeight: 400,
      }),
    ).toBe(true);
  });

  it("requires a message to intersect the visible vertical range", () => {
    const viewport = { top: 100, bottom: 500 };

    expect(intersectsVertically({ top: 450, bottom: 550 }, viewport)).toBe(
      true,
    );
    expect(intersectsVertically({ top: 500, bottom: 550 }, viewport)).toBe(
      false,
    );
    expect(intersectsVertically({ top: 20, bottom: 100 }, viewport)).toBe(
      false,
    );
  });

  it("queues only the newest marker while an acknowledgement is in flight", async () => {
    let releaseFirst: (() => void) | undefined;
    const firstRequest = new Promise<void>((resolve) => {
      releaseFirst = resolve;
    });
    const sent: string[] = [];
    const queue = createReadReceiptQueue(async (marker) => {
      sent.push(marker.messageId);
      if (marker.messageId === "message-1") await firstRequest;
    });

    queue.enqueue({ messageId: "message-1", readUpTo: "2026-07-18T10:00:00Z" });
    queue.enqueue({ messageId: "message-2", readUpTo: "2026-07-18T10:01:00Z" });
    queue.enqueue({ messageId: "message-3", readUpTo: "2026-07-18T10:02:00Z" });

    expect(sent).toEqual(["message-1"]);
    releaseFirst?.();
    await queue.whenIdle();
    expect(sent).toEqual(["message-1", "message-3"]);
  });

  it("continues with newer evidence after an acknowledgement fails", async () => {
    let rejectFirst: ((error: Error) => void) | undefined;
    const firstRequest = new Promise<void>((_resolve, reject) => {
      rejectFirst = reject;
    });
    const sent: string[] = [];
    const queue = createReadReceiptQueue(async (marker) => {
      sent.push(marker.messageId);
      if (marker.messageId === "message-1") await firstRequest;
    });

    queue.enqueue({ messageId: "message-1", readUpTo: "2026-07-18T10:00:00Z" });
    queue.enqueue({ messageId: "message-2", readUpTo: "2026-07-18T10:01:00Z" });
    rejectFirst?.(new Error("offline"));

    await queue.whenIdle();
    expect(sent).toEqual(["message-1", "message-2"]);
  });

  it("does not resend a marker already acknowledged by the server", async () => {
    const sent: string[] = [];
    const queue = createReadReceiptQueue(async (marker) => {
      sent.push(marker.messageId);
    });

    queue.enqueue({ messageId: "message-2", readUpTo: "2026-07-18T10:01:00Z" });
    await queue.whenIdle();
    queue.enqueue({ messageId: "message-1", readUpTo: "2026-07-18T10:00:00Z" });
    queue.enqueue({ messageId: "message-2", readUpTo: "2026-07-18T10:01:00Z" });
    await queue.whenIdle();

    expect(sent).toEqual(["message-2"]);
  });
});
