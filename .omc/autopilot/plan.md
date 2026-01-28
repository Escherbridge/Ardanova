# Implementation Plan: Opportunities Entity-Based Recruitment System

## Overview

This plan implements entity-based recruitment allowing Guilds, Projects, and Shops to create and manage opportunities from their respective contexts. The `/opportunities` page becomes a read-only aggregate feed.

**Key Changes:**
- Add `shopId` FK to Opportunity entity
- Add "Opportunities" tabs to Guild, Project, and Shop detail pages
- Add entity-filtered queries to backend services
- Add source type filter and source badge to opportunities feed
- Modify create page to require entity selection

---

## Backend Tasks

### B1: Add ShopId Foreign Key to Opportunity Entity
**Files:**
- `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/Opportunity.cs`
- `ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models/Entities/Shop.cs`
**Changes:** Add `shopId` FK property, navigation property to Opportunity. Add `Opportunities` collection to Shop.
**Dependencies:** None
**Complexity:** Low
**Agent:** executor-low

### B2: Update Opportunity DTOs with ShopId and Source Info
**Files:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/DTOs/OpportunityDtos.cs`
**Changes:**
- Add `ShopId` to `OpportunityDto` and `CreateOpportunityDto`
- Add new `OpportunitySourceDto` record with: `Type`, `Id`, `Name`, `Logo`, `Slug`
- Add `Source` property to `OpportunityDto`
**Dependencies:** B1
**Complexity:** Low
**Agent:** executor-low

### B3: Add Entity-Filtered Query Methods to IOpportunityService
**Files:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/Services/Interfaces/IOpportunityService.cs`
**Changes:** Add `GetByGuildIdAsync`, `GetByProjectIdAsync`, `GetByShopIdAsync` method signatures
**Dependencies:** B2
**Complexity:** Low
**Agent:** executor-low

### B4: Implement Entity-Filtered Query Methods in OpportunityService
**Files:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/Services/Implementations/OpportunityServices.cs`
**Changes:**
- Inject Guild, Project, Shop repositories
- Implement entity-filtered queries
- Enrich `Source` property in returned DTOs with entity name/logo/slug
- Add `sourceType` filter parameter to `SearchAsync`
**Dependencies:** B3
**Complexity:** Medium
**Agent:** executor

### B5: Update DependencyInjection
**Files:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.Application/DependencyInjection.cs`
**Changes:** Verify repository registrations for Guild, Project, Shop are available
**Dependencies:** B4
**Complexity:** Low
**Agent:** executor-low

### B6: Add API Controller Endpoints for Entity-Filtered Queries
**Files:** `ardanova-backend-api-mcp/api-server/src/ArdaNova.API/Controllers/OpportunitiesController.cs`
**Changes:**
- Add `GET /api/opportunities/guild/{guildId}` endpoint
- Add `GET /api/opportunities/project/{projectId}` endpoint
- Add `GET /api/opportunities/shop/{shopId}` endpoint
- Add `sourceType` query parameter to existing search/getAll endpoint
**Dependencies:** B4
**Complexity:** Medium
**Agent:** executor

### B7: Create Database Migration (EF Core)
**Files:**
- `ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Data/ArdaNovaDbContext.cs` (update entity config)
- Run: `dotnet ef migrations add AddShopIdToOpportunity`
**Changes:**
- Configure Shop-Opportunity relationship in DbContext
- Generate EF Core migration for shopId column
**Dependencies:** B1
**Complexity:** Low
**Agent:** executor-low

### B8: Update Prisma Schema (Client-side)
**Files:** `ardanova-client/prisma/schema.prisma`
**Changes:**
- Add `shopId` field to Opportunity model
- Add `shop` relation to Opportunity
- Add `opportunities` collection to Shop model
- Run: `npx prisma generate`
**Dependencies:** B7 (after DB migration is applied)
**Complexity:** Low
**Agent:** executor-low

---

## Frontend Tasks

### F1: Update tRPC Opportunity Router
**Files:** `ardanova-client/src/server/api/routers/opportunity.ts`
**Changes:**
- Add `shopId` to create schema
- Add `getByGuildId`, `getByProjectId`, `getByShopId` queries
- Add `sourceType` filter to `getAll` query
**Dependencies:** B6
**Complexity:** Medium
**Agent:** executor

### F2: Update API Client Endpoints
**Files:** `ardanova-client/src/lib/api/ardanova/endpoints/opportunities.ts`
**Changes:**
- Add `getByGuildId(guildId: string)` method
- Add `getByProjectId(projectId: string)` method
- Add `getByShopId(shopId: string)` method
**Dependencies:** B6
**Complexity:** Low
**Agent:** executor-low

### F3: Create Source Badge Component
**Files:** `ardanova-client/src/components/opportunities/source-badge.tsx` (NEW)
**Changes:**
- Create component that displays entity type icon, name, and logo
- Link to entity detail page
- Support guild/project/shop variants with distinct styling
**Dependencies:** None
**Complexity:** Low
**Agent:** executor-low

### F4: Update Opportunity Card with Source Badge
**Files:** `ardanova-client/src/components/opportunities/opportunity-card.tsx`
**Changes:**
- Import SourceBadge component
- Display source badge when `opportunity.source` is present
- Position after poster info line
**Dependencies:** F3
**Complexity:** Low
**Agent:** executor-low

### F5: Add Source Type Filter to Feed
**Files:** `ardanova-client/src/app/opportunities/page.tsx`
**Changes:**
- Add `selectedSource` state
- Add source filter dropdown in filters section
- Pass `sourceType` to API query
- Update `hasActiveFilters` and `clearFilters`
**Dependencies:** F1, F4
**Complexity:** Medium
**Agent:** executor

