import type { NovaBrief } from "~/lib/api/ardanova/endpoints/nova";

/** The fields of tRPC `project.create` a Nova AI brief can honestly fill. See `AGENTS.md` §Brief to draft. */
export interface NovaProjectDraft {
  title: string;
  description: string;
  summary: string;
  problemStatement: string;
  solution: string;
  categories: string[];
  targetAudience: string | null;
}

/** project.create's minimum for description, problem statement and solution. */
const MIN_PROSE = 10;
const MAX_SUMMARY = 200;
const MAX_TITLE = 120;

/** A brief value with real content; a placeholder such as "to decide" falls back to the member's own idea. */
function prose(value: string, fallback: string): string {
  const text = value.trim();
  return text.length >= MIN_PROSE && !/^to decide\b/i.test(text)
    ? text
    : fallback;
}

/** Maps a go/adjust brief plus the member's idea and chosen category onto a private DRAFT project. */
export function briefAsProjectDraft(
  brief: NovaBrief,
  idea: string,
  category: string,
): NovaProjectDraft {
  const ownIdea = idea.trim();
  const oneLiner = prose(brief.oneLiner, ownIdea);
  const customer = brief.customer.trim();
  return {
    title:
      brief.workingName.trim().slice(0, MAX_TITLE) ||
      ownIdea.slice(0, MAX_TITLE),
    description: oneLiner,
    summary: oneLiner.slice(0, MAX_SUMMARY),
    problemStatement: prose(brief.problem, ownIdea),
    solution: prose(brief.offer, ownIdea),
    categories: [category],
    targetAudience:
      customer && !/^to decide\b/i.test(customer) ? customer : null,
  };
}
