---
type: plan
---
# Plan: Backend ownership enforcement

## Phase 1 - Ownership in services [P0]
- [ ] EventService update/delete take the actor id; organizer or admin only; pipeline test; remove the check from event.ts.
- [ ] ProductService mutations check project membership through IHierarchyAuthorizationService; pipeline test.
- [ ] Limit Referrals.GetById to the referrer, the referred user, or an admin.
- [ ] Project AttachmentDto.UploadedBy to a public user DTO.

## Phase 2 - Error and identity plumbing [P1]
- [ ] One helper maps the BFF response status to a TRPCError code; replace the `throw new Error(response.error)` sites with it.
- [ ] The sdk/me route runs under runWithActorAssertion.
- [ ] Replace Enum.Parse on request values with TryParse + ValidationError; delete the ArgumentException arm.

## Phase 3 - Generator preservation [P1, CI gate]
- [ ] The generator emits partial entity classes; hand-written members move to Entities/Partials (coordinate with the finance lane for finance entities).
- [ ] DBML column notes become property doc-comments.
- [ ] Add enum members that exist only in C# to the DBML.
- [ ] Regenerating on a clean checkout produces a zero diff.
