---
type: runbook
title: Nova AI powered by OVERSEASGENAI - operator go-live checklist
status: ready-for-operator
tags: [nova, ogai, partner, rollout, railway, checklist]
updated: 2026-10-18
---
# Nova AI go-live checklist (operator only)

No agent provisions, pushes or deploys any of this. Every step below is a human operator action. The OGAI steps
also belong to the OGAI lease holder (`overseasgenai/conductor/RUNBOOK.md`, "Orchestrator lease"). Tick each box and
record who did it and when.

**Inputs at the time of writing**

| Item | Value |
|---|---|
| OGAI partner branch | `partner/ardanova-nova`, head `2d70a4f`, worktree `Escherbridge/overseasgenai-nova`. Not pushed, not deployed. |
| OGAI Railway | project `overseasgenai`, env `production`, service `overseasgenai-site` (`https://overseasgenai-site-production.up.railway.app`) |
| ArdaNova Railway | project `a876ca12-…`, env `production`, service **Ardanova-BFF** (the .NET API, despite the name) |
| ArdaNova code | Nova AI is already on `main` and ships in Production as `off` until both `Ogai__*` variables are set |
| Partner brand caps (OGAI `partner/brands.go`) | ArdaNova: 400 sessions a day, $12 a day. ArdaNova's own cap is 10 per member per UTC day. |

## Order (why it matters)

1. **OGAI first, ArdaNova second.** ArdaNova with `Ogai__*` set calls `POST /api/partner/v1/validate`. If the
   OGAI route is not mounted, that call falls through to the static site, and every member turn fails as
   `nova_unavailable`. If OGAI is live and ArdaNova is not configured, nothing happens: Nova stays `off`.
2. **Generate the key once.** The raw key goes only to ArdaNova. OGAI stores only its SHA-256.
3. **Merge ArdaNova's `loop4/nova` (or later) before the smoke test.** It refunds dropped first turns, ends the
   endless retry on a dropped continuation, and hides the provider credit outside live mode.

## A. Generate the partner key (operator terminal, never in chat or logs)

- [ ] Generate a raw key with 32+ random bytes, for example
      `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))` in PowerShell.
- [ ] Store it in the operator secret store:
      `scripts/secrets/Set-Secret.ps1 -Name ARDANOVA_PARTNER_KEY` (OGAI repo, hidden prompt).
- [ ] Compute its SHA-256 hex. It must match OGAI `partner.HashKey`, which is the lower-case hex of SHA-256 over the
      UTF-8 bytes of the raw key string:
      `Invoke-WithSecret.ps1 -Names ARDANOVA_PARTNER_KEY -Command { -join ([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($env:ARDANOVA_PARTNER_KEY)) | % { $_.ToString('x2') }) }`
      The hash is not a secret, but keep it next to the key's record.

## B. OGAI (lease holder only)

- [ ] Run the review gate on `partner/ardanova-nova`: `/security-review` covering partner auth, key handling and
      brand isolation. Record the verdict under `overseasgenai/conductor/reviews/`.
- [ ] Merge `partner/ardanova-nova` into OGAI `main`. It must not touch `wave/*` or entitlements; check that
      `git diff main...partner/ardanova-nova --stat` lists only `apps/site/server/partner/**`,
      `apps/site/server/intake/**`, `intake_routes.go` and docs.
- [ ] On service `overseasgenai-site`, set `PARTNER_API_KEYS=ardanova:<sha256hex>`. Only the hash goes here, never
      the raw key. A raw key, an unknown id or a duplicate leaves the route unmounted, and the logs say
      `partner api off`.
- [ ] Confirm that `OPENROUTER_API_KEY` and `INTAKE_HMAC_SECRET` are already set on the site. Partner state is signed
      with `partnerSecret(INTAKE_HMAC_SECRET, "ardanova")`, so rotating that secret ends in-flight Nova sessions,
      and members see "start again".
- [ ] Deploy. `.gitea/workflows/deploy-site.yml` runs on a push to `main` that touches `apps/site/**`. To deploy
      by hand: `railway up apps/site --path-as-root --service overseasgenai-site --ci`.
- [ ] Probe without a key: `POST https://<ogai-host>/api/partner/v1/validate` with no `Authorization` header must
      return **401** `unauthorized`. A 404 or HTML response means the route is unmounted: check the
      `PARTNER_API_KEYS` format.
- [ ] If `ORIGIN_TOKEN` is set on the site (Cloudflare in front), ArdaNova must call the Cloudflare hostname,
      because the edge adds `X-Origin-Token`. ArdaNova does not send that header itself. Calling the bare
      `*.up.railway.app` origin would return 403 on every turn.

## C. ArdaNova (Railway service Ardanova-BFF)

- [ ] Merge the Nova lane branch (`loop4/nova`) to `main` and let CI build both images.
- [ ] On **Ardanova-BFF**, set:
  - `Ogai__BaseUrl=https://<ogai-host>`. It must be https in Production, or the API refuses to start
    (`OgaiRegistration`). Give the origin only, with no path.
  - `Ogai__PartnerApiKey=<raw key>`. Paste it from the secret store. Never put it on Ardanova-Client.
  - Optional: `NOVA_SESSION_KEY=<32+ random chars>`, which gives tickets their own key. Without it, tickets derive
    from `ACTOR_ASSERTION_HMAC_KEY`, which is fine. Rotating either key expires in-flight refinements.
  - Leave `Ogai__TimeoutSeconds` unset (default 95). It is paired with the client's 100 s budget and OGAI's
    roughly 88 s worst case.
- [ ] Redeploy Ardanova-BFF. Nothing changes on Ardanova-Client: the BFF route and the 100 s Nova timeout are
      already there.

## D. Smoke test (signed-in member, Production)

- [ ] The `GET /api/nova/status` result in the sheet reads `live`. The header says "idea and answers go to Nova
      AI", the footer shows the provider credit, and "10 of 10 refinements left today" is visible.
- [ ] **Commercial idea**: run a refinement to a verdict (go or adjust) with a brief. "9 of 10 left" appears after
      the first turn.
- [ ] **Community idea** (for example a neighbourhood tool library): it must also reach a verdict with a brief
      (vertical `none`). It must not end in a 503.
- [ ] Create a draft project from one live brief. It stays a private `DRAFT`.
- [ ] White-label scan: in DevTools, save the raw `nova.refineProject` and `nova.status` response bodies, then
      search them case-insensitively for `ogai`, `overseas` and `og-ai`. Expect zero hits. The only allowed
      mention is the footer credit in the page.
- [ ] Logs: in the Ardanova-BFF logs for the smoke window, search for the raw key and for a fragment of the idea
      text. Expect zero hits for both. Only status codes and failure kinds should appear.
- [ ] Negative check: copy a continuation request from DevTools, change one character of `state`, and replay it.
      Expect the member copy "This refinement has expired. Start again from your idea." (404).

## E. Rollback

- Remove `Ogai__PartnerApiKey` (or `Ogai__BaseUrl`) from Ardanova-BFF and redeploy. Production then serves Nova as
  `off`, and the sheet says "not available on this deployment". No member data is stranded, because nothing is
  stored.
- On OGAI, clearing `PARTNER_API_KEYS` unmounts the partner route without touching the public site.

## F. After launch (tracked in `nova-ai-live-rollout`)

- Alert on `nova_capacity` 503s in the Ardanova-BFF logs, and sign off the 400 sessions / $12 per day numbers
  against expected daily active members. About 40 members using their full 10 fill the platform's day.
- Persist the per-member allowance before running a second API replica.
