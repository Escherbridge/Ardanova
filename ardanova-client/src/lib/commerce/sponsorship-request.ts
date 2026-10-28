import { z } from "zod";

import type { RequestFlow } from "~/lib/commerce/persisted-request-key";
import {
  dollarsToPlatformUnits,
  toPlatformUnitApiAmount,
} from "~/lib/commerce/sponsor-amount-contract";

/** The disclosure the invest page shows; part of what the server binds a sponsorship key to. */
export const FUNDING_DISCLOSURE_VERSION = "funding-disclosure-v1";

/** `fundingIntent.createCheckout`'s body minus the key, mirroring the router's own schema. */
export const sponsorshipWireSchema = z.object({
  projectTokenConfigId: z.string().min(1),
  amount: z.string().regex(/^\d+(?:\.\d{1,2})?$/u),
  disclosureVersion: z.string().min(1).max(100),
  displayNameConsent: z.boolean(),
});

export type SponsorshipWire = z.infer<typeof sponsorshipWireSchema>;

/**
 * One sponsorship attempt at a time per project per person. Two drafts are the same request when
 * the fields `FundingIntentService.ReplayAsync` binds the key to match: the platform-unit AMOUNT
 * STRING, the config and the disclosure. Never the dollars - a rate move changes the amount.
 */
export function sponsorshipRequestFlow(
  projectTokenConfigId: string,
): RequestFlow<SponsorshipWire> {
  return {
    scope: `invest:${projectTokenConfigId}`,
    wire: sponsorshipWireSchema,
    isSameRequest: (saved, draft) =>
      saved.amount === draft.amount &&
      saved.projectTokenConfigId === draft.projectTokenConfigId &&
      saved.disclosureVersion === draft.disclosureVersion,
  };
}

/** The body a FRESH sponsorship of `dollars` would send at today's rate; null when unpriceable. */
export function draftSponsorshipRequest(input: {
  projectTokenConfigId: string;
  dollars: number;
  usdPerArda: number | undefined;
  displayNameConsent: boolean;
}): SponsorshipWire | null {
  const platformUnits = dollarsToPlatformUnits(input.dollars, input.usdPerArda);
  if (!platformUnits) return null;

  return {
    projectTokenConfigId: input.projectTokenConfigId,
    amount: toPlatformUnitApiAmount(platformUnits),
    disclosureVersion: FUNDING_DISCLOSURE_VERSION,
    displayNameConsent: input.displayNameConsent,
  };
}
