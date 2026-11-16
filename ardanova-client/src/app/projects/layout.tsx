import type { Metadata } from "next";

import AuthenticatedLayout from "~/components/layouts/authenticated-layout";
import { isSignedOutProjectsExplainer } from "~/lib/projects-explainer-request";

export const metadata: Metadata = { title: "Projects" };

export default async function ProjectsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  if (await isSignedOutProjectsExplainer()) {
    return children;
  }
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
