# Analytics & Admin Dashboard — Technical Specification

## Overview

Platform operators need visibility into platform health, financial status, and user activity. Backend has treasury services, user management, and financial tracking. No admin analytics UI exists beyond the KYC admin dashboard.

## Current State

- **Backend**: TreasuryService with three-bucket model, transaction history, audit log
- **Backend**: All financial services (Stripe, payouts, exchange, token balances)
- **Backend**: User management endpoints, XP/achievement tracking
- **Frontend**: KYC admin dashboard exists at `/admin/kyc`
- **Frontend**: No platform analytics, no treasury dashboard, no financial reports

## What Needs to Be Built

### Platform Metrics Dashboard
- Total users (by type: innovator, contributor, supporter)
- Total projects (by status: draft, funding, active, succeeded, failed)
- Total funded amount (via Stripe)
- Active opportunities count
- Guild count and member distribution

### Treasury Overview
- Three-bucket visualization (index fund 60%, liquid reserve 30%, operations 10%)
- Current balances per bucket
- Transaction history with filters
- Rebalancing alerts when buckets drift from targets

### User Analytics
- Registration trends (daily/weekly/monthly)
- Active user counts (DAU/WAU/MAU)
- User type distribution over time
- Top contributors by XP, tasks completed, reviews

### Financial Reports
- Revenue: platform fees, subscription income
- Payouts: total distributed, pending, by project
- Escrow: total locked, released, disputed
- Token metrics: total supply, circulating, locked in gates
