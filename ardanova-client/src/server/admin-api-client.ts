import "server-only";

import { env } from "~/env";
import { ArdaNovaApiClient } from "~/lib/api/ardanova";

/** Creates the server-only client required by .NET's administrative payout policy. */
export function createAdminApiClient(baseUrl: string, serviceApiKey: string, adminApiKey: string): ArdaNovaApiClient {
  if (!adminApiKey) {
    throw new Error("ADMIN_API_KEY is required for administrative ArdaNova API operations.");
  }

  return new ArdaNovaApiClient({
    baseUrl,
    headers: {
      "X-Api-Key": serviceApiKey,
      "X-Admin-Api-Key": adminApiKey,
    },
  });
}

/** Resolves administrative credentials only inside an authorized server procedure. */
export function getAdminApiClient(): ArdaNovaApiClient {
  return createAdminApiClient(env.API_URL, env.API_KEY, process.env.ADMIN_API_KEY ?? "");
}
