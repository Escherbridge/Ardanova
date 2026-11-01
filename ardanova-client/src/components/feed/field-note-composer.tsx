"use client";

import { useId, useState } from "react";
import Link from "next/link";
import { Loader2, Send } from "lucide-react";

import { Button } from "~/components/ui/button";
import { MutationErrorNotice } from "~/components/ui/query-error-state";
import type { FeedCard } from "~/lib/contracts/feed-contract";
import { feedRoutes } from "~/lib/feed/routes";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

import { ComposerSelect } from "./composer/composer-select";
import { HashtagTextarea } from "./composer/hashtag-textarea";
import { helpWantedExistingPostIdOf } from "./composer/help-conflict";
import { PromptTabs, promptRadioId } from "./composer/prompt-tabs";
import {
  COMPOSER_PROMPTS,
  attachmentFor,
  createInputFor,
  type ComposerAttachment,
  type ComposerPrompt,
} from "./composer/prompts";
import {
  cardGuildOf,
  cardProjectOf,
  feedCardFromPost,
  type ComposerAuthor,
  type PublishedPost,
} from "./composer/published-card";
import {
  useAskableTasks,
  useAttachableSubjects,
} from "./composer/use-composer-subjects";
import {
  audienceDescription,
  audienceLabel,
  audienceOptionsFor,
  type AudienceContext,
  type FieldNoteAudience,
} from "./field-note-audience";

/** Props contract for mounting lanes (G Home, H project page, W work-item modal); see ./composer/AGENTS.md §Props. */
export interface FieldNoteComposerProps {
  /** Fixes the note to this project (no attach picker). The pre-Phase-3 prop. */
  projectId?: string;
  /** Fixes the note to this guild (no attach picker, no "Who can help?"). */
  guildId?: string;
  /** Opens on "Who can help?" when "help"; defaults to "help" when `taskId` is given, else "note". */
  mode?: "note" | "help";
  /** Fixes the task an ask names, e.g. from the work-item modal. */
  taskId?: string;
  /** The signed-in person: names the published card and tells "Edit your ask" whose ask it found. */
  author?: ComposerAuthor;
  /** Fires after every successful publish or ask edit. The pre-Phase-3 prop. */
  onPosted?: () => void;
  /** Fires with the published post as a feed card (same id after an ask edit), for `FeedStream leadingCards`. */
  onPublished?: (card: FeedCard) => void;
  className?: string;
}

type AskLookup =
  | { status: "loading" }
  | { status: "someone-else" }
  | { status: "failed"; error: unknown };

