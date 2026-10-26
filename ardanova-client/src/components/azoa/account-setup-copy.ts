/** Plain copy for account-setup and identity-verification failures; the node's own wording never renders. */
import {
  describeMutationError,
  describeQueryError,
  isOperatorMessage,
  readErrorCode,
} from "~/components/ui/query-error-copy";

export const ACCOUNT_SETUP_NOT_OPEN =
  "Account setup is not open yet. Nothing about your account has changed. Check back later.";
export const ACCOUNT_STATUS_UNREADABLE =
  "We couldn't check your account setup right now. Nothing changed. Use Check status to try again.";
export const ACCOUNT_SETUP_START_FAILED =
  "Account setup could not be started. Nothing changed. Try again in a moment.";
export const ACCOUNT_SETUP_START_NOT_OPEN =
  "Account setup could not be started because it is not open yet. Nothing changed.";

export interface AccountSetupNotice {
  /** False when the status read already proved setup cannot start, so the button is not offered. */
  setupOpen: boolean;
  statusMessage: string | null;
  ensureMessage: string | null;
}

/** Plain-copy verdict for a failed status read / setup attempt; the node's own wording never renders. */
export function describeAccountSetupNotice({
  statusError,
  statusErrorCode,
  ensureError,
}: {
  statusError?: string | null;
  statusErrorCode?: string | null;
  ensureError?: string | null;
}): AccountSetupNotice {
  const notOpen =
    Boolean(statusError) &&
    (statusErrorCode === "PRECONDITION_FAILED" ||
      isOperatorMessage({ message: statusError }));
  return {
    setupOpen: !notOpen,
    statusMessage: notOpen
      ? ACCOUNT_SETUP_NOT_OPEN
      : statusError
        ? ACCOUNT_STATUS_UNREADABLE
        : null,
    ensureMessage: !ensureError
      ? null
      : isOperatorMessage({ message: ensureError })
        ? ACCOUNT_SETUP_START_NOT_OPEN
        : ACCOUNT_SETUP_START_FAILED,
  };
}

export const IDENTITY_VERIFICATION_NOT_OPEN =
  "Identity verification isn't open yet. Nothing about your account has changed.";

/** Plain copy for the identity-verification problem box, or null when there is nothing to report. */
export function describeIdentityVerificationProblem({
  beginKycError,
  capabilitiesError,
  kycReady,
}: {
  beginKycError?: unknown;
  capabilitiesError?: unknown;
  kycReady?: boolean;
}): string | null {
  const notOpen = (error: unknown) =>
    readErrorCode(error) === "PRECONDITION_FAILED" || isOperatorMessage(error);
  if (beginKycError) {
    return notOpen(beginKycError)
      ? IDENTITY_VERIFICATION_NOT_OPEN
      : describeMutationError(beginKycError, "start identity verification");
  }
  if (capabilitiesError) {
    return notOpen(capabilitiesError)
      ? IDENTITY_VERIFICATION_NOT_OPEN
      : `We couldn't check whether identity verification is open. ${describeQueryError(capabilitiesError).explanation}`;
  }
  return kycReady ? null : IDENTITY_VERIFICATION_NOT_OPEN;
}
