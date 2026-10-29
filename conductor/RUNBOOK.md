---
type: runbook
---

# ArdaNova Runbook

**Written 2026-09-07. Revised 2026-09-08 (Wave 0 remediation).** State of the
tree, the blockers that make "green locally" untrue in CI, and the corrections
owed to earlier commit messages. Read this before believing any checkbox in
[`tracks.md`](tracks.md).

- Branch: `codex/azoa-security-hardening`. **HEAD moves — read it, do not trust a
  hash transcribed here:** `git log --oneline -1`. The wave-0 documentation-truth
  commit is `ea9b9d1`; further wave commits have landed on top of it since.
- Working tree: **clean** apart from untracked `.omc/` agent scratch. The
  security-hardening pass that earlier revisions of this file described as
  "81 uncommitted paths — do not commit them blind" was committed by `ea9b9d1`
  itself; see §3.

> ### Correction to `ea9b9d1`'s commit message (recorded 2026-09-08)
>
> That message claimed: *"Restored ^0.3.0 and the lockfile so package.json:61 and
> package-lock.json:33,4942-4944 match what RUNBOOK sections 1-2 already
> document."* **The lockfile half of that sentence is false.**
> `git show --stat ea9b9d1 -- ardanova-client/package-lock.json` returns empty —
> the file is not in the commit — and the lines it cites as proof read `^0.1.0`
> and `azoa-sdk-0.1.0.tgz`, the opposite of the claim.
>
> No control was lost (the tree was already in that state, and §§1–2 have always
> described it correctly), but a reader trusting the message would conclude that
> publishing the SDK alone turns CI green. It does not: the lockfile must be
> regenerated too.
>
> **The prose is no longer what carries this.**
> `ardanova-client/scripts/verify-azoa-sdk-pin.mjs` now checks the claim
> mechanically, at both seams that can act on it — CI runs it **before** `npm ci`
> (`.github/workflows/ardanova-client-image.yml:85`), and it is the first step of
> the release gate (`ardanova-client/scripts/frontend-release-check.mjs:21`).
> Regression tests: `ardanova-client/src/lib/dependencies/azoa-sdk-pin.test.ts`.

> ### Correction to the Wave 0 remediation's own thesis refutation (recorded 2026-09-08)
>
> That remediation refuted a thesis finding by writing: *"The only surface in the
> client that renders a raw wallet address or GUID is `admin/kyc/page.tsx` ... so a
> non-crypto sponsor cannot reach it."* The admin gate half is true and still holds
> (`ardanova-client/src/app/admin/kyc/page.tsx:96` redirects a non-`ADMIN` session
> server-side, and the unbind action re-checks at `:53`). **The refutation was
> scoped narrower than the test it answered, and read as complete.**
>
> The product test names six things, not one: *a wallet address, a mnemonic, a gas
> fee, **a chain name**, a signed challenge, or the word "token" where
> "funds"/"stake"/"units" would do.* Only the address clause was checked. The
> chain-name clause was violated on an **ungated** page —
> `src/app/credentials/[id]/page.tsx`, a `"use client"` component with no role
> check, and the page on which a worker reads the tier they earned. It rendered
> "Blockchain Status", "Minted on Algorand", "This credential is verified on the
> Algorand blockchain", "On-chain data matches platform records", "Algorand
> Standard Asset", "ASA #", raw mint/revoke transaction hashes and a raw creator
> address. The defect predates the wave (`17ec01d` / `37ea5a7` / `f01d71c`); what
> the wave added was the impression it had been swept away.
>
> **The prose is no longer what carries this either.**
> `ardanova-client/scripts/verify-product-vocabulary.mjs` parses every member-facing
> `.ts`/`.tsx` under `src/app` and `src/components` and fails the build when a chain
> name, a wallet, a mint, a hash, a mnemonic, a gas fee or the word "token" appears
> in a span a person can read. Enforced at **both** seams: its own CI step, which
> runs after `npm ci` so TypeScript's parser is available
> (`.github/workflows/ardanova-client-image.yml:94`), and the release gate
> (`ardanova-client/scripts/frontend-release-check.mjs:22`, exposed as
> `npm run verify:vocabulary`). Regression tests:
> `ardanova-client/src/lib/product-vocabulary.test.ts` — including a fixture holding
> the exact copy that shipped, which the guard must reject, and a real-tree case
> asserting it walked >100 files so it cannot pass by scanning nothing.
>
> Fixing it turned up 42 violations across 17 files, not one page: "Token units",
> "TOKEN SUPPLY BREAKDOWN", "Project-token allocation", "Token burns", "Founder
> Burn", "Platform token", "e.g., Web3 Developer Workshop". All are now the
> member's words — units, stake, balance, write-off. The screens read those words
> through `src/lib/credentials/credential-proof.ts`, next to the same-shaped
> `src/lib/commerce/on-ramp-status.ts`.
>
> **What this guard does not cover, stated so it is not read as complete a second
> time:** it scans rendered surfaces only. `src/lib` is excluded, because it mixes
> member copy with operator tooling that must say what it means
> (`lib/commerce/migration-preflight.ts` is a database preflight run from a
> terminal, and legitimately says "on-chain assetId"). The copy modules under
> `src/lib` are covered instead by their own vocabulary tests —
> `lib/commerce/on-ramp-status.test.ts` and
> `lib/credentials/credential-proof.test.ts`. `src/app/admin/**` is excluded because
> `ardanova-client/src/app/admin/layout.tsx:26` is a server component that redirects
> any non-`ADMIN` session, gating the whole segment before a page renders — the
> exclusion is worth exactly as much as that line, and no more. Nothing outside
> `ardanova-client` was scanned: the .NET API, the game SDK and the AI client have
> not been swept for this vocabulary.

