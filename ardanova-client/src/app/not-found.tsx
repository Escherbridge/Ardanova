import type { Metadata } from "next";
import Link from "next/link";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

export const metadata: Metadata = { title: "Page not found" };

/** Branded 404 for any unmatched route. */
export default function NotFound() {
  return (
    <StatusPage
      eyebrow="404"
      title="Page not found."
      body={
        <p>Nothing you have saved has changed.</p>
      }
      actions={
        <>
          <Button asChild>
            <Link href="/dashboard">Open dashboard</Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/">Home page</Link>
          </Button>
        </>
      }
    />
  );
}
