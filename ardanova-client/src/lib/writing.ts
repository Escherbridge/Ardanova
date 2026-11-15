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

export type PatternShelfId =
  | "multi-ledger-agreements"
  | "record-clarity"
  | "membership-resolutions";

type WritingEntry = {
  slug: string;
  title: string;
  /** On-page H1 when it differs from the short title. */
  headline?: string;
  /** Kicker above the H1. */
  eyebrow?: string;
  /** Index-card continuation, after the short title. */
  indexBlurb?: string;
  shelf?: PatternShelfId;
  publishedAt: string;
  description: string;
  related: readonly WritingLink[];
  citations: readonly Citation[];
};

export const PATTERN_SHELVES: readonly {
  id: PatternShelfId;
  title: string;
  summary: string;
}[] = [
  {
    id: "multi-ledger-agreements",
    title: "Multi-Ledger Agreements",
    summary:
      "Why ledgers break when contribution numbers pretend to grant title or decision weight, and how split logs stay reconciled without a single opaque custodian.",
  },
  {
    id: "record-clarity",
    title: "Record Clarity & Minutes",
    summary:
      "Decision logs, append-only corrections, and capture habits that ensure someone joining six months later can trace what happened and why.",
  },
  {
    id: "membership-resolutions",
    title: "Membership & Resolutions",
    summary:
      "Structural distinctions between governing rules, membership registers, board minutes, and sole-steward decisions—contrasted cleanly with statutory baselines.",
  },
];

