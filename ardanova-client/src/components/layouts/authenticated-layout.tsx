import { TRPCReactProvider } from "~/trpc/react";
import { SessionProvider } from "next-auth/react";
import { auth } from "~/server/auth";
import { redirect } from "next/navigation";
import { AppSidebar } from "~/components/app-sidebar";

export default async function AuthenticatedLayout({
  children,
  wide,
}: {
  children: React.ReactNode;
  /** When true, main content uses full width (e.g. chats, tasks). */
  wide?: boolean;
}) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

  return (
    <SessionProvider session={session}>
      <TRPCReactProvider>
        <div className={"flex min-h-screen"}>
          <AppSidebar user={session.user} />
          <main
            className={
              wide
                ? "flex-1 min-w-0 w-full transition-all duration-300"
                : "flex-1 min-w-0 max-w-6xl mx-auto w-full px-4 sm:px-6 transition-all duration-300"
            }
          >
            {children}
          </main>
        </div>
      </TRPCReactProvider>
    </SessionProvider>
  );
}
