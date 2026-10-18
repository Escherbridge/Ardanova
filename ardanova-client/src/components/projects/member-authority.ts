/** Client mirror of the API's ProjectMemberRolePolicy, used only to decide which controls to show; the API decides. */

const RANK: Record<string, number> = {
  FOUNDER: 4,
  LEADER: 3,
  CORE_CONTRIBUTOR: 2,
  CONTRIBUTOR: 1,
  OBSERVER: 0,
};

/** Lowest rank that carries project-management rights (CORE_CONTRIBUTOR). */
const MANAGER_RANK = 2;

function rankOf(role: string | null | undefined): number {
  return role ? (RANK[role] ?? -1) : -1;
}

/** A role the viewer may give someone: never FOUNDER, and only below the viewer's own rank. */
export function canGrantRole(
  viewerRole: string | null | undefined,
  role: string,
): boolean {
  return role !== "FOUNDER" && rankOf(role) < rankOf(viewerRole);
}

export type MemberExitAction = "leave" | "remove" | null;

/** Which exit control a member row gets: the founder's seat is fixed, anyone else may leave, managers remove those below them. */
export function memberExitAction(input: {
  viewerId: string | null | undefined;
  viewerRole: string | null | undefined;
  memberUserId: string;
  memberRole: string;
}): MemberExitAction {
  if (input.memberRole === "FOUNDER") return null;
  if (input.viewerId && input.viewerId === input.memberUserId) return "leave";

  const viewerRank = rankOf(input.viewerRole);
  return viewerRank >= MANAGER_RANK && rankOf(input.memberRole) < viewerRank
    ? "remove"
    : null;
}
