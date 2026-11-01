---
type: review
track: mvp-readiness-loop_20261026
date: 2026-10-31
lane: copy
base: 9a40e43
status: approved-with-changes-applied
---

# Copy strip — 2026-10-31

**Owner direction.** Strip on-screen copy to "just enough". Use Apple's restraint with Žižek's subtlety, keep branded text off the working surfaces, and let the platform speak for itself.

**Standard applied.** See `documentation/LINGUISTIC_GUIDE.md` §Just enough.

- Never narrate what the UI shows. Headings are four words or fewer.
- A supporting line is optional, eight words or fewer, and must add something the UI doesn't show.
- Buttons are verb + noun, three words or fewer. Empty states get one line and one action.
- At most one Žižek-register line per page.
- Money, verification, consent, and safety copy is exempt and left verbatim.

**Scope.** All of `ardanova-client/src/app/**` and `src/components/**`, outside the hard exclusions, plus the root metadata, the two voice docs, and `src/app/AGENTS.md`. 68 files changed, +528 / −1275 lines.

**How to read the tables.** Line numbers point at the *before* file at base `9a40e43`. Eyebrows render uppercase via CSS, so they are shown uppercase here as the reader sees them. *(deleted)* means the element was removed and its parent's spacing fixed. P8-17 marks the decisions-ledger item this pass closes for the two client lines.

**Žižek-register lines allowed: one.** The landing page's closing heading went from `Give the next iteration a place to happen.` to `Problems are not in short supply.`, set above the button [Start with a problem]. It applies market language to the platform's raw material, problems as abundant supply. The line reads as a shrug, and the button beside it turns the shrug into agency. It isn't a pun and it isn't at the reader's expense, which makes it the only line in the pass that rewards a re-read.

---

## Changed and deleted strings, by page

### App shell — top bar (every page)

| File:line | Before | After |
| --- | --- | --- |
| `components/app-topbar.tsx:109-112` | Eyebrow `{context.artifact}` (e.g. `PROJECT WORKSPACE`) + subtitle `{context.title}` | *(slot removed)* |
| `components/app-topbar.tsx:118` | `ArdaNova member` (name fallback) | `Member` |
| `components/app-topbar.tsx:120-122` | `MEMBER` (role line under every name) | *(deleted)* |
| `lib/nova-context.ts:47` | `Draft and shape a project story` | *(deleted — field had no remaining reader)* |
| `lib/nova-context.ts:65` | `Turn an idea into a reviewable project brief` | *(deleted)* |
| `lib/nova-context.ts:83` | `Make the project easier to understand and review` | *(deleted)* |
| `lib/nova-context.ts:102` | `Clarify the work and its evidence` | *(deleted)* |
| `lib/nova-context.ts:121` | `Make expectations and fit legible` | *(deleted)* |
| `lib/nova-context.ts:142` | `Support a fair, traceable community decision` | *(deleted)* |
| `lib/nova-context.ts:161` | `Explain records without overstating value or rights` | *(deleted)* |
| `lib/nova-context.ts:183` | `Prepare a message without speaking for you` | *(deleted)* |
| `lib/nova-context.ts:201` | `Turn visible context into a reviewable next step` | *(deleted)* |

### Root metadata, error and 404 pages

| File:line | Before | After |
| --- | --- | --- |
| `app/layout.tsx:13` | title `ArdaNova — Shared project workspace` | `ArdaNova` |
| `app/layout.tsx:17` | `Plan shared work, record decisions, and reconcile contributor rights in one reviewable project workspace.` | *(unchanged — restored verbatim in review round 2: it carries the rights qualifier)* |
| `app/layout.tsx:33,35` | OG `ArdaNova — Shared project workspace` / `Where people who find a problem fund it, build it, and share in what it becomes.` | `ArdaNova` / `Find a problem. Fund it. Build it. Share in what it becomes, on agreed terms.` |
| `app/layout.tsx:41,43` | Twitter `ArdaNova — Shared project workspace` / `A reviewable workspace for shared work, decisions, and contributor rights.` | `ArdaNova` / `Find a problem. Fund it. Build it. Share in what it becomes, on agreed terms.` |
| `app/error.tsx:23` | `ERROR / PAGE FAILED TO LOAD` | `ERROR` |
| `app/error.tsx:24` | `This page could not be shown.` | `This page didn't load.` |
| `app/error.tsx:28` | `Something went wrong while loading it.` (+ safety sentence) | *(first sentence deleted; safety sentence kept verbatim)* |
| `app/error.tsx:44` | `Go to your dashboard` | `Open dashboard` |
| `app/not-found.tsx:13` | `404 / NOT FOUND` | `404` |
| `app/not-found.tsx:14` | `There is no page at this address.` | `Page not found.` |
| `app/not-found.tsx:17` | `The link may be mistyped, or the record it pointed to was moved or removed.` (+ safety sentence) | *(deleted; `Nothing you have saved has changed.` kept)* |
| `app/not-found.tsx:24,27` | `Go to your dashboard` / `Back to the home page` | `Open dashboard` / `Go home` |
| `app/global-error.tsx:27` | `ERROR / ARDANOVA IS UNAVAILABLE` | `ERROR` |
| `app/global-error.tsx:28` | `ArdaNova could not start this page.` | `This page didn't load.` |
| `app/global-error.tsx:32` | `The application stopped before this page could load.` (+ anti-double-submit sentence) | *(deleted; safety sentence kept verbatim)* |
| `app/global-error.tsx:50` | `Back to the home page` | `Go home` |

### Sign-in and auth error

| File:line | Before | After |
| --- | --- | --- |
| `app/auth/signin/page.tsx:18` | meta `Sign in to your ArdaNova project workspace.` | `Sign in to ArdaNova.` |
| `app/auth/signin/page.tsx:72` | `YOUR SHARED OPERATING VIEW` | *(deleted)* |
| `app/auth/signin/page.tsx:75` | `Bring the project back into focus.` | `Back to the work.` |
| `app/auth/signin/page.tsx:78` | `Return to the plans, decisions, contributions, and review states that keep shared work understandable.` | *(deleted)* |
| `app/auth/signin/page.tsx:139` | `NEW MEMBER ENTRY` / `SECURE WORKSPACE ENTRY` | *(deleted — the Sign in / Create account tabs sit directly above)* |
| `app/auth/signin/page.tsx:142` | `Start your workspace.` | `Welcome.` (sign-in keeps `Welcome back.`) |
| `app/auth/signin/page.tsx:146` | `Continue with Google to create your ArdaNova profile.` (+ consent sentence) | *(first sentence deleted — the button says it; consent sentence kept verbatim)* |
| `app/auth/signin/page.tsx:147` | `Continue with the Google account connected to your ArdaNova workspace.` | `Use the Google account you joined with.` |
| `app/auth/signin/page.tsx:199` | `You'll continue to Google in a new authentication step, then return to this ArdaNova workspace. Creating an account and signing in use the same secure provider flow.` | *(deleted)* |
| `app/auth/signin/page.tsx:241` | `Return to the public site` | `Go home` |
| `app/auth/error/page.tsx:39-41` | `Pause. / Keep the work safe.` | `Pause.` |

### Landing — `/` (logged-out; one brand line kept in the hero)

