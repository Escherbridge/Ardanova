import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { publicPageMetadata } from "~/lib/page-metadata";
import { breadcrumbList, itemList } from "~/lib/schema";
import { noteCanonical, notes } from "~/lib/writing";

const description =
  "Two notes on records of work and of decisions. The economy in this workspace is simulated. A record is not, by itself, a promise that anyone will pay you.";

export const metadata = publicPageMetadata({
  path: "/notes",
  title: "Notes",
  description,
});

export default function NotesIndexPage() {
  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Notes", path: "/notes" },
        ])}
      />
      <JsonLd
        data={itemList(
          "Notes",
          notes.map((note) => noteCanonical(note.slug)),
        )}
      />
      <NotesShell
        title="Notes"
        trail={[
          { href: "/", label: "Home" },
          { href: "/notes", label: "Notes" },
        ]}
      >
        <SimulationNotice />
        <ul className="space-y-8">
          {notes.map((note) => (
            <li key={note.slug}>
              <p className="text-muted-foreground font-mono text-xs tracking-[0.14em] uppercase">
                {note.publishedAt} · Escherbridge
              </p>
              <h2 className="mt-2 text-2xl">
                <Link
                  className="underline-offset-4 hover:underline"
                  href={`/notes/${note.slug}`}
                >
                  {note.title}
                </Link>
              </h2>
              <p className="text-muted-foreground mt-3">{note.description}</p>
            </li>
          ))}
        </ul>
        <p>
          <Link className="underline underline-offset-4" href="/patterns">
            Patterns
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/news">
            News
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/feed.xml">
            Feed
          </Link>
        </p>
      </NotesShell>
    </>
  );
}
