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
