# Nova AI components

`nova-assistant.tsx` is the topbar sheet (CTA "Nova AI"). It has two surfaces:

- **Refine idea** (`nova-refine-panel.tsx`): live. Calls tRPC `nova.refineProject` → .NET
  `POST /api/nova/project-refinement` → OVERSEASGENAI's partner validate API, white-labelled as
  Nova AI (see `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Ogai/AGENTS.md`).
  The conversation lives in `NovaAssistant` state so it survives closing the sheet, and is not
  persisted. A reply with `mode: "local-preview"` comes from .NET's unconfigured-environment stand-in;
  the panel then says nothing was analysed. Error copy is keyed by tRPC code (429 daily limit, 400
  input, else unavailable). The only OVERSEASGENAI mention is the footer credit.
- **Preview tools** (Ask, Draft, Review, Present, Rehearse): still local template copy, labelled as
  such. They become live only when a backend exists for them; keep their disclaimers until then.

Per-route trust copy comes from `~/lib/nova-context.ts` (`contextByArea`); extend that map for new
surfaces rather than falling back to `fallbackContext`.

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
