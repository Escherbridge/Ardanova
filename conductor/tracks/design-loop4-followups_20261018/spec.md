---
type: spec
---
# Spec: Design Loop-4 Follow-ups

Loop 4 fixed what the design lane owns: the text-safe `accent` token and its
gate rule, the operator-message filter in `describeMutationError`, the Card and
TabsList overflow guards, and the global-error copy. The items below sit in files
other lanes own. Several carry over from
[design-loop3-followups_20261018](../design-loop3-followups_20261018/spec.md).

## Requirements

1. **Retry predicate wired (P0).** `src/trpc/query-client.ts` sets
   `retry: shouldRetryQuery`. A missing project or a missing funding config
   then shows its message after one request (about 0.7 s), not after four
   (about 10.8 s). Loop-3 track §1 also covers this, and it is still not done.
2. **Custody errors use the copy mapper (P1).** `/dashboard/profile` and
   `/settings/verification` pass `custodyStatus.error?.message` and
   `ensureCustody.error?.message` straight to `CustodialAccountCard`, which is
   how "AZOA tenant-bound custodial account onboarding is not configured."
   reached users.
3. **Backend reason code for operator messages (P1).**
   `AzoaCustodialAccountService.cs:167` returns operator text as a user
   Conflict. Return user copy and a machine-readable reason, and keep the
   operator detail in the logs.
4. **Neon buttons meet AA (P2).** `bg-neon … text-black` measures 4.21:1 in the light theme.
5. **Task priority swatches use tokens (P2).** This lowers `RAW_PALETTE_CEILING`.
6. **/people opens on the People tab (P2).**
7. **The vendor is named consistently on /settings/verification (P3).**

## Acceptance

- The `design-gate.test.ts` ceilings drop to match, and `KNOWN_UNDEFINED_TOKENS` and `KNOWN_FILL_AS_TEXT` empty out.
- A flow test renders a missing project through the real query client and sees the not-found copy without backoff.
