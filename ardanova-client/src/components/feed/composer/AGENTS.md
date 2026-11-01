---
type: reference
---

# components/feed/composer: the authoring studio (SR-03 Phase 3, Lane C)

Plan: `conductor/tracks/mvp-readiness-loop_20261026/SR-03-PHASE3-PLAN.md` §1 (composer on top), §2 (who writes what),
§5 (copy), Revisions 1-2 (409 → "Edit your ask"; "Who can help?" is HELP_WANTED), §8.7 (the composer is the one update
writer). The public component is `FieldNoteComposer` in `../field-note-composer.tsx`; this directory holds its parts.

| File                       | Role                                                                                        |
| -------------------------- | ------------------------------------------------------------------------------------------- |
| `prompts.ts`               | The three prompts, `postTypeFor`, `createInputFor` (the exact `post.create` input, or null) |
| `prompt-tabs.tsx`          | The prompts as a radio group drawn as tabs on the sheet's top edge                          |
| `hashtag-textarea.tsx`     | The writing surface + inline `#` listbox                                                    |
| `hashtag-suggestions.ts`   | Pure: the `#fragment` at the caret, ranking, insertion                                      |
| `published-card.ts`        | Pure: the created/edited PostDto as a contract `FeedCard`                                   |
| `help-conflict.ts`         | Pure: `error.data.helpWantedExistingPostId` from a 409                                      |
| `use-composer-subjects.ts` | Attach lists (projects and guilds I belong to) and the help task list                       |
| `composer-select.tsx`      | Native select with the mono ledger label                                                    |

## Props

```ts
interface FieldNoteComposerProps {
  projectId?: string; // fixes the note to this project (no attach picker); the pre-Phase-3 prop
  guildId?: string; // fixes the note to this guild (no picker, no "Who can help?")
  mode?: "note" | "help"; // "help" opens on "Who can help?"; default: "help" when taskId is given, else "note"
  taskId?: string; // fixes the task an ask names (work-item modal)
  author?: { id: string; name?: string | null; image?: string | null }; // the session user
  onPosted?: () => void; // after every publish or ask edit; the pre-Phase-3 prop
  onPublished?: (card: FeedCard) => void; // the post as a contract FeedCard (same id after an ask edit)
  className?: string;
}
```

- **G (Home):** `<FieldNoteComposer author={me} onPublished={(card) => setLeading((cards) => [card, ...cards])} />`, then
  `<FeedStream leadingCards={leading} />`.
- **H (project page):** same with `projectId`.
- **W (work-item modal, "Ask for help"):** `projectId` + `taskId` (mode defaults to help). Pass `projectId` as well:
  the API derives the project from the task either way, but the picker needs it to show the task's title.
