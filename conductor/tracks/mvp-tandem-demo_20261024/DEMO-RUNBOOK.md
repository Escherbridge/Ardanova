---
type: runbook
---

# MVP Tandem Demo — how to run it

**As of 2026-10-25 the whole stack is RUNNING and the golden path has been driven end to
end.** Everything below is for bringing it back after a reboot, or driving it again.

## One command

```powershell
# from c:\Users\atooz\Documents\Escherbridge\ardanova
.omc\scripts\local-stack.ps1 -Action reset-db          # optional: fresh demo data
.omc\scripts\local-stack.ps1 -Action up -WithAzoa      # node + sidecar + SurrealDB + BFF + client
```

`-WithAzoa` runs oasis-sleek's `tools/dev-node/dev-node-up.ps1` (idempotent: SurrealDB
podman :8020 → KYC sidecar :8031/:8032 → WebAPI :5000 → tenant + keys + Identomat profile
→ tenant KYB + KYC → ARDA genesis → `on-ramp` partition) and maps its
`.omc/state/dev-node-env.json` onto the BFF env. Success check:
`curl http://127.0.0.1:5147/health` → `"economy":"simulated"`. Teardown:
`-Action down` here, `tools/dev-node/dev-node-down.ps1` there.

**Caveat — never WAIT on `dev-node-up.ps1`'s process**: its spawned servers inherit the
stdout pipe, so redirect its output to a file and poll health instead.

## The Stripe leg (only needed for Add-funds)

```bash
stripe listen --api-key <STRIPE_SECRET_API_KEY_DEV from oasis-sleek/.env> \
  --all-snapshot --forward-to http://127.0.0.1:5000/api/fiat/webhook/stripe
```

The CLI's signing secret is account-stable and already matches `Fiat__Test__WebhookSecret`.
Pay the hosted page with 4242 4242 4242 4242 (any future expiry/CVC; UNTICK "Save my
information with Link" or it demands a phone number). A listener from the drive session
may still be running (`/c/tmp/stripe-dev/pid.txt`, log `/c/tmp/stripe-dev/listen.log`).

## Personas (no sign-in needed; DEV_AUTH_BYPASS)

Set the cookie `ardanova-dev-persona` to switch identity, e.g. in devtools:
`document.cookie = "ardanova-dev-persona=demo-sponsor; path=/"`. Identities:
`demo-creator` (owner), `demo-sponsor`, `demo-freelancer`; absent cookie =
`local-preview-user` (steward). All three are already KYC-approved on the node (node-side
state survives `reset-db`; after a reset, each persona's first visit to
Settings → Verification re-binds the local account with one click).

## The golden path, as driven

1. **Onboard** — Settings → Verification: Start account setup → Begin verification
   (sidecar auto-approves) → all three steps green.
2. **Create/provision** — as demo-creator on project `solar-micro-grid-commons` →
   Funding tab → Set up funding (name, SOLR, 100000 total / 60000 sponsors / 40000
   workers, $15,000 goal) → tab flips to GATE STATUS: FUNDING with partitions declared.
3. **Sponsor** — as demo-sponsor: Add funds $25 (Stripe leg) → invest page → $25 →
   acknowledgements → SUPPORT → success page → settles to SETTLED (watch
   `FundingIntent.status`); funding tab shows $25 / 59,900 units left.
4. **Work for stake** — as demo-freelancer: opportunity page → apply + Propose paid
   terms (4,000 SOLR) → as demo-creator: Accept bid (hold RESERVED from the contributor
   partition) → task commerce page → Approve completed work → **PAID**.
5. **Verify** — freelancer's portfolio: 4,000 SOLR, window date; project Money view;
   node receipts (`GET /api/exchange/receipt/{tenantUser}:funding-intent:{intentId}` with
   the value key).

## Where the state lives

- Node contract: `oasis-sleek/.omc/state/dev-node-env.json` (keys, ARDA asset id,
  partition); tenant creds `.omc/state/dev-node/tenant.json`; logs `.omc/state/dev-node/logs/`.
- BFF/client PIDs + secrets: `ardanova/.omc/state/local-stack/`.
- Findings ledger + every fix of the bring-up: [plan.md](plan.md) §C2 findings.
- NOTHING IS COMMITTED in either repo — review and commit deliberately (oasis-sleek also
  carries the owner decisions: `.worktrees/main-release` cleanup, F22 peg, F29 reconcile).
