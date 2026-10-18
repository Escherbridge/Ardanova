"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { ClipboardCopy, FilePlus2, RotateCcw, Send } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Textarea } from "~/components/ui/textarea";
import { useEnumOptions } from "~/hooks/use-enum";
import {
  NOVA_TURN_TIMEOUT_MS,
  type NovaBrief,
  type NovaRefineResponse,
  type NovaStatus,
  type NovaTurn,
} from "~/lib/api/ardanova/endpoints/nova";
import { api } from "~/trpc/react";
import { briefAsProjectDraft } from "./nova-brief-draft";

/** One project-refinement conversation, held by NovaAssistant so it survives the sheet closing. */
export interface NovaRefineConversation {
  idea: string;
  turns: NovaTurn[];
  reply: NovaRefineResponse | null;
  answer: string;
}

export const emptyRefineConversation: NovaRefineConversation = {
  idea: "",
  turns: [],
  reply: null,
  answer: "",
};

const MIN_IDEA = 20;
const MAX_IDEA = 2000;

/** The slowest turn the member may wait for, in whole seconds (the BFF's own Nova timeout). */
export const NOVA_WORST_CASE_SECONDS = Math.ceil(NOVA_TURN_TIMEOUT_MS / 1000);

/** Shown while a turn is in flight, so a slow (retry and fallback) answer still reads as normal. */
export const pendingNote = `Most answers take a few seconds. A hard question can take up to ${NOVA_WORST_CASE_SECONDS} seconds; keep this open.`;

const outcomeLabel: Record<string, string> = {
  go: "Go",
  adjust: "Adjust first",
  not_yet: "Not yet",
  preview: "Preview only",
};

/** Member-facing copy for a failed turn, keyed by the tRPC code the nova router maps. */
export function describeError(code: string | undefined): string {
  if (code === "TOO_MANY_REQUESTS")
    return "You have used all of your Nova AI refinements for today. Your idea is still here; try again tomorrow.";
  if (code === "CONFLICT")
    return "Nova AI is still answering your last question, here or in another tab. Wait for that answer, then try again. Your idea and answers are still here.";
  if (code === "SERVICE_UNAVAILABLE")
    return "Nova AI is at capacity for today across ArdaNova. This is not your own limit. Your idea and answers are still here; try again tomorrow.";
  if (code === "TIMEOUT")
    return "Nova AI took too long to answer. Your idea and answers are still here; try again.";
  if (code === "BAD_REQUEST")
    return "Nova AI could not use that input. Check the idea and your answers, then try again.";
  if (code === "UNAUTHORIZED") return "Sign in again to use Nova AI.";
  if (code === "NOT_FOUND")
    return "This refinement has expired. Start again from your idea.";
  return "Nova AI is unavailable right now. Your idea and answers are still here; try again in a moment.";
}

/** Plain-text brief for the clipboard, so a member can paste it into a new project. */
export function briefAsText(brief: NovaBrief): string {
  const list = (title: string, items: string[]) =>
    items.length > 0 ? [title, ...items.map((item) => `- ${item}`)] : [];
  return [
    brief.workingName,
    brief.oneLiner,
    "",
    `Customer: ${brief.customer}`,
    `Problem: ${brief.problem}`,
    `Offer: ${brief.offer}`,
    `Pricing: ${brief.pricing}`,
    ...list("First channels", brief.channels),
    ...list("Milestones", brief.milestones),
    ...list("Numbers to watch", brief.metrics),
  ].join("\n");
}

