"use client";

import { SessionProvider } from "next-auth/react";

/** Client-side NextAuth session context for the whole app (see src/AGENTS.md). */
export function AuthSessionProvider({
  children,
  refetchOnWindowFocus = true,
}: Readonly<{
  children: React.ReactNode;
  refetchOnWindowFocus?: boolean;
}>) {
  return (
    <SessionProvider refetchOnWindowFocus={refetchOnWindowFocus}>
      {children}
    </SessionProvider>
  );
}
