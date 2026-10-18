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
| `Ogai:TimeoutSeconds` | `Ogai__TimeoutSeconds` | default 28, under the BFF client's 30 s so .NET answers first |

`OgaiRegistration.AddOgai` selects the client:

- both set: `OgaiClient`, a typed HttpClient with the bearer key as a default header, redirects off,
  and every logged header redacted (`RedactLoggedHeaders`). Logs carry status codes and failure
  kinds, never the idea, answers or key.
- unset outside Production: `LocalNovaPreviewClient`, three fixed practice questions and a
  `preview` outcome. Responses carry `mode: "local-preview"` and say plainly that nothing was
  analysed, so local dev and tests work without pretending to be AI.
- unset in Production: `DisabledOgaiClient`; Nova AI answers 503 rather than silently faking.

## Service rules (`NovaOrchestrationService`)

- Input mirrors the upstream form (idea 20-2000 chars, at most 8 turns, answers 1-1500), so a refusal
  never spends a session or a model call.
- `NovaSessionAllowance`: at most 10 new sessions per member per UTC day (in memory, single replica;
  a restart forgets the count, and the OGAI partner quota still binds). A first turn that fails
  upstream is given back.
- Session binding: every successful turn records `(actorId, sessionId)` in the allowance (6 h
  lifetime). A continuation whose session this actor was not issued returns NotFound
  `nova_session_not_found` (404) before any upstream call, so a leaked or guessed session id can
  neither be taken over nor used to run turns outside the daily cap. Keyed by the pair because the
  local preview derives session ids from the idea text. In memory like the counter: a restart makes
  in-flight refinements read as expired.
- Brand guard: OGAI already hides its own names; the service additionally drops any reply whose
  member-visible text contains `NovaBrand.HiddenNames` (503), and stamps `Assistant = "Nova AI"`
  itself rather than trusting upstream.
- No member identity, project data or PII leaves ArdaNova: only the idea and answers the member typed.
  OGAI keeps no transcript for partner calls.

## HTTP mapping

| Upstream | Service result | `NovaController` |
|---|---|---|
| 200 question/verdict | Success | 200 |
| 400 | ValidationError | 400 |
| 429 `quota`, or member allowance spent | Conflict `nova_daily_limit` | 429 |
| (none: continuation of a session not bound to this actor) | NotFound `nova_session_not_found` | 404 |
| 401, 5xx, timeout, malformed, brand leak | Failure `nova_unavailable` | 503 |

A 429 that is not `quota` (OGAI's turn-in-flight) maps to 503 so the member simply retries.

Tests: `tests/ArdaNova.API.Tests/Controllers/NovaControllerFlowTests.cs` drive the controller with a
fake partner HTTP handler (state round-trip, verdict with brief, brand leak dropped, status mapping
with no key in logs, allowance and refund, foreign session 404, local preview).
