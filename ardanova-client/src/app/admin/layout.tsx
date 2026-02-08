import AuthenticatedLayout from "~/components/layouts/authenticated-layout";
import { ToastContainer } from "~/components/ui/toast";
import { auth } from "~/server/auth";
import { redirect } from "next/navigation";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user?.role || session.user.role !== "ADMIN") {
    redirect("/dashboard");
  }

  return (
    <AuthenticatedLayout>
      {children}
      <ToastContainer />
    </AuthenticatedLayout>
  );
}
