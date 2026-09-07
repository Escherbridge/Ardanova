---
type: runbook
---

# ArdaNova Runbook

**Written 2026-09-07.** State of the working tree, the blockers that make "green
locally" untrue in CI, and what is sitting uncommitted. Read this before
believing any checkbox in [`tracks.md`](tracks.md).

- Branch HEAD: `f2e4bc5` — *fix!: cut over to AZOA's renamed user/action contract*
- Working tree: **81 uncommitted paths** (67 modified tracked, 14 untracked) from
  an in-flight security-hardening pass — see §3. **Do not commit them blind.**

---

## 1. Open item — `azoa-sdk@0.3.0` is unpublished, so `npm ci` cannot work

This is the single blocker that makes local success misleading.

**Evidence (all verified 2026-09-07):**

| Fact | Where |
|---|---|
| The client pins `"azoa-sdk": "^0.3.0"` | `ardanova-client/package.json:61` |
| The registry has only `0.1.0, 0.2.0, 0.2.1, 0.2.2` | `npm view azoa-sdk versions --json` |
| The 0.3.0 source exists only locally | `oasis-sleek/sdk/azoa-wallet/package.json` → `"version": "0.3.0"` |
| The lockfile still declares and resolves `0.1.0` | `ardanova-client/package-lock.json:33` (`"azoa-sdk": "^0.1.0"`) and `:4942-4944` (`resolved: .../azoa-sdk-0.1.0.tgz`) |
| Local installs only work through a machine-local symlink | `ardanova-client/node_modules/azoa-sdk -> /c/Users/atooz/Programming/Projects/oasis-sleek/sdk/azoa-wallet/` |
| CI installs with `npm ci` | `.github/workflows/ardanova-client-image.yml:83` |

**Why it fails twice over.** `npm ci` refuses to run at all when
`package.json` and `package-lock.json` disagree (`^0.3.0` vs `^0.1.0`) — it exits
`EUSAGE` before touching the network. Fix only the lockfile mismatch and it then
fails again on the registry, because `0.3.0` does not exist there.

**`npm link` is invisible to git.** The symlink above lives in
`node_modules/`, which is gitignored and machine-local. Nothing about the
"client `tsc` exit 0, 210 tests passing" result recorded in `f2e4bc5` is
reproducible from a clean clone. **Green locally is not green in CI, and this
branch's frontend leg should be treated as red until item 2 closes.**

---

## 2. Open item — regenerate `package-lock.json` once 0.3.0 ships

Ordered, and only in this order:

1. Publish `azoa-sdk@0.3.0` from `oasis-sleek/sdk/azoa-wallet` (AZOA's lane).
2. In `ardanova-client`: `npm install azoa-sdk@^0.3.0` to rewrite
   `package-lock.json` (it must resolve `.../azoa-sdk-0.3.0.tgz`).
3. Commit the lockfile.
4. Verify from a **clean clone with no `npm link`**: `npm ci` then `npm run build`.
   Until step 4 passes, the CI image build stays broken.

Interim option if publishing must wait: unlink and pin the last published
version, accepting that the renamed `user`/`action` SDK exports the client now
calls are absent from `0.2.2` — i.e. the client would not typecheck. There is no
correct halfway state; publish is the fix.

---

## 3. The ~81 uncommitted files (security-hardening pass) — do not commit or modify

Left exactly as found. What they are:

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

> ⚠ **Committing these with `git commit -a` produces a tree that does not
> build.** Seven tracked-and-modified files already reference
> `IAzoaValueEligibilityService` (`OpportunitiesController`, `TasksController`,
> `FundingIntentService`, `TaskCommerceService`, `EconomicOutboxDispatchService`,
> `AzoaAllocationService`, `DependencyInjection`) while the two files that
> *define* it are untracked. `-a` stages modifications but never untracked files.
> Stage the new files explicitly.

---

## 4. Terminology — what the AZOA rename did and did not touch

`f2e4bc5` adopted AZOA's **avatar → user** / **quest → action** rename on the
**contract surface only**.

| Renamed | Deliberately unchanged |
|---|---|
| `/api/user/register` (`AzoaPublicNodeClient.cs:10`) | `IAzoaAvatarService`, `AzoaAvatarService`, `AzoaQuestNodeAdapter`, `AzoaQuestNodeClient`, `IAzoaQuestAuthoringService` |
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
- **Owned by the source lane, not this one:** ten C# files cite the old track
  slugs in doc-comments and now point at directories that no longer exist —
  `DependencyInjection.cs:133,136,137`, `AzoaAvatarDtos.cs:5`,
  `AzoaQuestDtos.cs:7`, `AzoaAvatarService.cs:12`,
  `AzoaQuestAuthoringService.cs:12`, `AzoaQuestSignalService.cs:11`,
  `IAzoaAvatarService.cs:10`, `IAzoaQuestAuthoringService.cs:9`. Update
  `azoa-avatar-onboarding` → `azoa-user-onboarding` and `azoa-quest-authoring` →
  `azoa-action-authoring` in those comments when the source tree is next touched.
  Left untouched here deliberately: several of those files are mid-edit in the
  uncommitted security pass.
