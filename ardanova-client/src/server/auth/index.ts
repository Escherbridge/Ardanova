import type { Session } from "next-auth";
import NextAuth from "next-auth";
import { cache } from "react";

import { authConfig } from "./config";

const { auth: uncachedAuth, handlers, signIn, signOut } = NextAuth(authConfig);

const isHostedRuntime = Boolean(
  process.env.RAILWAY_ENVIRONMENT_ID ||
    process.env.RAILWAY_PROJECT_ID ||
    process.env.VERCEL_ENV,
);

export const isDevelopmentAuthPreviewEnabled =
  process.env.NODE_ENV === "development" &&
  !isHostedRuntime &&
  process.env.DEV_AUTH_BYPASS === "true";

function createDevelopmentPreviewSession(): Session {
  return {
    expires: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    user: {
      id: "local-preview-user",
      name: "Local Preview",
      email: "preview@local.ardanova.test",
      image: null,
      role: "INDIVIDUAL",
      userType: "VOLUNTEER",
      isVerified: false,
      verificationLevel: "ANONYMOUS",
    },
  };
}

const auth = cache(async (): Promise<Session | null> => {
  const session = await uncachedAuth();
  return session?.user?.id?.trim() ? session : null;
});

const authForPage = cache(async (): Promise<Session | null> => {
  if (isDevelopmentAuthPreviewEnabled) {
    return createDevelopmentPreviewSession();
  }

  return auth();
});

export { auth, authForPage, handlers, signIn, signOut };