- Without `author` the card's name renders as "Someone" until the next feed read, and "Edit your ask" cannot tell whose
  ask it found (the API's 403 on save is the backstop).

## Prompt rule

| Prompt                          | Attached to | Writes           | Carries                |
| ------------------------------- | ----------- | ---------------- | ---------------------- |
| "What moved?" / "What's stuck?" | nothing     | `POST`           |                        |
| "What moved?" / "What's stuck?" | a project   | `PROJECT_UPDATE` | `projectId`            |
| "What moved?" / "What's stuck?" | a guild     | `GUILD_ACTIVITY` | `guildId`              |
| "Who can help?"                 | a project   | `HELP_WANTED`    | `projectId` + `taskId` |

- The two note prompts write the same thing; the prompt is a frame for the words, not metadata (nothing is stored).
- Help mode drops a guild attachment (`attachmentFor`) instead of sending it: an ask lives on a project's task.
- `createInputFor` returns null for no words, or for an ask with no task, so the button is off and a keyboard submit
  (Ctrl/Cmd+Enter) sends nothing. A `taskId` is only ever sent with HELP_WANTED (the API refuses it elsewhere).
- Tasks offered: the attached project's tasks with no assignee and not COMPLETED (`task.getAll`, 100 max). BLOCKED
  counts: a stuck task is the likeliest ask.

## Edit your ask

The API's 409 (`Post.taskId [unique]`) carries the existing post id → `error.data.helpWantedExistingPostId`. The
composer shows "Already asked." + **Edit your ask** (`data-existing-post-id`) instead of an error. There is no post
page, so the edit happens in place: one `post.getById`, the ask's words load into the sheet ("Your ask"), **Save ask**
is `post.update`, and **Cancel** restores the draft that was there. If `author` is given and the ask is someone
else's, it shows "Asked by someone else." + **Open task** instead. The notice belongs to its task: picking another task
retires it. `onPublished` fires after a save with the same id, so `mergeFeedPages` shows the edit once, at the top.

## Hashtag suggestions

- Source: `taxonomy.getAll` (SECTOR and SKILL terms; PROJECT_TYPE is project metadata, not a post tag), read the first
  time a `#` is typed, then cached for the session.
- Spelling: `sectorTag(slug)` (FOOD_BEVERAGE → `food-beverage`), one of the spellings `FeedTopicCanon` resolves.
- Fragment detection mirrors `hashtag-tokens.ts` / `Hashtags.cs` (no mid-word, `/#`, `&#`, `##`), so a suggestion only
  appears where the server would store a tag.
- Keyboard: ArrowUp/Down move, Enter/Tab accept, Escape closes until a new `#`. Focus stays in the textarea
  (`aria-activedescendant` + `aria-autocomplete="list"`); a polite live region says how many tags were found.

## Published card

Built only from facts the write returned plus what the composer already knows (picker lists; otherwise one
`project.getById` / `guild.getById` read). `viewer.canClaim` is always false: the claim rule lives once, server-side
(TaskClaimRule), and the next feed read replaces the local card. A HELP_WANTED card's task is `OPEN` with `paid` from
the picked task's `equityReward` (omitted when unknown).

## Audience

`audienceOptionsFor(context)` offers only audiences `PostVisibilityPolicy.HasCoherentAudience` accepts; an ask is never
private. Inside a project the scope line is the existing `audienceDescription` verbatim (consent wording untouched);
the new contexts (nothing attached, a guild) read "Anyone can read it." for PUBLIC.

## Attach list

Every project and guild the writer belongs to: `project.getMyMemberships` → .NET `GET /api/projects/mine` (created
or a `ProjectMember` row, drafts of their own team included, by title) and `guild.getMyMemberships` → `GET
/api/guilds/mine` (owned or a `GuildMember` row, by name). Both routes are actor-only: .NET reads the signed actor
and there is no userId parameter. These match who the API lets write into a project or guild, so the picker never
offers a subject the post would be refused on. Proof: `MembershipListFlowTests` (real Postgres) and
`server/api/routers/memberships.test.ts`.

## Reads

| When                                                     | Reads                                        |
| -------------------------------------------------------- | -------------------------------------------- |
| Mounted without a fixed attachment                       | `project.getMyMemberships`, `guild.getMyMemberships` |
| "Who can help?" with a project                           | `task.getAll({ projectId, limit: 100 })`     |
| First `#`                                                | `taxonomy.getAll`                            |
| Publish with `onPublished`, subject not in a picker list | `project.getById` / `guild.getById`          |

The project page (fixed `projectId`, no `onPublished`) makes none of these until "Who can help?" or `#`.

## Deferred

- **Task list capped at 100** per project (one `task.getAll` page). Trigger: a project with >100 tasks.
- **Escape inside a Radix dialog** closes the dialog, not just the suggestions: Radix listens on `document` in the
  capture phase, before React. Trigger: Lane W mounts the composer in the modal → its `onEscapeKeyDown` can
  `preventDefault()` while the textarea has `aria-activedescendant`.
- **No headline field.** The old composer's optional title is gone (the prompt frames the words); cards still render a
  title when a post has one.
