---
type: spec
status: proposed
priority: P0
source: loop1 design QA adversarial critique (2026-10-17)
---
# User Directory Privacy

## Problem

`user.getAll`, `user.search` and `user.getById` are `publicProcedure`
(`ardanova-client/src/server/api/routers/user.ts:7, 28, 48`). They proxy
`apiClient.users.*`, which calls the .NET API with the server's own API key and
no actor scoping, and `/api/users` returns full user rows. Anyone without a
session can enumerate every user's email, phone, role, verificationLevel and
trustScore:

```
curl 'http://localhost:3000/api/trpc/user.getAll?batch=1&input={"0":{"json":{}}}'
```

The `/people` directory is the public face of this leak. The API key protects
the server-to-API hop only; it is not a user authorization.

## Outcome

- Directory reads require a signed-in actor.
- The .NET backend returns a `PublicUserProfileDto` (id, display name, avatar,
  headline, skills, public guild/project memberships) for list, search and
  get-by-id, unless the actor is that user or an admin.
- Email, phone, verificationLevel and trustScore never appear in a directory
  response to another non-admin user. The projection is enforced in the
  backend, not in the tRPC router.

## Acceptance

- .NET flow test: list, search and get users as a non-admin actor; assert no
  email or phone in any row. Same calls as the user themself / an admin return
  the full DTO.
- Client: unauthenticated `user.getAll` returns UNAUTHORIZED.
- `/people` still renders cards with only public fields.
