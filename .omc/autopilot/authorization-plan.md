# Authorization Implementation Plan

## Overview

Add entity-based permission validation to tRPC routers using Prisma, with proper error handling and toast notifications.

## Permission Matrix

| Entity  | Who Can Create Opportunity | Prisma Query |
|---------|---------------------------|--------------|
| Guild   | Owner, Admin, Recruiter   | `GuildMember.role in ['OWNER', 'ADMIN', 'MANAGER']` OR `Guild.ownerId === userId` |
| Project | Owner, Lead, Admin        | `ProjectMember.role in ['FOUNDER', 'LEADER', 'CORE_CONTRIBUTOR']` OR `Project.ownerId === userId` |
| Shop    | Owner only                | `Shop.ownerId === userId` |

## Tasks

### T1: Add Permission Helper Functions
**File:** `ardanova-client/src/server/api/lib/permissions.ts` (NEW)
**Changes:**
- Create `canCreateGuildOpportunity(db, userId, guildId)` - checks guild ownership or member role
- Create `canCreateProjectOpportunity(db, userId, projectId)` - checks project ownership or member role
- Create `canCreateShopOpportunity(db, userId, shopId)` - checks shop ownership
- Each returns `{ allowed: boolean, reason?: string }`

### T2: Update Opportunity Create Mutation
**File:** `ardanova-client/src/server/api/routers/opportunity.ts`
**Changes:**
- Import permission helpers
- Add permission validation before calling API:
  - If guildId: check canCreateGuildOpportunity
  - If projectId: check canCreateProjectOpportunity
  - If shopId: check canCreateShopOpportunity
- Throw TRPCError with FORBIDDEN code if not permitted

### T3: Create Opportunity Mutations Hook
**File:** `ardanova-client/src/hooks/use-opportunity-mutations.ts` (NEW)
**Changes:**
- Create hook similar to use-shop-mutations.ts
- Add toast.success on successful creation
- Add toast.error on failure with specific error messages
- Include invalidation of relevant queries

### T4: Update Create Opportunity Page
**File:** `ardanova-client/src/app/opportunities/create/page.tsx`
**Changes:**
- Import and use the new mutations hook
- Replace direct useMutation with hook
- Error messages will automatically show via toast

### T5: Add Error Handling to Entity Tabs
**Files:**
- `ardanova-client/src/components/guilds/opportunities-tab.tsx`
- `ardanova-client/src/components/projects/opportunities-tab.tsx`
- `ardanova-client/src/components/shops/opportunities-tab.tsx`
**Changes:**
- Add toast import
- Show toast on mutation errors

## Execution Order

1. T1 (permission helpers)
2. T2 (tRPC router update)
3. T3 (mutations hook)
4. T4 + T5 (UI updates)
5. TypeScript verification
