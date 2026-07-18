const DEFAULT_BOTTOM_THRESHOLD_PX = 120;

export interface ScrollViewportMetrics {
  scrollHeight: number;
  scrollTop: number;
  clientHeight: number;
}

export interface VerticalBounds {
  top: number;
  bottom: number;
}

export interface ReadReceiptMarker {
  messageId: string;
  readUpTo: string;
}

export interface ReadReceiptQueue {
  enqueue(marker: ReadReceiptMarker): void;
  whenIdle(): Promise<void>;
}

export function isChatViewportNearBottom(
  viewport: ScrollViewportMetrics,
  threshold = DEFAULT_BOTTOM_THRESHOLD_PX,
): boolean {
  return (
    viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight <
    threshold
  );
}

export function intersectsVertically(
  item: VerticalBounds,
  viewport: VerticalBounds,
): boolean {
  return item.bottom > viewport.top && item.top < viewport.bottom;
}

export function createReadReceiptQueue(
  send: (marker: ReadReceiptMarker) => Promise<unknown>,
): ReadReceiptQueue {
  let acknowledgedAt = Number.NEGATIVE_INFINITY;
  let activeAt = Number.NEGATIVE_INFINITY;
  let pending: (ReadReceiptMarker & { timestamp: number }) | null = null;
  let running = false;
  const idleWaiters = new Set<() => void>();

  const resolveIdle = () => {
    if (running || pending) return;
    for (const resolve of idleWaiters) resolve();
    idleWaiters.clear();
  };

  const drain = async () => {
    if (running || !pending) return;

    const current = pending;
    pending = null;
    running = true;
    activeAt = current.timestamp;
    try {
      await send(current);
      acknowledgedAt = Math.max(acknowledgedAt, current.timestamp);
    } catch {
      // Visibility can enqueue the same marker again; newer queued evidence still drains.
    } finally {
      running = false;
      activeAt = Number.NEGATIVE_INFINITY;
      if (pending) void drain();
      else resolveIdle();
    }
  };

  return {
    enqueue(marker) {
      const timestamp = Date.parse(marker.readUpTo);
      if (!Number.isFinite(timestamp) || timestamp <= acknowledgedAt) return;
      if (running && timestamp <= activeAt) return;
      if (pending && timestamp <= pending.timestamp) return;

      pending = { ...marker, timestamp };
      void drain();
    },
    whenIdle() {
      if (!running && !pending) return Promise.resolve();
      return new Promise<void>((resolve) => idleWaiters.add(resolve));
    },
  };
}
