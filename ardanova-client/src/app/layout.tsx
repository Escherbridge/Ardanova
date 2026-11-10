import "~/styles/globals.css";

import { type Metadata } from "next";
import { Toaster } from "sonner";

import { OrganizationJsonLd } from "~/components/brand/organization-json-ld";
import { fontVariables } from "~/styles/fonts";
import { TRPCReactProvider } from "~/trpc/react";
import { AuthSessionProvider } from "~/providers/session-provider";
import { isDevelopmentAuthPreviewEnabled } from "~/server/auth";
import { shareImage, SITE_ORIGIN } from "~/lib/site";
const siteTitle = "ArdaNova — Find it. Fund it. Build it.";
const siteDescription =
  "A workspace for describing a problem, coordinating the work, and keeping two records — work and voice — on agreed terms. The economy is simulated. Platform records do not automatically create legal ownership, equity, or payment rights.";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_ORIGIN),
  title: {
    default: siteTitle,
    template: "%s — ArdaNova",
  },
  description: siteDescription,
  keywords: [
    "shared projects",
    "project coordination",
    "contributor rights",
    "governance",
    "reconciliation",
    "problem solving",
  ],
  authors: [{ name: "Escherbridge" }],
  creator: "Escherbridge",
  publisher: "Escherbridge",
  icons: {
    icon: [
      { url: "/favicon.ico" },
      { url: "/favicon-32.png", sizes: "32x32", type: "image/png" },
      { url: "/favicon-48.png", sizes: "48x48", type: "image/png" },
      { url: "/icon-192.png", sizes: "192x192", type: "image/png" },
    ],
    apple: [
      { url: "/apple-touch-icon.png", sizes: "180x180", type: "image/png" },
    ],
  },
  openGraph: {
    type: "website",
    locale: "en_US",
    title: siteTitle,
    description: siteDescription,
    siteName: "ArdaNova",
    images: [shareImage],
  },
  twitter: {
    card: "summary_large_image",
    title: siteTitle,
    description: siteDescription,
    images: [shareImage.url],
  },
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={fontVariables}>
      <body className="bg-background min-h-screen font-sans antialiased">
        <OrganizationJsonLd />
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
