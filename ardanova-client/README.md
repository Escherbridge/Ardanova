# ArdaNova frontend

The ArdaNova frontend is a Next.js workspace for turning attention into accountable action. It is **social media for doing, not doom-scrolling**: people discover a problem, define a solution, and iterate through shared project work instead of consuming an endless generic feed. Its visual system is flat neobrutalism with editorial scale: expressive in public storytelling and quiet in the working application.

See the repository-level [Brand guidelines](../documentation/BRAND_GUIDELINES.md), [Linguistic guide](../documentation/LINGUISTIC_GUIDE.md), [Frontend experience](../documentation/FRONTEND_EXPERIENCE.md), and [release playbook](../documentation/FRONTEND_RELEASE_PLAYBOOK.md) before introducing new layout, color, community activity, AI, transaction, or deployment patterns.

## Run locally

Requirements: Node.js 20+, npm 10+, PostgreSQL, and the environment values described in [`.env.example`](./.env.example).

```bash
npm install
npm run dev
```

The default development origin is `http://localhost:3000`. The repository's full-stack setup and backend port guidance are in the [local smoke runbook](../documentation/LOCAL_DEVELOPMENT_SMOKE.md).

For data-backed browser QA, use a loopback PostgreSQL database named `ardanova_local_demo` (or `ardanova_local_demo_<suffix>`) and run `npm run seed:local-demo` with the per-run `ALLOW_LOCAL_DEMO_SEED=true` opt-in after synchronizing the Prisma schema. The seed is repeatable and uses synthetic identities. See the smoke runbook for the complete sequence.

### Interface preview without Google OAuth

For local UI development only, add this to the frontend's local `.env`:

```env
DEV_AUTH_BYPASS=true
```

The flag is honored only when `NODE_ENV=development`, creates a clearly marked preview session, and does not bypass backend authorization. It is ignored in production by design. Remove it to exercise the real NextAuth/Google sign-in flow, and never configure it in Railway.

## Experience map

- `/` — public ArdaNova story and trust model
- `/auth/signin` — branded authentication entry
- `/dashboard` — current work and decisions
- `/projects` — project discovery and management
- `/studio` — Nova drafting, presentation, and rehearsal interface preview
- `/tasks` and `/opportunities` — work surfaces
- `/people` and `/guilds` — people and collective coordination
- `/dashboard/profile` — identity, contribution history, and explicit Azoa account setup
- `/settings/verification` — Azoa-backed identity verification status and recovery
- `/settings/wallets` — non-custodial external address references, kept separate from Azoa custody
- `/portfolio` — pending and reconciled value
- `/swap` — supported exchange flow with explicit transaction state

Nova is an optional drafting collaborator. It may propose, organize, review, present, and rehearse material. It cannot publish, fund, approve, release escrow, swap assets, issue credentials, pay contributors, or change rights.

Studio starts blank; it never presents realistic sample material as accept-ready
user work. Studio, project-creation, and global Nova preview drafts survive
navigation and refresh within the current browser tab through validated,
versioned, size-bounded `sessionStorage`. They are
not server records, do not sync across tabs or devices, and disappear when the
tab session ends. Corrupt, oversized, or unavailable storage fails visibly.

### Identity and Azoa custody

ArdaNova explicitly provisions one tenant-bound Azoa account per user. Azoa is
authoritative for the avatar, KYC lifecycle, configured key-custody boundary, and managed
chain wallets; ArdaNova stores only thin identifiers and readiness state. Identity,
KYC-provider, and wallet-provisioning readiness are reported independently, so an
operator can enable a reviewed KYC provider while production custody remains
fail-closed. The setup action is convergent and idempotent and never returns
private key material. Creating the account or completing KYC does not approve any
later value-moving action.

The public verification surface supports a reviewed hosted-provider handoff.
It deliberately does not ask people to paste public identity-document URLs.
Reference-based providers require an Azoa-owned private upload, malware-scan,
retention, and scoped-review workflow before ArdaNova may enable that path.

External addresses saved under `/settings/wallets` are a separate,
non-custodial feature. Saving one does not prove control and never converts it
into the Azoa-managed wallet.

### Core product loop

```text
Discover a problem -> Define a solution -> Iterate -> Discover again
```

- **Discover** with affected people, field notes, evidence, constraints, and open questions.
- **Define** a reviewable solution brief with assumptions, roles, milestones, terms, and explicit decision or ownership rights.
- **Iterate** through contributions, work updates, decisions, evidence, exceptions, and revision.

The interface should reveal the current stage and next meaningful move. Social surfaces name concrete objects - field notes, questions, proposals, contributions, decisions, invitations, and work updates - rather than optimizing copy for virality, popularity, or time-on-platform.

Technology is used for **empowering ownership, not surveillance**. Product language names the context and permissions in scope and never infers ownership from activity alone. Any ownership claim must point to explicit, documented rights and the correct draft, submitted, confirmed, or reconciled state.

## Quality commands

```bash
npm run release:check
```

That canonical gate runs these checks in order:

```bash
npm run diff:check
npm run format:changed
npm run audit:prod
npm run lint
npm run typecheck
npm test
npm run build
```

The project uses Next.js 15, React 19, TypeScript, Tailwind CSS 4, Radix primitives, tRPC, NextAuth, Prisma, and Vitest.

Database changes use the immutable snapshot and versioned SQL under
`prisma/migrations`. A fresh database runs `npm run db:migrate`; an established
database must follow the fingerprinted baseline-adoption path in
`documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md`. Never use `db push` or
`migrate dev` on shared or hosted data.

Use `npm run format:check` only for deliberate repository-wide formatting cleanup; the release gate checks changed frontend source so feature diffs remain reviewable.

## Production and Railway

Production is built from [`Dockerfile`](./Dockerfile) using the standalone Next.js output. [`railway.toml`](./railway.toml) configures the Dockerfile builder and the `/api/ready` deployment check. `/api/health` is process liveness only; `/api/ready` additionally checks required runtime configuration and the backend through a bounded request.

Required production configuration includes the database, backend API URL/key, NextAuth secret and URL, OAuth provider credentials, `ADMIN_API_KEY`, and `ACTOR_ASSERTION_HMAC_KEY`. The service, admin, and actor keys are distinct server-only secrets; each must contain at least 32 bytes and must use the same respective value on the frontend BFF and .NET API services. Public variables are compiled into the client at build time; server secrets must remain runtime-only. Do not set `DEV_AUTH_BYPASS` or `NODE_TLS_REJECT_UNAUTHORIZED` in production.

For a coordinated release, deploy and validate the sibling .NET API first, then deploy from this directory with the Railway CLI. Observe terminal success for both services and verify the generated frontend domain, `/api/health`, `/api/ready`, `/auth/signin`, and `/api/auth/providers`. The full validation and rollback sequence is in the [frontend release playbook](../documentation/FRONTEND_RELEASE_PLAYBOOK.md).

## Interaction invariants

- Product journeys reinforce discover -> define -> iterate rather than an endless attention loop.
- Recommendations explain their relevant project, skill, location, or interest basis when known.
- Draft, submitted, confirmed, and reconciled are different states.
- Saving a wallet address does not verify control.
- Completing a task does not prove that an award was reconciled.
- Authorizing escrow release does not prove contributor settlement.
- Membership credentials, project tokens, and ownership shares are not interchangeable.
- Machine-generated material stays a draft until a person reviews and accepts it.
- Azoa KYC, avatar, and wallet readiness are separate states; none is inferred from an ArdaNova profile field.
- ArdaNova never handles Azoa private keys, mnemonics, signing secrets, or raw identity documents.
