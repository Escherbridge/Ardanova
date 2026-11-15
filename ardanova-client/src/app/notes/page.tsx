import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { WorkspaceFunnelGate } from "~/components/brand/WorkspaceFunnelGate";
import { publicPageMetadata } from "~/lib/page-metadata";
import { NOTES_DESCRIPTION } from "~/lib/public-copy";
import { breadcrumbList, itemList } from "~/lib/schema";
import { noteCanonical, notes } from "~/lib/writing";

export const metadata = publicPageMetadata({
  path: "/notes",
  title: "Notes",
  description: NOTES_DESCRIPTION,
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
        kicker="Essays · Ownership, Solutionary Thinking & Social Ecology"
        title="Notes on building, owning, and stewarding together."
        trail={[
          { href: "/", label: "Home" },
          { href: "/notes", label: "Notes" },
        ]}
      >
        <p className="text-lg leading-relaxed">
          Essays on what happens when communities stop scrolling past problems
          and start building shared responses: organizing guilds around social
          ecology instead of extraction, keeping democratic voice apart from
          money contributed, and sharing in what a project creates without
          turning participants into tracked data.
        </p>

        <ul className="grid gap-5 pt-2">
          {notes.map((note) => (
            <li
              className="bg-brand-paper-bright border border-black/70 p-6"
              key={note.slug}
            >
              <p className="text-brand-signal-deep font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                {note.publishedAt} · Escherbridge
              </p>
              <h2 className="mt-2 text-2xl font-bold tracking-[-0.02em]">
                <Link
                  className="underline-offset-4 hover:underline"
                  href={`/notes/${note.slug}`}
                >
                  {note.title}
                </Link>
              </h2>
              <p className="text-muted-foreground mt-3 leading-relaxed">
                {note.description}
              </p>
              <p className="mt-4">
                <Link
                  className="inline-flex min-h-11 items-center font-semibold underline underline-offset-4"
                  href={`/notes/${note.slug}`}
                >
                  Read note
                </Link>
              </p>
            </li>
          ))}
        </ul>

        <WorkspaceFunnelGate surface="notes-index" />

        <SimulationNotice includeAzoa includeLegalAdvice />

        <p className="text-muted-foreground text-sm">
          <Link className="underline underline-offset-4" href="/patterns">
            Pattern library
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/news">
            News
          </Link>
          {" · "}
          <Link className="underline underline-offset-4" href="/feed.xml">
            RSS feed
          </Link>
        </p>
      </NotesShell>
    </>
  );
}
