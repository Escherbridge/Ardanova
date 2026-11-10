import { publicCanonical } from "~/lib/site";

export type WritingLink = {
  href: string;
  label: string;
};

export type Citation = {
  label: string;
  href: string;
  /** ISO date when the cited page names one. */
  datePublished?: string;
};

type WritingEntry = {
  slug: string;
  title: string;
  /** On-page H1 when it differs from the short title. */
  headline?: string;
  /** Kicker above the H1. */
  eyebrow?: string;
  /** Index-card continuation, after the short title. */
  indexBlurb?: string;
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

const companiesActYear = "2006";
const icaStatementUrl = "https://ica.coop/en/cooperatives/cooperative-identity";
const hmrcRecordsUrl =
  "https://www.gov.uk/government/publications/records-and-information-created-by-hmrc/hmrc-records-and-information-collection-policy";
const termsUrl = "https://ardanova.com/terms";

export const patterns = [
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    headline: "A register is evidence. It is not, by itself, the right.",
    eyebrow: "A record is not a right",
    indexBlurb:
      "Companies Act 2006, s.127: a members' register is prima facie evidence of what was entered, not an unchallengeable right. A register can be evidence without, by itself, being the right.",
    publishedAt: "2026-11-10",
    description:
      "A members' register is evidence of what was entered, not ownership or a payment right. A record here is not a promise to pay; the economy is simulated.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — work is one record, voice is another, and the agreement is what binds them.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — if it cannot be found later, it was not decided. It was only said.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.113 (register of members)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/113",
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.127 (register is prima facie evidence)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/127",
        datePublished: companiesActYear,
      },
      {
        label:
          "Companies Act 2006, s.248 (directors' minutes; keep at least ten years)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/248",
        datePublished: companiesActYear,
      },
      {
        label:
          "Companies Act 2006, s.249 (minutes as evidence of the proceedings; rebuttable)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/249",
        datePublished: companiesActYear,
      },
      {
        label:
          "HM Revenue & Customs, HMRC Records and Information Collection Policy, 19 May 2026, quoting ISO 15489-1:2016",
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
      {
        label:
          "ArdaNova, A record is not a right, 9 November 2026 (sibling essay, not a source of law)",
        href: "https://ardanova.com/notes/a-record-is-not-a-right",
        datePublished: "2026-11-09",
      },
      {
        label:
          "ArdaNova homepage (ECONOMY: SIMULATED; platform records do not automatically create legal ownership, equity, or payment rights)",
        href: "https://ardanova.com/",
      },
      {
        label: "ArdaNova Terms of use, effective 10 November 2026",
        href: termsUrl,
        datePublished: "2026-11-10",
      },
    ],
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records, one agreement",
    headline:
      "Work is one record. Voice is another. The agreement is what binds them.",
    eyebrow: "Two records, one agreement",
    indexBlurb:
      "ICA Principle 2: in primary cooperatives, one member, one vote; Principle 3 describes economic participation on different terms. ArdaNova is not a cooperative, so work is one record and voice is another.",
    publishedAt: "2026-11-10",
    description:
      "Public cooperative identity separates one-member-one-vote from economic participation. A balance is not a vote or a share. The workspace economy is simulated.",
    related: [
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — a register is evidence, not, by itself, the right.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — if it cannot be found later, it was not decided. It was only said.",
      },
    ],
    citations: [
      {
        label:
          "International Cooperative Alliance, Statement on the Cooperative Identity (definition, adopted 1995)",
        href: icaStatementUrl,
        datePublished: "1995",
      },
      {
        label: "ICA Principle 2 (one member, one vote in primary cooperatives)",
        href: icaStatementUrl,
        datePublished: "1995",
      },
      {
        label:
          "ICA Principle 3 (economic participation distinct from, and democratically controlled with, capital)",
        href: icaStatementUrl,
        datePublished: "1995",
      },
      {
        label:
          "ICA Principle 4 (external capital on terms that preserve democratic control)",
        href: icaStatementUrl,
        datePublished: "1995",
      },
      {
        label:
          "ArdaNova, Two records. One agreement., 9 November 2026 (sibling essay)",
        href: "https://ardanova.com/notes/two-records-one-agreement",
        datePublished: "2026-11-09",
      },
      {
        label:
          "ArdaNova homepage (Record A / Record B jacket lines; ECONOMY: SIMULATED)",
        href: "https://ardanova.com/",
      },
      {
        label: "ArdaNova Terms of use, effective 10 November 2026",
        href: termsUrl,
        datePublished: "2026-11-10",
      },
    ],
  },
  {
    slug: "decision-logs",
    title: "Decision logs",
    headline:
      "If it cannot be found later, it was not decided. It was only said.",
    eyebrow: "Decision logs",
    indexBlurb:
      "Official guidance already names the fields: what was decided, why, by whom, and when, including inaction and retractions. If a later person cannot find those fields, the group only said something; they did not leave a decision.",
    publishedAt: "2026-11-10",
    description:
      "A usable decision log is contemporaneous: what was decided, why, by whom, and when, including inaction. A recorded decision here still does not move funds.",
    related: [
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — a register is evidence, not, by itself, the right.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — work is one record, voice is another, and the agreement is what binds them.",
      },
    ],
    citations: [
      {
        label:
          'HMRC Records and Information Collection Policy, 19 May 2026 (ISO 15489-1:2016; "what happened, what was decided")',
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
      {
        label:
          "Guidance for the management of Private Office information and records, 29 December 2023",
        href: "https://www.gov.uk/government/publications/guidance-management-of-private-office-information-and-records/guidance-for-the-management-of-private-office-information-and-records-html",
        datePublished: "2023-12-29",
      },
      {
        label:
          "The National Archives, Code of Practice under s.46 FOIA, published 15 July 2021",
        href: "https://www.gov.uk/government/publications/code-of-practice-on-the-management-of-records-issued-under-section-46-the-freedom-of-information-act-2000",
        datePublished: "2021-07-15",
      },
      {
        label: "UK Health and Safety Executive, key decision log",
        href: "https://www.hse.gov.uk/foi/internalops/og/ogprocedures/investigation/decisionlog.htm",
      },
      {
        label:
          "Companies Act 2006, s.355 (records of resolutions and meetings; at least ten years)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/355",
        datePublished: companiesActYear,
      },
      {
        label:
          "Companies Act 2006, s.248 (directors' minutes; at least ten years)",
        href: "https://www.legislation.gov.uk/ukpga/2006/46/section/248",
        datePublished: companiesActYear,
      },
      {
        label:
          "ArdaNova homepage (decisions stay recorded; a recorded vote does not, by itself, move funds or rewrite terms; ECONOMY: SIMULATED)",
        href: "https://ardanova.com/",
      },
      {
        label: "ArdaNova Terms of use, effective 10 November 2026",
        href: termsUrl,
        datePublished: "2026-11-10",
      },
    ],
  },
  {
    slug: "problem-intake",
    title: "Problem intake",
    headline: "Problem intake",
    eyebrow: "2026-11-10 · Escherbridge",
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

/** Reviewed research children. Problem intake stays routable and off this index. */
export const researchedPatterns = patterns.filter(
  (pattern) => pattern.slug !== "problem-intake",
);

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
