import { describe, expect, it } from "vitest";

import {
  getNovaContext,
  NOVA_MODES,
  NOVA_NON_ACTIONS,
} from "~/lib/nova-context";

describe("getNovaContext", () => {
  it("uses the most specific project creation context", () => {
    const context = getNovaContext("/projects/create");

    expect(context.artifact).toBe("New project");
    expect(context.scope).toContain("unsaved");
  });

  it("provides a prompt for every interaction mode", () => {
    const context = getNovaContext("/tasks/task-123");

    for (const mode of NOVA_MODES) {
      expect(context.suggestedPrompts[mode]).toBeTruthy();
    }
  });

  it("keeps consequential actions outside Nova's authority", () => {
    expect(NOVA_NON_ACTIONS.join(" ")).toMatch(/publish/i);
    expect(NOVA_NON_ACTIONS.join(" ")).toMatch(/fund/i);
    expect(NOVA_NON_ACTIONS.join(" ")).toMatch(/rights/i);
  });
});
