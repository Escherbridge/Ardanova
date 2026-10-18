import type { Metadata } from "next";
import Link from "next/link";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

export const metadata: Metadata = { title: "Terms of use" };

/** Route shell for the terms of use; text awaits human approval (conductor/tracks/legal-pages). */
export default function TermsPage() {
  return (
    <StatusPage
      eyebrow="Terms of use / Not yet published"
      title="ArdaNova has not published its terms of use yet."
      body={
        <>
          <p>
            The terms are being written. A person will review them before they
            appear on this page, together with the date they take effect.
          </p>
          <p className="mt-4">
            One thing the product already states holds today: platform records
            do not automatically create legal ownership, equity, or payment
            rights.
          </p>
        </>
      }
      actions={
        <>
          <Button asChild>
            <Link href="/">Back to the home page</Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/privacy">Privacy notice</Link>
          </Button>
        </>
      }
    />
  );
}
