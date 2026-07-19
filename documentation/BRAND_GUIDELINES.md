# ArdaNova brand guidelines

## Purpose

ArdaNova helps people turn ideas into accountable, community-supported work. The brand should make power, responsibility, and system state easy to understand. Technology supports that relationship; it is not the story by itself.

The visual direction is **flat neobrutalism with abstract-expressionist energy**: editorial scale, hard geometry, human texture, and precise operational detail. It combines the clarity of Swiss design, the directness of Neue Montréal, the expressive project storytelling in ArdaNova's Gamma deck, and the calm system legibility of Azoa.

For implementation rules and interaction patterns, see [Frontend experience](./FRONTEND_EXPERIENCE.md). For the complete voice, vocabulary, CTA, activity, and machine-language system, see the [Linguistic guide](./LINGUISTIC_GUIDE.md).

---

## Brand promise

**Turn attention into action.**

ArdaNova is a place to shape a project, make its terms visible, find collaborators, and keep a trustworthy record of decisions and value. We explain what a person can do now, what requires review, and what has actually happened.

Two category lines frame the promise:

- **Social media for doing, not doom-scrolling.** ArdaNova is social through shared inquiry, contribution, decisions, and iteration - not an endless attention feed.
- **Technology for ownership, not surveillance.** The system should make power and explicitly documented rights legible, not make people observable for extraction.

"Own what you build" remains an aspirational brand line, never a transactional claim. Product copy must name the specific instrument, terms, conditions, and reconciliation state behind any ownership language.

### Voice principles

- Lead with the human outcome, then explain the mechanism.
- Use plain, concrete language and active verbs.
- Orient attention toward the solutionary loop: **discover a problem -> define a solution -> iterate**.
- Give contributors agency; do not portray the platform as acting on their behalf.
- Name limits, prerequisites, and pending states early.
- Distinguish an intent from a confirmed or reconciled result.
- Avoid urgency, speculative-return language, and technological spectacle.

---

## Visual identity

### Core direction

The public experience can be expressive. The working application should be quiet, structured, and exact.

- **Flat first:** use color fields, rules, and typography instead of shadows or simulated depth.
- **Sharp geometry:** square corners are the default. A pill is reserved for small status or filter controls.
- **Type carries hierarchy:** prefer a decisive change in scale or weight over decorative effects.
- **One expressive move per view:** a signal-red field, oversized line of type, or abstract mark is enough.
- **Operational color has meaning:** cyan identifies system guidance or verification; green is reserved for confirmed positive states.
- **Texture is human, not digital noise:** painterly or collage imagery may frame stories, but never reduce readability.

Do not use neon glows, glassmorphism, gradient-heavy cyberpunk surfaces, CRT effects, ornamental grids, or decorative animations around financial and governance actions.

### Color system

| Role        | Token           | Default                        | Use                                         |
| ----------- | --------------- | ------------------------------ | ------------------------------------------- |
| Canvas      | `--background`  | `#f4efeb` warm parchment       | Primary page field                          |
| Ink         | `--foreground`  | `#151513` near-black           | Text, rules, high-emphasis controls         |
| Paper       | `--card`        | `#fffdfa` warm white           | Working surfaces and forms                  |
| Signal      | `--primary`     | `#ef4638` signal red           | Expressive fields and primary moments       |
| System      | `--system`      | `#117b88` cyan                 | Focus, verification, and Nova context       |
| Muted       | `--muted`       | Warm grey                      | Secondary surfaces, never low-contrast text |
| Success     | `--success`     | Deep green                     | Confirmed positive outcomes only            |
| Warning     | `--warning`     | Ochre                          | Prerequisites, review, and pending risk     |
| Destructive | `--destructive` | Deep red                       | Irreversible or failed states               |
| Rule        | `--border`      | `#87817a` accessible hard rule | Separation, controls, and structure         |

Signal red is expressive, not a universal success color. Green must not appear for an unconfirmed submission. Cyan indicates context or verification, not completion.

Dark surfaces are optional utility modes, not the default brand atmosphere. When used, preserve the same semantic mapping; the system cyan may shift to `#70d7e2`. All combinations must meet WCAG 2.1 AA contrast.

### Typography

| Element         | Treatment                                                               |
| --------------- | ----------------------------------------------------------------------- |
| Display         | Oversized grotesque/sans, tightly set, confident rather than futuristic |
| Headings        | Bold sans or restrained mono where the interface is operational         |
| Body            | Inter or a legible system sans, sentence case                           |
| Labels and data | JetBrains Mono or system mono, concise and tabular where useful         |
| Actions         | Sentence case; name the result of the action                            |

Avoid long passages in uppercase. Mono type is a signal for evidence, state, or metadata; it is not the voice for every paragraph.

### Layout

- Use a 12-column grid for wide editorial pages and a readable single-column flow on mobile.
- Recommended maximum width: 1440px; long-form copy should remain much narrower.
- Use 1px or 2px rules to define regions.
- Default border radius: `0`.
- Default elevation: none.
- Keep dense operational data aligned; keep project narratives spacious.
- Touch targets must be at least 44 by 44 CSS pixels where practical.

### Imagery and marks

Prefer documentary images of work, contributors, materials, and place. Abstract-expressionist collage may be used to convey movement, collective effort, or an unfinished idea. It should feel tactile and authored rather than generated as filler.

