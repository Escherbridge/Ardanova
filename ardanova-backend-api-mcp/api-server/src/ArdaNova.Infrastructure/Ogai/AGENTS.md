# Nova AI upstream (OVERSEASGENAI partner API)

Nova AI is ArdaNova's in-product assistant. Its project-refinement mode is powered by
OVERSEASGENAI's validate ("Grill-Me") stage, consumed white-label through the partner API
`POST /api/partner/v1/validate` (OGAI repo, branch `partner/ardanova-nova`,
`apps/site/server/partner/AGENTS.md`). Members only ever see "Nova AI".

## Call path

browser → tRPC `nova.refineProject` (thin proxy, `ardanova-client/src/server/api/routers/nova.ts`)
→ `POST /api/nova/project-refinement` (`NovaController`, API key + signed actor assertion)
→ `NovaOrchestrationService` (Application: validation, per-member allowance, brand guard)
→ `IOgaiClient` (this folder) → OGAI. .NET is the only caller: the partner key never reaches the
BFF or the browser, and the AI client service (`ardanova-ai-client`) is not on this path.

## Configuration

| Setting | Env | Notes |
|---|---|---|
| `Ogai:BaseUrl` | `Ogai__BaseUrl` | OGAI site origin; must be https in Production |
| `Ogai:PartnerApiKey` | `Ogai__PartnerApiKey` | raw key from the secret store; OGAI stores only its SHA-256 in `PARTNER_API_KEYS` |
| `Ogai:TimeoutSeconds` | `Ogai__TimeoutSeconds` | default 95; see §Turn budget |
| `Nova:SessionKey` | `NOVA_SESSION_KEY` | optional, 32+ chars; signs session tickets. Unset uses the actor-assertion key (HKDF, own label) |

`OgaiRegistration.AddOgai` selects the client:

- both set: `OgaiClient`, a typed HttpClient with the bearer key as a default header, redirects off,
  and every logged header redacted (`RedactLoggedHeaders`). Logs carry status codes and failure
  kinds, never the idea, answers or key.
- unset outside Production: `LocalNovaPreviewClient`, three fixed practice questions and a
  `preview` outcome. Responses carry `mode: "local-preview"` and say plainly that nothing was
  analysed, so local dev and tests work without pretending to be AI.
- unset in Production: `DisabledOgaiClient`; Nova AI answers 503 rather than silently faking.

Each client reports `Availability` (`live`, `local-preview`, `off`), served at `GET /api/nova/status`. The
client reads it before the first turn, so a deployment without Nova AI says so up front instead of showing a
form that fails with "try again in a moment" forever, and a local preview is disclosed before anything is typed.

## Service rules (`NovaOrchestrationService`)

- Input mirrors the upstream form (idea 20-2000 chars, at most 8 turns, answers 1-1500), so a refusal
  never spends a session or a model call.
- `NovaSessionAllowance`: at most 10 new sessions per member per UTC day (in memory, single replica;
  a restart forgets the count, and the OGAI partner quota still binds). A first turn the member
  does not receive is given back, whatever the reason: an upstream failure, a reply ArdaNova drops (brand
  leak, oversized state, unknown type or outcome) or a cancellation. `RunTurnAsync` refunds in a `finally`
  keyed on success, so a new drop reason cannot quietly cost members their day again (loop 4 found three
  drop paths that spent the allowance and then said "not your fault, try again"). Only a `live` client spends it: the local preview's fixed questions cost
  nothing, and counting them used to lock QA lanes out for the day while the sheet said "not connected".
  `GET /api/nova/status` (per actor) and every live reply carry `remainingToday` (and status `dailyLimit`),
  null when nothing is counted, so the sheet can say "N of 10 left today".
- One turn in flight per member (`NovaSessionAllowance.TryBeginTurn`/`EndTurn`, held in a `finally`). OGAI
  gives each partner 8 concurrent model-call slots and a turn can hold one for about 88 s, so without
  this one account firing parallel first turns could fill every slot and push every other member to
  "unavailable". A second turn while one runs is 429 `nova_turn_in_flight` and spends nothing.
- Session tickets (`NovaSessionTickets`): the `state` a member gets back is not the upstream's state but
  `nv1.<expiry>.<hmac>.<upstream state>`, with the HMAC over (actor, session, expiry, upstream state). A
  continuation whose ticket is not this actor's live ticket for this session returns NotFound
  `nova_session_not_found` (404) before any upstream call, so a leaked or guessed session id can neither be
  taken over nor used to run turns outside the daily cap. "Not yours" and "expired" are the same 404 on
  purpose: a different answer would tell a prober which ids are real. The MAC length-prefixes every field
  (`Mac()`), which is what stops two different (actor, session, expiry, state) lists from producing the same
  MAC input; keep the prefix if that loop is ever rewritten. Ownership lives in the signed state, not
  in memory, so a refinement survives a restart, a redeploy and a second replica. The expiry is set on the
  first turn and carried forward: `SessionLifetime` is 1 h 55 min, inside OGAI's 2 h `sessionLifetime` with
  margin for the first turn's own duration, so ArdaNova expires a session before OGAI would refuse it.
  Without `Nova:SessionKey` or the actor-assertion key (local dev only) the key is random per process, and a
  restart then reads in-flight refinements as expired.
