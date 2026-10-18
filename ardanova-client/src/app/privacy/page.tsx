import type { Metadata } from "next";
import Link from "next/link";

import { StatusPage } from "~/components/brand/status-page";
import { Button } from "~/components/ui/button";

export const metadata: Metadata = { title: "Privacy notice" };

/** Route shell for the privacy notice; text awaits human approval (conductor/tracks/legal-pages). */
export default function PrivacyPage() {
  return (
    <StatusPage
      eyebrow="Privacy notice / Not yet published"
      title="ArdaNova has not published its privacy notice yet."
      body={
        <>
          <p>
            The notice will say what ArdaNova stores, why it stores it, and who
            can see it. A person will review it before it appears on this page,
            together with the date it takes effect.
          </p>
          <p className="mt-4">
            You sign in with your Google account. ArdaNova receives the name,
            email address and profile image that Google shares when you sign in.
          </p>
        </>
      }
      actions={
        <>
          <Button asChild>
            <Link href="/">Back to the home page</Link>
          </Button>
          <Button asChild variant="outline">
            <Link href="/terms">Terms of use</Link>
          </Button>
        </>
      }
    />
  );
}
