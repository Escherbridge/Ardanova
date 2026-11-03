# App routes

## Copy

On-screen copy follows `documentation/LINGUISTIC_GUIDE.md` §Just enough: no narrating subheads or eyebrow stacks, headings four words or fewer, empty states one line and one action, money/verification/consent/safety copy exempt and left verbatim.

## Mutation origins

The tRPC POST boundary rejects cross-site fetch metadata and any supplied Origin that differs from the trusted configured `AUTH_URL` origin before reading the body. Same-origin browser requests and non-browser calls without Origin retain the existing authenticated actor policy. GET cannot invoke a mutation. Keep the streaming body cap after this guard.

Reverse proxies can supply an internal NextRequest URL (for example `0.0.0.0:8080`), so it is not the production origin authority. A request-URL fallback is limited to local or test environments. Never trust forwarded host headers as an origin allowlist, and reject malformed, opaque or path-bearing Origin values.
