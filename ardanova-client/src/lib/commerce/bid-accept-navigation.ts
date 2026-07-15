const taskCommercePath = /^\/tasks\/[A-Za-z0-9-]+\/commerce$/;

/** Navigates only to the internal commerce route supplied by bid acceptance. */
export function navigateToAcceptedBidCommerce(
  commerceUrl: string,
  navigate: (href: string) => void,
): void {
  if (!taskCommercePath.test(commerceUrl)) {
    throw new Error("Bid acceptance returned an invalid commerce route.");
  }

  navigate(commerceUrl);
}
