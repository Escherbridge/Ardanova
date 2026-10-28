/**
 * In-memory `Storage` for tests, matching `nova-refine-storage.test.ts`'s `TabStorage`. `blocked`
 * throws on every call, like a private window or a full quota.
 */
export class TabStorage implements Storage {
  private readonly items = new Map<string, string>();
  constructor(private readonly blocked = false) {}
  get length() {
    this.guard();
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
