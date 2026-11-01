---
type: reference
---

# components/projects

The project page's tabs and the forms behind them. The page itself is `src/app/projects/[slug]/page.tsx`.

## Project page

SR-03 Phase 3, Lane H. Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1.

- **Feed is the first tab and the default.** It shows the composer (team only), then `FeedStream scope="project:{id}"`.
  The scope names the project **id**: the API resolves it by id (FeedService `FeedScopeKind.Project`), and a slug
  would 404.
- **Light header:** status, title, the lede (`projectLedeOf`, moved here from Overview so it shows once), the tag
  chips, the creator and Edit/Publish, and Follow. Funding, metrics, credentials and the commerce notice render
  where they did; money copy is untouched.
- **Tags** (`projectTagsOf`): the API's derived `hashtags` (`#sector`, `#type`, from .NET `ProjectHashtagDisplay`)
  then the free `tags` column (the hashtags that resolved to neither), canonicalised with the same rule as post
  copy and deduped, each a `/tag/x` link. Legacy free text that is not a tag (`community garden`) stays as a plain
  chip rather than a dead link.
- **`?tab=` deep links** (`projectTabFrom`): a known tab opens itself, `?tab=updates` opens Feed, `?proposalId=`
  without a tab opens Proposals. Picking a tab writes `?tab=` with `history.replaceState` (no navigation, no RSC
  request; Next syncs it into `useSearchParams`). A picked tab holds only for the URL it was picked on, so an in-page
  link such as a DISCUSSION card's Reply (`?tab=comments`) still switches tabs.
- **Leading cards** live in the page, above the tab switch, keyed by project id: a just-published post survives a
  tab switch, and in-app navigation to another project does not carry it over. A saved ask (same id) moves to the
  top rather than appearing twice.

## Updates folded into Feed

The Updates tab is gone and `updates-tab.tsx` is deleted (plan §8-7: two update writers, the composer wins). It is
folded rather than kept as a read-only view because every row it wrote (`ProjectUpdate`) is already projected into
the feed as a PUBLIC `PROJECT_UPDATE` card (`ArdaNova.Application/Feed/AGENTS.md` §Mapping), so a filtered view
would only repeat the stream. Its delete control never fired for a stored row (it compared against the optimistic
row's placeholder id), so nothing reachable was lost. The Overview tab no longer mounts `ProjectFieldNotes` either:
that was a second composer and a second list of the same posts.

## Follow

`useProjectFollow` (`project-feed.tsx`) is the one state behind two toggles: the header button and every card's
subject chip on this project's stream.

- Readers who can follow: signed in, and known to be off the team. Until the member list answers, neither the button
  nor its `getMyFollowState` read happens, so a member never sees Follow flash.
- **Serialized optimistic toggle.** A click shows at once, in the header and on every cached feed card of this
  project (`setQueriesData` over all `feed.getPage` caches, so Home's cards agree too). Requests run in click order,
  so the server ends on the last click; only the latest click may restore state when the API refuses.
- **One state, handed to the stream.** `FeedStream`'s `following` prop (keyed by `feedSubjectKey`) wins over its own
  per-card optimistic override, so the page passes `{ "project:<id>": follow.following }` and a header change after a
  chip tap shows on every chip with no remount (the old `streamKey` workaround is gone; the final join added the
  prop). A chip on another project or a guild goes through the shared `lib/feed/use-subject-follow.ts`, the same
  dispatch Home and tag pages use, and keeps the stream's own optimistic state.

## Deferred

- `ProjectFieldNotes`, `post.getProjectFieldNotes` and the `project.createUpdate/getUpdates/deleteUpdate`
  procedures have no page mount now. Their owners (Lanes F/C and P) can delete them; the trigger is the next pass
  over those files.
