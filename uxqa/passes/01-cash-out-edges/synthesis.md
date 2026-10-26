---
type: UX Synthesis
pass: 01-cash-out-edges
scenario: cash-out-edges
lenses: [service-designer, accessibility, domain-expert, frontline-worker, learning-designer, content-designer, trust-behavioural]
metrics: uxqa/passes/01-cash-out-edges/ux-report.json
blocker: PREBAKE-OWNER-1
date: 2026-10-25
---

# Pass 01 synthesis: cash-out and the money's edges

## 1. Summary

1. No persona had a session (prebake blocker PREBAKE-OWNER-1), so all 11 runs were signed-out. 0 of 4 gold tasks ran. Coverage: 2 of 53 page routes, 1 of 12 APIs.
2. The pass tested one gate (`/` plus `/auth/signin`) eleven times. It did not test eleven journeys. Those two routes account for all 165 friction points.
3. The strongest cross-lens theme: before sign-in, nothing on the page mentions money and no projects can be viewed. Every money or project link goes to the same sign-in screen, which says nothing about where the visitor was going.
4. The pre-auth surface itself works. It has 0 HTTP errors, 0 console errors and no fake $0.00. The callbackUrl round-trips correctly. The only confirmed defect is one serious axe rule on `/` (`aria-prohibited-attr`).
5. None of the money forms this pass was named for were tested: amount validation, idempotency, KYC gating, locale cents parsing and screen-reader money forms. Re-run them first once sessions are seeded.

## 2. Affinity map

| Theme | Merged finding | Lenses | Disagreements kept visible |
|---|---|---|---|
| A. No way into the money before sign-in | **F1** The signed-out header, hero and footer have no portfolio, wallet, balance or funds entry. The role-based search for one failed 10 of 10 times across 3 runs (EN and fr-FR) | frontline, learning, trust, content, accessibility, service | frontline, learning and trust score this as goal failure (impact 3). Content and accessibility score it as a detour (impact 2), because money is reasonably behind auth and the real issue is discoverability. |
| B. Evaluate before you commit | **F2** "Discover projects" and the header "Projects" link go straight to `/auth/signin`. There is no public project preview, so a backer who receives a project link sees no project at all | service (x2), domain, frontline, learning, content, trust | Scores ranged 12-27. Service designer: 27 (blind gate for every visitor). Trust: 12, medium confidence. The kill condition is shared: a real project's slug page may show a preview, but no project existed to check. |
| C. A sign-in wall that ignores the destination | **F3** `/portfolio`, `/add-funds`, `/withdraw`, `/swap`, `/settings/payouts` and `/settings/verification` all render a byte-identical "Welcome back" screen (118310 B screenshots). Nothing confirms the user's intent | domain, learning, trust, service, frontline | Domain-expert gives credit: callbackUrl does return the user to the page they asked for. The mechanism works; it just isn't visible in the UI. |
| D. Google as the only way in | **F4** "Continue with Google" is the only sign-in and sign-up option on every variant. Every accessibility segment depends on a cross-origin OAuth page that was never observed | accessibility, domain, frontline, trust | Frontline scores impact 3 (shared and corporate devices). Domain-expert says low confidence. |
| E. Disclosure before commitment | **F5** No pre-auth page names verification or KYC tiers, fees, payout timelines or payout rails | domain (x2), learning, service | — |
| | **F6** The best risk-reducing line ("Publishing, approving, and settlement actions remain separate, deliberate steps") appears only after the OAuth prompt, not on the home page | learning, trust, content (positive) | Content designer: this line is the one piece of copy in the pass that does its job well. The issue is where it sits, not what it says. |
| F. Vocabulary and tone | **F7** The home page uses abstract legal wording ("Record A: Contribution and Economic Interests", "settlement actions"). An "IMPORTANT DISTINCTION" disclaimer ("no ... payment rights") contradicts the add-funds and withdraw features. Tone shifts between ad slogans and legal register | domain, learning, content (x2) | Domain-expert scores 18. Learning-designer scores 8. Every lens notes that a skimming human might never read the disclaimer, so its frequency is uncertain. |
| | **F12** The "SECURE WORKSPACE ENTRY" label gives no detail about what is secure | trust | Low confidence, single lens. |
| | **F14** The cash-out route is `/swap`, a crypto term, while users think "sell units" | domain | The on-page heading was never seen. |
| | **F10** The "WELCOME BACK." headline is shown to new users | learning, content | **Contradiction:** service designer looked at `03-create-attempt.png` and found the Sign in and Create account flows *are* differentiated, and dropped this hypothesis. Learning and content kept it. No `mode=signup` screenshot exists. Unresolved. |
| G. Locale | **F8** With browser locale fr-FR, all UI is English, including the legal consent line. A related, lower-confidence sub-hypothesis: an SC 3.1.1 `lang` mismatch | content, domain, frontline, trust, accessibility | Content scores impact 3 (exclusion). Domain scores 1, assuming i18n is out of scope. Whether i18n is a product goal has not been decided. |
| H. Accessibility defect | **F9** Serious `aria-prohibited-attr` on `/`, in 3 independent runs at different viewports and zoom levels | accessibility, service | Service designer: breadth 3, because every visitor loads `/`. Accessibility: breadth 1, because only assistive-technology users are affected. The accessibility reading is taken for scoring. |
| I. Performance | **F11** Two `GET /api/auth/session` calls per navigation (70 calls across 11 runs, always an even count per task) | frontline | — |
| J. Coverage gap (not a UX defect) | **G1** Every money form is untested: amount fields, error-to-input association, live regions, keyboard traps, comma-decimal parsing (`1,50`→`150`?), and whether withdraw stops a double submit | accessibility, content (x2), trust, service (x2), frontline | Every lens agrees. The frontline lens points out that the plain-words and speed lanes (Eli and Fay) produced no run directory at all. |

