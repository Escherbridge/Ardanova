export const NOVA_MODES = [
  "Ask",
  "Draft",
  "Review",
  "Present",
  "Rehearse",
] as const;

export type NovaMode = (typeof NOVA_MODES)[number];

export interface NovaArtifactContext {
  artifact: string;
  title: string;
  scope: string;
  sources: string[];
  assumptions: string[];
  uncertainty: string;
  suggestedPrompts: Record<NovaMode, string>;
}

export const NOVA_NON_ACTIONS = [
  "Publish a project or message",
  "Fund, approve, release, swap, or pay out value",
  "Change governance, credentials, ownership, or economic rights",
] as const;

type NovaContextDefinition = Omit<NovaArtifactContext, "suggestedPrompts"> & {
  suggestedPrompts?: Partial<Record<NovaMode, string>>;
};

const defaultPrompts: Record<NovaMode, string> = {
  Ask: "Explain the decisions this workspace is asking me to make.",
  Draft: "Draft a clear next step using only the visible context.",
  Review: "Review this artifact for gaps, assumptions, and unclear claims.",
  Present: "Turn this artifact into a concise, evidence-led presentation.",
  Rehearse: "Give me five likely questions and grounded response prompts.",
};

const contextByArea: Array<{
  matches: (pathname: string) => boolean;
  context: NovaContextDefinition;
}> = [
  {
    matches: (pathname) => pathname.startsWith("/studio"),
    context: {
      artifact: "Nova Studio",
      title: "Draft and shape a project story",
      scope: "The project brief and presentation draft open in this studio.",
      sources: ["Fields entered in this session", "Accepted studio drafts"],
      assumptions: ["All generated material remains a working draft"],
      uncertainty:
        "No project records or external research are connected in this interface preview.",
      suggestedPrompts: {
        Draft: "Shape this project intent into a one-page working brief.",
        Present: "Build a six-part presentation from the accepted brief.",
        Rehearse:
          "Challenge the presentation with contributor and funder questions.",
      },
    },
  },
  {
    matches: (pathname) => pathname.startsWith("/projects/create"),
    context: {
      artifact: "New project",
      title: "Turn an idea into a reviewable project brief",
      scope: "Only the unsaved project fields currently visible to you.",
      sources: ["Project form fields", "Your current draft"],
      assumptions: ["Proposed roles and rewards are not commitments"],
      uncertainty:
        "Nova cannot verify demand, feasibility, rights, or contributor consent.",
      suggestedPrompts: {
        Draft:
          "Draft a project brief without inventing evidence or commitments.",
        Review:
          "Flag missing evidence, unclear roles, and rights-related ambiguity.",
      },
    },
  },
  {
    matches: (pathname) => pathname.startsWith("/projects"),
    context: {
      artifact: "Project workspace",
      title: "Make the project easier to understand and review",
      scope:
        "The current project page, its visible records, and your draft text.",
      sources: ["Visible project fields", "Visible activity and milestones"],
      assumptions: ["Displayed records may be incomplete or out of date"],
      uncertainty:
        "Nova cannot verify off-platform work, contributor consent, or legal rights.",
      suggestedPrompts: {
        Ask: "Summarize what is known, what is proposed, and what needs a decision.",
        Present:
          "Outline a project update that separates evidence from ambition.",
      },
    },
  },
  {
    matches: (pathname) =>
      pathname.startsWith("/tasks") || pathname.includes("/tasks"),
    context: {
      artifact: "Task workspace",
      title: "Clarify the work and its evidence",
      scope:
        "The visible task, deliverables, discussion, and completion evidence.",
      sources: ["Task fields", "Visible deliverables and discussion"],
      assumptions: ["Task completion does not prove an award was reconciled"],
      uncertainty:
        "Nova cannot inspect private work or confirm delivery, acceptance, or settlement.",
      suggestedPrompts: {
        Draft:
          "Rewrite this task with a testable outcome and evidence checklist.",
        Review:
          "Separate completion evidence from award and settlement status.",
      },
    },
  },
  {
    matches: (pathname) => pathname.startsWith("/opportunities"),
    context: {
      artifact: "Opportunity",
      title: "Make expectations and fit legible",
      scope: "The visible opportunity, requirements, and application draft.",
      sources: ["Opportunity fields", "Your current application draft"],
      assumptions: ["Drafted claims require your verification"],
      uncertainty:
        "Nova cannot confirm selection, compensation, identity, or counterparty intent.",
      suggestedPrompts: {
        Draft: "Draft a truthful application using only claims I can verify.",
        Review:
          "Identify vague requirements and questions to ask before applying.",
      },
    },
  },
  {
    matches: (pathname) =>
      pathname.startsWith("/guilds") ||
      pathname.startsWith("/people") ||
      pathname.startsWith("/governance") ||
      pathname.startsWith("/events"),
    context: {
      artifact: "Community workspace",
      title: "Support a fair, traceable community decision",
      scope: "Visible community records, proposals, and your unsent draft.",
      sources: ["Visible community records", "Your draft"],
      assumptions: ["Participation does not imply consent or approval"],
      uncertainty:
        "Nova cannot vote, approve, verify identity, or determine community consensus.",
      suggestedPrompts: {
        Review:
          "Check this proposal for affected groups, tradeoffs, and missing evidence.",
        Present:
          "Create a neutral decision brief with options and open questions.",
      },
    },
  },
  {
    matches: (pathname) =>
      pathname.startsWith("/portfolio") || pathname.startsWith("/swap"),
    context: {
      artifact: "Portfolio",
      title: "Explain records without overstating value or rights",
      scope: "The visible portfolio and transaction-status records.",
      sources: [
        "Visible holdings",
        "Visible status and reconciliation records",
      ],
      assumptions: [
        "Displayed values may be estimates rather than realizable value",
      ],
      uncertainty:
        "Nova cannot value assets, provide financial advice, or execute transactions.",
      suggestedPrompts: {
        Ask: "Explain the difference between submitted, confirmed, and reconciled records.",
        Review:
          "Flag estimates, unresolved statuses, and rights that need verification.",
      },
    },
  },
  {
    matches: (pathname) => pathname.startsWith("/chats"),
    context: {
      artifact: "Conversation",
      title: "Prepare a message without speaking for you",
      scope: "Visible conversation context and your unsent message draft.",
      sources: ["Visible messages", "Your unsent draft"],
      assumptions: ["Tone and intent remain yours to verify"],
      uncertainty:
        "Nova cannot infer private intent, consent, or facts outside this conversation.",
      suggestedPrompts: {
        Draft:
          "Draft a concise reply that names the decision and open questions.",
        Review:
          "Check this message for assumptions, pressure, and ambiguous commitments.",
      },
    },
  },
];

const fallbackContext: NovaContextDefinition = {
  artifact: "Workspace",
  title: "Turn visible context into a reviewable next step",
  scope: "The current page and text you choose to provide.",
  sources: ["Visible workspace context", "Your prompt"],
  assumptions: ["Generated content requires human review"],
  uncertainty:
    "Nova may not have the records needed to verify claims or current status.",
};

export function getNovaContext(pathname: string): NovaArtifactContext {
  const match = contextByArea.find(({ matches }) => matches(pathname));
  const context = match?.context ?? fallbackContext;

  return {
    ...context,
    suggestedPrompts: {
      ...defaultPrompts,
      ...context.suggestedPrompts,
    },
  };
}
