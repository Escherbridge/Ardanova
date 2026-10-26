---
type: UX Synthesis
pass: 01-cash-out-edges
scenario: cash-out-edges
title: "Pass 01 synthesis: cash-out and the money's edges (re-run with sessions)"
lenses: [service-designer, accessibility, domain-expert, frontline-worker, learning-designer, content-designer, trust-behavioural]
metrics: uxqa/passes/01-cash-out-edges/ux-report.json
generated: 2026-10-25
---

# Pass 01 synthesis: cash-out and the money's edges

Method: `reference/research-panel.md` § Synthesis. Inputs were 7 lens reports (58 raw findings) and `ux-report.json`. Overlapping findings were merged into 25 synthesis findings (S-01..S-25). Each one is a **hypothesis**. Scores follow `impact x frequency x segmentBreadth`. Where lenses scored the same finding differently, the score used is stated and the disagreement is kept next to it.

## 1. Summary

1. **None of the money-moving jobs completed.** Three of the four gold tasks sat at 0% success (add-funds, withdraw, sell-units). Only see-balance passed. The controls that should move money (Continue on add funds, the withdraw amount and Send) are disabled or never enable, and they look like they are still loading rather than switched off (S-01, 27).
2. **The product contradicts itself about cash-out.** /swap says money "can be sent to your bank whenever you like". Withdraw, add-funds and payouts say it is not available. On a financial product this is a false promise, not just inconsistent copy (S-02, 18).
3. **Error recovery and the creator-to-sponsor hand-off are broken at the service layer.** "Try again" never recovered on /projects, /portfolio or /swap. Project CRUD returned 500 for every role, so the funding hand-off and the invest page were never reached (S-03, S-04, both 18).
4. **New users cannot build a correct picture of the product.** The signed-out home page and Workspace Home never mention money. ARDA, units, settlement, Nova and Azoa are never defined. Member-facing settings use operator language ("tenant-bound custodial account", "policy-gated") (S-05, S-06, S-07, S-20).
5. **Strength to keep: the honest refusal voice.** Copy like "This is not a zero balance — nothing has changed" never fakes a $0 or a fake success. The trouble starts where that voice breaks: the raw generic error on /swap, the timing-dependent fake zero on /projects, and the enabled "Start account setup" that does nothing visible (S-13, S-19, S-08).

## 2. Affinity map

Each theme lists its merged findings and the lenses that raised them. Contradictions between lenses are kept visible.

### Theme A: Money cannot move, and the UI does not say whether it is off or still loading
- **S-01** Disabled or dead primary money CTAs on add-funds and withdraw. Rolls up the 0% gold tasks. Lenses: frontline (27), trust (27), domain-expert (F2: 12 + gold rule; F9 rollup: 27).
  - *Disagreement:* domain-expert says the 0% may be **intentional**, because the page says "not switched on for this deployment", so the feature may be off rather than broken. Frontline and trust say that even if it is intentional, the presentation (an infinite spinner, a disabled 0.00 field, no feature-off message) is the defect. Triage has to settle which it is before anyone writes a fix.
- **S-17** Keyboard-only users get no cue when Tab skips the disabled withdraw field. Lenses: accessibility (2), frontline (3, held at MEDIUM).
  - *Disagreement:* frontline treats this as a keyboard exclusion (impact 3). Accessibility says the field is correctly disabled for everyone, and the only gap is the missing cue (impact 1).
- **S-18** Double-submit safety on withdraw is untested. Fay's run ended after 3 s. Lenses: frontline (4), trust (9). This is a **research question**, not an observed defect.

### Theme B: The product contradicts itself about money
- **S-02** /swap promises cash-out "whenever you like", while /portfolio/withdraw, /portfolio/add-funds and /settings/payouts say it is unavailable. Lenses: domain-expert (27), trust (18), learning (12), content (12, framed as "product-wide vs this deployment" scope), service (4).
  - *Disagreement:* service-designer scored this minor (4: one journey). Domain-expert and trust scored it critical, because it is a factual claim about money that can push someone to add funds.
- **S-10** VERIFIED badge vs "Identity check: not started". Lenses: service (4), learning (2), content (2).
  - *Disputed:* trust-behavioural checked the current screenshots. The role chip reads **MEMBER**, not VERIFIED, for both Gus and Ana. The claim rests only on lane summaries that may be stale. Confidence is low, and the finding may already be fixed.

