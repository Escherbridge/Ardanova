# Guild Module — Retroactive Specification

> This document retroactively captures the guild system that was implemented across early development iterations before conductor tracking was established.

## Status: COMPLETE

All backend services, controllers, API client endpoints, tRPC routers, and frontend pages are implemented and functional.

## Architecture

### Backend (7 Services)

**GuildsController** (`ArdaNova.API/Controllers/GuildsController.cs`) — 35+ endpoints across 7 injected services:

| Service | Purpose |
|---------|---------|
| `IGuildService` | Core CRUD, search, pagination, verification |
| `IGuildMemberService` | Member management (CRUD, role updates) |
| `IGuildReviewService` | Guild reviews (CRUD) |
| `IGuildUpdateService` | Guild announcements/updates (CRUD) |
| `IGuildApplicationService` | Join applications (apply, accept, reject) |
| `IGuildInvitationService` | Member invitations (invite, accept, reject) |
| `IGuildFollowService` | Follow/unfollow guilds, follower lists |

### API Endpoints (35+)

**Core Guild**
- `GET /api/guilds` — Get all
- `GET /api/guilds/paged` — Paginated
- `GET /api/guilds/{id}` — By ID
- `GET /api/guilds/slug/{slug}` — By slug
- `GET /api/guilds/owner/{ownerId}` — By owner
- `GET /api/guilds/verified` — Verified guilds only
- `POST /api/guilds` — Create
- `PUT /api/guilds/{id}` — Update
- `DELETE /api/guilds/{id}` — Delete
- `POST /api/guilds/{id}/verify` — Verify guild

**Members** — `{guildId}/members/` (CRUD + role update)
**Reviews** — `{guildId}/reviews/` (CRUD)
**Updates** — `{guildId}/updates/` (CRUD)
**Applications** — `{guildId}/applications/` (apply, accept, reject)
**Invitations** — `{guildId}/invitations/` (invite, accept, reject)
**Follows** — `{guildId}/follow` (follow, unfollow, check), `{guildId}/followers` (list)

### Frontend

**Pages:**
- `/guilds` — Listing page with search and filter (real API data)
- `/guilds/create` — Guild creation form
- `/guilds/[slug]` — Guild detail page
- `/guilds/[slug]/edit` — Guild editing page

**tRPC Router:** `guild.ts` — Thin proxy to `apiClient.guilds.*`
**API Client:** `endpoints/guilds.ts` — HTTP wrapper for all endpoints