---

## 1. Open item — `azoa-sdk@0.4.0` is unpublished, so `npm ci` cannot work

This is the single blocker that makes local success misleading.

> ### Correction to this section's own evidence (recorded 2026-09-08, L0.7)
>
> An earlier revision of this row read *"Publishing is blocked on a credential,
> not on work — `npm whoami` → `E401 Unauthorized`."* **Re-checked and no longer
> true:** `npm whoami` now resolves to `jadeoni-az` — a valid `_authToken` for
> `registry.npmjs.org` is present locally. The remaining blocker is narrower and
> unchanged in substance: `npm view azoa-sdk@0.4.0 version` still 404s, because
> nobody has run `npm publish` from `oasis-sleek/sdk/azoa-wallet` yet. That publish
> is AZOA's lane, not ArdaNova's, and this session's own operating rules forbid
> running `npm publish` or any other command that writes to a remote regardless of
> whether the credential works — so a working `npm whoami` does not shorten the
> path here. **OI-01 stays recorded as blocked-on-operator**, on the publish
> itself rather than on obtaining a token.

**Evidence (re-verified 2026-09-08):**

| Fact | Where |
|---|---|
| The client pins `"azoa-sdk": "^0.4.0"` | `ardanova-client/package.json:62` |
| The registry has only `0.1.0, 0.2.0, 0.2.1, 0.2.2` | `npm view azoa-sdk versions --json` |
| The publishable source is **0.4.0**, and exists only locally | `oasis-sleek/sdk/azoa-wallet/package.json` → `"version": "0.4.0"` |
| Publishing is blocked on the package not existing yet, not (any longer) on a credential | `npm view azoa-sdk@0.4.0 version` → `404 Not Found`; `npm whoami` → `jadeoni-az` (`OI-01`) |
| **Fixed 2026-09-08:** the pin used to read `^0.3.0`. npm's caret is minor-locked below 1.0, so `^0.3.0` means `>=0.3.0 <0.4.0` and could **never** have accepted the 0.4.0 that is the thing being published — publishing would have left CI red with an error blaming the registry | `scripts/verify-azoa-sdk-pin.mjs` `satisfies()`; regression test `azoa-sdk-pin.test.ts` "rejects a caret pin that the version being published cannot satisfy" |
| The lockfile still declares and resolves `0.1.0` | `ardanova-client/package-lock.json:33` (`"azoa-sdk": "^0.1.0"`) and `:4942-4944` (`resolved: .../azoa-sdk-0.1.0.tgz`) |
| Local installs only work through a machine-local symlink | `ardanova-client/node_modules/azoa-sdk -> /c/Users/atooz/Programming/Projects/oasis-sleek/sdk/azoa-wallet/` |
| CI installs with `npm ci` | `.github/workflows/ardanova-client-image.yml:88` |
| CI checks the pin **before** that install | `.github/workflows/ardanova-client-image.yml:85` |