### Theme C: Errors, retries and empty-vs-error truth
- **S-03** "Try again" never recovers, and it retries identically with no backoff or state change. Seen on /projects (4 tries), /portfolio (5 tries) and /swap (3 tries in 3 s). Lenses: service (27), frontline (18).
- **S-13** /swap's "What you hold" shows a raw "An error occurred processing your request." instead of the house honest-error voice. Lenses: content (12, critical by the gold-task rule). It overlaps S-03, since the same failing read is behind gold-sell-units.
- **S-19** Empty-vs-error honesty depends on timing. An early /projects capture shows a spinner plus a fabricated "0 projects / $0". A later capture shows an honest "PROJECT QUERY FAILED". Lenses: service (12). PREBAKE-2 rated its own confidence medium.

### Theme D: Service hand-offs break across roles
- **S-04** The project service returns 500 on create, getAll, getById and getMyProjects for every role. The creator cannot publish, the sponsor cannot find a project, and /projects/[slug]/invest was never reached. Lenses: service (18). Domain-expert (F8) and content flag the downstream coverage gap.
- **S-23** The no-equity disclosure ("not a share, a loan, or a promise of a return") appears only on add-funds. Whether it is shown at the invest decision point is unknown. Lenses: domain-expert (4, low). This is blocked by S-04.

### Theme E: Discoverability and the mental model (where money lives, what it is)
- **S-05** The signed-out home page uses only civic, mission language and has no route into money. It is the single highest-friction route (339). Lenses: content (27), learning (18), frontline (18, the / and /auth/signin funnel), accessibility (6).
  - *Disagreement:* accessibility notes this may be an intentional public-vs-authenticated boundary. Its validation plan is designed to tell "architecture" apart from "a11y bug".
- **S-14** The screen-reader variant: no announced link is named with money vocabulary, so the auditor had to type /portfolio. Lenses: learning (3), accessibility (part of its F3). Held at **MEDIUM** under the accessibility exception.
- **S-20** Workspace Home's Discover/Define/Iterate narrative never mentions balance, portfolio or payouts. Lenses: learning (12).
- **S-09** /portfolio never shows a "verify your identity to unlock this" prompt. Lenses: service (4), frontline (8), learning (8), trust (8). The evidence is a single run (qa-sponsor-ana, 3 failed tries).

### Theme F: Vocabulary, jargon and register
- **S-06** Core money terms are undefined on /portfolio: ARDA with no unit, and the rate "$1 = 100 ARDA" appears only on add-funds; units; settlement; "SUBMITTED ≠ CONFIRMED ≠ RECONCILED"; Nova, Nova Studio, Azoa and AZOA with no stated relationship. Lenses: domain (18), learning (F5: 18, F7: 4), content (12), trust (12).
- **S-07** Operator and compliance copy on Settings > Verification and Payouts, and no plain-language account anywhere of how raised money reaches a creator. Lenses: domain (F4: 8, F6: 2), content (8), trust (6), service (2).
- **S-22** Withdraw page structure: "STEP TWO OF TWO" sits beside an unrelated "STEP ONE / SALES STILL GOING THROUGH", and the same "not switched on" sentence appears three times with no timeline. Lenses: content (8 and 9, merged).
- **S-16** Locale: "1,50" and "1 000" are rejected with a US-only example, and the page is unlocalised under fr-FR. Lenses: domain (4), content (8).

### Theme G: Feedback on actions, and defaults
- **S-08** "Start account setup" is enabled while the sibling "Begin verification" is disabled for the same not-configured gate. Clicking it fires `POST azoaCustodialAccount.ensure`, and the page stays pixel-identical even after "Check status" and a reload. Lenses: all of them except accessibility (service 4, domain 6, frontline 8, learning 4, content 4, trust 8).
  - *Disagreement:* domain-expert rates impact 3, because a real mutation fired with no feedback. The others rate it 2.
- **S-21** Add funds opens with $25 preselected and "Continue — $25" already in the button. That anchors an undecided user toward a higher first amount. Lenses: trust (12, medium).

