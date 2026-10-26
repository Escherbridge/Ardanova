import "~/styles/globals.css";

import { type Metadata } from "next";
import { Toaster } from "sonner";

import { fontVariables } from "~/styles/fonts";
import { TRPCReactProvider } from "~/trpc/react";
import { AuthSessionProvider } from "~/providers/session-provider";
import { isDevelopmentAuthPreviewEnabled } from "~/server/auth";

export const metadata: Metadata = {
  title: {
    default: "ArdaNova — Shared project workspace",
    template: "%s — ArdaNova",
  },
  description:
    "Plan shared work, record decisions, and reconcile contributor rights in one reviewable project workspace.",
  keywords: [
    "shared projects",
    "project coordination",
    "contributor rights",
    "governance",
    "reconciliation",
    "problem solving",
  ],
  authors: [{ name: "ArdaNova Team" }],
  creator: "ArdaNova",
  publisher: "ArdaNova",
  icons: [{ rel: "icon", url: "/favicon.ico" }],
  openGraph: {
    type: "website",
    locale: "en_US",
    title: "ArdaNova — Shared project workspace",
    description:
      "Where people who find a problem fund it, build it, and share in what it becomes.",
    siteName: "ArdaNova",
  },
  twitter: {
    // No share image ships yet; "summary_large_image" without one renders a broken card.
    card: "summary",
    title: "ArdaNova — Shared project workspace",
    description:
      "A reviewable workspace for shared work, decisions, and contributor rights.",
  },
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={fontVariables}>
      <body className="bg-background min-h-screen font-sans antialiased">
        <AuthSessionProvider
          refetchOnWindowFocus={!isDevelopmentAuthPreviewEnabled}
        >
          <TRPCReactProvider>{children}</TRPCReactProvider>
          <Toaster
            closeButton
            containerAriaLabel="Notifications"
            position="top-right"
            theme="light"
            toastOptions={{
              classNames: {
                toast:
                  "!rounded-none !border-2 !border-border !bg-card !text-card-foreground !shadow-none",
                title: "font-semibold",
                description: "!text-current",
                success: "!border-success !bg-success !text-success-foreground",
                error:
                  "!border-destructive !bg-destructive !text-destructive-foreground",
                info: "!border-system !bg-system !text-system-foreground",
                warning: "!border-warning !bg-warning !text-warning-foreground",
                loading: "!border-system !bg-system !text-system-foreground",
                actionButton:
                  "!min-h-11 !rounded-none !border !border-current !bg-foreground !px-3 !text-background",
                cancelButton:
                  "!min-h-11 !rounded-none !border !border-current !bg-transparent !px-3 !text-current",
                closeButton:
                  "!size-11 !rounded-none !border !border-border !bg-card !text-card-foreground !shadow-none",
              },
            }}
          />
        </AuthSessionProvider>
      </body>
    </html>
  );
}
