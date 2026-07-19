# Frontend experience

## Status and scope

This document is the implementation contract for ArdaNova's redesigned Next.js frontend. It covers the public story, authenticated workspace, Nova-assisted workflows, responsive behavior, accessibility, and transaction-state language.

The north star is:

> **Expressive in story. Quiet in work. Explicit whenever a person or machine can change state.**

The canonical visual rules live in [Brand guidelines](./BRAND_GUIDELINES.md). The [Linguistic guide](./LINGUISTIC_GUIDE.md) defines product voice, community activity language, CTA grammar, and anti-surveillance guardrails. Product and technical source material remains in [`conductor/`](../conductor/), the architecture documentation, and the relevant API contracts.

---

## Experience architecture

### Core product loop

Every primary journey should make one recurring sequence legible:

```text
Discover a problem -> Define a solution -> Iterate -> Discover again
```

- **Discover** starts with affected people, lived context, evidence, constraints, and open questions. Discovery surfaces should help a person find a problem worth working on, not an attention trend to consume.
- **Define** turns that context into a reviewable solution brief with assumptions, scope, roles, milestones, participation terms, decision authority, and explicit rights. Nova may help draft the artifact; a person owns the framing and review.
- **Iterate** connects contributions, work updates, decisions, evidence, exceptions, and lessons to the next version. It can move forward, return to discovery, or end in a documented stop.

The loop is the organizing model for onboarding, project creation, project discovery, activity, Nova Studio, and project status language. Show the current stage, unresolved questions, and next meaningful move. Do not reduce progress to posting frequency, reactions, popularity, or time spent in the application.

### Public story

The landing and sign-in experiences use editorial scale, flat color fields, hard rules, and a small number of authored abstract forms. They should answer:

1. What can I make here?
2. How can other people participate?
3. What rights or value are—and are not—created?
4. What is available today?

Do not use fabricated counters, dead calls to action, or claims that every contribution automatically creates ownership.

### Working application

The authenticated interface is a calm project desk. Navigation is grouped by intent:

- **Home** — current work and decisions
- **Projects** — projects a person follows or stewards
- **Work** — tasks and opportunities
- **Community** — people, guilds, and governance
- **Value** — portfolio and supported commerce surfaces
- **Chats** — human coordination
- **Nova Studio** — optional drafting, presentation, and rehearsal workspace

Mobile navigation must preserve these destinations without relying on a permanently visible sidebar.

ArdaNova is **social media for doing, not doom-scrolling**. Social surfaces should be organized around field notes, open questions, proposals, contributions, decisions, invitations, and bounded work updates. Prefer a project workstream with a clear stopping point or next action over an endless generic feed. Recommendations should explain their relevance instead of implying opaque behavioral profiling.

### Project workspace

Project views should keep the artifact central and move secondary controls into clear regions. Avoid multiplying tabs without hierarchy. A project may contain planning, work, community, governance, funding, and evidence, but the interface should surface the next relevant decision rather than expose every subsystem at once.

---

## Nova interaction contract

Nova has five user-visible modes:

| Mode     | Purpose                                 | Example output                              |
| -------- | --------------------------------------- | ------------------------------------------- |
| Ask      | Explain the current artifact or state   | Plain-language answer with source scope     |
| Draft    | Propose new project material            | Editable brief, milestones, or task outline |
| Review   | Find gaps, conflicts, or unclear terms  | Review notes linked to fields               |
| Present  | Turn approved material into a narrative | Presentation outline or speaker notes       |
| Rehearse | Help a person prepare to present        | Questions, timing, and coaching notes       |

Each result must show:

- The project or artifact in scope
- Excluded material and unavailable context
- Sources, assumptions, and uncertainty where relevant
- The exact accepted upstream version for derived artifacts; superseded derivations are visibly stale and cannot unlock downstream work
- A visible difference or proposal, not an unexplained replacement
- Human controls to accept, edit, reject, or undo

