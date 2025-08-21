import { type DefaultSession, type NextAuthConfig } from "next-auth";
import GoogleProvider from "next-auth/providers/google";

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
    async signIn({ user, account, profile, email, credentials }) {
      console.log("[NextAuth] SignIn attempt:", { 
        user: user?.email, 
        account: account?.provider,
        profile: profile?.email 
      });
      
      if (account?.provider === "google" && profile) {
        try {
          // Check if user exists in database
          const existingUser = await db.user.findUnique({
            where: { email: profile.email as string }
          });
          
          if (!existingUser) {
            // Create new user
            await db.user.create({
              data: {
                email: profile.email as string,
                name: profile.name as string,
                image: profile.picture as string,
                emailVerified: new Date(),
              }
            });
            console.log("[NextAuth] Created new user:", profile.email);
          } else {
            console.log("[NextAuth] User already exists:", profile.email);
          }
          
          return true;
        } catch (error) {
          console.error("[NextAuth] Database error during signIn:", error);
          return false;
        }
      }
      
      return true;
    },
    async session({ session, token }) {
      if (token?.email) {
        try {
          const user = await db.user.findUnique({
            where: { email: token.email as string }
          });
          
          if (user) {
            session.user.id = user.id;
            session.user.role = user.role;
            session.user.userType = user.userType;
            session.user.isVerified = user.isVerified;
          }
        } catch (error) {
          console.error("[NextAuth] Database error during session:", error);
        }
      }
      
      return session;
    },
    async jwt({ token, user, account, profile }) {
      console.log("[NextAuth] JWT callback:", { 
        token: token?.email, 
        user: user?.email, 
        account: account?.provider 
      });
      
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
