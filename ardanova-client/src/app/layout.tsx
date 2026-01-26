import "~/styles/globals.css";

import { type Metadata } from "next";
import { Inter, JetBrains_Mono } from "next/font/google";

import { TRPCReactProvider } from "~/trpc/react";

export const metadata: Metadata = {
  title: "ArdaNova - Community-Owned Project Management",
  description: "Transform ideas into cooperative projects where every contributor becomes a co-owner. Democratic governance, transparent records, and fair profit-sharing for all.",
  keywords: "cooperative, worker ownership, project management, collaboration, democratic governance, profit sharing, community",
  authors: [{ name: "ArdaNova Team" }],
  creator: "ArdaNova",
  publisher: "ArdaNova",
  icons: [{ rel: "icon", url: "/favicon.ico" }],
  openGraph: {
    type: "website",
    locale: "en_US",
    url: "https://ardanova.io",
    title: "ArdaNova - Community-Owned Project Management",
    description: "Transform ideas into cooperative projects where every contributor becomes a co-owner. Democratic governance and fair profit-sharing.",
    siteName: "ArdaNova",
  },
  twitter: {
    card: "summary_large_image",
    title: "ArdaNova - Community-Owned Project Management",
    description: "Transform ideas into cooperative projects where every contributor becomes a co-owner. Democratic governance and fair profit-sharing.",
    creator: "@ArdaNova",
  },
};

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-jetbrains-mono",
  display: "swap",
});

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${inter.variable} ${jetbrainsMono.variable} dark`}>
      <body className="min-h-screen bg-background font-sans antialiased">
        <TRPCReactProvider>{children}</TRPCReactProvider>
      </body>
    </html>
  );
}
