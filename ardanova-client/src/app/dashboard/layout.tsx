import AuthenticatedLayout from "~/components/layouts/authenticated-layout";
import { ToastContainer } from "~/components/ui/toast";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthenticatedLayout>
      {children}
      <ToastContainer />
    </AuthenticatedLayout>
  );
}
