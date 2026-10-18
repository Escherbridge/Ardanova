import { describe, expect, it } from "vitest";

import type { NovaRefineConversation } from "./nova-refine-panel";
import {
  isRefinementInProgress,
  readRefineConversation,
  REFINE_STORAGE_KEY,
  writeRefineConversation,
} from "./nova-refine-storage";

/** sessionStorage stand-in; `blocked` throws like a private window or a full quota. */
class TabStorage implements Storage {
  private readonly items = new Map<string, string>();
  constructor(private readonly blocked = false) {}
  get length() {
    return this.items.size;
  }
  clear() {
    this.items.clear();
  }
  getItem(key: string) {
    return this.items.get(key) ?? null;
  }
  key(index: number) {
    return [...this.items.keys()][index] ?? null;
  }
  removeItem(key: string) {
    this.items.delete(key);
  }
  setItem(key: string, value: string) {
    if (this.blocked) throw new DOMException("blocked", "QuotaExceededError");
    this.items.set(key, value);
  }
}

const midRefinement: NovaRefineConversation = {
  idea: "A tool library run by a neighbourhood guild, lending power tools.",
  turns: [{ question: "Who has this problem?", answer: "Renters on our street." }],
  reply: {
    type: "question",
    assistant: "Nova AI",
    mode: "live",
    question: "How do they deal with it today?",
    number: 2,
    maxQuestions: 8,
    state: "nv1.1792000000.ABCDEF.upstream-state",
    sessionId: "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
  },
  answer: "They borrow from fri",
};

describe("Nova refinement survives a reload in the same tab", () => {
  it("restores the idea, answers, half-typed answer and the state needed to continue", () => {
    const tab = new TabStorage();

    expect(writeRefineConversation(tab, midRefinement)).toBe(true);
    const afterReload = readRefineConversation(tab);

    expect(afterReload).toEqual(midRefinement);
    expect(isRefinementInProgress(afterReload!)).toBe(true);
  });

  it("starts clean when the saved copy is unreadable, and Start over clears it", () => {
    const tab = new TabStorage();
    tab.setItem(REFINE_STORAGE_KEY, '{"version":1,"idea":42}');
    expect(readRefineConversation(tab)).toBeNull();

    writeRefineConversation(tab, midRefinement);
    writeRefineConversation(tab, { idea: "", turns: [], reply: null, answer: "" });
    expect(tab.getItem(REFINE_STORAGE_KEY)).toBeNull();
  });

  it("reports blocked storage so the sheet can warn before a reload loses the conversation", () => {
    expect(writeRefineConversation(new TabStorage(true), midRefinement)).toBe(false);
  });
});
