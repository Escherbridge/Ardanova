import { NextResponse, type NextRequest } from "next/server";
import { getSessionOrError } from "../../../_lib/session";
import { apiClient } from "~/lib/api";

const TIER_RANK: Record<string, number> = {
  BRONZE: 1,
  SILVER: 2,
  GOLD: 3,
  PLATINUM: 4,
  DIAMOND: 5,
};

/**
 * GET /api/sdk/me/credentials/check
 *
 * Check if the current user holds a credential matching the given criteria.
 * Used by Game SDKs for content gating.
 *
 * Query params:
 *   - projectId (optional): filter by project
 *   - guildId (optional): filter by guild
 *   - minTier (optional): minimum tier required (BRONZE/SILVER/GOLD/PLATINUM/DIAMOND)
 *
 * Returns: { hasCredential, tier, status, meetsMinTier }
 */
export async function GET(request: NextRequest) {
  const { session, error } = await getSessionOrError();
  if (error) return error;

  const { searchParams } = new URL(request.url);
  const projectId = searchParams.get("projectId");
  const guildId = searchParams.get("guildId");
  const minTier = searchParams.get("minTier");

  const userId = session!.user.id;

  // Check credential by project or guild
  if (projectId) {
    const response = await apiClient.membershipCredentials.getByProjectAndUser(projectId, userId);

    if (response.error || !response.data) {
      return NextResponse.json({
        hasCredential: false,
        tier: null,
        status: null,
        meetsMinTier: false,
      });
    }

    const credential = response.data;
    const isActive = credential.status === "ACTIVE";
    const credTierRank = TIER_RANK[credential.tier?.toUpperCase() ?? ""] ?? 0;
    const minTierRank = TIER_RANK[minTier?.toUpperCase() ?? ""] ?? 0;

    return NextResponse.json({
      hasCredential: isActive,
      tier: credential.tier,
      status: credential.status,
      meetsMinTier: isActive && credTierRank >= minTierRank,
    });
  }

  if (guildId) {
    const response = await apiClient.membershipCredentials.getByGuildAndUser(guildId, userId);

    if (response.error || !response.data) {
      return NextResponse.json({
        hasCredential: false,
        tier: null,
        status: null,
        meetsMinTier: false,
      });
    }

    const credential = response.data;
    const isActive = credential.status === "ACTIVE";
    const credTierRank = TIER_RANK[credential.tier?.toUpperCase() ?? ""] ?? 0;
    const minTierRank = TIER_RANK[minTier?.toUpperCase() ?? ""] ?? 0;

    return NextResponse.json({
      hasCredential: isActive,
      tier: credential.tier,
      status: credential.status,
      meetsMinTier: isActive && credTierRank >= minTierRank,
    });
  }

  // No projectId or guildId — check if user has ANY active credential
  const allResponse = await apiClient.membershipCredentials.getByUserId(userId);
  const credentials = allResponse.data ?? [];
  const activeCredential = credentials.find((c) => c.status === "ACTIVE");

  if (!activeCredential) {
    return NextResponse.json({
      hasCredential: false,
      tier: null,
      status: null,
      meetsMinTier: false,
    });
  }

  const credTierRank = TIER_RANK[activeCredential.tier?.toUpperCase() ?? ""] ?? 0;
  const minTierRank = TIER_RANK[minTier?.toUpperCase() ?? ""] ?? 0;

  return NextResponse.json({
    hasCredential: true,
    tier: activeCredential.tier,
    status: activeCredential.status,
    meetsMinTier: credTierRank >= minTierRank,
  });
}
