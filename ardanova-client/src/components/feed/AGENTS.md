---
type: reference
---

# components/feed — the polymorphic feed kit (SR-03 Phase 3, Lane F)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1-§5. Server side:
`ArdaNova.Application/Feed/AGENTS.md`. The field-note files (`field-note-*`, `project-field-notes`) predate the
feed and are unchanged.

## Component map

| Export                                      | File                  | Role                                                                                                            |
| ------------------------------------------- | --------------------- | --------------------------------------------------------------------------------------------------------------- |
| `FeedStream`                                | `feed-stream.tsx`     | `feed.getPage` infinite query for one `scope`, sentinel + "Load more", end row, optimistic Follow               |
| `FeedCard`                                  | `feed-card.tsx`       | One card: author/time/subject chip, body, attachment, tag chips, reason, counters, one action                   |
| `CardAttachment`                            | `card-attachment.tsx` | Renderer half of `lib/feed/registry.ts`: help state chip, funding mark, event time, launch summary              |
| `InlineReply`                               | `inline-reply.tsx`    | Mounted only while open: newest reply (at most one) + composer. Closed cards make no reply reads                |
| `LikeToggle`, `LikeCount`                   | `like-toggle.tsx`     | The quiet approval toggle beside the counters (§Like toggle); the bare count where there is no toggle           |
| `SubjectChip`                               | `subject-chip.tsx`    | Project/guild link + Follow toggle (handler supplied by the mounting lane)                                      |
| `ColdStartRow`                              | `cold-start-row.tsx`  | Where `nextCursor` is null: sector chips passed in + "Start a project"; empty platform = one line + same action |
| `HashtagText`, `HashtagChip`, `HashtagList` | `../hashtags/`        | `#tag` → `/tag/[tag]` links; see §Hashtags                                                                      |

Pure logic lives in `src/lib/feed/` (`primaryActionFor`, `FEED_CARD_REGISTRY`, `reasonFragment`, `feedRoutes`,
`mergeFeedPages`, `sectorsSeen`) so pages can reuse it without React.

## One action

Every card renders exactly one primary action (`data-primary-action`; the render test proves the count and that
the accessible name contains the visible label, WCAG 2.5.3). `primaryActionFor` reads only server-computed facts:
`postType`, `viewer.{isMember,isFollowing,canClaim}`, `task`, and the card's links. It never reads reason text.
A help ask may add one secondary "Message" (a draft DM; never on your own or a finished ask).

| Card                                                                                          | Action                                                                                                                                                                                                                       |
| --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| POST, PROJECT*UPDATE, GUILD*\*, MEMBER_JOINED, PROPOSAL, SHOP_ITEM, PROJECT_LAUNCHED, unknown | Reply (inline)                                                                                                                                                                                                               |
| TASK_COMPLETED, MILESTONE                                                                     | Open board → `/projects/{slug}/board`                                                                                                                                                                                        |
| FUNDING_PROGRESS                                                                              | Back this → `/projects/{slug}/invest`                                                                                                                                                                                        |
| OPPORTUNITY_OPENED / \_UPDATE                                                                 | Apply → `/opportunities/{slug ?? id}`                                                                                                                                                                                        |
| EVENT_SCHEDULED                                                                               | See events → `/events`                                                                                                                                                                                                       |
| DISCUSSION                                                                                    | Reply → native thread (`?tab=comments`, or the opportunity); the API refuses replies on the card                                                                                                                             |
| HELP_WANTED, first row wins                                                                   | no task facts → Reply · DONE → Open board · PICKED_UP → Reply · `canClaim` → **Pick up task** → `/tasks/{id}` (paid or not: the task page makes the one readiness read) · otherwise → Reply. No Apply row (plan Revision 2). |

A missing fact (no project, no opportunity) falls back to the inline Reply rather than a broken link.

## Tolerant reader

The API ships new PostTypes and reason kinds before this client knows them (`src/lib/AGENTS.md` §Tolerant enum
reads). `FEED_CARD_REGISTRY` is a `satisfies Record<KnownFeedPostType, FeedCardKind>`, so a type added to the
contract is a compile error until it is mapped; at runtime an unmapped type renders as a plain POST. Unknown reason
kinds are the only place the server's `text` is shown.

## Reasons

