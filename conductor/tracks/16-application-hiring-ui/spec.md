# Application & Hiring Flow UI — Technical Specification

## Overview

Wire the existing application/hiring backend to frontend UI. Backend has `ProjectApplication`, `OpportunityApplication`, `GuildApplication` entities with accept/reject endpoints in ProjectsController, OpportunitiesController, and GuildsController. Many "Apply Now" buttons on the frontend are currently decorative.

## Current State

- **Backend**: Application endpoints exist in ProjectsController (accept/reject/withdraw), OpportunitiesController, GuildsController
- **tRPC**: Project router has `applyToProject`, `getMyInvitations`, `acceptInvitation`, `rejectInvitation`; opportunity router has application support
- **Frontend**: Apply buttons render but many don't call APIs. No application management dashboard for project owners.

## What Needs to Be Built

### Opportunity Application Flow
1. "Apply Now" button → application form modal (cover letter, portfolio links)
2. Submit calls `api.opportunity.apply`
3. Application status indicator on opportunity card (applied/pending/accepted/rejected)

### Project Application Flow
1. "Join Project" button → application form
2. Submit calls `api.project.applyToProject`
3. Status tracking in user profile

### Application Review Dashboard (Project/Guild Owners)
1. Applications tab in project detail showing pending applications
2. Applicant profile preview
3. Accept/Reject actions with optional message
4. On acceptance: auto-add to team, option to grant credential

### Guild Application Flow
1. "Join Guild" button → application form
2. Guild owner review dashboard

## Key Files
- `src/components/projects/team-tab.tsx` — Add applications section
- `src/app/opportunities/[slug]/page.tsx` — Wire Apply button
- `src/components/opportunities/ApplicationsTab.tsx` — Exists, needs wiring