- Ticket size: the envelope is a fixed `EnvelopeLength` (80) characters, so `MaxTicketLength` = 80 +
  `MaxUpstreamStateLength` (512). `TryOpen` refuses anything longer, and a `question` turn whose upstream
  state exceeds 512 fails now (503 `nova_unavailable`) instead of issuing a ticket the client would refuse
  on the next turn. The client caps (`ardanova-client/src/lib/nova-state-limits.ts`) are the same sum.
- Brand guard: OGAI already hides its own names; the service additionally drops any reply whose
  member-visible text (vertical included) `NovaBrand.NamesUpstream` (503), and stamps
  `Assistant = "Nova AI"` itself rather than trusting upstream. `HiddenNames` mirrors OGAI's own list for
  this brand (including "ogai"), and the host's name is also matched letters-only (`HiddenLetters`), so
  "Overseas Gen-AI" or a zero-width space inside the name is still caught. The short name is also matched
  from a word start across up to four adjacent words (`HiddenShortLetters`), so "OG-AI", "O.G.A.I." and
  "OG AI" are caught while "yoga is" or "log aisle" are not. A rare false positive (a sentence ending
  "overseas." followed by "GenAI") costs one dropped reply, never a leak, and never the member's allowance.
- Dropped replies: on a first turn the code is `nova_unavailable` (a retry mints a fresh upstream session, so
  "try again" is honest). On a continuation it is `nova_reply_unusable`: OGAI's turn ledger replays an answered
  (session, turn) byte for byte, so the same reply would be dropped on every retry until the session expired.
  The client says "Start over" instead of "try again" for that code.
- A verdict outcome outside `go`, `adjust`, `not_yet`, `preview` (`NovaVerdictOutcomes.Known`) is dropped, so the
  client never renders an upstream string raw as a badge. Verdict sources whose URL is not absolute http(s) are
  removed before the reply leaves .NET (the client also renders such a source as plain text).
- The local preview answers a continuation that does not match its idea or state with NotFound, so the member
  sees "start again" (404) rather than "check your input" (400).
- No member identity, project data or PII leaves ArdaNova: only the idea and answers the member typed.
  OGAI keeps no transcript for partner calls.

## HTTP mapping

| Upstream | Service result | `NovaController` |
|---|---|---|
| 200 question/verdict | Success | 200 |
| 400 `session` (state expired or forged upstream) | NotFound `nova_session_not_found` | 404 |
| other 400 | ValidationError | 400 |
| (none: this member's own 10 a day spent) | Conflict `nova_daily_limit` | 429 |
| (none: this member already has a turn running) | Conflict `nova_turn_in_flight` | 429 |
| 429 `quota`, 503 `spend_closed` (the partner's whole day, shared by every member) | Failure `nova_capacity` | 503 |
| (none: ticket not this actor's, or expired) | NotFound `nova_session_not_found` | 404 |
| 401, 5xx, timeout, malformed; a first-turn reply ArdaNova drops | Failure `nova_unavailable` | 503 |
| a continuation reply ArdaNova drops (brand leak, oversized state, unknown type or outcome) | Failure `nova_reply_unusable` | 503 (client: start over) |

A 429 that is not `quota` (OGAI's turn-in-flight) maps to 503 so the member simply retries. The client keys
its copy on the code: `nova_daily_limit` says "your limit", `nova_capacity` says "Nova AI is at capacity today",
never blaming a member who has used none of their own allowance.

## Turn budget

OGAI can spend about 88 s on one partner turn: web search (8 s) on the first turn, then up to two models with
one corrective retry each (4 x 20 s). `Ogai:TimeoutSeconds` (95) covers that, and the BFF's Nova endpoint runs
with its own 100 s timeout (`ardanova-client/src/lib/api/ardanova/index.ts`, `NOVA_TURN_TIMEOUT_MS`), so the
retry and fallback path completes instead of being cut off as "unavailable". Most turns answer in a few
seconds; the long budget only matters on the slow path. If OGAI's budget changes, change all three together
(OGAI `intake/validate/AGENTS.md` §Partner brand records the same numbers).

Tests: `tests/ArdaNova.API.Tests/Controllers/NovaControllerFlowTests.cs` drive the controller with a
fake partner HTTP handler (state round-trip, verdict with brief, brand leak dropped in plain and disguised
forms, status mapping with capacity vs member limit and no key in logs, allowance and refund, foreign session
404, a refinement continuing on a fresh service instance, refund of every dropped first turn, a dropped
continuation answering "start over" on each retry, short-form brand names caught and ordinary words not, an
unknown outcome dropped, non-web sources removed, a stale local preview as 404, expiry before any upstream call, upstream session
expiry as 404, Production off, local preview that never spends the allowance, one turn at a time per member,
remaining count in status and replies) and pin the partner wire contract with OGAI
(`PartnerContract*`: a state of exactly 512 characters round-trips and 513 fails on the first turn; a community
verdict with vertical `none` keeps its brief). The OGAI twin is `apps/site/server/intake/validate/partner_contract_test.go`
on `partner/ardanova-nova`; change both together. OGAI's real state is 54 characters (8-byte start + 32-byte HMAC,
base64url) and its session id 26, well inside 512 and 64.
