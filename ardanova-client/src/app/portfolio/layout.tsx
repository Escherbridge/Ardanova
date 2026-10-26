import type { Metadata } from "next";

import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export const metadata: Metadata = { title: "Portfolio" };

export default function PortfolioLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
