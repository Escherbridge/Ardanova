# Reviews & Trust Enhancement — Technical Specification

## Overview

Backend has GuildReview entity and review endpoints on GuildsController. Trust and reputation are partially modeled through XP, achievements, and credentials. This track adds user-facing review UI and aggregated trust scores.

## Current State

- **Backend**: GuildReview entity exists, GuildsController has review endpoints (create, get by guild)
- **Backend**: XP system, achievements, streaks, leaderboards all complete
- **Backend**: MembershipCredentials serve as trust signals
- **Frontend**: No review UI anywhere, XP/achievements displayed on profile

## What Needs to Be Built

### Guild Reviews
- Wire existing guild review endpoints to UI
- Star rating + text review form on guild detail page
- Review listing with pagination
- Average rating display on guild cards

### Project Reviews
- Review after project completion or task completion
- Rate project: communication, fairness, organization
- Rate contributor: quality, timeliness, collaboration
- Reviews visible on project and user profiles

### Trust Score Display
- Aggregated trust score on user profiles
- Components: XP level, credential count, review average, completion rate, streak length
- Visual trust badge (Bronze/Silver/Gold/Platinum)

### Review Prompts
- After task completion, prompt both parties to review
- After project gate transition, prompt team to review project
- Gentle reminder notifications for pending reviews
