import { publicCanonical } from "~/lib/site";

export type WritingLink = {
  href: string;
  label: string;
};

export type Citation = {
  label: string;
  href: string;
};

type WritingEntry = {
  slug: string;
  title: string;
  publishedAt: string;
  description: string;
  related: readonly WritingLink[];
  citations: readonly Citation[];
};

export const notes = [
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    publishedAt: "2026-11-09",
    description:
      "ArdaNova will keep a record of work, of agreed value, and of decisions. That record is useful. It is not, by itself, ownership, equity, or a promise that anyone will pay you. The economy in the workspace is simulated, and the terms already say so.",
    related: [
      {
        href: "/notes/two-records-one-agreement",
        label: "Two records. One agreement.",
      },
      {
        href: "/patterns/a-record-is-not-a-right",
        label: "Pattern: a record is not a right",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      { label: "PrimusNeo", href: "https://primusneo.com" },
      { label: "AZOA", href: "https://azoa.ardanova.com" },
    ],
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records. One agreement.",
    publishedAt: "2026-11-09",
    description:
      "Work and voice are easy to mash into a single score. ArdaNova keeps them as two records on purpose: what was done and how value was agreed, on one side; who was allowed to decide, and what they decided, on the other. The agreement is what binds them, not a balance.",
    related: [
      {
        href: "/notes/a-record-is-not-a-right",
        label: "A record is not a right",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label: "Pattern: two records, one agreement",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      { label: "PrimusNeo", href: "https://primusneo.com" },
    ],
  },
] as const satisfies readonly WritingEntry[];

export type NoteSlug = (typeof notes)[number]["slug"];

export const patterns = [
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    publishedAt: "2026-11-10",
    description:
      "A record of work or of a decision is not, by itself, ownership or a promise of payment. The economy in this workspace is simulated.",
    related: [
      {
        href: "/notes/a-record-is-not-a-right",
        label: "Note: a record is not a right",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label: "Two records. One agreement.",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      {
        label: "Note: a record is not a right",
        href: "https://ardanova.com/notes/a-record-is-not-a-right",
      },
    ],
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records. One agreement.",
    publishedAt: "2026-11-10",
    description:
      "Work and voice stay in two records. The agreement is what binds them. The economy in this workspace is simulated.",
    related: [
      {
        href: "/notes/two-records-one-agreement",
        label: "Note: two records, one agreement",
      },
      {
        href: "/patterns/a-record-is-not-a-right",
        label: "A record is not a right",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      {
        label: "Note: two records, one agreement",
        href: "https://ardanova.com/notes/two-records-one-agreement",
      },
    ],
  },
  {
    slug: "decision-logs",
    title: "Decision logs",
    publishedAt: "2026-11-10",
    description:
      "Proposals and recorded outcomes stay distinct from the work record. A recorded vote does not, by itself, move funds. The economy is simulated.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label: "Two records. One agreement.",
      },
      {
        href: "/patterns/problem-intake",
        label: "Problem intake",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
    ],
  },
  {
    slug: "problem-intake",
    title: "Problem intake",
    publishedAt: "2026-11-10",
    description:
      "A problem is described before a plan is made. Describing it is not a payment. The economy in this workspace is simulated.",
    related: [
      {
        href: "/patterns/decision-logs",
        label: "Decision logs",
      },
      {
        href: "/patterns/a-record-is-not-a-right",
        label: "A record is not a right",
      },
    ],
    citations: [{ label: "ArdaNova homepage", href: "https://ardanova.com/" }],
  },
] as const satisfies readonly WritingEntry[];

export type PatternSlug = (typeof patterns)[number]["slug"];

export function noteBySlug(slug: string) {
  return notes.find((note) => note.slug === slug);
}

export function isPatternSlug(slug: string): slug is PatternSlug {
  return patterns.some((pattern) => pattern.slug === slug);
}

export function patternBySlug(slug: string) {
  return patterns.find((pattern) => pattern.slug === slug);
}

export function noteCanonical(slug: string): string {
  return publicCanonical(`/notes/${slug}`);
}

export function patternCanonical(slug: string): string {
  return publicCanonical(`/patterns/${slug}`);
}

/** Blog notes, then research children. News has no dated item yet. */
export function feedEntries() {
  const noteEntries = notes.map((note) => ({
    title: note.title,
    url: noteCanonical(note.slug),
    publishedAt: note.publishedAt,
    description: note.description,
  }));
  const patternEntries = patterns.map((pattern) => ({
    title: pattern.title,
    url: patternCanonical(pattern.slug),
    publishedAt: pattern.publishedAt,
    description: pattern.description,
  }));
  return [...noteEntries, ...patternEntries].sort((a, b) =>
    a.publishedAt < b.publishedAt ? 1 : a.publishedAt > b.publishedAt ? -1 : 0,
  );
}
