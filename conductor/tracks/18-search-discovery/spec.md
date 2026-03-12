# Search & Discovery Enhancement — Technical Specification

## Overview

Backend has search endpoints on Projects (multi-field text search with status/category/type filtering), Opportunities (type/status/experience/skills), Users (search by name/email), and Guilds. Frontend has basic search on individual listing pages but no unified global search or recommendation system.

## Current State

- Project listing page: Has search + filter using `api.project.getAll` with query params
- Opportunity listing: Has search + filter by type, skill, compensation
- People page: Has user search
- Guild listing: Has search + filter
- **No global search** across entity types
- **No recommendations** or trending content

## What Needs to Be Built

### Global Search
- Unified search bar in the app sidebar or header
- Searches across projects, opportunities, users, guilds simultaneously
- Grouped results by entity type
- Quick keyboard shortcut (Cmd/Ctrl+K)

### Advanced Filters
- Skill matching for opportunities (match user skills to requirements)
- Compensation range filters (equity %, bounty amount)
- Project funding status filters (funding, active, succeeded)

### Featured/Trending
- Algorithm based on: recent funding, member growth, activity volume
- Featured section on explore/dashboard pages
- "Hot Projects" sidebar widget

### Recommended For You
- Based on user skills matching opportunity requirements
- Based on user interests matching project categories
- "Projects looking for your skills" section
