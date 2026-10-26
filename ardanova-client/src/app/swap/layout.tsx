import type { Metadata } from "next";

import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export const metadata: Metadata = { title: "Sell your units" };

export default function SwapLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