/** The authoring studio: three prompts, one sheet. Writes POST / PROJECT_UPDATE / GUILD_ACTIVITY / HELP_WANTED. */
export function FieldNoteComposer({
  projectId,
  guildId,
  mode,
  taskId,
  author,
  onPosted,
  onPublished,
  className,
}: FieldNoteComposerProps) {
  const baseId = useId();
  const fieldId = `${baseId}-words`;
  const editHeadingId = `${baseId}-editing`;
  const utilities = api.useUtils();

  const fixedAttachment: ComposerAttachment = projectId
    ? { kind: "project", id: projectId }
    : guildId
      ? { kind: "guild", id: guildId }
      : null;
  // An ask names a project's task, so a guild-fixed composer has no "Who can help?".
  const offersHelp = fixedAttachment?.kind !== "guild";
  const prompts = offersHelp
    ? COMPOSER_PROMPTS
    : COMPOSER_PROMPTS.filter((prompt) => prompt.id !== "help");

  const [prompt, setPrompt] = useState<ComposerPrompt>(() =>
    offersHelp && (mode ?? (taskId ? "help" : "note")) === "help"
      ? "help"
      : "moved",
  );
  const [content, setContent] = useState("");
  const [audience, setAudience] = useState<FieldNoteAudience>("PUBLIC");
  const [chosenAttachment, setChosenAttachment] =
    useState<ComposerAttachment>(null);
  const [chosenTaskId, setChosenTaskId] = useState("");
  const [conflict, setConflict] = useState<{
    postId: string;
    taskId: string;
  } | null>(null);
  const [askLookup, setAskLookup] = useState<AskLookup | null>(null);
  const [editing, setEditing] = useState<{
    postId: string;
    draftBefore: string;
  } | null>(null);
  const [announcement, setAnnouncement] = useState("");

  const isHelp = prompt === "help";
  const attachment = attachmentFor(prompt, fixedAttachment ?? chosenAttachment);
  const attachedProjectId =
    attachment?.kind === "project" ? attachment.id : null;
  const askTaskId = taskId ?? (chosenTaskId || null);

  const subjects = useAttachableSubjects(fixedAttachment === null);
  const tasks = useAskableTasks(attachedProjectId, isHelp);

  const audienceContext: AudienceContext = isHelp
    ? "ask"
    : (attachment?.kind ?? null);
  const audienceOptions = audienceOptionsFor(audienceContext);
  const visibility = audienceOptions.includes(audience) ? audience : "PUBLIC";

  const input = createInputFor({
    prompt,
    content,
    visibility,
    attachment,
    taskId: askTaskId,
  });
  // A 409 belongs to the task it was about; picking another task (or prompt) retires it.
  const askConflict =
    isHelp && conflict && conflict.taskId === askTaskId ? conflict : null;

  /** The card for `onPublished`; subject details come from the picker's lists, else one read each. */
  const publishCard = async (post: PublishedPost) => {
    if (!onPublished) return;
    const knownProject = subjects.projects.find(
      (project) => project.id === post.projectId,
    );
    const knownGuild = subjects.guilds.find(
      (guild) => guild.id === post.guildId,
    );
    const [project, guild] = await Promise.all([
      knownProject?.card ??
        (post.projectId
          ? utilities.project.getById
              .fetch({ id: post.projectId })
              .then(cardProjectOf, () => null)
          : null),
      knownGuild
        ? knownGuild.card
        : post.guildId
          ? utilities.guild.getById
              .fetch({ id: post.guildId })
              .then(cardGuildOf, () => null)
          : null,
    ]);
    const askedTask = tasks.all.find((task) => task.id === post.taskId);
    onPublished(
      feedCardFromPost(post, {
        author,
        project,
        guild,
        taskPaid: askedTask ? (askedTask.equityReward ?? 0) > 0 : undefined,
      }),
    );
  };

  const create = api.post.create.useMutation({
    onSuccess: (post) => {
      setContent("");
      setConflict(null);
      setAnnouncement("Posted.");
      onPosted?.();
      void publishCard(post);
    },
    onError: (error, variables) => {
      const existingPostId = helpWantedExistingPostIdOf(error);
      setAskLookup(null);
      setConflict(
        existingPostId && variables.taskId
          ? { postId: existingPostId, taskId: variables.taskId }
          : null,
      );
    },
  });

  const save = api.post.update.useMutation({
    onSuccess: (post) => {
      setEditing(null);
      setContent("");
      setAnnouncement("Saved.");
      onPosted?.();
      void publishCard(post);
    },
  });

  /** "Edit your ask": the composer opens the existing ask in place, so the studio stays the one place to write. */
  const openExistingAsk = async (postId: string) => {
    setAskLookup({ status: "loading" });
    try {
      const existing = await utilities.post.getById.fetch({ id: postId });
      if (author && existing.authorId !== author.id) {
        setAskLookup({ status: "someone-else" });
        return;
      }
      setAskLookup(null);
      setConflict(null);
      setEditing({ postId: existing.id, draftBefore: content });
      setContent(existing.content);
      create.reset();
      // The button that was focused is gone; the words it opened are where the keyboard goes next.
      document.getElementById(fieldId)?.focus();
    } catch (error) {
      setAskLookup({ status: "failed", error });
    }
  };

  const cancelEditing = () => {
    if (!editing) return;
    setContent(editing.draftBefore);
    setEditing(null);
    save.reset();
    document.getElementById(fieldId)?.focus();
  };

  const createFailure =
    create.error && !helpWantedExistingPostIdOf(create.error)
      ? create.error
      : null;
  const pending = create.isPending || save.isPending;
  const canSubmit = editing
    ? content.trim().length > 0 && !pending
    : input !== null && !pending;

  const attachValue = attachment ? `${attachment.kind}:${attachment.id}` : "";
  const fixedTaskTitle = taskId
    ? tasks.all.find((task) => task.id === taskId)?.title
    : undefined;

  return (
    <form
      data-prompt={prompt}
      className={cn(
        "bg-card border-border focus-within:border-foreground relative border transition-colors",
        className,
      )}
      onSubmit={(event) => {
        event.preventDefault();
        if (!canSubmit) return;
        if (editing) {
          save.mutate({ id: editing.postId, content: content.trim() });
        } else if (input) {
          create.mutate(input);
        }
      }}
    >
      {/* The sheet previews the card it becomes: an ask gets the help card's red rule. */}
      <span
        aria-hidden
        className={cn(
          "bg-primary pointer-events-none absolute inset-y-[-1px] -left-px z-10 w-[3px] origin-top transition-transform duration-300 ease-out motion-reduce:transition-none",
          isHelp ? "scale-y-100" : "scale-y-0",
        )}
      />

      {editing ? (
        <div className="border-border flex min-h-11 items-center justify-between gap-3 border-b pr-2 pl-4">
          <span
            id={editHeadingId}
            className="font-mono text-[11px] font-semibold tracking-[0.08em] uppercase"
          >
            Your ask
          </span>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={cancelEditing}
          >
            Cancel
          </Button>
        </div>
      ) : (
        <PromptTabs
          baseId={baseId}
          value={prompt}
          onChange={setPrompt}
          prompts={prompts}
        />
      )}

      <HashtagTextarea
        id={fieldId}
        value={content}
        labelledBy={editing ? editHeadingId : promptRadioId(baseId, prompt)}
        onChange={(next) => {
          setContent(next);
          setAnnouncement("");
        }}
      />

      <div className="border-border flex flex-wrap items-center gap-x-4 gap-y-2 border-t border-dashed px-4 py-3">
        {editing ? null : (
          <>
            {fixedAttachment === null ? (
              <ComposerSelect
                label={isHelp ? "Project" : "Attach"}
                value={attachValue}
                onChange={(value) => {
                  setChosenAttachment(parseAttachment(value));
                  setChosenTaskId("");
                }}
              >
                <option value="">{isHelp ? "Pick a project" : "None"}</option>
                {subjects.projects.length > 0 ? (
                  <optgroup label="Projects">
                    {subjects.projects.map((project) => (
                      <option key={project.id} value={`project:${project.id}`}>
                        {project.name}
                      </option>
                    ))}
                  </optgroup>
                ) : null}
                {!isHelp && subjects.guilds.length > 0 ? (
                  <optgroup label="Guilds">
                    {subjects.guilds.map((guild) => (
                      <option key={guild.id} value={`guild:${guild.id}`}>
                        {guild.name}
                      </option>
                    ))}
                  </optgroup>
                ) : null}
              </ComposerSelect>
            ) : null}

            {isHelp ? (
              <TaskField
                fixedTaskId={taskId}
                fixedTaskTitle={fixedTaskTitle}
                projectChosen={attachedProjectId !== null}
                loading={tasks.isLoading}
                tasks={tasks.askable}
                value={chosenTaskId}
                onChange={setChosenTaskId}
              />
            ) : null}

            <ComposerSelect
              label="Who can read"
              value={visibility}
              onChange={(value) => setAudience(value as FieldNoteAudience)}
            >
              {audienceOptions.map((option) => (
                <option key={option} value={option}>
                  {audienceLabel(option)}
                </option>
              ))}
            </ComposerSelect>
          </>
        )}

        <Button
          type="submit"
          size="sm"
          disabled={!canSubmit}
          className="ml-auto"
        >
          {pending ? (
            <Loader2 aria-hidden className="animate-spin" />
          ) : (
            <Send aria-hidden />
          )}
          {editing ? "Save ask" : isHelp ? "Ask for help" : "Post update"}
        </Button>
      </div>

      {editing ? null : (
        <p className="text-muted-foreground px-4 pb-3 font-mono text-[11px]">
          {audienceDescription(visibility)}
        </p>
      )}

      {askConflict && !editing ? (
        <div
          role="status"
          className="border-primary bg-primary/10 mx-4 mb-3 flex flex-wrap items-center justify-between gap-x-3 gap-y-2 border-l-[3px] py-1.5 pr-1.5 pl-3"
        >
          {askLookup?.status === "someone-else" ? (
            <>
              <p className="text-sm">Asked by someone else.</p>
              <Button asChild size="sm" variant="outline">
                <Link href={feedRoutes.task(askConflict.taskId)}>
                  Open task
                </Link>
              </Button>
            </>
          ) : (
            <>
              <p className="text-sm">Already asked.</p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                data-existing-post-id={askConflict.postId}
                disabled={askLookup?.status === "loading"}
                onClick={() => void openExistingAsk(askConflict.postId)}
              >
                {askLookup?.status === "loading" ? (
                  <Loader2 aria-hidden className="animate-spin" />
                ) : null}
                Edit your ask
              </Button>
            </>
          )}
        </div>
      ) : null}

      {askLookup?.status === "failed" && askConflict ? (
        <MutationErrorNotice
          error={askLookup.error}
          action="open your ask"
          className="mx-4 mb-3"
        />
      ) : null}
      <MutationErrorNotice
        error={createFailure}
        action={isHelp ? "ask for help" : "post this"}
        className="mx-4 mb-3"
      />
      <MutationErrorNotice
        error={save.error}
        action="save your ask"
        className="mx-4 mb-3"
      />

      <p className="sr-only" role="status">
        {announcement}
      </p>
    </form>
  );
}

