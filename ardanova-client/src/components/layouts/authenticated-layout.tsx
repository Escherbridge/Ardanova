import { TRPCReactProvider } from "~/trpc/react";
import { SessionProvider } from "next-auth/react";
import { auth } from "~/server/auth";
import { redirect } from "next/navigation";
import { AppSidebar } from "~/components/app-sidebar";

export default async function AuthenticatedLayout({
  children,
  wide = false,
}: {
  children: React.ReactNode;
  wide?: boolean;
}) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  return (
    <SessionProvider session={session}>
      <TRPCReactProvider>
        <div className={wide ? "flex min-h-screen" : "flex justify-center min-h-screen"}>
          <AppSidebar user={session.user} />
          <main className={wide ? "flex-1 transition-all duration-300" : "transition-all duration-300"}>
            {children}
          </main>
        </div>
      </TRPCReactProvider>
    </SessionProvider>
  );
}
