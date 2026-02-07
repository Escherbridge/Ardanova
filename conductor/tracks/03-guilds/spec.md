# Guild Module Specification

## Overview
Guilds are professional communities that provide talent, verify skills, and act as service providers to Projects.

## Data Models

### Guild
- `Id`: UUID (PK)
- `OwnerId`: UUID (FK)
- `Name`: String
- `Slug`: String (Unique)
- `Description`: String (Markdown)
- `Email`: String
- `Phone`: String?
- `Website`: String?
- `Address`: String?
- `Logo`: String?
- `Portfolio`: String? (Markdown/Link)
- `Specialties`: String? (Markdown/Tags)
- `IsVerified`: Boolean
- `Rating`: Decimal (Aggregated)
- `ReviewsCount`: Integer
- `ProjectsCount`: Integer (Auto-calculated)
- `MembersCount`: Integer (Auto-calculated)

### GuildMember
- `UserId`: UUID (FK)
- `GuildId`: UUID (FK)
- `Role`: String (Enum mapped: OWNER, ADMIN, MANAGER, MEMBER, APPRENTICE)
- `JoinedAt`: DateTime

### GuildLogic
- **GuildReview**: `Rating` (1-5), `Comment`, linked to `ProjectId` (optional) and `UserId`.
- **GuildFollow**: User can follow Guild. Preferences: `NotifyUpdates`, `NotifyEvents`, `NotifyProjects`.
- **GuildInvitation**: `Role`, `Message`, `Token` (for email invites).
- **GuildApplication**: `RequestedRole`, `Message`, `Skills`, `Portfolio`, `Availability`.

## API Endpoints (`GuildsController`)

| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/guilds` | Create Guild | `CreateGuildDto` |
| `GET` | `/api/guilds/{id}` | Get details | `GuildDto` |
| `PUT` | `/api/guilds/{id}` | Update settings | `UpdateGuildDto` |
| `POST` | `/api/guilds/{id}/members` | Invite/Add member | `CreateGuildInvitationDto` |
| `PUT` | `/api/guilds/{id}/members/{userId}` | Update role | `UpdateGuildMemberDto` |
| `POST` | `/api/guilds/{id}/reviews` | Post review | `CreateGuildReviewDto` |
| `POST` | `/api/guilds/{id}/updates` | Post update | `CreateGuildUpdateDto` |
| `POST` | `/api/guilds/{id}/apply` | Apply to join | `CreateGuildApplicationDto` |
| `POST` | `/api/guilds/{id}/follow` | Follow settings | `CreateGuildFollowDto` |

## Business Logic & Validation

### 1. Structure
- **Owner**: One owner per guild (`OwnerId` unique constraint).
- **Admins**: Can manage members and edit guild details.

### 2. Verification & Reputation
- **Verification**: Manual process implies `IsVerified` = true.
- **Rating**: Weighted average of `GuildReview` entries.
- **Specialties**: Keywords allowing projects to find guilds for specific needs (e.g., "Frontend", "Auditing").

### 3. Engagement
- **Follow System**: Users can follow to get feed updates (`GuildUpdate`) or event notifications.
- **Applications**: Users apply -> Admins review -> Accept/Reject.
- **Invitations**: Admins invite -> Users accept/decline.

## Integration Points
- **Projects**: Guilds are "assigned" to projects (`AssignedGuildId` on Project) to provide oversight/resources.
- **Opportunities**: Guilds can post `GUILD_POSITION` opportunities.
- **Events**: Guild-hosted events.
