import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

/**
 * Snapshot of a user's AZOA avatar/wallet linkage. Mirrors the .NET
 * `AzoaAvatarStatusDto`. Carries only the thin reference ArdaNova holds — no
 * keys, mnemonics or balances (those live on the AZOA node).
 */
export interface AzoaAvatarStatusDto {
  avatarId?: string | null;
  walletAddress?: string | null;
  walletId?: string | null;
  avatarLinked: boolean;
  walletBound: boolean;
  /** Consumer-side Tier-2 readiness (avatar present). Does NOT reflect KYC. */
  tier2Ready: boolean;
}

/** Mirrors the .NET `AzoaTier2ReadinessDto`. */
export interface AzoaTier2ReadinessDto {
  ready: boolean;
  /** Reason when not ready (e.g. no avatar linked); null when ready. */
  reason?: string | null;
  avatarId?: string | null;
  walletBound: boolean;
}

// ============ Azoa Avatar Endpoint ============

/**
 * Thin wrapper over the .NET `AzoaAvatarController`
 * (`/api/azoa/avatar/*`). The target user is resolved server-side from the
 * session/JWT claim; `userId` is passed as a query param only during the
 * interim auth-forwarding gap (it is never a request body field — IDOR-safe).
 */
export class AzoaAvatarEndpoint {
  constructor(private client: BaseApiClient) {}

  /** Idempotently link the user to an AZOA avatar. */
  ensureAvatar(_userId?: string): Promise<ApiResponse<AzoaAvatarStatusDto>> {
    return this.client.post<AzoaAvatarStatusDto>("/api/azoa/avatar/ensure");
  }

  /** Ensure a wallet reference is bound/cached for the user's avatar. */
  ensureWallet(_userId?: string): Promise<ApiResponse<AzoaAvatarStatusDto>> {
    return this.client.post<AzoaAvatarStatusDto>("/api/azoa/avatar/wallet");
  }

  /** Read the user's avatar/wallet linkage status. */
  getStatus(_userId?: string): Promise<ApiResponse<AzoaAvatarStatusDto>> {
    return this.client.get<AzoaAvatarStatusDto>("/api/azoa/avatar/status");
  }
}