Nova output is always a draft until a person accepts it. Studio starts blank: it
does not preload an accept-ready project or disguise example prose as a person's
work. In the current Studio and global Nova interface previews, acceptance
changes validated, size-bounded browser-tab session state only and does not
write an ArdaNova record. Drafts survive navigation and refresh in that tab, do
not sync across tabs or devices, and end with the tab session. A connected
implementation may update a working artifact only after that behavior is
explicitly wired and labeled. Publication, funding, governance, credential,
swap, escrow, payout, and rights-changing actions remain separate explicit
workflows.

Nova should reinforce the solutionary loop: help document and compare context during **Discover**, shape an inspectable proposal during **Define**, and surface evidence, differences, and open questions during **Iterate**. It must not collapse all three stages into an opaque "generate a project" action.

### Current integration status

The frontend provides the interaction model and a Studio preview. The sibling AI service exposes project-analysis and pitch workflow routes, but some LLM, MCP, and Gamma execution paths remain stubs or TODOs. Until those integrations are configured and verified, the UI must label generated examples as an **interface preview** and must not claim that external presentation generation completed.

---

## Trust-state contract

User-visible state follows a four-stage model:

```text
Draft -> Submitted -> Confirmed -> Reconciled
```

- **Draft** is editable work with no external effect.
- **Submitted** means an instruction left the interface.
- **Confirmed** means the receiving service or ledger accepted it.
- **Reconciled** means ArdaNova verified and reflected the result in its own records.

A failure or review requirement can occur between any stages. The UI must retain the last known evidence and give a concrete recovery path.

### Required distinctions

- A saved wallet address is not proof of wallet control.
- Task completion is not proof that an award was reconciled.
- Escrow release authorization is not contributor settlement.
- A transaction identifier is evidence of submission, not automatic success.
- A project token, membership credential, and ownership share are distinct instruments.
- Portfolio totals should distinguish pending, confirmed, and reconciled value.

### Azoa identity and custody

The profile is the humane setup surface for a tenant-bound Azoa account. The
person explicitly creates or continues setup; merely opening a page never
creates an avatar, wallet, KYC submission, or chain instruction.

The interface shows independent, non-substitutable states:

1. tenant- and user-bound Azoa identity readiness;
2. the selected KYC provider's operational readiness and authoritative status;
3. Azoa wallet-provisioning and existing-wallet readiness.

Identity creation remains useful when wallet custody is unavailable. A missing
production KMS/HSM must not block a person from creating their Azoa identity or
starting an independently ready KYC flow, and the interface must not collapse
those partial states into a generic failure.

Azoa owns its configured key-custody boundary, wallet creation, the KYC ledger, and provider
integration. ArdaNova holds only thin ids, a public address where appropriate,
and readiness evidence. Private keys, mnemonics, signing secrets, raw KYC
documents, and tenant credentials must never enter a browser response or an
ArdaNova persistence model. External addresses saved in wallet settings remain
non-custodial references and require a separate signed control proof.

Hosted verification opens only through an explicit user action and an HTTPS
provider destination validated by the ArdaNova API. ArdaNova never asks a person
to paste an identity-document URL. A manual/reference provider remains an
operator-only scaffold until Azoa supplies private uploads, opaque object ids,
malware scanning, scoped reviewer access, retention/deletion, and audit evidence.

Account readiness never implies authorization for funding, allocation, minting,
bridging, swapping, escrow release, or payout. Each remains a distinct reviewed
workflow with its own evidence and reconciliation state.

Project creation checks this readiness before the final create action and tells
the person exactly which setup surface can resolve it. Drafting may continue
without pretending the project exists: the wizard persists a validated,
versioned recovery copy in the current browser tab, survives a trip to
verification, and clears only after the project record is confirmed. The API
rechecks KYC and custody authoritatively at creation time; browser state is never
proof of eligibility.

### Coordination and read evidence

