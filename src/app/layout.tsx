import "~/styles/globals.css";

import { type Metadata } from "next";
import { Inter } from "next/font/google";

import { TRPCReactProvider } from "~/trpc/react";

export const metadata: Metadata = {
  title: "DoSo - The Social Network for Doing",
  description: "Transform innovative ideas into tangible solutions. Join the community that turns dreams into reality through collaboration, support, and integrated business tools.",
  keywords: "innovation, projects, collaboration, business tools, social network, entrepreneurship, SME, startups",
  authors: [{ name: "DoSo Team" }],
  creator: "DoSo",
  publisher: "DoSo",
  icons: [{ rel: "icon", url: "/favicon.ico" }],
  openGraph: {
    type: "website",
    locale: "en_US",
    url: "https://doso.app",
    title: "DoSo - The Social Network for Doing",
    description: "Transform innovative ideas into tangible solutions through community collaboration and integrated business tools.",
    siteName: "DoSo",
  },
  twitter: {
    card: "summary_large_image",
    title: "DoSo - The Social Network for Doing",
    description: "Transform innovative ideas into tangible solutions through community collaboration and integrated business tools.",
    creator: "@DoSoApp",
  },
};

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${inter.variable}`}>
      <body className="min-h-screen bg-gray-50 font-sans antialiased">
        <TRPCReactProvider>{children}</TRPCReactProvider>
      </body>
    </html>
  );
}
