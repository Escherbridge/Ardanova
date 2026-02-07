# ArdaNova Development Workflow

## Architecture: Separation of Concerns

### .NET Backend (`ardanova-backend-api-mcp/api-server/`)
**This is where ALL business logic lives.**

| Layer | Location | Responsibility |
|-------|----------|---------------|
| Controllers | `ArdaNova.API/Controllers/` | HTTP endpoints, request/response handling |
| Services (interfaces) | `ArdaNova.Application/Services/Interfaces/` | Service contracts |
| Services (implementations) | `ArdaNova.Application/Services/Implementations/` | Business logic, validation, orchestration |
| DTOs | `ArdaNova.Application/DTOs/` | Data transfer objects |
| Domain entities | `ArdaNova.Domain/` | Entity models (generated from DBML) |
| Infrastructure | `ArdaNova.Infrastructure/` | EF Core DbContext, repositories, storage |
| Tests | `tests/ArdaNova.Application.Tests/` | Unit tests for services |
| Tests | `tests/ArdaNova.API.Tests/` | Integration tests for API |

### Next.js Frontend (`ardanova-client/`)
**Frontend only. No business logic.**

| Layer | Location | Responsibility |
|-------|----------|---------------|
| API Client | `src/lib/api/` | Typed HTTP client that calls .NET API |
| tRPC Routers | `src/server/api/routers/` | Thin proxies that call `apiClient` |
| Auth | `src/server/auth/` | NextAuth.js session management (exception: uses Prisma for user creation on sign-in) |
| UI Components | `src/components/ui/` | Reusable base primitives (button, card, badge, etc.) |
| Feature Components | `src/components/[feature]/` | Composed domain components (projects/, guilds/, etc.) |
| Pages | `src/app/` | Next.js App Router pages |
| Layouts | `src/components/layouts/` | Page layout wrappers |

### What goes where?

| Task | Where |
|------|-------|
| New service/business logic | .NET `ArdaNova.Application/Services/` |
| New API endpoint | .NET `ArdaNova.API/Controllers/` |
| New DTO | .NET `ArdaNova.Application/DTOs/` |
| Unit tests for services | .NET `tests/ArdaNova.Application.Tests/` |
| API client endpoint wrapper | Next.js `src/lib/api/ardanova/endpoints/` |
| tRPC router (thin proxy) | Next.js `src/server/api/routers/` |
| New reusable UI primitive | Next.js `src/components/ui/` |
| New feature UI component | Next.js `src/components/[feature]/` |
| New page | Next.js `src/app/[route]/page.tsx` |
| Frontend component tests | Next.js vitest (for UI components only) |
| Auth callbacks | Next.js `src/server/auth/` (exception: Prisma OK here) |
| RBAC middleware | Next.js `src/server/api/lib/` (checks session claims, no DB calls) |

## Database Change Workflow

**Source of truth:** `ardanova-client/prisma/database-architecture.dbml`

1. Edit the DBML file
2. Run `npm run generate:prisma` from `ardanova-client/` (DBML → Prisma schema → generate client → db push)
3. Run `npm run generate:csharp` from `ardanova-client/` (DBML → C# EF Core entities)
4. NEVER edit `schema.prisma` directly

## Fullstack Implementation Pattern

Features are built **fullstack** so they can be tested end-to-end as they're implemented. Each task delivers a working vertical slice: backend service → API → frontend UI.

### Adding a new feature (e.g., XP System):

#### 1. Schema (if needed)
- Add/verify tables and enums in DBML
- Run `npm run generate:prisma` + `npm run generate:csharp`

#### 2. Backend (.NET)
- Define service interface in `ArdaNova.Application/Services/Interfaces/`
- Implement service in `ArdaNova.Application/Services/Implementations/`
- Define DTOs in `ArdaNova.Application/DTOs/`
- Create controller in `ArdaNova.API/Controllers/`
- Write service tests in `tests/ArdaNova.Application.Tests/`
- Verify: `dotnet build` and `dotnet test`

#### 3. API Client (Next.js bridge)
- Add endpoint wrapper in `src/lib/api/ardanova/endpoints/`
- Register in `src/lib/api/ardanova/index.ts`
- Create thin tRPC router proxy in `src/server/api/routers/`
- Register in `src/server/api/root.ts`

#### 4. Frontend (Next.js UI)
- Build reusable UI primitives if needed → `src/components/ui/`
- Build feature components → `src/components/[feature]/`
- Create/update pages → `src/app/[route]/page.tsx`
- Follow modular component-driven design (compose from ui/ primitives)
- Follow Swiss grid layout and brand design system (see `tech-stack.md`)
- **All copy and text MUST follow `documentation/BRAND_GUIDELINES.md`**
- Verify: `npm run check` (lint + typecheck)

#### 5. Manual verification
- Run both servers (`npm run dev` + `dotnet run`)
- Test the feature end-to-end in the browser

### tRPC Router Pattern (correct)
```typescript
// CORRECT: Thin proxy to .NET API
import { apiClient } from "~/lib/api";

export const xpRouter = createTRPCRouter({
  getMyXP: protectedProcedure.query(async ({ ctx }) => {
    const response = await apiClient.xp.getByUserId(ctx.session.user.id);
    if (!response.data) throw new TRPCError({ code: "NOT_FOUND" });
    return response.data;
  }),
});
```

```typescript
// WRONG: Direct Prisma calls in Next.js
export const xpRouter = createTRPCRouter({
  getMyXP: protectedProcedure.query(async ({ ctx }) => {
    return await db.xpEvent.findMany({ where: { userId: ctx.session.user.id } });
  }),
});
```

## Frontend Design Principles

### Component-Driven Design
- **Modular composition**: Build complex UI from small, reusable primitives
- **Shared base components** in `src/components/ui/` (button, card, badge, input, dialog, etc.)
- **Feature components** in `src/components/[feature]/` compose ui/ primitives for specific domains
- **Extract shared patterns**: If a pattern appears in 2+ features, extract to ui/
- **Props over configuration**: Components accept data/callbacks via props, not internal state management

### UI Patterns
- **Swiss grid**: 12-column grid, consistent gutters (1.5rem), max-width 1440px
- **Clean and simple**: Minimal decoration, clear hierarchy through typography and spacing
- **Sharp brutalist geometry**: No rounded corners (border-radius: 0)
- **Restrained color**: Dark slate foundations, electric accents used sparingly for emphasis
- **Accessibility**: Semantic HTML, keyboard navigation, ARIA labels

### Brand Guidelines Reference
- **All UI copy MUST reference** `documentation/BRAND_GUIDELINES.md`
- Use values-based language (e.g., "ownership shares" not "tokens")
- Headlines in JetBrains Mono bold, body in Inter
- Electric Cyan for primary CTAs, Neon Green for success, Hot Pink for highlights

## Quality Checks

- **Next.js**: `npm run check` (lint + typecheck) from `ardanova-client/`
- **.NET**: `dotnet build` and `dotnet test` from `ardanova-backend-api-mcp/api-server/`
- **Fullstack**: Both servers running, feature testable in browser
