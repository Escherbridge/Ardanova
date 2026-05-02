# Track 11 — Position Management, Equity Controls & Treasury Pool

## 1. Position Detail/Edit Pages (Frontend)

- [x] **[P0] Create shared OpportunityForm component**
    - `ardanova-client/src/components/opportunity-form.tsx`
    - Supports `create` and `edit` modes
    - Filters out HOURLY_SHARES from CompensationModel dropdown
    - Pre-fills form in edit mode from opportunity data
    - Project role select only for project-linked opportunities

- [x] **[P0] Create opportunity detail page**
    - `ardanova-client/src/app/opportunities/[slug]/page.tsx`
    - Server component fetches by slug via `api.opportunity.getById`
    - Renders `<OpportunityDetailView>` with full position details
    - Shows "Edit" button for owners

- [x] **[P0] Create OpportunityDetailView client component**
    - `ardanova-client/src/components/opportunity-detail-view.tsx`
    - Displays: title, status badge, description, skills, compensation, deadline, project role
    - Formatted enum labels via `formatEnumLabel()`

- [x] **[P0] Create opportunity edit page**
    - `ardanova-client/src/app/opportunities/[slug]/edit/page.tsx`
    - Server component with auth + ownership check
    - Renders `<OpportunityForm mode="edit" opportunity={opp} />`

- [x] **[P0] Add layout for opportunity slug routes**
    - `ardanova-client/src/app/opportunities/[slug]/layout.tsx`
    - Wraps with `<AuthenticatedLayout>`

- [x] **[P0] Refactor create page to use shared form**
    - `ardanova-client/src/app/opportunities/create/page.tsx`
    - Replaced 480-line inline form with 20-line wrapper using `<OpportunityForm mode="create">`

- [x] **[P0] Add click-through links in team tab**
    - `ardanova-client/src/components/projects/team-tab.tsx`
    - Linked positions: title → `<Link href="/opportunities/{slug}">`
    - Custom positions: badge → `<Link href="/opportunities/{slug}">`

## 2. Equity Cap Enforcement (Backend)

- [x] **[P0] Add equity cap validation to ProjectEquityService**
    - `api-server/src/ArdaNova.Application/Services/Implementations/ProjectServices.cs`
    - `CreateAsync`: validates `sum(existing) + new ≤ 100%`
    - `UpdateAsync`: validates `sum(others) + new ≤ 100%`
    - Returns detailed error: current %, requested %, available %

- [x] **[P0] Add GetTotalEquityAsync interface method**
    - `api-server/src/ArdaNova.Application/Services/Interfaces/IProjectService.cs`
    - Returns sum of all `sharePercent` for a project

## 3. Treasury Pool Allocation

- [x] **[P0] Add pool allocation display in team tab**
    - Uses existing `projectTokens.getConfigByProject` query
    - Shows colored progress bars for contributor/investor/founder pools
    - Displays token counts and percentages
    - Shows unallocated remainder

- [ ] **[P1] Add pool target percentage editor (owner only)**
    - Store target percentages in `ProjectShare.allocation` JSONB
    - Backend: add get/set allocation endpoints
    - Frontend: editable sliders with sum=100% validation

- [ ] **[P1] Add per-member equity allocation view**
    - Show each member's equity share in team tab
    - Visual breakdown of allocated vs available equity
    - Uses existing ProjectEquity API

## 4. Conductor Track

- [x] **[P0] Create track 11 with spec.md and plan.md**
    - `conductor/tracks/11-position-equity-treasury/`

## 5. Verification

- [ ] **[P0] Build verification**
    - `dotnet build` — .NET backend compiles
    - `npm run build` — Next.js frontend builds
    - Manual: team tab → click position → detail page → edit → save
    - Manual: verify HOURLY_SHARES hidden from compensation dropdowns
