"use client";

import Link from "next/link";
import { useEffect } from "react";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

/** Branded error boundary for any route segment below the root layout. */
export default function RouteError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <StatusPage
      eyebrow="Error / Page failed to load"
      title="This page could not be shown."
      body={
        <>
          <p>
            Something went wrong while loading it. This is a service error, not
            a sign that your records are missing.
          </p>
          {error.digest ? (
            <p className="mt-4 font-mono text-sm">
              Reference: <span className="select-all">{error.digest}</span>
            </p>
          ) : null}
        </>
      }
      actions={
        <>
          <Button type="button" onClick={reset}>
            Try again
          </Button>
          <Button asChild variant="outline">
            <Link href="/dashboard">Go to your dashboard</Link>
          </Button>
        </>
      }
    />
  );
}
