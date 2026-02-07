## 1. Guild Core & Membership
- [ ] **[P0] Guild CRUD**: Create/Update/Delete with `name`, `slug`, `description`, `ownerId`, `portfolio`, `specialties`.
- [ ] **[P0] Ownership & Roles**:
    - [ ] Roles: `OWNER` (1), `ADMIN`, `MANAGER`, `MEMBER`, `APPRENTICE`.
    - [ ] Transfer Ownership logic.
- [ ] **[P0] Membership Management**: Add/Remove members, enforce unique membership.
- [ ] **[P0] Verification**: Admin-controlled `isVerified` badge.

## 2. Growth & Engagement
- [ ] **[P0] Invitations & Applications**:
    - [ ] Invitations: Token-based, Expiration, `PENDING` -> `ACCEPTED`.
    - [ ] Applications: `PENDING` -> `APPROVED`/`REJECTED` with admin review.
- [ ] **[P0] Guild Follow**: `GuildFollow` entity, Notification preferences.
- [ ] **[P1] Reviews & Ratings**: `GuildReview` (1-5 stars, comment), Aggregate `rating` and `reviewsCount`.
- [ ] **[P1] Trending**: Calculate `trendingScore`, `isTrending` flag logic.

## 3. Communication & Projects
- [ ] **[P1] Guild Updates**: `GuildUpdate` posts (Announcements) visible to followers/members.
- [ ] **[P1] Project Assignment**: Link projects to Guild (`assignedGuildId`), track `projectsCount`.
- [ ] **[P2] Notification Integration**: Notify followers on Updates/Events/Projects.