When no suitable image exists, use simple authored SVG or CSS geometry. Do not imply a real community, project, or result with fabricated photography.

---

## Language system

### The solutionary loop

**Solutionary** is a deliberate brand word for moving from a lived problem to a testable response and learning with the people affected. It is not a synonym for fashionable or innovative.

The central product narrative is:

1. **Discover a problem** through lived context, affected people, evidence, and open questions.
2. **Define a solution** as a reviewable proposal with visible assumptions, roles, terms, and success measures.
3. **Iterate** by testing, contributing, comparing results, reconciling exceptions, and revising or stopping.

This is a loop rather than a funnel. The interface should privilege field notes, open questions, proposals, contributions, decisions, work updates, and next moves over popularity or time-on-platform metrics.

The [Linguistic guide](./LINGUISTIC_GUIDE.md) is canonical for detailed vocabulary, sentence patterns, CTA grammar, community activity language, anti-surveillance guardrails, and Nova voice.

### Values-based terms

| Avoid as the lead | Prefer                                                   | Notes                                                   |
| ----------------- | -------------------------------------------------------- | ------------------------------------------------------- |
| Blockchain        | Secure ledger / transparent record                       | Answer directly when asked                              |
| Crypto            | Digital payment / digital asset                          | Do not conceal the underlying mechanism                 |
| Token             | Project token, membership credential, or ownership share | Use the exact asset type; these are not interchangeable |
| DAO               | Member-led governance                                    | Explain decision rights                                 |
| Smart contract    | Automated agreement                                      | State what it can and cannot do                         |
| AI agent          | Nova drafting or review tool                             | Say when a feature is an interface preview              |
| Wallet            | Connected account or portfolio                           | Use “wallet” where technically necessary                |
| Mint              | Issue                                                    | Reserve for technical documentation                     |
| On-chain          | Submitted, confirmed, or reconciled on the ledger        | Use the exact state                                     |

### Rights vocabulary

Never collapse these concepts into a generic “ownership” label:

- A **membership credential** represents eligibility or governance participation under its stated rules.
- A **project token** has only the utility described for that project.
- An **ownership share** or equity interest exists only where separately approved and documented.
- Redemption, dividends, transferability, and voting rights must each be stated rather than inferred.

### State vocabulary

| State        | Meaning                                         |
| ------------ | ----------------------------------------------- |
| Draft        | Local or saved work that has not been submitted |
| Submitted    | An instruction or intent was sent               |
| Confirmed    | The receiving system accepted or recorded it    |
| Reconciled   | ArdaNova verified the resulting platform record |
| Needs review | A person must inspect or approve the next step  |
| Failed       | The requested operation did not complete        |

Use these words consistently in buttons, notices, timelines, portfolio values, escrow views, and activity history.

---

## Humane machine interaction

Nova is a collaborator beside the work, not an autonomous authority above it.

Every machine-assisted surface should answer:

1. What material is in scope?
2. What is excluded or unknown?
3. What did Nova change or propose?
4. Why is that proposal useful?
5. What sources, assumptions, or uncertainties influenced it?
6. What will happen if the person accepts it?

Nova may draft, organize, compare, summarize, rehearse, and present. It must not publish a project, approve governance, move funds, release escrow, swap assets, issue credentials, or change rights. Those actions remain explicit, separately reviewed human steps.

Prefer **Accept**, **Edit**, **Reject**, and **Undo** over a single opaque “Generate” action. Preserve the previous version. Never present generated output as a completed platform action.

---

## Audience framing

### Contributors

Emphasize clear terms, meaningful work, recognition, and any separately documented value or rights. Do not promise ownership from task completion unless the project terms actually provide it and reconciliation is complete.

### Project stewards

Emphasize turning an idea into a legible plan, inviting the right participation, and making decisions accountable. Nova can help shape the draft; the steward remains responsible for publishing it.

### Communities and cooperatives

Emphasize collective decision-making, transparent responsibilities, and accessible records. Avoid suggesting that a technical mechanism makes a process democratic by itself.

### Funders and supporters

Emphasize terms, risk, use of funds, and current state. Never use FOMO, guaranteed-return language, or a generic “investment” label for utility tokens.

---

## Example copy

### Good

- “Draft the project brief with Nova.”
- “Review 4 proposed changes before saving.”
- “Release instruction submitted. Settlement is not yet reconciled.”
- “Wallet address saved. Control has not been verified.”
- “This project token does not grant equity or governance rights.”

### Avoid

- “Let AI build your project.”
- “Escrow released successfully” immediately after submission.
- “You now own the project” without explicit, reconciled rights.
- “Verified” when only an address or profile field was saved.
- “Risk-free,” “instant returns,” or “guaranteed.”

---

## Reference set

- [Azoa](https://azoa-frontend-production.up.railway.app/) — flat operational state, graph-like system context, and restrained mono detail.
- [Neue Montréal](https://neuemontreal.com/) — radical flatness, oversized type, sparse navigation, and hard rules.
- [ArdaNova visual narrative](https://gamma.app/docs/ARDA-NOVA-A-FAIR-DEAL-FOR-THE-DIGITAL-AGE-06lxdcxj2910jwt) — signal color, expressive collage, and project storytelling.

References are directional, not templates to copy. ArdaNova's differentiator is humane control: expressive enough to invite imagination and precise enough to trust with work, governance, and value.