Chat is for coordination, not attention capture. A read receipt is emitted only
while the document is visible, the browser has focus, the reader is at the live
edge of the conversation, and an incoming message intersects the message
viewport. The client sends that visible message's server timestamp; it never
substitutes the local current time. Loading a background tab, selecting a
conversation while scrolled up, or receiving a realtime event is not evidence
of reading. The message stream is an accessible live log so new coordination is
announced without turning typing status or historical pagination into false
activity.

---

## Component rules

### Activity and discovery

- Name the object being shared: field note, open question, proposal, decision, contribution, invitation, or work update.
- Write activity as a person performing a meaningful verb on a project object; avoid vague "engaged with" language.
- Prioritize relevance to active work, affected people, unanswered questions, and requested skills over popularity.
- Do not celebrate impressions, scrolling time, content velocity, or virality as product success.
- Give a recommendation basis when known, such as a selected skill, tracked project, service area, or explicit interest.
- Keep privacy and retention claims aligned with implemented behavior and policy; the interface must not invent guarantees.

### Buttons and links

- Use a button for an action and a link for navigation.
- Button labels describe the immediate outcome: “Create project,” “Review draft,” or “Submit release instruction.”
- In solutionary journeys, prefer stage-specific actions such as “Describe the problem,” “Shape a solution,” and “Review this iteration.”
- Destructive and rights-changing actions require an explicit confirmation step.
- Icon-only controls require an accessible name.

### Cards and rows

- A clickable card must be a semantic link or button and remain keyboard operable.
- Avoid nested interactive elements inside a card-wide link.
- Status is written in text; color and iconography are supporting cues only.

### Forms

- Every input has a visible label and useful error message.
- Explain why sensitive or unusual information is needed before asking for it.
- Preserve entered data when validation or a request fails.
- Do not report success before the underlying state reaches the named stage.

### Motion

- Motion may orient a person to a panel, changed field, or reordered item.
- Honor `prefers-reduced-motion`.
- Avoid ambient looping motion in the workspace and around financial actions.

---

## Accessibility and responsive baseline

The release target is WCAG 2.1 AA for core journeys.

- Complete all primary journeys with a keyboard.
- Keep focus indicators visible against every color field.
- Meet contrast requirements for text, controls, and meaningful graphics.
- Use heading order, landmarks, form labels, and live regions appropriately.
- Provide text alongside status color and icons.
- Keep touch targets at least 44 by 44 CSS pixels where practical.
- Verify landing, sign-in, dashboard, project creation, Studio, portfolio, swap, and escrow at desktop and narrow mobile widths.

---

## Development preview authentication

Local interface work may use `DEV_AUTH_BYPASS=true`. The bypass is intentionally constrained:

- It is accepted only while `NODE_ENV=development`.
- Production continues through the configured NextAuth providers even if the flag is accidentally present.
- A visible banner identifies the preview session.
- The flag belongs in a local frontend environment only and is never a production Railway variable.
- A preview session does not waive backend authorization; data-dependent routes may still require a seeded local user and services.

Turn the flag off before testing the real sign-in path.

---

## Release checklist

1. Run formatting, linting, type checking, unit tests, and a production build.
2. Exercise the public, sign-in, dashboard, Studio, portfolio, swap, and escrow surfaces at desktop and mobile widths.
3. Verify the normal authentication route with the preview flag absent.
4. Confirm no production environment includes `DEV_AUTH_BYPASS`.
5. Promote the reviewed `.NET` API digest first, then the paired frontend digest from the GitHub Actions release manifest. Production services must use image sources pinned by digest; repository linking and nested `railway.toml` source-build configuration are intentionally unsupported.
6. Observe terminal Railway success for both services, then verify backend `/health` and `/ready`, frontend `/api/health` and `/api/ready`, auth, and representative public/protected routes on the deployed domains.

---

## Change discipline

When adding a workflow, document its human decision, machine assistance, state transitions, evidence, and recovery path. If an action can affect money, rights, governance, identity, or publication, clarity beats compression.
