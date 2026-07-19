# Frontend source guidance

## Experience invariant

ArdaNova is expressive in public storytelling, quiet in the workspace, and explicit whenever a person or machine can change state. The canonical rationale lives in [`../../documentation/FRONTEND_EXPERIENCE.md`](../../documentation/FRONTEND_EXPERIENCE.md) and [`../../documentation/BRAND_GUIDELINES.md`](../../documentation/BRAND_GUIDELINES.md).

## Visual hierarchy

- Use the semantic tokens in `styles/globals.css`; do not introduce one-off neon colors or gradients.
- Default to square corners, hard rules, flat surfaces, and no decorative elevation.
- Reserve signal red for expressive or primary moments and cyan for system context, focus, and verification.
- Prefer typography, spacing, and dividers to ornamental containers.
- Keep the authenticated workspace calmer and denser than the public pages.

### Compatibility visual variants

Legacy `neon-*`, `gradient-*`, `warm-glow`, and `elevated` names remain available while older call sites migrate, but they must render as flat semantic treatments: cyan maps to system context, pink maps to signal red, green maps to confirmed success, and yellow maps to warning. Do not add new call sites with those legacy names. Rounded avatars are the deliberate geometry exception because a stable human silhouette improves recognition; other media may be rounded only when the crop itself benefits recognition.

The root radius and shadow tokens deliberately flatten older utility call sites, and workspace backdrop blur is disabled. New or substantially edited surfaces should still use semantic flat classes directly so the compatibility layer can shrink over time.

## Human and machine actions

Nova output is a proposal. A machine-assisted surface should expose its scope, excluded or unavailable context, assumptions, and uncertainty, then offer reviewable accept/edit/reject/undo controls.

Nova may draft, organize, review, present, and rehearse. It may not publish, approve governance, move funds, release escrow, swap assets, issue credentials, pay contributors, or change rights. Those actions remain separate human-controlled workflows.

Studio starts blank and stores only validated, size-bounded tab-session drafts.
Never preload realistic content into an accept-ready state. Chat read evidence
requires a focused visible document, a near-bottom viewport, and a visible
incoming message; send that message's server time rather than the local clock.
Studio's three artifact modes share one UTF-8 byte budget; keep current work and
edit recovery before pruning history, and label a session saved only when the
exact stored value passes the restore contract. Project creation recovery keeps
only failed child work and the existing project reference so a retry cannot
silently create a duplicate. Verification return links must remain internal and
must not loop back into the verification page.

The dispute form, tRPC router, REST client, and .NET API carry the selected reason and trimmed 20–4000 character narrative together. The API derives the actor from its signed assertion and persists the context atomically with the status transition. Never add a client-only dispute field or accept a caller-supplied actor ID at the public HTTP boundary.

Sensitive mutations currently paused in both the BFF and backend include token
distribution, credential issuance and lifecycle changes, governance and vote
delegation changes, and referral reward claims. Keep their read surfaces
available, but do not
restore forwarding until the backend derives authority and scope, and the
state transition is atomic, auditable, and idempotent. A caller-selected grant
reason, user ID, beneficiary, or reward amount is data, never authorization.

## State and rights language

- Draft, submitted, confirmed, and reconciled are distinct states.
- A saved wallet address does not prove wallet control.
- Task completion does not prove award reconciliation.
- Escrow authorization does not prove contributor settlement.
- Membership credentials, project tokens, and ownership shares are separate instruments.

Prefer a precise intermediate state over optimistic “success” copy. Retain transaction or service evidence and provide a recovery path when a workflow does not reconcile.

## Accessibility

- Use semantic links and buttons; do not make a `div` clickable.
- Give every control a visible label or accessible name.
- Preserve a visible focus indicator and 44px touch target where practical.
- Never encode status with color alone.
- Honor reduced motion and keep ambient animation out of operational flows.
- Test primary routes with keyboard navigation and at desktop and narrow mobile widths.

## Comments and documentation

Keep source comments terse and local. Put cross-cutting rationale in this file or the nearest directory-level `AGENTS.md`, then link to it from code only when necessary.
