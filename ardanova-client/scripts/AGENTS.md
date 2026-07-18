# Frontend release automation

`frontend-release-check.mjs` is the canonical local and CI release gate for the Next.js client. It deliberately runs changed-source formatting, the production dependency audit, lint, TypeScript, tests, and the production build as one ordered sweep so a release cannot pass on only one quality signal.

The gate injects deterministic, non-secret loopback fixtures for required runtime configuration. This keeps lint, tests, and builds independent of developer or hosted `.env` files while still exercising the environment schema; deployment readiness performs the real secret, database, and upstream checks.

The release fixtures force normal TLS certificate verification. The application environment schema rejects `NODE_TLS_REJECT_UNAUTHORIZED=0`, so a developer-shell override cannot silently become a production release setting.

`format-changed.mjs` limits the release formatting gate to source changed from `HEAD` plus untracked frontend source. This keeps release diffs reviewable while the legacy tree is normalized incrementally. The broad `format:check` command remains available for repository-wide cleanup work.

Keep the script orchestration-only. Product-specific assertions belong in Vitest or browser smoke checks, while deployment and rollback guidance belongs in `documentation/FRONTEND_RELEASE_PLAYBOOK.md`.

## Local demo data

`seed-local-demo.ts` creates a small, repeatable solutionary workspace for browser QA. It must remain additive and idempotent, use synthetic identities, and pass the shared guard before constructing Prisma. The guard requires a per-run `ALLOW_LOCAL_DEMO_SEED=true` opt-in, a non-production process, a loopback PostgreSQL host, and a dedicated database name matching `ardanova_local_demo` or `ardanova_local_demo_<suffix>`. Never weaken those independent checks to seed a shared, hosted, or locally restored production database.

## Legacy Google account linking

`link-google-account.ts` is an operator-only recovery tool for a user created before immutable provider links were recorded. It accepts only a persisted user ID and a separately verified Google `sub`; it never looks up or links by email. Before running it, an operator must verify the Google ID token signature, issuer, audience, expiry, and subject through an approved identity workflow and confirm the intended ArdaNova user out of band.

The tool requires a per-run `ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK=true` opt-in, refuses subject collisions and users that already have a Google link, and writes inside a serializable transaction. Clear the three linking variables immediately after the run. Do not turn this into an automatic startup migration or a public route.
