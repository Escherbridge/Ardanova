import type { Metadata } from "next";

export const metadata: Metadata = { title: "Verification" };

export default function VerificationSettingsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
