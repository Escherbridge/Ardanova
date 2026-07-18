import { SessionProvider } from "next-auth/react";
import { headers } from "next/headers";
import { redirect } from "next/navigation";

import { AppSidebar } from "~/components/app-sidebar";
import { AppTopbar } from "~/components/app-topbar";
import {
  ARDANOVA_REQUEST_PATH_HEADER,
  buildSignInHref,
  normalizeInternalCallbackUrl,
} from "~/lib/auth-navigation";
import { authForPage, isDevelopmentAuthPreviewEnabled } from "~/server/auth";
import { cn } from "~/lib/utils";

export default async function AuthenticatedLayout({
  children,
  wide,
}: {
  children: React.ReactNode;
  /** Full-width workspaces include chats, task boards, and other dense artifacts. */
  wide?: boolean;
}) {
  const session = await authForPage();

  if (!session) {
    const requestHeaders = await headers();
    const callbackUrl = normalizeInternalCallbackUrl(
      requestHeaders.get(ARDANOVA_REQUEST_PATH_HEADER) ?? undefined,
    );
    redirect(buildSignInHref(callbackUrl));
  }

  return (
    <SessionProvider
      session={session}
      refetchOnWindowFocus={!isDevelopmentAuthPreviewEnabled}
    >
      <div className="bg-background flex min-h-screen" data-workspace-shell>
        <a className="skip-link" href="#workspace-content">
          Skip to workspace
        </a>
        <AppSidebar
          user={session.user}
          authPreview={isDevelopmentAuthPreviewEnabled}
        />
        <div className="min-w-0 flex-1">
          <AppTopbar
            user={session.user}
            authPreview={isDevelopmentAuthPreviewEnabled}
          />
          <main
            id="workspace-content"
            tabIndex={-1}
            className={cn(
              "w-full min-w-0",
              wide ? "p-0" : "mx-auto max-w-[96rem] px-4 pb-12 sm:px-6 lg:px-8",
            )}
          >
            {children}
          </main>
        </div>
      </div>
    </SessionProvider>
  );
}
