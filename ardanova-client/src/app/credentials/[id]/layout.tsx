import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default function CredentialLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
