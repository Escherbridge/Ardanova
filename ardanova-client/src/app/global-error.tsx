"use client";

import "~/styles/globals.css";

import { useEffect } from "react";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

/** Last-resort boundary when the root layout itself fails; must render its own html/body. */
export default function GlobalError({
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
    <html lang="en">
      <body className="bg-background min-h-screen font-sans antialiased">
        <StatusPage
          eyebrow="Error / ArdaNova is unavailable"
          title="ArdaNova could not start this page."
          body={
            <>
              <p>
                The application failed before it could load. Your records are
                stored on the server and are not affected.
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
                {/* eslint-disable-next-line @next/next/no-html-link-for-pages -- global-error replaces the root layout and the client router may be what failed; a full document load is the point. */}
                <a href="/">Back to the home page</a>
              </Button>
            </>
          }
        />
      </body>
    </html>
  );
}
