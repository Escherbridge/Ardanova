import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default async function EventsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