| File:line | Before | After |
| --- | --- | --- |
| `app/page.tsx:161` | `SOCIAL LAYER FOR DOING` | *(deleted — eyebrow stack)* |
| `app/page.tsx:163` | `BUILD TOGETHER / GOVERN TOGETHER` | *(deleted)* |
| `app/page.tsx:169` | Badge `Social media for doing—not doom-scrolling` | *(deleted)* |
| `app/page.tsx:180-183` | `Not an endless feed. ArdaNova is where people who find a problem fund it, build it, and share in what it becomes—with decisions, contributions, and agreed terms visible for review.` | `People who find a problem fund it, build it, and share in what it becomes, on agreed terms.` **(P8-17)** — the rights qualifier travels with the claim |
| `app/page.tsx:229` | `01 / THE SOLUTIONARY LOOP` | `01 / METHOD` (matches the nav label; the band above already names the loop) |
| `app/page.tsx:236-239` | `Start with lived context, shape a proposal people can inspect, and keep learning with those affected. Every iteration can move forward, return to discovery, or end in a documented stop.` | *(deleted — the three cards say it)* |
| `app/page.tsx:27` | `Begin with affected people, lived context, evidence, constraints, and open questions—not whatever happens to be trending.` | `Start with the people it affects.` |
| `app/page.tsx:34` | `Shape a reviewable proposal with visible assumptions, roles, milestones, terms, rights, and measures of progress.` | `Make a plan others can check.` |
| `app/page.tsx:41` | `Contribute, test, compare what happened with what was intended, and revise—or stop—with the people affected.` | `Build, test, revise. Or stop.` |
| `app/page.tsx:281` | `02 / RIGHTS WITHOUT HAND-WAVING` | `02 / RIGHTS` |
| `app/page.tsx:284` | `Two records. One understandable agreement.` | `Two records. One agreement.` |
| `app/page.tsx:342` | `NOVA / OPTIONAL DRAFTING HELP` | `NOVA` |
| `app/page.tsx:345` | `From rough notes to a brief people can act on.` | `From notes to a brief.` |
| `app/page.tsx:351-353` | `Nova asks hard questions about a project idea and drafts a brief from your answers. You decide what stays, what changes, and whether it becomes a project.` | `Nova asks the hard questions and drafts the brief. You decide what stays.` |
| `app/page.tsx:432` | `03 / TECHNOLOGY FOR OWNERSHIP, NOT SURVEILLANCE` | `03 / TRUST` |
| `app/page.tsx:439-441` | `A humane system explains what it uses and who can act. It supports explicit, documented rights without treating human activity as raw material for attention extraction.` | *(deleted — contradicts the ranked-feed decision)* |
| `app/page.tsx:50` | `Show what information is being used, why it is needed, what is excluded, and who can act on it.` | `What is used, why, and who can act on it.` |
| `app/page.tsx:58-60` | `Progress over engagement` / `Surface open questions, contributions, decisions, and next moves—not popularity or time spent scrolling.` | `Decisions stay recorded` / `Proposals, votes, and outcomes stay on the record.` (old cue was false under SR-03-D2) |
| `app/page.tsx:474` | `DISCOVER. DEFINE. ITERATE.` | *(deleted)* |
| `app/page.tsx:477` | `Give the next iteration a place to happen.` | `Problems are not in short supply.` **(Žižek line)** |
| `app/page.tsx:499-501` | `A place for people who find a problem to fund it, build it, and share in what it becomes. Build together, govern together, prosper together.` | *(deleted — wordmark stands alone)* |
| `app/page.tsx:559` | `© 2026 ArdaNova / Discover · define · iterate` | `© 2026 ArdaNova` |

### Dashboard — `/dashboard`

