import type { PostType } from "~/lib/api/ardanova/endpoints/posts";

import type { FieldNoteAudience } from "../field-note-audience";

/** The composer's three prompts (plan §1); the chip text is the composer's copy. See ./AGENTS.md §Prompt rule. */
export const COMPOSER_PROMPTS = [
  { id: "moved", label: "What moved?" },
  { id: "stuck", label: "What's stuck?" },
  { id: "help", label: "Who can help?" },
] as const;

export type ComposerPrompt = (typeof COMPOSER_PROMPTS)[number]["id"];

/** What a note is attached to: one project or one guild the author belongs to, or nothing. */
export type ComposerAttachment =
  | { kind: "project"; id: string }
  | { kind: "guild"; id: string }
  | null;

/** The post type a prompt writes. "Who can help?" is always HELP_WANTED, never POST (plan Revision 1). */
export function postTypeFor(
  prompt: ComposerPrompt,
  attachment: ComposerAttachment,
): PostType {
  if (prompt === "help") return "HELP_WANTED";
  if (attachment?.kind === "project") return "PROJECT_UPDATE";
  if (attachment?.kind === "guild") return "GUILD_ACTIVITY";
  return "POST";
}

/** An ask lives on a project's task, so help mode drops a guild attachment rather than sending it. */
export function attachmentFor(
  prompt: ComposerPrompt,
  attachment: ComposerAttachment,
): ComposerAttachment {
  return prompt === "help" && attachment?.kind === "guild" ? null : attachment;
}

export interface ComposerDraft {
  prompt: ComposerPrompt;
  content: string;
  visibility: FieldNoteAudience;
  attachment: ComposerAttachment;
  /** The task an ask names; ignored by the note prompts. */
  taskId: string | null;
}

/** Exactly the `post.create` input the composer sends. */
export interface ComposerCreateInput {
  content: string;
  type: PostType;
  visibility: FieldNoteAudience;
  projectId?: string;
  guildId?: string;
  taskId?: string;
}

/** The create input for a draft, or null while it cannot publish (no words; an ask with no task). */
export function createInputFor(
  draft: ComposerDraft,
): ComposerCreateInput | null {
  const content = draft.content.trim();
  if (!content) return null;

  const attachment = attachmentFor(draft.prompt, draft.attachment);
  const type = postTypeFor(draft.prompt, attachment);
  if (type === "HELP_WANTED" && !draft.taskId) return null;

  return {
    content,
    type,
    visibility: draft.visibility,
    ...(attachment?.kind === "project" ? { projectId: attachment.id } : {}),
    ...(attachment?.kind === "guild" ? { guildId: attachment.id } : {}),
    // The API refuses a taskId on any other type (plan §2), so only an ask carries one.
    ...(type === "HELP_WANTED" && draft.taskId ? { taskId: draft.taskId } : {}),
  };
}
