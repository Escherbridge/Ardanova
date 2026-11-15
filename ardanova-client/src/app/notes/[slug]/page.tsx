import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { RelatedLinks } from "~/components/brand/related-links";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { WorkspaceFunnelGate } from "~/components/brand/WorkspaceFunnelGate";
import { publicPageMetadata } from "~/lib/page-metadata";
import { articleSchema, breadcrumbList } from "~/lib/schema";
import { entryTitle, noteBySlug, noteCanonical, notes } from "~/lib/writing";

import { NoteBody } from "../articles";

export function generateStaticParams() {
  return notes.map((note) => ({ slug: note.slug }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const note = noteBySlug(slug);
  if (!note) return { title: "Notes" };
  return publicPageMetadata({
    path: `/notes/${note.slug}`,
    title: entryTitle(note),
    description: note.description,
    kind: "article",
    publishedAt: note.publishedAt,
  });
}

export default async function NotePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const note = noteBySlug(slug);
  if (!note) notFound();

  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Notes", path: "/notes" },
          { name: note.title, path: `/notes/${note.slug}` },
        ])}
      />
      <JsonLd
        data={articleSchema({
          headline: note.title,
          description: note.description,
          publishedAt: note.publishedAt,
          canonical: noteCanonical(note.slug),
          citations: note.citations,
        })}
      />
      <NotesShell
        kicker={`${note.publishedAt} · Escherbridge`}
        title={note.title}
        trail={[
          { href: "/", label: "Home" },
          { href: "/notes", label: "Notes" },
          { href: `/notes/${note.slug}`, label: note.title },
        ]}
      >
        <p className="mb-2">
          <Link
            className="inline-flex min-h-11 items-center font-mono text-xs font-semibold tracking-[0.12em] uppercase underline underline-offset-4"
            href="/notes"
          >
            ← All essays &amp; notes
          </Link>
        </p>
        <NoteBody slug={note.slug} />
        <RelatedLinks heading="Also worth reading" items={note.related} />
        <WorkspaceFunnelGate surface="note-detail" />
        <SimulationNotice includeAzoa includeLegalAdvice />
      </NotesShell>
    </>
  );
}