/** The task an ask names: fixed by the mounting page, or picked from the project's open, unassigned tasks. */
function TaskField({
  fixedTaskId,
  fixedTaskTitle,
  projectChosen,
  loading,
  tasks,
  value,
  onChange,
}: {
  fixedTaskId?: string;
  fixedTaskTitle?: string;
  projectChosen: boolean;
  loading: boolean;
  tasks: readonly { id: string; title: string }[];
  value: string;
  onChange: (taskId: string) => void;
}) {
  if (fixedTaskId) {
    return fixedTaskTitle ? (
      <p className="flex min-w-0 items-center gap-2 text-sm">
        <span className="text-muted-foreground shrink-0 font-mono text-[11px] font-semibold tracking-[0.08em] uppercase">
          Task
        </span>
        <span className="truncate font-medium">{fixedTaskTitle}</span>
      </p>
    ) : null;
  }
  if (!projectChosen) return null;
  if (loading) {
    return (
      <Loader2
        aria-label="Loading tasks"
        className="text-muted-foreground size-4 animate-spin"
      />
    );
  }
  if (tasks.length === 0) {
    return <p className="text-muted-foreground text-sm">No open tasks.</p>;
  }
  return (
    <ComposerSelect label="Task" value={value} onChange={onChange}>
      <option value="" disabled>
        Pick a task
      </option>
      {tasks.map((task) => (
        <option key={task.id} value={task.id}>
          {task.title}
        </option>
      ))}
    </ComposerSelect>
  );
}

function parseAttachment(value: string): ComposerAttachment {
  const separator = value.indexOf(":");
  if (separator < 0) return null;
  const kind = value.slice(0, separator);
  const id = value.slice(separator + 1);
  if (!id) return null;
  return kind === "project" || kind === "guild" ? { kind, id } : null;
}