## 3. Friction heatmap narrative

| Route | Friction | Abandons | Workarounds | Errors | Revisits | Axe |
|---|---|---|---|---|---|---|
| `/auth/signin` | 94 | 2 | 8 | 13 | 50 | 0 |
| `/` | 71 | 3 | 3 | 10 | 7 | 3 |

- **`/auth/signin` (94).** Every persona arrived here whatever it was trying to do: Ben checking a project before backing it, Ivo auditing withdraw labels, Hana typing `1,50` at 200% zoom, Chidi checking payout status. They typed money URLs directly (8 workarounds) and landed back on the same screen (50 revisits). The 13 errors are scripted "amount field unreachable" and locator timeouts, not app errors. The screen is clean: 0 axe violations in about 15 scans, and it is keyboard-reachable in 5 tab stops. The friction comes from where the screen sits in the journey and what it says, not from anything broken.
- **`/` (71).** People came to find their money or browse projects. Three runs gave up searching for a money link after 3-4 tries. The CTAs led to the sign-in wall. All 3 axe hits (one rule, one node) are here.
- **Trend caveat.** `frictionByPass` went from 1 (00-setup) to 15, and `frictionTrend.ok=false`. Most of that jump is one gate counted across 11 runs. It is not a regression against 00-setup, and the two passes cannot be compared (service designer). Several tasks show friction=0 (keyboard-payout-1/2, phone-locale-1, home-ctas). Those runs never reached anything that could produce friction, so 0 does not mean a smooth experience.

## 4. Heuristic violations (Nielsen)

| Heuristic | Route | Evidence | Severity |
|---|---|---|---|
| 6 Recognition rather than recall | `/` | F1: money-link search failed 10/10 across `empty-vs-error-truth-1`, `ivo-signed-out` and `hana-signed-out`; `01-home.png` nav | 27 critical |
| 2 Match with the real world | `/`, `/projects` | F2: "Discover projects" leads to `/auth/signin` (`home-ctas/discover-after-3s.png`, ben-1366 and ben-390) | 18 critical |
| 1 Visibility of system status | `/auth/signin` | F3: byte-identical gate screenshots for `/portfolio/*` (ben-1366 `03-*.png`) | 18 critical |
| 3 User control and freedom | `/auth/signin` | F4: Google is the only option (`focus-on-signin-wall.png`, `03-create-attempt.png`) | 12 serious |
| 10 Help and documentation | `/`, `/settings/verification` | F5: no mention of KYC, fees or timeline anywhere pre-auth (`01-home.png`, keyboard-payout-1) | 12 serious |
| 2 Match with the real world | `/` | F7: Record A/B wording and the "no payment rights" disclaimer (`01-home.png`) | 12 serious |
| 1 Visibility / 10 Help | `/` to `/auth/signin` | F6: reassurance line appears only after the OAuth prompt | 8 serious |
| 4 Consistency and standards | `/` | F9: axe `aria-prohibited-attr`, serious (WCAG 4.1.2) | 6 minor (confirmed defect) |
| 7 Flexibility and efficiency | `/`, `/auth/signin` | F11: duplicate session calls | 6 minor |
| 2 Match with the real world | `/`, `/auth/signin` | F8: English UI under fr-FR (`home-fr-200.png`, `signin-fr-200.png`) | 4 minor |
| 2 Match with the real world | `/auth/signin` | F10: "WELCOME BACK" (contested) | 4 minor |
| 2 Match with the real world | `/swap` | F14: crypto slug | 1 minor |

