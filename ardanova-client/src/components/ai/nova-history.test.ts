import { describe, expect, it } from "vitest";

import {
  commitSnapshot,
  compactHistories,
  createEmptyHistories,
  getLatestAccepted,
  getStaleReason,
  hasCurrentUpstream,
  nextContentVersion,
  undoSnapshot,
  type NovaHistories,
  type NovaSnapshot,
  type NovaUpstream,
} from "./nova-history";

/** Replays the component's actions on the pure history: generate, edit-save, accept, undo. */
function session() {
  let histories: NovaHistories = createEmptyHistories();
  const present = (mode: keyof NovaHistories) => histories[mode].present!;
  const write = (
    mode: keyof NovaHistories,
    content: string,
    upstream: NovaUpstream | null = null,
  ) => {
    const snapshot: NovaSnapshot = {
      content,
      contentVersion: nextContentVersion(histories[mode]),
      decision: "pending",
      sourceLabel: "test",
      sourcePrompt: "test",
      changeLabel: "test",
      upstream,
    };
    histories = commitSnapshot(histories, mode, snapshot);
  };
  const accept = (mode: keyof NovaHistories) => {
    histories = commitSnapshot(histories, mode, {
      ...present(mode),
      decision: "accepted",
    });
  };
  const undo = (mode: keyof NovaHistories) => {
    histories = undoSnapshot(histories, mode);
  };
  const presentStale = () => {
    const acceptedDraft = getLatestAccepted(histories.Draft);
    const acceptedPresentation = getLatestAccepted(
      histories.Present,
      (snapshot) => hasCurrentUpstream(snapshot, "Draft", acceptedDraft),
    );
    return getStaleReason(
      "Present",
      present("Present"),
      acceptedDraft,
      acceptedPresentation,
    );
  };
  return {
    write,
    accept,
    undo,
    presentStale,
    present,
    get histories() {
      return histories;
    },
    set histories(next) {
      histories = next;
    },
  };
}

describe("Nova stale-source detection", () => {
  it("keeps a Present built from a superseded Draft stale after undo and re-edit", () => {
    const nova = session();
    nova.write("Draft", "draft one");
    nova.accept("Draft");
    nova.write("Draft", "draft two");
    nova.accept("Draft");
    nova.write("Present", "deck from draft two", {
      mode: "Draft",
      contentVersion: nova.present("Draft").contentVersion,
    });
    nova.accept("Present");
    expect(nova.presentStale()).toBeNull();

    nova.undo("Draft");
    nova.undo("Draft");
    expect(nova.presentStale()).toMatch(/^Stale source/);

    nova.write("Draft", "a different draft two");
    nova.accept("Draft");

    expect(nova.present("Draft").contentVersion).toBe(3);
    expect(nova.presentStale()).toBe(
      "Stale source: this Present artifact uses Draft content v02; the current source is Draft content v03. Regenerate it before accepting or using it downstream.",
    );
  });

  it("never reissues an undone version after the tab's history is compacted and restored", () => {
    const nova = session();
    nova.write("Draft", "one");
    nova.write("Draft", "two");
    nova.undo("Draft");
    nova.histories = JSON.parse(
      JSON.stringify(compactHistories(nova.histories)),
    ) as NovaHistories;

    nova.write("Draft", "two again");

    expect(nova.present("Draft").contentVersion).toBe(3);
  });
});
