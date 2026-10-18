---
type: spec
status: proposed
priority: P0
source: loop2 auth adversarial critique (2026-10-17)
---
# API Path-Segment Safety

## Problem

The ArdaNova API client builds .NET URLs by putting caller-supplied ids straight
into template strings (`/api/users/${id}`). At 29d057f there are about 286 such
interpolations across 20 `src/lib/api/ardanova/endpoints/*.ts` files, and most
tRPC inputs validate those ids only with `z.string()`. The client sends every
request with the server's API key. That makes any public procedure taking an id
a confused-deputy gadget: `..` segments collapse when the URL is parsed, and a
trailing `?x=` turns the rest of the path into a query value. The following
anonymous probes worked on loop 2:

- `user.getById({ id: "paged?page=1&pageSize=3" })` returned the paged user list with raw emails
- `user.getFollowCounts({ userId: "../../api/projects?x=" })` returned `/api/projects`
- `profile.getProfileById({ userId: "email/<addr>" })` looked up an account by email

Loop 2 closed these vectors in `routers/user.ts` and `routers/profile.ts`, using
`recordIdSchema` and `requireRequestedUser`. Every other router is still open.

## Goal

No caller-controlled value can change which API route is called.

## Acceptance

- One path builder in `base-client.ts`, e.g. `` path`/api/projects/${id}/tasks` ``, encodes each segment with `encodeURIComponent` and rejects `/`, `?`, `#`, `%`, and `.`/`..` segments. Every endpoints file uses it.
- `recordIdSchema` moves to a shared module, and every id input in every router uses it.
- A flow test drives a sample of public procedures from each router with traversal and query payloads and asserts `BAD_REQUEST`, with no outbound fetch.
- A `base-client.test.ts` case shows that an id containing `?` or `../` reaches `fetch` encoded, so the route does not change.
