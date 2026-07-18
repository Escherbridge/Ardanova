import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

export default function PortfolioLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
