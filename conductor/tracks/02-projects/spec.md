# Project Management — Retroactive Specification

> This document retroactively captures the project management system that was implemented across early development iterations before conductor tracking was established.

## Status: COMPLETE

All backend services, controllers, API client endpoints, tRPC routers, and frontend pages are implemented and functional.

## Architecture

### Backend (10+ Services, 620-line Controller)

**ProjectsController** (`ArdaNova.API/Controllers/ProjectsController.cs`) — 60+ endpoints across 10 injected services:

| Service | Purpose |
|---------|---------|
| `IProjectService` | Core CRUD, search, pagination, publish, featured |
| `IProjectResourceService` | Resources linked to projects (CRUD, mark obtained) |
| `IProjectMilestoneService` | Milestones (CRUD, mark complete) |
| `IProjectMemberService` | Team members (CRUD, role management) |
| `IProjectApplicationService` | Join applications (apply, accept, reject, withdraw) |
| `IProjectCommentService` | Discussion comments (CRUD) |
| `IProjectUpdateService` | Project updates/announcements (CRUD) |
| `IProjectSupportService` | Supporter/backer management (toggle, list) |
| `IGovernanceService` | Proposals, voting, execution (nested under project) |
| `IProjectInvitationService` | Invite users to join (accept, reject, list) |

### API Endpoints (60+)

**Core Project**
- `GET /api/projects` — Get all
- `GET /api/projects/paged` — Paginated list
- `GET /api/projects/search` — Multi-field search (term, status, category, type)
- `GET /api/projects/{id}` — By ID
- `GET /api/projects/slug/{slug}` — By slug
- `GET /api/projects/user/{userId}` — By owner
- `GET /api/projects/status/{status}` — By status
- `GET /api/projects/category/{category}` — By category
- `GET /api/projects/type/{projectType}` — By type
- `GET /api/projects/featured` — Featured projects
- `POST /api/projects` — Create
- `PUT /api/projects/{id}` — Update
- `DELETE /api/projects/{id}` — Delete
- `POST /api/projects/{id}/publish` — Publish
- `POST /api/projects/{id}/featured` — Toggle featured

**Resources** — `{projectId}/resources/` (CRUD + mark obtained)
**Milestones** — `{projectId}/milestones/` (CRUD + mark complete)
**Members** — `{projectId}/members/` (CRUD + role update)
**Applications** — `{projectId}/applications/` (apply, accept, reject, withdraw)
**Comments** — `{projectId}/comments/` (CRUD)
**Updates** — `{projectId}/updates/` (CRUD)
**Support** — `{projectId}/support/` (CRUD + toggle)
**Proposals** — `{projectId}/proposals/` (CRUD + execute, cancel, publish, votes, comments, summary)
**Invitations** — `{projectId}/invitations/` (CRUD + accept, reject, user invitations)

### Frontend

**Pages:**
- `/projects` — Listing page with search and filters (real API data)
- `/projects/create` — Multi-step creation wizard
- `/projects/[slug]` — Detail page with 6 tabbed sub-views

**tRPC Router:** `project.ts` — Thin proxy to `apiClient.projects.*`
**API Client:** `endpoints/projects.ts` — HTTP wrapper for all endpoints

### Agile Hierarchy (Additional Controllers)

The project module includes full agile project management:
- `EpicsController` — Epics with project/milestone scoping
- `SprintsController` — Sprint planning and tracking
- `FeaturesController` — Feature management within sprints
- `ProductBacklogItemsController` — Backlog item CRUD
- `TasksController` — Task CRUD with status tracking

Each has corresponding tRPC router and API client endpoint.
