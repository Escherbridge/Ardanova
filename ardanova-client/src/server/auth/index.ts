import type { Session } from "next-auth";
import NextAuth from "next-auth";
import type { NextRequest } from "next/server";
import { cache } from "react";

import { db } from "~/server/db";
import { authConfig } from "./config";

const {
  auth: uncachedAuth,
  handlers: providerHandlers,
  signIn,
  signOut,
} = NextAuth(authConfig);

const isHostedRuntime = Boolean(
  process.env.RAILWAY_ENVIRONMENT_ID ||
    process.env.RAILWAY_PROJECT_ID ||
    process.env.VERCEL_ENV,
);

export const isDevelopmentAuthPreviewEnabled =
  process.env.NODE_ENV === "development" &&
  !isHostedRuntime &&
  process.env.DEV_AUTH_BYPASS === "true";

/** Fixed identity written by `scripts/seed-local-demo.ts`; see `AGENTS.md` §Development preview. */
export const DEVELOPMENT_PREVIEW_USER_ID = "local-preview-user";
const DEVELOPMENT_PREVIEW_USER_EMAIL = "preview@local.ardanova.test";
const PREVIEW_SESSION_LIFETIME_MS = 60 * 60 * 1000;

function previewExpiry(): string {
  return new Date(Date.now() + PREVIEW_SESSION_LIFETIME_MS).toISOString();
}

/** Page-only stand-in for an unseeded database; never authorizes data access. */
function createUnseededPreviewSession(): Session {
  return {
    expires: previewExpiry(),
    user: {
      id: DEVELOPMENT_PREVIEW_USER_ID,
      name: "Local Preview",
      email: DEVELOPMENT_PREVIEW_USER_EMAIL,
      image: null,
      role: "INDIVIDUAL",
      userType: "VOLUNTEER",
      isVerified: false,
      verificationLevel: "ANONYMOUS",
    },
  };
}

/** Claims for the seeded preview row, read from the database like a JWT refresh. */
const loadSeededPreviewSession = cache(async (): Promise<Session | null> => {
  try {
    const user = await db.user.findUnique({
      where: { id: DEVELOPMENT_PREVIEW_USER_ID },
      select: {
        id: true,
        email: true,
        name: true,
        image: true,
        role: true,
        userType: true,
        isVerified: true,
        verificationLevel: true,
      },
    });
    if (user?.email !== DEVELOPMENT_PREVIEW_USER_EMAIL) return null;

    return {
      expires: previewExpiry(),
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
        image: user.image,
        role: user.role,
        userType: user.userType,
        isVerified: user.isVerified,
        verificationLevel: user.verificationLevel,
      },
    };
  } catch {
    return null;
  }
});

/** The provider (Google) session only, ignoring the development preview. */
const providerAuth = cache(async (): Promise<Session | null> => {
  const session = await uncachedAuth();
  return session?.user?.id?.trim() ? session : null;
});

/** The request identity: the provider session, else the guarded seeded preview identity. */
const auth = cache(async (): Promise<Session | null> => {
  const session = await providerAuth();
  if (session || !isDevelopmentAuthPreviewEnabled) return session;

  return loadSeededPreviewSession();
});

/** Page-shell identity: `auth()`, else an unseeded synthetic preview for visual QA. */
const authForPage = cache(async (): Promise<Session | null> => {
  const session = await auth();
  if (session || !isDevelopmentAuthPreviewEnabled) return session;

  return createUnseededPreviewSession();
});

function isSessionEndpoint(request: NextRequest): boolean {
  return request.nextUrl.pathname.replace(/\/+$/, "") === "/api/auth/session";
}

/** Auth.js route handlers; under the preview, `/session` reports the same identity as `auth()`. */
const handlers = {
  POST: providerHandlers.POST,
  async GET(request: NextRequest) {
    if (
      isDevelopmentAuthPreviewEnabled &&
      isSessionEndpoint(request) &&
      !(await providerAuth())
    ) {
      const preview = await loadSeededPreviewSession();
      if (preview) return Response.json(preview);
    }

    return providerHandlers.GET(request);
  },
};

export { auth, authForPage, handlers, providerAuth, signIn, signOut };
