import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default async function TasksLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout wide>{children}</AuthenticatedLayout>;
}
