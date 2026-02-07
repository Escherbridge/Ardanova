## 1. Event Management
- [ ] **[P0] Event CRUD**: Create/Update/Delete event with `startDate`, `endDate`, `location`/`meetingUrl`.
- [ ] **[P0] Lifecycle**: `DRAFT` -> `SCHEDULED` -> `LIVE` -> `COMPLETED`/`CANCELLED`.
- [ ] **[P1] Types & Visibility**: 
    - [ ] Types: `MEETING`, `WORKSHOP`, `WEBINAR`, `TOWN_HALL`, `HACKATHON`.
    - [ ] Visibility: `PUBLIC`, `PROJECT_MEMBERS`, `GUILD_MEMBERS`, `INVITE_ONLY`.
- [ ] **[P1] Timezones**: Store/display dates with timezone support.
- [ ] **[P2] Co-Hosts**: `EventCoHost` management (permissions to edit/manage).

## 2. Attendance & Engagement
- [ ] **[P0] RSVP System**: `EventAttendee` with Status (`GOING`, `MAYBE`, `NOT_GOING`, `INVITED`, `ATTENDED`).
- [ ] **[P0] Capacity**: Enforce `maxAttendees`, Waitlist logic (optional).
- [ ] **[P1] Reminders**: `EventReminder` system (User-set, e.g., "1 hour before").
- [ ] **[P2] Notifications**: `EVENT_INVITATION`, `EVENT_REMINDER`, `EVENT_STARTING_SOON`, `EVENT_UPDATED`.

## 3. Discovery
- [ ] **[P1] Search & Filtering**: Filter by Type, Status, Date (Upcoming/Past), Organizer.
- [ ] **[P2] Online vs In-Person**: Handling logic for `meetingUrl` visibility (only to `GOING` attendees).
