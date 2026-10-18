import { NOVA_MODES, type NovaMode } from "~/lib/nova-context";

// Version-pinned Nova preview history; see ./AGENTS.md §Stale-source detection before simplifying any of this.

export type DraftDecision = "pending" | "accepted" | "rejected";
export type NovaUpstreamMode = Extract<NovaMode, "Draft" | "Present">;

export interface NovaUpstream {
  mode: NovaUpstreamMode;
  contentVersion: number;
}

export interface NovaSnapshot {
  content: string;
  contentVersion: number;
  decision: DraftDecision;
  sourceLabel: string;
  sourcePrompt: string;
  changeLabel: string;
  upstream: NovaUpstream | null;
}

export interface NovaHistory {
  past: NovaSnapshot[];
  present: NovaSnapshot | null;
  /** Highest content version ever issued in this mode, undone ones included; versions are never reused. */
  highWater?: number;
}

export type NovaHistories = Record<NovaMode, NovaHistory>;

export const NOVA_HISTORY_LIMIT = 25;

export function createEmptyHistories(): NovaHistories {
  return {
    Ask: { past: [], present: null },
    Draft: { past: [], present: null },
    Review: { past: [], present: null },
    Present: { past: [], present: null },
    Rehearse: { past: [], present: null },
  };
}

/** Keeps the last NOVA_HISTORY_LIMIT past states per mode and the version high-water mark. */
export function compactHistories(histories: NovaHistories): NovaHistories {
  return Object.fromEntries(
    NOVA_MODES.map((item) => [
      item,
      {
        past: histories[item].past.slice(-NOVA_HISTORY_LIMIT),
        present: histories[item].present,
        highWater: nextContentVersion(histories[item]) - 1,
      },
    ]),
  ) as NovaHistories;
}

/** The next unused content version in a mode: above every kept, undone or compacted-away version. */
export function nextContentVersion(history: NovaHistory): number {
  const kept = history.present
    ? [...history.past, history.present]
    : history.past;
  return (
    Math.max(
      history.highWater ?? 0,
      ...kept.map((snapshot) => snapshot.contentVersion),
    ) + 1
  );
}

/** Pushes the present state into the past and makes next the present. */
export function commitSnapshot(
  histories: NovaHistories,
  mode: NovaMode,
  next: NovaSnapshot,
): NovaHistories {
  const target = histories[mode];
  const past = target.present ? [...target.past, target.present] : target.past;
  return {
    ...histories,
    [mode]: {
      past,
      present: next,
      highWater: Math.max(target.highWater ?? 0, next.contentVersion),
    },
  };
}

/** Restores the previous state; the undone version stays reserved so it can never be issued again. */
export function undoSnapshot(
  histories: NovaHistories,
  mode: NovaMode,
): NovaHistories {
  const target = histories[mode];
  const previous = target.past.at(-1);
  if (!previous) return histories;
  return {
    ...histories,
    [mode]: {
      past: target.past.slice(0, -1),
      present: previous,
      highWater: nextContentVersion(target) - 1,
    },
  };
}

export function getLatestAccepted(
  history: NovaHistory,
  isEligible: (snapshot: NovaSnapshot) => boolean = () => true,
): NovaSnapshot | null {
  const latestStateByVersion = new Map<number, NovaSnapshot>();
  const timeline = history.present
    ? [...history.past, history.present]
    : history.past;

  for (const snapshot of timeline) {
    latestStateByVersion.set(snapshot.contentVersion, snapshot);
  }

  return (
    [...latestStateByVersion.values()]
      .reverse()
      .find(
        (snapshot) => snapshot.decision === "accepted" && isEligible(snapshot),
      ) ?? null
  );
}

export function hasCurrentUpstream(
  snapshot: NovaSnapshot,
  mode: NovaUpstreamMode,
  acceptedUpstream: NovaSnapshot | null,
): boolean {
  return Boolean(
    acceptedUpstream &&
      snapshot.upstream?.mode === mode &&
      snapshot.upstream.contentVersion === acceptedUpstream.contentVersion,
  );
}

export function getStaleReason(
  mode: NovaMode,
  snapshot: NovaSnapshot | null,
  acceptedDraft: NovaSnapshot | null,
  acceptedPresentation: NovaSnapshot | null,
): string | null {
  if (!snapshot || (mode !== "Present" && mode !== "Rehearse")) return null;

  const expectedMode: NovaUpstreamMode =
    mode === "Present" ? "Draft" : "Present";
  const acceptedUpstream =
    mode === "Present" ? acceptedDraft : acceptedPresentation;

  if (hasCurrentUpstream(snapshot, expectedMode, acceptedUpstream)) return null;

  const capturedSource = snapshot.upstream
    ? `${snapshot.upstream.mode} content v${formatVersion(snapshot.upstream.contentVersion)}`
    : "an unversioned source";
  const currentSource = acceptedUpstream
    ? `${expectedMode} content v${formatVersion(acceptedUpstream.contentVersion)}`
    : `no currently accepted ${expectedMode} artifact`;

  return `Stale source: this ${mode} artifact uses ${capturedSource}; the current source is ${currentSource}. Regenerate it before accepting or using it downstream.`;
}

export function formatVersion(version: number): string {
  return String(version).padStart(2, "0");
}
