import { type DefaultSession, type NextAuthConfig } from "next-auth";
import GoogleProvider from "next-auth/providers/google";

import type { User } from "@prisma/client";
import { db } from "~/server/db";
import { env } from "~/env";

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

/**
 * Options for NextAuth.js used to configure adapters, providers, callbacks, etc.
 *
 * @see https://next-auth.js.org/getting-started/typescript#module-augmentation
 */
export const authConfig = {
  providers: [
    // Google OAuth (Primary and only provider)
    GoogleProvider({
      clientId: env.GOOGLE_CLIENT_ID,
      clientSecret: env.GOOGLE_CLIENT_SECRET,
      authorization: {
        params: {
          prompt: "consent",
          access_type: "offline",
          response_type: "code",
          scope: "openid email profile"
        }
      }
    }),
  ],
  session: {
    strategy: "jwt",
  },

  callbacks: {
    async signIn({ user, account, profile }) {
      console.log("[NextAuth] SignIn attempt:", {
        user: user?.email,
        account: account?.provider,
        profile: profile?.email
      });

      if (account?.provider === "google" && profile) {
        try {
          const existingUser = await db.user.findUnique({
            where: { email: profile.email as string }
          });

          if (!existingUser) {
            await db.user.create({
              data: {
                email: profile.email as string,
                name: profile.name as string,
                image: profile.picture as string,
                emailVerified: new Date(),
                role: "INDIVIDUAL",
                userType: "VOLUNTEER",
                isVerified: false,
              }
            });
            console.log("[NextAuth] Created new user:", profile.email);
          } else {
            console.log("[NextAuth] User already exists:", profile.email);
          }

          return true;
        } catch (error) {
          console.warn("[NextAuth] Database not ready (fresh DB?), allowing sign-in:", (error as Error).message);
          return true;
        }
      }

      return true;
    },
    async session({ session, token }) {
      if (token.id) { // Ensure token has the id, which should be populated by jwt callback
        session.user.id = token.id as string;
        session.user.role = token.role as string;
        session.user.userType = token.userType as string;
        session.user.isVerified = token.isVerified as boolean;
        session.user.verificationLevel = token.verificationLevel as string;
      }
      // Also propagate default session fields
      if (token.email) {
          session.user.email = token.email;
      }
      if (token.name) {
          session.user.name = token.name;
      }
      if (token.picture) {
          session.user.image = token.picture;
      }

      return session;
    },
    async jwt({ token, user, account, profile }) {
      console.log("[NextAuth] JWT callback:", {
        token: token?.email,
        user: user?.email,
        account: account?.provider,
      });

      try {
        let dbUser: User | null = null;
        if (user) {
          dbUser = await db.user.findUnique({ where: { email: user.email as string } });
        } else if (token.email) {
          dbUser = await db.user.findUnique({ where: { email: token.email as string } });
        }

        if (dbUser) {
          token.id = dbUser.id;
          token.role = dbUser.role;
          token.userType = dbUser.userType;
          token.isVerified = dbUser.isVerified;
          token.verificationLevel = dbUser.verificationLevel;
        }
      } catch (error) {
        console.warn("[NextAuth] Database not ready during JWT (fresh DB?), skipping claims:", (error as Error).message);
      }

      if (account?.provider === "google" && profile) {
        token.email = profile.email;
        token.name = profile.name;
        token.picture = profile.picture;
      }

      return token;
    },
    async redirect({ url, baseUrl }) {
      console.log("[NextAuth] Redirect:", { url, baseUrl });

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
  debug: process.env.NODE_ENV === "development",
  logger: {
    error(code, ...message) {
      console.error(`[NextAuth] Error ${code}:`, ...message);
    },
    warn(code, ...message) {
      console.warn(`[NextAuth] Warning ${code}:`, ...message);
    },
    debug(code, ...message) {
      if (process.env.NODE_ENV === "development") {
        console.log(`[NextAuth] Debug ${code}:`, ...message);
      }
    },
  },
} satisfies NextAuthConfig;
