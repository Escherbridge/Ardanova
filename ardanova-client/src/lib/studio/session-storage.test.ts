import { describe, expect, it } from "vitest";

import {
  STUDIO_SESSION_MAX_BYTES,
  parseStudioSession,
  serializeStudioSession,
  studioStorageBytes,
  type StudioArtifacts,
  type StudioSnapshot,
} from "./session-storage";

function snapshot(version: number, content: string): StudioSnapshot {
  return {
    content,
    contentVersion: version,
    decision: "pending",
    sourceLabel: "Test",
    changeLabel: "Test",
    upstream: null,
  };
}

function sessionWith(artifacts: StudioArtifacts) {
  return {
    version: 2 as const,
    mode: "Project brief" as const,
    brief: { title: "", purpose: "", people: "", evidence: "" },
    artifacts,
    edit: null,
  };
}

it("measures the shared Studio budget in UTF-8 bytes", () => {
  expect(studioStorageBytes("\u00e9")).toBe(2);
  expect(studioStorageBytes("\u{1f600}")).toBe(4);
});

describe("Studio session storage", () => {
  it("prunes shared history until the exact stored value can be restored", () => {
    const history = Array.from({ length: 25 }, (_, index) =>
      snapshot(index + 1, "\u{1f600}".repeat(25_000)),
    );
    const artifacts: StudioArtifacts = {
      "Project brief": { past: [...history], present: null },
      Presentation: { past: [], present: null },
      Rehearsal: { past: [], present: null },
    };

    const serialized = serializeStudioSession(sessionWith(artifacts));

    expect(serialized).not.toBeNull();
    expect(studioStorageBytes(serialized!)).toBeLessThanOrEqual(
      STUDIO_SESSION_MAX_BYTES,
    );
    const restored = parseStudioSession(serialized);
    expect(restored).not.toBeNull();
    expect(
      Object.values(restored!.artifacts).reduce(
        (count, item) => count + item.past.length,
        0,
      ),
    ).toBeLessThan(25);
  });

  it("rejects a valid-shaped value whose characters fit but UTF-8 bytes do not", () => {
    const history = Array.from({ length: 21 }, (_, index) =>
      snapshot(index + 1, "\u{1f600}".repeat(25_000)),
    );
    const raw = JSON.stringify(
      sessionWith({
        "Project brief": { past: history, present: null },
        Presentation: { past: [], present: null },
        Rehearsal: { past: [], present: null },
      }),
    );

    expect(raw.length).toBeLessThan(STUDIO_SESSION_MAX_BYTES);
    expect(studioStorageBytes(raw)).toBeGreaterThan(STUDIO_SESSION_MAX_BYTES);
    expect(parseStudioSession(raw)).toBeNull();
  });

  it("refuses invalid state instead of returning a value that restore rejects", () => {
    const serialized = serializeStudioSession({ version: 2, mode: "Unknown" });
    expect(serialized).toBeNull();
  });
});
