import {
  type DefaultSession,
  type NextAuthConfig,
  type Session,
} from "next-auth";
import type { JWT } from "next-auth/jwt";
import GoogleProvider from "next-auth/providers/google";

import type { Prisma } from "@prisma/client";
import { db } from "~/server/db";
import { env } from "~/env";

const isDevelopment = process.env.NODE_ENV === "development";

function clearAuthorizationClaims(token: JWT) {
  delete token.id;
  delete token.role;
  delete token.userType;
  delete token.isVerified;
  delete token.verificationLevel;
}

/**
 * Module augmentation for `next-auth` types. Allows us to add custom properties to the `session`
 * object and keep type safety.
 *
 * @see https://next-auth.js.org/getting-started/typescript#module-augmentation
 */
declare module "next-auth" {
  interface Session extends DefaultSession {
    user: {
      id: string;
      role?: string;
      userType?: string;
      isVerified?: boolean;
      verificationLevel?: string;
    } & DefaultSession["user"];
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    id?: string;
    role?: string;
    userType?: string;
    isVerified?: boolean;
    verificationLevel?: string;
  }
}

type AuthCallbacks = NonNullable<NextAuthConfig["callbacks"]>;
type SignInCallbackParams = Parameters<NonNullable<AuthCallbacks["signIn"]>>[0];
type LibraryJwtCallbackParams = Parameters<
  NonNullable<AuthCallbacks["jwt"]>
>[0];
type JwtCallbackParams = Omit<LibraryJwtCallbackParams, "user"> & {
  // Auth.js omits `user` on refresh even though its public declaration marks it required.
  user?: LibraryJwtCallbackParams["user"];
};
type SessionCallbackParams = {
  session: Session;
  token: JWT;
};

const authUserSelect = {
  id: true,
  email: true,
  name: true,
  image: true,
  role: true,
  userType: true,
  isVerified: true,
  verificationLevel: true,
} satisfies Prisma.UserSelect;

type AuthUser = Prisma.UserGetPayload<{ select: typeof authUserSelect }>;

type GoogleIdentity = {
  accountType: string;
  email: string;
  providerAccountId: string;
};

function profileImage(profile: SignInCallbackParams["profile"]): string | null {
  return typeof profile?.picture === "string" ? profile.picture : null;
}

function getVerifiedGoogleIdentity({
  account,
  profile,
}: Pick<SignInCallbackParams, "account" | "profile">): GoogleIdentity | null {
  if (account?.provider !== "google" || profile?.email_verified !== true) {
    return null;
  }

  const email =
    typeof profile.email === "string" ? profile.email.trim().toLowerCase() : "";
  const subject = typeof profile.sub === "string" ? profile.sub.trim() : "";
  const providerAccountId =
    typeof account.providerAccountId === "string"
      ? account.providerAccountId.trim()
      : "";
  const accountType = typeof account.type === "string" ? account.type : "";
  if (
    !email ||
    !subject ||
    !providerAccountId ||
    !accountType ||
    subject !== providerAccountId
  ) {
    return null;
  }

  return {
    accountType,
    email,
    providerAccountId,
  };
}

async function findGoogleAccount(providerAccountId: string) {
  return db.account.findUnique({
    where: {
      provider_providerAccountId: {
        provider: "google",
        providerAccountId,
      },
    },
    select: { user: { select: authUserSelect } },
  });
}

function applyAuthorizationClaims(token: JWT, user: AuthUser) {
  token.id = user.id;
  token.email = user.email;
  token.name = user.name;
  token.picture = user.image;
  token.role = user.role;
  token.userType = user.userType;
  token.isVerified = user.isVerified;
  token.verificationLevel = user.verificationLevel;
}

