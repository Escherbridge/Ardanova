---
type: plan
---
# Track — User Directory Privacy

> Spec: [spec.md](spec.md). P0: active data exposure on any deployed stack.

## 1. Backend projection [P0]

- [ ] Add `PublicUserProfileDto` under `ArdaNova.Application/DTOs/`.
- [ ] `UserService` list/search/get return the public projection unless the
  actor id matches or the actor is an admin; thread actor id from the
  controller (`UsersController`) like other actor-scoped endpoints.
- [ ] Flow tests in `tests/ArdaNova.Application.Tests/` (non-admin, self, admin).

## 2. Client proxy [P0]

- [ ] `routers/user.ts`: `getAll`, `search`, `getById` -> `protectedProcedure`,
  passing the session user id to the API client.
- [ ] Update `/people` and any profile consumers to the public DTO shape.

## 3. Verify [P0]

- [ ] Anonymous curl of `user.getAll` returns UNAUTHORIZED.
- [ ] Signed-in non-admin directory response contains no email/phone.
