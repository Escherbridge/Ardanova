---
type: product
---

# ArdaNova Product Definition

**ArdaNova** is social media for doing: people present a problem and a solution with as much
clarity and context as possible, then ask the community to sponsor it — bringing in workers to
do the work for stake — while a real, spendable currency (ARDA) supports an internal economy,
without turning the platform into a blockchain product. Blockchain is the accountability layer
beneath the experience, never the pitch: a sponsor should never need to know a chain name, see a
wallet address, or hear the word "gas."

## Architecture ownership
Four tiers, four owners: the **client** (`ardanova-client/`, Next.js) owns the user experience;
the **.NET backend** (`ardanova-backend-api-mcp/`) owns the HTTP API and the MCP server —
business logic, authorization, and the only tier holding Azoa credentials; **Azoa** (external
node, separate repo) owns financial workflows — custody, minting, exchange, cash-out; **Nova
AI** (`ardanova-ai-client/`, the AI orchestration service) operates the platform assistant. See
[`tech-stack.md`](tech-stack.md) for the technical breakdown.

## Core Value Proposition
- **The solutionary loop**: discover a problem, define a solution, iterate — evidence-rich work
  over passive engagement.
- **Sponsorship with skin in the game**: a sponsor spends ARDA to fund a project and receives
  that project's own token in return; workers are brought in to do the work for stake in the
  same per-project token.
- **A real internal currency, not a stablecoin lookalike**: ARDA is the one thing every user
  buys, holds and spends — see "How value moves," below. There is no separate on-platform
  stablecoin leg.
- **Community-run decisions, off the chain**: proposals, voting and treasury policy are decided
  by ArdaNova's own vote of record, not by an on-chain governance contract — see "Governance."

## How value moves (D44, D47)
Fiat is only the on-ramp and off-ramp for ARDA — never a way to sponsor a project directly.
1. **Fiat → ARDA.** A sponsor buys ARDA with a card. ARDA is a real Algorand-native asset the
   sponsor's custodial wallet holds; the wallet stays invisible, the ARDA balance does not.
2. **ARDA → project token.** Spending ARDA to fund a project moves already-issued units of that
   project's fixed-supply ASA out of a platform reserve into the sponsor's wallet
   (`IssueUnits`, never a per-pledge `Mint`). A stake is a **real per-project ASA from day one**,
   not a ledger row promising one later.
3. **Project token → ARDA.** Once a holder's tokens are liquid (past their project's gate and
   any hold window), they trade the project token back for ARDA through the same reserve-backed
   exchange — a composed set of value-movement steps under ArdaNova's own rate, never an AMM or
   a Tinyman pool.
4. **ARDA → fiat.** ARDA past its on-ramp lot's reversal window cashes out to a bank account.

Every leg above happens on the AZOA node ArdaNova is a tenant of; ArdaNova decides the rate and
the terms, AZOA moves the value under its own custody. See
[`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](ARDANOVA-AZOA-INTEGRATION-CONTRACT.md).

## Key Features
- **Projects**: The core unit of work. Can be temporary, long-term, or foundation-based.
- **Guilds**: Collectives of skilled workers (e.g., "React Developers Guild") that can bid on
  projects or mentor members.
- **Opportunities**: Job listings and tasks that users can apply for.
- **Membership Credentials**: Soulbound, non-transferable governance credentials with on-chain
  verification via Algorand ASAs, scoped to projects or guilds. A credential grants a vote in
  ArdaNova's own vote of record — never in AZOA's on-chain governance (see "Governance").
- **Credential Tiers**: BRONZE → SILVER → GOLD → PLATINUM → DIAMOND progression for projects and
  guilds, tracked on-chain.
- **Identity & KYC**: full document KYC gates **value**, not creation — sponsoring, working for
  stake, or receiving a project token all require it; drafting or browsing a project does not.
  The custodial wallet stays invisible; the identity check never does. See "KYC and business
  verification."
- **Tokenomics & Project Equity**: real per-project ASAs (stakes) issued from a platform reserve;
  ARDA is the single platform currency that bridges every project token to fiat. No separate
  stablecoin leg, no "ARDA is internal-only" framing — ARDA is what the user sees.
- **Game SDK**: Integration for play-to-earn mechanics.
- **Nova AI (Gamma API + MCP)**: AI-powered project generation and management, via the AI
  orchestration service — implemented, not yet wired into the product (see `tech-stack.md`).

## User Roles
- **Innovator**: Project creators and founders.
- **Contributor**: Freelancers and workers.
- **Supporter**: Investors and community backers.
- **Guild Member**: Part of a specialized skill group.

## KYC and business verification (D46, gotcha 41)
Every value action — sponsoring, working for stake, or owning a project that will receive funds
— needs full document KYC **and** a provisioned custodial wallet, uniformly; there is no
lighter-touch tier for a small sponsor. Compliance policy (which provider, what trust profile,
AML screening) is set by the AZOA node operator, not chosen per tenant. **KYB stance: accepted
as natural-person founders only, for now.** The platform does not screen a project owner's
business as a legal entity at creation or funding time; a Stripe Connect business-verification
step is deferred to that founder's first cash-out, the one moment paperwork is actually expected
— not the moment of funding a project.

## Governance (Q6, off-chain by decision)
ArdaNova is the **vote of record**. Proposals, quorum, and one-member-one-vote resolution are
computed and enforced by ArdaNova's own backend from the authenticated actor and a held
membership credential — never inferred from a token balance. This is a permanent architectural
decision, not a stopgap: AZOA ships an on-chain governance primitive, but ArdaNova does not plan
to move its vote of record onto it even once that primitive is hardened. Today it also is not
usable — its tally counts raw holons, a live Sybil hole — so it stays gated from ArdaNova's path
on both grounds.

## Blockchain operations

Blockchain primitives and key custody are delegated to an **AZOA node**, not
implemented in ArdaNova. The canonical boundary is
[`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](ARDANOVA-AZOA-INTEGRATION-CONTRACT.md);
that file is byte-identical with AZOA's copy and must stay that way. AZOA's
domain nouns are **user** and **action** (renamed from avatar/quest; ArdaNova cut
over in `f2e4bc5`). ArdaNova is a **tenant** on that node, never its operator: the
node operator owns custody, compliance policy, and completeness (D48); ArdaNova
provisions its own users as tenant-custodial child users and never holds a key,
a seed phrase, or a payment-provider secret.
