---
type: reference
---

# app/dashboard — Home is the feed (SR-03 Phase 3, Lane G)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1, §5, Revisions 1-2 (H4).

## page.tsx, top to bottom

Main column (≤40rem): `SetupPrompt` (verification nudge, unchanged) → composer → the record notes (below lg only) →
`FeedStream scope="home"` → the cold-start row where `nextCursor` is null. Rail (lg+ only, sticky): "Your work" (up
to three open assigned tasks) → the record notes. Final-review M3: below lg the rail used to stack after an infinite
stream, so its notes were effectively never reached; the rail is now hidden there and the same `RecordNotes`
(`components/feed/record-notes.tsx`) renders above the stream instead (two placements, one shown per breakpoint).

The hero, the Discover/Define/Iterate lists, featured projects, the platform-wide proposals/events lists and both
Studio CTAs ("Open Nova Studio", "Draft with Nova") are gone. Studio is reachable from the sidebar utilities.

## Composer

`FieldNoteComposer` is mounted bare with `author` (final join a): its own attach picker offers every project and
guild the person belongs to (`project.getMyMemberships` / `guild.getMyMemberships`, see
`components/feed/composer/AGENTS.md` §Attach list), or nothing at all for a plain `POST`. Anyone signed in gets the
composer; there is no Home-side "Post to" picker.

A just-published post goes first via `leadingCards`. `usePublishedCards` observes the mutation cache for
`post.create` and `post.update` successes while Home is open (Observer pattern) instead of relying on the composer's
callback, so it keeps working whatever callback shape the composer ends up with, and it holds the posts in state so
mutation-cache garbage collection cannot drop them. `post.update` is the composer's "Edit your ask": the edited ask
sits at the top once (same id), showing the stream's copy, which the composer patched with the new words. The card is `feedCardFromPost` (the composer's own builder) with the
project or guild looked up in the composer's attach lists, which share its query cache, so a guild note names its
guild and a joined project names its project without another read.

## Cold-start row data source

No API reports "sectors with content" (`taxonomy.getAll` lists terms, not usage), so the chips are the sectors on
the cards already loaded (`sectorsSeen`, via `FeedStream`'s default end for `home`). Those are by construction
sectors with content the reader can see.

## Guard copy kept (verbatim, exempt from "Just enough")

`RecordNotes` keeps both lines the copy lane ruled must stay: the stream carries PROPOSAL and EVENT_SCHEDULED cards
("Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as
attendance or consent.") and FUNDING_PROGRESS / MEMBER_JOINED cards ("Participation is not proof of ownership.
Governance credentials, project utility, and economic rights remain separate, reviewable records."). They sit in
the rail at lg+ and above the stream below lg, opened by the same double rule that ends the stream; tag pages mount
the same component.

## Deferred

- "Your work" runs its query on small screens too, where it is hidden. One cheap read; revisit with a media-query
  gate if the rail grows more reads.
