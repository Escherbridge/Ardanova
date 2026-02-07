# Project Management Specification

## Overview
This track manages the core `Project` entity, its lifecycle, the agile work hierarchy, and the governance/opportunities layer. It is the central hub for collaboration.

## Data Models

### Project
- `Id`: UUID (PK)
- `Slug`: String (Unique)
- `Title`: String
- `Description`: String (Markdown)
- `ProblemStatement`: String (Markdown)
- `Solution`: String (Markdown)
- `Categories`: List<String> (Stored as CSV/JSON)
- `Tags`: String (CSV)
- `OwnerId`: UUID (FK `CreatedById`)
- `AssignedGuildId`: UUID? (FK)
- `Status`: Enum (DRAFT, PUBLISHED, SEEKING_SUPPORT, FUNDED, IN_PROGRESS, COMPLETED, CANCELLED)
- `Type`: Enum (TEMPORARY, LONG_TERM, FOUNDATION, BUSINESS, PRODUCT, OPEN_SOURCE, COMMUNITY)
- `Duration`: Enum (ONE_TWO_WEEKS ... ONGOING)
- `FundingGoal`: Decimal?
- `CommerceEnabled`: Boolean
- `StorefrontDescription`: String?

### Agile Hierarchy
- **ProjectMilestone**: High-level goal.
    - Fields: `Title`, `Description`, `TargetDate`, `Status`, `Priority`, `Order`.
- **Epic**: Large feature set. Parent: `Milestone`.
    - Fields: `EquityBudget`, `Progress`, `StartDate`, `TargetDate`.
- **Sprint**: Time-boxed iteration. Parent: `Epic`.
    - Fields: `Goal`, `Velocity`, `Status` (PLANNED, ACTIVE, COMPLETED).
- **Feature**: Specific requirement. Parent: `Sprint`.
- **ProductBacklogItem (PBI)**: Work item. Parent: `Feature`.
    - Type: `FEATURE`, `BUG`, `TECHNICAL_DEBT`, `SPIKE`.
    - Fields: `StoryPoints`, `AcceptanceCriteria`.
- **ProjectTask**: Executable unit. Parent: `Project` (Direct) OR `PBI` (Optional).
    - Fields: `Status` (TODO...BLOCKED), `Priority`, `EstimatedHours`, `ActualHours`, `DueDate`.
    - **Escrow**: `EscrowStatus`, `EquityReward`, `CompensationModel`.

### Opportunity
Opening for engagement.
- `Id`: UUID (PK)
- `Type`: Enum (GUILD_POSITION, PROJECT_ROLE, TASK_BOUNTY, FREELANCE)
- `Title`: String
- `Description`: String
- `ExperienceLevel`: Enum (ENTRY...EXPERT)
- `Compensation`: Decimal?
- `CompensationDetails`: String?
- `IsRemote`: Boolean
- `Location`: String?
- `Skills`: String (CSV)
- `MaxApplications`: Integer?

## API Endpoints (`ProjectsController`, `OpportunitiesController`)

### Projects
| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/projects` | Create project | `CreateProjectDto` |
| `PUT` | `/api/projects/{id}` | Update details | `UpdateProjectDto` |
| `POST` | `/api/projects/{id}/publish` | Publish | - |
| `POST` | `/api/projects/{id}/resources` | Add resource | `CreateProjectResourceDto` |
| `POST` | `/api/projects/{id}/updates` | Post update | `CreateProjectUpdateDto` |
| `POST` | `/api/projects/{id}/support` | Toggle backing | `CreateProjectSupportDto` |
| `POST` | `/api/projects/{id}/milestones` | Add milestone | `CreateProjectMilestoneDto` |

### Opportunities
| Method | Endpoint | Description | DTO |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/opportunities` | Post role/bounty | `CreateOpportunityDto` |
| `POST` | `/api/opportunities/{id}/apply` | Apply | `ApplyToOpportunityDto` |
| `POST` | `/api/opportunities/{id}/comments` | Q&A | `CreateOpportunityCommentDto` |
| `PATCH`| `/api/opportunities/applications/{id}/status` | Review App | `UpdateApplicationStatusDto` |

## Business Logic & Validation

### 1. Project Lifecycle
- **Draft Mode**: Visible only to Creator/Members.
- **Slug Generation**: collision-resistant (append `-1`, `-2`).
- **Commerce**: If `CommerceEnabled`, `Product` entities can be linked.

### 2. Task & Agile
- **Dependencies**: `ProjectTaskDependency` table allows `TaskId` -> `DependsOnId`.
- **Compensation**: Tasks can have attached `EquityReward` or `TaskCompensation` record.
- **Submissions**: Workers submit content/attachments -> `PENDING` -> Owner Reviews -> `APPROVED`.

### 3. Opportunity Workflow
- **Application**: 
    - Applicant provides Cover Letter, Portfolio.
    - Owner updates status: `PENDING` -> `ACCEPTED` / `REJECTED`.
    - `ACCEPTED` application often triggers Member addition or Task assignment.
