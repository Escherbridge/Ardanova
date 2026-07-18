"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Check,
  FilePenLine,
  GitCompareArrows,
  RotateCcw,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";
import { z } from "zod";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "~/components/ui/sheet";
import { Textarea } from "~/components/ui/textarea";
import {
  getNovaContext,
  NOVA_MODES,
  NOVA_NON_ACTIONS,
  type NovaMode,
} from "~/lib/nova-context";
import { cn } from "~/lib/utils";

type DraftDecision = "pending" | "accepted" | "rejected";
type NovaUpstreamMode = Extract<NovaMode, "Draft" | "Present">;

interface NovaUpstream {
  mode: NovaUpstreamMode;
  contentVersion: number;
}

interface NovaSnapshot {
  content: string;
  contentVersion: number;
  decision: DraftDecision;
  sourceLabel: string;
  sourcePrompt: string;
  changeLabel: string;
  upstream: NovaUpstream | null;
}

interface NovaHistory {
  past: NovaSnapshot[];
  present: NovaSnapshot | null;
}

type NovaHistories = Record<NovaMode, NovaHistory>;

const NOVA_HISTORY_LIMIT = 25;

const novaUpstreamSchema = z.object({
  mode: z.enum(["Draft", "Present"]),
  contentVersion: z.number().int().positive(),
});
const novaSnapshotSchema = z.object({
  content: z.string().max(50_000),
  contentVersion: z.number().int().positive(),
  decision: z.enum(["pending", "accepted", "rejected"]),
  sourceLabel: z.string().max(1_000),
  sourcePrompt: z.string().max(10_000),
  changeLabel: z.string().max(1_000),
  upstream: novaUpstreamSchema.nullable(),
});
const novaHistorySchema = z.object({
  past: z.array(novaSnapshotSchema).max(NOVA_HISTORY_LIMIT),
  present: novaSnapshotSchema.nullable(),
});
const novaSessionSchema = z.object({
  version: z.literal(1),
  mode: z.enum(NOVA_MODES),
  prompt: z.string().max(10_000),
  histories: z.object({
    Ask: novaHistorySchema,
    Draft: novaHistorySchema,
    Review: novaHistorySchema,
    Present: novaHistorySchema,
    Rehearse: novaHistorySchema,
  }),
  edit: z
    .object({
      mode: z.enum(NOVA_MODES),
      buffer: z.string().max(50_000),
    })
    .nullable()
    .optional(),
});

function compactHistories(histories: NovaHistories): NovaHistories {
  return Object.fromEntries(
    NOVA_MODES.map((item) => [
      item,
      {
        past: histories[item].past.slice(-NOVA_HISTORY_LIMIT),
        present: histories[item].present,
      },
    ]),
  ) as NovaHistories;
}

const modeLead: Record<NovaMode, string> = {
  Ask: "A grounded question about the labeled workspace",
  Draft: "A working draft you can reshape",
  Review: "A review of claims, gaps, and assumptions",
  Present: "A presentation structure from an accepted Nova draft",
  Rehearse: "Practice prompts from an accepted Nova presentation",
};

function createEmptyHistories(): NovaHistories {
  return {
    Ask: { past: [], present: null },
    Draft: { past: [], present: null },
    Review: { past: [], present: null },
    Present: { past: [], present: null },
    Rehearse: { past: [], present: null },
  };
}

