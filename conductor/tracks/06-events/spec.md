# Events Module Specification

## Overview
This track manages the lifecycle of events (virtual or physical) and their integration with Projects and Guilds.

## Data Models

### Event
- `Id`: UUID (PK)
- `OrganizerId`: UUID (FK)
- `ProjectId`: UUID? (FK)
- `GuildId`: UUID? (FK)
- `Title`: String
- `Slug`: String (Unique)
- `Description`: String (Markdown)
- `Type`: Enum (MEETING, WORKSHOP, WEBINAR, TOWN_HALL, HACKATHON, SOCIAL)
- `Visibility`: Enum (PUBLIC, PROJECT_MEMBERS, GUILD_MEMBERS, INVITE_ONLY)
- `Status`: Enum (DRAFT, SCHEDULED, LIVE, COMPLETED, CANCELLED)
- `StartDate`: DateTime
- `EndDate`: DateTime
- `Timezone`: String (IANA format)
- `IsOnline`: Boolean
- `Location`: String? (Physical address)
- `LocationUrl`: String? (Map link)
- `MeetingUrl`: String? (Zoom/Google Meet)
- `MaxAttendees`: Integer?
- `AttendeesCount`: Integer
- `CoverImage`: String?

### EventAttendee
- `EventId`: UUID (FK)
- `UserId`: UUID (FK)
- `Status`: Enum (INVITED, GOING, MAYBE, NOT_GOING, ATTENDED)
- `RsvpAt`: DateTime
- `Notes`: String?

## API Endpoints (`EventsController`)

| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/events` | List events | `EventDto[]` |
| `POST` | `/api/events` | Create event | `CreateEventDto` |
| `GET` | `/api/events/{id}` | Get details | `EventDto` |
| `PUT` | `/api/events/{id}` | Update details | `UpdateEventDto` |
| `POST` | `/api/events/{id}/register` | RSVP | `RegisterEventDto` |
| `DELETE` | `/api/events/{id}/register` | Cancel RSVP | - |
| `GET` | `/api/events/{id}/attendees` | List attendees | `EventAttendeeDto[]` |

## Business Logic & Validation

### 1. Creation Context
- **Personal**: Created by User (Organizer).
- **Project**: Linked to `ProjectId`, visibility often `PROJECT_MEMBERS`.
- **Guild**: Linked to `GuildId`, visibility often `GUILD_MEMBERS`.

### 2. Time & Attendance
- **Conflicting RSVPs**: System could warn if User has another event at same time (Future feature).
- **Capacity**: logic enforces `AttendeesCount < MaxAttendees`.
- **Status Transition**: Scheduled -> Live -> Completed (Often manual or cron-based).

### 3. URL Handling
- `LocationUrl` vs `MeetingUrl`: Explicit distinction allows UI to show "Get Directions" vs "Join Call".

## Integration Points
- **Social**: Event creation posts to Feed.
- **Notifications**: `EVENT_INVITATION`, `EVENT_REMINDER` (Scheduled jobs).
- **Calendars**: .ics export (Frontend feature).