## 5. JTBD gaps

| Job users hired the product for | Status this pass |
|---|---|
| See what money or units I have (Ana, gold-see-balance) | No path pre-auth (F1). Untested post-auth (G1). |
| Evaluate a project and whether it's legitimate before I commit my identity or money (Ben, the backer handoff) | Not possible: browsing is behind the gate (F2). |
| Know which money action I'm about to unlock, and that I'll return to it after sign-in | Works under the hood (callbackUrl) but never shown (F3). |
| Know what cashing out will require (verification, fees, time) before investing the sign-up step | Not disclosed anywhere (F5). |
| Get in with an identity I control, without a forced provider | Google only (F4). |
| Understand what claim I actually hold for my money or labour | The home page disclaimer contradicts the money UI (F7). |
| Use a money product in my language and number format | English only (F8). Amount parsing untested (G1). |
| Withdraw and be sure it happened only once | Untested (G1, idempotency). |
| As a blind or keyboard-only member, operate every money form on my own | Untested (G1). |

## 6. Findings ranked by severity (impact x frequency x segmentBreadth)

Where lenses disagreed, the score below is reconciled toward the majority. The range is shown in brackets.

| # | Finding | I | F | B | Score | Band / ledger | Confidence |
|---|---|---|---|---|---|---|---|
| G1 | Money forms entirely untested: 0/4 gold tasks; idempotency, locale parsing, SR labels and WCAG 3.3.x/4.1.3 not checked | 3 | 3 | 3 | 27 | critical, **coverage blocker** (process, not product) | high |
| F1 | No discoverable money entry point pre-auth (EN and FR) | 3 | 3 | 3 | 27 [12-27] | critical / HIGH | high |
| F2 | No public project preview; "Discover projects" and project links go to the sign-in wall | 3 | 2 | 3 | 18 [12-27] | critical / HIGH | high (medium for the slug route) |
| F3 | Sign-in wall ignores the destination; nothing confirms the money intent | 2 | 3 | 3 | 18 [8-18] | critical / HIGH | high |
| F4 | Google is the only sign-in, with no fallback; the cross-origin return was never observed | 2 | 3 | 2 | 12 [12-18] | serious / MEDIUM | medium |
| F5 | Verification tiers, fees, payout time and rails undisclosed pre-auth | 2 | 2 | 3 | 12 [8-12] | serious / MEDIUM | medium |
| F7 | Abstract or legal home-page vocabulary; "no payment rights" disclaimer contradicts the money UI | 2 | 3 | 2 | 12 [4-18] | serious / MEDIUM | medium |
| F12 | "SECURE WORKSPACE ENTRY" label gives no specifics | 1 | 3 | 3 | 9 | serious / MEDIUM | low |
| F6 | Reassurance copy comes only after the OAuth prompt | 2 | 2 | 2 | 8 [8-12] | serious / MEDIUM | medium |
| F9 | axe `aria-prohibited-attr` (serious) on `/` | 2 | 3 | 1 | 6 [6-18] | minor / LOW (the only confirmed defect, so eligible for FIX once the node is located) | high |
| F11 | Duplicate `GET /api/auth/session` per navigation | 1 | 3 | 2 | 6 | minor / LOW | high |
| F8 | fr-FR gets an English-only UI and consent text; `lang` mismatch unverified | 2 | 2 | 1 | 4 [1-6] | minor / LOW (pending an i18n scope decision) | medium |
| F10 | "WELCOME BACK" shown to new users (contested) | 1 | 2 | 2 | 4 [4-9] | minor / LOW | low |
| F14 | `/swap` slug doesn't match "sell units" | 1 | 1 | 1 | 1 | minor / LOW | low |