function createPreviewDraft(
  mode: NovaMode,
  prompt: string,
  artifact: string,
  acceptedUpstream?: string,
): string {
  const intent = prompt.trim();
  const contextLabel = ["WORKING CONTEXT LABEL", artifact, ""].join("\n");

  if (mode === "Ask") {
    return [
      contextLabel,
      "LOCAL PREVIEW READ",
      "Based only on your prompt and the route-level label above, start by clarifying purpose, evidence, and the next human decision.",
      "",
      "QUESTION TO RESOLVE",
      intent,
      "",
      "LIMIT",
      "This preview has not inspected page records. Confirm which records are current and who is affected before relying on the response.",
    ].join("\n");
  }

  if (mode === "Review") {
    return [
      contextLabel,
      "REVIEW FOCUS",
      intent,
      "",
      "1. Separate observed evidence from proposed outcomes.",
      "2. Name the people affected and how their consent will be gathered.",
      "3. Define the next decision, its owner, and the record that will prove it happened.",
      "4. Verify any language about value, credentials, ownership, or rights before sharing.",
      "",
      "LIMIT",
      "The review covers your prompt, not the underlying workspace records.",
    ].join("\n");
  }

  if (mode === "Present") {
    return [
      contextLabel,
      "PRESENTATION SPINE",
      "",
      "01 — The situation: what is true now",
      "02 — The people: who experiences the problem",
      "03 — The proposal: what this project will test",
      "04 — The work: roles, milestones, and evidence",
      "05 — The compact: governance and economic rights stated separately",
      "06 — The ask: one concrete next decision",
      "",
      "WORKING DIRECTION",
      intent,
      "",
      "ACCEPTED NOVA DRAFT USED AS SOURCE",
      acceptedUpstream ?? "No accepted source was available.",
    ].join("\n");
  }

  if (mode === "Rehearse") {
    return [
      contextLabel,
      "REHEARSAL PROMPTS",
      "",
      "1. What evidence supports the need for this work?",
      "2. Which outcomes are commitments, and which are still hypotheses?",
      "3. How can contributors challenge or change the plan?",
      "4. What does participation grant—and what does it not grant?",
      "5. What record will show that funds or awards were reconciled?",
      "",
      "PRACTICE GOAL",
      intent,
      "",
      "ACCEPTED NOVA PRESENTATION BEING REHEARSED",
      acceptedUpstream ?? "No accepted source was available.",
    ].join("\n");
  }

  return [
    contextLabel,
    "WORKING DRAFT",
    "",
    "PURPOSE",
    "State the change this project intends to test and who it serves.",
    "",
    "EVIDENCE",
    "List what is already known, with a source for each material claim.",
    "",
    "CONTRIBUTION",
    "Define the work, decision rights, and what proof marks it complete.",
    "",
    "OPEN QUESTIONS",
    "Name dependencies, consent still needed, and claims that require verification.",
    "",
    "DRAFT DIRECTION",
    intent,
  ].join("\n");
}