export function NovaRefinePanel({
  conversation,
  onChange,
  availability,
  allowance,
}: {
  conversation: NovaRefineConversation;
  onChange: (next: NovaRefineConversation) => void;
  /** From nova.status; undefined while loading and null when unknown, which both keep the form. */
  availability?: NovaStatus["refinement"] | null;
  /** From nova.status when live: the member's daily limit and what is left of it. */
  allowance?: { dailyLimit: number; remainingToday: number } | null;
}) {
  const refine = api.nova.refineProject.useMutation();
  const createDraft = api.project.create.useMutation();
  const utils = api.useUtils();
  const { options: categories } = useEnumOptions("ProjectCategory");
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [limitReached, setLimitReached] = useState(false);
  const [category, setCategory] = useState("");
  const [draft, setDraft] = useState<{ slug: string; title: string } | null>(
    null,
  );
  const [announcement, setAnnouncement] = useState("");
  const answerRef = useRef<HTMLTextAreaElement>(null);
  const verdictRef = useRef<HTMLDivElement>(null);
  const moveFocus = useRef(false);
  const { idea, turns, reply, answer } = conversation;
  const ideaLength = idea.trim().length;
  const started = reply !== null;
  const isPreview =
    reply?.mode === "local-preview" ||
    (reply === null && availability === "local-preview");
  const pendingLabel = "Asking Nova AI…";
  const outOfRefinements =
    limitReached || (!isPreview && allowance?.remainingToday === 0);

  // After a member's own turn, focus follows the conversation: the next answer box, or the verdict.
  useEffect(() => {
    if (!moveFocus.current) return;
    moveFocus.current = false;
    if (reply?.type === "question") answerRef.current?.focus();
    else if (reply?.type === "verdict") verdictRef.current?.focus();
  }, [reply]);

  async function copyBrief(brief: NovaBrief) {
    try {
      await navigator.clipboard.writeText(briefAsText(brief));
      setCopied(true);
    } catch {
      setError(
        "Copying is blocked in this browser. Select the brief and copy it instead.",
      );
    }
  }

  if (availability === "off" && !started) {
    return (
      <section className="border-b-2 p-5" aria-labelledby="nova-refine-heading">
        <h3
          id="nova-refine-heading"
          className="mb-3 font-mono text-sm font-black tracking-[0.08em] uppercase"
        >
          Refine a project idea
        </h3>
        <p
          className="bg-muted border-l-4 p-3 text-xs leading-relaxed"
          role="status"
        >
          Nova AI project refinement is not available on this deployment.
          Nothing you type here would be sent. The preview tools still work
          locally.
        </p>
      </section>
    );
  }

  async function send(nextTurns: NovaTurn[]) {
    setError(null);
    setAnnouncement("");
    try {
      const next = await refine.mutateAsync({
        idea,
        turns: nextTurns,
        state: nextTurns.length > 0 ? (reply?.state ?? undefined) : undefined,
        sessionId:
          nextTurns.length > 0 ? (reply?.sessionId ?? undefined) : undefined,
      });
      setCopied(false);
      setDraft(null);
      moveFocus.current = true;
      setAnnouncement(
        next.type === "question"
          ? `Nova AI asked question ${next.number ?? nextTurns.length + 1}: ${next.question ?? ""}`
          : `Nova AI gave its verdict: ${outcomeLabel[next.verdict?.outcome ?? ""] ?? next.verdict?.outcome ?? ""}.`,
      );
      if (nextTurns.length === 0) void utils.nova.status.invalidate();
      onChange({ idea, turns: nextTurns, reply: next, answer: "" });
    } catch (cause) {
      const code = (cause as { data?: { code?: string } } | null)?.data?.code;
      if (code === "TOO_MANY_REQUESTS") setLimitReached(true);
      setError(describeError(code));
    }
  }

  async function draftProject(brief: NovaBrief) {
    if (!category) return;
    setError(null);
    try {
      const project = await createDraft.mutateAsync(
        briefAsProjectDraft(brief, idea, category),
      );
      setDraft({ slug: project.slug, title: project.title });
    } catch {
      setError(
        "The draft project could not be created. The brief is still here; copy it or try again.",
      );
    }
  }

  function answerQuestion() {
    if (!reply?.question || !answer.trim()) return;
    void send([...turns, { question: reply.question, answer: answer.trim() }]);
  }

  return (
    <section className="border-b-2 p-5" aria-labelledby="nova-refine-heading">
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <h3
          id="nova-refine-heading"
          className="font-mono text-sm font-black tracking-[0.08em] uppercase"
        >
          Refine a project idea
        </h3>
        <Badge
          variant={isPreview ? "outline" : "secondary"}
          className="font-mono uppercase"
        >
          {isPreview ? "Local preview" : "Nova AI"}
        </Badge>
      </div>
      <p className="text-muted-foreground mb-4 text-xs leading-relaxed">
        {isPreview
          ? "Nova AI is not connected in this environment. These are fixed practice questions and nothing you write is analysed."
          : "Nova AI asks a few hard questions about your idea, then gives a feasibility verdict: go, adjust first, or not yet. Your idea and answers go through ArdaNova's server to the Nova AI service for this conversation only; nothing is saved to ArdaNova records."}
      </p>

      <label
        htmlFor="nova-idea"
        className="mb-2 block font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase"
      >
        Your project idea
      </label>
      <Textarea
        id="nova-idea"
        value={idea}
        onChange={(event) =>
          onChange({ ...conversation, idea: event.target.value })
        }
        disabled={started || refine.isPending}
        maxLength={MAX_IDEA}
        placeholder="Who it is for, the problem it solves, and how it would work."
        className="min-h-24 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
      />
      {!started && (
        <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
          <p className="text-muted-foreground text-xs">
            {ideaLength < MIN_IDEA
              ? `Write at least ${MIN_IDEA} characters to start (${ideaLength} so far).`
              : `${ideaLength} of ${MAX_IDEA} characters.`}
            {!isPreview && allowance && (
              <span className="block">
                {`${allowance.remainingToday} of ${allowance.dailyLimit} refinements left today.`}
              </span>
            )}
          </p>
          <Button
            className="min-h-11"
            onClick={() => void send([])}
            disabled={
              ideaLength < MIN_IDEA || refine.isPending || outOfRefinements
            }
          >
            <Send className="size-4" aria-hidden="true" />
            {refine.isPending ? pendingLabel : "Start refinement"}
          </Button>
        </div>
      )}

      {turns.length > 0 && (
        <ol
          className="mt-4 space-y-2 text-xs leading-relaxed"
          aria-label="Questions answered so far"
        >
          {turns.map((turn, index) => (
            <li key={index} className="border-l-4 pl-3">
              <p className="font-bold">{turn.question}</p>
              <p className="text-muted-foreground [overflow-wrap:anywhere]">
                {turn.answer}
              </p>
            </li>
          ))}
        </ol>
      )}

      {reply?.type === "question" && (
        <div className="mt-4">
          <label
            htmlFor="nova-answer"
            className="mb-2 block text-sm font-semibold"
          >
            <span className="text-muted-foreground mr-2 font-mono text-[0.65rem] tracking-[0.14em] uppercase">
              Question {reply.number} of up to {reply.maxQuestions}
            </span>
            <span className="block">{reply.question}</span>
          </label>
          <Textarea
            id="nova-answer"
            ref={answerRef}
            value={answer}
            onChange={(event) =>
              onChange({ ...conversation, answer: event.target.value })
            }
            disabled={refine.isPending}
            maxLength={1500}
            className="min-h-20 min-w-0 rounded-none border-2 [overflow-wrap:anywhere] break-words shadow-none"
          />
          <Button
            className="mt-3 min-h-11"
            onClick={answerQuestion}
            disabled={!answer.trim() || refine.isPending}
          >
            <Send className="size-4" aria-hidden="true" />
            {refine.isPending ? pendingLabel : "Answer"}
          </Button>
        </div>
      )}

      {refine.isPending && (
        <p className="text-muted-foreground mt-2 text-xs" role="status">
          {pendingNote}
        </p>
      )}
      <p className="sr-only" aria-live="polite">
        {announcement}
      </p>

      {reply?.type === "verdict" && reply.verdict && (
        <div
          ref={verdictRef}
          tabIndex={-1}
          aria-labelledby="nova-verdict-headline"
          className="border-foreground mt-4 border-2 p-4 focus-visible:outline-2"
        >
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <Badge
              variant={reply.verdict.outcome === "go" ? "secondary" : "outline"}
              className="font-mono uppercase"
            >
              {outcomeLabel[reply.verdict.outcome] ?? reply.verdict.outcome}
            </Badge>
            <p id="nova-verdict-headline" className="text-sm font-bold">
              {reply.verdict.headline}
            </p>
          </div>
          <p className="text-sm leading-relaxed">{reply.verdict.summary}</p>
          {reply.verdict.strengths.length > 0 && (
            <VerdictList title="Holds up" items={reply.verdict.strengths} />
          )}
          {reply.verdict.risks.length > 0 && (
            <VerdictList title="Needs work" items={reply.verdict.risks} />
          )}
          <p className="bg-muted mt-3 border-l-4 p-2 text-xs leading-relaxed">
            <span className="font-bold">Next step: </span>
            {reply.verdict.nextStep}
          </p>
          {reply.verdict.sources.length > 0 && (
            <div className="mt-3 text-xs">
              <p className="mb-1 font-bold">
                Public web sources the verdict relied on (may be wrong or out of
                date)
              </p>
              <ul className="list-square list-inside space-y-1">
                {reply.verdict.sources.map((source) => (
                  <li key={source.url}>
                    <a
                      href={source.url}
                      target="_blank"
                      rel="noopener noreferrer nofollow"
                      className="[overflow-wrap:anywhere] underline"
                    >
                      {source.title}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}
          {reply.brief && (
            <div className="mt-4 border-t-2 pt-3 text-xs leading-relaxed">
              <p className="mb-2 font-mono text-[0.65rem] font-bold tracking-[0.14em] uppercase">
                Refined brief: {reply.brief.workingName}
              </p>
              <p className="mb-2">{reply.brief.oneLiner}</p>
              <dl className="grid gap-1 sm:grid-cols-[8rem_1fr]">
                <dt className="font-bold">Customer</dt>
                <dd>{reply.brief.customer}</dd>
                <dt className="font-bold">Problem</dt>
                <dd>{reply.brief.problem}</dd>
                <dt className="font-bold">Offer</dt>
                <dd>{reply.brief.offer}</dd>
                <dt className="font-bold">Pricing</dt>
                <dd>{reply.brief.pricing}</dd>
              </dl>
              <VerdictList
                title="First channels"
                items={reply.brief.channels}
              />
              <VerdictList title="Milestones" items={reply.brief.milestones} />
              <VerdictList
                title="Numbers to watch"
                items={reply.brief.metrics}
              />
              <p className="text-muted-foreground mt-2">
                This brief is a starting point for you to review. Nova AI has
                not created or changed any project; a draft is made only when
                you choose to create one, and stays private until you publish
                it.
              </p>
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <Button
                  variant="outline"
                  className="min-h-11"
                  onClick={() => reply.brief && void copyBrief(reply.brief)}
                >
                  <ClipboardCopy className="size-4" aria-hidden="true" />
                  {copied ? "Brief copied" : "Copy brief"}
                </Button>
                <Button asChild variant="ghost" className="min-h-11">
                  <Link href="/projects/create">Start a project</Link>
                </Button>
              </div>
              <p className="text-muted-foreground mt-2" aria-live="polite">
                {copied
                  ? "Paste it into the new project form. Nothing is filled in for you."
                  : "Copy the brief, then paste it into the new project form."}
              </p>
              <div className="mt-3 border-t pt-3">
                {draft ? (
                  <p role="status">
                    Draft &ldquo;{draft.title}&rdquo; created. Only you can see
                    it until you publish it.{" "}
                    <Link
                      href={`/projects/${draft.slug}`}
                      className="font-bold underline"
                    >
                      Open the draft
                    </Link>
                  </p>
                ) : (
                  <div className="flex flex-wrap items-end gap-2">
                    <label className="grid gap-1">
                      <span className="font-bold">Category for the draft</span>
                      <select
                        value={category}
                        onChange={(event) => setCategory(event.target.value)}
                        className="bg-background min-h-11 border-2 px-2"
                      >
                        <option value="">Choose a category</option>
                        {categories.map((option) => (
                          <option key={option.id} value={option.id}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </label>
                    <Button
                      variant="outline"
                      className="min-h-11"
                      onClick={() =>
                        reply.brief && void draftProject(reply.brief)
                      }
                      disabled={!category || createDraft.isPending}
                    >
                      <FilePlus2 className="size-4" aria-hidden="true" />
                      {createDraft.isPending
                        ? "Creating draft…"
                        : "Create a draft project"}
                    </Button>
                  </div>
                )}
                {!draft && (
                  <p className="text-muted-foreground mt-1">
                    Uses the brief&apos;s name, one-liner, problem, offer and
                    customer. You review and edit everything before publishing.
                  </p>
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {error && (
        <p
          className="border-destructive bg-muted mt-3 border-l-4 p-3 text-xs leading-relaxed"
          role="alert"
        >
          {error}
        </p>
      )}

      {started && (
        <Button
          variant="ghost"
          size="sm"
          className="mt-3 min-h-11"
          onClick={() => {
            setError(null);
            setCopied(false);
            setDraft(null);
            setAnnouncement("");
            onChange({ ...emptyRefineConversation, idea });
          }}
          disabled={refine.isPending}
        >
          <RotateCcw className="size-4" aria-hidden="true" />
          Start over
        </Button>
      )}
      {started && !isPreview && (
        <p className="text-muted-foreground mt-1 text-xs">
          Starting over begins a new refinement, which counts toward your daily
          Nova AI limit.
        </p>
      )}
    </section>
  );
}

function VerdictList({ title, items }: { title: string; items: string[] }) {
  if (items.length === 0) return null;
  return (
    <div className="mt-3 text-xs">
      <p className="mb-1 font-bold">{title}</p>
      <ul className="list-square list-inside space-y-1">
        {items.map((item, index) => (
          <li key={index}>{item}</li>
        ))}
      </ul>
    </div>
  );
}
