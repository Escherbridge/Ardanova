import type { Metadata } from "next";
import Link from "next/link";

import { NotesShell } from "~/components/brand/notes-shell";
import { publicCanonical, shareImage } from "~/lib/site";

import { notes } from "./articles";

const canonical = publicCanonical("/notes");

export const metadata: Metadata = {
  title: "Notes",
  description:
    "Two notes on records and agreements. The economy in the workspace is simulated.",
  alternates: { canonical },
  openGraph: { url: canonical, images: [shareImage] },
};

export default function NotesIndexPage() {
  return (
    <NotesShell title="Notes">
      <ul className="space-y-8">
        {notes.map((note) => (
          <li key={note.slug}>
            <p className="text-muted-foreground font-mono text-xs tracking-[0.14em] uppercase">
              {note.date}
            </p>
            <h2 className="mt-2 text-2xl">
              <Link
                className="underline-offset-4 hover:underline"
                href={`/notes/${note.slug}`}
              >
                {note.title}
              </Link>
            </h2>
            <p className="text-muted-foreground mt-3">{note.summary}</p>
          </li>
        ))}
      </ul>
    </NotesShell>
  );
}