function getLatestAccepted(
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

function hasCurrentUpstream(
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

function getStaleReason(
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

function formatVersion(version: number): string {
  return String(version).padStart(2, "0");
}

function describeComparison(
  previous: NovaSnapshot,
  current: NovaSnapshot,
): string {
  const contentChange =
    previous.content === current.content
      ? "Content is unchanged."
      : "Content changed.";
  const decisionChange =
    previous.decision === current.decision
      ? "Decision remains " + current.decision + "."
      : "Decision changed from " +
        previous.decision +
        " to " +
        current.decision +
        ".";

  return contentChange + " " + decisionChange;
}

export function NovaAssistant({ pathname }: { pathname: string }) {
  const context = useMemo(() => getNovaContext(pathname), [pathname]);
  const storageKey = useMemo(
    () => `ardanova:nova:v1:${encodeURIComponent(pathname)}`,
    [pathname],
  );
  const [mode, setMode] = useState<NovaMode>("Ask");
  const [prompt, setPrompt] = useState(context.suggestedPrompts.Ask);
  const [histories, setHistories] =
    useState<NovaHistories>(createEmptyHistories);
  const [isEditing, setIsEditing] = useState(false);
  const [editBuffer, setEditBuffer] = useState("");
  const [showComparison, setShowComparison] = useState(false);
  const [restoredStorageKey, setRestoredStorageKey] = useState<string | null>(
    null,
  );
  const [sessionSaveState, setSessionSaveState] = useState<
    "restoring" | "saved" | "unavailable"
  >("restoring");

  const history = histories[mode];
  const draftState = history.present;
  const previousState = history.past.at(-1) ?? null;
  const hasUnsavedEdit = Boolean(
    isEditing && draftState && editBuffer !== draftState.content,
  );
  const acceptedDraft = getLatestAccepted(histories.Draft);
  const latestAcceptedPresentation = getLatestAccepted(histories.Present);
  const acceptedPresentation = getLatestAccepted(
    histories.Present,
    (snapshot) => hasCurrentUpstream(snapshot, "Draft", acceptedDraft),
  );
  const staleReason = getStaleReason(
    mode,
    draftState,
    acceptedDraft,
    acceptedPresentation,
  );

  useEffect(() => {
    setRestoredStorageKey(null);
    setSessionSaveState("restoring");

    let nextMode: NovaMode = "Ask";
    let nextPrompt = context.suggestedPrompts.Ask;
    let nextHistories = createEmptyHistories();
    let nextEdit: { mode: NovaMode; buffer: string } | null = null;
    try {
      const raw = window.sessionStorage.getItem(storageKey);
      if (raw && raw.length <= 2_000_000) {
        const restored = novaSessionSchema.safeParse(JSON.parse(raw));
        if (restored.success) {
          nextMode = restored.data.mode;
          nextPrompt = restored.data.prompt;
          nextHistories = restored.data.histories;
          const recoveredEdit = restored.data.edit;
          if (
            recoveredEdit &&
            nextHistories[recoveredEdit.mode].present !== null
          ) {
            nextMode = recoveredEdit.mode;
            nextEdit = recoveredEdit;
          }
        } else {
          setSessionSaveState("unavailable");
        }
      }
    } catch {
      setSessionSaveState("unavailable");
    }

    setMode(nextMode);
    setPrompt(nextPrompt);
    setHistories(nextHistories);
    setIsEditing(nextEdit !== null);
    setEditBuffer(nextEdit?.buffer ?? "");
    setShowComparison(false);
    setRestoredStorageKey(storageKey);
  }, [context, storageKey]);

  useEffect(() => {
    if (restoredStorageKey !== storageKey) return;

    const timeout = window.setTimeout(() => {
      const payload = {
        version: 1 as const,
        mode,
        prompt,
        histories: compactHistories(histories),
        edit: isEditing ? { mode, buffer: editBuffer } : null,
      };
      if (!novaSessionSchema.safeParse(payload).success) {
        setSessionSaveState("unavailable");
        return;
      }

      try {
        window.sessionStorage.setItem(storageKey, JSON.stringify(payload));
        setSessionSaveState("saved");
      } catch {
        setSessionSaveState("unavailable");
      }
    }, 180);

    return () => window.clearTimeout(timeout);
  }, [
    editBuffer,
    histories,
    isEditing,
    mode,
    prompt,
    restoredStorageKey,
    storageKey,
  ]);

  useEffect(() => {
    if (!hasUnsavedEdit) return;

    const warnBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    const confirmLinkNavigation = (event: MouseEvent) => {
      const target = event.target;
      const link =
        target instanceof Element
          ? target.closest<HTMLAnchorElement>("a[href]")
          : null;
      if (!link) return;
      if (
        !window.confirm(
          "Leave with an unsaved Nova edit? Choose Cancel to return and save it. A recovery copy is kept in this tab when storage is available.",
        )
      ) {
        event.preventDefault();
        event.stopPropagation();
      }
    };

    window.addEventListener("beforeunload", warnBeforeUnload);
    document.addEventListener("click", confirmLinkNavigation, true);
    return () => {
      window.removeEventListener("beforeunload", warnBeforeUnload);
      document.removeEventListener("click", confirmLinkNavigation, true);
    };
  }, [hasUnsavedEdit]);

  function canUseMode(nextMode: NovaMode): boolean {
    if (nextMode === "Present") {
      return acceptedDraft !== null || histories.Present.present !== null;
    }
    if (nextMode === "Rehearse") {
      return (
        acceptedPresentation !== null || histories.Rehearse.present !== null
      );
    }
    return true;
  }

  function commitState(targetMode: NovaMode, next: NovaSnapshot) {
    setHistories((current) => {
      const target = current[targetMode];
      const past = target.present
        ? [...target.past, target.present]
        : target.past;

      return {
        ...current,
        [targetMode]: { past, present: next },
      };
    });
  }

  function changeMode(nextMode: NovaMode) {
    if (isEditing || !canUseMode(nextMode)) return;
    setMode(nextMode);
    setPrompt(context.suggestedPrompts[nextMode]);
    setShowComparison(false);
  }

  function generateDraft() {
    if (isEditing) return;
    let acceptedUpstream: NovaSnapshot | null = null;
    let upstream: NovaUpstream | null = null;
    let sourceLabel = "Your prompt + route label “" + context.artifact + "”";

    if (mode === "Present") {
      if (!acceptedDraft) return;
      acceptedUpstream = acceptedDraft;
      upstream = {
        mode: "Draft",
        contentVersion: acceptedDraft.contentVersion,
      };
      sourceLabel =
        "Accepted Nova Draft content v" +
        formatVersion(acceptedDraft.contentVersion) +
        " + your prompt";
    } else if (mode === "Rehearse") {
      if (!acceptedPresentation) return;
      acceptedUpstream = acceptedPresentation;
      upstream = {
        mode: "Present",
        contentVersion: acceptedPresentation.contentVersion,
      };
      sourceLabel =
        "Accepted Nova Present content v" +
        formatVersion(acceptedPresentation.contentVersion) +
        " + your prompt";
    }

    const content = createPreviewDraft(
      mode,
      prompt,
      context.artifact,
      acceptedUpstream?.content,
    );

    if (
      draftState?.content === content &&
      draftState.sourceLabel === sourceLabel
    ) {
      setIsEditing(false);
      setShowComparison(false);
      return;
    }

    commitState(mode, {
      content,
      contentVersion: (draftState?.contentVersion ?? 0) + 1,
      decision: "pending",
      sourceLabel,
      sourcePrompt: prompt.trim(),
      changeLabel: "Local preview generated for review",
      upstream,
    });
    setIsEditing(false);
    setEditBuffer("");
    setShowComparison(false);
  }

  function setDecision(decision: Exclude<DraftDecision, "pending">) {
    if (
      !draftState ||
      draftState.decision === decision ||
      isEditing ||
      (staleReason && decision === "accepted")
    ) {
      return;
    }
    commitState(mode, {
      ...draftState,
      decision,
      changeLabel:
        decision === "accepted" ? "Accepted by you" : "Rejected by you",
    });
    setShowComparison(false);
  }

  function startEditing() {
    if (!draftState) return;
    setEditBuffer(draftState.content);
    setIsEditing(true);
    setShowComparison(false);
  }

  function saveEditing() {
    if (!draftState || !isEditing) return;
    if (editBuffer !== draftState.content) {
      commitState(mode, {
        ...draftState,
        content: editBuffer,
        contentVersion: draftState.contentVersion + 1,
        decision: "pending",
        sourceLabel: draftState.sourceLabel + " · revised by you",
        changeLabel: "Human edit finished",
      });
    }
    setIsEditing(false);
    setEditBuffer("");
  }

  function cancelEditing() {
    if (!isEditing) return;
    setIsEditing(false);
    setEditBuffer("");
  }

  function undoLastChange() {
    if (isEditing || !draftState || history.past.length === 0) return;

    setHistories((current) => {
      const target = current[mode];
      const previous = target.past.at(-1);
      if (!previous) return current;

      return {
        ...current,
        [mode]: {
          past: target.past.slice(0, -1),
          present: previous,
        },
      };
    });
    setShowComparison(false);
  }

  const canGenerate =
    !isEditing &&
    Boolean(prompt.trim()) &&
    (mode === "Present"
      ? acceptedDraft !== null
      : mode === "Rehearse"
        ? acceptedPresentation !== null
        : true);

  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button size="sm" className="min-h-11 border-2 px-3 sm:px-4">
          <Sparkles className="size-4" aria-hidden="true" />
          <span className="hidden sm:inline">Ask Nova</span>
          <span className="sm:hidden">Nova</span>
        </Button>
      </SheetTrigger>
      <SheetContent
        className="w-full gap-0 border-l-2 p-0 shadow-none sm:max-w-[34rem]"
        aria-describedby="nova-description"
      >
        <SheetHeader className="border-b-2 p-5 pr-14 text-left">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge variant="outline" className="font-mono uppercase">
              Interface preview
            </Badge>
            <span className="text-muted-foreground font-mono text-[0.65rem] tracking-[0.12em] uppercase">
              Local draft only · nothing is sent ·{" "}
              {isEditing
                ? sessionSaveState === "unavailable"
                  ? "artifact not saved · edit recovery unavailable"
                  : sessionSaveState === "saved"
                    ? "artifact not saved · edit recovery stored in this tab"
                    : "artifact not saved · storing edit recovery"
                : sessionSaveState === "unavailable"
                  ? "tab storage unavailable"
                  : sessionSaveState === "saved"
                    ? "saved in this tab"
                    : "restoring this tab"}
            </span>
          </div>
          <SheetTitle className="font-mono text-2xl font-black tracking-[-0.05em] uppercase">
            Nova / {context.artifact}
          </SheetTitle>
          <SheetDescription id="nova-description" className="max-w-md text-sm">
            This local preview uses your prompt and the route label, not the
            page records. You review every draft and retain every consequential
            decision.
          </SheetDescription>
        </SheetHeader>

        <div className="min-h-0 flex-1 overflow-y-auto">
          <section className="border-b-2 p-5">
            <div className="border-foreground bg-muted/50 mb-5 space-y-3 border-2 p-3">
              <div>
                <p className="mb-1 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                  Input used by this preview
                </p>
                <p className="text-sm leading-relaxed">
                  Your prompt, the selected interaction, and the route label “
                  {context.artifact}”. It does not inspect the page records.
                </p>
              </div>
              <div className="border-t pt-3">
                <p className="mb-1 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                  Potential connected scope
                </p>
                <p className="text-muted-foreground text-xs leading-relaxed">
                  {context.scope} This context is not connected to the local
                  preview.
                </p>
              </div>
            </div>

            <fieldset>
              <legend className="mb-2 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                Choose an interaction
              </legend>
              <div className="grid grid-cols-3 border-t-2 border-l-2 sm:grid-cols-5">
                {NOVA_MODES.map((item) => (
                  <button
                    key={item}
                    type="button"
                    onClick={() => changeMode(item)}
                    aria-pressed={mode === item}
                    disabled={isEditing || !canUseMode(item)}
                    className={cn(
                      "min-h-11 border-r-2 border-b-2 px-2 font-mono text-[0.68rem] font-bold uppercase transition-colors disabled:cursor-not-allowed disabled:opacity-50",
                      mode === item
                        ? "bg-foreground text-background"
                        : "bg-background hover:bg-muted",
                    )}
                  >
                    {item}
                  </button>
                ))}
              </div>
              <div className="text-muted-foreground mt-2 space-y-1 text-xs leading-relaxed">
                <p>
                  Present:{" "}
                  {acceptedDraft
                    ? "ready from accepted Draft content v" +
                      formatVersion(acceptedDraft.contentVersion)
                    : "accept a Draft to unlock"}
                  .
                </p>
                <p>
                  Rehearse:{" "}
                  {acceptedPresentation
                    ? "ready from accepted Present content v" +
                      formatVersion(acceptedPresentation.contentVersion)
                    : latestAcceptedPresentation
                      ? "the accepted Present artifact is stale; regenerate it from the current Draft and accept it"
                      : "accept a Present artifact to unlock"}
                  .
                </p>
              </div>
            </fieldset>

            <label
              htmlFor="nova-prompt"
              className="mt-5 mb-2 block font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase"
            >
              {modeLead[mode]}
            </label>
            <Textarea
              id="nova-prompt"
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
              disabled={isEditing}
              className="min-h-28 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
            />
            <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
              <p className="text-muted-foreground max-w-xs text-xs leading-relaxed">
                This preview creates local interface copy. It does not call an
                AI service or write to ArdaNova records.
              </p>
              <Button
                onClick={generateDraft}
                disabled={!canGenerate}
                className="min-h-11"
              >
                Create draft
              </Button>
            </div>
          </section>

          {draftState && (
            <section className="p-5">
              <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                <div className="min-w-0">
                  <p className="font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                    {mode} / Content v{formatVersion(draftState.contentVersion)}{" "}
                    /{" "}
                    {sessionSaveState === "saved"
                      ? "Saved in this tab"
                      : "Not saved"}
                  </p>
                  <p className="text-muted-foreground text-xs [overflow-wrap:anywhere]">
                    {draftState.sourceLabel} · {draftState.changeLabel}
                  </p>
                </div>
                <Badge
                  variant={
                    staleReason
                      ? "destructive"
                      : draftState.decision === "pending"
                        ? "outline"
                        : "secondary"
                  }
                  className="font-mono uppercase"
                >
                  {staleReason ? "stale" : draftState.decision}
                </Badge>
              </div>

              {isEditing ? (
                <Textarea
                  aria-label="Edit Nova draft"
                  value={editBuffer}
                  onChange={(event) => setEditBuffer(event.target.value)}
                  className="min-h-72 min-w-0 rounded-none border-2 font-mono text-sm [overflow-wrap:anywhere] break-words shadow-none"
                />
              ) : (
                <div className="border-foreground max-w-full min-w-0 border-2 p-4 text-sm leading-6 [overflow-wrap:anywhere] break-words whitespace-pre-wrap">
                  {draftState.content}
                </div>
              )}

              <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
                {isEditing ? (
                  <>
                    <Button
                      size="sm"
                      className="min-h-11 sm:col-span-2"
                      onClick={saveEditing}
                    >
                      <Check className="size-4" aria-hidden="true" />
                      Save edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="min-h-11 sm:col-span-2"
                      onClick={cancelEditing}
                    >
                      <X className="size-4" aria-hidden="true" />
                      Cancel edit
                    </Button>
                  </>
                ) : (
                  <>
                    <Button
                      variant="outline"
                      size="sm"
                      className="min-h-11"
                      onClick={() => setDecision("accepted")}
                      disabled={
                        isEditing ||
                        draftState.decision === "accepted" ||
                        Boolean(staleReason)
                      }
                    >
                      <Check className="size-4" aria-hidden="true" />
                      Accept
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="min-h-11"
                      onClick={startEditing}
                    >
                      <FilePenLine className="size-4" aria-hidden="true" />
                      Edit
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      className="min-h-11"
                      onClick={() => setDecision("rejected")}
                      disabled={isEditing || draftState.decision === "rejected"}
                    >
                      <Trash2 className="size-4" aria-hidden="true" />
                      Reject
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="min-h-11"
                      onClick={undoLastChange}
                      disabled={isEditing || history.past.length === 0}
                    >
                      <RotateCcw className="size-4" aria-hidden="true" />
                      Undo
                    </Button>
                  </>
                )}
              </div>

              <p
                className={cn(
                  "bg-muted mt-3 border-l-4 p-3 text-xs leading-relaxed",
                  staleReason ? "border-destructive" : "border-primary",
                )}
                role="status"
                aria-live="polite"
                aria-atomic="true"
              >
                {isEditing
                  ? staleReason
                    ? "Editing a stale working copy. Save creates a local version, but it remains stale until regenerated from the current accepted source. Cancel restores the current artifact."
                    : hasUnsavedEdit
                      ? "Unsaved edit. Choose Save edit to create one new content version or Cancel edit to restore the current artifact. Recovery is kept in this tab when storage is available."
                      : "Editing the current artifact. Make a change, then choose Save edit or Cancel edit."
                  : (staleReason ??
                    (draftState.decision === "accepted"
                      ? "Accepted for this preview session only. This exact content version can be used by the next gated Nova interaction."
                      : draftState.decision === "rejected"
                        ? "Rejected in this preview session. No workspace record or downstream artifact was changed."
                        : "Pending your review. No workspace record or downstream artifact has changed."))}
              </p>

              <div className="mt-4 border-t-2 pt-4">
                <Button
                  variant="outline"
                  size="sm"
                  className="min-h-11 w-full sm:w-auto"
                  onClick={() => setShowComparison((visible) => !visible)}
                  disabled={!previousState || isEditing}
                  aria-expanded={showComparison}
                  aria-controls="nova-version-comparison"
                >
                  <GitCompareArrows className="size-4" aria-hidden="true" />
                  {showComparison ? "Hide comparison" : "Compare previous"}
                </Button>

                {showComparison && previousState && (
                  <div id="nova-version-comparison" className="mt-3 min-w-0">
                    <p className="mb-3 text-xs leading-relaxed" role="status">
                      {describeComparison(previousState, draftState)}
                    </p>
                    <div className="grid min-w-0 gap-3">
                      {(
                        [
                          ["Previous", previousState],
                          ["Current", draftState],
                        ] as const
                      ).map(([label, item]) => {
                        const itemStaleReason = getStaleReason(
                          mode,
                          item,
                          acceptedDraft,
                          acceptedPresentation,
                        );

                        return (
                          <section
                            key={label}
                            className="border-foreground min-w-0 border-2"
                            aria-label={label + " Nova state"}
                          >
                            <div className="border-b-2 p-3">
                              <p className="font-mono text-[0.65rem] font-bold tracking-[0.12em] uppercase">
                                {label} · Content v
                                {formatVersion(item.contentVersion)} ·{" "}
                                {itemStaleReason ? "stale" : item.decision}
                              </p>
                              <p className="text-muted-foreground mt-1 text-xs">
                                {item.changeLabel}
                              </p>
                              {itemStaleReason && (
                                <p className="text-destructive mt-1 text-xs leading-relaxed">
                                  {itemStaleReason}
                                </p>
                              )}
                            </div>
                            <div className="max-h-64 overflow-auto p-3 font-mono text-xs leading-5 [overflow-wrap:anywhere] break-words whitespace-pre-wrap">
                              {item.content}
                            </div>
                          </section>
                        );
                      })}
                    </div>
                  </div>
                )}
              </div>

              <details className="mt-5 border-t-2 pt-4">
                <summary className="min-h-11 cursor-pointer font-mono text-xs font-bold tracking-[0.1em] uppercase">
                  Sources, assumptions, and limits
                </summary>
                <div className="mt-3 grid gap-4 text-xs leading-relaxed sm:grid-cols-2">
                  <div>
                    <p className="mb-1 font-bold">Actually used</p>
                    <ul className="list-square list-inside space-y-1">
                      <li>Route label: {context.artifact}</li>
                      <li>Selected interaction: {mode}</li>
                      <li>{draftState.sourceLabel}</li>
                    </ul>
                    <p className="bg-muted mt-2 border-l-4 p-2 [overflow-wrap:anywhere]">
                      Prompt captured for this version: “
                      {draftState.sourcePrompt}”
                    </p>
                  </div>
                  <div>
                    <p className="mb-1 font-bold">Not connected in preview</p>
                    <ul className="list-square list-inside space-y-1">
                      {context.sources.map((source) => (
                        <li key={source}>{source}</li>
                      ))}
                    </ul>
                  </div>
                  <div>
                    <p className="mb-1 font-bold">Assuming</p>
                    <ul className="list-square list-inside space-y-1">
                      {context.assumptions.map((assumption) => (
                        <li key={assumption}>{assumption}</li>
                      ))}
                    </ul>
                  </div>
                  <div>
                    <p className="mb-1 font-bold">Uncertainty</p>
                    <p>{context.uncertainty}</p>
                  </div>
                </div>
              </details>
            </section>
          )}
        </div>

        <div className="bg-foreground text-background border-t-2 p-4">
          <p className="mb-2 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
            Nova cannot
          </p>
          <p className="text-xs leading-relaxed">
            {NOVA_NON_ACTIONS.join(" · ")}
          </p>
        </div>
      </SheetContent>
    </Sheet>
  );
}
