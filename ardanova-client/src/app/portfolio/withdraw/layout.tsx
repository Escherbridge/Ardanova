import type { Metadata } from "next";

export const metadata: Metadata = { title: "Withdraw money" };

export default function WithdrawLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
