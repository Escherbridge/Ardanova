---
type: reference
---

# app/tag/[tag] — one tag's stream (SR-03 Phase 3, Lane G)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1, Revision 1 M6.

- **Signed-in only for now.** `layout.tsx` wraps the segment in `AuthenticatedLayout`, so a signed-out reader is
  redirected to `/auth/signin?callbackUrl=/tag/<tag>` and returns to the tag after signing in. Signed-out tag pages
  are deferred (plan §10); reversing that is deleting this layout and passing `viewerId={null}`.
- **One URL per tag.** `page.tsx` canonicalises the segment with `canonicalHashtag` (the same rule as
  `Hashtags.cs`): `/tag/Carpentry` redirects to `/tag/carpentry`, anything that is not a tag is a 404. Sector chips
  link here via `sectorTag` (`FOOD_BEVERAGE` → `food-beverage`), which the API's tag scope resolves back to the
  sector (Feed/AGENTS.md; plan §11 Q3).
- **Nothing else on the page:** the `#tag` heading and `FeedStream scope="tag:<tag>"`. The stream ends silently, or
  with one line when empty (`defaultFeedEnd`).

## Shared follow hook

`use-subject-follow.ts` is the `onToggleFollow` both Home and this page pass to `FeedStream`: project subjects go
through `project.followProject` / `unfollowProject`, guild subjects through the existing `guild.followGuild` /
`unfollowGuild`. It returns the mutation promise, so a refusal rejects and FeedStream reverts its optimistic state.
It sits here only because Lane G's ownership did not include a shared directory; the natural home is
`src/lib/feed/` or `src/components/feed/`, and moving it is a one-import change at the join.
