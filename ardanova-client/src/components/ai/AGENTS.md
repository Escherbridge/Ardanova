# Nova AI components

`nova-assistant.tsx` is the topbar sheet (CTA "Nova AI"). It has two surfaces:

- **Refine idea** (`nova-refine-panel.tsx`): live. Calls tRPC `nova.refineProject` → .NET
  `POST /api/nova/project-refinement` → OVERSEASGENAI's partner validate API, white-labelled as
  Nova AI (see `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Ogai/AGENTS.md`).
  The conversation lives in `NovaAssistant` state so it survives closing the sheet and route changes,
  and is saved to this tab's sessionStorage (see §Refinement persistence). Before the first turn the
  sheet reads `nova.status` (.NET `GET /api/nova/status`): `off` replaces the form with a plain "not
  available on this deployment", `local-preview` shows the preview disclosure before anything is typed,
  and an unknown status (null, API unreachable) keeps the form, so a brief outage never reads as "off".
  Error copy is keyed by tRPC code: `TOO_MANY_REQUESTS` is the member's own daily limit (Start stays
  disabled after it), `CONFLICT` is a turn of theirs still running (one at a time, see the .NET Ogai notes),
  `SERVICE_UNAVAILABLE` is Nova AI's platform-wide capacity ("not your own limit"), `NOT_FOUND` is an
  expired or foreign session ("start again"), `TIMEOUT` is a slow turn, `BAD_REQUEST` is input, else
  unavailable. A go/adjust brief has "Copy brief" (plain text, `briefAsText`) and a link to
  `/projects/create`; the copy says plainly that nothing is filled in for you. It can also become a
  private draft project (§Brief to draft). The only OVERSEASGENAI mention is the footer credit.
  When live, the sheet shows "N of 10 refinements left today" from `nova.status` (refetched after each
  first turn). While a turn is pending, a status line says most answers take seconds and a slow one up to
  `NOVA_TURN_TIMEOUT_MS` (100 s), derived from the constant so the copy cannot drift from the real budget
  again (it said "up to a minute" for two loops). After a member's own turn, focus moves to the new answer
  box or the verdict and an `sr-only` live region reads the new question or outcome, so screen-reader and
  keyboard users are not left on a disabled button.
- **Preview tools** (Ask, Draft, Review, Present, Rehearse): still local template copy, labelled as
  such. They become live only when a backend exists for them; keep their disclaimers until then.

Per-route trust copy comes from `~/lib/nova-context.ts` (`contextByArea`); extend that map for new
surfaces rather than falling back to `fallbackContext`.

## Brief to draft (`nova-brief-draft.ts`)

A go/adjust brief can become a project with one deliberate click: the member picks a category, and the panel
calls the projects lane's existing tRPC `project.create`, which always creates a `DRAFT` that only the creator,
members and admins can see. Nothing is published, and nothing happens without that click. `briefAsProjectDraft`
fills only what the brief honestly carries (working name to title, one-liner to description and summary, problem,
offer to solution, customer to target audience). A field shorter than `project.create`'s 10-character minimum, or
an upstream placeholder ("to decide"), falls back to the member's own idea, so create never refuses the draft.
Pricing, channels, milestones and metrics stay in the brief for the member to carry over by hand: they do not map
onto funding or timeline fields without inventing numbers. Tests: `nova-brief-draft.test.ts`.

## Refinement persistence (`nova-refine-storage.ts`)

A reload used to drop the whole refinement silently. The conversation (idea, answered turns, last
reply including its signed `state` and `sessionId`, and the half-typed answer) is now saved under one
tab-scoped key, `ardanova:nova:refine:v1`, not per route like the preview tools, because a refinement
is one conversation that follows the member around. It is schema-checked on read (a tampered or old
copy starts clean), and Start over (empty idea, no reply) removes it. sessionStorage is per tab, so
nothing outlives the tab, and the state it holds is useless to anyone else: .NET binds it to the actor
(`NovaSessionTickets`). When storage is blocked, the header says a reload loses the conversation and a
`beforeunload` warning guards an in-progress refinement, the same contract the preview tools follow
for unsaved edits. Tests: `nova-refine-storage.test.ts`.

## Session ticket size (`lib/nova-state-limits.ts`)

The `state` a member carries is `nv1.<10-digit expiry>.<64 hex HMAC>.<upstream state>`: a fixed
80-character envelope around OGAI's state. The tRPC input and the tab-storage schema both used to cap
`state` at 512, the pre-ticket upstream limit, so the envelope ate 80 characters of it with zero margin:
an upstream state over 432 characters would have been issued by .NET and then refused (400) on the next
turn, or dropped on reload. Both caps are now `NOVA_STATE_MAX_CHARS` = envelope + `OGAI_STATE_MAX_CHARS`
(512). .NET mirrors the same numbers (`NovaSessionTickets.EnvelopeLength`, `MaxUpstreamStateLength`,
`MaxTicketLength`) and refuses an oversized continuation before any HMAC work. If OGAI ever documents a
different state limit, change both twins together. Boundary tests: `server/api/routers/nova.test.ts` and
`NovaSessionTicketsTests` in Application.Tests.

## Stale-source detection (`nova-history.ts`)

Present is built from an accepted Draft, Rehearse from an accepted Present. Each snapshot records
the `{mode, contentVersion}` it was built from, and `hasCurrentUpstream` compares that pointer with
the _currently_ accepted upstream version, not a "was ever accepted" flag. That is what flips an
accepted Present to "stale" (Accept disabled) once a newer Draft is accepted.

The comparison is only sound if a content version is never reused for different content. Undo used
to pop the present state, so the next edit re-issued the undone number and an old Present matched a
different Draft. `NovaHistory.highWater` now records the highest version ever issued (undone ones
included, persisted through `compactHistories` and the session schema), and `nextContentVersion`
always issues above it. Do not compute versions as `present + 1`. `nova-history.test.ts` replays the
undo, re-edit, accept sequence that used to pass stale content as current.
