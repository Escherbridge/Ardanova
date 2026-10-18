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
  Error copy is keyed by tRPC code: `TOO_MANY_REQUESTS` is the member's own daily limit,
  `SERVICE_UNAVAILABLE` is Nova AI's platform-wide capacity ("not your own limit"), `NOT_FOUND` is an
  expired or foreign session ("start again"), `TIMEOUT` is a slow turn, `BAD_REQUEST` is input, else
  unavailable. A go/adjust brief has "Copy brief" (plain text, `briefAsText`) and a link to
  `/projects/create`; the copy says plainly that nothing is filled in for you. The only OVERSEASGENAI
  mention is the footer credit.
- **Preview tools** (Ask, Draft, Review, Present, Rehearse): still local template copy, labelled as
  such. They become live only when a backend exists for them; keep their disclaimers until then.

Per-route trust copy comes from `~/lib/nova-context.ts` (`contextByArea`); extend that map for new
surfaces rather than falling back to `fallbackContext`.

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

## Stale-source detection (`nova-history.ts`)

Present is built from an accepted Draft, Rehearse from an accepted Present. Each snapshot records
the `{mode, contentVersion}` it was built from, and `hasCurrentUpstream` compares that pointer with
the *currently* accepted upstream version, not a "was ever accepted" flag. That is what flips an
accepted Present to "stale" (Accept disabled) once a newer Draft is accepted.

The comparison is only sound if a content version is never reused for different content. Undo used
to pop the present state, so the next edit re-issued the undone number and an old Present matched a
different Draft. `NovaHistory.highWater` now records the highest version ever issued (undone ones
included, persisted through `compactHistories` and the session schema), and `nextContentVersion`
always issues above it. Do not compute versions as `present + 1`. `nova-history.test.ts` replays the
undo, re-edit, accept sequence that used to pass stale content as current.
