import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default function SwapLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
