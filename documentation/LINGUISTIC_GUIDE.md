# ArdaNova linguistic guide

## Purpose

ArdaNova is a social layer for collective action. This guide sets the words for product interfaces, community activity, Nova-assisted work, and public storytelling. It complements the [brand guidelines](./BRAND_GUIDELINES.md) and the [frontend experience contract](./FRONTEND_EXPERIENCE.md).

North star: **Turn attention into action.**

---

## Just enough

The interface speaks through what it shows. Copy fills only the gap the UI leaves.

Owner direction (2026-10-31): Apple's restraint, with the subtlety of Slavoj Žižek's ad copy. The platform should speak for itself.

### Rules

1. **Never narrate what the screen already shows.** No eyebrow that repeats the heading, no subhead that explains what the page is for.
2. **Headings: four words or fewer, sentence case.** A noun is usually enough: `Projects`, `New event`, `Coming up`.
3. **A supporting line is optional, eight words or fewer,** and earns its place only by adding something the UI does not show: a prerequisite (`Add milestones and epics in Roadmap first.`), a consequence (`Whoever you accept joins the team.`), or a scope (`Filters search every project, not just this page.`).
4. **Buttons: verb + noun, three words or fewer.** `Create guild`, `Post opportunity`, `Try again`. Name what the click does now, not what may happen later.
5. **Empty states: one short line, one action.** `No projects yet.` + [Define a project]. When filtered: `No matches.` + [Clear filters]. Never "Be the first to…".
6. **Errors say what failed.** `Proposals didn't load.` + [Try again]. Don't follow it with "This is a service error"; the heading already said so.
7. **Placeholders ask one question.** `What does done look like?` · `What changed?` · `Who benefits?`
8. **No branded text on working surfaces.** No taglines, mission lines, eyebrow stacks, or "we/our" on functional pages. "ArdaNova" appears in the logo, page titles and metadata, and legal text, never in running copy. The logged-out landing page may carry one brand line and stays restrained.
9. **Žižek register, rationed.** At most one line per page may carry a quiet inversion, paradox, or second meaning that rewards a re-read. Dry. Never a pun, never a joke at the reader's expense, never explained. Most pages get none. If in doubt, plain wins.
10. **Delete before you rewrite.** Most long lines should go, not shrink. Then fix the spacing the removed element leaves behind.

### What stays long

Money, verification, consent, and safety copy is exempt. It keeps the full **state → evidence → next move** pattern however long it runs:

> "Release instruction submitted. The receiving service returned reference 41A7. Check confirmation before treating the settlement as reconciled."

That covers holds, refusals, needs-review, KYC/KYB, payouts, cash-out, withdraw, invest, add funds, escrow, double-submission guards (`If you were saving something, it was only saved if you saw a confirmation; check before trying again.`), irreversible actions (`Your vote is final and cannot be changed.`), data-scope disclosures, and terms and privacy. Form labels, help that correctness needs, and aria-labels and sr-only text may stay descriptive.

A CTA must still never imply a later state the action cannot complete. Don't write **Complete settlement** for a button that only submits an instruction.

### Before → after (2026-10-31 copy strip)

| Surface | Before | After |
| --- | --- | --- |
| Top bar, every page | `PROJECT WORKSPACE` / `Make the project easier to understand and review` | *(slot removed)* |
| `/projects` header | `DISCOVER A PROBLEM / DEFINE A SOLUTION / ITERATE` / `Projects for doing` / `Open a project to understand the problem, inspect the proposed solution, and find the work that moves it forward.` | `Projects` |
| `/projects` empty state | `No projects returned` / `No one has posted a project yet. If you have seen a problem worth solving, define it here so others can help fund and build it.` | `No projects yet.` + [Define a project] |
| Governance error | `Governance proposals could not be loaded.` / `No proposal or vote counts are shown.` | `Proposals didn't load.` |
| Task placeholder | `Describe the task in detail. Include acceptance criteria if applicable...` | `What does done look like?` |

