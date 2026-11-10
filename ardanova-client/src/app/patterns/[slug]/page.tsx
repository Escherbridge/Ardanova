import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { articleSchema, breadcrumbList } from "~/lib/schema";
import { publicPageMetadata } from "~/lib/page-metadata";
import {
  isPatternSlug,
  patternBySlug,
  patternCanonical,
  patterns,
} from "~/lib/writing";

import { PatternBody } from "../bodies";

export function generateStaticParams() {
  return patterns.map((pattern) => ({ slug: pattern.slug }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const pattern = patternBySlug(slug);
  if (!pattern) return { title: "Patterns" };
  return publicPageMetadata({
    path: `/patterns/${pattern.slug}`,
    title: pattern.title,
    description: pattern.description,
    kind: "article",
    publishedAt: pattern.publishedAt,
  });
}

export default async function PatternPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const pattern = patternBySlug(slug);
  if (!pattern || !isPatternSlug(slug)) notFound();
  const canonical = patternCanonical(pattern.slug);

  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Patterns", path: "/patterns" },
          { name: pattern.title, path: `/patterns/${pattern.slug}` },
        ])}
      />
      <JsonLd
        data={articleSchema({
          headline: pattern.title,
          description: pattern.description,
          publishedAt: pattern.publishedAt,
          canonical,
          citations: pattern.citations,
        })}
      />
      <NotesShell
        kicker={`${pattern.publishedAt} · Escherbridge`}
        title={pattern.title}
        trail={[
          { href: "/", label: "Home" },
          { href: "/patterns", label: "Patterns" },
          { href: `/patterns/${pattern.slug}`, label: pattern.title },
        ]}
      >
        <PatternBody slug={slug} />
        <p>
          <Link className="underline underline-offset-4" href="/patterns">
            Back to patterns
          </Link>
        </p>
      </NotesShell>
    </>
  );
}
