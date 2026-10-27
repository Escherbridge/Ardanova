import { describe, expect, it } from "vitest";

import {
  claimRequestKey,
  clearSavedRequest,
  isDefiniteRefusal,
  readSavedRequest,
} from "./persisted-request-key";

/** Storage stand-in matching `nova-refine-storage.test.ts`'s `TabStorage`; `blocked` throws on
 * every call, like a private window or a full quota. */
class TabStorage implements Storage {
  private readonly items = new Map<string, string>();
  constructor(private readonly blocked = false) {}
  get length() {
    return this.items.size;
  }
  clear() {
    this.guard();
    this.items.clear();
  }
  getItem(key: string) {
    this.guard();
    return this.items.get(key) ?? null;
  }
  key(index: number) {
    this.guard();
    return [...this.items.keys()][index] ?? null;
  }
  removeItem(key: string) {
    this.guard();
    this.items.delete(key);
  }
  setItem(key: string, value: string) {
    this.guard();
    this.items.set(key, value);
  }
  private guard() {
    if (this.blocked) throw new DOMException("blocked", "QuotaExceededError");
  }
}

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe("claimRequestKey persistence", () => {
  it("persists a freshly minted key BEFORE returning it", () => {
    const tab = new TabStorage();
    const claim = claimRequestKey(tab, "add-funds", "user-1", 2_500);

    expect(claim.kind).toBe("ready");
    // Read back independently of the function under test, proving the write already landed.
    expect(readSavedRequest(tab, "add-funds", "user-1")).toEqual(
      claim.kind === "ready" ? claim.request : null,
    );
  });

  it("returns the same key across a remount over the same storage (the reload the ref lost)", () => {
    const tab = new TabStorage();
    const first = claimRequestKey(tab, "add-funds", "user-1", 2_500);
    // A "remount" is just a second call against the same backing store — a `useRef` would have
    // reset to null here, which is the exact bug this module exists to fix.
    const second = claimRequestKey(tab, "add-funds", "user-1", 2_500);

    expect(first.kind).toBe("ready");
    expect(second.kind).toBe("ready");
    expect(second.kind === "ready" && second.request.requestKey).toBe(
      first.kind === "ready" && first.request.requestKey,
    );
  });

  it("never reuses another signed-in subject's key, and erases it on sight", () => {
    const tab = new TabStorage();
    const ownerClaim = claimRequestKey(tab, "add-funds", "user-1", 2_500);
    const otherClaim = claimRequestKey(tab, "add-funds", "user-2", 2_500);

    expect(ownerClaim.kind).toBe("ready");
    expect(otherClaim.kind).toBe("ready");
    expect(otherClaim.kind === "ready" && otherClaim.request.requestKey).not.toBe(
      ownerClaim.kind === "ready" && ownerClaim.request.requestKey,
    );
    // The first subject's entry does not linger once a different subject has read this scope.
    expect(readSavedRequest(tab, "add-funds", "user-1")).toBeNull();
  });

  it("returns pending for a changed amount, and leaves the saved key untouched", () => {
    const tab = new TabStorage();
    const first = claimRequestKey(tab, "invest:config-1", "user-1", 10_000);
    const second = claimRequestKey(tab, "invest:config-1", "user-1", 25_000);

    expect(first.kind).toBe("ready");
    expect(second).toEqual({
      kind: "pending",
      pending: first.kind === "ready" ? first.request : undefined,
    });
    // Querying again proves the differing amount never overwrote the original entry.
    expect(readSavedRequest(tab, "invest:config-1", "user-1")).toEqual(
      first.kind === "ready" ? first.request : null,
    );
  });

  it("mints a new key once a finished outcome clears the saved one", () => {
    const tab = new TabStorage();
    const before = claimRequestKey(tab, "add-funds", "user-1", 2_500);
    clearSavedRequest(tab, "add-funds");
    expect(readSavedRequest(tab, "add-funds", "user-1")).toBeNull();

    const after = claimRequestKey(tab, "add-funds", "user-1", 2_500);
    expect(after.kind).toBe("ready");
    expect(after.kind === "ready" && after.request.requestKey).not.toBe(
      before.kind === "ready" && before.request.requestKey,
    );
  });

  it("degrades to a usable, unpersisted key when storage throws on every call", () => {
    const blocked = new TabStorage(true);

    const claim = claimRequestKey(blocked, "add-funds", "user-1", 2_500);
    expect(claim.kind).toBe("ready");
    expect(claim.kind === "ready" && UUID_PATTERN.test(claim.request.requestKey)).toBe(
      true,
    );
    expect(() => readSavedRequest(blocked, "add-funds", "user-1")).not.toThrow();
    expect(() => clearSavedRequest(blocked, "add-funds")).not.toThrow();
  });

  it("drops a saved entry that is not valid JSON instead of throwing", () => {
    const tab = new TabStorage();
    tab.setItem("ardanova:commerce:request:v1:add-funds", "{not json");

    expect(readSavedRequest(tab, "add-funds", "user-1")).toBeNull();
  });
});

describe("isDefiniteRefusal", () => {
  it.each([
    ["BAD_REQUEST", true],
    ["FORBIDDEN", true],
    ["NOT_FOUND", true],
    ["UNPROCESSABLE_CONTENT", true],
    ["UNAUTHORIZED", false],
    ["CONFLICT", false],
    ["PRECONDITION_FAILED", false],
    ["TOO_MANY_REQUESTS", false],
    ["INTERNAL_SERVER_ERROR", false],
    ["BAD_GATEWAY", false],
    [undefined, false],
    [null, false],
  ] as const)("treats %s as a definite refusal: %s", (code, expected) => {
    expect(isDefiniteRefusal(code)).toBe(expected);
  });
});