### Theme H: WCAG conformance
- **S-11** color-contrast (1.4.3 AA, serious) fails on 9 routes, probably from one reused eyebrow or caption token. Lenses: accessibility (12).
- **S-12** aria-prohibited-attr (4.1.2 AA, serious) on / (1 node, reproduced twice) and /settings/verification (3 nodes). Lenses: accessibility (3, held at MEDIUM).
- **S-15** Amount errors are role=alert, but the input never sets aria-invalid, and aria-describedby points only at the help text. Lenses: accessibility (4). The source is narrative only, backed indirectly by a screenshot.
- **S-24** At 200% zoom the sidebar collapses and the menu has to be reopened at each step. Lenses: accessibility (2, single run).
- **S-25** Whether loading or busy states are announced on a slow network is unverified. Lenses: accessibility (1, research question).

### Cross-lens data-quality flags
- **Eli "withdraw did not navigate":** service, frontline and trust each found that Eli's ux.json shows a clean click-to-/portfolio/withdraw navigation in 254 ms, with workarounds 0 and outcome COMPLETED. The telemetry overrides the lane-summary prose, so **no finding was filed**. The content lens still listed it as out of scope for other lenses. Treat it as unsupported.
- **Fay's run:** ABANDONED at 3 s with zero steps. This is a probable harness or browser-close issue, not a product defect. Its screenshot is still valid evidence for static copy (S-22).
- **Possibly stale lane summaries:** trust notes that ux-report.json, personas.json and prebake.json are locally modified, and that a `review findings` fix commit landed after the summaries were written. Findings that rest only on a summary (S-10, parts of S-03 and S-07) carry lower confidence.
- **httpErrors = 0** in totals, even though PREBAKE-1 recorded 500s. The 500s seem to surface only as console errors (283). This looks like an instrumentation gap in the friction metrics.

## 3. Friction heatmap narrative

Totals: friction 1012, 82 failed attempts, 12 workarounds, 283 console errors, 30 axe violations, 0 rage or dead clicks. Coverage was 14 of 53 page routes (26%).

| Route | Friction | What people were trying to do, and what happened |
|---|---|---|
| `/` | 339 (4 abandons, 4 workarounds, 136 errors, 21 revisits) | Signed-out people arrived wanting "my money". The page talks about loops, records and legible power, and has no money link, either visible or announced. Three independent runs (sighted 390px slow-network, accessibility-tree, signed-out screen reader) gave up or typed /portfolio directly (S-05, S-14). |
| `/portfolio` | 226 (2 abandons, 30 revisits, 100 errors) | People wanted to see their balance and then act on it. The balance read failed repeatedly, "Try again" never helped (Ben gave up after 5 tries), the unit (ARDA) was undefined, and nothing pointed to verification as the unlock (S-03, S-06, S-09). It was revisited 30 times because it is the hub every journey bounces back to. |
| `/portfolio/withdraw` | 211 (4 abandons, 83 errors) | People wanted to take $1 out. The amount field was `<input disabled>` and the bank-details panel spun forever. The gold sponsor spent 183 s across 3 timeouts, and keyboard-only Chidi could never focus the field. The copy repeats "not switched on" three times (S-01, S-17, S-22). |
| `/projects` | 166 (4 abandons, 62 errors) | Creators were trying to find or publish their project, and sponsors were trying to find one to back. The project service returned 500. Sometimes it showed a fake "0 projects / $0", later an honest error, and Try again never recovered (S-04, S-19, S-03). |
| `/auth/signin` | 94 (8 workarounds, 50 revisits) | This is where 8 of the pass's 12 workarounds happened. Signed-out journeys were sent here repeatedly instead of to the destination (S-05, frontline). Pass 01's own fixes (ca1be70) added "stated post-login return destination" copy, and the next pass should check whether that reduced revisits. |
| `/dashboard` | 89 (44 errors) | This was the first post-login screen. Its narrative never mentions money, and at 200% zoom the sidebar is hidden (S-20, S-24). |
| `/portfolio/add-funds` | 84 (1 abandon, 14 revisits) | People wanted to add $5–$25. Valid amounts left Continue disabled behind "We couldn't check your account… try again shortly", with no retry control. "1,50" and "1 000" were rejected, and $25 was preselected (S-01, S-16, S-21). |
| `/swap` | 55 (1 abandon) | People wanted to see what they held and sell it. "What you hold" showed a raw generic error, and the page promised cash-out "whenever you like" (S-02, S-13, S-03). |
| `/settings/verification` | 18 | Low friction, but the trust impact is high. "Start account setup" fires a mutation and shows nothing, and the page uses operator language (S-08, S-07, S-12). |

## 4. Heuristic violations (Nielsen)

