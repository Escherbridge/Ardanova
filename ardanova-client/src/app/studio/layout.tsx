import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default function StudioLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
