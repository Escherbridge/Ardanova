---
type: plan
---
# Track: Backend Authorization Hardening

> Follows the loop-1 backend lane: the denial envelope and pipeline tests are in place.

## 1. User PII projection [P0]

- [ ] Add `PublicUserProfileDto` and project list, search and by-id reads to it for non-self, non-admin viewers.
- [ ] Make `user.getAll` and `user.search` protected, or rate-limited; stop passing raw DTOs through the BFF.
- [ ] HTTP-pipeline test: an anonymous or foreign actor gets no `email` or `phone`; the subject gets both.

## 2. Actor gating for the 12 service-key-only controllers [P0]

- [ ] Add `[Authorize(Policy = ActorAssertion)]` to the mutating actions; take the actor from the claim.
- [ ] Move the guild-owner, product-creator and event-organizer checks into Application services, with tests.
- [ ] Gate XP award, achievement award, guild verify and leaderboard writes on admin.
- [ ] Add a reflection test that every mutating action has a policy or is on the allowlist.
- [ ] Shrink `guild.ts`, `product.ts`, `xp.ts` and `achievement.ts` to thin proxies.

## 3. Robustness follow-ups [P1]

- [ ] Replace the `Enum.Parse` calls on request values with `TryParse` and `ValidationError`, then remove the `ArgumentException` to 400 mapping.
- [ ] Make the realtime SSE route use `authForPage`.
- [ ] Add a BFF helper that maps `response.status` to a `TRPCError` code, and replace the `throw new Error(response.error)` sites.
