import type { Metadata } from "next";

export const metadata: Metadata = { title: "Payouts" };

export default function PayoutsSettingsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
