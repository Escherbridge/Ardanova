import type { Metadata } from "next";
import Link from "next/link";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

export const metadata: Metadata = { title: "Page not found" };

/** Branded 404 for any unmatched route. */
export default function NotFound() {
  return (
    <StatusPage
      eyebrow="404 / Not found"
      title="There is no page at this address."
      body={
        <p>
          The link may be mistyped, or the record it pointed to was moved or
          removed. Nothing you have saved has changed.
        </p>
      }
      actions={
        <>
          <Button asChild>
            <Link href="/dashboard">Go to your dashboard</Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/">Back to the home page</Link>
          </Button>
        </>
      }
    />
  );
}
