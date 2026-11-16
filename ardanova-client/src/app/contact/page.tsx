import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { PublicEmailLine } from "~/components/brand/public-email";
import { CONTACT_DESCRIPTION } from "~/lib/public-copy";
import { publicPageMetadata } from "~/lib/page-metadata";
import { breadcrumbList } from "~/lib/schema";

export const metadata = publicPageMetadata({
  path: "/contact",
  title: "Contact",
  description: CONTACT_DESCRIPTION,
});

export default function ContactPage() {
  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Contact", path: "/contact" },
        ])}
      />
      <NotesShell
        title="Contact"
        trail={[
          { href: "/", label: "Home" },
          { href: "/contact", label: "Contact" },
        ]}
      >
        <p>
          Escherbridge, LLC operates ArdaNova. Questions about the workspace go
          to the operator.
        </p>
        <address className="space-y-1 not-italic">
          <p>Escherbridge, LLC, Boise, Idaho</p>
          <PublicEmailLine />
        </address>
        <p>
          There is no form on this page.{" "}
          <Link className="underline underline-offset-4" href="/terms">
            Terms of use
          </Link>{" "}
          and the{" "}
          <Link className="underline underline-offset-4" href="/privacy">
            privacy notice
          </Link>{" "}
          name the same operator.
        </p>
      </NotesShell>
    </>
  );
}