### F6: Update Feed Empty State Messaging
**Files:** `ardanova-client/src/app/opportunities/page.tsx`
**Changes:** Update empty state to explain opportunities come from guilds/projects/shops (already done, verify)
**Dependencies:** None
**Complexity:** Low
**Agent:** executor-low

### F7: Create Guild Opportunities Tab with Permissions
**Files:** `ardanova-client/src/components/guilds/opportunities-tab.tsx` (NEW)
**Changes:**
- Create tab component receiving `guildId`, `guildSlug`, `isOwner`, `userRole`
- Check permission: `isOwner || userRole in ['OWNER', 'ADMIN', 'RECRUITER']`
- Show "Create Opportunity" button only if permitted
- List guild's opportunities with status badges
- Link to opportunity detail and create pages
**Dependencies:** F1
**Complexity:** Medium
**Agent:** executor

### F8: Create Project Opportunities Tab with Permissions
**Files:** `ardanova-client/src/components/projects/opportunities-tab.tsx` (NEW)
**Changes:**
- Create tab component receiving `projectId`, `projectSlug`, `isOwner`, `userRole`
- Check permission: `isOwner || userRole in ['LEAD', 'ADMIN']`
- Show "Create Opportunity" button only if permitted
- List project's opportunities
**Dependencies:** F1
**Complexity:** Medium
**Agent:** executor

### F9: Create Shop Opportunities Tab (Owner Only)
**Files:** `ardanova-client/src/components/shops/opportunities-tab.tsx` (NEW)
**Changes:**
- Create tab component receiving `shopId`, `shopSlug`, `isOwner`
- Check permission: `isOwner` only (shops have no membership)
- Show "Create Opportunity" button only if owner
- List shop's opportunities
**Dependencies:** F1
**Complexity:** Medium
**Agent:** executor

### F10: Add Tab to Guild Detail Page
**Files:**
- `ardanova-client/src/app/guilds/[slug]/page.tsx`
- `ardanova-client/src/components/guilds/index.ts`
**Changes:**
- Add "Opportunities" to tabs array with Briefcase icon
- Import and render OpportunitiesTab when active
- Pass guildId, slug, isOwner, and user's role in guild
- Export OpportunitiesTab from index.ts
**Dependencies:** F7
**Complexity:** Low
**Agent:** executor-low

### F11: Add Tab to Project Detail Page
**Files:**
- `ardanova-client/src/app/projects/[slug]/page.tsx`
- `ardanova-client/src/components/projects/index.ts`
**Changes:** Same pattern as F10 for projects
**Dependencies:** F8
**Complexity:** Low
**Agent:** executor-low

### F12: Add Tab to Shop Detail Page
**Files:**
- `ardanova-client/src/app/shops/[slug]/page.tsx`
- `ardanova-client/src/components/shops/index.ts`
**Changes:** Same pattern as F10 for shops (owner check only)
**Dependencies:** F9
**Complexity:** Low
**Agent:** executor-low

### F13: Create Entity Selector Component
**Files:** `ardanova-client/src/components/opportunities/entity-selector.tsx` (NEW)
**Changes:**
- Create component for selecting entity type (guild/project/shop)
- Fetch user's owned/managed entities for each type
- Display entity cards with logos
- Return selected entityType, entityId, entitySlug
**Dependencies:** F15
**Complexity:** Medium
**Agent:** executor

### F14: Update Create Opportunity Page with Entity Selection
**Files:** `ardanova-client/src/app/opportunities/create/page.tsx`
**Changes:**
- Read URL params: `entityType`, `entityId`, `entitySlug`
- If params present, pre-fill and show entity badge in header
- If no params, show EntitySelector as step 0
- Include entityType-appropriate ID in create mutation
- Add "Change entity" button to switch selection
**Dependencies:** F13, F1
**Complexity:** High
**Agent:** executor-high

### F15: Add Entity List Queries (User's Entities)
**Files:**
- `ardanova-client/src/server/api/routers/guild.ts`
- `ardanova-client/src/server/api/routers/project.ts`
- `ardanova-client/src/server/api/routers/shop.ts`
**Changes:**
- Add `getMyGuilds` query (where user is owner or has ADMIN/RECRUITER role)
- Add `getMyProjects` query (where user is owner or has LEAD/ADMIN role)
- Add `getMyShops` query (where user is owner)
**Dependencies:** None
**Complexity:** Medium
**Agent:** executor

---

## Execution Order

### Phase 1: Backend Foundation (Parallel)
- B1 (entity changes)
- B7 (EF Core migration)

### Phase 2: Backend Service Layer
- B2 → B3 → B4 → B5 → B6

### Phase 3: Client Schema
- B8 (Prisma schema update)

### Phase 4: Frontend API Layer
- F2 → F1

### Phase 5: Frontend Components (Parallel)
- F3, F15, F6

### Phase 6: Entity Tab Components (Parallel)
- F7, F8, F9

### Phase 7: Frontend Integration
- F4 → F5
- F10, F11, F12 (parallel)
- F13 → F14

---

## Summary

| Category | Tasks | Low | Medium | High |
|----------|-------|-----|--------|------|
| Backend | 8 | 5 | 2 | 0 |
| Frontend | 15 | 6 | 7 | 1 |
| **Total** | **23** | **11** | **9** | **1** |

## Permission Matrix

| Entity | Who Can Create | Check Logic |
|--------|----------------|-------------|
| Guild | Owner, Admin, Recruiter | `isOwner OR userRole in ['OWNER','ADMIN','RECRUITER']` |
| Project | Owner, Lead, Admin | `isOwner OR userRole in ['LEAD','ADMIN']` |
| Shop | Owner only | `isOwner` |
