import { TRPCReactProvider } from "~/trpc/react";
import { SessionProvider } from "next-auth/react";
import { auth } from "~/server/auth";
import { redirect } from "next/navigation";
import { AppSidebar } from "~/components/app-sidebar";
import { RealtimeProvider } from "~/providers/realtime-provider";

export default async function ChatsLayout({
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
        <div className="flex min-h-screen">
          <AppSidebar user={session.user} />
          <RealtimeProvider>
          <main className="flex-1 transition-all duration-300">
            {children}
          </main>
          </RealtimeProvider>
        </div>
      </TRPCReactProvider>
    </SessionProvider>
  );
}
