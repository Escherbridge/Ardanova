/**
 * AZOA SDK — client-side READ / STATE layer
 * ==========================================
 *
 * WHAT THIS IS
 * ------------
 * A browser-safe singleton `AzoaApiClient` (from `azoa-sdk/api`) used ONLY to
 * READ AZOA "oasis-objects" — wallets, portfolios, holons, and quest run-state —
 * into client state. React Query (see `queries.ts`) is the state store: SDK
 * reads land in the query cache and that cache IS the client-held object state.
 *
 * WHAT THIS IS NOT
 * ----------------
 * This layer performs NO value-bearing writes. Mint / transfer / allocation /
 * swap-execute / bridge and any other economics-touching operation goes through
 * the .NET backend (ArdaNova.API), per the project architecture rule:
 * "business logic lives in the backend; Next.js is frontend only." The SDK's
 * write methods (`mintNft`, `fungibleMint`, `transferNft`, `executeSwap`,
 * `initiateBridge`, ...) are intentionally never invoked from here.
 *
 * CUSTODY / SECURITY DECISION
 * ---------------------------
 * The tenant API key is a custodial secret and MUST NOT reach the browser, so
 * this client is constructed with NO `apiKey`. It is configured from
 * `NEXT_PUBLIC_AZOA_BASE_URL` only.
 *
 * Two read paths, by trust level:
 *   1. PUBLIC / SESSION-SCOPED reads — wallets, portfolio, holons, quest
 *      run-state for the signed-in user. These are safe to issue from the
 *      browser using the *user's own* bearer token (their avatar JWT), which we
 *      attach per call via `getAzoaSdkClient(token)`. The token authorizes only
 *      the user's own objects; it is not a tenant-wide credential.
 *   2. TENANT-KEY reads — any read that genuinely requires the tenant API key
 *      must NOT happen here. Route those through a server-side Next.js route
 *      handler / tRPC proxy that holds the tenant key server-side and calls the
 *      .NET API. Do not add `apiKey` to this browser client.
 *
 * If no user token is available the client still works for any endpoint the
 * backend exposes anonymously; auth-required endpoints will return an
 * `SdkError` (surfaced through React Query) rather than silently using a secret.
 */

import { AzoaApiClient } from "azoa-sdk/api";
import type { AzoaApiConfig } from "azoa-sdk/api";

/**
 * Public base URL of the AZOA API. `NEXT_PUBLIC_*` vars are inlined by Next.js
 * at build time, so reading `process.env` directly here is the standard,
 * browser-safe pattern. We do NOT pull this through `~/env.js` because that
 * module also validates server-only secrets (the tenant key) we must keep off
 * the client.
 */
const AZOA_BASE_URL =
  process.env.NEXT_PUBLIC_AZOA_BASE_URL ?? "http://127.0.0.1:5147";

/**
 * Cache one anonymous client (no user token). Token-scoped clients are created
 * on demand and not cached, since the token is request/session specific.
 */
let anonymousClient: AzoaApiClient | null = null;

function buildClient(token?: string): AzoaApiClient {
  const config: AzoaApiConfig = {
    baseUrl: AZOA_BASE_URL,
    // Deliberately NO `apiKey` — the tenant key never enters the browser.
    ...(token ? { token } : {}),
  };
  return new AzoaApiClient(config);
}

/**
 * Get a browser-safe AZOA read client.
 *
 * @param token Optional bearer token — the signed-in user's own avatar JWT.
 *   When provided, a fresh token-scoped client is returned (not cached). When
 *   omitted, a shared anonymous client is returned for public reads.
 */
export function getAzoaSdkClient(token?: string): AzoaApiClient {
  if (token) {
    return buildClient(token);
  }
  anonymousClient ??= buildClient();
  return anonymousClient;
}

/**
 * Shared anonymous singleton for convenience in hooks/components that only need
 * public reads. Prefer `getAzoaSdkClient(userToken)` when a user session token
 * is available so reads are scoped to the user's own objects.
 */
export const azoaSdkClient = getAzoaSdkClient();