How G1 was scored. Its sub-hypotheses sit on gold tasks. If a double withdraw is possible (frontline 3x1x2), or comma-decimal input is misread, impact would be 3 on a gold task, so the rubric makes it critical when confirmed. An unlabelled control on a money form would be an accessibility exclusion, which is at least MEDIUM whatever its breadth.

Two meta-observations are left out of the ranking: friction concentrated on two routes (service designer F4), and the expensive accessibility personas producing no distinct signal (service designer F5). Both are symptoms of the blocker, not product defects. See §3 and §9.

## 7. Gold tasks and trend

| Gold task | Persona | Ran | Result |
|---|---|---|---|
| gold-see-balance | qa-gold-sponsor | no (empty run dir) | — |
| gold-add-funds | qa-gold-sponsor | no | — |
| gold-withdraw | qa-gold-sponsor | no | — |
| gold-sell-units | qa-gold-worker | no | — |

- `goldTrend: {}`. `exitChecks.goldSuccess.ok=false` (target 0.8, all 4 missing).
- `frictionTrend` 1 → 15, `ok=false`. This is a blocker artifact and can't be compared with 00-setup.
- `routeCoverage` ratio 0.038. `newHigh` was not computed.
- The confirm-session dirs for qa-sponsor-ana and qa-freelancer-chidi are empty. The ux-plain-words-cash-out lane (Eli and Fay) has no run dir.
- **Exit criteria are not met. This pass does not count toward the loop's two-consecutive-pass stop.**

## 8. Human validation plan (5 sessions per affected segment)

Before any human sessions, close G1 mechanically. Resolve PREBAKE-OWNER-1 by seeding sessions.json for every pass-01 persona, then re-run this pass. Several of the hypotheses below may then die without a human ever being involved: F2 if a slug preview exists, F10 from a `mode=signup` screenshot, F8 and lang from the `<html lang>` value.

| Segment | Findings | Each of the 5 sessions must test | Kills the hypothesis |
|---|---|---|---|
| Newcomer sponsor / backer (cold, cookie-cleared) | F1, F2, F3, F5, F6, F7 | (a) Land on `/` and find where you'd check a balance or withdraw, unaided; time it. (b) Open a real project link and decide whether to back it. (c) Before sign-in, say what withdrawing would cost, take and require. (d) Paraphrase what you get for backing. | ≥4/5 find a money entry within about 15-60 s. A public preview renders. ≥4/5 answer (c) and (d) accurately from the page. |
| Creator | F2, F3 | Publish a real project, share its URL with a cold tester, and watch what the tester sees before auth. | The project page renders a title, goal and story before the wall. |
| Freelancer / worker | F3, F5, F7, F14, G1 | Deep-link to `/portfolio/withdraw`, sign in, and say where you expected to land. Find "sell my units" without being told `/swap`. | ≥4/5 predict landing on withdraw. The in-page heading reads plainly as cash-out. |
| Screen-reader + keyboard-only | F9, F4, G1 | NVDA/VoiceOver cold on `/`: is anything mis-announced near the axe node? Complete Google OAuth end to end and check focus on return. On the add-funds and withdraw forms: accessible names, error association, live-region status, no keyboard trap. | A fresh axe scan is clean or the node isn't exposed. Focus returns cleanly and is announced. 0 unlabelled controls and 0 unassociated errors. |
| fr-FR + 200% zoom (SME owner) | F8, G1 | Paraphrase the home page and the consent line. Type `5,00`, `1,50` and `1 000` into the amount fields and cross-check the parsed value on the server. Compare against an EN control group. | i18n is ruled out of scope, or comprehension and hesitation match the EN group. Amounts are parsed correctly or clearly rejected. |
| Mobile / slow network | F11, G1 | HAR trace across a full withdraw journey on a throttled connection. Double-tap the primary actions. | Duplicate session calls are deduped or add under 20 ms. Submit disables within about 300 ms. |
| Low-literacy / impatient (Eli, Fay) | F1, F7, G1 | Withdraw $1 "quickly", reading the copy aloud. Double-tap submit. Restate in one sentence what happens to $5 put in. | The button disables and no duplicate appears in the server log. An accurate one-sentence restatement. |
| Reluctant-to-link-Google users (shared, work or multi-account devices) | F4, F12 | Sign in on a shared or corporate device. Recall any security claim after seeing the screen for 5 s. | A fallback exists or no one hesitates. The label has no effect either way. |

