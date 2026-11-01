# Application common

## Failure codes

`Result<T>.Code` is an optional machine discriminator on a FAILURE, added so a node's own code
survives the layers between the transport and a controller (P3-17). Before it,
`AzoaNodeClient.MapError` collapsed every 403 to "AZOA node denied the request." and the money
controllers had to guess the cause from which call site they were in.

- **Who sets it.** `AzoaNodeClient.SendAsync` copies the node envelope's `code` onto every error
  result; a 429 is labelled `UpstreamFailureCodes.UpstreamRateLimited` by status instead. The
  typed clients' route guards set `RouteNotAllowed`, an unconfigured transport sets
  `TransportNotConfigured`, and `DisabledFiatRailGateway` sets `PaymentsNotConfigured`.
  `FundingIntentService` sets `IntentFinished` / `RequestKeyInUse` on its own final-state 409s,
  and `RequestVoided` on a voided key. `IntentFinished` never means "nothing was paid" - the node
  sends it for paid intents too; only `RequestVoided` / `IntentExpired` carry that proof.
- **Who must carry it.** Every helper that re-types a failure (`MapFailure`, `Propagate`,
  `ValueEligibilityFailure.Propagate`, `AzoaCashOutClient.Refused`) ends with
  `.WithCode(source.Code)`. Dropping it silently turns a real `KYB_REQUIRED` back into a guess.
  `AzoaExchangeClient.MapFailure` did not, and round 5 found it: the exchange route's 409
  `ADMISSION_DEADLINE_PASSED` reached the settlement gateway as a bare (ambiguous) Conflict.
- **Who reads it.** `ArdaNova.API/Controllers/MoneyRefusal.cs` (see that directory's AGENTS.md
  §Money refusal codes), and `AzoaSettlementGateway`, which maps `AdmissionDeadlinePassed` to "this
  attempt admitted nothing" (`Services/Implementations/AGENTS.md` §Node proof). The prose in `Error`
  is still collapsed as before; the code is additive and never shown to a person.
- **`Reference`** (round 4 #3) is the code's companion: the upstream's own id for the record a
  failure is about, when its error envelope's `result` named one. Two writers set it: the fiat rail
  (`AzoaNodeClient.SendAsync(failureReference:)`, the intent a 409 INTENT_NEEDS_REVIEW holds, read by
  `MoneyRefusal` for `needs_review`), and `PostService` on a HELP_WANTED create's 409
  `HELP_WANTED_EXISTS` (the id of the ask already holding the task, read by `PostsController` into
  `PostConflictDto.conflictId` so the composer can offer "Edit your ask"; `Services/Implementations/AGENTS.md`
  §Help wanted). `WithCode` keeps it, `WithReference` keeps the code, and a re-typing helper that must
  carry it ends with `.WithReference(source.Reference)`. It is never a success value - a failure still
  has no `Value`.
- **Codes outside money.** `PostService` sets `HELP_WANTED_EXISTS`; `TaskService` sets the claim
  refusals `TASK_TAKEN` / `TASK_DONE` / `TASK_UNDER_AGREEMENT` (`TaskClaimCodes` in `Tasks/ITaskClaimStore.cs`), which
  `TasksController` writes as `{ error, code }`.

## Resource authority

`Security/ResourceAuthority.cs` answers record-level questions for the API
pipeline (`ArdaNova.API/Filters/ResourceAuthorizationFilter`):

- may this actor manage this event, product, or project catalog;
- may they read this referral;
- may this viewer see this event.

It reads only through `IRepository<T>` with id-keyed, projected queries, so it
needs no feature service and works against list-backed repositories in the
API tests.

- **Project managers.** The project creator, or a member whose role is
  FOUNDER, LEADER or CORE_CONTRIBUTOR. This is deliberately the same set as
  `HierarchyAuthorizationService.CanManageProjectAsync`. Change both together.
- **Event visibility.** `EventVisibilityPolicy` is a pure function over
  `EventVisibilityFacts` and the viewer's `EventViewerRelations` (hosted,
  attending, invited, project ids, guild ids). The filter loads the relations
  once for a whole list, and the policy then judges every row in memory.
  "Attending" means `GOING`, `MAYBE` or `ATTENDED`. `INVITED` grants
  visibility but not the meeting link. `NOT_GOING` grants nothing.
- **Affiliation.** `CanAffiliateWithProjectAsync` and
  `CanAffiliateWithGuildAsync` accept the creator or owner, or any member (any
  role). Events.Create uses them so that an event can only be attached to a
  community the organizer belongs to. The set matches the `ProjectIds` and
  `GuildIds` relations that `EventVisibilityPolicy` reads.
- **Not found vs denied.** An event the viewer may not see is `NotFound`, not
  `Denied`, so ids cannot be used to discover private events. Other rules
  return `Denied` for a row that exists, because the caller already knows it
  exists from a list they were allowed to read.

## Upload content policy

`Security/UploadContentPolicy.cs` bounds what a signed storage URL can carry:

- upload content types must be well-formed and must not be types a browser
  executes from the bucket origin (HTML, SVG, XML, JavaScript, wasm);
- download links live 1 to 60 minutes, whatever the query string asks;
- attachment names are stripped of quotes, backslashes and control characters
  before they go into `Content-Disposition`.

A presigned S3 `PUT` cannot enforce a size limit. The declared `FileSize` is
checked, but the bytes actually uploaded are not. Enforcing the real size needs
a presigned `POST` policy with `content-length-range`, which is tracked in
`backend-api-boundary-completion_20261018`.
