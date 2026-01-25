import { TRPCReactProvider } from "~/trpc/react";
import { SessionProvider } from "next-auth/react";
import { auth } from "~/server/auth";
import { Navigation } from "~/components/navigation";

export default async function GuildsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  return (
    <SessionProvider session={session}>
      <TRPCReactProvider>
        <Navigation user={session?.user} />
        {children}
      </TRPCReactProvider>
    </SessionProvider>
  );
}
