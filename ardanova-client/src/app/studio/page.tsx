"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  Check,
  FilePenLine,
  GitCompareArrows,
  Mic2,
  Presentation,
  RotateCcw,
  Sparkles,
  Trash2,
  X,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import {
  STUDIO_MODES,
  STUDIO_SESSION_KEY,
  compactStudioArtifacts,
  parseStudioSession,
  serializeStudioSession,
  type StudioArtifacts,
  type StudioHistory,
  type StudioMode,
  type StudioSnapshot,
  type StudioUpstreamMode,
} from "~/lib/studio/session-storage";
import { cn } from "~/lib/utils";

const studioModes = STUDIO_MODES;
type DraftDecision = "pending" | "accepted" | "rejected";

interface StudioUpstream {
  mode: StudioUpstreamMode;
  contentVersion: number;
}

const initialBrief = {
  title: "",
  purpose: "",
  people: "",
  evidence: "",
};

function buildProjectBrief(brief: typeof initialBrief): string {
  return [
    brief.title.toUpperCase() + " / WORKING BRIEF",
    "",
    "INTENT",
    brief.purpose,
    "",
    "PEOPLE AND ROLES",
    brief.people,
    "",
    "EVIDENCE",
    brief.evidence,
    "",
    "WORKING COMPACT",
    "Contributors review scope, evidence, and decision rights before work begins. Governance credentials, project utility, and economic rights are described separately.",
    "",
    "OPEN QUESTIONS",
    "What consent is still needed? Which claims require outside verification? What record will mark the first test complete?",
  ].join("\n");
}

function buildPresentationDraft(acceptedBrief: string): string {
  return [
    "PRESENTATION / ACCEPTED BRIEF",
    "",
    "01 / THE SITUATION",
    "Open with the problem and distinguish observed evidence from proposed outcomes.",
    "",
    "02 / THE PEOPLE",
    "Name who experiences the problem, who contributes, and who holds each decision.",
    "",
    "03 / THE PROPOSAL",
    "Present the smallest reversible test described by the accepted brief.",
    "",
    "04 / THE EVIDENCE",
    "Show what is verified, what is inferred, and what still needs a source.",
    "",
    "05 / THE COMPACT",
    "State contribution, governance credentials, project utility, and any economic rights separately.",
    "",
    "06 / THE NEXT DECISION",
    "Ask for one decision, name who can authorize it, and identify the record that will prove it happened.",
    "",
    "ACCEPTED SOURCE MATERIAL",
    acceptedBrief,
  ].join("\n");
}

function buildRehearsalDraft(acceptedPresentation: string): string {
  return [
    "REHEARSAL / ACCEPTED PRESENTATION",
    "",
    "Q1 — What proves this problem is shared, not assumed?",
    "Response prompt — Name the evidence you have and the evidence still needed.",
    "",
    "Q2 — Who can change the plan?",
    "Response prompt — Identify decision owners, affected groups, and a challenge path.",
    "",
    "Q3 — What does contribution grant?",
    "Response prompt — Separate participation, credentials, utility, ownership, and redemption rights.",
    "",
    "Q4 — What could fail first?",
    "Response prompt — Name the riskiest dependency and the smallest reversible test.",
    "",
    "Q5 — How will people know value was reconciled?",
    "Response prompt — Point to the status record, not an optimistic interface message.",
    "",
    "ACCEPTED PRESENTATION BEING REHEARSED",
    acceptedPresentation,
  ].join("\n");
}

function createInitialArtifacts(): StudioArtifacts {
  return {
    "Project brief": { past: [], present: null },
    Presentation: { past: [], present: null },
    Rehearsal: { past: [], present: null },
  };
}

function getLatestAccepted(
  history: StudioHistory,
  isEligible: (snapshot: StudioSnapshot) => boolean = () => true,
): StudioSnapshot | null {
  const latestStateByVersion = new Map<number, StudioSnapshot>();
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
  snapshot: StudioSnapshot,
  mode: StudioUpstreamMode,
  acceptedUpstream: StudioSnapshot | null,
): boolean {
  return Boolean(
    acceptedUpstream &&
      snapshot.upstream?.mode === mode &&
      snapshot.upstream.contentVersion === acceptedUpstream.contentVersion,
  );
}

