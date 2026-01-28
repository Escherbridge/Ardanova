# Opportunities Entity-Based Recruitment System - Specification

## User Request
> The opportunities feed should only be an aggregate view - no creation for jobs or opportunities on the feed itself. Guilds, projects, and shops can recruit for new members.

## Decisions Made
1. **Shop recruitment**: Owner-only (no membership system needed)
2. **Standalone create page**: Keep but redirect to entity selection first
3. **Permissions**: Owner + specific roles (Recruiter/Admin) can create for guilds/projects

---

# Part 1: Requirements Analysis

## Functional Requirements

| ID | Requirement |
|----|-------------|
| **FR-01** | The `/opportunities` page must display opportunities as a read-only aggregate feed |
| **FR-02** | No "Create Opportunity" action should exist on the opportunities feed page |
| **FR-03** | Guilds must be able to create opportunities from their management context |
| **FR-04** | Projects must be able to create opportunities from their management context |
| **FR-05** | Shops must be able to create opportunities for recruiting members |
| **FR-06** | Opportunities must display their source entity (Guild/Project/Shop) |
| **FR-07** | The standalone `/opportunities/create` page should require entity selection |

## Non-Functional Requirements

| ID | Requirement | Category |
|----|-------------|----------|
| **NFR-01** | Aggregate feed must load within 3 seconds for up to 50 items | Performance |
| **NFR-02** | Source entity (Guild/Project/Shop) must be visually distinct on each opportunity card | UX |
| **NFR-03** | Only authorized entity owners/admins can create opportunities for their entity | Security |
| **NFR-04** | Users should be able to filter opportunities by source type (Guild/Project/Shop) | UX |

## Implicit Requirements

| ID | Requirement |
|----|-------------|
| **IR-01** | Add `shopId` FK to Opportunity entity |
| **IR-02** | Create "Opportunities" tab on Guild detail pages |
| **IR-03** | Create "Opportunities" tab on Project detail pages |
| **IR-04** | Create "Opportunities" tab on Shop detail pages |
| **IR-05** | Opportunity display must show entity logo/avatar alongside user info |
| **IR-06** | Database migration required for Shop-Opportunity relationship |

## Out of Scope
- Direct messaging between applicants and recruiters
- Payment/escrow for bounties
- Opportunity recommendations/matching algorithm
- Opportunity templates
- Multi-entity ownership
- Opportunity expiration automation
- Analytics/metrics

---

# Part 2: Technical Specification

## Tech Stack
| Layer | Technology |
|-------|------------|
| **Frontend** | Next.js 14 (App Router), React, TypeScript |
| **State/Data** | tRPC, React Query |
| **Backend** | .NET 8, ASP.NET Core |
| **Database** | Entity Framework Core, PostgreSQL |
| **UI Components** | shadcn/ui |

## Architecture Flow

### Current Flow
```
User -> /opportunities/create (standalone) -> Creates opportunity
```

### Proposed Flow
```
User -> /guilds/[slug] or /projects/[slug] or /shops/[slug]
     -> "Opportunities" tab
     -> "Create Opportunity" button (permission-gated)
     -> /opportunities/create?entityType=X&entityId=Y (pre-filled)
```

## Permissions Model

| Entity | Who Can Create |
|--------|----------------|
| **Guild** | Owner, Admin, Recruiter role |
| **Project** | Owner (createdById), Admin/Lead role |
| **Shop** | Owner only |

## Files to Modify

### Backend (C#)
| File | Change |
|------|--------|
| `ArdaNova.Domain/Models/Entities/Opportunity.cs` | Add `shopId` FK |
| `ArdaNova.Domain/Models/Entities/Shop.cs` | Add `Opportunities` navigation |
| `ArdaNova.Application/DTOs/OpportunityDtos.cs` | Add `ShopId` to DTOs |
| `ArdaNova.Application/Services/*/OpportunityServices.cs` | Add entity-filtered queries |
| `ArdaNova.Infrastructure/Data/ArdaNovaDbContext.cs` | Register relationship |

### Frontend (TypeScript)
| File | Change |
|------|--------|
| `opportunities/page.tsx` | Add source type filter, display source entity |
| `opportunities/create/page.tsx` | Add entity selection step |
| `guilds/[slug]/page.tsx` | Add "Opportunities" tab |
| `projects/[slug]/page.tsx` | Add "Opportunities" tab |
| `shops/[slug]/page.tsx` | Add "Opportunities" tab |
| `server/api/routers/opportunity.ts` | Add `shopId`, entity-filtered queries |

## Files to Create

### Frontend
| File | Purpose |
|------|---------|
| `components/guilds/opportunities-tab.tsx` | Guild opportunities management |
| `components/projects/opportunities-tab.tsx` | Project opportunities management |
| `components/shops/opportunities-tab.tsx` | Shop opportunities management |
| `components/opportunities/entity-selector.tsx` | Entity selection component |
| `components/opportunities/source-badge.tsx` | Source badge component |

## Database Migration

```sql
ALTER TABLE "Opportunity"
ADD COLUMN "shopId" VARCHAR(255) NULL;

ALTER TABLE "Opportunity"
ADD CONSTRAINT "FK_Opportunity_Shop_shopId"
FOREIGN KEY ("shopId") REFERENCES "Shop"("id")
ON DELETE SET NULL;

CREATE INDEX "IX_Opportunity_shopId" ON "Opportunity"("shopId");
```

## Implementation Order

1. Backend schema change - Add `shopId` FK, migration
2. Backend service methods - Entity-filtered queries
3. Backend controller endpoints
4. Frontend tRPC router updates
5. Opportunities tab components (guild, project, shop)
6. Entity detail pages - Add tabs
7. Create wizard - Entity selection step
8. Opportunity card - Source badge
9. Feed filter - Source type filter
