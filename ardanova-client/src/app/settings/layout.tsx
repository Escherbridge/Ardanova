import AuthenticatedLayout from "~/components/layouts/authenticated-layout";

import { SettingsNav } from "./settings-nav";

export default async function SettingsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthenticatedLayout>
      <SettingsNav />
      {children}
    </AuthenticatedLayout>
  );
}
