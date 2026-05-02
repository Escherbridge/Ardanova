# Events Module — Retroactive Specification

> This document retroactively captures the events system implemented across early development.

## Status: COMPLETE

All backend services, controllers, API client endpoints, tRPC routers, and frontend pages are implemented and functional.

## Architecture

### Backend

**EventsController** (`ArdaNova.API/Controllers/EventsController.cs`) — 14 endpoints:

| Endpoint | Purpose |
|----------|---------|
| `GET /api/events` | Get all events |
| `GET /api/events/paged` | Paginated listing |
| `GET /api/events/search` | Multi-field search (term, type, status, upcoming) |
| `GET /api/events/{id}` | By ID |
| `GET /api/events/slug/{slug}` | By slug |
| `GET /api/events/upcoming` | Upcoming events |
| `GET /api/events/organizer/{organizerId}` | By organizer |
| `GET /api/events/user/{userId}/registered` | User's registered events |
| `POST /api/events` | Create event |
| `PUT /api/events/{id}` | Update event |
| `DELETE /api/events/{id}` | Delete event |
| `POST /api/events/{id}/register` | RSVP / register |
| `DELETE /api/events/{id}/register` | Cancel registration |
| `GET /api/events/{id}/attendees` | List attendees |

**Service:** `IEventService` / `EventService`

**Search capabilities:**
- Text search by term
- Filter by `EventType` enum
- Filter by `EventStatus` enum
- Filter by upcoming (boolean)
- Pagination support

### Frontend

**Pages:**
- `/events` — Event listing page with search
- `/events/create` — Event creation form

**tRPC Router:** `event.ts` — Thin proxy to `apiClient.events.*`
**API Client:** `endpoints/events.ts` — HTTP wrapper for all endpoints

### Domain Model

**Event entity fields:** id, organizerId, title, slug, description, type (EventType), status (EventStatus), startDate, endDate, location, isVirtual, virtualLink, maxAttendees, coverImage, tags, metadata, createdAt, updatedAt

**EventType enum:** Values include community meetup, workshop, hackathon, demo day, etc.
**EventStatus enum:** DRAFT, PUBLISHED, CANCELLED, COMPLETED

**Related entities:**
- `EventAttendee` — RSVP tracking (userId, eventId, status, registeredAt)
- `EventCoHost` — Co-host management
- `EventReminder` — Scheduled reminders
