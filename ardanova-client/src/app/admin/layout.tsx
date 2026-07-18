import AuthenticatedLayout from "~/components/layouts/authenticated-layout";
import {
  ARDANOVA_REQUEST_PATH_HEADER,
  buildSignInHref,
  normalizeInternalCallbackUrl,
} from "~/lib/auth-navigation";
import { auth } from "~/server/auth";
import { headers } from "next/headers";
import { redirect } from "next/navigation";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session) {
    const requestHeaders = await headers();
    const callbackUrl = normalizeInternalCallbackUrl(
      requestHeaders.get(ARDANOVA_REQUEST_PATH_HEADER) ?? undefined,
    );
    redirect(buildSignInHref(callbackUrl));
  }

  if (session.user.role !== "ADMIN") {
    redirect("/dashboard");
  }

  return <AuthenticatedLayout>{children}</AuthenticatedLayout>;
}