| # | Heuristic | Route(s) | Evidence | Finding / severity |
|---|---|---|---|---|
| 1 | Visibility of system status | /portfolio/add-funds, /portfolio/withdraw | gold-sponsor ux.json: 3x timeouts on a disabled CTA; withdraw-start.png shows an infinite spinner | S-01 / 27 critical |
| 1 | Visibility of system status | /settings/verification | ana ux.json: ensure-mutation, then 05/06 png are pixel-identical | S-08 / 8 serious |
| 1 | Visibility of system status | /projects | PREBAKE-2; gus-projects.png shows a fake $0 | S-19 / 12 serious |
| 2 | Match with the real world | / | anon--ux-empty-vs-error-truth-1/00-home.png; heatmap / = 339 | S-05 / 18 critical |
| 2 | Match with the real world | /portfolio | eli-portfolio-load.png ("SUBMITTED ≠ CONFIRMED ≠ RECONCILED", ARDA) | S-06 / 18 critical |
| 2 | Match with the real world | /settings/verification, /settings/payouts | gus 03-payouts.png, 04-verification.png | S-07 / 8 serious |
| 2 | Match with the real world | /portfolio/add-funds | hana 02-add-funds-after-locale-typing.png ("1 000" rejected) | S-16 / 4 minor |
| 4 | Consistency and standards | /swap vs /portfolio/withdraw, /settings/payouts | ben 04-sell-units.png vs 03-withdraw.png | S-02 / 18 critical |
| 4 | Consistency and standards | /settings/verification | 04-verification-entry.png: enabled vs disabled CTA for one gate | S-08 / 8 serious |
| 4 | Consistency and standards | /portfolio/withdraw | fay-withdraw-page.png: two step sequences | S-22 / 8 serious |
| 5 | Error prevention | /portfolio/withdraw | fay ux.json: double-submit never ran | S-18 / 9 research question |
| 6 | Recognition rather than recall | /portfolio | ana ux.json: "no verification prompt visible" x3 | S-09 / 8 serious |
| 6 | Recognition rather than recall | / (AT) | ivo-explore ux.json: 4x getByRole timeouts; typed URL | S-14 / 3 MEDIUM (a11y exception) |
| 7 | Flexibility and efficiency | /auth/signin, /dashboard (zoom) | heatmap: 8 workarounds; hana zoom steps | S-05, S-24 |
| 9 | Recognise, diagnose, recover from errors | /projects, /portfolio, /swap | gold-worker sell-units ux.json; ben amounts ux.json | S-03 / 18 critical |
| 9 | Recognise, diagnose, recover from errors | /swap | swap-t2-settled.png: raw generic error | S-13 / 12 critical (gold rule) |
| 9 | Recognise, diagnose, recover from errors | /projects, /dashboard | prebake.json PREBAKE-1 (500s) | S-04 / 18 critical |
| 9 | Recognise, diagnose, recover from errors | /portfolio/add-funds | ben 02-add-amount-_1.png (no aria-invalid) | S-15 / 4 minor |
| 10 | Help and documentation | /portfolio/withdraw, /settings/payouts | repeated sentence, no timeline; payouts written for operators | S-22, S-07 |

WCAG items with no Nielsen mapping: S-11 (1.4.3 contrast, 9 routes, 12 serious) and S-12 (4.1.2, MEDIUM).

## 5. JTBD gaps

| Job users hired ArdaNova for | Status this pass | Findings |
|---|---|---|
| Put money in so I can back a project | **Not done.** Continue never enabled (gold-add-funds 0/1) | S-01, S-16, S-21 |
| Get my money back to my bank | **Not done**, and the product contradicts itself about whether it ever will be | S-01, S-02, S-22 |
| Sell units I hold | **Not done.** The holdings read fails and the error is raw (gold-sell-units 0/1) | S-03, S-13 |
| See what I have and what it is worth | **Partly done.** gold-see-balance passed, but the unit is undefined and repeat reads fail | S-06, S-03 |
| Creator: raise funding, and have a backer find and back it | **Not done.** Project service 500; invest page never reached | S-04, S-23 |
| Creator: understand how raised money reaches me | **Not done.** No creator-facing explanation anywhere | S-07 |
| Find out why I'm blocked and fix it | **Only by workaround.** Users had to discover Settings > Verification on their own, then hit a button that does nothing | S-09, S-08 |
| Get to my money from the front door | **Only by workaround.** Users typed the URL | S-05, S-14 |
| Enter amounts in my own number format | **Not done** for comma-decimal locales | S-16 |