function getStaleReason(
  mode: StudioMode,
  snapshot: StudioSnapshot | null,
  acceptedBrief: StudioSnapshot | null,
  acceptedPresentation: StudioSnapshot | null,
): string | null {
  if (!snapshot || mode === "Project brief") return null;

  const expectedMode: StudioUpstreamMode =
    mode === "Presentation" ? "Project brief" : "Presentation";
  const acceptedUpstream =
    mode === "Presentation" ? acceptedBrief : acceptedPresentation;

  if (hasCurrentUpstream(snapshot, expectedMode, acceptedUpstream)) return null;

  const capturedSource = snapshot.upstream
    ? `${snapshot.upstream.mode} content v${formatVersion(snapshot.upstream.contentVersion)}`
    : "an unversioned source";
  const currentSource = acceptedUpstream
    ? `${expectedMode} content v${formatVersion(acceptedUpstream.contentVersion)}`
    : `no currently accepted ${expectedMode.toLowerCase()} artifact`;

  return `Stale source: this ${mode.toLowerCase()} uses ${capturedSource}; the current source is ${currentSource}. Regenerate it before accepting or using it downstream.`;
}

function formatVersion(version: number): string {
  return String(version).padStart(2, "0");
}

function describeComparison(
  previous: StudioSnapshot,
  current: StudioSnapshot,
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

export default function StudioPage() {
  const [mode, setMode] = useState<StudioMode>("Project brief");
  const [brief, setBrief] = useState(initialBrief);
  const [artifacts, setArtifacts] = useState<StudioArtifacts>(
    createInitialArtifacts,
  );
  const [isEditing, setIsEditing] = useState(false);
  const [editBuffer, setEditBuffer] = useState("");
  const [showComparison, setShowComparison] = useState(false);
  const [sessionRestored, setSessionRestored] = useState(false);
  const [sessionSaveState, setSessionSaveState] = useState<
    "restoring" | "saved" | "unavailable"
  >("restoring");
  const persistenceBlockedRef = useRef(false);

  const history = artifacts[mode];
  const draftState = history.present;
  const previousState = history.past.at(-1) ?? null;
  const hasUnsavedEdit = Boolean(
    isEditing && draftState && editBuffer !== draftState.content,
  );
  const acceptedBrief = getLatestAccepted(artifacts["Project brief"]);
  const latestAcceptedPresentation = getLatestAccepted(artifacts.Presentation);
  const acceptedPresentation = getLatestAccepted(
    artifacts.Presentation,
    (snapshot) => hasCurrentUpstream(snapshot, "Project brief", acceptedBrief),
  );
  const staleReason = getStaleReason(
    mode,
    draftState,
    acceptedBrief,
    acceptedPresentation,
  );

  const scopeLabel = useMemo(() => {
    if (mode === "Presentation") {
      return acceptedBrief
        ? "Accepted project brief content v" +
            formatVersion(acceptedBrief.contentVersion) +
            " → presentation outline"
        : "Accept a project brief before creating a presentation.";
    }
    if (mode === "Rehearsal") {
      return acceptedPresentation
        ? "Accepted presentation content v" +
            formatVersion(acceptedPresentation.contentVersion) +
            " → rehearsal prompts"
        : "Accept a presentation before creating a rehearsal.";
    }
    return "Brief fields shown at left → working project brief";
  }, [acceptedBrief, acceptedPresentation, mode]);

  const briefHasUnacceptedChanges =
    acceptedBrief !== null &&
    buildProjectBrief(brief) !== acceptedBrief.content;

  useEffect(() => {
    try {
      const raw = window.sessionStorage.getItem(STUDIO_SESSION_KEY);
      if (raw) {
        const restored = parseStudioSession(raw);
        if (restored) {
          const recoveredEdit = restored.edit;
          const recoveredMode =
            recoveredEdit &&
            restored.artifacts[recoveredEdit.mode].present !== null
              ? recoveredEdit.mode
              : restored.mode;
          setMode(recoveredMode);
          setBrief(restored.brief);
          setArtifacts(restored.artifacts);
          if (recoveredEdit && recoveredEdit.mode === recoveredMode) {
            setEditBuffer(recoveredEdit.buffer);
            setIsEditing(true);
          }
        } else {
          persistenceBlockedRef.current = true;
          setSessionSaveState("unavailable");
        }
      }
    } catch {
      persistenceBlockedRef.current = true;
      setSessionSaveState("unavailable");
    } finally {
      setSessionRestored(true);
    }
  }, []);

  useEffect(() => {
    if (!sessionRestored || persistenceBlockedRef.current) return;

    const timeout = window.setTimeout(() => {
      const payload = {
        version: 2 as const,
        mode,
        brief,
        artifacts: compactStudioArtifacts(artifacts),
        edit: isEditing ? { mode, buffer: editBuffer } : null,
      };
      const serialized = serializeStudioSession(payload);
      if (!serialized || !parseStudioSession(serialized)) {
        setSessionSaveState("unavailable");
        return;
      }

      try {
        window.sessionStorage.setItem(STUDIO_SESSION_KEY, serialized);
        setSessionSaveState("saved");
      } catch {
        setSessionSaveState("unavailable");
      }
    }, 180);

    return () => window.clearTimeout(timeout);
  }, [artifacts, brief, editBuffer, isEditing, mode, sessionRestored]);

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
          "Leave with an unsaved Studio edit? Choose Cancel to return and save it. A recovery copy is kept in this tab when storage is available.",
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

  function updateBrief(field: keyof typeof brief, value: string) {
    setBrief((current) => ({ ...current, [field]: value }));
  }

  function canOpenMode(nextMode: StudioMode): boolean {
    if (nextMode === "Presentation") {
      return acceptedBrief !== null || artifacts.Presentation.present !== null;
    }
    if (nextMode === "Rehearsal") {
      return (
        acceptedPresentation !== null || artifacts.Rehearsal.present !== null
      );
    }
    return true;
  }

  function commitState(targetMode: StudioMode, next: StudioSnapshot) {
    setArtifacts((current) => {
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

  function changeMode(nextMode: StudioMode) {
    if (isEditing || !canOpenMode(nextMode)) return;
    setMode(nextMode);
    setShowComparison(false);
  }

  function generateDraft() {
    if (isEditing) return;
    let content: string;
    let sourceLabel: string;
    let upstream: StudioUpstream | null = null;

    if (mode === "Presentation") {
      if (!acceptedBrief) return;
      content = buildPresentationDraft(acceptedBrief.content);
      upstream = {
        mode: "Project brief",
        contentVersion: acceptedBrief.contentVersion,
      };
      sourceLabel =
        "Accepted project brief content v" +
        formatVersion(acceptedBrief.contentVersion);
    } else if (mode === "Rehearsal") {
      if (!acceptedPresentation) return;
      content = buildRehearsalDraft(acceptedPresentation.content);
      upstream = {
        mode: "Presentation",
        contentVersion: acceptedPresentation.contentVersion,
      };
      sourceLabel =
        "Accepted presentation content v" +
        formatVersion(acceptedPresentation.contentVersion);
    } else {
      content = buildProjectBrief(brief);
      sourceLabel = "Brief fields captured when this version was created";
    }

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
      changeLabel: "Local draft generated for review",
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

    setArtifacts((current) => {
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
    (mode === "Project brief"
      ? Boolean(brief.title.trim() && brief.purpose.trim())
      : mode === "Presentation"
        ? acceptedBrief !== null
        : acceptedPresentation !== null);

  return (
    <div className="py-8 sm:py-12">
      <header className="border-foreground relative overflow-hidden border-2 p-5 sm:p-8">
        <div
          className="bg-primary absolute top-0 right-0 h-full w-5 sm:w-8"
          aria-hidden="true"
        />
        <div className="max-w-5xl pr-6">
          <div className="mb-5 flex flex-wrap items-center gap-2">
            <Badge variant="outline" className="font-mono uppercase">
              Interface preview
            </Badge>
            <span className="text-muted-foreground font-mono text-xs tracking-[0.12em] uppercase">
              Local drafts · no AI service connected ·{" "}
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
          <h1 className="max-w-6xl font-mono text-[clamp(2.7rem,8vw,7rem)] leading-[0.84] font-black tracking-[-0.09em] uppercase">
            Shape the work.
            <br />
            Keep the agency.
          </h1>
          <p className="mt-6 max-w-2xl text-base leading-relaxed sm:text-lg">
            Nova Studio is a reviewable workspace for drafting a project,
            shaping its presentation, and rehearsing the hard questions. You
            remain the author and decision-maker.
          </p>
        </div>
      </header>

      <section className="border-foreground border-x-2 border-b-2">
        <div className="grid md:grid-cols-3">
          {[
            [
              "01 / Scope",
              "Nova uses only the brief and accepted drafts in this session.",
            ],
            ["02 / Evidence", "Claims stay marked for human verification."],
            [
              "03 / Agency",
              "Publishing, funding, rights, and approvals stay human.",
            ],
          ].map(([label, copy], index) => (
            <div
              key={label}
              className={cn(
                "p-4 sm:p-5",
                index > 0 && "border-t-2 md:border-t-0 md:border-l-2",
              )}
            >
              <p className="mb-2 font-mono text-xs font-bold tracking-[0.12em] uppercase">
                {label}
              </p>
              <p className="text-muted-foreground text-sm leading-relaxed">
                {copy}
              </p>
            </div>
          ))}
        </div>
      </section>

      <div className="mt-8 grid min-w-0 items-start gap-6 xl:grid-cols-[minmax(17rem,0.8fr)_minmax(24rem,1.4fr)_minmax(16rem,0.7fr)]">
        <section className="border-foreground order-1 min-w-0 border-2 xl:order-none">
          <div className="bg-foreground text-background border-b-2 p-4">
            <p className="font-mono text-xs font-bold tracking-[0.14em] uppercase">
              Source / Project brief
            </p>
          </div>
          <div className="space-y-5 p-5">
            <div className="space-y-2">
              <Label htmlFor="studio-title">Working title</Label>
              <Input
                id="studio-title"
                value={brief.title}
                onChange={(event) => updateBrief("title", event.target.value)}
                className="min-w-0 border-2 [overflow-wrap:anywhere]"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="studio-purpose">Purpose / change to test</Label>
              <Textarea
                id="studio-purpose"
                value={brief.purpose}
                onChange={(event) => updateBrief("purpose", event.target.value)}
                className="min-h-32 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="studio-people">People / roles</Label>
              <Textarea
                id="studio-people"
                value={brief.people}
                onChange={(event) => updateBrief("people", event.target.value)}
                className="min-h-28 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="studio-evidence">Evidence / unknowns</Label>
              <Textarea
                id="studio-evidence"
                value={brief.evidence}
                onChange={(event) =>
                  updateBrief("evidence", event.target.value)
                }
                className="min-h-28 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
              />
            </div>
            {briefHasUnacceptedChanges && acceptedBrief && (
              <p className="bg-muted border-primary border-l-4 p-3 text-xs leading-relaxed">
                These fields differ from accepted brief content v
                {formatVersion(acceptedBrief.contentVersion)}. Presentation mode
                continues to use that accepted version until you create and
                accept a new brief.
              </p>
            )}
          </div>
        </section>

        <section className="border-foreground order-3 min-w-0 border-2 xl:order-none">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b-2 p-4">
            <div className="min-w-0">
              <p className="font-mono text-xs font-bold tracking-[0.14em] uppercase">
                Artifact / {mode}
                {draftState
                  ? " · Content v" + formatVersion(draftState.contentVersion)
                  : " · No draft yet"}
              </p>
              <p className="text-muted-foreground mt-1 text-xs [overflow-wrap:anywhere]">
                {draftState
                  ? draftState.sourceLabel + " · " + draftState.changeLabel
                  : scopeLabel}
              </p>
            </div>
            {draftState && (
              <Badge
                variant={
                  staleReason
                    ? "destructive"
                    : draftState.decision === "pending"
                      ? "outline"
                      : "secondary"
                }
              >
                {staleReason ? "stale" : draftState.decision}
              </Badge>
            )}
          </div>

          <div className="p-4 sm:p-6">
            {!draftState ? (
              <div className="bg-muted border-foreground border-2 p-6 text-sm leading-relaxed">
                <p className="font-bold">No {mode.toLowerCase()} draft yet.</p>
                <p className="text-muted-foreground mt-2">
                  {scopeLabel}. Creating one starts content version 01 and
                  leaves it pending your review.
                </p>
              </div>
            ) : isEditing ? (
              <Textarea
                aria-label="Edit studio draft"
                value={editBuffer}
                onChange={(event) => setEditBuffer(event.target.value)}
                className="min-h-[34rem] min-w-0 rounded-none border-2 font-mono text-sm leading-6 [overflow-wrap:anywhere] break-words shadow-none"
              />
            ) : (
              <article className="bg-card border-primary max-w-full min-w-0 border-l-8 p-5 font-mono text-sm leading-7 [overflow-wrap:anywhere] break-words whitespace-pre-wrap sm:p-7">
                {draftState.content}
              </article>
            )}

            {draftState && (
              <>
                <div className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-4">
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
                        disabled={
                          isEditing || draftState.decision === "rejected"
                        }
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
                    "bg-muted mt-4 border-l-4 p-3 text-xs leading-relaxed",
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
                        ? "Accepted inside this browser preview only. This exact content version may now be used by the next Studio mode."
                        : draftState.decision === "rejected"
                          ? "Rejected locally. No project record or downstream artifact was changed."
                          : "Awaiting human review. No project record or downstream artifact has changed."))}
                </p>

                <div className="mt-4 border-t-2 pt-4">
                  <Button
                    variant="outline"
                    size="sm"
                    className="min-h-11 w-full sm:w-auto"
                    onClick={() => setShowComparison((visible) => !visible)}
                    disabled={!previousState || isEditing}
                    aria-expanded={showComparison}
                    aria-controls="studio-version-comparison"
                  >
                    <GitCompareArrows className="size-4" aria-hidden="true" />
                    {showComparison ? "Hide comparison" : "Compare previous"}
                  </Button>

                  {showComparison && previousState && (
                    <div
                      id="studio-version-comparison"
                      className="mt-3 min-w-0"
                    >
                      <p className="mb-3 text-xs leading-relaxed" role="status">
                        {describeComparison(previousState, draftState)}
                      </p>
                      <div className="grid min-w-0 gap-3 lg:grid-cols-2">
                        {(
                          [
                            ["Previous", previousState],
                            ["Current", draftState],
                          ] as const
                        ).map(([label, item]) => {
                          const itemStaleReason = getStaleReason(
                            mode,
                            item,
                            acceptedBrief,
                            acceptedPresentation,
                          );

                          return (
                            <section
                              key={label}
                              className="border-foreground min-w-0 border-2"
                              aria-label={label + " artifact state"}
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
                              <div className="max-h-72 overflow-auto p-3 font-mono text-xs leading-5 [overflow-wrap:anywhere] break-words whitespace-pre-wrap">
                                {item.content}
                              </div>
                            </section>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        </section>

        <aside className="border-foreground order-2 min-w-0 border-2 xl:sticky xl:top-28 xl:order-none">
          <div className="bg-primary text-primary-foreground border-b-2 p-4">
            <p className="flex items-center gap-2 font-mono text-xs font-bold tracking-[0.14em] uppercase">
              <Sparkles className="size-4" aria-hidden="true" />
              Nova control
            </p>
          </div>
          <div className="p-4">
            <fieldset>
              <legend className="mb-2 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                Output mode
              </legend>
              <div className="space-y-2">
                {studioModes.map((item) => (
                  <button
                    key={item}
                    type="button"
                    aria-pressed={mode === item}
                    onClick={() => changeMode(item)}
                    disabled={isEditing || !canOpenMode(item)}
                    className={cn(
                      "flex min-h-12 w-full items-center gap-3 border-2 px-3 text-left text-sm font-bold transition-colors disabled:cursor-not-allowed disabled:opacity-50",
                      mode === item
                        ? "bg-foreground text-background"
                        : "hover:bg-muted",
                    )}
                  >
                    {item === "Project brief" ? (
                      <Sparkles className="size-4" aria-hidden="true" />
                    ) : item === "Presentation" ? (
                      <Presentation className="size-4" aria-hidden="true" />
                    ) : (
                      <Mic2 className="size-4" aria-hidden="true" />
                    )}
                    {item}
                  </button>
                ))}
              </div>
            </fieldset>

            <div className="my-5 space-y-3 border-y-2 py-4">
              <div>
                <p className="mb-1 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                  Scope
                </p>
                <p className="text-sm leading-relaxed">{scopeLabel}</p>
              </div>
              <div className="text-muted-foreground space-y-1 text-xs leading-relaxed">
                <p>
                  Presentation:{" "}
                  {acceptedBrief
                    ? "ready from accepted brief content v" +
                      formatVersion(acceptedBrief.contentVersion)
                    : "accept a project brief to unlock"}
                  .
                </p>
                <p>
                  Rehearsal:{" "}
                  {acceptedPresentation
                    ? "ready from accepted presentation content v" +
                      formatVersion(acceptedPresentation.contentVersion)
                    : latestAcceptedPresentation
                      ? "the accepted presentation is stale; regenerate it from the current brief and accept it"
                      : "accept a presentation to unlock"}
                  .
                </p>
                <p>
                  No external sources or private project records are connected.
                </p>
              </div>
            </div>

            <Button
              onClick={generateDraft}
              disabled={!canGenerate}
              className="min-h-11 w-full"
            >
              Create interface draft
            </Button>

            <div className="mt-6">
              <p className="mb-2 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                Always human
              </p>
              <ul className="list-square list-inside space-y-2 text-xs leading-relaxed">
                <li>Accept, reject, edit, publish, or send</li>
                <li>Fund, approve, release, swap, or pay out</li>
                <li>Grant credentials, ownership, or economic rights</li>
                <li>Confirm evidence or contributor consent</li>
              </ul>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