## 9. Limits of this evidence (agents are not users)

- **Everything here is pre-auth.** All 11 runs were signed-out substitutes. Nothing is known about what happens after Google sign-in. Do not read the clean gate as evidence that the cash-out flow is trustworthy (trust lens F8).
- **Patience inflates persistence.** Agents gave up after 3-4 timed tries. Real distracted or low-literacy users would give up sooner. Read F1 and F2 abandonment as a floor, not a ceiling.
- **Literacy and thoroughness cut both ways.** Agents read every pixel, so the disclaimer finding (F7) and the reassurance finding (F6) may overstate how often a skimming human notices them. A human might also take in a distrust cue without registering it. The abstract-vocabulary findings likely *understate* how much real low-literacy users struggle to understand the page.
- **No emotion.** Perceived risk in F4 (Google-only) and F12 (security label) is inferred from behavioural priors, not measured, so both are low or medium confidence.
- **Role queries are not a screen reader.** `getByRole` gives precise evidence for F1 and F9. It cannot measure reading speed, frustration or OAuth tab confusion.
- **One gate, counted 11 times.** The heatmap concentration and the 1→15 friction trend are artifacts of the seeding blocker until a seeded re-run says otherwise. Any defect on `/` or `/auth/signin` has guaranteed maximum reach, but so does the double counting.
- **No devtools.** The `lang` sub-hypothesis of F8 and the "WELCOME BACK" signup-mode question (F10) were inferred, not inspected.
- **Triage rule.** Only F9 (once the axe node is located in code) and F11 (if confirmed in network code) are candidates for FIX. Everything else is a RESEARCH_QUESTION for the plan in §8. F3 was later promoted to FIX on code evidence, and F1 stays open. See §10.

## 10. Triage outcomes (what the fix commits actually did)

The §9 triage rule allowed FIX only for F9 and F11. The fix commits went further. This section records what each one changed and which findings stay open.

| Finding | Outcome | Commit | Code evidence |
|---|---|---|---|
| F9 `aria-prohibited-attr` on `/` | **FIX** (as the rule allowed) | `0a79d90` | The node is the `ArdaNovaWordmark` outer `<span aria-label="ArdaNova">`. It had no role, so the label was prohibited. It now has `role="img"` (`ardanova-client/src/components/brand/wordmark.tsx`). |
| F3 sign-in wall ignores the destination | **Promoted to FIX** (outside the rule; recorded here) | `ca1be70` | The defect can be checked in code, so it does not need user research. Before `ca1be70`, `auth/signin/page.tsx` normalized `callbackUrl` and passed it to `signIn` but never displayed it: no destination copy existed, which is why the six gate screenshots were byte-identical. The page now shows `destinationLabel(callbackUrl)` for named internal routes (`data-testid="signin-return-destination"`). The §8 "Freelancer / worker" session still checks whether users *predict* landing on withdraw. |
| Mobile primary nav (no finding number) | **FIX** of a code-checkable defect, not of F1 | `0a79d90` | The home `<nav>` is `hidden ... md:flex` with no fallback below 768px. `HomePrimaryNavMenu` (`ardanova-client/src/components/marketing/home-primary-nav.tsx`) exposes Method / Rights / Nova / Projects there. |
| F1 no money entry before sign-in | **Still a RESEARCH_QUESTION. Not resolved.** | — | The mobile menu only contains the same four non-money links as the desktop nav. The signed-out header, hero and footer still have no portfolio, wallet, balance or funds entry. The `0a79d90` trailer IDs do not close F1. |
| F11 duplicate session calls | Not fixed in this pass | — | Not yet confirmed in network code. |

Regression checks for the three fixes: `uxqa/checks/01-cash-out-edges.check.mjs` (signin destination present / absent for an off-site callbackUrl, mobile menu disclosure + Escape focus return at 390px, axe scan of `/` without `aria-prohibited-attr`).
