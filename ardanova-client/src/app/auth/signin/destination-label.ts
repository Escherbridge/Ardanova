/** Known internal return destinations, most specific prefix first. */
const DESTINATION_LABELS: ReadonlyArray<readonly [prefix: string, label: string]> = [
  ["/portfolio/withdraw", "withdraw money"],
  ["/portfolio/add-funds", "add money to your balance"],
  ["/portfolio", "your balance and holdings"],
  ["/swap", "sell your units"],
  ["/settings/payouts", "your payout details"],
  ["/settings/verification", "verification"],
  ["/projects", "projects"],
];

/** Human name for a normalized internal callbackUrl, or null when the route is not one we name. */
export function destinationLabel(callbackUrl: string): string | null {
  const pathname = callbackUrl.split(/[?#]/u, 1)[0] ?? "";
  for (const [prefix, label] of DESTINATION_LABELS) {
    // Segment-aware prefix: "/portfolio" names "/portfolio/x" but not "/portfolios".
    if (pathname === prefix || pathname.startsWith(`${prefix}/`)) return label;
  }
  return null;
}
