# Frontend library guidance

Shared library helpers should encode a behavior contract without coupling to a page layout. `accessibility.ts` centralizes manual tablist keyboard behavior for legacy page-level tabs while those surfaces migrate to the shared Radix primitive: arrow keys wrap, Home/End move to an edge, focus follows selection, and non-tab key events remain untouched.

Prefer semantic platform behavior or an existing UI primitive before adding another helper here. Keep product and authorization decisions in their owning domain modules.
