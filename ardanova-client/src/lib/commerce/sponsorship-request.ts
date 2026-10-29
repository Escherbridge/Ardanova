import { z } from "zod";

import { looksFinal, type MoneyRefusalCode } from "~/lib/commerce/money-refusal";
import {
  readSavedRequest,
  voidSavedRequest,
  type RequestFlow,
  type SavedRequest,
  type VoidSettlement,
} from "~/lib/commerce/persisted-request-key";
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
 * every field the key's bytes carry matches: the fields `FundingIntentService.ReplayAsync` binds
 * (the platform-unit AMOUNT STRING, the config, the disclosure) AND the naming choice, which a
 * replay would otherwise silently keep from the first send (L-3). Never the dollars.
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
      saved.disclosureVersion === draft.disclosureVersion &&
      saved.displayNameConsent === draft.displayNameConsent,
  };
}

/** A sponsorship the void route revealed: the key stays, and the page follows this one. */
export interface RevealedSponsorship {
  intentId: string;
  status: string;
  holdingReasonDetail?: string | null;
}

/** `fundingIntent.voidRequest`'s answer, as the router validated it. */
export type SponsorshipVoidAnswer =
  | { outcome: "voided"; intentId?: string | null }
  | { outcome: "revealed"; intentId: string; intent: RevealedSponsorship };

type SponsorshipVoidCall = (idempotencyKey: string) => Promise<SponsorshipVoidAnswer>;

function settleByVoid(
  storage: Storage,
  flow: RequestFlow<SponsorshipWire>,
  subjectId: string,
  requestKey: string,
  voidRequest: SponsorshipVoidCall,
): Promise<VoidSettlement<SponsorshipWire, RevealedSponsorship>> {
  return voidSavedRequest<SponsorshipWire, RevealedSponsorship>(
    storage,
    flow,
    subjectId,
    requestKey,
    async (key) => {
      const answer = await voidRequest(key);
      return answer.outcome === "voided"
        ? { outcome: "voided" }
        : { outcome: "revealed", revealed: answer.intent };
    },
  );
}

/**
 * After Support / Try again failed. A final-looking code (`definite`, `finished`) is settled by
 * void-or-reveal - the server, not the code, says whether the key may go; anything else keeps it.
 */
export async function settleSponsorshipFailure(input: {
  storage: Storage;
  flow: RequestFlow<SponsorshipWire>;
  subjectId: string;
  requestKey: string;
  moneyCode: MoneyRefusalCode | null;
  voidRequest: SponsorshipVoidCall;
}): Promise<VoidSettlement<SponsorshipWire, RevealedSponsorship>> {
  if (!looksFinal(input.moneyCode)) {
    return {
      kind: "kept",
      saved: readSavedRequest(input.storage, input.flow, input.subjectId),
      error: null,
    };
  }
  return settleByVoid(input.storage, input.flow, input.subjectId, input.requestKey, input.voidRequest);
}

/** "Cancel that request" (H-1): re-reads storage, then voids the saved key or reveals what it covers. */
export async function cancelSavedSponsorship(input: {
  storage: Storage;
  flow: RequestFlow<SponsorshipWire>;
  subjectId: string;
  voidRequest: SponsorshipVoidCall;
}): Promise<VoidSettlement<SponsorshipWire, RevealedSponsorship> | { kind: "none" }> {
  const saved: SavedRequest<SponsorshipWire> | null = readSavedRequest(
    input.storage,
    input.flow,
    input.subjectId,
  );
  if (!saved) return { kind: "none" };
  return settleByVoid(input.storage, input.flow, input.subjectId, saved.requestKey, input.voidRequest);
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
