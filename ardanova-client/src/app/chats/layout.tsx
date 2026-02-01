import AuthenticatedLayout from "~/components/layouts/authenticated-layout";
import { RealtimeProvider } from "~/providers/realtime-provider";

export default async function ChatsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthenticatedLayout wide>
      <RealtimeProvider>
        {children}
      </RealtimeProvider>
    </AuthenticatedLayout>
  );
}
