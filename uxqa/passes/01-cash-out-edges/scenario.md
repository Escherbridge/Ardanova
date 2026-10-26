---
type: UX Pass Scenario
pass: "01-cash-out-edges"
scenario: "cash-out-edges"
timestamp: "2026-10-26"
---

# Scenario: Cash-out and the money's edges

## Brief

A first cohort of invited members — some verified, most not — try to
understand and move their money on the released environment: what they have,
how to add to it, how a project gets funded, and above all how money comes
back OUT (withdraw to a bank, selling units, payout settings). The economy's
value rails are deliberately gated right now (economy=disabled), so every
surface's honesty under refusal is itself under test: a member must always
learn what is true, what is pending, and what their next step is — never a
dead end, a fake zero, or jargon.

## Focus

- Feature areas this pass must reach: /portfolio (balances, holdings),
  /portfolio/add-funds, /portfolio/withdraw (destination setup, amount
  validation, sales call-off), /swap, settings/payouts, a project's invest
  page and funding tab, settings/verification (KYC entry states).
- Edge cases to hunt: unverified users on money surfaces; zero/empty states
  vs error states (an unreadable balance must never render as $0.00); amount
  validation (cents parsing, minimums/maximums, more-than-available); refusal
  copy honesty while rails are gated; double-tap/idempotency affordances
  (request keys on withdraw); keyboard-only and 390px flows on the money
  pages.
- Questions carried from earlier passes: none (first pass).

## Out of scope this pass

- Live Stripe checkout completion (rails gated; reaching the refusal honestly
  is in scope, paying is not).
- The AZOA node's own admin/operator surfaces.
- Nova drafting features.
