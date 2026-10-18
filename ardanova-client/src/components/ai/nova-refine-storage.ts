import { z } from "zod";

import { NOVA_STATE_MAX_CHARS } from "~/lib/nova-state-limits";
import type { NovaRefineConversation } from "./nova-refine-panel";

/** One key per tab: the refinement follows the member across routes. See AGENTS.md §Refinement persistence. */
export const REFINE_STORAGE_KEY = "ardanova:nova:refine:v1";

const MAX_STORED_CHARS = 200_000;

const text = (max: number) => z.string().max(max);
const list = z.array(text(1_000)).max(10);

const replySchema = z.object({
  type: z.enum(["question", "verdict"]),
  assistant: text(100),
  mode: z.enum(["live", "local-preview"]),
  question: text(400).nullish(),
  number: z.number().int().nullish(),
  maxQuestions: z.number().int().nullish(),
  state: text(NOVA_STATE_MAX_CHARS).nullish(),
  sessionId: text(64).nullish(),
  verdict: z
    .object({
      outcome: text(40),
      headline: text(1_000),
      summary: text(3_000),
      strengths: list,
      risks: list,
      nextStep: text(1_000),
      sources: z
        .array(z.object({ title: text(1_000), url: text(2_000) }))
        .max(10),
    })
    .nullish(),
  vertical: text(40).nullish(),
  brief: z
    .object({
      workingName: text(200),
      oneLiner: text(500),
      customer: text(500),
      problem: text(1_000),
      offer: text(1_000),
      pricing: text(500),
      channels: list,
      milestones: list,
      metrics: list,
    })
    .nullish(),
});

const conversationSchema = z.object({
  version: z.literal(1),
  idea: text(2_000),
  turns: z.array(z.object({ question: text(400), answer: text(1_500) })).max(8),
  reply: replySchema.nullable(),
  answer: text(1_500),
});

/** True once a refinement has started and has not reached its verdict: the state a reload would lose. */
export function isRefinementInProgress(
  conversation: NovaRefineConversation,
): boolean {
  return conversation.reply !== null && conversation.reply.type !== "verdict";
}

/** The conversation saved in this tab, or null when there is none, it is unreadable, or storage is blocked. */
export function readRefineConversation(
  storage: Storage,
): NovaRefineConversation | null {
  try {
    const raw = storage.getItem(REFINE_STORAGE_KEY);
    if (!raw || raw.length > MAX_STORED_CHARS) return null;
    const parsed = conversationSchema.safeParse(JSON.parse(raw));
    if (!parsed.success) return null;
    const { idea, turns, reply, answer } = parsed.data;
    return { idea, turns, reply, answer };
  } catch {
    return null;
  }
}

/** Saves the conversation in this tab (an empty one clears it); false when storage is blocked or full. */
export function writeRefineConversation(
  storage: Storage,
  conversation: NovaRefineConversation,
): boolean {
  try {
    if (!conversation.idea && conversation.reply === null) {
      storage.removeItem(REFINE_STORAGE_KEY);
      return true;
    }
    const payload = { version: 1 as const, ...conversation };
    if (!conversationSchema.safeParse(payload).success) return false;
    storage.setItem(REFINE_STORAGE_KEY, JSON.stringify(payload));
    return true;
  } catch {
    return false;
  }
}