**Why it fails twice over.** `npm ci` refuses to run at all when
`package.json` and `package-lock.json` disagree (`^0.4.0` vs `^0.1.0`) — it exits
`EUSAGE` before touching the network. Fix only the lockfile mismatch and it then
fails again on the registry, because `0.4.0` does not exist there. Both halves
have to close, and `npm run verify:sdk-pin` names whichever is still open rather
than leaving `npm ci` to fail with an error that points at neither.

**`npm link` is invisible to git.** The symlink above lives in
`node_modules/`, which is gitignored and machine-local. Nothing about the
"client `tsc` exit 0, 210 tests passing" result recorded in `f2e4bc5` is
reproducible from a clean clone. **Green locally is not green in CI, and this
branch's frontend leg should be treated as red until item 2 closes.** The
release gate now says so out loud instead of leaving it to this document:
`npm run verify:sdk-pin` (and therefore `npm run release:check`) exits non-zero
while the pin, the lockfile and the install disagree.

---

## 2. Open item — regenerate `package-lock.json` once 0.4.0 ships

> **Superseded 2026-10-26:** `azoa-sdk@0.5.0` is published and pinned; this
> section is history. The next SDK step is 0.6.0 (money/react subpaths), which is
> **blocked on its security fix round** — see the decisions ledger
> (`conductor/tracks/mvp-readiness-loop_20261026/DECISIONS-LEDGER.md`). For the
> lockfile failure mode that is still live, see §2a.

Ordered, and only in this order:

