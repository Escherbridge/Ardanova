# Application common

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
