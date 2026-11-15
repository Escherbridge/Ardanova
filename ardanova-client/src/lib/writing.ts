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
    title: "Shared Value & Agreements",
    summary:
      "Why cooperative ventures fracture when contribution shares pretend to grant title or decision weight—and how explicit agreements keep both work equity and democratic voice honest.",
  },
  {
    id: "record-clarity",
    title: "Community Memory & Minutes",
    summary:
      "Open decision logs, forward revisions, and capture habits so neighborhood coalitions and guilds inherit shared memory instead of guesswork across seasons.",
  },
  {
    id: "membership-resolutions",
    title: "Voice & Membership",
    summary:
      "Keeping standing charters, active member rosters, group assemblies, and single-steward calls clear—contrasted cleanly with formal registries so commons protect collective power.",
  },
];

export const notes = [
  {
    slug: "solutionary-thinking-in-practice",
    title:
      "What solutionary thinking looks like when a community stops doom-scrolling",
    publishedAt: "2026-11-15",
    description:
      "Moving past symptom-chasing and outrage feeds into root-cause inquiry, testable proposals, and learning in public with the people living through the problem.",
    related: [
      {
        href: "/notes/social-ecology-and-shared-stewardship",
        label:
          "Social ecology for builders: why healthy commons start with how people relate",
      },
      {
        href: "/notes/ownership-without-surveillance",
        label:
          "Ownership without surveillance: sharing in what we build without turning people into data",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label: "Pattern: two records, one agreement",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
      { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
    ],
  },
  {
    slug: "social-ecology-and-shared-stewardship",
    title:
      "Social ecology for builders: why healthy commons start with how people relate",
    publishedAt: "2026-11-15",
    description:
      "Why durable ecological and civic projects depend less on hero founders than on non-extractive institutions, mutual aid, skilled guilds, and federated commons.",
    related: [
      {
        href: "/notes/solutionary-thinking-in-practice",
        label:
          "What solutionary thinking looks like when a community stops doom-scrolling",
      },
      {
        href: "/notes/separating-contribution-ledgers-from-governance-votes",
        label: "Separating shared value from democratic voice",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label: "Pattern: keeping partner commons aligned without a gatekeeper",
      },
    ],
    citations: [
      { label: "ArdaNova homepage", href: "https://ardanova.com/" },
      {
        label: "International Cooperative Alliance Statement (1995)",
        href: "https://ica.coop/en/cooperatives/cooperative-identity",
        datePublished: "1995",
      },
      { label: "ArdaNova terms of use", href: "https://ardanova.com/terms" },
    ],
  },
  {
    slug: "ownership-without-surveillance",
    title:
      "Ownership without surveillance: sharing in what we build without turning people into data",
    publishedAt: "2026-11-15",
    description:
      "How communities, contributors, and sponsors can share in value and steer decisions together with explicit rights—without behavioral profiling or hollow scoreboards.",
    related: [
      {
        href: "/notes/a-record-is-not-a-right",
        label: "A record is not a right",
      },
      {
        href: "/patterns/informal-channels",
        label: "Pattern: decisions on informal channels without monitoring",
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
    ],
  },
  {
    slug: "why-shared-work-breaks-between-orgs",
    title: "Why coalitions and partner guilds drift without shared memory",
    publishedAt: "2026-11-14",
    description:
      "Community builds rarely break from bad intentions. They stall when groups keep separate accounts of what was promised, who joined the decision, and what terms still hold.",
    related: [
      {
        href: "/notes/social-ecology-and-shared-stewardship",
        label: "Social ecology for builders",
      },
      {
        href: "/notes/separating-contribution-ledgers-from-governance-votes",
        label: "Separating shared value from democratic voice",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label: "Pattern: keeping partner commons aligned without a gatekeeper",
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
    title: "Separating shared value from democratic voice",
    publishedAt: "2026-11-14",
    description:
      "Combining money contributed, hours worked, and voting weight into one number lets capital silence a community. Keeping stewardship shares and governance voice independent protects cooperation.",
    related: [
      {
        href: "/notes/why-shared-work-breaks-between-orgs",
        label: "Why coalitions and partner guilds drift without shared memory",
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
      "Shared ownership means naming the real agreement behind what a group builds—never pretending an activity count or screen badge grants legal title on its own.",
    related: [
      {
        href: "/notes/ownership-without-surveillance",
        label: "Ownership without surveillance",
      },
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
      { label: "AZOA Workflows", href: "https://azoa.ardanova.com/workflows" },
    ],
  },
  {
    slug: "two-records-one-agreement",
    title: "Two records. One agreement.",
    publishedAt: "2026-11-09",
    description:
      "Healthy commons track two distinct realities under one clear covenant: stewardship work and shared value on one hand, democratic community voice on the other.",
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
    headline:
      "Written stewardship memory supports trust. The real agreement creates the right.",
    eyebrow: "Shared Value & Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "Even formal statutory registers are only prima facie evidence—never unchallengeable title by themselves. Keep shared ownership honest by grounding every project share in readable agreements.",
    publishedAt: "2026-11-10",
    description:
      "Why stewardship logs and registers serve as transparent evidence rather than creating automatic legal ownership, equity, or payment rights on their own.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — keeping shared value apart from democratic voice.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Keeping partner commons aligned without a gatekeeper — noticing quiet drift early.",
      },
      {
        href: "/patterns/company-registers",
        label:
          "What is a member register — distinguishing participant rosters from ownership claims.",
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
      "Stewardship value is one record. Community voice is another. The agreement binds them.",
    eyebrow: "Shared Value & Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "When capital contributed and democratic authority share one tally, funders silently outvote the community doing the work. Keep stewardship shares and collective voice independent.",
    publishedAt: "2026-11-10",
    description:
      "Separating stewardship value (Record A) from democratic governance authority (Record B) so money never quietly overrides one-member, one-voice commons rules.",
    related: [
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — transparent memory vs. rights rooted in an agreement.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Keeping partner commons aligned without a gatekeeper — shared checkpoints across guilds.",
      },
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "Standing charters vs. daily proceedings — keeping community constitutions readable.",
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
    title: "Keeping partner commons aligned without a central gatekeeper",
    headline:
      "Independent guilds can share real accountability without surrendering autonomy to one boss.",
    eyebrow: "Shared Value & Agreements",
    shelf: "multi-ledger-agreements",
    indexBlurb:
      "Federated projects shouldn't force every workshop or watershed group onto one single landlord server. Pair shared agreement anchors with periodic checkpoints so nobody edits history behind closed doors.",
    publishedAt: "2026-11-14",
    description:
      "How autonomous guilds and cooperative coalitions keep independent stewardship accounts aligned via agreement anchors and open discrepancy checks without centralizing control.",
    related: [
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — why stewardship value and community voice stay distinct.",
      },
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Open revisions beat silent edits — preserving trust across course changes.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — resolving genuine disagreements out in the open.",
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
        label:
          "ArdaNova, Why coalitions and partner guilds drift without shared memory",
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
      "When community choices live only in memory, the loudest voice wins next month.",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "A neighborhood coalition finished a gathering weeks ago and neighbors recall opposite commitments. Capturing date, participants, reasoning, and deliberate pauses builds trust everyone can inspect.",
    publishedAt: "2026-11-10",
    description:
      "Shared community decision logging: capturing what was agreed, why, by whom, and when—including conscious decisions to pause and open course corrections.",
    related: [
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Open revisions beat silent edits — documenting course changes honestly.",
      },
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What shared memory is for — grounding action in evidence, agreements, and next moves.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone can follow — welcoming new stewards without tribal gatekeeping.",
      },
      {
        href: "/patterns/tna-section-46",
        label:
          "Public records stewardship — value, integrity, and accountability in practice.",
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
    title: "Open revisions beat silent edits",
    headline:
      "Erasing yesterday's charter edit hides why the community adapted along the way.",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Quietly overwriting an old shared agreement makes current text tidy while erasing the story of how consensus changed. Append dated revisions linked to prior commitments so learning stays visible.",
    publishedAt: "2026-11-14",
    description:
      "Why open append-only revisions and explicit change notes protect community trust better than silent document rewrites.",
    related: [
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — honoring pauses, reconsiderations, and reasoned shifts.",
      },
      {
        href: "/patterns/reconciling-two-ledgers-without-a-central-custodian",
        label:
          "Keeping partner commons aligned without a gatekeeper — shared revision anchors.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone can follow — letting newcomers trace why rules evolved.",
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
    eyebrow: "Solutionary Loop Pattern",
    publishedAt: "2026-11-10",
    description:
      "Begin with lived reality and the community living with the problem before proposing remedies, roles, or funding structures.",
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
    title: "Stewardship circle and board minutes",
    headline:
      "Minutes protect communities from quiet drift between elected stewards",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "When elected stewards settle major choices verbally and forget to record them, accountability dissolves within a year. Statutory minute standards reveal why clear retention discipline matters.",
    publishedAt: "2026-11-10",
    description:
      "Why stewardship circles record proceedings and keep memory across seasons, contrasted cleanly with formal UK statutory company baselines.",
    related: [
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Community assemblies & written resolutions — preserving broader member voice cleanly.",
      },
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — date, participants, channel, and actual commitments.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — day-to-day shared accountability between assemblies.",
      },
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "Standing charters vs. daily proceedings — keeping community rules readable.",
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
    title: "Records of resolutions and general assemblies",
    headline: "Written consent and live community votes belong side by side",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "Not everyone can make Tuesday's town hall. Keeping async written resolutions beside live assembly votes ensures working members aren't erased when memory gets checked next year.",
    publishedAt: "2026-11-10",
    description:
      "Keeping async written consent, assembly votes, and single-steward calls clear and accessible so participation stays fair across schedules.",
    related: [
      {
        href: "/patterns/directors-minutes",
        label:
          "Stewardship circle minutes — tracking delegated group choices clearly.",
      },
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — simple fields that survive seasonal turnover.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records. One agreement. — keeping member voice separate from financial shares.",
      },
      {
        href: "/patterns/sole-member-decisions",
        label:
          "Single-steward decisions — documenting early founder calls before neighbors join.",
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
      "Four simple fields keep a gathering useful long after the chairs are stacked",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "A summary reading 'good talk, aligned on next steps' leaves everyone confused a season later. Capture when, where, who took part, and the plain commitment made.",
    publishedAt: "2026-11-10",
    description:
      "A humane minimal habit for community notes—date, time, participants, medium, and explicit outcome—drawn from durable institutional records practice.",
    related: [
      {
        href: "/patterns/informal-channels",
        label:
          "Decisions on informal channels — honoring privacy while saving agreed outcomes.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone can follow — welcoming new contributors cleanly.",
      },
      {
        href: "/patterns/decision-logs",
        label:
          "Decision logs — honoring reversals and conscious pauses transparently.",
      },
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What shared memory is for — context, commitments, and how-to practice.",
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
    title: "Write memory anyone new can follow",
    headline:
      "Insider shorthand turns a community commons into a closed clique",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Cryptic notes like 'same as last Thursday, Sam handling' shut out anyone who wasn't in the room. Explaining why an option was set aside keeps shared power genuinely open.",
    publishedAt: "2026-11-10",
    description:
      "Writing decision memory clearly enough that neighbors joining months later can understand choices and participate on equal footing.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — capturing essential time and attendance context.",
      },
      {
        href: "/patterns/informal-channels",
        label:
          "Decisions on informal channels — welcoming side-channel choices into shared space.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — keeping community reasoning searchable.",
      },
      {
        href: "/patterns/append-only-amendments-and-retractions",
        label:
          "Open revisions beat silent edits — showing how perspectives shifted over time.",
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
    title: "Decisions in informal chats still belong to the commons",
    headline:
      "A side conversation shouldn't lock agreements away from the whole group",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Real mutual aid and project pivots happen on porches, voice calls, and group texts. Mirroring the agreed commitment into the commons preserves trust—without ever turning messaging into surveillance.",
    publishedAt: "2026-11-10",
    description:
      "How cooperative groups bring agreements reached in casual chats back into open shared memory while respecting human privacy and refusing workplace monitoring.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — recording channel, date, participants, and outcome.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Stewardship circle minutes — ensuring informal venues never hide authority.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone new can follow — keeping shared context accessible.",
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
    title: "What shared memory is for: what happened, what was decided",
    headline:
      "Hoarding raw chat transcripts isn't transparency unless people can find answers",
    eyebrow: "Community Memory & Minutes",
    shelf: "record-clarity",
    indexBlurb:
      "Dumping every group message into a shared archive overwhelms volunteers. A humane commons separates what actually happened, what the group decided, and how people work together next.",
    publishedAt: "2026-11-10",
    description:
      "Separating noisy message dumps from living community memory centered on events, shared decisions, and repeatable stewardship habits.",
    related: [
      {
        href: "/patterns/what-belongs-in-the-minutes",
        label:
          "What belongs in the minutes — capturing participants, time, and choices cleanly.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — making group choices clear to everyone.",
      },
      {
        href: "/patterns/tna-section-46",
        label:
          "Public records stewardship — keeping what holds value and letting clutter go.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone new can follow — welcoming fresh contributors.",
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
    title: "Keeping what matters and letting clutter retire",
    headline:
      "Public stewardship standards teach collectives not to hoard everything forever",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "Communities don't need surveillance archives that preserve every stray message indefinitely. FOIA section 46 principles frame a lighter alternative rooted in value, integrity, and accountability.",
    publishedAt: "2026-11-10",
    description:
      "What community guilds can borrow from The National Archives s.46 Code: valuing clear stewardship memory over endless data hoarding.",
    related: [
      {
        href: "/patterns/what-happened-what-was-decided",
        label:
          "What shared memory is for — separating facts and choices from raw noise.",
      },
      {
        href: "/patterns/decision-logs",
        label: "Decision logs — keeping reasoning reachable.",
      },
      {
        href: "/patterns/readable-record",
        label:
          "Write memory anyone new can follow — clarity for future stewards.",
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
    title: "Standing charters vs. daily agreements",
    headline:
      "Constitutional rules, seasonal decisions, and member rosters stay clearest apart",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "Standing community charters set how decisions are fairly made; minutes capture what happened Tuesday; rosters track active voices. Keep those three drawers visibly separate.",
    publishedAt: "2026-11-10",
    description:
      "Distinguishing standing governance charters and cooperative bylaws from daily meeting notes and member rosters.",
    related: [
      {
        href: "/patterns/company-registers",
        label:
          "What is a member register — tracking active voices clearly across seasons.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Stewardship circle minutes — recording delegated coordination over time.",
      },
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and assemblies — member-wide decisions kept open.",
      },
      {
        href: "/patterns/two-records-one-agreement",
        label:
          "Two records, one agreement — keeping voice separate from economic shares.",
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
    title: "Keeping an honest member roster",
    headline:
      "Knowing who currently holds a voice shouldn't require archaeology",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "Trying to reconstruct who joined, who stepped back, and who votes cleanly from months of old discussion threads inevitably disenfranchises quiet members. Keep an open roster distinct from minutes.",
    publishedAt: "2026-11-10",
    description:
      "Why explicit participant rosters with clear join and step-back dates protect fairness compared to digging through old chat or meeting threads.",
    related: [
      {
        href: "/patterns/articles-of-incorporation",
        label:
          "Standing charters vs. daily agreements — keeping bylaws and rosters apart.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Stewardship circle minutes — tracking leadership proceedings separately.",
      },
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and assemblies — keeping community votes clear.",
      },
      {
        href: "/patterns/a-record-is-not-a-right",
        label:
          "A record is not a right — membership lists vs. documented participation terms.",
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
    title: "Documenting early calls before neighbors join",
    headline:
      "Starting solo does not excuse hiding the assumptions your project rests on",
    eyebrow: "Voice & Membership",
    shelf: "membership-resolutions",
    indexBlurb:
      "Projects often begin with one person sketching an ecological or civic response alone—until collaborators arrive months later wondering why key commitments exist. Writing early decisions down welcomes new co-stewards honestly.",
    publishedAt: "2026-11-10",
    description:
      "Why early initiators still leave clear written notes of founding commitments so incoming community members step into shared clarity rather than founder myth.",
    related: [
      {
        href: "/patterns/records-of-resolutions",
        label:
          "Records of resolutions and assemblies — opening decisions to wider consent.",
      },
      {
        href: "/patterns/directors-minutes",
        label:
          "Stewardship circle minutes — scaling cleanly as more stewards join.",
      },
      {
        href: "/patterns/company-registers",
        label:
          "Keeping an honest member roster — onboarding new members fairly.",
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