## 6. Findings ranked by severity

Severity = impact x frequency x segmentBreadth. Bands: critical is 18–27, or impact 3 on a gold task. Serious is 8–17. Minor is 1–7. The accessibility exception holds an impact-3 barrier to at least MEDIUM.

| Rank | ID | Finding | I x F x B | Score | Band | Ledger | Conf. | Lenses |
|---|---|---|---|---|---|---|---|---|
| 1 | S-01 | Money CTAs disabled or dead and indistinguishable from loading; 3/4 gold tasks at 0% | 3x3x3 | 27 | critical | HIGH (BLOCKER candidate: no workaround, intent unresolved) | high | frontline, trust, domain |
| 2 | S-02 | /swap promises cash-out "whenever you like", contradicting withdraw, add-funds and payouts | 3x2x3 | 18 | critical | HIGH | high | domain, trust, learning, content, service |
| 3 | S-03 | "Try again" never recovers on /projects, /portfolio, /swap (no backoff, no state) | 2x3x3 (service: 27) | 18 | critical | HIGH | high | service, frontline |
| 4 | S-04 | Project service 500 for every role breaks the creator-to-sponsor funding hand-off | 3x3x2 | 18 | critical | HIGH (BLOCKER: no workaround) | high | service |
| 5 | S-05 | Signed-out home has no money framing or entry point; / is the top-friction route | 2x3x3 (content: 27; a11y: 6) | 18 | critical | HIGH | high | content, learning, frontline, accessibility |
| 6 | S-06 | Money vocabulary undefined on /portfolio (ARDA unit, units, settlement, ≠, Nova/Azoa) | 2x3x3 | 18 | critical | HIGH | medium-high | domain, learning, content, trust |
| 7 | S-13 | Raw generic error on /swap "What you hold" breaks the honest-error voice (gold-sell-units) | 3x2x2 | 12 | critical (gold rule) | HIGH | high | content |
| 8 | S-11 | color-contrast failure (WCAG 1.4.3) on 9 money routes | 2x3x2 | 12 | serious | MEDIUM | high | accessibility |
| 9 | S-19 | Empty-vs-error honesty depends on timing on /projects (fake $0, then honest error) | 2x2x3 | 12 | serious | MEDIUM | medium | service |
| 10 | S-20 | Workspace Home narrative omits money entirely | 2x2x3 | 12 | serious | MEDIUM | high | learning |
| 11 | S-21 | $25 preselected on add-funds anchors the first amount | 2x2x3 | 12 | serious | MEDIUM | medium | trust |
| 12 | S-18 | Withdraw double-submit / idempotency untested (research question) | 3x1x3 (frontline: 4) | 9 | serious | RESEARCH_QUESTION | low | trust, frontline |
| 13 | S-08 | "Start account setup" enabled vs sibling disabled; fires ensure-mutation with zero feedback | 2x2x2 (domain: 6 at impact 3) | 8 | serious | MEDIUM | high | all except accessibility |
| 14 | S-09 | /portfolio never prompts "verify to unlock" | 2x2x2 | 8 | serious | MEDIUM | medium (single run) | frontline, learning, trust, service |
| 15 | S-07 | Operator copy on Verification and Payouts; no creator-facing payout explanation | 2x2x2 | 8 | serious | MEDIUM | high | domain, content, trust, service |
| 16 | S-22 | Withdraw page: clashing step numbering, sentence repeated 3x with no timeline | 2x2x2 | 8 | serious | MEDIUM | medium | content |
| 17 | S-16 | Comma-decimal and space-grouped amounts rejected; no fr-FR localisation | 2x2x1 (content: 8) | 4 | minor | LOW | medium | domain, content |
| 18 | S-23 | No-equity disclosure not confirmed at invest decision point (blocked by S-04) | 2x1x2 | 4 | minor | LOW | low | domain |
| 19 | S-15 | Amount error not wired to aria-invalid / aria-describedby | 2x2x1 | 4 | minor | LOW | medium | accessibility |
| 20 | S-12 | aria-prohibited-attr on / and /settings/verification (WCAG 4.1.2) | 3x1x1 | 3 | minor -> **MEDIUM** (a11y exception) | MEDIUM | high | accessibility |
| 21 | S-14 | No AT-announced money link on home; screen-reader user must type the URL | 3x1x1 | 3 | minor -> **MEDIUM** (a11y exception) | MEDIUM | high | learning, accessibility |
| 22 | S-17 | Tab silently skips disabled withdraw amount with no cue | 1x2x1 (frontline: 3, MEDIUM) | 2 | minor | LOW | medium | accessibility, frontline |
| 23 | S-10 | VERIFIED badge vs "not started" (disputed: screenshots show MEMBER) | 2x1x1 | 2 | minor | LOW | low | service, learning, content (trust disputes) |
| 24 | S-24 | 200% zoom: sidebar must be reopened at every step | 1x2x1 | 2 | minor | LOW | low | accessibility |
| 25 | S-25 | Loading/busy announcements on a slow network unverified (research question) | 1x1x1 | 1 | minor | RESEARCH_QUESTION | low | accessibility |

