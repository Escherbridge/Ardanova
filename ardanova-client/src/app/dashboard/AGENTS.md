---
type: reference
---

# app/dashboard — Home is the feed (SR-03 Phase 3, Lane G)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1, §5, Revisions 1-2 (H4).

## page.tsx, top to bottom

Main column (≤40rem): `SetupPrompt` (verification nudge, unchanged) → composer → `FeedStream scope="home"` → the
cold-start row where `nextCursor` is null. Rail (lg+, sticky): "Your work" (up to three open assigned tasks) → the
record notes. Below lg the rail stacks after the stream, so "Your work" is hidden there and the notes close the page.

The hero, the Discover/Define/Iterate lists, featured projects, the platform-wide proposals/events lists and both
Studio CTAs ("Open Nova Studio", "Draft with Nova") are gone. Studio is reachable from the sidebar utilities.

## Composer

`FieldNoteComposer` is mounted with its current props (`projectId`, required), so Home offers it over the person's
own projects (`project.getMyProjects`, which is projects they created) with a "Post to" picker when there are
several, and shows no composer to someone with none. **Join step:** once Lane C's composer takes an optional
attach target, mount it bare and delete `HomeComposer`'s picker.

A just-published post goes first via `leadingCards`. `usePublishedCards` observes the mutation cache for
`post.create` successes while Home is open (Observer pattern) instead of relying on the composer's callback, so it
keeps working whatever callback shape the composer ends up with, and it holds the posts in state so mutation-cache
garbage collection cannot drop them. The card is the author's own view of what the API will later serve.

## Cold-start row data source

No API reports "sectors with content" (`taxonomy.getAll` lists terms, not usage), so the chips are the sectors on
the cards already loaded (`sectorsSeen`, via `FeedStream`'s default end for `home`). Those are by construction
sectors with content the reader can see.

## Guard copy kept (verbatim, exempt from "Just enough")

`RecordNotes` keeps both lines the copy lane ruled must stay: the stream carries PROPOSAL and EVENT_SCHEDULED cards
("Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as
attendance or consent.") and FUNDING_PROGRESS / MEMBER_JOINED cards ("Participation is not proof of ownership.
Governance credentials, project utility, and economic rights remain separate, reviewable records."). They sit once,
in the rail, closed by the same double rule that ends the stream.

## Deferred

- "Your work" runs its query on small screens too, where it is hidden. One cheap read; revisit with a media-query
  gate if the rail grows more reads.
- Posting to a project the person joined but did not create waits for Lane C's composer (above).
