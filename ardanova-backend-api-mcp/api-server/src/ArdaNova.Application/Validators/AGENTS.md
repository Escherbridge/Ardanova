# Request validators

A request validator is a `RequestValidator<TDto>` with one `Check` method.
`AddRequestValidators` (called from `AddApplication`) registers every concrete
validator in the assembly. The API's `RequestValidationFilter` runs the one
that matches each bound argument's type. A new DTO therefore needs only a new
class here, with no controller or service edit.

The library was hand-rolled rather than adding FluentValidation for two
reasons: the rules are few and flat, and this lane may not add dependencies.
`FieldRules` holds the shared checks.

## Rules (loop 3)

| DTO | Rules |
| --- | --- |
| `CreateEventDto` / `UpdateEventDto` | title 1-200 characters; description up to 20,000; location up to 500; `meetingUrl`/`locationUrl` absolute `http(s)` only; `coverImage` `http(s)` or a site path; timezone known; `maxAttendees` 1-100,000; end not before start (when both are sent) |
| `RegisterEventDto` | notes up to 500 |
| `CreateProductDto` / `UpdateProductDto` | name 1-200; `price`/`cost` 0 to 10^12; sku and category up to 100 |
| `CreateReferralDto` | referrer is not the referred user; code is 4-64 characters from `[A-Za-z0-9_-]` |

Why these rules:

- **URLs.** QA stored `javascript:alert(document.cookie)` as a meeting link.
  Any UI that renders it as an `href` is one click from stored XSS.
- **Timezones.** The event form sends `UTC`, `EST`, `PST`, `CET`, `IST` and
  `JST`, and not all of those are IANA ids on every host. They are accepted
  explicitly. Anything else must resolve through
  `TimeZoneInfo.TryFindSystemTimeZoneById`. Strict IANA validation would have
  broken the form.
- **Updates.** Update DTOs treat `null` as "unchanged". An update that sends
  only `endDate` cannot be compared with the stored start here; the events
  service owns that check.
- **Not covered.** HTML in descriptions is stored as text. Encoding it is the
  renderer's job. React escapes it by default, but `dangerouslySetInnerHTML`
  does not.

## Rules (loop 4)

| DTO | Rules |
| --- | --- |
| `CreateGuildDto` / `UpdateGuildDto` | name 1-200; description up to 20,000 (required on create); `email` a plain address of at most 254 characters (required on create); `website` absolute `http(s)`; `logo` `http(s)` or a site path; phone up to 50; address up to 500; portfolio and specialties up to 20,000 |
| `UpdateUserDto` | name up to 200; bio up to 20,000; location up to 500; phone up to 50; `website` absolute `http(s)`; `linkedIn`/`twitter` a handle or an `http(s)` URL; `image` `http(s)` or a site path |
| `CreateGroupConversationDto` / `UpdateGroupConversationDto` | name 1-200; `avatarUrl` `http(s)` or a site path |
| `CreateNotificationDto` | title 1-500; message 1-20,000; `actionUrl` `http(s)` or a site path |

Loop 4 QA stored a guild whose website was `javascript:alert(document.domain)`
and whose email was `not-an-email`. These rules mirror the BFF's zod schemas
(`routers/profile.ts` and `endpoints/guilds.ts`). They are stricter in one
place: the guild `logo`, which the BFF accepts as any string. The guild form's
placeholder (`https://example.com/logo.png`) asks for a URL, and a scheme-less
value would not render as an image anyway, so the API now returns a field error
for it.

`HandleOrWebUrl` accepts a bare handle such as `@jade` or `in/jade`, because the
profile form does. A value that names a scheme must use `http(s)`, and any
control character is refused. Browsers strip tabs and newlines out of an `href`
scheme, so `java\tscript:` would otherwise slip past the scheme check.