Per the panel rules, triage may mark one of these FIX only once it is confirmed in code **and** backed by ux.json evidence. Everything else becomes a RESEARCH_QUESTION.

## 7. Gold tasks and trend

| Gold task | Runs | Success | Median s | Median clicks | Median friction | Note |
|---|---|---|---|---|---|---|
| gold-see-balance | 1 | 1.00 | 4 | 0 | 6 | Passed. This was the only job that worked. |
| gold-add-funds | 1 | 0.00 | 93 | 0 | 20 | Continue never enabled with a valid amount (S-01) |
| gold-withdraw | 1 | 0.00 | 183 | 0 | 30 | Amount field was `<input disabled>`; 3x 30 s timeouts (S-01) |
| gold-sell-units | 1 | 0.00 | 6 | 3 | 25 | "What you hold" read failed 3x within 3 s (S-03, S-13) |

- **Exit checks:** goldSuccess fails, with target 0.8 and three tasks below it. frictionTrend fails: per-journey friction went from 1.0 to 28.1 against 00-setup. That baseline is a setup pass, so the jump is mostly a baseline artefact, not a regression measurement. newHigh is not computed. routeCoverage is 0.26 with no target.
- **Trend:** this is the first pass with gold data, so there is no trend yet. With n=1 per task, each success rate is a single observation. Pass 02 needs at least 3 runs per gold task before any trend claim means anything.

## 8. Human validation plan

Run 5 sessions per affected segment. Each session tests the listed findings. The **kill** condition is the result that would retire a hypothesis.

| Segment (5 sessions each) | Findings | What each session must test | Kill if |
|---|---|---|---|
| **Signed-in sponsors / backers** (sighted, desktop + phone) | S-01, S-02, S-03, S-09, S-21 | Add $5 from a cold session, then withdraw $1; time how long until they conclude it is "off" vs "loading". Read /swap first and predict what /withdraw will allow. On a failing read, press Try again for up to 60 s. Land on /portfolio and find out why money cannot move. Compare a $25 default against no default. | Buttons enable within the page's own retry window (S-01). 4/5 predict "withdraw disabled" after /swap (S-02). Try again recovers once the backend is healthy, which would reclassify S-03 as an outage artefact. 4/5 reach Settings > Verification within 60–120 s unprompted (S-09). The no-default median amount is not lower (S-21). |
| **Creators** | S-04, S-07, S-19, S-20, S-23 | Publish and share a project in an environment where CRUD returns 200, while a sponsor finds and backs it. Read Home and Payouts and explain how raised money reaches them. Screen-record the first 20 s of /projects with the backend down. Go straight to /invest and say whether they bought equity. | The hand-off completes with no 500 (S-04). Most explain the payout path from copy alone (S-07). The fake $0 never appears before the honest error (S-19). 4/5 describe the money function from Home (S-20). /invest repeats the no-equity disclosure (S-23). |
| **Signed-out newcomers** | S-05, S-02 | From /, with no URL given: "you have money here, go see it". Count detours through /auth/signin and record whether the post-login return lands on the right destination. | 4/5 reach a money path within 60 s without typing a URL. |
| **Low-literacy / plain-language members** | S-06, S-07, S-22, S-13 | Read /portfolio for 60 s, then paraphrase ARDA, settlement, and "submitted ≠ confirmed ≠ reconciled". Say who Azoa is. On /withdraw, point to "step one" and say when they can withdraw. React to the /swap error. | 4/5 paraphrase correctly. They identify Azoa's relationship correctly. They read the step numbering correctly. |
| **Screen-reader users** (NVDA / VoiceOver) | S-14, S-12, S-15, S-25 | Reach the balance from home using only announced links. Land on the aria-prohibited-attr elements and report the role/state heard. Enter -1, Tab away, Shift+Tab back. Use /swap and /portfolio on slow 3G and listen for a busy announcement. | 4/5 reach /portfolio within 90 s via announced navigation. The heard role/state matches what is visible. The error is re-announced on refocus. A live region announces loading. |
| **Keyboard-only, non-screen-reader** | S-17, S-18 | On staging with the gate lifted: Tab through withdraw, then double/triple-press Send. Check server records. With the gate on: ask why the field was skipped. | Focus order is correct with the gate lifted, and exactly one withdrawal is recorded. The user can explain the skip unaided. |
| **Low-vision / 200% zoom** | S-11, S-24 | Read "READY TO SEND" / "STILL HELD" and the amount placeholder at normal distance. Run add-funds → withdraw → settings at 200% on a 1366px screen and count menu reopens. | Labels are readable without strain. Menu reopens do not exceed the 100% control and are not reported as friction. |
| **Non-US-locale users** (fr-FR and other comma-decimal locales) | S-16 | Add 40,50 unprompted in their usual format. | Most type a dot first, or the product is confirmed US-only by design (then out of scope). |
| **Unverified users** (any role) | S-08, S-10 | Before clicking, predict which verification buttons "will do something". After pressing Start account setup and Check status, say whether setup started. Screenshot the badge and /settings/verification in one session. | 4/5 correctly identify the dead action, or the UI shows an accurate error. No VERIFIED badge exists (kills S-10 outright). |