One fragment per card, rendered from `kind` + `aboutId` with names taken from the card itself ("Following Barn
raising", "You're on P", "New in Construction", "#carpentry"). "Your activity" renders nothing: the name on the card
already says it. On a project/guild/tag stream a reason naming that same subject is dropped; a sector match on a
tag page stays, because it explains why an untagged card is there.

## Hashtags

`hashtag-tokens.ts` mirrors `Hashtags.cs`: same look-behind (no mid-word, `/#anchor`, `&#39;`, `##x`), same 2-40
class with `\p{M}`, same ends-trim and lowercase, same 10-per-post cap. It matches the raw copy (the server NFKCs
first), so the writer's characters render exactly as typed. A card shows tag chips only for tags its visible copy
does not already link inline.

## FeedStream

- **Paging contract (Feed/AGENTS.md §Ranking, MED-C):** keep fetching while `nextCursor` is non-null, even after a
  short or empty page; only `null` ends it. The sentinel never unmounts, so a short page leaves it in view and the
  effect fires again when the page lands.
- **Two guards, each pinned by a test that goes red without it:** the effect depends on `pagesLoaded`, because a
  fast answer can land before any render commits `isFetching: true`, so `shouldLoadMore` never changes and the
  scroll would stall (zero-latency edge). `!isFetchNextPageError` stops a failed page from re-requesting itself
  while the sentinel stays in view (5 ms edge, so the in-flight render commits). `!isFetching` also keeps
  auto-paging from cancelling a background refetch, which `fetchNextPage` does by default.
- **Dedupe:** `mergeFeedPages` keeps the first copy of each id; `leadingCards` (a just-published post) come first
  and the stream's later copy is skipped.
- **Follow:** optimistic per subject key, so every card of one project flips together; a rejected handler reverts
  and toasts `describeMutationError`. Team members never see the toggle.
- **ARIA feed pattern:** `role="feed"`, `aria-busy`, articles with `aria-posinset`/`aria-setsize` (-1 while more
  may load), Page Down / Page Up between articles. "Load more" stays visible as the non-observer fallback.

## Counters

Raw likes and replies, small, mono, only when non-zero. Ranking ignores the raw numbers (distinct people only), so
they are never styled or ordered as a quality signal. Share counts are not shown: they are inflatable by one account
(Feed/AGENTS.md §Deferred) and showing them is that note's trigger.

## Like toggle

Owner default SR-03-D3 (DECISIONS-LEDGER): distinct non-author likes are the ranker's main quality signal, so
removing likes from the main surface would starve ranking. The one-action rule governs the primary CTA, not a passive
approval, so a card carries a quiet "Like" toggle beside its counters and never a `data-primary-action`.

- **Who sees it:** a signed-in reader on someone else's card. The author (whose like the ranker ignores) and a
  signed-out reader (the procedure is protected) see only the count.
- **State:** `aria-pressed`; the label stays "Like" so the accessible name keeps its visible label (WCAG 2.5.3).
- **Optimistic, then the server wins:** the tap flips state and count at once; success replaces the count with the
  API's `likesCount` (others' likes included); failure restores both and toasts `describeMutationError`. One request
  at a time, because a like and an unlike in flight together could land in either order. The API's like/unlike are
  idempotent, so a replayed tap never double-counts.
- **Proof:** `like-toggle.test.tsx` drives a real tRPC client through the real `postRouter` and API client in-process;
  only the .NET HTTP edge is faked.

## Inputs (what mounting lanes pass) and asks

- **G (Home), H (project page):** `<FeedStream scope viewerId onToggleFollow leadingCards renderEnd? />`.
  `onToggleFollow(subject, follow)` returns a promise; reject to revert. H passes the published post mapped to a
  card in `leadingCards`; a scoped stream ends silently unless `renderEnd` is given.
- **Contract (joined at wave 1):** `FeedKitCard` is exactly the contract's `FeedCard`, and `KnownFeedPostType` is
  exactly `FeedPostType` (HELP_WANTED is in `FEED_POST_TYPES`). HELP_WANTED cards carry
  `task: {id, state: "OPEN"|"PICKED_UP"|"DONE", assigneeName, paid}`; the opportunity link carries `slug` (the page
  resolves by slug; an id-only Apply is the older-API fallback). `feedCardSchema` is a plain `z.object`, which strips
  unknown keys: a new card field goes in the contract, not just the DTO.
- **Router name:** the live procedure is `feed.getPage`.

## Deferred

- The card has no "you liked this" fact (`FeedViewerRelationDto` carries none), so the like toggle starts unpressed;
  a reader who liked earlier sees it unpressed until a tap reconciles (the API's like is idempotent, so no double
  count). Revisit by adding `viewer.liked` to the feed read (one `PostLike` lookup per page) when readers report it.
- Reply authors other than you and the asker show no name: `PostComment` carries only `authorId`. Revisit when the
  comment DTO carries a display name.
- No virtualisation; plan §10 trigger: sessions past ~200 cards.
