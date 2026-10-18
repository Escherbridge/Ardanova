import {
  buildSignInHref,
  normalizeInternalCallbackUrl,
} from "~/lib/auth-navigation";

/** Copy for each Auth.js error code; ArdaNova's own refusals never blame the provider. */
export const authErrorMessages = {
  Configuration: {
    title: "Sign-in is not configured here.",
    description:
      "This environment cannot reach its authentication provider. No account or workspace action was completed.",
  },
  AccessDenied: {
    title: "This account cannot enter.",
    description:
      "Sign-in was refused, either by your provider or by ArdaNova's account checks. Check the account you selected or ask a workspace steward for help.",
  },
  AccountNotLinked: {
    title: "This email already has an account.",
    description:
      "An ArdaNova account already uses this email, and ArdaNova does not attach a new Google sign-in to it automatically. Ask a workspace steward to link this Google account to it.",
  },
  Verification: {
    title: "That verification is no longer valid.",
    description:
      "The sign-in verification expired or was already used. Start a new sign-in request.",
  },
  Default: {
    title: "Sign-in did not complete.",
    description:
      "Your workspace remains unchanged. Try again, or return to the public site while the connection is checked.",
  },
} as const;

export type AuthErrorReference = keyof typeof authErrorMessages;

/** Maps an untrusted `error` query value to a known reference. */
export function resolveAuthErrorReference(error: string | undefined): AuthErrorReference {
  return error && Object.hasOwn(authErrorMessages, error)
    ? (error as AuthErrorReference)
    : "Default";
}

/** Sign-in link that keeps the caller's internal destination, if it is safe. */
export function retrySignInHref(callbackUrl: string | string[] | undefined): string {
  return buildSignInHref(normalizeInternalCallbackUrl(callbackUrl));
}
