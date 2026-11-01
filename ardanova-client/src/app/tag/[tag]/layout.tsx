import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

/** Tag pages are signed-in only for now (SR-03 plan §1, Revision 1 M6): a signed-out reader is sent to sign in. */
export default async function TagLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
