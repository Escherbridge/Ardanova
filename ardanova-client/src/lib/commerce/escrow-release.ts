/**
 * Release evidence is the shared node's own confirmed hold state, read back after the call —
 * never a transaction hash a person has to find and type into a form. This file has no
 * "validate the hash the user pasted in" helper for exactly that reason.
 */

const ACCOUNT_SETUP_INCOMPLETE_PREFIX = "Complete your account setup";

export interface EscrowActionErrorPresentation {
  message: string;
  /** True when the backend refused because an account's setup is incomplete, not the action itself. */
  requiresAccountSetup: boolean;
}

/**
 * Recognizes the backend's account-readiness refusal (see
 * `ValueEligibilityMessages.SetupIncomplete` in the .NET API) so the UI can offer a "Complete
 * setup" link instead of leaving the funder at a dead-end error string.
 */
export function describeEscrowActionError(
  message: string,
): EscrowActionErrorPresentation {
  return {
    message,
    requiresAccountSetup: message
      .trim()
      .startsWith(ACCOUNT_SETUP_INCOMPLETE_PREFIX),
  };
}