1. Publish `azoa-sdk@0.4.0` from `oasis-sleek/sdk/azoa-wallet` (AZOA's lane).
   Blocked on `OI-01`: the version has never been published (`npm view
   azoa-sdk@0.4.0 version` → 404). Not blocked on the credential any more — see
   the correction above — but still an operator step no agent session may take
   on this branch (network-write commands are disallowed here).
2. In `ardanova-client`: `npm install azoa-sdk@^0.4.0` to rewrite
   `package-lock.json` (it must resolve `.../azoa-sdk-0.4.0.tgz`).
3. Commit the lockfile.
4. Verify from a **clean clone with no `npm link`**: `npm ci` then `npm run build`.
   Until step 4 passes, the CI image build stays broken.

`npm run verify:sdk-pin` reports which of these is still outstanding, in this
order. When it exits 0 from a clean clone, the item is closed — that check is
the closing condition, not a re-reading of this section.

Interim option if publishing must wait: unlink and pin the last published
version, accepting that the renamed `user`/`action` SDK exports the client now
calls are absent from `0.2.2` — i.e. the client would not typecheck. There is no
correct halfway state; publish is the fix. **Do not "fix" this by hand-editing
the lockfile:** there is no integrity hash for a version that was never
published, and a fabricated one fails at install with a checksum error instead
of the honest `EUSAGE` it fails with today.

**OI-04 — recorded here, in the other repo (2026-09-08).** AZOA's
`oasis-sleek/frontend/src/app/(dashboard)/admin/fiat-reversals/page.tsx:27` hand-rolls a
`FiatReversalView` interface that duplicates a type the SDK already exports —
`sdk/azoa-wallet/src/api/client.ts:1807`, re-exported at `sdk/azoa-wallet/src/index.ts:180`,
already present in the built `dist/index.d.ts` that ships today. A `TODO(OI-04)` at that
line names the SDK type to swap in. **This is not the same blocker as OI-01**: that
frontend consumes the SDK via `"azoa-sdk": "file:../sdk/azoa-wallet"` (a sibling directory
in the same repo), not the published npm package, so nothing there waits on the 0.4.0
publish above. What the swap actually needs — and why this pass left a `TODO` instead of
doing it — is a `tsc --noEmit` to confirm several fields that differ optional-vs-nullable
between the two shapes (e.g. `statusReason?: string | null` locally vs. the SDK's
`statusReason: string | null`) don't break callers, and this item's rules forbid running
`tsc`. Track it alongside OI-01 because both are "finish the SDK v0.4.0 story," not because
one is gating the other.

---

## 2a. Client lockfile platform drift (live failure mode)

**Symptom:** Ardanova-Client builds fail on Railway with `npm ci` `EUSAGE` —
`Missing: @emnapi/core@1.11.1 from lock file` — while Railway keeps serving the
last good build, so the site looks fine and nothing alerts. This happened for
**7 consecutive builds (2026-10-26 06:52 → 2026-10-27 17:38)**.

**Cause:** npm on **Windows** never writes the nested, exact-pinned dependencies
of the wasm32 optional bindings (`@rolldown/binding-wasm32-wasi`,
`@tailwindcss/oxide-wasm32-wasi`, …). Any `npm install` on Windows that rewrites
`package-lock.json` drops them; the Linux builder then resolves those bindings to
the hoisted copy (e.g. `@emnapi/core@1.11.2` for a `1.11.1` pin) and refuses.

**Guard:** `npm run verify:lockfile-platform`
(`ardanova-client/scripts/verify-lockfile-platform.mjs`, tested by
`src/lib/dependencies/lockfile-platform.test.ts`, and run by CI before `npm ci`).
It names every binding dependency that would not resolve.

**Fix — regenerate on the builder's platform, never on Windows:**

```powershell
podman run --rm -v "C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-client:/app" -w /app node:20-alpine `
  sh -c "npm i -g npm@^11 && npm install --package-lock-only"
# confirm with the builder's own npm, without writing Linux node_modules over the Windows ones:
podman run --rm -v "C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-client:/app" -w /app node:20-alpine `
  sh -c "npm i -g npm@^11 && npm ci --dry-run"
```

Then commit `package-lock.json` alone and confirm the deploy actually lands:
`/api/health` on the client reports `commit`, and `railway deployment list
--service Ardanova-Client --environment production` shows `SUCCESS`, not the
previous build still serving.

---

## 3. The security-hardening pass — **landed in `ea9b9d1`**, no longer uncommitted

Until 2026-09-08 this section read *"The ~81 uncommitted files — do not commit or
modify"*. That was already untrue when it was last edited: `ea9b9d1` committed
these very files and edited this file in three other places without touching this
heading. Nothing here is outstanding work; the list is kept as a map of what that
commit contains.

**New, untracked — the fail-closed value gate**
- `ArdaNova.Application/Services/{Interfaces/IAzoaValueEligibilityService.cs,Implementations/AzoaValueEligibilityService.cs}`
  — `RequireReadyAsync(userId, action, ct)`: "fails closed unless identity,
  approved verification, avatar, and managed-account evidence are all ready."
- Tests: `AzoaValueEligibilityServiceTests`, `ProjectMemberServiceTests`,
  `SwapServiceTests`, `CollaborationActorBoundaryTests`,
  `OpportunityActorBoundaryTests`, `ProjectMemberValueBoundaryTests`,
  and frontend `base-client.test.ts`, `collaboration-identity.test.ts`,
  `opportunity-identity.test.ts`.
- `.omc/artifacts/`, `.omc/notepads/`, `.omc/state/` — agent scratch state, not
  product code.

**Modified, tracked**
- 11 controllers and 8 application services wired to the new eligibility gate,
  plus economic-outbox lease/dispatch changes and actor-boundary test updates.
- Client: `base-client.ts`, tRPC routers (`chat`, `notification`, `opportunity`,
  `swap`), `server/auth/config.ts`, `project-create-contract.ts`, several pages
  and components, `.env.example`, `env.js`.

> ✅ **Resolved.** The staging hazard this section used to warn about — seven
> tracked files referencing `IAzoaValueEligibilityService` while the two files
> that *define* it were untracked, so `git commit -a` would have produced a tree
> that does not build — no longer applies: `ea9b9d1` staged the new files
> explicitly and the interface is tracked. Kept here because the failure mode
> recurs whenever a new interface and its callers land in one pass.

---

## 4. Terminology — what the AZOA rename did and did not touch

`f2e4bc5` adopted AZOA's **avatar → user** / **quest → action** rename on the
**contract surface only**.

| Renamed | Deliberately unchanged |
|---|---|
| `/api/user/register` — **no longer called from ArdaNova production code.** The `AzoaPublicNodeClient` transport and the whole self-sovereign path were deleted in Wave 0; the route survives only in the E2E fixture (`AzoaGoldenPathE2ETests.cs:240`). | `AzoaQuestNodeAdapter`, `AzoaQuestNodeClient`, `IAzoaQuestAuthoringService`. `IAzoaAvatarService`/`AzoaAvatarService` were **deleted**, not merely left unrenamed. |
| `/api/action*` (`AzoaCustodialNodeClient.cs:78` — `QuestPrefix = "/api/action"`) | `User.azoaAvatarId` (`database-architecture.dbml:84`) |
| `POST /api/allocation/{userId}` | `Azoa:QuestApiKey`, `ArdaNova.Application/Azoa/Quests/` |
| Wire field `userId` — `[JsonPropertyName("userId")] public string? AvatarId` in `AzoaCustodialAccountGateway.StatusWire` | |

The `[JsonPropertyName]` pin is the whole point of the commit: without it,
`System.Text.Json` silently yielded `null` for the renamed field, tripping the
readiness invariant and failing every money-moving action closed with no error
surfaced anywhere.

**Track slugs were renamed** to match contract §10:
`azoa-avatar-onboarding` → `azoa-user-onboarding`,
`azoa-quest-authoring` → `azoa-action-authoring`.

---

## 5. Contract sync

`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md` must be **byte-identical** in both repos.
It had drifted again after `f2e4bc5`: AZOA landed `c089268` (article agreement)
and `d58d14b` (`ActionDefinition` suffix leaks + stale path citations) on its
copy only. Resynced from AZOA's copy on 2026-09-07; `diff` is now empty.

Check it whenever either side changes:

```sh
diff oasis-sleek/conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md \
     ardanova/conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md
```

---

## 6. Do not plan against these AZOA primitives yet

Two AZOA Phase-5 primitives are **committed but not enabled**, with design tracks
dated `20260906`:

- **Governance** — the tally counts raw holons, which is a live Sybil hole.
- **Credential revocation** — inert.

Neither is usable from ArdaNova. Nothing in this tree should describe them as
available. (ArdaNova's own credential `REVOKED` status in tracks 01/07 is a
separate, ArdaNova-side concept — don't conflate them.)

**Governance is off-chain by decision, not just by readiness (Q6, 2026-09-07).** Even once
AZOA's on-chain governance primitive is hardened (`W9.2` in
`ardanova-production-readiness-waves_20260907`), ArdaNova does not plan to move its vote of
record onto it: ArdaNova **is** its own vote of record, one member one vote, computed
server-side from the authenticated actor and a held membership credential (`W9.1`). AZOA
governance therefore stays gated from ArdaNova's path permanently, not provisionally — see
`conductor/product.md` "Governance."

---

## 7. Known documentation debt

- Most tracks `01`–`23` still lack OKF frontmatter (`type:`). Per convention it
  is added lazily when a file is touched; a bulk rewrite was deliberately not
  done. Newly touched files (`product.md`, `index.md`,
  `azoa-provider-adapter/spec.md`) now carry it.
- Checkbox drift is widespread outside Phase E: the AZOA tracks were reconciled
  against disk on 2026-09-07, tracks `01`–`23` were **not**.
- `conductor/.conductor_session_log` stops at 2026-06-28 and no longer reflects
  edits to this tree.
- **Owned by the source lane, not this one:** seven C# files cite the old track
  slugs in doc-comments and now point at directories that no longer exist —
  `DependencyInjection.cs:135,136`, `AzoaQuestDtos.cs:7`,
  `AzoaQuestNodeAdapter.cs:17`, `AzoaQuestAuthoringService.cs:12`,
  `AzoaQuestSignalService.cs:11`, `IAzoaQuestAuthoringService.cs:9`,
  `IAzoaQuestSignalService.cs:8` — the exact set returned by
  `grep -rl "azoa-avatar-onboarding\|azoa-quest-authoring" --include=*.cs`.
  (`AzoaAvatarDtos.cs`, `AzoaAvatarService.cs`
  and `IAzoaAvatarService.cs` were on this list until Wave 0 deleted the files
  outright, so their citations are gone rather than fixed.) Update
  `azoa-avatar-onboarding` → `azoa-user-onboarding` and `azoa-quest-authoring` →
  `azoa-action-authoring` in those comments when the source tree is next touched.
  Left untouched here deliberately: several of those files are mid-edit in the
  uncommitted security pass.