The one Žižek line in this pass is the landing page's closing heading: `Give the next iteration a place to happen.` became `Problems are not in short supply.`, set above [Start with a problem]. It applies market language to the platform's raw material, and the button turns the shrug into agency.

The full per-string ledger is `conductor/tracks/mvp-readiness-loop_20261026/COPY-STRIP-2026-10-31.md`.

---

## The solutionary loop

**Solutionary** is a deliberate ArdaNova word for moving from a lived problem to a testable response and learning in public with the people affected. It belongs on the landing page and in long-form writing, not on working surfaces. Never use it as a synonym for "innovative".

```text
Discover a problem -> Define a solution -> Iterate
        ^                                      |
        +--------------------------------------+
```

It is a loop, not a funnel. Returning to discovery, or stopping, is progress when the evidence calls for it.

### Discover a problem

- Use: discover, notice, document, ask · problem statement, field note, evidence, affected people, open question.
- Avoid: "pain point" for a human problem; "trending" or "viral" as a proxy for importance; "target audience" for the people affected.

### Define a solution

- Use: define, shape, propose, scope, set terms · hypothesis, milestone, role, participation terms, success measure.
- Avoid: "perfect", "guaranteed", "instant fix"; "ownership" as shorthand for membership, project-token utility, or future value; "community-owned" without governing documents behind it.

### Iterate

- Use: test, contribute, build, compare, revise, reconcile, pause, stop · work update, decision, result, next move.
- Avoid: "done" while settlement, rights, or downstream effects remain unresolved.

---

## Social language

The feed is ranked and continuous (owner decision SR-03-D2, 2026-10-31). Copy must not promise it ends ("finite by design", "no infinite feed", "close the tab"), and it must not narrate engagement either. Let the feed run without comment.

What stays true: every card names a meaningful object and offers one action.

| Prefer | Use it for |
| --- | --- |
| Field note | Firsthand context or evidence |
| Open question | Something the group needs to understand or decide |
| Contribution | Work, knowledge, or resources offered under visible terms |
| Work update | What changed, what was learned, what comes next |
| Proposal | A reviewable suggestion, not yet a decision |
| Decision | A recorded outcome made under stated authority |
| Invitation | A specific request for a person, skill, or resource |

Activity reads as **person + verb + object**: "Mara added field evidence to Water Commons." Avoid "Mara engaged with Water Commons."

Don't put engagement words in copy: go viral, boost reach, trending now, never miss out, keep scrolling. Never present popularity as proof that a problem or solution matters.

---

## Ownership without surveillance

**Ownership** always needs an object and a basis: what is owned, what rights attach, where they are documented, and what state the record has reached.

- Use: "This membership credential grants the stated participation rights; it does not represent equity."
- Avoid: "You own the project" after saving, joining, or contributing; "Earn ownership" without naming the instrument and its conditions; treating credentials, tokens, shares, governance authority, and compensation as interchangeable.

"Own what you build" is an aspiration. Transactional copy states the exact documented right.

Name what context is used, why it is needed, and who can act on it. Don't imply background profiling ("We know what you need", "recommended for you" without an explainable basis). Never invent a privacy promise. Retention, deletion, and "we never track" claims must match implemented behavior and policy.

---

## Nova and machine language

Nova works beside an artifact. It may ask, draft, organize, compare, review, present, and rehearse. It does not decide, approve, own, publish, fund, settle, or grant rights.

- Use: "Nova proposes…" · "Drafted from the selected brief." · "No funding or rights-changing action is included."
- Avoid: "Nova solved your project" · "AI-powered magic" · "One-click project creation" when review is required.

Nova surfaces disclose scope, exclusions, and uncertainty, and that disclosure counts as consent copy under **What stays long**. The human controls are **Accept**, **Edit**, **Reject**, and **Undo**.

---

## Brand lines

These are for the landing page and campaigns only, one per surface.

- Turn attention into action.
- Social media for doing, not doom-scrolling.
- Technology for ownership, not surveillance.
- Discover a problem. Define a solution. Iterate.

Any line about ownership sits near copy that states explicit rights. Any line about privacy must match implemented behavior and policy.
