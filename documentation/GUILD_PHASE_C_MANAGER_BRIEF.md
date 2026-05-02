# Guild “set up” — alignment with manager brief (Phase C)

**Source:** [`CONTRIBUTOR_USD400_ROADMAP.md`](../CONTRIBUTOR_USD400_ROADMAP.md) Phase C — *Guild create → profile → slug*, *Members & invitations*, *Guild applications*.

This doc tracks **what the repo implements** vs **what still needs manual QA**.

---

## Implemented (code paths)

| Area | What exists | Notes |
|------|-------------|--------|
| **Create → slug URL** | `GuildForm` → `guild.create` → redirect `/guilds/{slug}` | Backend generates unique slug (`GenerateSlug`). |
| **Profile / detail** | `app/guilds/[slug]/page.tsx` | Loads via `guild.getBySlug`; tabs: overview, updates, members, opportunities, reviews. |
| **Edit** | `app/guilds/[slug]/edit/page.tsx` | Owner-only; `GuildForm` `mode="edit"`. |
| **Verified badge** | Detail header uses `guild.isVerified` | New guilds are unverified until an admin/API verify flow runs. |
| **Owner as member** | `GuildService.CreateAsync` | Creates a `GuildMember` row with role **`OWNER`**, sets **`membersCount = 1`** (single `SaveChanges`). Needed so Members tab and role-based UI work. |
| **REST + tRPC** | `lib/api/ardanova/endpoints/guilds.ts`, `server/api/routers/guild.ts` | Invitations, applications, follow/unfollow, etc. |

---

## Still manual QA (per roadmap)

- **Invitations:** invite → accept/reject → wrong user / edge cases (see `QA-TESTING-CHECKLIST.md` guild section).
- **Applications:** submit → owner list → accept/reject → member appears (verify end-to-end in UI).
- **Follow/unfollow:** confirm UI uses the API if exposed.

---

## Smoke (after creating a guild)

1. Redirect lands on **`/guilds/<slug>`** (not 404).
2. **Members** tab lists the creator with role **OWNER** (after backend change above).
3. **Edit** from `/guilds/<slug>/edit` works for the owner only.

---

## Env reminder

- `.NET` and Next `.env` **`API_KEY`** must match (see [`LOCAL_DEVELOPMENT_SMOKE.md`](./LOCAL_DEVELOPMENT_SMOKE.md)).
