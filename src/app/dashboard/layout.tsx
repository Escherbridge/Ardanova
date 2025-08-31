import { TRPCReactProvider } from "~/trpc/react";
import { SessionProvider } from "next-auth/react";
import { auth } from "~/server/auth";
import { redirect } from "next/navigation";
import { ToastContainer } from "~/components/ui/toast";
import { Navigation } from "~/components/navigation";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  return (
    <SessionProvider session={session}>
      <TRPCReactProvider>
        <Navigation user={session.user} />
        {children}
        <ToastContainer />
      </TRPCReactProvider>
    </SessionProvider>
  );
}
