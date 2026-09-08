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