export const notes = [
  {
    slug: "why-shared-work-breaks-between-orgs",
    title: "Why shared work breaks between organizations",
    publishedAt: "2026-11-14",
    description:
      "Multi-org builds rarely fail over bad code. They stall when nobody can tell which spreadsheet reflects the real agreement, who approved a scope change, or which chat thread settled a dispute.",
    related: [
      {
        href: "/notes/separating-contribution-ledgers-from-governance-votes",
        label: "Separating contribution logs from governance votes",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label: "Pattern: reconciling two ledgers without a central custodian",
      },
      {
        href: "/patterns/decision-logs",
        label: "Pattern: decision logs",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
    ],
  },
  {
    slug: "separating-contribution-ledgers-from-governance-votes",
    title: "Separating contribution logs from governance votes",
    publishedAt: "2026-11-14",
    description:
      "Combining work hours, funds, and governance authority into a single scalar score invites quiet drift. Keeping Record A and Record B independent makes every downstream audit legible.",
    related: [
      {
        href: "/notes/why-shared-work-breaks-between-orgs",
        label: "Why shared work breaks between organizations",
      },
      {
        href: "/notes/two-records-one-agreement",
        label: "Two records. One agreement.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label: "Pattern: two records, one agreement",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      {
        label: "International Cooperative Alliance Statement (1995)",
        href: "https://ica.coop/en/cooperatives/cooperative-identity",
        datePublished: "1995",
      },
    ],
  },
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    publishedAt: "2026-11-09",
    description:
      "Keeping a clean ledger of work, agreed value, and votes is essential for coordination—and useless unless you separate bookkeeping rows from legal title and payment promises.",
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
      { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
    ],
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records. One agreement.",
    publishedAt: "2026-11-09",
    description:
      "Work logs and governance authority collapse when mashed into a single leaderboard score. Splitting Record A and Record B lets teams bind them explicitly inside readable project terms.",
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
const section248Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/248";
const section355Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/355";
const privateOfficeRecordsUrl =
  "https://www.gov.uk/government/publications/guidance-management-of-private-office-information-and-records/guidance-for-the-management-of-private-office-information-and-records-html";
const hseKeyDecisionLogUrl =
  "https://www.hse.gov.uk/foi/internalops/og/ogprocedures/investigation/decisionlog.htm";
const section8Url = "https://www.legislation.gov.uk/ukpga/2006/46/section/8";
const section17Url = "https://www.legislation.gov.uk/ukpga/2006/46/section/17";
const section18Url = "https://www.legislation.gov.uk/ukpga/2006/46/section/18";
const section20Url = "https://www.legislation.gov.uk/ukpga/2006/46/section/20";
const section113Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/113";
const section288Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/288";
const section357Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/357";
const section1134Url =
  "https://www.legislation.gov.uk/ukpga/2006/46/section/1134";
const tnaCodeLandingUrl =
  "https://www.gov.uk/government/publications/code-of-practice-on-the-management-of-records-issued-under-section-46-the-freedom-of-information-act-2000";
const tnaCodePdfUrl =
  "https://assets.publishing.service.gov.uk/media/6114c3278fa8f506c4d84100/Freedom_Information_Code_Practice_Web_Accessible.pdf";

export const patterns = [
  {
    slug: "a-record-is-not-a-right",
    title: "A record is not a right",
    headline: "A register is evidence. It is not, by itself, the right.",
    eyebrow: "Multi-Ledger Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "Even a statutory register is prima facie evidence of what happened—never unchallengeable title. Keep workspace entries honest by pointing rights back to readable agreements.",
    publishedAt: "2026-11-10",
    description:
      "Why ledger entries and registers provide audit evidence rather than creating automatic legal ownership, equity, or payment rights.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — splitting work logs from decision voice.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Reconciling two ledgers without a central custodian — spotting silent drift early.",
      },
      {
        href: "/patterns/company-registers",
        label:
          "What is a company register of members — distinguishing membership lists from title.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.113 (register of members)",
        href: section113Url,
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
        href: section248Url,
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
        label: "ArdaNova, A record is not a right, 9 November 2026",
        href: "https://ardanova.com/notes/a-record-is-not-a-right",
        datePublished: "2026-11-09",
      },
      {
        label: "ArdaNova homepage",
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
    eyebrow: "Multi-Ledger Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "When hours logged and voting influence collapse into a single point score, contributors lose track of both fairness and authority. Keep work logs and governance voice independent.",
    publishedAt: "2026-11-10",
    description:
      "Separating contribution accounting (Record A) from governance authority (Record B) so work performed never quietly overrides agreed decision rules.",
    related: [
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — evidence inside a database vs. rights under an agreement.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Reconciling two ledgers without a central custodian — cross-org checkpoints.",
      },
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "What are articles of association — rules separated from daily minutes.",
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
        label: "ArdaNova, Two records. One agreement., 9 November 2026",
        href: "https://ardanova.com/notes/two-records-one-agreement",
        datePublished: "2026-11-09",
      },
      {
        label: "ArdaNova homepage",
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
    slug: "reconciling-two-ledgers-without-a-central-custodian",
    title: "Reconciling two ledgers without a central custodian",
    headline:
      "When two partner organizations keep their own books, quiet drift starts on day two.",
    eyebrow: "Multi-Ledger Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "Exported CSVs go stale the hour they leave someone's laptop. Pair immutable entry IDs, agreement references, and scheduled reconciliation checkpoints so neither side edits history silently.",
    publishedAt: "2026-11-14",
    description:
      "Practical architecture for keeping two independent partner records aligned via agreement anchors, sequence numbers, and exception trails without handing single custody to one vendor.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — why work logs and decision logs stay distinct.",
      },
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Why append-only amendments beat quiet edits — leaving visible correction trails.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — recording who approved reconciliation exceptions and when.",
      },
    ],
    citations: [
      {
        label:
          "HM Revenue & Customs, HMRC Records and Information Collection Policy, 19 May 2026, quoting ISO 15489-1:2016",
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
      {
        label: "ArdaNova, Why shared work breaks between organizations",
        href: "https://ardanova.com/notes/why-shared-work-breaks-between-orgs",
        datePublished: "2026-11-14",
      },
      {
        label: "AZOA Workflow Specifications",
        href: "https://azoa.ardanova.com/workflows",
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
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "A sprint call ended three weeks ago and two leads remember different scope cutoffs. Capture date, timestamp, actor, decision, reasoning, and explicit inaction in one durable feed.",
    publishedAt: "2026-11-10",
    description:
      "Contemporaneous decision logging: what was decided, why, by whom, and when—including explicit decisions not to act and documented reversals.",
    related: [
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Why append-only amendments beat quiet edits — recording reversals cleanly.",
      },
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What a record is for — separating facts, decisions, and runbooks.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — passing the six-month newcomer test.",
      },
      {
        href: "/patterns/tna-section-46",
        label:
          "Public-authority record frameworks — FOIA section 46 as external contrast.",
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
        href: privateOfficeRecordsUrl,
        datePublished: "2023-12-29",
      },
      {
        label:
          "The National Archives, Code of Practice under s.46 FOIA, published 15 July 2021",
        href: tnaCodeLandingUrl,
        datePublished: "2021-07-15",
      },
      {
        label: "UK Health and Safety Executive, key decision log",
        href: hseKeyDecisionLogUrl,
      },
      {
        label:
          "Companies Act 2006, s.355 (records of resolutions and meetings; at least ten years)",
        href: section355Url,
        datePublished: companiesActYear,
      },
      {
        label:
          "Companies Act 2006, s.248 (directors' minutes; at least ten years)",
        href: section248Url,
        datePublished: companiesActYear,
      },
      {
        label: "ArdaNova homepage",
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
    slug: "append-only-amendments-and-retractions",
    title: "Why append-only amendments beat quiet edits",
    headline:
      "Overwriting a settled note erases why a team changed course halfway through.",
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Editing an old milestone note in place saves thirty seconds now and destroys trust during an audit. Append dated superseding entries linked to the original ID instead.",
    publishedAt: "2026-11-14",
    description:
      "Why immutable revision chains and dated retraction entries preserve system history better than silent in-place document updates.",
    related: [
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — capturing changes and inaction contemporaneously.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Reconciling two ledgers without a central custodian — catching divergence.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — clarity for future stewards.",
      },
    ],
    citations: [
      {
        label: "UK Health and Safety Executive, key decision log",
        href: hseKeyDecisionLogUrl,
      },
      {
        label:
          "HMRC Records and Information Collection Policy, published 19 May 2026",
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
      {
        label: "AZOA Workflow Specifications",
        href: "https://azoa.ardanova.com/workflows",
      },
    ],
  },
  {
    slug: "problem-intake",
    title: "Problem intake",
    headline: "Problem intake",
    eyebrow: "Shared Workspace Pattern",
    publishedAt: "2026-11-10",
    description:
      "Start with a crisp statement of the problem and who it affects before inventing metrics, milestones, or funding scopes.",
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
  {
    slug: "directors-minutes",
    title: "Directors' meeting minutes",
    headline: "Board and director meeting minutes belong in one durable file",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "When directors sign off in conversation and forget the minute book, there is no institutional trace years later. See how long statutory standards expect boards to retain minutes.",
    publishedAt: "2026-11-10",
    description:
      "Why boards record proceedings and preserve meeting minutes across a ten-year baseline, and how shared workspaces mirror that discipline without impersonating statutory filings.",
    related: [
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and general meetings — member votes kept separately from board proceedings.",
      },
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — date, time, attendees, and exact outcome.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — day-to-day operational traceability between board meetings.",
      },
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "What are articles of association — rules prescribing authority vs. individual meeting notes.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.248",
        href: section248Url,
        datePublished: companiesActYear,
      },
    ],
  },
  {
    slug: "records-of-resolutions",
    title: "Records of resolutions and general meetings",
    headline: "Written resolutions and member votes leave a three-part trail",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "Async member sign-offs scattered in email threads vanish quickly. Group written resolutions, general meeting minutes, and sole-member decisions into one addressable archive.",
    publishedAt: "2026-11-10",
    description:
      "Keeping written resolutions, general meeting minutes, and sole-member decisions distinct from board chatter and ephemeral notifications.",
    related: [
      {
        href: "/patterns/directors-minutes",
        label:
          "Directors' meeting minutes — board-level proceedings tracked alongside member votes.",
      },
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — minimum fields for reproducible notes.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records. One agreement. — separating voice logs from economic units.",
      },
      {
        href: "/patterns/sole-member-decisions",
        label:
          "Records of decisions by a sole member — why single stewards still log decisions.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.355",
        href: section355Url,
        datePublished: companiesActYear,
      },
    ],
  },
  {
    slug: "what-belongs-in-the-minutes",
    title: "What belongs in the minutes: date, time, who, and what was decided",
    headline:
      "Four fields every meeting note needs before anyone forgets the call",
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "A doc that reads 'aligned on next steps' is worthless three sprints later. Capture date, timestamp, attendees, format, and the literal decision made.",
    publishedAt: "2026-11-10",
    description:
      "A minimal checklist for meeting notes—date, time, participants, medium, and outcome—drawn from real operational records practice.",
    related: [
      {
        href: "/patterns/informal-channels",
        label:
          "Decisions on informal channels — moving chat sign-offs into the permanent log.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — writing for future maintainers.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — tracking scope choices and explicit inaction together.",
      },
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What a record is for — facts, outcomes, and actionable procedures.",
      },
    ],
    citations: [
      {
        label:
          '"Guidance for the management of Private Office information and records (HTML)," published 29 December 2023',
        href: privateOfficeRecordsUrl,
        datePublished: "2023-12-29",
      },
      {
        label:
          "HMRC Records and Information Collection Policy, 19 May 2026 (ISO 15489-1:2016 as quoted there)",
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
    ],
  },
  {
    slug: "readable-record",
    title: "Write a record someone else can read",
    headline:
      "Write every decision record for the engineer joining six months after you ship",
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Cryptic notes like 'same as Tuesday, JD to handle' make sense only to two people in one timezone. Record explicit reasoning, timing, and decisions not to act.",
    publishedAt: "2026-11-10",
    description:
      "Designing decision records that stay self-contained, timestamped, reasoned, and understandable to operators who were never in the room.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — capturing essential date and attendance metadata.",
      },
      {
        href: "/patterns/informal-channels",
        label:
          "Decisions on informal channels — pulling side-channel outcomes back into main logs.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — building a single searchable trail.",
      },
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Why append-only amendments beat quiet edits — recording reversals cleanly.",
      },
    ],
    citations: [
      {
        label: "UK Health and Safety Executive, key decision log",
        href: hseKeyDecisionLogUrl,
      },
    ],
  },
  {
    slug: "informal-channels",
    title: "Decisions on informal channels still need a record",
    headline: "A thumbs-up in chat does not stay recoverable six months later",
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Real product and governance decisions routinely happen in DMs, voice huddles, and Slack threads. Move the outcome into the project log right away without turning chat into surveillance.",
    publishedAt: "2026-11-10",
    description:
      "How engineering and co-op teams mirror outcomes from ephemeral messaging apps into permanent decision logs cleanly.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — recording channel, date, participants, and outcome.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Directors' meeting minutes — why informal tools never excuse missing board notes.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — giving future readers the missing context.",
      },
    ],
    citations: [
      {
        label:
          '"Guidance for the management of Private Office information and records (HTML)," published 29 December 2023',
        href: privateOfficeRecordsUrl,
        datePublished: "2023-12-29",
      },
      {
        label: "Companies Act 2006, s.248, as a sibling",
        href: section248Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.355, as a sibling",
        href: section355Url,
        datePublished: companiesActYear,
      },
    ],
  },
  {
    slug: "what-happened-what-was-decided",
    title: "What a record is for: what happened, what was decided",
    headline:
      "A noisy chat dump is not a record unless it answers three plain questions",
    eyebrow: "Record Clarity & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Storing raw logs without structure confuses volume with accountability. A working record captures what happened, what was decided, and how to execute next.",
    publishedAt: "2026-11-10",
    description:
      "Separating noise from evidence: structuring workspace records around events, decisions, and repeatable operational runbooks.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — capturing actors, time, and decisions cleanly.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — making project choices auditable.",
      },
      {
        href: "/patterns/tna-section-46",
        label:
          "Public-authority record frameworks — value, integrity, and accountability in practice.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — clarity across handoffs.",
      },
    ],
    citations: [
      {
        label:
          "HMRC Records and Information Collection Policy, published 19 May 2026",
        href: hmrcRecordsUrl,
        datePublished: "2026-05-19",
      },
    ],
  },
  {
    slug: "tna-section-46",
    title: "The National Archives Code of Practice on managing records",
    headline:
      "What public-sector record standards teach teams about keeping only what counts",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "Not every file or chat ping belongs in long-term storage. Examines how FOIA section 46 guidance anchors retention around value, integrity, and accountability.",
    publishedAt: "2026-11-10",
    description:
      "Lessons for builders from The National Archives Code of Practice: distinguishing actionable records from ephemeral messages via value, integrity, and accountability.",
    related: [
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What a record is for — structuring evidence around events and choices.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — keeping reasoning reachable.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write a record someone else can read — clarity for future stewards.",
      },
    ],
    citations: [
      {
        label: "GOV.UK landing, 15 July 2021",
        href: tnaCodeLandingUrl,
        datePublished: "2021-07-15",
      },
      {
        label:
          "Code of Practice on the Management of Records issued under section 46 of the Freedom of Information Act 2000, laid 15 July 2021, ISBN 978-1-5286-2517-3",
        href: tnaCodePdfUrl,
        datePublished: "2021-07-15",
      },
    ],
  },
  {
    slug: "articles-of-incorporation",
    title: "What are articles of association",
    headline:
      "Standing rules, daily minutes, and membership lists do not belong in the same bucket",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "Constitutional rules govern how an organization can act; minutes record what happened Tuesday; registers list who joined. Keep these three artifacts clearly separated.",
    publishedAt: "2026-11-10",
    description:
      "Distinguishing standing governance articles from daily meeting minutes and membership registers in shared-work architectures.",
    related: [
      {
        href: "/patterns/company-registers",
        label:
          "What is a company register of members — separating participant rosters from bylaws.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Directors' meeting minutes — tracking board-level operations over time.",
      },
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and general meetings — member-level approvals.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label: "Two records, one agreement — binding execution terms cleanly.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.18",
        href: section18Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.20",
        href: section20Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.17",
        href: section17Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.8, for the contrast",
        href: section8Url,
        datePublished: companiesActYear,
      },
    ],
  },
  {
    slug: "company-registers",
    title: "What is a company register of members",
    headline:
      "A participant roster and a decision minute book solve completely different bugs",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "Trying to extract who joined, who left, and who voted from a giant chronological thread always fails under pressure. Maintain an explicit member register alongside the minute log.",
    publishedAt: "2026-11-10",
    description:
      "Why membership rosters track start/end dates separately from meeting minutes and economic balances.",
    related: [
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "What are articles of association — rules separated from member lists.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Directors' meeting minutes — tracking leadership decisions separately.",
      },
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and general meetings — preserving formal approvals.",
      },
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — evidence of membership vs. enforceable title.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.113",
        href: section113Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.1134",
        href: section1134Url,
        datePublished: companiesActYear,
      },
    ],
  },
  {
    slug: "sole-member-decisions",
    title: "Records of decisions by a sole member",
    headline:
      "Working alone does not remove the need to write down key project decisions",
    eyebrow: "Membership & Resolutions",
    shelf: "membership-resolutions",
    indexBlurb:
      "Solo maintainers skip notes because nobody disagreed—until six months later when a partner joins or a dispute arrives. Capture single-steward calls explicitly in the record.",
    publishedAt: "2026-11-10",
    description:
      "Why decisions taken by a single steward or sole member still belong in the written project record, backed by statutory contrast.",
    related: [
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and general meetings — full approval trails.",
      },
      {
        href: "/patterns/directors-minutes",
        label: "Directors' meeting minutes — multi-party meeting records.",
      },
      {
        href: "/patterns/company-registers",
        label:
          "What is a company register of members — tracking active stewards over time.",
      },
    ],
    citations: [
      {
        label: "Companies Act 2006, s.357",
        href: section357Url,
        datePublished: companiesActYear,
      },
      {
        label: "Companies Act 2006, s.288, for the exception",
        href: section288Url,
        datePublished: companiesActYear,
      },
    ],
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
