import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default async function ProjectsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