The first S-01 human check is for triage, not users: settle whether the money rails are **intentionally** off in this deployment. If they are, S-01 changes from "broken" to "how a switched-off feature presents itself", and its fix is copy and state design, not plumbing.

## 9. Limits of this evidence (agents are not users)

- **Patience inflates time-to-abandon and hides rage.** Agents tolerated 5 Try-again presses, 85 s (Ben) and 183 s (gold sponsor) before giving up, and recorded **0 rage clicks** across 36 UX runs. Real users would quit sooner and click harder. So S-01, S-03 and S-17 are probably **understated** in the frustration they cause.
- **Literacy inflates comprehension.** Agents parsed "AZOA tenant-bound custodial account onboarding" and compared wording across three pages. A real skimming user would not. That makes S-06, S-07 and S-22 likely **understated**. S-02 is a mixed case: a real user might never *notice* the contradiction, but would act on whichever page they read first, which is worse.
- **Pixel-level cross-referencing inflates subtle findings.** Comparing disabled-state parity (S-08) or spotting a sentence repeated three times (S-22) is reviewer work. A real user may just bounce. The *existence* of these findings is agent-flavoured, even though the experience they describe is plausibly real.
- **Diagnostic access.** The Ivo audit runs read console and network state. No ordinary user can do that, so findings sourced only from those runs overstate how fast users self-diagnose.
- **Proxy assistive tech.** "Screen-reader" runs use Playwright `getByRole(name)`, not NVDA, JAWS or VoiceOver. S-12, S-14, S-15 and S-25 need a real AT session before they become tickets.
- **Search-by-name bias.** S-05 and S-14 could partly come from agents methodically searching by accessible name instead of glancing at a visually obvious "Open workspace" CTA.
- **Single-run and narrative-only evidence.** S-09 (Ana only), S-24 (Hana only), S-15 (lane narrative), S-10 (lane narrative, contradicted by screenshots) and S-19 (PREBAKE-2's own confidence was medium) carry lower confidence than the triangulated findings (S-01, S-02, S-05, S-11, which each rest on 3+ runs).
- **No real interruption modelled.** Nobody closed the tab and came back a day later. Ana's cold reload is the closest proxy, so re-entry is untested.
- **Coverage.** Only 14 of 53 routes and 2 of 12 APIs were exercised. Admin KYC review → member notification, guild, event and task hand-offs, and /projects/[slug]/invest were not observed at all. A missing finding there is **not** evidence that those areas are healthy.
- **Test-infrastructure noise.** Fay's 3 s ABANDONED run and the contradicted Eli "withdraw didn't navigate" narrative are harness or summary artefacts, not product defects. httpErrors=0 despite recorded 500s means the friction metric undercounts server failures.
- **Small n.** There was one run per gold task. Every success rate in § 7 is a single observation.
