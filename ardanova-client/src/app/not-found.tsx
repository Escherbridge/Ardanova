import type { Metadata } from "next";
import Link from "next/link";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";
import { authForPage } from "~/server/auth";

export const metadata: Metadata = {
  title: "Page not found",
  description: "This page doesn't exist on ArdaNova.",
  robots: { index: false, follow: false },
};

/** Branded 404 for any unmatched route. */
export default async function NotFound() {
  const session = await authForPage();

  return (
    <StatusPage
      eyebrow="404"
      title="Page not found."
      body={<p>Nothing you have saved has changed.</p>}
      actions={
        <>
          <Button asChild>
            {session ? (
              <Link href="/dashboard">Open dashboard</Link>
            ) : (
              <Link href="/notes">Read the notes</Link>
            )}
          </Button>
          <Button asChild variant="outline">
            <Link href="/">Go home</Link>
          </Button>
        </>
      }
    />
  );
}
