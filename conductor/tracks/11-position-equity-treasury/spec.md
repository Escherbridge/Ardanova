# Position Management, Equity Controls & Treasury Pool — Technical Specification

## Overview

This track extends the project team management system with three capabilities:

1. **Position Detail/Edit Pages** — Full-page view and editing for opportunities/positions linked to project roles
2. **Equity Cap Enforcement** — Backend validation ensuring total equity allocations never exceed 100% per project
3. **Treasury Pool Management** — UI for viewing and configuring how project token supply is split between contributor, investor, and founder/member pools

## Dependencies

- Track 09 (Tokenomics) — ProjectTokenConfig, TokenAllocation, ProjectShare, ProjectEquity entities and services
- Track 02 (Projects) — ProjectMember, role assignments, team tab
- Opportunity entity — full CRUD exists (OpportunityService, OpportunitiesController)

## Architecture

### Position Pages

Follows the guild edit page pattern:
- **Detail page** (`/opportunities/[slug]`): Server component → auth check → client view component
- **Edit page** (`/opportunities/[slug]/edit`): Server component → auth + ownership check → shared `<OpportunityForm>` client component
- **Shared form** (`<OpportunityForm>`): Supports `create` and `edit` modes, filters out HOURLY_SHARES from CompensationModel dropdown
- **Team tab click-through**: Position titles in accordion link to `/opportunities/{slug}`

### Equity Cap Enforcement

- `ProjectEquityService.CreateAsync()` and `UpdateAsync()` validate: `sum(sharePercent) + new ≤ 100`
- New method: `GetTotalEquityAsync(projectId)` returns current total allocation
- Backend returns detailed error messages with current/requested/available percentages

### Treasury Pool Management

- Uses existing `ProjectTokenConfig` fields: `contributorSupply`, `investorSupply`, `founderSupply`, `burnedSupply`, `totalSupply`
- Pool percentages are derived: `(classSupply / totalSupply) * 100`
- Display: colored progress bars per pool with token counts
- Future: editable pool target percentages stored in `ProjectShare.allocation` JSONB field

## Key Entities

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| Opportunity | Position/job posting | title, description, type, compensationDetails, compensation, projectRole, slug |
| ProjectEquity | Per-user equity share | projectId, userId, sharePercent, investmentAmount |
| ProjectTokenConfig | Token supply tracking | totalSupply, contributorSupply, investorSupply, founderSupply |
| ProjectShare | Token economic rights | allocation (JSONB), vestingConfig (JSONB), totalSupply |

## CompensationModel Enum

Available options (HOURLY_SHARES hidden from UI):
- FIXED_SHARES — Fixed amount of shares
- ~~HOURLY_SHARES~~ — Hidden from new selection UI
- EQUITY_PERCENT — Percentage of project equity
- HYBRID — Combination of models
- BOUNTY — One-time reward for completion
- MILESTONE — Payment at milestone completion
