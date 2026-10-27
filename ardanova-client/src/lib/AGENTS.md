# Frontend library guidance

Shared library helpers should encode a behavior contract without coupling to a page layout. `accessibility.ts` centralizes manual tablist keyboard behavior for legacy page-level tabs while those surfaces migrate to the shared Radix primitive: arrow keys wrap, Home/End move to an edge, focus follows selection, and non-tab key events remain untouched.

Prefer semantic platform behavior or an existing UI primitive before adding another helper here. Keep product and authorization decisions in their owning domain modules.

Runtime readiness treats documented placeholders as missing secrets even when
they satisfy a byte-length rule. Keep the marker contract aligned with the
server environment schema and the .NET middleware.

Commerce migration preflight expectations mirror PostgreSQL's
`information_schema` output from the checked-in Prisma migrations. An
unannotated Prisma `String` is `data_type=text` and `udt_name=text`; DBML's
source-level `varchar` label is not the deployed catalog contract.

## Tolerant enum reads

The API and this client deploy separately, so the API can add an enum value
before the client knows it. A strict `z.enum` on a response turns that into a
failed read (the project router answered every project read with BAD_GATEWAY).
For a vocabulary that grows by design, read it with a tolerant schema and keep
the strict one for what the client sends: `projectTypeCodeSchema` accepts any
non-empty code and types it as `ProjectType | (string & {})`, so known values
still autocomplete and `isProjectType` still narrows. Send-side choices come
from the API's own option list (`useEnumOptions`) filtered by the known list,
so the client never offers a value an older API would reject. Render unknown
codes through `taxonomy/labels.ts`, never through an exhaustive
`Record<ProjectType, …>` lookup that returns `undefined` for them.

## Taxonomy labels

`taxonomy/labels.ts` turns stored codes (ProjectType values, category codes,
skill codes) into labels. The database owns the vocabulary in `TaxonomyTerm`
(seeded by `prisma/migrations/20261027003000_taxonomy_term`); until the API
serves those rows, the module mirrors the seed and `labels.test.ts` fails if the
two drift. A code it has no entry for is humanized (`PHYSICAL_BUILD` ->
"Physical build"); a person's free-text category is shown as typed. Labels are
plain English with no product hype.

## AZOA workflow statuses

Use the SDK's `isAwaiting` helper for parked workflow runs, then add `Pending`
and `Running` for pollable in-flight state. Do not introduce application-only
workflow statuses; settlement reconciliation belongs to the economic outbox.

## Member vocabulary — the node's words never reach a screen

The product is a social platform for doing, not a blockchain company. A member
must never be shown a chain name, a wallet address, a mnemonic, a gas fee, a
signed challenge, a mint, a hash, or the word "token" where "funds", "stake" or
"units" would do. That is a defect, not a wording preference.

Every such word is translated once, in a module here, and screens read only the
translation:

- `commerce/on-ramp-status.ts` — the node's add-funds and settlement words.
- `credentials/credential-proof.ts` — the node's `isOnChain`, `chainVerified`,
  `mintTxHash` and `asaInfo` readings, used by both
  `components/credentials/credential-card.tsx` and
  `app/credentials/[id]/page.tsx` so the two cannot drift.

Each translator carries a test that asserts the forbidden words over *every*
string it can return, not over a sampled few — see the "the words a non-crypto
member/sponsor is shown" blocks in the sibling `*.test.ts` files. Copy that a
translator returns is the only copy allowed to describe an economic fact.

The rendered surface (`src/app`, `src/components`) is swept mechanically by
`scripts/verify-product-vocabulary.mjs`, wired into CI and the release gate and
covered by `lib/product-vocabulary.test.ts`. `src/lib` itself is **not** in that
sweep, because operator tooling lives here too — `commerce/migration-preflight.ts`
runs from a terminal and legitimately says "on-chain assetId". That is why every
member-facing module here needs its own vocabulary test; the scanner will not
catch it for you.
