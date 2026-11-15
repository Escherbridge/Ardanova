import "~/styles/globals.css";

import { type Metadata } from "next";
import { Toaster } from "sonner";

import { OrganizationJsonLd } from "~/components/brand/organization-json-ld";
import { fontVariables } from "~/styles/fonts";
import { TRPCReactProvider } from "~/trpc/react";
import { AuthSessionProvider } from "~/providers/session-provider";
import { isDevelopmentAuthPreviewEnabled } from "~/server/auth";
import { HOME_DESCRIPTION, HOME_TITLE } from "~/lib/public-copy";
import { shareImage, SITE_ORIGIN } from "~/lib/site";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_ORIGIN),
  title: {
    default: HOME_TITLE,
    template: "%s — ArdaNova",
  },
  description: HOME_DESCRIPTION,
  keywords: [
    "solutionary thinking",
    "social ecology",
    "shared ownership",
    "cooperative governance",
    "community stewardship",
    "collective action",
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
    title: HOME_TITLE,
    description: HOME_DESCRIPTION,
    siteName: "ArdaNova",
    images: [shareImage],
  },
  twitter: {
    card: "summary_large_image",
    title: HOME_TITLE,
    description: HOME_DESCRIPTION,
    images: [
      {
        url: shareImage.url,
        alt: shareImage.alt,
        width: shareImage.width,
        height: shareImage.height,
      },
    ],
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
