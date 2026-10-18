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
