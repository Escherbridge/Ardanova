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
  a restart forgets the count, and the OGAI partner quota still binds). A first turn that fails
  upstream is given back.
- Session tickets (`NovaSessionTickets`): the `state` a member gets back is not the upstream's state but
  `nv1.<expiry>.<hmac>.<upstream state>`, with the HMAC over (actor, session, expiry, upstream state). A
  continuation whose ticket is not this actor's live ticket for this session returns NotFound
  `nova_session_not_found` (404) before any upstream call, so a leaked or guessed session id can neither be
  taken over nor used to run turns outside the daily cap. "Not yours" and "expired" are the same 404 on
  purpose: a different answer would tell a prober which ids are real. Ownership lives in the signed state, not
  in memory, so a refinement survives a restart, a redeploy and a second replica. The expiry is set on the
  first turn and carried forward: `SessionLifetime` is 1 h 55 min, inside OGAI's 2 h `sessionLifetime` with
  margin for the first turn's own duration, so ArdaNova expires a session before OGAI would refuse it.
  Without `Nova:SessionKey` or the actor-assertion key (local dev only) the key is random per process, and a
  restart then reads in-flight refinements as expired.
- Brand guard: OGAI already hides its own names; the service additionally drops any reply whose
  member-visible text (vertical included) `NovaBrand.NamesUpstream` (503), and stamps
  `Assistant = "Nova AI"` itself rather than trusting upstream. `HiddenNames` mirrors OGAI's own list for
  this brand (including "ogai"), and the host's name is also matched letters-only (`HiddenLetters`), so
  "Overseas Gen-AI" or a zero-width space inside the name is still caught. A rare false positive (a
  sentence ending "overseas." followed by "GenAI") costs one 503 and a retry, never a leak.
- No member identity, project data or PII leaves ArdaNova: only the idea and answers the member typed.
  OGAI keeps no transcript for partner calls.

## HTTP mapping

| Upstream | Service result | `NovaController` |
|---|---|---|
| 200 question/verdict | Success | 200 |
| 400 `session` (state expired or forged upstream) | NotFound `nova_session_not_found` | 404 |
| other 400 | ValidationError | 400 |
| (none: this member's own 10 a day spent) | Conflict `nova_daily_limit` | 429 |
| 429 `quota`, 503 `spend_closed` (the partner's whole day, shared by every member) | Failure `nova_capacity` | 503 |
| (none: ticket not this actor's, or expired) | NotFound `nova_session_not_found` | 404 |
| 401, 5xx, timeout, malformed, brand leak | Failure `nova_unavailable` | 503 |

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
404, a refinement continuing on a fresh service instance, expiry before any upstream call, upstream session
expiry as 404, Production off, local preview).
