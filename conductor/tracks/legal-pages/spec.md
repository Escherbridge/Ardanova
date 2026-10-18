---
type: spec
status: proposed
priority: P1
source: loop1 design QA adversarial critique (2026-10-17)
---
# Terms and Privacy Pages

## Problem

Sign-up and add-funds flows exist, but there is no `/terms` or `/privacy`
route and nothing in the product links to either. Users are asked to create
accounts and move value without a published agreement or data notice.

## Why this is a track, not a loop fix

The page content is a legal commitment about data handling, custody (the Azoa
boundary), funding risk and dispute handling. It must be written or approved
by a human owner, not drafted by an agent as filler. The design lane owns the
route shells (`ardanova-client/src/app/{terms,privacy}/`) and the footer links.

## Outcome

- `/terms` and `/privacy` render approved text in the public-shell design
  (reuse `components/brand/status-page.tsx` header and the landing footer).
- Landing footer, sign-up form and add-funds confirmation link to both.
- Each page shows a "last updated" date sourced from the approved text.

## Acceptance

- Both routes return 200 with per-page `<title>` ("Terms of use — ArdaNova",
  "Privacy notice — ArdaNova").
- Sign-up cannot complete without the terms link being visible next to the
  submit control.