async function handleSignIn({
  account,
  profile,
}: SignInCallbackParams): Promise<boolean> {
  const identity = getVerifiedGoogleIdentity({ account, profile });
  if (!identity) {
    console.error(
      "[NextAuth] Google identity was incomplete or unverified; sign-in denied.",
    );
    return false;
  }

  try {
    const linkedAccount = await findGoogleAccount(identity.providerAccountId);
    if (linkedAccount) return true;

    const emailOwner = await db.user.findUnique({
      where: { email: identity.email },
      select: { id: true },
    });
    if (emailOwner) {
      console.error(
        "[NextAuth] Google identity requires explicit account linking; sign-in denied.",
      );
      return false;
    }

    await db.user.create({
      data: {
        email: identity.email,
        name: typeof profile?.name === "string" ? profile.name : null,
        image: profileImage(profile),
        emailVerified: new Date(),
        role: "INDIVIDUAL",
        userType: "VOLUNTEER",
        isVerified: false,
        accounts: {
          create: {
            type: identity.accountType,
            provider: "google",
            providerAccountId: identity.providerAccountId,
          },
        },
      },
    });
    if (isDevelopment) {
      console.info("[NextAuth] Created a linked local user record.");
    }

    return true;
  } catch {
    console.error("[NextAuth] User sync failed; sign-in denied.");
    return false;
  }
}

async function handleSession({
  session,
  token,
}: SessionCallbackParams): Promise<Session> {
  if (token.id) {
    session.user.id = token.id;
    session.user.role = token.role;
    session.user.userType = token.userType;
    session.user.isVerified = token.isVerified;
    session.user.verificationLevel = token.verificationLevel;
  }
  if (token.email) session.user.email = token.email;
  if (token.name) session.user.name = token.name;
  if (token.picture) session.user.image = token.picture;

  return session;
}

async function handleJwt({ token, account }: JwtCallbackParams): Promise<JWT> {
  try {
    let dbUser: AuthUser | null = null;
    if (
      account?.provider === "google" &&
      typeof account.providerAccountId === "string" &&
      account.providerAccountId.trim()
    ) {
      const linkedAccount = await findGoogleAccount(
        account.providerAccountId.trim(),
      );
      dbUser = linkedAccount?.user ?? null;
    } else if (typeof token.id === "string" && token.id) {
      dbUser = await db.user.findUnique({
        where: { id: token.id },
        select: authUserSelect,
      });
    }

    if (dbUser) {
      applyAuthorizationClaims(token, dbUser);
    } else {
      clearAuthorizationClaims(token);
    }
  } catch {
    clearAuthorizationClaims(token);
    console.warn(
      "[NextAuth] User claims could not be refreshed; authorization removed.",
    );
  }

  return token;
}

/**
 * Options for NextAuth.js used to configure adapters, providers, callbacks, etc.
 *
 * @see https://next-auth.js.org/getting-started/typescript#module-augmentation
 */
export const authConfig = {
  pages: {
    signIn: "/auth/signin",
    error: "/auth/error",
  },
  providers: [
    // Google OAuth (Primary and only provider)
    GoogleProvider({
      clientId: env.GOOGLE_CLIENT_ID,
      clientSecret: env.GOOGLE_CLIENT_SECRET,
    }),
  ],
  session: {
    strategy: "jwt",
  },

  callbacks: {
    signIn: handleSignIn,
    session: handleSession,
    jwt: handleJwt,
    async redirect({ url, baseUrl }) {
      // Handle callback URLs properly
      if (url.startsWith("/api/auth/callback")) {
        return `${baseUrl}/dashboard`;
      }

      // Allows relative callback URLs
      if (url.startsWith("/")) return `${baseUrl}${url}`;
      // Allows callback URLs on the same origin
      else if (new URL(url).origin === baseUrl) return url;

      // Default fallback
      return `${baseUrl}/dashboard`;
    },
  },
  debug: false,
  logger: {
    error(code) {
      console.error(`[NextAuth] Error ${code}`);
    },
    warn(code) {
      console.warn(`[NextAuth] Warning ${code}`);
    },
  },
} satisfies NextAuthConfig;
