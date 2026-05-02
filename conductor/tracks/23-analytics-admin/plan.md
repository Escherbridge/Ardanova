# Track 23 — Analytics & Admin Dashboard

## 1. Platform Metrics Dashboard
- [ ] **[P3] Admin metrics page**
    - `/admin/analytics` route (admin-only)
    - Stat cards: total users, total projects, total funded, active opportunities
    - Breakdown by type/status
    - Uses existing count endpoints or new aggregation queries
- [ ] **[P3] Metrics trend charts**
    - Registration trend (line chart, daily/weekly/monthly)
    - Funding trend (bar chart)
    - Lightweight chart library (recharts or similar)

## 2. Treasury Dashboard
- [ ] **[P3] Treasury overview page**
    - `/admin/treasury` route (admin-only)
    - Three-bucket visualization (donut chart or stacked bar)
    - Current balance per bucket with target vs actual
    - Rebalancing alert when drift > 5%
- [ ] **[P3] Treasury transaction history**
    - Paginated table of treasury transactions
    - Filters: date range, bucket, transaction type
    - Export to CSV

## 3. User Analytics
- [ ] **[P3] User analytics page**
    - `/admin/users` route (admin-only)
    - DAU/WAU/MAU metrics
    - User type distribution pie chart
    - Top contributors table (by XP, tasks, reviews)
- [ ] **[P3] User detail admin view**
    - Click user to see full profile + activity
    - XP history, credentials held, projects participated in
    - Admin actions: suspend, verify, grant role

## 4. Financial Reports
- [ ] **[P3] Financial overview page**
    - `/admin/finance` route (admin-only)
    - Revenue summary: fees collected, by period
    - Payout summary: distributed, pending, by project
    - Escrow summary: locked, released, disputed
- [ ] **[P3] Token metrics**
    - Total ARDA supply, circulating, locked in gates
    - Project token summaries across all projects
    - Exchange volume and conversion rates