| File:line | Before | After |
| --- | --- | --- |
| `app/dashboard/page.tsx:259` | `WORKSPACE / {firstName}` | *(deleted — folded into the heading)* |
| `app/dashboard/page.tsx:265` | `Social software for doing.` | `Hello, {firstName}.` (or `Hello.`) |
| `app/dashboard/page.tsx:268-270` | `Discover a problem. Define a solution with the people affected. Iterate from evidence. Share in what you build, on terms everyone can read.` | *(deleted)* |
| `app/dashboard/page.tsx:277` | `AGENCY, NOT ATTENTION` | *(deleted)* |
| `app/dashboard/page.tsx:280` | `No engagement score. No infinite feed. No manufactured urgency.` | *(deleted)* **(P8-17)** |
| `app/dashboard/page.tsx:302` | `Start a project record` | `Start a project` |
| `app/dashboard/page.tsx:316` | `CORE LOOP` | *(deleted)* |
| `app/dashboard/page.tsx:319` | `Move one useful thing forward.` | `Move one thing forward.` |
| `app/dashboard/page.tsx:323-324` | `Each list is intentionally short. Choose a record, take an action, then return when the work changes.` | *(deleted)* |
| `app/dashboard/page.tsx:343-344` | `Start with lived experience and evidence. Inspect what is already being tried before proposing another answer.` | *(deleted)* |
| `app/dashboard/page.tsx:349` | `FEATURED PROJECT RECORDS` | `FEATURED PROJECTS` |
| `app/dashboard/page.tsx:352` | `Loading projects to inspect…` | `Loading projects…` |
| `app/dashboard/page.tsx:354` | `Project records are unavailable right now.` | `Projects are unavailable right now.` |
| `app/dashboard/page.tsx:357` | `No featured project records are available.` | `No featured projects yet.` |
| `app/dashboard/page.tsx:401-402` | `Name the people, evidence, scope, and next decision. Keep assumptions visible so collaborators can challenge them.` | *(deleted)* |
| `app/dashboard/page.tsx:407` | `YOUR PROJECT RECORDS` | `YOUR PROJECTS` |
| `app/dashboard/page.tsx:410,412` | `Loading your project records…` / `Your project records are unavailable right now.` | `Loading your projects…` / `Your projects are unavailable right now.` |
| `app/dashboard/page.tsx:414` | `You have not started a project yet. Begin with a problem you have seen.` | `No projects yet.` (the column's `Create project` button is the action) |
| `app/dashboard/page.tsx:451` | `Complete a bounded task, review the evidence, and change the plan.` (+ award sentence) | *(first sentence deleted; `A task state does not imply an award has been reconciled.` kept verbatim and moved directly above `View your tasks`)* |
| `app/dashboard/page.tsx:457` | `WORK ASSIGNED TO YOU` | `ASSIGNED TO YOU` |
| `app/dashboard/page.tsx:476` | `No task records are currently assigned to you.` | `Nothing assigned to you.` |
| `app/dashboard/page.tsx:500` | `Open assigned work` | `View your tasks` |
| `app/dashboard/page.tsx:511` | `COORDINATE` | *(deleted)* |
| `app/dashboard/page.tsx:514` | `Review windows, not attention traps.` | `Proposals and events` (open-for-voting proposals are not "coming up") |
| `app/dashboard/page.tsx:518-519` | `Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as attendance or consent.` | *(unchanged — restored verbatim in review round 2: consent and proposal-state copy)* |
| `app/dashboard/page.tsx:528` | `GOVERNANCE` (eyebrow over `Active proposals`) | *(deleted)* |
| `app/dashboard/page.tsx:541` | `No active proposals are recorded.` | `No active proposals.` |
| `app/dashboard/page.tsx:579` | `WORKING SESSIONS` (eyebrow over `Upcoming events`) | *(deleted)* |
| `app/dashboard/page.tsx:595` | `No upcoming events are scheduled.` | `No upcoming events.` |
| `app/dashboard/page.tsx:629` | `Open event calendar` | `Open calendar` |
| `app/dashboard/page.tsx:645-655` | `That is the whole snapshot.` / `Finite by design. Choose one useful action, or close the tab and return to the work when something changes.` / `End of list` | *(whole aside deleted — false under the infinite-feed decision)* |

### Projects — `/projects` (the owner's archetype)

| File:line | Before | After |
| --- | --- | --- |
| `app/projects/page.tsx:311` | `DISCOVER A PROBLEM / DEFINE A SOLUTION / ITERATE` | *(deleted)* |
| `app/projects/page.tsx:314` | `Projects for doing` | `Projects` |
| `app/projects/page.tsx:317-318` | `Open a project to understand the problem, inspect the proposed solution, and find the work that moves it forward.` | *(deleted)* |
| `app/projects/page.tsx:392` | `Filters run against the full project catalog, not just this page.` | `Filters search every project, not just this page.` |
| `app/projects/page.tsx:233` | `Current page` | `This page` |
| `app/projects/page.tsx:237` | `Projects on this page` | `Projects` (card title now says the scope) |
| `app/projects/page.tsx:264-265` | `These totals summarize page {p} of {n} — {N} projects match the current filters.` / `These totals summarize this page only.` | `Totals cover page {p} of {n}. {N} projects match.` / `Totals cover this page only.` (page-only caveat kept) |
| `app/projects/page.tsx:274` | `Recently updated in this view` | `Recently updated` |
| `app/projects/page.tsx:283-284` | `Not available while projects can't be loaded.` / `No loaded records.` | `Unavailable right now.` / `None.` |
| `app/projects/page.tsx:453` | `Loading project records...` | `Loading projects…` |
| `app/projects/page.tsx:461` | `PROJECT QUERY FAILED` | *(deleted)* |
| `app/projects/page.tsx:464` | `The current project page is unavailable` | `Projects didn't load.` |
| `app/projects/page.tsx:467` | `No sample records are substituted when the API fails.` | *(deleted)* |
| `app/projects/page.tsx:487` | `No matches for these filters` | `No matches.` |
| `app/projects/page.tsx:488` | `No projects returned` | `No projects yet.` |
| `app/projects/page.tsx:492` | `Change or clear the filters to search a wider set of projects.` | *(deleted — `Clear filters` is the action)* |
| `app/projects/page.tsx:493` | `No one has posted a project yet. If you have seen a problem worth solving, define it here so others can help fund and build it.` | *(deleted — `Define a project` is the action)* |
| `app/projects/page.tsx:510-513` | `None of the {n} projects on this page are funded or complete / in this view. Try another tab or page.` | `Nothing on this page is funded or complete.` / `Nothing on this page.` |
| `app/projects/page.tsx:525-528` | `Showing {n} on this page · {N} total match these filters` | `{n} on this page · {N} total` |
| `app/projects/page.tsx:566` | `Creator name unavailable` | `Unknown creator` |
| `app/projects/page.tsx:589` | `Project creator / {time}` | `{time}` |
| `app/projects/page.tsx:623` | `PROPOSED SOLUTION` | `SOLUTION` |
| `app/projects/page.tsx:658` | `Votes recorded` | `Votes` |

### Opportunities — `/opportunities`

| File:line | Before | After |
| --- | --- | --- |
| `app/opportunities/page.tsx:315` | `Visible results` | `This view` |
| `app/opportunities/page.tsx:321,329,337,345` | `Opportunities in this view` / `Bounties in this view` / `Task-generated in view` / `Applications in this view` | `Opportunities` / `Bounties` / `From tasks` / `Applications` |
| `app/opportunities/page.tsx:359` | `Most active in this view` | `Most active` |
| `app/opportunities/page.tsx:433` | `Counts reflect the current filtered view.` | *(deleted)* |
| `app/opportunities/page.tsx:448` | `Post Job` | `Post opportunity` (matches the empty-state button) |
| `app/opportunities/page.tsx:774` | `Opportunities could not be loaded.` | `Opportunities didn't load.` |
| `app/opportunities/page.tsx:777` | `No opportunity counts or results are shown.` | *(deleted)* |
| `app/opportunities/page.tsx:784-789` | `No opportunities found` + `No opportunities match the current view.` / `No one has posted an opportunity yet. Post the work your project needs so the people who can do it can find it.` | `No matches.` / `No opportunities yet.` (one line + the existing action) |
| `app/opportunities/page.tsx:846` | `posted a new opportunity` (under every card's byline) | *(deleted)* |

### Guilds — `/guilds`

| File:line | Before | After |
| --- | --- | --- |
| `app/guilds/page.tsx:271` | `Visible results` | `This view` |
| `app/guilds/page.tsx:276,284,292,300` | `Guilds in this view` / `Verified in this view` / `Projects across visible guilds` / `Reviews across visible guilds` | `Guilds` / `Verified` / `Projects` / `Reviews` |
| `app/guilds/page.tsx:314` | `Highest rated in this view` | `Highest rated` |
| `app/guilds/page.tsx:385` | `Counts reflect the current filtered view.` | *(deleted)* |
| `app/guilds/page.tsx:400,722` | `Create Guild` | `Create guild` |
| `app/guilds/page.tsx:694-697` | `Guilds could not be loaded.` / `No guild counts or results are shown.` | `Guilds didn't load.` |
| `app/guilds/page.tsx:705-710` | `No guilds found` + `No guilds match the current view.` / `No guild records were returned.` | `No matches.` / `No guilds yet.` |

### People — `/people`

| File:line | Before | After |
| --- | --- | --- |
| `app/people/page.tsx:328` | `Visible results` | `This view` |
| `app/people/page.tsx:333,341,349,357` | `People in this view` / `Verified people in view` / `Guilds in this view` / `Verified guilds in view` | `People` / `Verified people` / `Guilds` / `Verified guilds` |
| `app/people/page.tsx:371` | `Highest rated in this view` | `Highest rated` |
| `app/people/page.tsx:444` | `Counts reflect the current filtered view.` | *(deleted)* |
| `app/people/page.tsx` (2×) | `Create Guild` | `Create guild` |
| `app/people/page.tsx:669-672` | `Guilds could not be loaded.` / `No guild counts or results are shown.` | `Guilds didn't load.` |
| `app/people/page.tsx:680-685` | `No guilds found` + `No guilds match the current view.` / `No guild records were returned.` | `No matches.` / `No guilds yet.` |
| `app/people/page.tsx:821-824` | `People could not be loaded.` / `No people counts or profiles are shown.` | `People didn't load.` |
| `app/people/page.tsx:832-837` | `No people found` + `No people match the current view.` / `No people records were returned.` | `No matches.` / `No people yet.` |

### Governance — `/governance`

| File:line | Before | After |
| --- | --- | --- |
| `app/governance/page.tsx:400` | `Visible results` | `This view` |
| `app/governance/page.tsx:405,413,421,429` | `Proposals in this view` / `Active in this view` / `Passed in this view` / `Votes recorded in this view` | `Proposals` / `Active` / `Passed` / `Votes` |
| `app/governance/page.tsx:443` | `Active in this view` (card title) | `Voting now` |
| `app/governance/page.tsx:507` | `Counts reflect the current filtered view.` | *(deleted)* |
| `app/governance/page.tsx:519` | `Proposals are created within projects` | `Proposals start in projects.` (kept — the only pointer to where creation happens) |
| `app/governance/page.tsx:807-810` | `Governance proposals could not be loaded.` / `No proposal or vote counts are shown.` | `Proposals didn't load.` |
| `app/governance/page.tsx:818-823` | `No proposals found` + `No proposals match the current view.` / `No proposal records were returned.` | `No matches.` / `No proposals yet.` |

### Events — `/events`

| File:line | Before | After |
| --- | --- | --- |
| `app/events/page.tsx:370` | `First events in this page` | `Up next` |
| `app/events/page.tsx:443` | `Create Event` | `Create event` |
| `app/events/page.tsx:725-728` | `Events could not be loaded.` / `No event records or registration actions are shown.` | `Events didn't load.` |
| `app/events/page.tsx:737-740` | `Your event registrations could not be loaded.` / `No registration state is inferred from missing data.` | `Your registrations didn't load.` (register buttons are already hidden on this branch) |
| `app/events/page.tsx:747` | `No events found matching your criteria.` | `No matches.` / `No events yet.` |

### Tasks — `/tasks`

| File:line | Before | After |
| --- | --- | --- |
| `app/tasks/page.tsx:587` | `My Tasks` | `My tasks` |
| `app/tasks/page.tsx:747` | `Tasks could not be loaded. {error}` | `Tasks didn't load. {error}` |
| `app/tasks/page.tsx:754` | `No tasks match the current view.` | `No tasks here.` |

### Chats — `/chats`

| File:line | Before | After |
| --- | --- | --- |
| `app/chats/page.tsx:331,334` | `More conversations could not be loaded.` | `More conversations didn't load.` |
| `app/chats/page.tsx:352,356` | `Earlier messages could not be loaded.` | `Earlier messages didn't load.` |
| `app/chats/page.tsx:471-474` | `Conversations could not be loaded` / `This is a connection or service error, not an empty inbox.` | `Conversations didn't load.` |
| `app/chats/page.tsx:483` | `Retry conversations` | `Try again`, accessible name `Try again to load conversations` |
| `app/chats/page.tsx:496-501` | `No matching conversations` / `No conversations available` + `Change the search or filter to widen the list.` / `Start a direct conversation when you have something concrete to move forward.` | `No matches.` / `No conversations yet.` |
| `app/chats/page.tsx:640` | `A USEFUL CONVERSATION STARTS WITH INTENT` | *(deleted)* |
| `app/chats/page.tsx:646-647` | `Coordinate the next decision, share context, or invite someone into the work.` | *(deleted)* |
| `app/chats/page.tsx:677` | `Conversation could not be loaded` | `Conversation didn't load.` |
| `app/chats/page.tsx:680` | `The conversation service returned an error.` (+ access sentence) | *(deleted; `Access has not been inferred or changed.` kept verbatim)* |
| `app/chats/page.tsx:689` | `Retry conversation` | `Try again`, accessible name `Try again to load this conversation` |
| `app/chats/page.tsx:720` | `Back to conversations` | `Back to chats` |
| `app/chats/page.tsx:697` | `Back` (conversation-error panel, same destination) | `Back to chats` |
| `app/chats/page.tsx:776` | `Retry details` | `Try again`, accessible name `Try again to refresh details` |
| `app/chats/page.tsx:785-786` | `Current conversation details could not be refreshed. The list snapshot may be stale.` | `Details didn't refresh and may be out of date.` |
| `app/chats/page.tsx:863-867` | `Messages could not be loaded` / `This is a service error, not an empty conversation. No message history is shown until the request succeeds.` | `Messages didn't load.` |
| `app/chats/page.tsx:874` | `Retry messages` | `Try again`, accessible name `Try again to load messages` |
| `app/chats/page.tsx:885-889` | `Start with the next useful thing` / `Share the problem, ask a precise question, or name the decision this conversation can move forward.` | `No messages yet.` |

### Profile — `/dashboard/profile`, `/dashboard/profile/[id]`, `/settings/profile`

| File:line | Before | After |
| --- | --- | --- |
| `app/dashboard/profile/page.tsx:259` | `Add a bio so collaborators know what you work on.` | `Add a bio.` |
| `app/dashboard/profile/page.tsx:376,509` | `Your Projects` / `Your Guilds` | `Your projects` / `Your guilds` |
| `app/dashboard/profile/page.tsx:412` | `Project Invitations` | `Invitations` |
| `app/dashboard/profile/page.tsx:382,419,515,559` | `Projects / Invitations / Guilds / Contribution details could not be loaded.` | `Projects / Invitations / Guilds / Contributions didn't load.` |
| `app/dashboard/profile/page.tsx:386,519` | `You haven't joined any projects yet.` / `You haven't joined any guilds yet.` | `No projects yet.` / `No guilds yet.` |
| `app/dashboard/profile/page.tsx:546-547` | `Credentials could not be loaded.` / `You have no credentials yet. They are earned through work in a project or guild.` | `Credentials didn't load.` / `No credentials yet. They're earned through work.` |
| `app/dashboard/profile/[id]/page.tsx:66,169,201` | `This profile could not be loaded` / `Projects could not be loaded.` / `Guilds could not be loaded.` | `This profile didn't load.` / `Projects didn't load.` / `Guilds didn't load.` |
| `app/dashboard/profile/[id]/page.tsx:71` | `Back to People` | `Back to people` |
| `app/settings/profile/page.tsx:19` | `How other members see you: name, bio, links, skills and experience.` (+ verification sentence) | `How other members see you.` (verification sentence kept verbatim) |

### Dashboard tasks — `/dashboard/tasks`

| File:line | Before | After |
| --- | --- | --- |
| `app/dashboard/tasks/page.tsx:567` | `Track your work across all projects` | *(deleted)* |
| `app/dashboard/tasks/page.tsx:805` | `Tasks could not be loaded.` / `Retry` | `Tasks didn't load.` / `Try again` |
| `app/dashboard/tasks/page.tsx:820-826` | `No tasks found` / `Try adjusting your filters or create a task` | `No tasks here.` / link `Create a task` |

### Governance create and detail — `/governance/create`, `/governance/[id]`

| File:line | Before | After |
| --- | --- | --- |
| `app/governance/create/page.tsx:18` | `Back to Governance` | `Back to governance` |
| `app/governance/create/page.tsx:25` | `Create Proposal` | `New proposal` |
| `app/governance/create/page.tsx:28` | `Proposals are created within projects` | `Proposals start in projects.` |
| `app/governance/create/page.tsx:37` | `How to Create a Proposal` | `How to propose` |
| `app/governance/create/page.tsx:42-44` | `To submit a proposal, you need to go to a specific project first. Proposals are tied to projects and can only be created within the project context.` | *(deleted)* |
| `app/governance/create/page.tsx:53-55` | `Browse Projects` / `Find a project you want to create a proposal for` | `Open a project you belong to` (membership is the real prerequisite — the form renders only for members) |
| `app/governance/create/page.tsx:66-69` | `Go to Proposals Tab` / `Navigate to the Proposals tab within the project` | `Open its Proposals tab` |
| `app/governance/create/page.tsx:79-82` | `Create Proposal` / `Click the "New Proposal" button to create your proposal` | `Start a new proposal` |
| `app/governance/create/page.tsx:92-97` | `Why project-level proposals?` / `This ensures that proposals are always contextual and relevant to specific projects, making governance more organized and meaningful.` | *(deleted)* |
| `app/governance/create/page.tsx:109` | `Browse Projects` | `Browse projects` |
| `app/governance/[id]/page.tsx:31` | `Redirecting to project page...` | `Opening the project…` |
| `app/governance/[id]/page.tsx:40` | `This proposal may have been deleted or does not exist.` | *(deleted — heading says `Proposal not found`)* |
| `app/governance/[id]/page.tsx:46` | `Back to Governance` | `Back to governance` |

### Project detail, board, edit — `/projects/[slug]/…`

| File:line | Before | After |
| --- | --- | --- |
| `app/projects/[slug]/page.tsx:179` | `PROJECT UNAVAILABLE` | *(deleted)* |
| `app/projects/[slug]/page.tsx:182` | `We could not open this project` | `This project didn't load.` on a query error / `Project not found` when the record is absent |
| `app/projects/[slug]/page.tsx:185-186` | `The project may not exist, or its API record did not match the current project contract.` | *(deleted)* |
| `app/projects/[slug]/page.tsx:199` | `Creator name unavailable` | `Unknown creator` |
| `app/projects/[slug]/page.tsx:299,303` | `Votes recorded` / `Views recorded` | `Votes` / `Views` |
| `app/projects/[slug]/board/page.tsx:552` | `BOARD COULD NOT BE LOADED` | `Board didn't load.` |
| `app/projects/[slug]/board/page.tsx:582` | `Back to Projects` | `Back to projects` |
| `app/projects/[slug]/board/page.tsx:769` | `No tasks found. Create one to get started.` | `No tasks here.` |
| `app/projects/[slug]/edit/page.tsx:49-50` | `Project not found` (for both cases) + `This project could not be loaded, or it no longer exists.` | `This project didn't load.` on a query error / `Project not found` when absent; sentence deleted |
| `app/projects/[slug]/edit/page.tsx:72` | `Only the project's creator can revise its Basic Info here.` | `Only its creator can.` |
| `app/projects/[slug]/edit/page.tsx:97-104` | `Edit Project` / `Revise the problem, solution, and details for {title}.` | `Edit project` / `{title}` |

### Task detail — `/tasks/[id]` (escrow copy untouched)

| File:line | Before | After |
| --- | --- | --- |
| `app/tasks/[id]/page.tsx:103` | `This task could not be loaded, or is no longer available to you.` | `This task didn't load, or you no longer have access.` |
| `app/tasks/[id]/page.tsx:126` | `No description was supplied for this task.` | `No description.` |

### Create forms — events, tasks, opportunities, guilds

| File:line | Before | After |
| --- | --- | --- |
| `app/events/create/page.tsx:201` | `Back to Events` | `Back to events` |
| `app/events/create/page.tsx:207` | `Create Event` | `New event` |
| `app/events/create/page.tsx:210` | `Organize a community event for members to join` | *(deleted)* |
| `app/events/create/page.tsx:219,336` | `Event Details` / `Event Format` | `Details` / `Format` |
| `app/events/create/page.tsx:271` | placeholder `Describe the event, what attendees will learn or experience...` | `What will people do or learn?` |
| `app/events/create/page.tsx:564` | placeholder `Leave empty for unlimited` | `Unlimited` (help line `Leave empty for no limit` kept — it is the aria-describedby target) |
| `app/events/create/page.tsx:657,660` | `Creating Event...` / `Create Event` | `Creating…` / `Create event` |
| `app/events/create/page.tsx:672` | `After creating the event, community members can register to attend. You'll be able to manage attendees and send updates.` | *(deleted with its info box)* |
| `app/tasks/create/page.tsx:244` | `Back to Tasks` | `Back to tasks` |
| `app/tasks/create/page.tsx:250` | `Create New {PBI/Task}` | `New {PBI/task}` |
| `app/tasks/create/page.tsx:253` | `Add a new work item to track progress` | *(deleted)* |
| `app/tasks/create/page.tsx:301` | `{PBI/Task} Details` | `Details` |
| `app/tasks/create/page.tsx:354` | placeholder `Describe the task in detail. Include acceptance criteria if applicable...` | `What does done look like?` |
| `app/tasks/create/page.tsx:672,677` | `Creating {PBI/Task}...` / `Create Task` | `Creating…` / `Create task` |
| `app/tasks/create/page.tsx:691` | `PBIs represent user stories or work items in your product backlog. They can be broken down into tasks.` | `PBI: a backlog item you break into tasks.` (the one term the UI never defines) |
| `app/tasks/create/page.tsx:692` | `Tasks will appear in your backlog. You can move them to different stages as you work on them.` | *(deleted — box now renders for PBIs only)* |
| `components/opportunity-form.tsx:327,352` | `Back to Opportunities` / `Back to Position` | `Back to opportunities` / `Back to position` |
| `components/opportunity-form.tsx:331` | `Every opportunity belongs to a project or guild you can manage.` | `Only projects or guilds you manage.` |
| `components/opportunity-form.tsx:359` | `Edit Position` / `Post a Job` | `Edit position` / `New opportunity` |
| `components/opportunity-form.tsx:363-364` | `Update the details for this position` / `Create an opportunity to find talented contributors for your project` | *(deleted)* |
| `components/opportunity-form.tsx:376` | `Creating a team position for project: {slug}` | `For {slug}` |
| `components/opportunity-form.tsx:386,574` | `Job Details` / `Required Skills` | `Details` / `Required skills` |
| `components/opportunity-form.tsx:562` | `Assign this position to a team role so it appears under that role in the team view` | `Shows under this role in the team view.` |
| `components/opportunity-form.tsx:755-760` | `Saving...` / `Posting Job...` / `Save Changes` / `Post Job` | `Saving…` / `Posting…` / `Save changes` / `Post opportunity` |
| `components/opportunity-form.tsx:773` | `Your job posting will be visible to all community members. You'll receive applications that you can review and respond to.` | *(deleted with its info box)* |
| `components/guild-form.tsx:161-164` | `Authentication Required` / `You need to be signed in to create/edit a guild.` | `Sign in to create a guild` / `Sign in to edit this guild` |
| `components/guild-form.tsx:172` | `Sign In` | `Sign in` |
| `components/guild-form.tsx:188` | `Back to Guilds` | `Back to guilds` |
| `components/guild-form.tsx:195` | `Create Guild` / `Edit Guild` | `New guild` / `Edit guild` |
| `components/guild-form.tsx:199-200` | `Set up your professional guild to start bidding on projects` / `Update your guild information` | *(deleted)* |
| `components/guild-form.tsx:208` | `Guild Details` | `Details` |
| `components/guild-form.tsx:260` | placeholder `Describe what your guild does, your expertise, and what makes you unique...` | `What does your guild do?` |
| `components/guild-form.tsx:432` | `Enter your specialties separated by commas` | `Separate with commas.` |
| `components/guild-form.tsx:450` | placeholder `Describe your past work and achievements...` | `Past work and results` |
| `components/guild-form.tsx:499-504` | `Creating Guild...` / `Saving Changes...` / `Create Guild` / `Save Changes` | `Creating…` / `Saving…` / `Create guild` / `Save changes` |
| `components/guild-form.tsx:518` | `After creating your guild, you can invite members, set up specialties, and start bidding on project opportunities.` | *(deleted with its info box)* |

### Guild detail, credentials, opportunity detail — back links and errors

| File:line | Before | After |
| --- | --- | --- |
| `app/guilds/[slug]/page.tsx:102` | `Guild could not be loaded` | `Guild didn't load.` |
| `app/guilds/[slug]/page.tsx:105,149` | `Back to Guilds` | `Back to guilds` |
| `app/credentials/[id]/page.tsx:75,114` | `Back to Profile` | `Back to profile` |
| `app/projects/create/page.tsx:1203` | `Back to Projects` | `Back to projects` |
| `components/opportunity-detail-view.tsx:85` | `Back to Opportunities` | `Back to opportunities` |

### Project create — `/projects/create` (money, stake, account-setup and recovery copy untouched)

| File:line | Before | After |
| --- | --- | --- |
| `app/projects/create/page.tsx:1206` | `Create New Project` | `New project` |
| `app/projects/create/page.tsx:1207` | `Define a problem, shape a solution, then make the work actionable.` | *(deleted)* |
| `app/projects/create/page.tsx:1323` | `Project Overview` | `Overview` |
| `app/projects/create/page.tsx:1379` | placeholder `The one sentence someone reads before deciding to care` | `One sentence` |
| `app/projects/create/page.tsx:1398` | `Shown on cards and in search results. Optional, but it is the first thing anyone reads.` | `Shown on cards and in search.` |
| `app/projects/create/page.tsx:1365` | label `One-line summary` | `One-line summary (optional)` (optionality moved from the help line to the label) |
| `app/projects/create/page.tsx:1417` | `Problem Definition` | `Problem` |
| `app/projects/create/page.tsx:1441` | placeholder `Describe the problem you're solving. What is the issue? Who does it affect?` | `What is wrong, and who does it affect?` |
| `app/projects/create/page.tsx:1475` | `Who are the primary beneficiaries?` | `Who benefits?` |
| `app/projects/create/page.tsx:1493` | `What impact do you expect to achieve?` | `What changes if this works?` |
| `app/projects/create/page.tsx:1507` | `Solution Definition` | `Solution` |
| `app/projects/create/page.tsx:1527` | placeholder `Describe your proposed solution. How does it address the problem?` | `How does this solve it?` |
| `app/projects/create/page.tsx:1551,1623` | `Project Type` / `Project Details` | `Type` / `Details` |
| `app/projects/create/page.tsx:2014-2017` | `Project Resources` / `Define resources needed for your project` | `Resources` |
| `app/projects/create/page.tsx:2371-2372` | `No resources added yet` / `Click "Add Resource" to define project resources` | `No resources yet.` |
| `app/projects/create/page.tsx:2594-2597` | `Team Roles` / `Define roles and compensation for your team` | `Team roles` |
| `app/projects/create/page.tsx:2749` | `The organizational role granted when this position is filled` | `Granted when the position is filled.` |
| `app/projects/create/page.tsx:2841-2842` | `No roles defined yet` / `Click "Add Role" to define team roles` | `No roles yet.` |
| `app/projects/create/page.tsx:2916-2919` | `Project Milestones` / `Define key milestones for your project timeline` | `Milestones` |
| `app/projects/create/page.tsx:3051-3052` | `No milestones defined yet` / `Click "Add Milestone" to define project milestones` | `No milestones yet.` |
| `app/projects/create/page.tsx:3941` | `This wizard will guide you through creating a comprehensive project. You can skip optional steps and come back to edit them later.` | `Skip optional steps. Edit them later.` |

Step names (`Basic Info`, `Team Roles`, …) were left as-is: the money validation string at line 647 (`Set a funding goal above zero on the Basic Info step`) names a step, and renaming the step would make protected copy wrong.

### Project tabs — `components/projects/*` (funding, money and equity tabs untouched)

| File:line | Before | After |
| --- | --- | --- |
| `components/projects/comments-tab.tsx:161` | `COMMENTS COULD NOT BE LOADED` | `Comments didn't load.` |
| `components/projects/comments-tab.tsx:233-236` | `No comments yet` / `Be the first to share your thoughts on this project` | `No comments yet.` |
| `components/projects/updates-tab.tsx:160` | `Project Updates` | `Updates` |
| `components/projects/updates-tab.tsx:165` | `New Update` | `New update` |
| `components/projects/updates-tab.tsx:174` | `Post a New Update` | `New update` |
| `components/projects/updates-tab.tsx:206` | placeholder `Share your progress, achievements, or news...` | `What changed?` |
| `components/projects/updates-tab.tsx:261` | `Failed to load updates: {error}` | `Updates didn't load: {error}` |
| `components/projects/updates-tab.tsx:272-277` | `No updates yet` / `Share your progress with supporters by posting your first update.` / `Check back later for project updates from the team.` | `No updates yet.` |
| `components/projects/roadmap-tab.tsx:116` | `FAILED TO LOAD ROADMAP` | `Roadmap didn't load.` |
| `components/projects/roadmap-tab.tsx:212` | `Add milestones to track your project roadmap.` | *(deleted)* |
| `components/projects/roadmap-tab.tsx:367,529` | `Epics could not be loaded: …` / `Sprints could not be loaded: …` | `Epics didn't load: …` / `Sprints didn't load: …` |
| `components/projects/roadmap-tab.tsx:379` | `NO EPICS IN THIS MILESTONE` | `NO EPICS` |
| `components/projects/sprints-tab.tsx:87` | `FAILED TO LOAD SPRINTS` | `Sprints didn't load.` |
| `components/projects/sprints-tab.tsx:112-114` | `NO SPRINTS AVAILABLE` / `Create milestones and epics in the Roadmap tab first, then add sprints.` | `NO SPRINTS YET` / `Add milestones and epics in Roadmap first.` (prerequisite kept) |
| `components/projects/sprints-tab.tsx:153` | `Choose a sprint from the sidebar to view details.` | *(deleted — `SELECT A SPRINT` above it)* |
| `components/projects/sprints-tab.tsx:234,319` | `Epics / Sprints could not be loaded: …` | `Epics / Sprints didn't load: …` |
| `components/projects/sprints-tab.tsx:491` | `SPRINT WORK COULD NOT BE LOADED` | `Sprint work didn't load.` |
| `components/projects/sprints-tab.tsx:760` | `Add features and backlog items to plan this sprint.` | *(deleted)* |
| `components/projects/tasks-tab.tsx:198` | `FAILED TO LOAD TASKS` | `Tasks didn't load.` |
| `components/projects/team-tab.tsx:257` | `Invite Member as {role}` | `Invite as {role}` |
| `components/projects/team-tab.tsx:258` | `Search for a user to invite to this project` (visible) | same text, now `sr-only` (Radix needs a description) |
| `components/projects/team-tab.tsx:339` | `No matching users found` | `No matches.` |
| `components/projects/team-tab.tsx:434` | placeholder `Add a personal message to the invitation...` | `A short note` |
| `components/projects/team-tab.tsx:705` | `TEAM COULD NOT BE LOADED` | `Team didn't load.` |
| `components/projects/team-tab.tsx:767` | `Team positions, members, and open opportunities` | *(deleted)* |
| `components/projects/team-tab.tsx:1212-1213` | `Apply to Join Project` / `Tell the project owner why you'd like to join and what you can contribute` | `Apply to join` |
| `components/projects/team-tab.tsx:1224,1242` | `Desired Role Title *` / `Application Message * (min 20 characters)` | `Desired role *` / `Message * (min. 20 characters)` |
| `components/projects/team-tab.tsx:1249` | placeholder `Explain why you want to join this project and what you can contribute...` | `What would you bring?` |
| `components/projects/team-tab.tsx:1279,1284` | `Experience (Optional)` / placeholder `Briefly describe your relevant experience...` | `Experience (optional)` / `Relevant experience` |
| `components/projects/team-tab.tsx:1455-1458` | `Pending Applications` / `Review applications from people who want to join your project` | `Pending applications` |
| `components/projects/team-tab.tsx:1473` | `No pending applications` | `No pending applications.` |
| `components/projects/proposals-tab.tsx:218` | `No comments yet. Be the first to start the discussion.` | `No comments yet.` |
| `components/projects/proposals-tab.tsx:603` | `Error loading proposals: {error}` | `Proposals didn't load: {error}` |
| `components/projects/proposals-tab.tsx:616` | `Create New Proposal` | `New proposal` |
| `components/projects/proposals-tab.tsx:934-939` | `No proposals yet` / `Create the first proposal to get started with project governance.` / `No proposals have been created for this project yet.` | `No proposals yet.` |
| `components/projects/proposals-tab.tsx:1519` | `Cast Your Vote` | `Cast your vote` |
| `components/projects/proposals-tab.tsx:1590` | `You have voted on this proposal` | `You voted` |
| `components/projects/opportunities-tab.tsx:1901-1905` | `No team positions` / `Post a role and the person you accept joins the team in it.` / `No team positions have been posted yet.` | `No positions yet.` / managers only: `Whoever you accept joins the team.` |
| `components/projects/opportunities-tab.tsx:1912` | `Create Position` | `Create position` |
| `components/projects/opportunities-tab.tsx:2347-2353` | `No work items yet` / `Start by creating a milestone, or switch to 'All Tasks' for a flat list.` / `No work items have been created for this project yet.` | `No work items yet.` / managers only: `Start with a milestone.` |
| `components/projects/project-edit-form.tsx:273,442,514` | `Project Overview` / `Project Type` / `Project Details` | `Overview` / `Type` / `Details` |
| `components/projects/project-edit-form.tsx:318,400` | `Problem Definition` / `Solution Definition` | `Problem` / `Solution` |
| `components/projects/project-edit-form.tsx:338,370,386,420` | the four long placeholders (same as project create) | `What is wrong, and who does it affect?` / `Who benefits?` / `What changes if this works?` / `How does this solve it?` |
| `components/projects/work-item-detail-modal.tsx:358` | `Comments could not be loaded. The work item is still available.` | `Comments didn't load.` |
| `components/projects/work-item-detail-modal.tsx:435` | `Sign in to contribute to this discussion.` | *(deleted — the `Sign in to comment` button stays)* |
| `components/projects/create-work-item-modal.tsx:407` | placeholder `Describe the task in detail...` | `What does done look like?` |

### Chats modal and field notes

| File:line | Before | After |
| --- | --- | --- |
| `components/chats/new-conversation-modal.tsx:91` | `New Message` | `New message` |
| `components/chats/new-conversation-modal.tsx:93` | `Search for a user to start a conversation` (visible) | same text, now `sr-only` |
| `components/chats/new-conversation-modal.tsx:146-149` | `People could not be loaded` / `This is a service error, not an empty directory.` | `People didn't load.` |
| `components/chats/new-conversation-modal.tsx:155` | `Retry search` | `Try again` |
| `components/chats/new-conversation-modal.tsx:163` | `No users found` / `No users available` | `No matches.` / `No people yet.` |
| `components/feed/project-field-notes.tsx:49` | `Progress, setbacks and decisions, written down as the work happens.` | *(deleted)* |
| `components/feed/project-field-notes.tsx:70` | `No field notes yet. The first update tells supporters the work has started.` | `No field notes yet.` |

### Governance components

| File:line | Before | After |
| --- | --- | --- |
| `components/governance/discussion-tab.tsx:35-39` | `Discussion Coming Soon` / `This feature will allow proposal discussions and debates. Community members will be able to share their thoughts, ask questions, and engage in meaningful dialogue about proposals.` | `Discussion is coming soon.` |
| `components/governance/discussion-tab.tsx:51,57` | `Add Your Comment` / placeholder `Share your thoughts on this proposal...` | `Add a comment` / `What do you think?` |
| `components/governance/overview-tab.tsx:297` | `Cast Your Vote` | `Cast your vote` |
| `components/governance/overview-tab.tsx:301` | `Select your voting choice and optionally provide a reason.` (+ finality sentence) | *(deleted; `Your vote is final and cannot be changed.` kept verbatim)* |
| `components/governance/overview-tab.tsx:348` | placeholder `Share why you voted this way...` | `Why this choice?` |
| `components/governance/votes-tab.tsx:81-82` | `No votes yet` / `Be the first to vote on this proposal` | `No votes yet.` |

### Guild components

| File:line | Before | After |
| --- | --- | --- |
| `components/guilds/applications-panel.tsx:54` | `People asking to join.` (+ consequence sentence) | *(deleted; `Accepting makes them a member straight away.` kept)* |
| `components/guilds/join-guild-panel.tsx:184` | `The guild's owner or admins review applications. You become a member only when one of them accepts.` | `You join only when an owner or admin accepts.` |
| `components/guilds/members-tab.tsx:175-177` | `Guild Members` / `People who are part of this guild` | `Members` |
| `components/guilds/members-tab.tsx:326-329` | `Pending Invitations` / `Sent, but not yet accepted. These people are not members yet.` | `Pending invitations` / `Not members until they accept.` |
| `components/guilds/members-tab.tsx:350-353` | `Invite New Member` / `Invite someone by the email on their ArdaNova account. They join only when they accept it from their guild invitations.` | `Invite a member` / `Use their account email. They join once they accept.` |
| `components/guilds/members-tab.tsx:511-512` | `Member Roles` / `Understanding guild member roles and permissions` | `Roles` |
| `components/guilds/opportunities-tab.tsx:128-130` | `Guild Opportunities` / `Open positions and opportunities posted by this guild` | `Opportunities` |
| `components/guilds/opportunities-tab.tsx:139` | `Post Opportunity` | `Post opportunity` |
| `components/guilds/opportunities-tab.tsx:150-156` | `No opportunities yet` / `Post your first opportunity to attract talented developers.` / `This guild hasn't posted any opportunities yet.` | `No opportunities yet.` |
| `components/guilds/overview-tab.tsx:158-161` | `Work With Us` / `Looking for experienced professionals to bring your project to life? Use the guild's published contact channel to discuss the work.` | `Work with this guild` |
| `components/guilds/overview-tab.tsx:174` | `Visit the guild website` | `Visit website` |
| `components/guilds/reviews-tab.tsx:267-268` | `Write a Review` / `Share your experience working with this guild` | `Write a review` |
| `components/guilds/reviews-tab.tsx:291` | placeholder `Share your thoughts about working with this guild...` | `How was it?` |
| `components/guilds/reviews-tab.tsx:334-338` | `No reviews yet` / `Your guild doesn't have any reviews yet. Encourage clients to share their experiences.` / `Be the first to review this guild and share your experience.` | `No reviews yet.` |
| `components/guilds/updates-tab.tsx:134,139,148` | `Guild Updates` / `New Update` / `Post a New Update` | `Updates` / `New update` / `New update` |
| `components/guilds/updates-tab.tsx:180` | placeholder `Share news, achievements, or announcements...` | `What's new?` |
| `components/guilds/updates-tab.tsx:246-249` | `No updates yet` / `Share news and announcements with your community by posting your first update.` / `Check back later for updates from the guild.` | `No updates yet.` |

### Opportunity components (bids, bid-accept, status and applications confirmations untouched)

| File:line | Before | After |
| --- | --- | --- |
| `components/opportunities/updates-tab.tsx:212` | `Opportunity Updates` | `Updates` |
| `components/opportunities/updates-tab.tsx:217,274` | `Post Update` | `Post update` |
| `components/opportunities/updates-tab.tsx:226` | `Post a New Update` | `New update` |
| `components/opportunities/updates-tab.tsx:258` | placeholder `Share progress, announcements, or news about this opportunity...` | `What's new?` |
| `components/opportunities/updates-tab.tsx:319-322` | `No updates yet` / `Share progress and announcements with applicants by posting updates.` / `Check back later for updates from the opportunity owner.` | `No updates yet.` |
| `components/opportunities/comments-tab.tsx:245,258` | placeholder `Share your thoughts about this opportunity...` / `Post Comment` | `Add a comment` / `Post comment` |
| `components/opportunities/comments-tab.tsx:270-271` | `No comments yet` / `Be the first to share your thoughts on this opportunity` | `No comments yet.` |
| `components/opportunities/entity-selector.tsx:41,46` | `Post opportunity on behalf of your guild` / `Post opportunity for your project` | `On behalf of your guild` / `For your project` |
| `components/opportunities/entity-selector.tsx:88-89` | `Select Entity Type` / `Choose which entity will be posting this opportunity` | `Post as` |
| `components/opportunities/entity-selector.tsx:194-197` | `{Type}s could not be loaded` / `Try again before choosing who will own this opportunity.` | `{Type}s didn't load.` / `Try again before choosing an owner.` |
| `components/opportunities/apply-form.tsx:98` | placeholder `What you'd bring to the team and how you'd like to help.` | `What would you bring?` |

### Profile components

| File:line | Before | After |
| --- | --- | --- |
| `components/profile/profile-editor.tsx:127` | `Your profile could not be loaded` | `Your profile didn't load.` |
| `components/profile/profile-editor.tsx:241` | `Add the skills you want project leads to find you for.` | `Add skills project leads can find you by.` |

### Studio (benched) and final sweep

| File:line | Before | After |
| --- | --- | --- |
| `app/studio/page.tsx:552-554` | `Shape the work. / Keep the agency.` | `Nova Studio` |
| `app/studio/page.tsx:556-558` | `Nova Studio is a reviewable workspace for drafting a project, shaping its presentation, and rehearsing the hard questions. You remain the author and decision-maker.` | *(deleted — the `03 / Agency` cell directly below says `Only you can publish, fund, change rights, or approve.`)* |
| `app/chats/page.tsx:396` | `COORDINATION, NOT DISTRACTION` (eyebrow over `Messages`) | *(deleted)* |
| `components/projects/roadmap-tab.tsx:141` | `PROJECT ROADMAP` (inside the Roadmap tab) | `ROADMAP` |
| `components/opportunities/entity-selector.tsx:171-174` | `Select {Guild/Project}` / `Choose from your {guild/project}s` | `Choose a {guild/project}` |
| `components/opportunities/entity-selector.tsx:204-209` | `No {Guild/Project}s Found` / `You don't have any {x}s yet. Create one to post opportunities.` | `No {x}s yet.` |
| `components/opportunities/entity-selector.tsx:212` | `Create {Guild/Project}` | `Create {guild/project}` |

Mechanical, not copy: every `didn't` that landed in raw JSX text is written `didn&apos;t` (`react/no-unescaped-entities`). It renders identically.

---

## Deliberately left

### Hard exclusions (files on the open money branch; not opened for edit)

`app/portfolio/add-funds/page.tsx`, `app/projects/[slug]/invest/page.tsx`, `components/app-sidebar.tsx`, `components/ui/query-error-copy.ts`, `lib/commerce/**`, `server/api/**`, `lib/api/**`, `app/tasks/[id]/commerce/**`.

- That includes the third P8-17 line, `server/api/routers/post.ts:11` ("there is no global feed"). It is still live and needs a follow-up after the money branch merges.

### Whole surfaces left untouched (money and verification)

`app/portfolio/page.tsx`, `app/portfolio/withdraw/page.tsx`, `app/swap/page.tsx`, `app/settings/payouts/page.tsx`, `app/settings/verification/page.tsx`, `app/projects/[slug]/invest/success/page.tsx`, `app/admin/{kyc,treasury,disputes}/page.tsx`, `components/equity/*`, `components/escrow/*`, `components/projects/{funding,money,equity}-tab.tsx`, `components/azoa/*`, `components/tasks/task-economic-state.tsx`, `components/portfolio/holdings.tsx`, `components/onboarding/setup-prompt.tsx` (KYC), `components/opportunities/{bid-form,opportunity-bids-panel,bid-accept-button}.tsx`.

### Individual strings kept verbatim on pages that were otherwise stripped

| File:line | String | Why kept |
| --- | --- | --- |
| `app/global-error.tsx:32-34` | `If you were saving something, it was only saved if you saw a confirmation; check before trying again.` | Safety: guards against double submission |
| `app/error.tsx:28-29` | `This is a service error, not a sign that your records are missing.` | Safety: data-loss reassurance |
| `app/not-found.tsx:18` | `Nothing you have saved has changed.` | Safety |
| `app/auth/error/copy.ts:9-31` | All six refusal/expiry messages | Refusal copy; pinned by `auth/error/page.test.ts` |
| `app/auth/error/page.tsx:36,46-47` | `Authentication / no state changed` · `Authentication proves who is entering. Publishing, approvals, funds, settlement, and rights remain separate actions inside the workspace.` | Consent: sign-in changes nothing else |
| `app/auth/signin/page.tsx:146` | `You will review project and rights-changing actions separately inside the workspace.` | Consent (its narrating first sentence was cut) |
| `app/auth/signin/page.tsx:204-219,229-231` | Terms/Privacy agreement line · `Signing in authenticates your account. Publishing, approving, and settlement actions remain separate, deliberate steps in the workspace.` | Terms and consent |
| `app/page.tsx:299-301,315-317` | Record A / Record B paragraphs | Rights copy (economic vs. governance interests) |
| `app/page.tsx:325-327` | `Platform records do not automatically create legal ownership, equity, or payment rights. …` | Legal |
| `app/page.tsx:356-357` | `A draft stays private until you publish it. Nova cannot fund, pay, or vote.` | Consent and money boundary |
| `app/page.tsx:54-55` | Trust cue `Ownership and decision authority come from documented terms, never from activity, membership, or a balance alone.` | Rights |
| `app/dashboard/page.tsx:283-285` | `Participation is not proof of ownership. Governance credentials, project utility, and economic rights remain separate, reviewable records.` | Rights (kept in place — review round 2 ruling) |
| `app/dashboard/page.tsx:452` | `A task state does not imply an award has been reconciled.` | Money state (its first sentence was cut; moved directly above `View your tasks`) |
| `app/projects/page.tsx:262` | `Totals are not shown because this page of projects could not be loaded.` | Money-figure caveat; stat labels `Funding recorded` / `Supporters recorded` also kept |
| `app/projects/[slug]/page.tsx:308,328,343-350` | `Current funding`, `Funding record`, the `Commerce enabled` box | Money and rights |
| `app/projects/create/page.tsx` | Every funding, stake, account-setup and recovery string (600-647, 969-1144, 1235-1240, 1928-1996, 2457-2583, 3121-3413, 3697, 3878-3894); the step names (line 647's money validation names the `Basic Info` step) | Money, KYC, and the recovery line `It will not create a duplicate project.` |
| `app/tasks/[id]/page.tsx:168-216` | Escrow and set-aside copy | Money |
| `app/chats/page.tsx:303` | `Your message was not sent. Check your connection and try again.` | Safety: no double send |
| `app/chats/page.tsx:680-681` | `Access has not been inferred or changed.` | Permission safety (its narrating first sentence was cut) |
| `app/settings/profile/page.tsx:20` | `Identity verification is a separate step under Verification.` | Verification |
| `app/credentials/[id]/page.tsx:69,186` | Withdrawn-credential and outside-verification lines | Verification |
| `components/projects/proposals-tab.tsx:146-154,1503-1505,1522-1523,1535` | Outcome explanations · `Vote history is temporarily unavailable. Voting is paused here to prevent a duplicate submission.` · membership-eligibility line · vote-visibility placeholder | Governance results, anti-double-submit, consent |
| `components/projects/team-tab.tsx:110-130,521-522,1338-1340` | Role permission descriptions · leave/remove consequences · unit pool allocation | Permissions, consequence, money |
| `components/governance/overview-tab.tsx:165-166,301` | Unreadable-options line · `Your vote is final and cannot be changed.` | Safety and irreversibility |
| `components/governance/proposal-tally.ts:99-102` | Voting-state lines | Pinned by `proposal-tally.test.ts` |
| `components/guilds/applications-panel.tsx:55,91,96` | `Accepting makes them a member straight away.` and the two permission lines | Consequence and permissions |
| `components/guilds/join-guild-panel.tsx:299` · `members-tab.tsx:468,521-535` | Leave/remove consequences; role permissions | Consequence and permissions |
| `components/opportunities/applications-tab.tsx:126,250-251` · `apply-form.tsx:56` · `opportunity-status-control.tsx:18-19` | Owner-only line, accept/reject confirmations, sent confirmation, Draft/Open visibility | Permission, consent, visibility |
| `components/feed/field-note-audience.ts:17-20` | Audience/visibility descriptions | Privacy |
| `components/profile/profile-data-card.tsx:43,62` · `profile-editor.tsx:40,183` | Data export, account closure, field visibility | Privacy and stakes |
| `components/ai/**`, `app/studio/page.tsx` (except the header) | Data-scope, "nothing is sent", third-party model disclosure, rate-limit and recovery messages, the `01 / Scope · 02 / Evidence · 03 / Agency` cells and the "cannot do" lists | Consent and privacy; `nova-refine-copy.ts` is pinned by its test. Benched, but every remaining line is a disclosure. |
| `components/app-topbar.tsx:56-57` | Local-preview banner (`…backend permissions still apply`) | Dev-only permission notice |

### Left because they are already just enough

Short uppercase state labels (`NO SPRINTS`, `NO MILESTONES YET`, `SELECT A SPRINT`) and the keyboard hint `Enter to send · Shift + Enter for a new line` stay. So do `Leave empty for no limit`, which is an `aria-describedby` target, and the `© 2026 ArdaNova` legal lines. The landing band `The solutionary loop` is the one brand word on the logged-out page, and the canvas caption `Find it. Fund it. Build it.` is part of the hero art. Three dialog descriptions became `sr-only` instead of being deleted, because Radix warns when a dialog has no description.

`app/_components/post.tsx` (`Welcome to ArdaNova` / `The Social Network for Doing`) is never imported. It is dead scaffold and was not edited.

---

## Review round 2 (coordinator: APPROVE WITH CHANGES) — resolutions

| Item | Resolution |
| --- | --- |
| HIGH-1 rights qualifier | The hero line and the OG and Twitter descriptions now end `…share in what it becomes, on agreed terms.` The main meta description (`layout.tsx:16`) is restored verbatim (`…reconcile contributor rights…`). A claim's qualifier travels with the claim. |
| MED-2 | `Proposals remain proposals until the recorded process resolves them. Events are shown as scheduled, not as attendance or consent.` is restored verbatim under the dashboard heading, which is now `Proposals and events`. |
| MED-3 | Project detail and project edit split their error headings. A query error shows `This project didn't load.`, an absent record shows `Project not found` (the `guilds/[slug]` pattern). |
| LOW-4 | `A task state does not imply an award has been reconciled.` moved, wording unchanged, to directly above `View your tasks`. |
| LOW-7 | The four chats retry buttons keep the visible text `Try again` and gain accessible names: `Try again to load conversations` / `…to load this conversation` / `…to refresh details` / `…to load messages`. **Deviation:** the review suggested `Retry conversations` etc. Under WCAG 2.5.3 (Label in Name, level A) the accessible name must contain the visible text, or a voice-control user saying "click Try again" gets no match. So each name starts with the visible words. |
| LOW-8 | `Home page` → `Go home` (sign-in, global-error, not-found). Both chats `Back` buttons return to the list (`setSelectedChatId(null)`), so both now read `Back to chats`. `Optional, but read first.` was cut, leaving `Shown on cards and in search.`. The label lacked any optional marker, so it became `One-line summary (optional)`. `Sign in to create a guild` was **left**: it is the only text on an otherwise empty page above a `Sign in` button, so shortening it would lose the purpose. |
| Test timeout | `lib/vocabulary/vocabulary.test.ts` repo-wide scan: 5 s → 15 s. The sibling repo-wide scan in `lib/product-vocabulary.test.ts` ("reports the real client tree as clean") then timed out the same way and got the same 15 s. No assertion changed in either file. |
| Rulings | `Problems are not in short supply.` stays; no second Žižek line; `Participation is not proof of ownership…` stays in place. |

### Still open for the owner

1. **The new trust cue needs confirming.** `Decisions stay recorded / Proposals, votes, and outcomes stay on the record.` replaced the cue that SR-03-D2 made false. Confirm the claim, or delete the cue.
2. **Nova is benched but still advertised.** The landing page keeps its `#nova` section and nav link, and the dashboard still offers `Open Nova Studio`. Removing them is a structural change outside this lane.
3. **P8-17 is not fully closed.** `server/api/routers/post.ts:11` ("there is no global feed") sits behind the money-branch exclusion.

---

## Verification (after review round 2)

| Check | Result |
| --- | --- |
| `npx tsc --noEmit` | exit 0 |
| `npx eslint` on the 11 files touched in round 2 | exit 0 |
| `npm run lint` (full, round 1) | 1 error, 4 warnings, all pre-existing in files this branch does not touch (`app/api/auth/[...nextauth]/route.ts` `unbound-method`; unused vars in `lib/api`, `lib/contracts`, `server/api`). The 38 `no-unescaped-entities` errors round 1 introduced are fixed. |
| `npx vitest run` | **692 / 692 passed, 72 / 72 files** |
| Excluded-file check | `git diff --name-only 9a40e43` contains no hard-excluded path and no money or verification surface |
| Copy-asserting tests | None asserted changed copy. The only test edits are the two timeouts. |
