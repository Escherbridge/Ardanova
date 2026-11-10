import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { NotesShell } from "~/components/brand/notes-shell";

import { shareImage } from "~/lib/site";

import { noteBySlug, noteCanonical, NoteBody, notes } from "../articles";

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
  const canonical = noteCanonical(note.slug);
  return {
    title: note.title,
    description: note.summary,
    alternates: { canonical },
    openGraph: {
      url: canonical,
      title: note.title,
      description: note.summary,
      images: [shareImage],
    },
  };
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
    <NotesShell kicker={note.date} title={note.title}>
      <NoteBody slug={note.slug} />
    </NotesShell>
  );
}
