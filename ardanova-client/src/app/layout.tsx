import "~/styles/globals.css";

import { type Metadata } from "next";
import { Inter, JetBrains_Mono } from "next/font/google";

import { TRPCReactProvider } from "~/trpc/react";

export const metadata: Metadata = {
  title: "ArdaNova - Decentralized Project Management",
  description: "Transform innovative ideas into tangible solutions. Join the decentralized community that turns dreams into reality through collaboration, governance, and blockchain-powered tools.",
  keywords: "web3, blockchain, dao, project management, collaboration, algorand, decentralized, governance",
  authors: [{ name: "ArdaNova Team" }],
  creator: "ArdaNova",
  publisher: "ArdaNova",
  icons: [{ rel: "icon", url: "/favicon.ico" }],
  openGraph: {
    type: "website",
    locale: "en_US",
    url: "https://ardanova.io",
    title: "ArdaNova - Decentralized Project Management",
    description: "Transform innovative ideas into tangible solutions through decentralized collaboration and blockchain-powered tools.",
    siteName: "ArdaNova",
  },
  twitter: {
    card: "summary_large_image",
    title: "ArdaNova - Decentralized Project Management",
    description: "Transform innovative ideas into tangible solutions through decentralized collaboration and blockchain-powered tools.",
    creator: "@ArdaNovaDAO",
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
