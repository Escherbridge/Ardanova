export type FundingIntentPresentation = {
  heading: string;
  detail: string;
  poll: boolean;
};

const presentations: Record<string, FundingIntentPresentation> = {
  DRAFT: {
    heading: "FUNDING SETUP INCOMPLETE",
    detail:
      "The server has not opened a payment checkout for this funding intent.",
    poll: true,
  },
  AWAITING_PAYMENT: {
    heading: "PAYMENT VERIFICATION PENDING",
    detail:
      "The server is waiting for the payment provider to report this funding intent.",
    poll: true,
  },
  PAYMENT_VERIFIED: {
    heading: "PAYMENT VERIFIED",
    detail:
      "The server recorded payment verification. Settlement and allocation remain pending.",
    poll: true,
  },
  SETTLEMENT_PENDING: {
    heading: "SETTLEMENT PENDING",
    detail:
      "Payment verification is recorded. The economic settlement is still pending and no portfolio allocation is confirmed.",
    poll: true,
  },
  SETTLED: {
    heading: "SETTLEMENT CONFIRMED",
    detail:
      "The server reports this funding settlement as confirmed. Your portfolio remains the source for any available holdings.",
    poll: false,
  },
  REJECTED: {
    heading: "FUNDING NOT APPROVED",
    detail:
      "The server rejected this funding intent. No portfolio allocation is confirmed.",
    poll: false,
  },
  CANCELLED: {
    heading: "FUNDING CANCELLED",
    detail:
      "This funding intent was cancelled. No portfolio allocation is confirmed.",
    poll: false,
  },
  FAILED: {
    heading: "FUNDING NEEDS REVIEW",
    detail:
      "The server recorded a funding failure. No portfolio allocation is confirmed.",
    poll: false,
  },
};

const unavailable: FundingIntentPresentation = {
  heading: "FUNDING STATUS UNAVAILABLE",
  detail:
    "The authoritative funding intent could not be loaded. A browser redirect is not a payment receipt.",
  poll: false,
};

/** Maps only server-owned funding lifecycle states to conservative return-page copy. */
export function getFundingIntentPresentation(
  status?: string,
): FundingIntentPresentation {
  return status ? (presentations[status] ?? unavailable) : unavailable;
}
