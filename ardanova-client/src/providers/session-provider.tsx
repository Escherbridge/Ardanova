"use client";

import { SessionProvider } from "next-auth/react";

/** Client-side NextAuth session context for the whole app (see src/app/AGENTS.md). */
export function AuthSessionProvider({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return <SessionProvider>{children}</SessionProvider>;
}
