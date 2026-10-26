import type { Metadata } from "next";

import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export const metadata: Metadata = { title: "Projects" };

export default async function ProjectsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
