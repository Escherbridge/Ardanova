# components/guilds

The API decides every guild permission (`ardanova-backend-api-mcp/.../Services/AGENTS.md`
§Guild authority). These components only decide which controls to offer, and they mirror the
API so a control is never shown to someone it would refuse.

## Who manages

`app/guilds/[slug]/page.tsx` computes `canManage = isOwner || userRole === "ADMIN"`, the same
set as `GuildAuthority.CanManageAsync`. `canManage` gates Edit Guild, posting updates, inviting,
removing members, changing roles and reviewing applications. `isOwner` remains only for what the
API reserves to the owner: offering the ADMIN role (invite or role change), and touching an
existing ADMIN (`canChangeMember` in `members-tab.tsx`). The edit route
(`app/guilds/[slug]/edit/page.tsx`) applies the same owner-or-ADMIN rule before rendering the
form; the API re-checks on save.

A non-owner ADMIN's role comes from the members list, so it is unknown while that query loads.
The page derives `roleResolved` (signed out, the owner, or members loaded) and renders no
role-gated control until it is true: Edit Guild and `JoinGuildPanel` wait in the header, and the
non-overview tabs show a spinner instead of mounting with `canManage = false` and then re-rendering
with the manager controls. This is the same wait `JoinGuildPanel` already had, applied to every
`canManage` consumer.

## Joining a guild

`join-guild-panel.tsx` (in the guild header, for signed-in viewers) is the only way into a guild
from the product:

- **Follow / Unfollow** for anyone.
- **Apply to join** for non-members without a pending application. Requestable roles are MEMBER,
  APPRENTICE and MANAGER. ADMIN is appointed by the owner and never requested. The message
  minimum (20 characters) matches the tRPC schema.
- **Withdraw application** while one is pending (`guild.getMyApplications`, `GET .../applications/mine`).
- **Leave guild** for non-owner members (the API's `isLeaving` branch of member delete). The
  owner cannot leave; ownership transfer does not exist yet.

`applications-panel.tsx` (Members tab, managers only) lists pending applications with
Accept/Reject. Accept is disabled for an ADMIN request unless the viewer is the owner, and for an
applicant who is already a member.

There is no "add member directly" control, and the tRPC `guild.addMember` procedure was removed.
The API refuses that path to everyone except platform admins, because it put people in a guild
without their consent.

## Invitations

The invite dialog sends only an email. The API answers the same way whether or not an account
uses that email (no account-existence oracle), so the success copy says "recorded" and does not
claim that a known person was reached.
